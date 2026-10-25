"""Siebwalde K8B active-session orchestrator — protocol-free lifecycle glue.

Coordinates the two already-proven live mechanisms for the Diagnostics MCP:

* state-changing init (B2) + SLAVEINFO register readback (B3) + stop (B5) — run
  the proven C# ``SiebwaldeApp.Koploper.Validation.Host.exe`` as a bounded child
  process (``readonly-validate --live --read-registers --json --wait-ms N``);
* read-only command 113 (B1 identity probe / B4) — reuse the existing
  ``live_transport.Command113Transport`` + ``observe_guard.ObserveSession``.

This module is **pure lifecycle/safety/authorization glue**. It contains NO wire
framing, NO SLAVEINFO decode, and NO init state machine:

* it never constructs a PIC32 frame (no ``struct``, no ``HEADER``/``FOOTER``);
* it never decodes a SLAVEINFO payload (no ``slaveinfo_decoder`` import);
* the six-command init + the 12-register decode live exclusively in the C# host;
* command 113 lives exclusively in ``live_transport``.

Safety posture (fail-closed, no flash/reset):

* every ACTIVE/LIVE entry point validates + atomically consumes a scoped grant
  **before** any live I/O; with no valid grant it performs NO I/O;
* ownership is gated by the OS UDP bind (authoritative) + the in-process
  registry + an advisory lock file — it never terminates another application;
* 108/107/flash are structurally unreachable (the combined allowlist is
  {250,100,101,104,106,105,113} and is enforced by the C# guard + the 113
  transport, not re-implemented here).

Stdlib only. Socket and subprocess are used *only* inside the ACTIVE paths
(``_check_recv_port`` / ``_default_runner``) and never at import time, so the
offline tools remain socket- and subprocess-free.
"""

from __future__ import annotations

import errno
import hashlib
import itertools
import json
import os
import socket
import threading
from datetime import datetime, timezone
from pathlib import Path
from typing import Optional

from . import active_guard
from . import live_transport
from . import observe_guard
from . import session_authority
from .session_authority import (
    GrantAbsent,
    GrantConsumed,
    GrantExpired,
    GrantScopeMismatch,
    consume_grant,
    load_grant,
    validate_grant,
)

# --------------------------------------------------------------------------- #
# Paths / constants
# --------------------------------------------------------------------------- #

_REPO_ROOT = Path(__file__).resolve().parents[2]
_LOG_ROOT = _REPO_ROOT / "Logging" / "diagnostics-mcp"

DEFAULT_ENDPOINT = observe_guard.REQUIRED_ENDPOINT  # "192.168.1.193"
DEFAULT_TX_PORT = live_transport.DEFAULT_TX_PORT      # 10000
DEFAULT_RECV_PORT = live_transport.DEFAULT_RECV_PORT  # 10001

DEFAULT_WAIT_MS = 15000
MIN_WAIT_MS = 0
MAX_WAIT_MS = 120_000

# The standalone C# host artifact (framework-dependent, net8.0-windows7.0).
HOST_EXE_REL = Path(
    "SiebwaldeApp/SiebwaldeApp.Koploper.Validation.Host/bin/Debug/"
    "net8.0-windows7.0/SiebwaldeApp.Koploper.Validation.Host.exe"
)

STATUS_PASS = "PASS"
STATUS_FAILED = "FAILED"
STATUS_UNKNOWN = "UNKNOWN"

AUTHORIZATION_ABSENT = "authorization_absent"
SESSION_CONFLICT = "session_conflict"
CONTROLLER_UNAVAILABLE = "controller_unavailable"

# Always-zero movement/application write counters (by construction; the C# host
# has no write path for these and 108 is structurally unreachable).
_ZERO_MOVEMENT_WRITES = {
    "register_write_108": 0,
    "hr0": 0,
    "pwm": 0,
    "direction": 0,
    "brake": 0,
    "emo": 0,
    "switch": 0,
    "loco": 0,
}


# --------------------------------------------------------------------------- #
# Result / status helpers
# --------------------------------------------------------------------------- #


def _refusal(reason: str, detail: Optional[str] = None, **extra) -> dict:
    """Fail-closed refusal before any I/O: UNKNOWN + a machine-readable reason."""
    result = {
        "status": STATUS_UNKNOWN,
        "reason": reason,
        "evidence": {"reason": reason},
    }
    if detail is not None:
        result["evidence"]["detail"] = detail
    result.update(extra)
    return result


def _classification_for_reason(reason: str) -> str:
    return {
        AUTHORIZATION_ABSENT: active_guard.UNKNOWN,
        SESSION_CONFLICT: active_guard.SESSION_CONFLICT,
        CONTROLLER_UNAVAILABLE: active_guard.CONTROLLER_UNAVAILABLE,
    }.get(reason, active_guard.UNKNOWN)


def _run_refusal(reason: str, session_id=None, detail=None, grant_id=None) -> dict:
    return _refusal(
        reason,
        detail=detail,
        session_id=session_id,
        authorization={"grant_id": grant_id, "consumed": False} if grant_id else None,
        init=None,
        amplifiers=[],
        shutdown={"classification": _classification_for_reason(reason), "reason": detail or reason},
        audit_trail_file=None,
    )


def _status_for_classification(classification: str) -> str:
    if classification == active_guard.CLEAN_SHUTDOWN:
        return STATUS_PASS
    if classification in (
        active_guard.ACK_TIMEOUT,
        active_guard.INCOMPLETE_INIT,
        active_guard.CONTROLLER_UNAVAILABLE,
        active_guard.SESSION_CONFLICT,
    ):
        return STATUS_FAILED
    return STATUS_UNKNOWN  # safety_stop / po_intervention / unknown


def _reason_for_classification(classification: str) -> Optional[str]:
    return None if classification == active_guard.CLEAN_SHUTDOWN else classification


# --------------------------------------------------------------------------- #
# Input normalization
# --------------------------------------------------------------------------- #


def _clamp_wait_ms(wait_ms) -> int:
    if isinstance(wait_ms, bool) or not isinstance(wait_ms, int):
        raise ValueError("wait_ms must be an int")
    return max(MIN_WAIT_MS, min(MAX_WAIT_MS, wait_ms))


def _ms_to_seconds(wait_ms: int) -> float:
    return max(0.001, wait_ms / 1000.0)


def _resolve_endpoint(target_endpoint) -> str:
    if target_endpoint is None or target_endpoint == "":
        return DEFAULT_ENDPOINT
    if observe_guard.validate_endpoint(target_endpoint) != observe_guard.ALLOWED:
        raise ValueError(
            "target_endpoint %r is not the explicit allowed endpoint %r"
            % (target_endpoint, DEFAULT_ENDPOINT)
        )
    return str(target_endpoint).strip()


_RUN_COUNTER = itertools.count(1)


def _run_id(clock=None) -> str:
    n = next(_RUN_COUNTER)
    if clock is not None:
        stamp = clock().strftime("%H%M%S%f")
    else:
        stamp = datetime.now(timezone.utc).strftime("%H%M%S%f")
    return "%s-%d" % (stamp, n)


# --------------------------------------------------------------------------- #
# Active-session registry (in-process single-session)
# --------------------------------------------------------------------------- #


class ActiveSessionRegistry:
    """In-process single-active-session registry.

    Explicitly NOT the exclusive-access proof (the OS socket bind is
    authoritative). It only prevents a second concurrent session within this MCP
    process and records the child-process handle so :func:`abort_session` can
    stop a running session (kill — never flash/reset).
    """

    def __init__(self) -> None:
        self._lock = threading.Lock()
        self._session_id: Optional[str] = None
        self._proc = None
        self._aborted = False

    def try_begin(self, session_id: str) -> bool:
        with self._lock:
            if self._session_id is not None:
                return False
            self._session_id = session_id
            self._proc = None
            self._aborted = False
            return True

    def attach_process(self, session_id: str, proc) -> None:
        with self._lock:
            if self._session_id == session_id:
                self._proc = proc

    def current(self) -> Optional[str]:
        with self._lock:
            return self._session_id

    def current_process(self):
        with self._lock:
            return self._proc

    def mark_aborted(self, session_id: str) -> None:
        with self._lock:
            if self._session_id == session_id:
                self._aborted = True

    def was_aborted(self, session_id: str) -> bool:
        with self._lock:
            return self._session_id == session_id and self._aborted

    def end(self, session_id: str) -> None:
        with self._lock:
            if self._session_id == session_id:
                self._session_id = None
                self._proc = None
                self._aborted = False

    def reset(self) -> None:
        with self._lock:
            self._session_id = None
            self._proc = None
            self._aborted = False


_DEFAULT_REGISTRY = ActiveSessionRegistry()


# --------------------------------------------------------------------------- #
# Ownership: OS UDP bind (authoritative) + advisory lock
# --------------------------------------------------------------------------- #


def _default_socket_factory() -> socket.socket:
    return socket.socket(socket.AF_INET, socket.SOCK_DGRAM)


def _check_recv_port(recv_port: int = DEFAULT_RECV_PORT, socket_factory=None) -> dict:
    """Transient exclusive bind on the receive port (host-side, no transmission).

    Returns ``{"bound": True, "recv_port_bind": "free", ...}`` on success, or a
    fail-closed ``{"bound": False, ...}`` on EADDRINUSE / any bind error.
    """
    factory = socket_factory or _default_socket_factory
    sock = None
    try:
        sock = factory()
        sock.bind(("", recv_port))
        return {"bound": True, "recv_port_bind": "free", "error": None}
    except OSError as exc:
        in_use = getattr(exc, "errno", None) == errno.EADDRINUSE
        return {
            "bound": False,
            "recv_port_bind": "in_use" if in_use else "error",
            "error": str(exc),
        }
    finally:
        if sock is not None:
            try:
                sock.close()
            except Exception:
                pass


def _session_lock_path(lock_dir=None) -> Path:
    root = Path(lock_dir) if lock_dir is not None else _LOG_ROOT
    return root / "session.lock"


def _pid_alive(pid) -> bool:
    """True when a PID is alive. Never terminates another application.

    On POSIX uses ``os.kill(pid, 0)`` (existence check). On Windows opens a
    read-only ``PROCESS_QUERY_LIMITED_INFORMATION`` handle and checks the exit
    code — it never requests ``PROCESS_TERMINATE``.
    """
    if not isinstance(pid, int) or pid <= 0:
        return False
    if os.name != "nt":
        try:
            os.kill(pid, 0)
            return True
        except OSError:
            return False
    try:
        import ctypes  # lazy: Windows-only path
    except Exception:
        return False
    PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
    STILL_ACTIVE = 259
    try:
        kernel32 = ctypes.windll.kernel32
        handle = kernel32.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, False, pid)
        if not handle:
            return False
        try:
            exit_code = ctypes.c_ulong()
            if not kernel32.GetExitCodeProcess(handle, ctypes.byref(exit_code)):
                return False
            return exit_code.value == STILL_ACTIVE
        finally:
            kernel32.CloseHandle(handle)
    except Exception:
        return False


def _read_session_lock(lock_dir=None) -> dict:
    path = _session_lock_path(lock_dir)
    if not path.exists():
        return {"present": False, "owner_pid": None, "stale": False}
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {"present": True, "owner_pid": None, "stale": False}
    if not isinstance(data, dict):
        return {"present": True, "owner_pid": None, "stale": False}
    pid = data.get("pid")
    if isinstance(pid, int) and pid > 0:
        alive = _pid_alive(pid)
        return {"present": True, "owner_pid": pid, "stale": not alive}
    return {"present": True, "owner_pid": None, "stale": False}


def _stale_lock(lock_dir=None) -> bool:
    return _read_session_lock(lock_dir)["stale"]


def _acquire_session_lock(lock_dir=None, session_id=None, endpoint=None, pid=None, clock=None) -> dict:
    """Create the advisory session lock with exclusive create (cross-process)."""
    path = _session_lock_path(lock_dir)
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
    except OSError as exc:
        return {"acquired": False, "reason": "lock_error", "detail": str(exc)}
    now = clock() if clock is not None else datetime.now(timezone.utc)
    record = {
        "pid": pid if isinstance(pid, int) else os.getpid(),
        "endpoint": endpoint,
        "session_id": session_id,
        "started_at": now.isoformat(),
    }
    try:
        fd = os.open(str(path), os.O_CREAT | os.O_EXCL | os.O_WRONLY)
    except FileExistsError:
        return {"acquired": False, "reason": "lock_present"}
    except OSError as exc:
        return {"acquired": False, "reason": "lock_error", "detail": str(exc)}
    try:
        os.write(fd, json.dumps(record).encode("utf-8"))
    finally:
        os.close(fd)
    return {"acquired": True, "path": str(path)}


def _release_session_lock(lock_dir=None, session_id=None) -> None:
    path = _session_lock_path(lock_dir)
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
        if data.get("session_id") == session_id:
            path.unlink(missing_ok=True)
    except (OSError, json.JSONDecodeError):
        pass


def _reclaim_stale_lock(lock_dir=None, note=None) -> dict:
    path = _session_lock_path(lock_dir)
    try:
        path.unlink(missing_ok=True)
        return {"reclaimed": True, "note": note}
    except OSError as exc:
        return {"reclaimed": False, "note": note, "detail": str(exc)}


# --------------------------------------------------------------------------- #
# Subprocess (the ONLY init/readback path) — no framing/decode here
# --------------------------------------------------------------------------- #


def _resolve_host_command(repo_root=None, wait_ms: int = DEFAULT_WAIT_MS) -> dict:
    root = Path(repo_root) if repo_root is not None else _REPO_ROOT
    exe = root / HOST_EXE_REL
    if not exe.exists():
        return {"error": "host_exe_missing", "command": None, "exe": str(exe)}
    command = [
        str(exe),
        "readonly-validate",
        "--live",
        "--read-registers",
        "--json",
        "--wait-ms",
        str(wait_ms),
    ]
    return {"error": None, "command": command, "exe": str(exe)}


def _default_runner(command, wait_seconds: float) -> dict:
    """Run the C# host bounded by ``wait_seconds``, killing it on timeout."""
    import subprocess  # lazy: the ACTIVE path only

    proc = subprocess.Popen(
        command,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
    )
    try:
        stdout, _ = proc.communicate(timeout=wait_seconds)
        timed_out = False
    except subprocess.TimeoutExpired:
        proc.kill()
        stdout, _ = proc.communicate()
        timed_out = True
    return {
        "returncode": proc.returncode,
        "stdout": stdout or "",
        "timed_out": timed_out,
    }


def _run_host(command, wait_seconds: float, runner=None) -> dict:
    if runner is not None:
        return runner(command, wait_seconds)
    return _default_runner(command, wait_seconds)


def _parse_host_output(stdout) -> dict:
    """Parse the C# host's machine-readable JSON lines (no decode of frames)."""
    amplifiers = []
    summary = None
    conflict = None
    init_failure = None
    for line in (stdout or "").splitlines():
        line = line.strip()
        if not line:
            continue
        try:
            obj = json.loads(line)
        except json.JSONDecodeError:
            continue  # skip any non-JSON line (shouldn't appear with --json)
        if not isinstance(obj, dict):
            continue
        kind = obj.get("type")
        if kind == "amplifierIdentityReadback":
            amplifiers.append(obj)
        elif kind == "sessionSummary":
            summary = obj
        elif kind == "sessionConflict":
            conflict = obj
        elif kind == "initFailure":
            init_failure = obj
    return {
        "amplifiers": amplifiers,
        "summary": summary,
        "sessionConflict": conflict,
        "initFailure": init_failure,
    }


# --------------------------------------------------------------------------- #
# Result builders (readback shape mapping — no decode, no fabrication)
# --------------------------------------------------------------------------- #


def _comm_error_label(name) -> str:
    if name == "SLAVE_DATA_OK":
        return "OK"
    return name or "UNKNOWN"


def _map_amplifier(line: dict, observed_at: str) -> dict:
    mb_name = line.get("mbCommErrorName")
    return {
        "slave_number": line.get("slaveNumber"),
        "slave_detected_raw": line.get("slaveDetected"),
        "hr7_raw": line.get("hr7RawHex"),
        "hr11_checksum": line.get("swChecksumHex"),
        "hr21_raw": line.get("fwVersionMajorMinorHex"),
        "hr22_raw": line.get("fwVersionPatchProtocolHex"),
        "fw_version": line.get("fwVersion"),
        "firmware_protocol_support": line.get("versionStatus", "UNSUPPORTED"),
        "mb_comm_error": _comm_error_label(mb_name),
        "mb_comm_error_name": mb_name,
        "mb_comm_error_code": line.get("mbCommError"),
        "physical_section": line.get("physicalSection"),
        "observed_at_utc": observed_at,
        "last_data_received_utc": line.get("lastDataReceivedUtc"),
        "fresh": line.get("fresh"),
    }


def _build_amplifiers(readback_lines, observed_at: str) -> list:
    return [_map_amplifier(line, observed_at) for line in readback_lines]


def _build_init(summary_init, classification: str) -> dict:
    if isinstance(summary_init, dict):
        return {
            "sequence": list(summary_init.get("sequence", list(active_guard.COLD_START_ORDER))),
            "commands": summary_init.get("commands", []),
            "rejected_commands": summary_init.get("rejected_commands", 0),
            "order_violations": summary_init.get("order_violations", 0),
            "acknowledgement_timeouts": summary_init.get("acknowledgement_timeouts", 0),
            "movement_writes": summary_init.get("movement_writes", dict(_ZERO_MOVEMENT_WRITES)),
            "readiness": summary_init.get("readiness", "INCOMPLETE"),
        }

    # No summary (e.g. ack timeout / incomplete): synthesize fail-closed.
    commands = [
        {
            "command": cmd,
            "name": active_guard.COMMAND_NAMES.get(cmd, "0x%02X" % cmd),
            "transmitted": 0,
            "acknowledged": 0,
            "state": "NOT_SENT",
        }
        for cmd in active_guard.COLD_START_ORDER
    ]
    readiness = "FAILED" if classification == active_guard.ACK_TIMEOUT else "INCOMPLETE"
    ack_timeouts = 1 if classification == active_guard.ACK_TIMEOUT else 0
    return {
        "sequence": list(active_guard.COLD_START_ORDER),
        "commands": commands,
        "rejected_commands": 0,
        "order_violations": 0,
        "acknowledgement_timeouts": ack_timeouts,
        "movement_writes": dict(_ZERO_MOVEMENT_WRITES),
        "readiness": readiness,
    }


def _write_audit(audit_dir, session_id: str, record: dict) -> Optional[str]:
    root = Path(audit_dir) if audit_dir is not None else _LOG_ROOT / "audit"
    try:
        root.mkdir(parents=True, exist_ok=True)
        path = root / ("%s.jsonl" % session_id)
        with path.open("a", encoding="utf-8") as fh:
            fh.write(json.dumps(record) + "\n")
        return str(path)
    except OSError:
        return None


# --------------------------------------------------------------------------- #
# B4 — read-only command 113 (reuses the existing transport)
# --------------------------------------------------------------------------- #


def probe_controller_version(
    grant_id,
    directory=None,
    transport_factory=None,
    socket_factory=None,
) -> dict:
    """Send exactly one command-113 request under a consumed read-only grant.

    Reuses :class:`live_transport.Command113Transport` bound to a consumed
    :class:`observe_guard.ObserveSession`. No framing is added here.
    """
    if grant_id is None or (isinstance(grant_id, str) and not grant_id.strip()):
        return _refusal(AUTHORIZATION_ABSENT, detail="grant_absent")

    # 1. load + validate the grant (no I/O).
    try:
        grant = load_grant(grant_id, directory)
    except GrantAbsent as exc:
        return _refusal(AUTHORIZATION_ABSENT, detail="grant_absent: %s" % exc)
    except GrantConsumed as exc:
        return _refusal(AUTHORIZATION_ABSENT, detail="grant_consumed: %s" % exc)
    try:
        validate_grant(grant, session_authority.MODE_READ_ONLY, DEFAULT_ENDPOINT, [113])
    except GrantExpired as exc:
        return _refusal(AUTHORIZATION_ABSENT, detail="grant_expired: %s" % exc)
    except GrantScopeMismatch as exc:
        return _refusal(AUTHORIZATION_ABSENT, detail="grant_scope_mismatch: %s" % exc)

    # 2. consume the grant BEFORE any I/O.
    try:
        consume_grant(grant, directory)
    except GrantConsumed as exc:
        return _refusal(AUTHORIZATION_ABSENT, detail="grant_consumed: %s" % exc)
    except GrantAbsent as exc:
        return _refusal(AUTHORIZATION_ABSENT, detail="grant_absent: %s" % exc)

    # 3. build the transport bound to a consumed ObserveSession, then send.
    session = observe_guard.ObserveSession(113, DEFAULT_ENDPOINT)
    if transport_factory is not None:
        transport = transport_factory(session)
    else:
        transport = live_transport.Command113Transport(
            DEFAULT_ENDPOINT, socket_factory=socket_factory, session=session
        )
    result = transport.send_get_controller_version()
    result["authorization"] = {
        "grant_id": grant.grant_id,
        "consumed": True,
        "mode": session_authority.MODE_READ_ONLY,
    }
    if isinstance(result.get("evidence"), dict):
        result["evidence"]["grant_id"] = grant.grant_id
    return result


# --------------------------------------------------------------------------- #
# B1 — controller preflight (ownership + optional identity probe)
# --------------------------------------------------------------------------- #


def controller_preflight(
    include_identity_probe: bool = False,
    grant_id=None,
    target_endpoint=None,
    directory=None,
    lock_dir=None,
    socket_factory=None,
    transport_factory=None,
    clock=None,
) -> dict:
    """Ownership bind-check + advisory lock read; optional command-113 probe.

    The bind-check portion is a host-side socket action with no transmission and
    requires no grant. The identity probe sends one command-113 datagram and
    therefore requires a scoped read-only grant (``grant_id``); that grant is
    validated and consumed *before* any socket I/O, so a missing grant fails
    closed with ``authorization_absent`` and no I/O.
    """
    try:
        endpoint = _resolve_endpoint(target_endpoint)
    except ValueError as exc:
        return _refusal("invalid_endpoint", detail=str(exc))

    # A requested identity probe is validated/consumed FIRST (fail-closed before
    # any socket I/O): no valid read-only grant => authorization_absent, no I/O.
    probe = None
    if include_identity_probe:
        probe = probe_controller_version(
            grant_id,
            directory=directory,
            transport_factory=transport_factory,
            socket_factory=socket_factory,
        )
        if probe.get("status") != STATUS_PASS:
            return probe  # already a fail-closed refusal / UNKNOWN result

    recv = _check_recv_port(DEFAULT_RECV_PORT, socket_factory)
    lock_info = _read_session_lock(lock_dir)
    owned_by_other = recv["recv_port_bind"] != "free" or lock_info["present"]
    verdict = "owned_by_other" if owned_by_other else "available"

    result = {
        "status": STATUS_PASS if verdict == "available" else STATUS_FAILED,
        "reason": None,
        "configured_endpoint": endpoint,
        "tx_port": DEFAULT_TX_PORT,
        "recv_port": DEFAULT_RECV_PORT,
        "ownership": {
            "recv_port_bind": recv["recv_port_bind"],
            "session_lock_present": lock_info["present"],
            "session_owner_pid": lock_info["owner_pid"],
            "verdict": verdict,
        },
        "evidence": {"reason": None},
    }

    if probe is not None:
        evidence = probe.get("evidence") or {}
        result["identity"] = {
            "probed": True,
            "controller_version": probe.get("controller_version"),
            "physical_execution": evidence.get("physical_execution", "unknown"),
            "source_verified": True,
        }

    return result


# --------------------------------------------------------------------------- #
# B2 + B3 + B5 — the single bounded controller session
# --------------------------------------------------------------------------- #


def run_controller_session(
    grant_id,
    purpose,
    wait_ms: int = DEFAULT_WAIT_MS,
    target_endpoint=None,
    directory=None,
    registry=None,
    lock_dir=None,
    repo_root=None,
    socket_factory=None,
    subprocess_runner=None,
    clock=None,
    audit_dir=None,
) -> dict:
    """Run the validated cold-start init + readback + stop as one bounded child.

    Order: (1) load+validate grant, (2) in-process registry, (3) ownership
    bind pre-flight, (4) advisory lock, (5) consume grant, (6) resolve host,
    (7) run the bounded child process, (8) classify + audit. The grant is
    consumed before any live I/O; a failed ownership check leaves it unconsumed.
    """
    reg = registry or _DEFAULT_REGISTRY

    if grant_id is None or (isinstance(grant_id, str) and not grant_id.strip()):
        return _run_refusal(AUTHORIZATION_ABSENT, detail="grant_absent")

    try:
        wait_ms = _clamp_wait_ms(wait_ms)
    except ValueError as exc:
        return _run_refusal("invalid_wait_ms", detail=str(exc))

    try:
        endpoint = _resolve_endpoint(target_endpoint)
    except ValueError as exc:
        return _run_refusal("invalid_endpoint", detail=str(exc))

    # 1. grant (no I/O).
    try:
        grant = load_grant(grant_id, directory)
    except GrantAbsent as exc:
        return _run_refusal(AUTHORIZATION_ABSENT, detail=str(exc))
    except GrantConsumed as exc:
        return _run_refusal(AUTHORIZATION_ABSENT, detail=str(exc))
    try:
        validate_grant(grant, session_authority.MODE_SESSION, endpoint, active_guard.ACTIVE_COMMANDS)
    except GrantExpired as exc:
        return _run_refusal(AUTHORIZATION_ABSENT, detail=str(exc))
    except GrantScopeMismatch as exc:
        return _run_refusal(AUTHORIZATION_ABSENT, detail=str(exc))

    session_id = "k8b-%s-%s" % (grant.grant_id, _run_id(clock))

    # 2. in-process registry.
    if not reg.try_begin(session_id):
        return _run_refusal(SESSION_CONFLICT, session_id=None, detail="another_session_active")

    acquired_lock = False
    try:
        # 3. ownership bind pre-flight.
        recv = _check_recv_port(DEFAULT_RECV_PORT, socket_factory)
        if not recv["bound"]:
            return _run_refusal(SESSION_CONFLICT, session_id=session_id, detail=recv["recv_port_bind"])

        # 4. advisory session lock (with stale reclamation).
        lock = _acquire_session_lock(lock_dir, session_id=session_id, endpoint=endpoint, clock=clock)
        if not lock["acquired"] and _stale_lock(lock_dir):
            _reclaim_stale_lock(lock_dir, note="stale_lock_reclaimed")
            lock = _acquire_session_lock(lock_dir, session_id=session_id, endpoint=endpoint, clock=clock)
        if not lock["acquired"]:
            return _run_refusal(SESSION_CONFLICT, session_id=session_id, detail="session_lock_held")
        acquired_lock = True

        # 5. consume the grant BEFORE live I/O.
        try:
            consume_grant(grant, directory)
        except GrantConsumed:
            return _run_refusal(AUTHORIZATION_ABSENT, session_id=session_id, detail="grant_consumed")
        except GrantAbsent:
            return _run_refusal(AUTHORIZATION_ABSENT, session_id=session_id, detail="grant_absent")

        # 6. resolve the host command.
        host = _resolve_host_command(repo_root, wait_ms)
        if host["error"] is not None:
            return _run_refusal(CONTROLLER_UNAVAILABLE, session_id=session_id, detail=host["error"])

        # 7. run the bounded child process (the ONLY init/readback path).
        proc = _run_host(host["command"], _ms_to_seconds(wait_ms), subprocess_runner)
        parsed = _parse_host_output(proc["stdout"])
        aborted = reg.was_aborted(session_id)
        classification = active_guard.classify_shutdown(
            proc["returncode"], proc["stdout"], proc["timed_out"], aborted
        )

        # 8. build the result + audit trail.
        return _build_session_result(
            session_id, grant, endpoint, purpose, wait_ms, parsed, classification, proc,
            audit_dir, clock,
        )
    finally:
        if acquired_lock:
            _release_session_lock(lock_dir, session_id)
        reg.end(session_id)


def _build_session_result(
    session_id, grant, endpoint, purpose, wait_ms, parsed, classification, proc,
    audit_dir, clock,
) -> dict:
    now = clock() if clock is not None else datetime.now(timezone.utc)
    observed_at = now.isoformat()
    summary = parsed["summary"]
    init = _build_init(summary.get("init") if summary else None, classification)
    amplifiers = _build_amplifiers(parsed["amplifiers"], observed_at)
    status = _status_for_classification(classification)

    audit_file = _write_audit(audit_dir, session_id, {
        "type": "controller_session",
        "session_id": session_id,
        "grant_id": grant.grant_id,
        "purpose": purpose,
        "endpoint": endpoint,
        "wait_ms": wait_ms,
        "started_at": observed_at,
        "status": status,
        "shutdown": classification,
        "amplifier_count": len(amplifiers),
        "stdout_sha256": hashlib.sha256((proc.get("stdout") or "").encode("utf-8")).hexdigest(),
    })

    return {
        "status": status,
        "reason": None,
        "session_id": session_id,
        "authorization": {"grant_id": grant.grant_id, "consumed": True},
        "init": init,
        "amplifiers": amplifiers,
        "shutdown": {"classification": classification, "reason": _reason_for_classification(classification)},
        "audit_trail_file": audit_file,
        "evidence": {"reason": None},
    }


# --------------------------------------------------------------------------- #
# B5 — explicit safety stop
# --------------------------------------------------------------------------- #


def abort_session(session_id=None, registry=None) -> dict:
    """Kill the tracked child process (no flash/reset) and classify the stop."""
    reg = registry or _DEFAULT_REGISTRY
    current = reg.current()
    if current is None:
        return {
            "status": STATUS_UNKNOWN,
            "reason": "no_active_session",
            "session_id": None,
            "shutdown": {"classification": active_guard.UNKNOWN, "reason": "no_active_session"},
        }
    if session_id is not None and session_id != current:
        return {
            "status": STATUS_UNKNOWN,
            "reason": "session_id_mismatch",
            "session_id": current,
            "shutdown": {"classification": active_guard.UNKNOWN, "reason": "session_id_mismatch"},
        }
    proc = reg.current_process()
    if proc is not None:
        try:
            proc.kill()
        except Exception:
            pass
    reg.mark_aborted(current)
    reg.end(current)
    return {
        "status": STATUS_UNKNOWN,
        "reason": None,
        "session_id": current,
        "shutdown": {"classification": active_guard.SAFETY_STOP, "reason": "explicit_abort"},
    }
