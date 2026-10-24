"""Deterministic offline tests for the K6-A OBSERVE guard.

Covers the executable enforcement contract of ``tools.diagnostics.observe_guard``:
the ``{113}`` command allowlist, the explicit-literal endpoint requirement (no
fallback, no DNS), single-use / exactly-once authorization, bounded timeout, and
that the guard never reaches the flash/reset state machine. No sockets, serial
ports, or hardware are ever opened.

Run with:
    tools/knowledge/.venv/Scripts/python.exe -m pytest tools/diagnostics/tests -q
"""

from __future__ import annotations

import ast
import sys
from pathlib import Path

import pytest

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics import observe_guard as guard  # noqa: E402
from tools.diagnostics.observe_guard import (  # noqa: E402
    ALLOWED,
    BLOCKED,
    ALLOWED_COMMANDS,
    DEFAULT_TIMEOUT_SECONDS,
    MAX_TIMEOUT_SECONDS,
    REQUIRED_ENDPOINT,
    AuthorizationBlocked,
    CommandBlocked,
    EndpointBlocked,
    ObserveSession,
    bound_timeout,
    validate_command,
    validate_endpoint,
)


# --------------------------------------------------------------------------- #
# Command allowlist
# --------------------------------------------------------------------------- #

def test_allowlist_contains_only_113():
    assert ALLOWED_COMMANDS == {113}


def test_validate_command_allows_113():
    assert validate_command(113) == ALLOWED


def test_validate_command_rejects_flash_reset_family():
    # 122 (FLASH_ALL_SLAVES) and 123..130 (SELECT_SLAVE..EXIT_SLAVExFWxHANDLER)
    # are the destructive/state-changing flash/reset commands — all BLOCKED.
    for cmd in (122, 123, 124, 125, 126, 127, 128, 129, 130):
        assert validate_command(cmd) == BLOCKED, cmd


def test_validate_command_rejects_any_other_command():
    for cmd in (0, 1, 10, 100, 109, 110, 111, 112, 114, 250, 255, 1000, -1):
        assert validate_command(cmd) == BLOCKED, cmd


def test_validate_command_rejects_non_integer_and_bool():
    for cmd in (None, "113", "113.0", 113.0, True, False, [113], {"113": 1}):
        assert validate_command(cmd) == BLOCKED, repr(cmd)


# --------------------------------------------------------------------------- #
# Endpoint validation (explicit literal, no fallback, no DNS)
# --------------------------------------------------------------------------- #

def test_validate_endpoint_allows_explicit_literal():
    assert validate_endpoint(REQUIRED_ENDPOINT) == ALLOWED
    assert validate_endpoint(" 192.168.1.193 ") == ALLOWED  # whitespace stripped


def test_validate_endpoint_rejects_missing_or_invalid():
    for ep in ("", "   ", None, 123, ["192.168.1.193"], object()):
        assert validate_endpoint(ep) == BLOCKED, repr(ep)


def test_validate_endpoint_rejects_any_other_address():
    for ep in (
        "192.168.1.100",      # the historical DNS fallback
        "192.168.1.12",       # a different host on the subnet
        "192.168.1.194",
        "0.0.0.0",
        "TRACKCONTROL",       # a hostname — never resolved
        "not-an-ip",
        "192.168.1.193:10000",
    ):
        assert validate_endpoint(ep) == BLOCKED, ep


def test_guard_has_no_dns_or_fallback_imports():
    """The guard must not import socket/DNS resolution, so no fallback can exist."""
    src = Path(guard.__file__).read_text(encoding="utf-8")
    tree = ast.parse(src)
    forbidden = {"socket", "Comm", "DataAquisition", "flash_one", "bootload"}
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                assert alias.name.split(".")[0] not in forbidden, alias.name
        elif isinstance(node, ast.ImportFrom) and node.level == 0:
            root = (node.module or "").split(".")[0]
            assert root not in forbidden, node.module


# --------------------------------------------------------------------------- #
# Single-use authorization session
# --------------------------------------------------------------------------- #

def test_consume_flips_count_1_to_0():
    session = ObserveSession(113, REQUIRED_ENDPOINT)
    assert session.AUTHORIZED_COMMAND_COUNT_REMAINING == 1
    result = session.consume()
    assert result["status"] == ALLOWED
    assert result["command_id"] == 113
    assert result["endpoint"] == REQUIRED_ENDPOINT
    assert session.AUTHORIZED_COMMAND_COUNT_REMAINING == 0
    assert result["authorized_command_count_remaining"] == 0


def test_consume_twice_is_blocked():
    session = ObserveSession(113, REQUIRED_ENDPOINT)
    session.consume()
    with pytest.raises(AuthorizationBlocked):
        session.consume()


def test_consume_missing_ownership_is_blocked():
    # No command and no endpoint -> no ownership -> BLOCKED.
    with pytest.raises((CommandBlocked, EndpointBlocked)):
        ObserveSession(None, None).consume()
    # Missing endpoint only -> BLOCKED.
    with pytest.raises(EndpointBlocked):
        ObserveSession(113, None).consume()
    # Missing command only -> BLOCKED.
    with pytest.raises(CommandBlocked):
        ObserveSession(None, REQUIRED_ENDPOINT).consume()


def test_consume_invalid_command_is_blocked():
    with pytest.raises(CommandBlocked):
        ObserveSession(125, REQUIRED_ENDPOINT).consume()


def test_consume_invalid_endpoint_is_blocked():
    with pytest.raises(EndpointBlocked):
        ObserveSession(113, "192.168.1.100").consume()
    with pytest.raises(EndpointBlocked):
        ObserveSession(113, "").consume()


# --------------------------------------------------------------------------- #
# Bounded timeout / no retry / no recovery
# --------------------------------------------------------------------------- #

def test_bound_timeout_clamps_and_rejects():
    assert bound_timeout(5.0) == 5.0
    assert bound_timeout(DEFAULT_TIMEOUT_SECONDS) == 5.0
    assert bound_timeout(1.5) == 1.5
    assert bound_timeout(999.0) == MAX_TIMEOUT_SECONDS  # clamped, not unbounded
    assert MAX_TIMEOUT_SECONDS == 5.0
    for bad in (0, -1, -0.001, None, "x", []):
        with pytest.raises(ValueError):
            bound_timeout(bad)


def test_guard_exposes_no_retry_or_recovery_api():
    """The guard is enforcement-only: no retry/recovery/flash/reset entry point."""
    public = {n for n in dir(guard) if not n.startswith("_")}
    assert "retry" not in public
    assert "recover" not in public
    assert "flash" not in public
    assert "reset" not in public
    # The only session transition is consume(); there is no re-arm method.
    assert not hasattr(ObserveSession, "rearm")
    assert not hasattr(ObserveSession, "retry")
    assert not hasattr(ObserveSession, "recover")
