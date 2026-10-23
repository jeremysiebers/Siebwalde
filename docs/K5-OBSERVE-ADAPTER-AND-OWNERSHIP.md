# K5 — OBSERVE Adapter, Exclusive Controller Ownership, and Future Physical OBSERVE Plan

Date: 2026-10-23. OFFLINE_ONLY. This records the K5 OBSERVE adapter design, the
exclusive controller-ownership contract (offline/simulated only), and the
future physical OBSERVE validation plan (prepared, NOT executed).

## 1. OBSERVE adapter (offline)

`tools/diagnostics/observe.py` provides typed, OFFLINE diagnostic models and
parsers (ControllerSnapshot, AmplifierSnapshot, AmplifierInventory) and an
explicit capability classification per operation:

- `OFFLINE_ONLY`
- `LIVE_READ_ONLY_SOURCE_VERIFIED`
- `LIVE_READ_ONLY_HARDWARE_VALIDATED`
- `STATE_CHANGING`
- `DESTRUCTIVE`
- `UNKNOWN_EFFECT`

All K5 parsers are `OFFLINE_ONLY`. Classification does not grant execution
authority. Offline mode cannot open a socket/serial/controller connection
(enforced in tests by asserting stdlib-only imports). The Diagnostics Core
remains usable without MCP; the MCP is only the agent-facing surface.

## 2. Exclusive PIC32 controller ownership (design; offline contract only)

The future live adapter must prevent competing controller sessions. Required
contract (to be implemented + hardware-tested in a later, separately authorized
increment — NOT started in K5):

- **Single live owner**: an ownership record keyed by process id + session id,
  acquired exclusively before any live command and released on completion.
- **Bounded lifetime / lease**: ownership expires after a bounded period unless
  refreshed; a crash or stalled owner is detected (stale-owner) and reclaimed.
- **Safe shutdown**: an explicit release path that returns the controller to a
  neutral state before releasing.
- **Operator visibility**: the owner process/session/window is visible (e.g. a
  named session), with a manual stop path.
- **No implicit firmware update**: the live diagnostic path must not run the
  C# `FlashFwTrackamplifiers` startup (which auto-flashes on HR11 mismatch) —
  the diagnostic adapter uses a verified non-flashing command subset.
- **Bypass awareness**: raw UDP / shell tools can bypass an MCP permission, so
  ownership + safety are enforced in the core, not only at the MCP boundary.

## 3. Future physical OBSERVE validation plan (prepared, NOT executed)

Goal: a controlled, read-only physical session that starts with **PIC32-only**
identity/status readback before touching any PIC18.

Prerequisites (operator): exclusive controller ownership; no C# runtime started
(it could auto-flash); no competing UDP client; a recorded operator present.

Sequence (source-verified, read-only only): controller CONNECT (250) →
`GET_CONTROLLER_VERSION` (113) readback; then, only if non-destructive status is
proven, PIC18 detection/readback via the non-flashing `DETECT`(101)/`START_DATA_UPLOAD`(106)
read-only path — never `RESET`(107)/`SELECT_SLAVE`(123)/`ERASE`(125)/`WRITE`(126)/`FLASH`(122).

Stop conditions: any unexpected response, timeout, or unverified side effect →
STOP and report UNKNOWN; amplifier 1 and 3 state reported UNKNOWN unless a fresh
independent HR11 readback is obtained. Evidence: host log + (optional, separately
authorized) PicoScope D0/D1 correlation to distinguish transmitted commands from
actual PIC18 replies.

**GO/NO-GO**: GO only if a source-verified non-destructive read-only command
subset is established and all prerequisites hold. If no suitable non-destructive
command can be established, report NO-GO. **No physical test may execute without
a new explicit scoped PO LIVE_HARDWARE authorization.**

## 4. Three-level Diagnostics roadmap (unchanged from K4)

Level 1 OBSERVE (offline foundation in K4/K5) → Level 2 TEST → Level 3
FLASH/RECOVERY, each gated by separate authorization and the K3 fail-closed
contract. The unverified PIC18 bootloader response must not be concealed by any
MCP abstraction.
