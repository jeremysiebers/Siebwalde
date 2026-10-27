"""Diagnostics MCP server — offline trace analysis + OBSERVE parsing.

Run as a module (FastMCP, stdio transport)::

    python -m tools.diagnostics.mcp_server

The venv used for now is the shared project venv ``tools/knowledge/.venv``
(Python 3.13, fastmcp 4.1.3, pytest 9.1.1) — path:

    C:\\Localdata\\Siebwalde\\tools\\knowledge\\.venv

Read-only offline core + a grant-gated active/live surface. The offline tools
(fixed tools, no filesystem access, no raw command, no hardware-control tool, no
network) wrap ``tools.diagnostics.core`` and ``tools.diagnostics.observe``, which
never open a socket, a serial port, or a hardware connection, so the offline
surface is incapable of opening a PIC32 connection. The four active tools
(``active_tools.py``) are thin wrappers over ``tools.diagnostics.active_session``
and perform NO I/O without a valid, scoped, single-use grant.
"""

from __future__ import annotations

from fastmcp import FastMCP

from . import active_tools
from .. import active_guard, core, observe

mcp = FastMCP("diagnostics")

# 1 MiB input bound for textual trace tools.
MAX_INPUT_BYTES = 1 * 1024 * 1024
MAX_LIST_LEN = 100_000


def _error(message: str) -> dict:
    return {"error": message, "ok": False}


def _guard_text(value, name: str):
    """Return an error dict if `value` is empty/malformed/oversized, else None."""
    if value is None:
        return _error("%s: missing input" % name)
    if not isinstance(value, str):
        return _error("%s: expected a string, got %s" % (name, type(value).__name__))
    if not value.strip():
        return _error("%s: empty input" % name)
    try:
        size = len(value.encode("utf-8", "surrogatepass"))
    except UnicodeEncodeError:
        return _error("%s: malformed (non-encodable) input" % name)
    if size > MAX_INPUT_BYTES:
        return _error("%s: input exceeds %d bytes" % (name, MAX_INPUT_BYTES))
    return None


def _guard_int_list(value, name: str):
    """Validate a list-of-int argument; return an error dict or None."""
    if value is None:
        return _error("%s: missing input" % name)
    if not isinstance(value, list):
        return _error("%s: expected a list of ints, got %s" % (name, type(value).__name__))
    if len(value) > MAX_LIST_LEN:
        return _error("%s: too many elements (%d > %d)" % (name, len(value), MAX_LIST_LEN))
    for i, item in enumerate(value):
        if isinstance(item, bool) or not isinstance(item, int):
            return _error("%s[%d]: expected an int, got %s" % (name, i, type(item).__name__))
    return None


def _guard_str_list(value, name: str):
    """Validate a list-of-str argument; return an error dict or None."""
    if value is None:
        return _error("%s: missing input" % name)
    if not isinstance(value, list):
        return _error("%s: expected a list of strings, got %s" % (name, type(value).__name__))
    if len(value) > MAX_LIST_LEN:
        return _error("%s: too many elements (%d > %d)" % (name, len(value), MAX_LIST_LEN))
    for i, item in enumerate(value):
        if not isinstance(item, str):
            return _error("%s[%d]: expected a string, got %s" % (name, i, type(item).__name__))
    return None


@mcp.tool()
def diagnostics_capabilities() -> dict:
    """Describe the offline-only Diagnostics Core + OBSERVE operations.

    Read-only. Reports ``offline_only: true``, ``hardware_access: false``, the
    offline operations, and the capability-classification vocabulary.
    """
    caps = dict(core.capabilities())
    obs = observe.capabilities()
    caps["operations"] = sorted(set(caps.get("operations", [])) | set(obs["operations"]))
    caps["capability_classifications"] = obs["capability_classifications"]
    caps["operation_classifications"] = obs["operation_classifications"]
    caps["models"] = obs["models"]
    caps["register_maps"] = obs["register_maps"]
    caps["register_address_tables"] = obs["register_address_tables"]
    caps["checksum_register"] = obs["checksum_register"]
    # Active/live surface (additive; the offline tools above are unchanged). The
    # server is offline by construction: the active tools are grant-gated and
    # perform no I/O without a valid scoped grant.
    caps["mode"] = "offline"
    caps["active_surface_available"] = True
    caps["active_tools"] = list(active_tools.ACTIVE_TOOL_NAMES)
    caps["active_allowlist"] = sorted(active_guard.ACTIVE_ALLOWLIST)
    caps["active_effect_classes"] = dict(active_tools.ACTIVE_EFFECT_CLASSES)
    # The distinct state-changing CONFIG/APPLY command set (mode "apply_config").
    caps["apply_config_commands"] = sorted(active_guard.APPLY_CONFIG_COMMANDS)
    return caps


@mcp.tool()
def diagnostics_analyze_udp_trace(trace_text: str) -> dict:
    """Classify each frame of a textual UDP/feedback trace as ok/timeout/nok/error/unknown.

    Accepts one feedback tuple per line, decimal ``(200, 201, 9, 100)`` or hex
    ``(0xC8, 0xC9, 0x09, 0x64)`` (or a full ``fb=(...)`` line). Malformed lines
    are skipped and counted. Read-only, offline.
    """
    err = _guard_text(trace_text, "trace_text")
    if err is not None:
        return err
    return core.analyze_udp_trace(trace_text)


@mcp.tool()
def diagnostics_analyze_firmware_session(log_text: str) -> dict:
    """Analyse a saved flash_one.py textual log and classify the final FlashState.

    Returns run id, command sequence, acks, state progression, final state and
    evidence level. A FAILED_UNKNOWN_HARDWARE_STATE / ERROR / timeout session is
    classified UNKNOWN (never VERIFIED); only VERIFIED_COMPLETE is VERIFIED.
    Read-only, offline.
    """
    err = _guard_text(log_text, "log_text")
    if err is not None:
        return err
    return core.analyze_firmware_session(log_text)


@mcp.tool()
def diagnostics_compare_protocol_traces(expected: list, actual: list) -> dict:
    """Compare two command sequences (lists of command ints) and report differences.

    Returns ``{expected, actual, differences, match}``. Read-only, offline.
    """
    err = _guard_int_list(expected, "expected")
    if err is not None:
        return err
    err = _guard_int_list(actual, "actual")
    if err is not None:
        return err
    return core.compare_protocol_traces(expected, actual)


@mcp.tool()
def diagnostics_parse_controller_snapshot(text: str) -> dict:
    """Parse a recorded command-113 controller version response into a snapshot.

    Accepts the textual form (``version 1.0.0 protocol 0x01 build 2026-10-17``)
    or the raw bytes as a hex string. Malformed input returns an error dict.
    Read-only, offline.
    """
    err = _guard_text(text, "text")
    if err is not None:
        return err
    try:
        snapshot = observe.parse_controller_version_response(text)
    except ValueError as exc:
        return _error("parse_controller_snapshot: %s" % exc)
    return snapshot.to_dict()


@mcp.tool()
def diagnostics_parse_amplifier_snapshot(text: str) -> dict:
    """Parse a recorded amplifier snapshot line into a typed snapshot.

    Accepts e.g. ``slave 3 detected=1 identity=0xF101 version=0x0100
    patch=0x0001 checksum=0x251F``. Missing fields degrade evidence to UNKNOWN
    (never fabricated); unparseable input returns an error dict. Read-only,
    offline.
    """
    err = _guard_text(text, "text")
    if err is not None:
        return err
    try:
        snapshot = observe.parse_amplifier_snapshot(text)
    except ValueError as exc:
        return _error("parse_amplifier_snapshot: %s" % exc)
    return snapshot.to_dict()


@mcp.tool()
def diagnostics_compare_amplifier_inventory(expected: list, actual: list) -> dict:
    """Compare two amplifier inventories (lists of snapshot lines) as detected sets.

    Returns ``{expected, actual, expected_detected, actual_detected, missing,
    extra, unexpected, match}``. ``missing`` = detected in expected but not
    actual; ``extra`` = detected in actual but not expected; ``unexpected`` =
    unparseable actual lines. Read-only, offline.
    """
    err = _guard_str_list(expected, "expected")
    if err is not None:
        return err
    err = _guard_str_list(actual, "actual")
    if err is not None:
        return err
    try:
        return observe.compare_amplifier_inventory(expected, actual)
    except ValueError as exc:
        return _error("compare_amplifier_inventory: %s" % exc)


@mcp.tool()
def diagnostics_classify_operation(name: str) -> dict:
    """Classify an operation name by capability effect class.

    Returns ``{name, classification}``. Offline parse/compare/report operations
    classify ``OFFLINE_ONLY``; unknown names classify ``UNKNOWN_EFFECT``. The
    label is advisory and grants NO execution authority. Read-only, offline.
    """
    if name is None:
        return _error("name: missing input")
    if not isinstance(name, str):
        return _error("name: expected a string, got %s" % type(name).__name__)
    if not name.strip():
        return _error("name: empty input")
    classification = observe.classify_operation(name)
    return {"name": name, "classification": classification}


@mcp.tool()
def diagnostics_report_init_discrepancies(expected_sequence: list, actual_sequence: list) -> dict:
    """Report structured discrepancies between two ordered init sequences.

    Returns ``{expected, actual, missing, unexpected, wrong_order, match}``.
    ``missing``/``unexpected`` are multiset differences; ``wrong_order`` reports
    elements present in both but out of order. Read-only, offline.
    """
    err = _guard_str_list(expected_sequence, "expected_sequence")
    if err is not None:
        return err
    err = _guard_str_list(actual_sequence, "actual_sequence")
    if err is not None:
        return err
    return observe.report_init_discrepancies(expected_sequence, actual_sequence)


def main() -> None:
    mcp.run(transport="stdio")


active_tools.register(mcp)


if __name__ == "__main__":
    main()
