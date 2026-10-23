"""Boundary tests for the Diagnostics MCP server adapter (offline, read-only).

Verifies the four registered tools, the offline/hardware flags, and the input
bounds (empty / oversized / malformed -> clear error dict). The server is
incapable of opening a hardware connection: the wrapped core never touches a
socket/serial port and the adapter exposes no filesystem/raw/network tools.
"""

from __future__ import annotations

import asyncio
import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics.mcp_server import server  # noqa: E402


def test_four_readonly_tools_registered():
    names = {t.name for t in asyncio.run(server.mcp.list_tools())}
    assert names == {
        "diagnostics_capabilities",
        "diagnostics_analyze_udp_trace",
        "diagnostics_analyze_firmware_session",
        "diagnostics_compare_protocol_traces",
    }


def test_capabilities_offline_flags_via_mcp():
    caps = server.diagnostics_capabilities()
    assert caps["offline_only"] is True
    assert caps["hardware_access"] is False


def test_rejects_empty_text():
    assert server.diagnostics_analyze_udp_trace("")["ok"] is False
    assert server.diagnostics_analyze_firmware_session("   ")["ok"] is False
    assert server.diagnostics_analyze_udp_trace(None)["ok"] is False


def test_rejects_oversized_text():
    big = "x" * (1024 * 1024 + 1)
    res = server.diagnostics_analyze_firmware_session(big)
    assert res["ok"] is False
    assert "exceeds" in res["error"]


def test_rejects_malformed_int_list():
    res = server.diagnostics_compare_protocol_traces(["not-an-int"], [1])
    assert res["ok"] is False
    res = server.diagnostics_compare_protocol_traces(None, [1])
    assert res["ok"] is False
    res = server.diagnostics_compare_protocol_traces([125], {"a": 1})
    assert res["ok"] is False


def test_compare_through_mcp():
    res = server.diagnostics_compare_protocol_traces([125, 126], [125, 122])
    assert res["match"] is False
    assert {"index": 1, "expected": 126, "actual": 122} in res["differences"]


def test_analyze_through_mcp():
    res = server.diagnostics_analyze_udp_trace("(200, 201, 9, 100)")
    assert res["counts"]["timeout"] == 1
    assert res["frames"][0]["classification"] == "timeout"
