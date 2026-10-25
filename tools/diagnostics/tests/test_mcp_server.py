"""Boundary tests for the Diagnostics MCP server adapter (offline, read-only).

Verifies the registered tools (K4 core + K5-A OBSERVE), the offline/hardware
flags, and the input bounds (empty / oversized / malformed -> clear error dict).
The server is incapable of opening a hardware connection: the wrapped core and
OBSERVE adapter never touch a socket/serial port and the adapter exposes no
filesystem/raw/network tools.
"""

from __future__ import annotations

import asyncio
import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics.mcp_server import server  # noqa: E402

# The four K4 tools must remain present and unchanged.
K4_TOOLS = {
    "diagnostics_capabilities",
    "diagnostics_analyze_udp_trace",
    "diagnostics_analyze_firmware_session",
    "diagnostics_compare_protocol_traces",
}

# The five new K5-A OBSERVE tools.
OBSERVE_TOOLS = {
    "diagnostics_parse_controller_snapshot",
    "diagnostics_parse_amplifier_snapshot",
    "diagnostics_compare_amplifier_inventory",
    "diagnostics_classify_operation",
    "diagnostics_report_init_discrepancies",
}

ALL_TOOLS = K4_TOOLS | OBSERVE_TOOLS


def test_all_readonly_tools_registered():
    names = {t.name for t in asyncio.run(server.mcp.list_tools())}
    # The 9 offline tools must remain registered; the 4 grant-gated active tools
    # are additive (see test_mcp_active_tools.py for their registration).
    assert ALL_TOOLS <= names


def test_k4_tools_preserved():
    names = {t.name for t in asyncio.run(server.mcp.list_tools())}
    assert K4_TOOLS <= names


def test_capabilities_offline_flags_via_mcp():
    caps = server.diagnostics_capabilities()
    assert caps["offline_only"] is True
    assert caps["hardware_access"] is False


def test_capabilities_includes_new_operations_and_classification_set():
    caps = server.diagnostics_capabilities()
    assert "OFFLINE_ONLY" in caps["capability_classifications"]
    assert "DESTRUCTIVE" in caps["capability_classifications"]
    assert "UNKNOWN_EFFECT" in caps["capability_classifications"]
    assert "diagnostics_parse_controller_snapshot" in caps["operations"]
    assert "diagnostics_parse_amplifier_snapshot" in caps["operations"]
    assert "diagnostics_compare_amplifier_inventory" in caps["operations"]
    assert "diagnostics_report_init_discrepancies" in caps["operations"]


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


# --------------------------------------------------------------------------- #
# K5-A OBSERVE tools
# --------------------------------------------------------------------------- #

def test_parse_controller_snapshot_through_mcp():
    res = server.diagnostics_parse_controller_snapshot(
        "version 1.0.0 protocol 0x01 build 2026-10-17"
    )
    assert res["version"]["major"] == 1
    assert res["version"]["protocol"] == 1
    assert res["build_timestamp"] == "2026-10-17"


def test_parse_controller_snapshot_malformed_is_error_dict():
    for bad in ("", "garbage", None):
        res = server.diagnostics_parse_controller_snapshot(bad)
        assert res["ok"] is False
        assert "error" in res


def test_parse_amplifier_snapshot_through_mcp():
    res = server.diagnostics_parse_amplifier_snapshot(
        "slave 3 detected=1 identity=0xF101 version=0x0100 patch=0x0001 checksum=0x251F"
    )
    assert res["slave_number"] == 3
    assert res["detected"] is True
    assert res["identity"] == 0xF101
    assert res["fw_version"] == 0x0100
    assert res["patch_proto"] == 0x0001
    assert res["checksum"] == 0x251F
    assert res["register_map"] == "UNKNOWN"


def test_parse_amplifier_snapshot_missing_field_unknown_through_mcp():
    res = server.diagnostics_parse_amplifier_snapshot("slave 3 detected=1 identity=0xF101")
    assert res["identity"] == 0xF101
    assert res["fw_version"] is None
    assert res["checksum"] is None
    assert res["evidence_level"] == "UNKNOWN"


def test_compare_amplifier_inventory_through_mcp():
    line = "identity=0xF101 version=0x0100 patch=0x0001 checksum=0x251F"
    res = server.diagnostics_compare_amplifier_inventory(
        ["slave 1 detected=1 " + line, "slave 2 detected=1 " + line],
        ["slave 1 detected=1 " + line, "slave 3 detected=1 " + line],
    )
    assert res["match"] is False
    assert res["missing"] == [2]
    assert res["extra"] == [3]


def test_compare_amplifier_inventory_bad_list_is_error_dict():
    res = server.diagnostics_compare_amplifier_inventory(None, ["slave 1"])
    assert res["ok"] is False
    res = server.diagnostics_compare_amplifier_inventory([1, 2], ["slave 1"])
    assert res["ok"] is False


def test_classify_operation_through_mcp():
    res = server.diagnostics_classify_operation("parse_amplifier_snapshot")
    assert res["name"] == "parse_amplifier_snapshot"
    assert res["classification"] == "OFFLINE_ONLY"
    res = server.diagnostics_classify_operation("nope")
    assert res["classification"] == "UNKNOWN_EFFECT"
    assert server.diagnostics_classify_operation("")["ok"] is False
    assert server.diagnostics_classify_operation(None)["ok"] is False


def test_report_init_discrepancies_through_mcp():
    res = server.diagnostics_report_init_discrepancies(
        ["A", "B", "C", "D"], ["A", "C", "B"]
    )
    assert res["match"] is False
    assert res["missing"] == ["D"]
    assert res["wrong_order"] == ["B"]
    assert res["unexpected"] == []


def test_report_init_discrepancies_bad_list_is_error_dict():
    res = server.diagnostics_report_init_discrepancies(None, ["A"])
    assert res["ok"] is False
    res = server.diagnostics_report_init_discrepancies(["A"], "not-a-list")
    assert res["ok"] is False
