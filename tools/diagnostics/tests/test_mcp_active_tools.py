"""Offline tests for the grant-gated active MCP tool surface.

Verifies: each ACTIVE/LIVE tool refuses with ``authorization_absent`` when no
grant is present (and performs no I/O); ``diagnostics_capabilities`` reports the
new active-surface fields while keeping the offline flags; the offline tools are
unchanged; and the four active tools are registered on the FastMCP instance.
"""

from __future__ import annotations

import asyncio
import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics.mcp_server import active_tools, server  # noqa: E402

ACTIVE_TOOL_NAMES = set(active_tools.ACTIVE_TOOL_NAMES)


# --------------------------------------------------------------------------- #
# Capabilities extension (additive; offline flags preserved)
# --------------------------------------------------------------------------- #

def test_capabilities_keeps_offline_flags_and_adds_active_surface():
    caps = server.diagnostics_capabilities()
    assert caps["offline_only"] is True
    assert caps["hardware_access"] is False
    assert caps["socket_access"] is False
    # New additive fields.
    assert caps["mode"] == "offline"
    assert caps["active_surface_available"] is True
    assert caps["active_tools"] == list(active_tools.ACTIVE_TOOL_NAMES)
    assert caps["active_allowlist"] == [100, 101, 104, 105, 106, 113, 250]
    assert caps["active_effect_classes"]["diagnostics_probe_controller_version"] == "LIVE_READ_ONLY_HARDWARE_VALIDATED"
    assert caps["active_effect_classes"]["diagnostics_run_controller_session"] == "STATE_CHANGING"
    assert caps["active_effect_classes"]["diagnostics_abort_active_session"] == "STATE_CHANGING"


def test_capabilities_keeps_offline_operations():
    caps = server.diagnostics_capabilities()
    for op in ("analyze_firmware_session", "analyze_udp_trace", "compare_protocol_traces"):
        assert op in caps["operations"]


# --------------------------------------------------------------------------- #
# Registration
# --------------------------------------------------------------------------- #

def test_active_tools_registered():
    names = {t.name for t in asyncio.run(server.mcp.list_tools())}
    assert ACTIVE_TOOL_NAMES <= names


def test_offline_tools_still_registered():
    names = {t.name for t in asyncio.run(server.mcp.list_tools())}
    for tool in ("diagnostics_capabilities", "diagnostics_analyze_udp_trace",
                 "diagnostics_parse_amplifier_snapshot"):
        assert tool in names


# --------------------------------------------------------------------------- #
# Grant-gating: no grant -> authorization_absent, no I/O
# --------------------------------------------------------------------------- #

def test_run_session_tool_refuses_without_grant():
    res = active_tools.diagnostics_run_controller_session("no-such-grant-000", "purpose")
    assert res["reason"] == "authorization_absent"


def test_probe_tool_refuses_without_grant():
    res = active_tools.diagnostics_probe_controller_version("no-such-grant-000")
    assert res["reason"] == "authorization_absent"


def test_preflight_probe_refuses_without_grant():
    res = active_tools.diagnostics_controller_preflight(
        include_identity_probe=True, authorization=None)
    assert res["reason"] == "authorization_absent"


def test_abort_tool_with_no_active_session():
    res = active_tools.diagnostics_abort_active_session()
    assert res["reason"] == "no_active_session"


# --------------------------------------------------------------------------- #
# Input guards (mirror server.py)
# --------------------------------------------------------------------------- #

def test_run_session_tool_guards_inputs():
    # Empty purpose is a malformed input -> error dict.
    assert active_tools.diagnostics_run_controller_session("g", "")["ok"] is False
    # Missing grant id -> authorization_absent (no I/O), not a hard input error.
    assert active_tools.diagnostics_run_controller_session("", "p")["reason"] == "authorization_absent"
    assert active_tools.diagnostics_run_controller_session(None, "p")["reason"] == "authorization_absent"
    # Non-string purpose is a malformed input.
    assert active_tools.diagnostics_run_controller_session("g", 123)["ok"] is False


def test_probe_tool_guards_inputs():
    # Missing grant id -> authorization_absent (no I/O).
    assert active_tools.diagnostics_probe_controller_version("")["reason"] == "authorization_absent"
    assert active_tools.diagnostics_probe_controller_version(None)["reason"] == "authorization_absent"
    # Non-string grant id -> malformed input.
    assert active_tools.diagnostics_probe_controller_version(123)["ok"] is False


def test_preflight_tool_guards_inputs():
    # Missing grant id while probing -> authorization_absent (no I/O).
    res = active_tools.diagnostics_controller_preflight(include_identity_probe=True, authorization="")
    assert res["reason"] == "authorization_absent"
    # include_identity_probe must be a bool.
    res = active_tools.diagnostics_controller_preflight(include_identity_probe="yes")
    assert res["ok"] is False
