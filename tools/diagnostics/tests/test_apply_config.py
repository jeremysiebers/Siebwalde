"""Offline tests for the grant-gated apply-config Diagnostics MCP capability.

Covers: the ``apply_config`` grant mode (session_authority), the apply-config
command set + shutdown classification (active_guard), the bounded apply-config
session orchestrator (active_session, injected fakes, no hardware/socket/child),
and the MCP tool surface (active_tools + capabilities). All offline.
"""

from __future__ import annotations

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
    apply_config_session,
)
from tools.diagnostics.mcp_server import active_tools, server  # noqa: E402
from tools.diagnostics.session_authority import (  # noqa: E402
    MODE_APPLY_CONFIG,
    MODE_SESSION,
    Grant,
    GrantDirectory,
    validate_grant,
)

ENDPOINT = "192.168.1.193"
APPLY_CONFIG_COMMANDS = {250, 100, 101, 104, 106, 105, 108, 110}


def _apply_config_grant(grant_id="K9-AC-2026-10-27"):
    return Grant(
        grant_id=grant_id,
        purpose="safe config/APPLY diagnostic",
        target_endpoint=ENDPOINT,
        allowed_commands=frozenset(APPLY_CONFIG_COMMANDS),
        allowed_modes=frozenset({MODE_APPLY_CONFIG}),
        issued_by="PO + Project Lead",
        issued_at="2026-10-27T00:00:00Z",
        expires_at="2999-01-01T00:00:00Z",
        max_uses=1,
        revision="038a9937",
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


def _boom_socket_factory():
    raise AssertionError("socket factory must not be called (no I/O allowed)")


def _boom_runner(command, wait_seconds):
    raise AssertionError("subprocess runner must not be called (no I/O allowed)")


def _touch_host_exe(root):
    exe = Path(root) / active_session.HOST_EXE_REL
    exe.parent.mkdir(parents=True, exist_ok=True)
    exe.write_bytes(b"")
    return exe


_AMP_LINE = {
    "type": "amplifierConfigApply",
    "slaveNumber": 1,
    "detected": True,
    "compatible": True,
    "incompatibilityReason": None,
    "identityRaw": 0xF101,
    "identityRawHex": "0xF101",
    "protocolVersion": 1,
    "identityVerified": True,
    "disableAccepted": True,
    "disableVerified": True,
    "stateAfterDisable": 0xC003,
    "enableActiveAfterDisable": False,
    "inhibitedAfterDisable": True,
    "configDownloaded": True,
    "configVerified": True,
    "initDoneConfigOk": True,
    "stateAfterApply": 0xC601,
    "faultRaw": 0,
    "inhibited": True,
    "appliedPwm": 399,
    "pwmCommand": 0,
    "pwm3Enabled": False,
    "brakeActive": False,
    "failureCommand": None,
    "failureResponse": None,
    "failure": None,
}

_SUMMARY_LINE = {
    "type": "applyConfigSummary",
    "status": "PASS",
    "failure": None,
    "failedCommand": None,
    "failedCommandName": None,
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
    "heartbeat": {
        "supervised": True,
        "heartbeat_sent": 3,
        "global_stop_sent": 0,
        "global_stop_state": "Armed",
        "global_stop_reason": None,
        "last_observed_pic32_status": 3,
        "latched": False,
    },
    "amplifierCount": 1,
}


def _clean_apply_config_stdout():
    return json.dumps(_AMP_LINE) + "\n" + json.dumps(_SUMMARY_LINE) + "\n"


# --------------------------------------------------------------------------- #
# session_authority — the distinct apply_config grant mode
# --------------------------------------------------------------------------- #

def test_apply_config_mode_is_known():
    from tools.diagnostics.session_authority import KNOWN_MODES
    assert MODE_APPLY_CONFIG in KNOWN_MODES


def test_apply_config_grant_validates(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_apply_config_grant())
    g = validate_grant(
        d.load("K9-AC-2026-10-27"), MODE_APPLY_CONFIG, ENDPOINT,
        list(APPLY_CONFIG_COMMANDS),
    )
    assert g.allowed_modes == frozenset({MODE_APPLY_CONFIG})


def test_session_grant_does_not_authorise_apply_config(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(Grant(
        grant_id="K9-SESSION", purpose="session", target_endpoint=ENDPOINT,
        allowed_commands=frozenset({250, 100, 101, 104, 106, 105}),
        allowed_modes=frozenset({MODE_SESSION}),
        issued_by="x", issued_at="2026-10-27T00:00:00Z",
        expires_at="2999-01-01T00:00:00Z",
    ))
    from tools.diagnostics.session_authority import GrantScopeMismatch
    with pytest.raises(GrantScopeMismatch):
        validate_grant(d.load("K9-SESSION"), MODE_APPLY_CONFIG, ENDPOINT, list(APPLY_CONFIG_COMMANDS))


def test_apply_config_grant_requires_108_and_110(tmp_path):
    d = GrantDirectory(tmp_path)
    # Missing 108 (the register write vector) => scope mismatch.
    d.write(Grant(
        grant_id="K9-NO108", purpose="no108", target_endpoint=ENDPOINT,
        allowed_commands=frozenset({250, 100, 101, 104, 106, 105, 110}),
        allowed_modes=frozenset({MODE_APPLY_CONFIG}),
        issued_by="x", issued_at="2026-10-27T00:00:00Z",
        expires_at="2999-01-01T00:00:00Z",
    ))
    from tools.diagnostics.session_authority import GrantScopeMismatch
    with pytest.raises(GrantScopeMismatch):
        validate_grant(d.load("K9-NO108"), MODE_APPLY_CONFIG, ENDPOINT, list(APPLY_CONFIG_COMMANDS))


# --------------------------------------------------------------------------- #
# active_guard — command set + shutdown classification
# --------------------------------------------------------------------------- #

def test_apply_config_commands_are_exact():
    assert set(active_guard.APPLY_CONFIG_COMMANDS) == APPLY_CONFIG_COMMANDS


def test_apply_config_commands_not_in_readonly_allowlist():
    # The read-only/session allowlist is unchanged (no 108/110).
    assert 108 not in active_guard.ACTIVE_ALLOWLIST
    assert 110 not in active_guard.ACTIVE_ALLOWLIST


def test_classify_apply_config_shutdown_clean():
    assert active_guard.classify_apply_config_shutdown(0, _clean_apply_config_stdout(), False, False) == active_guard.CLEAN_SHUTDOWN
    # A FAILED config still completes cleanly (exit 1 + applyConfigSummary).
    failed = json.dumps({**_SUMMARY_LINE, "status": "FAILED", "failure": "no_compatible_amplifiers"}) + "\n"
    assert active_guard.classify_apply_config_shutdown(1, failed, False, False) == active_guard.CLEAN_SHUTDOWN


def test_classify_apply_config_shutdown_init_failure():
    stdout = json.dumps({"type": "initFailure", "command": 100, "commandName": "EXEC_MBUS_STATE_SLAVES_ON", "reason": "ack_timeout"}) + "\n"
    assert active_guard.classify_apply_config_shutdown(1, stdout, False, False) == active_guard.ACK_TIMEOUT


def test_classify_apply_config_shutdown_timeout_and_unknown():
    assert active_guard.classify_apply_config_shutdown(0, "", True, False) == active_guard.INCOMPLETE_INIT
    assert active_guard.classify_apply_config_shutdown(0, "", False, False) == active_guard.UNKNOWN


# --------------------------------------------------------------------------- #
# active_session.apply_config_session — grant-gating, no I/O
# --------------------------------------------------------------------------- #

def test_apply_config_without_grant_refuses_without_io(tmp_path):
    d = GrantDirectory(tmp_path)
    res = apply_config_session(
        "missing", "purpose", registry=ActiveSessionRegistry(), directory=d,
        socket_factory=_boom_socket_factory, subprocess_runner=_boom_runner,
        lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["reason"] == "authorization_absent"
    assert res["status"] == "UNKNOWN"


def test_apply_config_session_grant_does_not_authorise_apply_config(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(Grant(
        grant_id="K9-SESSION", purpose="session", target_endpoint=ENDPOINT,
        allowed_commands=frozenset({250, 100, 101, 104, 106, 105}),
        allowed_modes=frozenset({MODE_SESSION}),
        issued_by="x", issued_at="2026-10-27T00:00:00Z",
        expires_at="2999-01-01T00:00:00Z",
    ))
    res = apply_config_session(
        "K9-SESSION", "purpose", registry=ActiveSessionRegistry(), directory=d,
        socket_factory=_boom_socket_factory, subprocess_runner=_boom_runner,
        lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["reason"] == "authorization_absent"
    assert d.exists("K9-SESSION")  # not consumed (scope mismatch before consume)


def test_apply_config_host_exe_missing(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_apply_config_grant())
    res = apply_config_session(
        "K9-AC-2026-10-27", "purpose", registry=ActiveSessionRegistry(), directory=d,
        socket_factory=lambda: FakeSocket(), subprocess_runner=_boom_runner,
        repo_root=tmp_path, lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["reason"] == "controller_unavailable"


def test_apply_config_bind_conflict(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_apply_config_grant())
    _touch_host_exe(tmp_path)
    res = apply_config_session(
        "K9-AC-2026-10-27", "purpose", registry=ActiveSessionRegistry(), directory=d,
        socket_factory=lambda: FakeSocketInUse(), subprocess_runner=_boom_runner,
        repo_root=tmp_path, lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["reason"] == "session_conflict"
    assert d.exists("K9-AC-2026-10-27")  # grant not consumed


# --------------------------------------------------------------------------- #
# active_session.apply_config_session — successful parse + mapping
# --------------------------------------------------------------------------- #

def test_apply_config_parses_amplifier_and_summary(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_apply_config_grant())
    _touch_host_exe(tmp_path)
    reg = ActiveSessionRegistry()
    res = apply_config_session(
        "K9-AC-2026-10-27", "purpose", registry=reg, directory=d,
        socket_factory=lambda: FakeSocket(),
        subprocess_runner=lambda c, w: {"returncode": 0, "stdout": _clean_apply_config_stdout(), "timed_out": False},
        repo_root=tmp_path, lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["status"] == "PASS"
    assert res["shutdown"]["classification"] == "clean_shutdown"
    assert res["authorization"]["consumed"] is True
    assert res["init"]["readiness"] == "READY"

    assert res["heartbeat"]["supervised"] is True
    assert res["heartbeat"]["heartbeat_sent"] == 3
    assert res["heartbeat"]["latched"] is False

    amp = res["amplifiers"][0]
    assert amp["slave_number"] == 1
    assert amp["compatible"] is True
    assert amp["disable_verified"] is True
    assert amp["enable_active_after_disable"] is False
    assert amp["inhibited_after_disable"] is True
    assert amp["config_verified"] is True
    assert amp["init_done_config_ok"] is True
    assert amp["pwm3_enabled"] is False
    assert amp["applied_pwm"] == 399

    assert d.is_consumed("K9-AC-2026-10-27")
    assert reg.current() is None


def test_apply_config_failed_config_reports_failed(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_apply_config_grant())
    _touch_host_exe(tmp_path)
    failed = json.dumps({**_SUMMARY_LINE, "status": "FAILED", "failure": "no_compatible_amplifiers"}) + "\n"
    res = apply_config_session(
        "K9-AC-2026-10-27", "purpose", registry=ActiveSessionRegistry(), directory=d,
        socket_factory=lambda: FakeSocket(),
        subprocess_runner=lambda c, w: {"returncode": 1, "stdout": failed, "timed_out": False},
        repo_root=tmp_path, lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["status"] == "FAILED"
    assert res["reason"] == "no_compatible_amplifiers"
    assert res["shutdown"]["classification"] == "clean_shutdown"


def test_apply_config_init_failure_classifies_ack_timeout(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_apply_config_grant())
    _touch_host_exe(tmp_path)
    stdout = json.dumps({"type": "initFailure", "command": 104,
                         "commandName": "EXEC_MBUS_STATE_SLAVE_INIT", "reason": "ack_timeout"}) + "\n"
    res = apply_config_session(
        "K9-AC-2026-10-27", "purpose", registry=ActiveSessionRegistry(), directory=d,
        socket_factory=lambda: FakeSocket(),
        subprocess_runner=lambda c, w: {"returncode": 1, "stdout": stdout, "timed_out": False},
        repo_root=tmp_path, lock_dir=tmp_path, audit_dir=tmp_path,
    )
    assert res["status"] == "FAILED"
    assert res["shutdown"]["classification"] == "ack_timeout"


# --------------------------------------------------------------------------- #
# MCP tool surface
# --------------------------------------------------------------------------- #

def test_apply_config_tool_refuses_without_grant():
    res = active_tools.diagnostics_apply_config("no-such-grant", "purpose")
    assert res["reason"] == "authorization_absent"


def test_apply_config_tool_guards_inputs():
    assert active_tools.diagnostics_apply_config("g", "")["ok"] is False
    assert active_tools.diagnostics_apply_config("", "p")["reason"] == "authorization_absent"
    assert active_tools.diagnostics_apply_config(None, "p")["reason"] == "authorization_absent"
    assert active_tools.diagnostics_apply_config("g", 123)["ok"] is False


def test_capabilities_reports_apply_config():
    caps = server.diagnostics_capabilities()
    assert "diagnostics_apply_config" in caps["active_tools"]
    assert caps["active_effect_classes"]["diagnostics_apply_config"] == "STATE_CHANGING"
    assert caps["apply_config_commands"] == [100, 101, 104, 105, 106, 108, 110, 250]
    # The read-only/session allowlist is unchanged.
    assert caps["active_allowlist"] == [100, 101, 104, 105, 106, 113, 250]
