"""Offline orchestration tests for the K8B active-session orchestrator.

Uses injected fakes (socket factory, subprocess runner, grant directory, lock
directory, audit directory, registry) so no hardware, socket, or child process
is ever touched. Verifies: grant-gating with no I/O, ownership bind conflict,
subprocess stdout parsing to the B3 readback shape, ack-timeout classification,
host-exe-missing classification, single-session registry, explicit abort, and
the "no framing / no SLAVEINFO decode in Python" source guard.
"""

from __future__ import annotations

import ast
import errno
import json
import sys
from pathlib import Path

import pytest

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics import active_guard, active_session  # noqa: E402
from tools.diagnostics.active_session import (  # noqa: E402
    ActiveSessionRegistry,
    abort_session,
    controller_preflight,
    probe_controller_version,
    run_controller_session,
)
from tools.diagnostics.session_authority import (  # noqa: E402
    MODE_READ_ONLY,
    MODE_SESSION,
    Grant,
    GrantDirectory,
)

ENDPOINT = "192.168.1.193"


def _session_grant(grant_id="K8B-B3-2026-10-25"):
    return Grant(
        grant_id=grant_id,
        purpose="K8B B3 physical register readback",
        target_endpoint=ENDPOINT,
        allowed_commands=frozenset({250, 100, 101, 104, 106, 105}),
        allowed_modes=frozenset({MODE_SESSION}),
        issued_by="PO + Project Lead",
        issued_at="2026-10-25T00:00:00Z",
        expires_at="2999-01-01T00:00:00Z",
        max_uses=1,
        revision="a7df6d26",
    )


def _readonly_grant(grant_id="K8B-B4-2026-10-25"):
    return Grant(
        grant_id=grant_id,
        purpose="K8B B4 controller identity",
        target_endpoint=ENDPOINT,
        allowed_commands=frozenset({113}),
        allowed_modes=frozenset({MODE_READ_ONLY}),
        issued_by="PO + Project Lead",
        issued_at="2026-10-25T00:00:00Z",
        expires_at="2999-01-01T00:00:00Z",
        max_uses=1,
        revision="a7df6d26",
    )


class FakeSocket:
    def __init__(self):
        self.closed = False
        self.bound = None

    def bind(self, addr):
        self.bound = addr

    def close(self):
        self.closed = True


class FakeSocketInUse:
    def bind(self, addr):
        raise OSError(errno.EADDRINUSE, "address already in use")

    def close(self):
        pass


class FakeProcess:
    def __init__(self):
        self.killed = False

    def kill(self):
        self.killed = True


def _boom_socket_factory():
    raise AssertionError("socket factory must not be called (no I/O allowed)")


def _boom_runner(command, wait_seconds):
    raise AssertionError("subprocess runner must not be called (no I/O allowed)")


def _touch_host_exe(root):
    exe = Path(root) / active_session.HOST_EXE_REL
    exe.parent.mkdir(parents=True, exist_ok=True)
    exe.write_bytes(b"")
    return exe


_READBACK_LINE = {
    "type": "amplifierIdentityReadback",
    "slaveNumber": 1,
    "slaveDetected": 0,
    "hr7Raw": 45,
    "hr7RawHex": "0x002D",
    "swChecksum": 0x251F,
    "swChecksumHex": "0x251F",
    "fwVersionMajorMinor": 0,
    "fwVersionMajorMinorHex": "0x0000",
    "fwVersionPatchProtocol": 0,
    "fwVersionPatchProtocolHex": "0x0000",
    "fwVersion": None,
    "fwProtocolVersion": None,
    "versionStatus": "UNSUPPORTED",
    "mbCommError": 2,
    "mbCommErrorName": "SLAVE_DATA_OK",
    "physicalSection": 1,
    "protocolReadbackObserved": True,
    "lastDataReceivedUtc": "2026-10-25T00:00:00Z",
    "fresh": True,
}

_SUMMARY_LINE = {
    "type": "sessionSummary",
    "live": True,
    "profile": "Simple Loop",
    "init": {
        "sequence": [250, 100, 101, 104, 106, 105],
        "commands": [
            {"command": 250, "name": "CLIENT_CONNECTION_REQUEST", "transmitted": 1, "acknowledged": 1, "state": "DONE"},
            {"command": 100, "name": "EXEC_MBUS_STATE_SLAVES_ON", "transmitted": 1, "acknowledged": 1, "state": "DONE"},
            {"command": 101, "name": "EXEC_MBUS_STATE_SLAVE_DETECT", "transmitted": 1, "acknowledged": 1, "state": "DONE"},
            {"command": 104, "name": "EXEC_MBUS_STATE_SLAVE_INIT", "transmitted": 1, "acknowledged": 1, "state": "DONE"},
            {"command": 106, "name": "EXEC_MBUS_STATE_START_DATA_UPLOAD", "transmitted": 1, "acknowledged": 1, "state": "DONE"},
            {"command": 105, "name": "EXEC_MBUS_STATE_SLAVE_ENABLE", "transmitted": 1, "acknowledged": 1, "state": "DONE"},
        ],
        "rejected_commands": 0,
        "order_violations": 0,
        "acknowledgement_timeouts": 0,
        "movement_writes": {"register_write_108": 0, "hr0": 0, "pwm": 0, "direction": 0,
                            "brake": 0, "emo": 0, "switch": 0, "loco": 0},
        "readiness": "READY",
    },
}


def _clean_run_result():
    return {
        "returncode": 0,
        "stdout": json.dumps(_READBACK_LINE) + "\n" + json.dumps(_SUMMARY_LINE) + "\n",
        "timed_out": False,
    }


# --------------------------------------------------------------------------- #
# Grant-gating (no I/O)
# --------------------------------------------------------------------------- #

def test_run_session_without_grant_refuses_without_io(tmp_path):
    d = GrantDirectory(tmp_path)
    res = run_controller_session(
        "missing", "purpose", registry=ActiveSessionRegistry(), directory=d,
        socket_factory=_boom_socket_factory, subprocess_runner=_boom_runner,
        lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["reason"] == "authorization_absent"
    assert res["status"] == "UNKNOWN"


def test_probe_without_grant_refuses(tmp_path):
    d = GrantDirectory(tmp_path)
    res = probe_controller_version(None, directory=d)
    assert res["reason"] == "authorization_absent"


def test_probe_without_grant_does_not_touch_transport(tmp_path):
    d = GrantDirectory(tmp_path)
    called = []

    def transport_factory(session):
        called.append(session)
        raise AssertionError("transport must not be built")

    res = probe_controller_version("missing", directory=d, transport_factory=transport_factory)
    assert res["reason"] == "authorization_absent"
    assert called == []


def test_preflight_without_grant_probe_refuses(tmp_path):
    res = controller_preflight(
        include_identity_probe=True, grant_id=None,
        socket_factory=lambda: FakeSocket(), lock_dir=tmp_path,
    )
    assert res["reason"] == "authorization_absent"


def test_preflight_ownership_check_needs_no_grant(tmp_path):
    res = controller_preflight(
        include_identity_probe=False,
        socket_factory=lambda: FakeSocket(), lock_dir=tmp_path,
    )
    assert res["status"] == "PASS"
    assert res["ownership"]["verdict"] == "available"
    assert res["ownership"]["recv_port_bind"] == "free"


# --------------------------------------------------------------------------- #
# Ownership conflict
# --------------------------------------------------------------------------- #

def test_run_session_bind_conflict(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    _touch_host_exe(tmp_path)
    res = run_controller_session(
        "K8B-B3-2026-10-25", "purpose", registry=ActiveSessionRegistry(), directory=d,
        socket_factory=lambda: FakeSocketInUse(), subprocess_runner=_boom_runner,
        repo_root=tmp_path, lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["reason"] == "session_conflict"
    # The grant was NOT consumed (ownership failed before consume).
    assert d.exists("K8B-B3-2026-10-25")


def test_run_session_second_concurrent_session_conflicts(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    _touch_host_exe(tmp_path)
    reg = ActiveSessionRegistry()
    assert reg.try_begin("k8b-other-1") is True
    res = run_controller_session(
        "K8B-B3-2026-10-25", "purpose", registry=reg, directory=d,
        socket_factory=_boom_socket_factory, subprocess_runner=_boom_runner,
        repo_root=tmp_path, lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["reason"] == "session_conflict"
    assert d.exists("K8B-B3-2026-10-25")  # still not consumed


# --------------------------------------------------------------------------- #
# Host availability
# --------------------------------------------------------------------------- #

def test_run_session_host_exe_missing(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    res = run_controller_session(
        "K8B-B3-2026-10-25", "purpose", registry=ActiveSessionRegistry(), directory=d,
        socket_factory=lambda: FakeSocket(), subprocess_runner=_boom_runner,
        repo_root=tmp_path, lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["reason"] == "controller_unavailable"


# --------------------------------------------------------------------------- #
# Successful parse (clean shutdown)
# --------------------------------------------------------------------------- #

def test_run_session_parses_readback_and_summary(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    _touch_host_exe(tmp_path)
    reg = ActiveSessionRegistry()
    res = run_controller_session(
        "K8B-B3-2026-10-25", "purpose", registry=reg, directory=d,
        socket_factory=lambda: FakeSocket(), subprocess_runner=lambda c, w: _clean_run_result(),
        repo_root=tmp_path, lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["status"] == "PASS"
    assert res["shutdown"]["classification"] == "clean_shutdown"
    assert res["authorization"]["consumed"] is True
    assert res["init"]["readiness"] == "READY"
    assert res["init"]["rejected_commands"] == 0
    amp = res["amplifiers"][0]
    assert amp["slave_number"] == 1
    assert amp["hr7_raw"] == "0x002D"          # legacy HR7 is raw, not identity
    assert amp["hr11_checksum"] == "0x251F"
    assert amp["fw_version"] is None
    assert amp["firmware_protocol_support"] == "UNSUPPORTED"
    assert amp["mb_comm_error"] == "OK"
    assert amp["physical_section"] == 1
    assert res["audit_trail_file"] is not None
    assert d.is_consumed("K8B-B3-2026-10-25")  # grant consumed before I/O
    assert reg.current() is None                # registry released


# --------------------------------------------------------------------------- #
# Ack timeout
# --------------------------------------------------------------------------- #

def test_run_session_ack_timeout(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    _touch_host_exe(tmp_path)

    def runner(command, wait_seconds):
        return {
            "returncode": 4,
            "stdout": json.dumps(
                {"type": "initFailure", "command": 105,
                 "commandName": "EXEC_MBUS_STATE_SLAVE_ENABLE", "reason": "ack_timeout"}
            ) + "\n",
            "timed_out": False,
        }

    res = run_controller_session(
        "K8B-B3-2026-10-25", "purpose", registry=ActiveSessionRegistry(), directory=d,
        socket_factory=lambda: FakeSocket(), subprocess_runner=runner,
        repo_root=tmp_path, lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["status"] == "FAILED"
    assert res["shutdown"]["classification"] == "ack_timeout"
    assert res["init"]["readiness"] == "FAILED"
    assert res["init"]["acknowledgement_timeouts"] == 1


# --------------------------------------------------------------------------- #
# Registry + abort
# --------------------------------------------------------------------------- #

def test_registry_rejects_second_concurrent_session():
    reg = ActiveSessionRegistry()
    assert reg.try_begin("a") is True
    assert reg.try_begin("b") is False
    reg.end("a")
    assert reg.try_begin("b") is True


def test_abort_session_kills_and_classifies_safety_stop():
    reg = ActiveSessionRegistry()
    reg.try_begin("k8b-x-1")
    fake_proc = FakeProcess()
    reg.attach_process("k8b-x-1", fake_proc)
    res = abort_session("k8b-x-1", registry=reg)
    assert res["shutdown"]["classification"] == active_guard.SAFETY_STOP
    assert fake_proc.killed is True
    assert reg.current() is None


def test_abort_session_with_no_active_session():
    reg = ActiveSessionRegistry()
    res = abort_session(registry=reg)
    assert res["reason"] == "no_active_session"


# --------------------------------------------------------------------------- #
# Probe with a read-only grant (injected transport)
# --------------------------------------------------------------------------- #

def test_probe_with_grant_consumes_and_calls_transport(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_readonly_grant())

    class FakeTransport:
        def __init__(self, session):
            self.session = session

        def send_get_controller_version(self):
            return {
                "status": "PASS",
                "controller_version": {
                    "major": 0, "minor": 1, "patch": 0,
                    "protocol": 0, "build_timestamp": "2026-10-20",
                },
                "evidence": {"physical_execution": "unknown", "request_bytes": "aa71"},
            }

    res = probe_controller_version(
        "K8B-B4-2026-10-25", directory=d, transport_factory=lambda s: FakeTransport(s)
    )
    assert res["status"] == "PASS"
    assert res["controller_version"]["minor"] == 1
    assert res["authorization"]["consumed"] is True
    assert d.is_consumed("K8B-B4-2026-10-25")


# --------------------------------------------------------------------------- #
# Source guard: no framing / no SLAVEINFO decode in Python
# --------------------------------------------------------------------------- #

def test_active_session_has_no_framing_or_slaveinfo_decode():
    src = Path(active_session.__file__).read_text(encoding="utf-8")
    tree = ast.parse(src)
    forbidden = {"struct", "slaveinfo_decoder", "Comm", "DataAquisition", "flash_one", "bootload"}
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                assert alias.name.split(".")[0] not in forbidden, alias.name
        elif isinstance(node, ast.ImportFrom) and node.level == 0:
            root = (node.module or "").split(".")[0]
            assert root not in forbidden, node.module
    # The orchestrator must not re-declare a wire-frame builder.
    assert not hasattr(active_session, "build_get_controller_version_request")
    assert not hasattr(active_session, "decode_controller_version_response")


def test_active_guard_has_no_socket_or_subprocess():
    src = Path(active_guard.__file__).read_text(encoding="utf-8")
    tree = ast.parse(src)
    forbidden = {"socket", "subprocess"}
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                assert alias.name.split(".")[0] not in forbidden, alias.name
        elif isinstance(node, ast.ImportFrom) and node.level == 0:
            root = (node.module or "").split(".")[0]
            assert root not in forbidden, node.module
