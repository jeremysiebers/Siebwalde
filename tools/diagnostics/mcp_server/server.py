"""Diagnostics MCP server — offline trace analysis over the Diagnostics Core.

Run as a module (FastMCP, stdio transport)::

    python -m tools.diagnostics.mcp_server

The venv used for now is the shared project venv ``tools/knowledge/.venv``
(Python 3.13, fastmcp 4.1.3, pytest 9.1.1) — path:

    C:\\Localdata\\Siebwalde\\tools\\knowledge\\.venv

Read-only and offline only: four fixed tools, no filesystem access, no raw
command, no hardware-control tool, no network. The wrapped core
(``tools.diagnostics.core``) never opens a socket, a serial port, or a hardware
connection, so this adapter is incapable of opening a PIC32 connection.
"""

from __future__ import annotations

from fastmcp import FastMCP

from .. import core

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


@mcp.tool()
def diagnostics_capabilities() -> dict:
    """Describe the offline-only Diagnostics Core operations and evidence levels.

    Read-only. Reports ``offline_only: true`` and ``hardware_access: false``.
    """
    return core.capabilities()


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


def main() -> None:
    mcp.run(transport="stdio")


if __name__ == "__main__":
    main()
