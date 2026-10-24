"""Deterministic offline tests for the Siebwalde OBSERVE adapter (K5-A).

Covers typed snapshot parsing (controller + amplifier), the semantic-field /
register-map model, evidence-level fail-closed behaviour, inventory comparison,
init-discrepancy reporting, capability classification, and the stdlib-only /
no-transport enforcement of ``tools.diagnostics.observe``. No sockets, serial
ports, or hardware are ever opened.

Run with:
    tools/knowledge/.venv/Scripts/python.exe -m pytest tools/diagnostics/tests -q
"""

from __future__ import annotations

import ast
import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics import observe  # noqa: E402
from tools.diagnostics.observe import (  # noqa: E402
    DESTRUCTIVE,
    OFFLINE_ONLY,
    UNKNOWN_EFFECT,
    HR_CHECKSUM,
    REGISTER_ADDRESS_TABLES,
    REGISTER_MAP_LEGACY_12,
    REGISTER_MAP_PHASE1_23,
    REGISTER_MAP_UNKNOWN,
    AmplifierInventory,
    AmplifierSnapshot,
    ControllerSnapshot,
    capabilities,
    classify_operation,
    compare_amplifier_inventory,
    parse_amplifier_snapshot,
    parse_controller_version_response,
    report_init_discrepancies,
)
from tools.diagnostics import core  # noqa: E402


# --------------------------------------------------------------------------- #
# Controller version parsing
# --------------------------------------------------------------------------- #

def test_parse_controller_version_text_valid():
    snap = parse_controller_version_response(
        "version 1.0.0 protocol 0x01 build 2026-10-17"
    )
    assert isinstance(snap, ControllerSnapshot)
    assert snap.version.major == 1
    assert snap.version.minor == 0
    assert snap.version.patch == 0
    assert snap.version.protocol == 1
    assert snap.build_timestamp == "2026-10-17"
    assert snap.source_revision is None
    assert snap.evidence_level == core.ACKNOWLEDGED


def test_parse_controller_version_text_missing_protocol_is_unknown():
    snap = parse_controller_version_response("version 1.0.0 build 2026-10-17")
    assert snap.version.major == 1
    assert snap.version.protocol is None
    assert snap.evidence_level == core.UNKNOWN


def test_parse_controller_version_bytes_valid():
    raw = bytes([0xAA, 0x0A, 0x71, 0x01, 0x00, 0x00, 0x01, 0x00]) + b"2026-10-17"
    snap = parse_controller_version_response(raw)
    assert snap.version.major == 1
    assert snap.version.minor == 0
    assert snap.version.patch == 0
    assert snap.version.protocol == 1
    assert snap.build_timestamp == "2026-10-17"
    assert snap.evidence_level == core.ACKNOWLEDGED


def test_parse_controller_version_hex_string():
    snap = parse_controller_version_response(
        "aa 0a 71 01 00 00 01 00 32 30 32 36 2d 31 30 2d 31 37"
    )
    assert snap.version.major == 1
    assert snap.version.protocol == 1
    assert snap.build_timestamp == "2026-10-17"


def test_parse_controller_version_rejects_malformed():
    for bad in ("", "   ", "not a version", "version x.y.z", 123, None, ["x"]):
        try:
            parse_controller_version_response(bad)
            raise AssertionError("expected ValueError for %r" % (bad,))
        except ValueError:
            pass


def test_parse_controller_version_rejects_bad_frame():
    # wrong header (0x55) / wrong command / wrong echo
    for bad in (
        bytes([0x55, 0x0A, 0x71, 0x01, 0x00, 0x00, 0x01, 0x00]),
        bytes([0xAA, 0x0A, 0x70, 0x01, 0x00, 0x00, 0x01, 0x00]),  # echo 112 != 113
        b"\xaa\x0a",  # too short
    ):
        try:
            parse_controller_version_response(bad)
            raise AssertionError("expected ValueError for %r" % (bad,))
        except ValueError:
            pass


# --------------------------------------------------------------------------- #
# Amplifier snapshot parsing — semantic fields + register map
# --------------------------------------------------------------------------- #

def test_parse_amplifier_snapshot_semantic_fields():
    # Phase-1-style semantic names, no map hint -> register_map UNKNOWN.
    snap = parse_amplifier_snapshot(
        "slave 3 detected=1 identity=0xF101 version=0x0100 patch=0x0001 checksum=0x251F"
    )
    assert isinstance(snap, AmplifierSnapshot)
    assert snap.slave_number == 3
    assert snap.detected is True
    assert snap.identity == 0xF101
    assert snap.fw_version == 0x0100
    assert snap.patch_proto == 0x0001
    assert snap.checksum == 0x251F
    assert snap.register_map == REGISTER_MAP_UNKNOWN
    # No HR numbers are invented for an UNKNOWN map.
    assert snap.holding_registers() == {}
    assert snap.evidence_level == core.ACKNOWLEDGED


def test_parse_amplifier_snapshot_missing_fields_unknown_not_fabricated():
    snap = parse_amplifier_snapshot("slave 3 detected=1 identity=0xF101")
    assert snap.slave_number == 3
    assert snap.detected is True
    assert snap.identity == 0xF101
    # Missing fw_version/patch_proto/checksum are NOT fabricated: they stay None
    # and the evidence level degrades to UNKNOWN (fail-closed).
    assert snap.fw_version is None
    assert snap.patch_proto is None
    assert snap.checksum is None
    assert snap.evidence_level == core.UNKNOWN


def test_parse_amplifier_snapshot_detected_false():
    snap = parse_amplifier_snapshot(
        "slave 4 detected=0 identity=0xF101 version=0x0100 patch=0x0001 checksum=0x251F"
    )
    assert snap.slave_number == 4
    assert snap.detected is False


def test_parse_amplifier_snapshot_rejects_malformed():
    for bad in ("", "   ", "hello world", 123, None, ["slave 3"]):
        try:
            parse_amplifier_snapshot(bad)
            raise AssertionError("expected ValueError for %r" % (bad,))
        except ValueError:
            pass


def test_parse_amplifier_snapshot_phase1_map_hint():
    snap = parse_amplifier_snapshot(
        "slave 3 detected=1 identity=0xF101 version=0x0100 patch=0x0001 "
        "checksum=0x251F map=PHASE1_23"
    )
    assert snap.register_map == REGISTER_MAP_PHASE1_23
    assert snap.holding_registers() == {
        "HR7": 0xF101,
        "HR21": 0x0100,
        "HR22": 0x0001,
        "HR11": 0x251F,
    }
    assert snap.evidence_level == core.ACKNOWLEDGED


def test_parse_amplifier_snapshot_legacy_map_hint():
    snap = parse_amplifier_snapshot(
        "slave 3 detected=1 checksum=0x251F map=LEGACY_12"
    )
    assert snap.register_map == REGISTER_MAP_LEGACY_12
    # Legacy has no identity/version/patch registers.
    assert snap.identity is None
    assert snap.fw_version is None
    assert snap.patch_proto is None
    assert snap.checksum == 0x251F
    assert snap.holding_registers() == {"HR11": 0x251F}
    assert snap.evidence_level == core.ACKNOWLEDGED


def test_register_address_tables_checksum_is_hr11_in_both_maps():
    # HR11 (checksum) is the single stable anchor in BOTH maps.
    assert REGISTER_ADDRESS_TABLES[REGISTER_MAP_LEGACY_12]["checksum"] == 11
    assert REGISTER_ADDRESS_TABLES[REGISTER_MAP_PHASE1_23]["checksum"] == 11
    assert HR_CHECKSUM == 11


def test_register_address_tables_map_layouts():
    legacy = REGISTER_ADDRESS_TABLES[REGISTER_MAP_LEGACY_12]
    phase1 = REGISTER_ADDRESS_TABLES[REGISTER_MAP_PHASE1_23]

    # LEGACY_12 has NO identity/version/patch addresses.
    for name in ("identity", "version", "patch_proto"):
        assert name not in legacy
    assert legacy["status"] == 3
    assert legacy["config_id_pwm"] == 9
    assert legacy["accel_params"] == 10
    assert legacy["checksum"] == 11

    # PHASE1_23 maps identity->HR7, version->HR21, patch_proto->HR22, checksum->HR11.
    assert phase1["identity"] == 7
    assert phase1["version"] == 21
    assert phase1["patch_proto"] == 22
    assert phase1["checksum"] == 11


def test_amplifier_inventory_detected_set():
    a = parse_amplifier_snapshot(
        "slave 3 detected=1 identity=0xF101 version=0x0100 patch=0x0001 checksum=0x251F"
    )
    b = parse_amplifier_snapshot(
        "slave 4 detected=0 identity=0xF101 version=0x0100 patch=0x0001 checksum=0x251F"
    )
    inv = AmplifierInventory.from_snapshots([a, b])
    assert inv.detected == {3}  # slave 4 is present but not detected
    d = inv.to_dict()
    assert d["detected"] == [3]
    assert len(d["amplifiers"]) == 2


# --------------------------------------------------------------------------- #
# Inventory comparison
# --------------------------------------------------------------------------- #

_SLAVE = "identity=0xF101 version=0x0100 patch=0x0001 checksum=0x251F"


def _line(slave: int, detected: int = 1) -> str:
    return "slave %d detected=%d %s" % (slave, detected, _SLAVE)


def test_compare_amplifier_inventory_match():
    expected = [_line(1), _line(2)]
    actual = [_line(1), _line(2)]
    res = compare_amplifier_inventory(expected, actual)
    assert res["match"] is True
    assert res["missing"] == []
    assert res["extra"] == []
    assert res["unexpected"] == []


def test_compare_amplifier_inventory_missing_extra():
    expected = [_line(1), _line(2)]
    actual = [_line(1), _line(3)]
    res = compare_amplifier_inventory(expected, actual)
    assert res["match"] is False
    assert res["missing"] == [2]
    assert res["extra"] == [3]
    assert res["unexpected"] == []


def test_compare_amplifier_inventory_unexpected_malformed_line():
    expected = [_line(1)]
    actual = [_line(1), "garbage line"]
    res = compare_amplifier_inventory(expected, actual)
    assert res["match"] is False
    assert res["missing"] == []
    assert res["extra"] == []
    assert len(res["unexpected"]) == 1
    assert res["unexpected"][0]["index"] == 1


def test_compare_amplifier_inventory_detected_zero_not_in_set():
    # detected=0 slave is present but not part of the detected set.
    expected = [_line(1), _line(2, detected=0)]
    actual = [_line(1)]
    res = compare_amplifier_inventory(expected, actual)
    assert res["match"] is True
    assert res["expected_detected"] == [1]
    assert res["actual_detected"] == [1]


# --------------------------------------------------------------------------- #
# Init-discrepancy reporting
# --------------------------------------------------------------------------- #

def test_report_init_discrepancies_match():
    res = report_init_discrepancies(["A", "B", "C"], ["A", "B", "C"])
    assert res["match"] is True
    assert res["missing"] == []
    assert res["unexpected"] == []
    assert res["wrong_order"] == []


def test_report_init_discrepancies_missing():
    res = report_init_discrepancies(["A", "B", "C", "D"], ["A", "B", "C"])
    assert res["match"] is False
    assert res["missing"] == ["D"]
    assert res["unexpected"] == []
    assert res["wrong_order"] == []


def test_report_init_discrepancies_unexpected():
    res = report_init_discrepancies(["A", "B"], ["A", "B", "X"])
    assert res["match"] is False
    assert res["missing"] == []
    assert res["unexpected"] == ["X"]
    assert res["wrong_order"] == []


def test_report_init_discrepancies_wrong_order():
    res = report_init_discrepancies(["A", "B", "C"], ["A", "C", "B"])
    assert res["match"] is False
    assert res["missing"] == []
    assert res["unexpected"] == []
    assert res["wrong_order"] == ["B"]


def test_report_init_discrepancies_combined():
    res = report_init_discrepancies(
        ["A", "B", "C", "D"], ["A", "C", "B", "E"]
    )
    assert res["match"] is False
    assert res["missing"] == ["D"]
    assert res["unexpected"] == ["E"]
    assert res["wrong_order"] == ["B"]


# --------------------------------------------------------------------------- #
# Capability classification
# --------------------------------------------------------------------------- #

def test_classify_offline_operations_offline_only_and_not_destructive():
    ops = capabilities()["operations"]
    assert ops
    for name in ops:
        assert classify_operation(name) == OFFLINE_ONLY, name
        assert classify_operation(name) != DESTRUCTIVE, name


def test_classify_unknown_and_non_string_is_unknown_effect():
    assert classify_operation("does_not_exist") == UNKNOWN_EFFECT
    assert classify_operation(None) == UNKNOWN_EFFECT
    assert classify_operation(123) == UNKNOWN_EFFECT
    assert classify_operation("") == UNKNOWN_EFFECT


def test_classify_illustrative_destructive():
    # Advisory vocabulary is exercised; classification does NOT grant authority.
    assert classify_operation("ERASE_FLASH") == DESTRUCTIVE
    assert classify_operation("WRITE_FLASH") == DESTRUCTIVE


def test_capabilities_offline_flags_and_classification_set():
    caps = capabilities()
    assert caps["offline_only"] is True
    assert caps["hardware_access"] is False
    assert caps["socket_access"] is False
    assert caps["serial_access"] is False
    assert OFFLINE_ONLY in caps["capability_classifications"]
    assert DESTRUCTIVE in caps["capability_classifications"]
    assert UNKNOWN_EFFECT in caps["capability_classifications"]
    # Register-map view: no mislabelled HR3/HR9/HR10 identity/version/patch keys.
    assert set(caps["register_maps"]) == {
        REGISTER_MAP_LEGACY_12,
        REGISTER_MAP_PHASE1_23,
        REGISTER_MAP_UNKNOWN,
    }
    assert caps["checksum_register"] == "HR11"
    tables = caps["register_address_tables"]
    assert tables[REGISTER_MAP_PHASE1_23]["identity"] == "HR7"
    assert tables[REGISTER_MAP_PHASE1_23]["version"] == "HR21"
    assert tables[REGISTER_MAP_PHASE1_23]["patch_proto"] == "HR22"
    assert tables[REGISTER_MAP_LEGACY_12]["checksum"] == "HR11"
    assert "identity" not in tables[REGISTER_MAP_LEGACY_12]


# --------------------------------------------------------------------------- #
# Offline enforcement (no socket / serial / Comm imports)
# --------------------------------------------------------------------------- #

def test_observe_stdlib_only_no_transport_imports():
    """observe.py must not import socket/serial/Comm/DataAquisition/flash_one."""
    src = Path(observe.__file__).read_text(encoding="utf-8")
    tree = ast.parse(src)

    forbidden = {
        "socket",
        "serial",
        "pyserial",
        "usb",
        "Comm",
        "DataAquisition",
        "flash_one",
        "bootload",
    }
    # Absolute imports must be stdlib only (relative imports are our own package).
    stdlib = {
        "re",
        "typing",
        "dataclasses",
        "__future__",
        "collections",
        "struct",
        "sys",
        "os",
        "json",
        "datetime",
        "enum",
        "abc",
        "textwrap",
    }

    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                root = alias.name.split(".")[0]
                assert root not in forbidden, "forbidden import: %s" % alias.name
                assert root in stdlib, "non-stdlib import: %s" % alias.name
        elif isinstance(node, ast.ImportFrom):
            if node.level == 0:
                root = (node.module or "").split(".")[0]
                assert root not in forbidden, "forbidden import: %s" % node.module
                assert root in stdlib, "non-stdlib import: %s" % node.module
            # relative imports (level >= 1) resolve inside tools.diagnostics
            else:
                assert node.level >= 1
