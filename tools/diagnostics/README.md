# Siebwalde Diagnostics (K4-B / K5) — offline core + OBSERVE adapter + MCP

Minimal **offline** Diagnostics Core + typed OBSERVE adapter plus a narrow
**read-only Diagnostics MCP**. The core analyses *saved* textual traces from the
K3 fail-closed single-target flash driver `TrackController5/Python/flash_one.py`
and recorded controller/amplifier snapshots; it never opens a socket, a serial
port, or a hardware connection, and it never imports `Comm`/`DataAquisition`
(or `flash_one.py` itself, whose import graph reaches the UDP transport).

## Layout

```
tools/diagnostics/
  core.py                 typed, MCP-independent, stdlib-only diagnostic core
  observe.py              typed OFFLINE OBSERVE adapter (snapshots + classification)
  mcp_server/server.py    FastMCP server ("diagnostics"); nine read-only tools
  mcp_server/__main__.py  `python -m tools.diagnostics.mcp_server`
  tests/                  deterministic offline pytest suite
```

## Core API (`tools.diagnostics.core`)

- `analyze_firmware_session(log_text: str) -> dict` — parse flash_one.py log lines
  (run id, TX command sequence, RX feedback acks, STATE transitions), classify
  the final `FlashState`, and return the evidence level.
- `analyze_udp_trace(trace_text: str) -> dict` — classify each feedback tuple
  (decimal or `0x..` hex) as `ok`/`timeout`/`nok`/`error`/`unknown`; skip and
  count malformed lines.
- `compare_protocol_traces(expected: list[int], actual: list[int]) -> dict` —
  compare two command sequences, return `{expected, actual, differences, match}`.
- `capabilities() -> dict` — offline-only operation description with
  `offline_only: true` and `hardware_access: false`.

Evidence levels (never auto-promoted): `REQUESTED`, `SENT`, `ACKNOWLEDGED`,
`PHYSICALLY_OBSERVED`, `VERIFIED`, `UNKNOWN`.

Fail-closed contract: a session ending at `FAILED_UNKNOWN_HARDWARE_STATE` (or
aborting on an ERROR/timeout) is classified `UNKNOWN` — never `VERIFIED`. Only a
session reaching `VERIFIED_COMPLETE` (128 checksum OK + 129 reset OK) is
`VERIFIED`. A sent command is never promoted to confirmed success without its
`*_RETURNED_OK`/DONE ack.

## MCP tools (read-only)

- `diagnostics_capabilities()`
- `diagnostics_analyze_udp_trace(trace_text: str)`
- `diagnostics_analyze_firmware_session(log_text: str)`
- `diagnostics_compare_protocol_traces(expected: list[int], actual: list[int])`

Input bounds: empty / oversized (> 1 MiB text) / malformed inputs are rejected
with a clear error dict. No filesystem access, no raw command, no
hardware-control tool, no network.

## Runtime

Reuses the existing project venv for now:

```
tools/knowledge/.venv/Scripts/python.exe
```

(Python 3.13.12, fastmcp 4.1.3, pytest 9.1.1.)

Run the server:

```powershell
tools/knowledge/.venv/Scripts/python.exe -m tools.diagnostics.mcp_server
```

Run the tests:

```powershell
tools/knowledge/.venv/Scripts/python.exe -m pytest tools/diagnostics/tests -q
```

## Source references

- `TrackController5/Python/flash_one.py` — `FlashState` (7 states) + log format.
- `TrackController5/firmware/src/enums.h` — timeout/OK/NOK code contract.
- `docs/K3-FAIL-CLOSED-FIRMWARE-DIAGNOSTICS.md` — protocol/ack matrix.
