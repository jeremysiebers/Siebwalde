# Siebwalde Diagnostics MCP Foundation

Date: 2026-10-23. This records the K4-B offline Diagnostics Core and the narrow
offline Diagnostics MCP, plus the three-level Diagnostics roadmap. **Only the
offline OBSERVE foundation is implemented in K4** — no live TEST or
FLASH/RECOVERY, no hardware access.

## 1. Diagnostics Core (`tools/diagnostics/`)

A typed, OFFLINE, MCP-independent Python core that analyzes **saved** textual
traces (firmware-session logs, raw UDP/feedback traces, command sequences). It
never opens a socket, a serial port or a hardware connection, and never imports
the live `Comm.DataAquisition` transport.

Evidence levels it distinguishes (never auto-promoted):

- `REQUESTED` — an operation was requested.
- `SENT` — the command was transmitted (TX).
- `ACKNOWLEDGED` — the host received the PIC32 `*_RETURNED_OK`/DONE ack.
- `PHYSICALLY_OBSERVED` — an independent instrument observation exists.
- `VERIFIED` — completion is externally verifiable (e.g. checksum match).
- `UNKNOWN` — the hardware state cannot be established (fail closed).

Core API: `analyze_firmware_session(log_text)`, `analyze_udp_trace(trace_text)`,
`compare_protocol_traces(expected, actual)`, `capabilities()`. It reuses the K3
`FlashState` semantics (7 states) and the protocol/ack matrix (timeout codes
201/211/[211 for WRITE_CONFIG]/231/241; OK 202/212/222/232/242) without creating
a second firmware state machine. A sent command is never promoted to confirmed
success without its ack; a session ending at `FAILED_UNKNOWN_HARDWARE_STATE`
is reported `evidence_level: UNKNOWN`.

## 2. Diagnostics MCP (`tools/diagnostics/mcp_server/`)

A narrow FastMCP server (`diagnostics`) wrapping the core with four read-only
tools:

- `diagnostics_capabilities()`
- `diagnostics_analyze_udp_trace(trace_text)`
- `diagnostics_analyze_firmware_session(log_text)`
- `diagnostics_compare_protocol_traces(expected, actual)`

Inputs are bounded (size-limited) and malformed input is rejected with a clear
error. No filesystem access, no raw command, no shell, no hardware-control
entry point, and default execution is offline-only and incapable of opening a
PIC32 connection. The existing `picoscope` and `knowledge` MCPs are unchanged;
the new MCP is registered project-scoped only after the offline boundary is
proven (tested).

## 3. Three-level Diagnostics roadmap

**Level 1 — OBSERVE** (K4: offline replay/saved-evidence analysis implemented).
Future physical read-only controller diagnostics require explicit
`LIVE_HARDWARE` authorization and a proven side-effect classification — a command
named GET/READ is not by itself non-destructive.

**Level 2 — TEST** (future): selected initialization steps, protocol tests,
controlled reset/state-changing diagnostics, scoped hardware sessions; requires
exclusive controller ownership and backend-enforced authorization.

**Level 3 — FLASH/RECOVERY** (future): firmware-artifact qualification, target
identification, physical-isolation checks, controlled bootloader entry,
ERASE/WRITE with verified acknowledgements, post-write verification, explicit
recovery. The K3 fail-closed contract remains mandatory; the unverified PIC18
bootloader response must not be concealed; the backend rejects unsafe operations
regardless of agent prompt or MCP permission.

## 4. Next step toward verified physical OBSERVE

The offline core + MCP are the foundation. The next increment toward *physical*
OBSERVE is a **separately authorized, read-only controller-diagnostic path**
using the existing C#/Python `--read-hr`-style readback and the PicoScope MCP —
gated by a fresh scoped `LIVE_HARDWARE` grant, with a proven non-destructive
side-effect classification before any live command. This is not started in K4.
