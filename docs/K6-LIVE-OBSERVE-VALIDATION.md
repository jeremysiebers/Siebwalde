# K6 — Live Physical OBSERVE Validation (Command 113)

**Increment:** K6 (K6-A offline development + K6-B live physical OBSERVE)
**Status:** K6-B executed and independently reviewed (OBSERVE_PASS). Branch not merged into master (merge requires separate PO authorization).
**Branch:** `feature/k6-observe` (base = merged master `5269624e4d203e6a118ccb88625267f1cafb3483`, PR #50)
**Authorized by:** Product Owner directive K6-A + K6-B (single-use, target-specific, command-specific LIVE_HARDWARE grant).

---

## K6-A — Offline development and validation

### Source revisions
- `5cc2402f` — `feat(k6-a): typed allowlisted PIC32 live OBSERVE transport for command 113`
- `ae18cb44` — `feat(k6-b): single-use live OBSERVE entry point for one command-113 transaction`

### Command-113 protocol contract (source-verified)
- **Request:** `[0xAA, 0x71]` — 2 bytes, header + command 113, **no payload, no footer** (mirrors C# `TrackCommClientAsync.BuildFrameFromSendMessage`; `QueryControllerVersionStep` sends `SendMessage(GET_CONTROLLER_VERSION, Array.Empty<byte>())`).
- **Response:** header `0xAA`, command `CONTROLLER(10)`, `data[0]=113` (echo), `data[1..3]=MAJOR/MINOR/PATCH`, `data[4]=PROTOCOL_VERSION`, `data[5]=0`, `data[6..]=CTRL_RELEASE_TIMESTAMP` (null-terminated). Source: `TrackController5/firmware/src/controller.c` `REPLYxCONTROLLERxVERSION` (119–139).
- **Side effect:** NONE — `REPLYxCONTROLLERxVERSION` only constructs a reply and pushes it to the send mailbox (`ethernet.c` `PUTxDATAxINxSENDxMAILxBOX`). No flash/reset/MBUS/amplifier action. **VERIFIED_READ_ONLY.**

### Verified controller endpoint
- `192.168.1.193` — from `SiebwaldeApp/SiebwaldeApp.Core/app.config:79`, `Properties/CoreSettings.settings:42`, `SiebwaldeApp/App.config:57`, and the Knowledge Foundation `device:pic32.address`. TX port `10000`, recv bind `10001` (`ethernet.c` `SERVER_PORT`/`CLIENT_PORT`). No DNS, no `.100` fallback, no scan.

### Implemented transport and safety guards
- `tools/diagnostics/live_transport.py` — single-purpose `Command113Transport`; only I/O entry point `send_get_controller_version()`; stdlib-only; socket opened in-method + closed in `finally`; fail-closed decode (wrong source / wrong length / bad header / wrong command / bad echo → `FAILED`; timeout / socket error → `UNKNOWN`).
- `tools/diagnostics/observe_guard.py` — executable enforcement (not docstrings): command allowlist `{113}`, literal endpoint `192.168.1.193` (no DNS/fallback), single-use `ObserveSession.consume()` (1→0, second call `AuthorizationBlocked`), bounded timeout `≤5.0s`, no retry/recovery/flash/reset.
- `tools/diagnostics/k6b_observe.py` — the single sanctioned live entry point (consume once → one send → write git-ignored evidence).

### Offline tests and regressions
- Diagnostics (K6-A): **91 passed** (36 new: protocol + safety + runtime + no-socket).
- K3 firmware driver (`test_flash_one.py`): **43 OK**.
- Comm.py (`test_Comm.py`): **5 OK**.
- Knowledge Foundation: **22 passed** (→ 25 after K6 knowledge additions).
- Tool preservation guard: **exit 0** (all protected roots present; accepted MCPs registered).

### Independent review
- **Integrator:** `PASS — LIVE OBSERVE READINESS` (7 points verified against source; allowlist `{113}` + `VERIFIED_READ_ONLY` side-effect classification confirmed without uncertainty).
- **Architect:** `DESIGN_ACCEPTABLE` (ownership/session/safety architecture sound; see residual risks below).

### Preflight
Machine-readable preflight written to `Logging/K6B/k6b-preflight.json`:
`K6A_PREFLIGHT=PASS`, `INTEGRATOR_VERDICT=PASS`, `EXCLUSIVE_OWNERSHIP=VERIFIED`, `COMMAND_113_SIDE_EFFECT_CLASSIFICATION=VERIFIED_READ_ONLY`, `AUTHORIZED_COMMAND_COUNT_REMAINING=1`, `TARGET_IDENTITY=VERIFIED` → **final GO**.
- Ownership: C# Siebwalde app **not running**; only Python processes present were the 3 offline OpenCode MCP servers (picoscope/knowledge/diagnostics). A local socket bind on `10001` provides mutual exclusion against a concurrently running `Comm`/`flash_one` client; a local check cannot rule out an *external* network client, but the operation is read-only and safe under that limitation.

---

## K6-B — Live physical OBSERVE

**Executed:** yes — exactly one command-113 request.

| Field | Value |
| --- | --- |
| Request count | 1 |
| Request bytes | `aa71` |
| Response bytes | `aa0a710001000000323032362d31302d323000…` (82-byte `udpTrans_t`) |
| Destination | `192.168.1.193:10000` |
| Response source | `192.168.1.193` (source port 61401) |
| Decoded version | **0.1.0 / protocol 0 / build "2026-10-20"** |
| Elapsed | sent 09:29:44.337Z → received 09:29:44.338Z (~1 ms) |
| Response SHA-256 | `f40a31edf3924488f82e1ee542bac9400d6dd9d3864249b8492504acb4a669c8` |
| Authorization | consumed (`authorized_command_count_remaining: 0`) |
| Classification | **OBSERVE_PASS** |

Evidence file (git-ignored): `Logging/K6B/k6b-observe-20261024T092944Z.json`.

### Independent post-execution review
**Integrator: PASS.** Verified exactly one request, command 113 encoding, correct destination `192.168.1.193`, no other command emitted, ownership/authorization handled correctly, version decode matches source layout, and no reset/flash/recovery/bootloader/PIC18/C#-INIT in the execution path. One sub-claim ("no competing C# client") rests on the preflight registration rather than an in-transaction process snapshot (documented UNKNOWN for that sub-claim only).

### Final physical OBSERVE verdict
**OBSERVE_PASS** — a valid, source-consistent physical response was received and independently analyzed.

### Key finding — firmware-version mismatch
- Physical readback: **0.1.0 / protocol 0 / "2026-10-20"**.
- Master-branch source `TrackController5/firmware/src/version.h` (identical at `376c56e` and `5269624e`): **1.0.0 / 0x01 / "2026-10-17"**.
- The physical readback matches the Knowledge Foundation's recorded `firmware:pic32-0.1.0` (already PROVEN_HARDWARE). The `version.h` bump to 1.0.0 was a **source-only change that was never flashed** to the physical PIC32. The readback proves the controller *reports* 0.1.0; it does **not** independently prove the flash image content.

---

## Closure

### Knowledge Foundation update
Three nodes added (graph rebuilt to **144 nodes / 83 relationships**; tests **25 passed**):
- `command:113` — `ProtocolCommand` GET_CONTROLLER_VERSION, HOST_TO_PIC32, non-destructive (PROVEN_SOURCE).
- `obs:k6b-cmd113-readback` — physical readback 0.1.0 / protocol 0 / "2026-10-20" (PROVEN_HARDWARE; summary keeps the "reports, not flash-proven" distinction).
- `obs:k6b-version-mismatch` — deployed 0.1.0 vs master `version.h` 1.0.0 (PROVEN_HARDWARE).

`device:pic32 RUNS_FIRMWARE firmware:pic32-0.1.0` left unchanged (already correct). PIC18 / amp-1 / amp-3 / PICO-5B assertions untouched.

### Tool preservation
`tools/preservation_guard.py` exit 0; all protected roots present; accepted MCPs (picoscope, knowledge, diagnostics) still registered in `opencode.json`. No tool deleted, retired, or destructively migrated.

### Commits (pushed branch)
- `5cc2402f` — K6-A transport + guard + 36 tests.
- `ae18cb44` — K6-B live entry point + `.gitignore` for evidence.
- Knowledge seed + tests (recorded in the knowledge update; committed as part of K6 closure).
Branch `feature/k6-observe` based on `5269624e`. **No master merge** (requires separate PO authorization).

### Remaining uncertainties / limitations
- **PIC18 amplifier state remains UNKNOWN** for amp-1 and amp-3 (no fresh independent HR11 readback; command 113 says nothing about PIC18 firmware). Not affected by K6.
- The command-113 readback proves the reported PIC32 version, not the programmed-flash content.
- Response source port was `61401`, not the documented `CLIENT_PORT 10001` (ephemeral Harmony UDP port); the decoder validates source IP only. Non-material for the read-only query; recorded as a hardening note.
- External network clients cannot be ruled out by a local lock (documented visibility limitation; safe for this read-only operation).

### Architect residual risks (non-safety-critical, logged to backlog)
1. Single-use is enforced at the orchestration layer (`k6b_observe.py` consume-once) but the stateless `Command113Transport` does not itself consult the session token — a second request is technically possible via a direct transport import (read-only blast radius).
2. The `except OSError` branch reports `"sent": True` unconditionally; on a bind-failure nothing was sent (evidence over-claim).
3–5 (minor): `port`/`recv_port` unvalidated; socket creation outside `try`; no dedicated EADDRINUSE test.

### Recommended next increment
A bounded corrective increment to close the Architect residual risks 1–2 (bind the transport to a consumed session or make it self-consuming one-shot; fix the `sent` flag), then — only under a new scoped PO grant — a follow-up physical diagnostic (e.g., a read-only `GET_BOOTLOADER_VERSION` 124, or an independent HR11 readback to resolve the amp-1/amp-3 UNKNOWN).

### New PO decision required for further hardware diagnostics
This K6 authorization was single-use, target-specific, command-specific, and is now consumed. Any further physical command (including a different command, target, or a repeat of 113) requires a new scoped Product Owner grant.

---

## K7 — Corrective hardening (OFFLINE ONLY)

**Status:** implemented + offline-tested; no hardware, no socket, no firmware/C# change, no master merge. Branch `feature/k7-diagnostics-hardening` (worktree `C:\Localdata\Siebwalde-k6`).

Closes the Architect residual risks 1, 2, 4, 5 (risk 3 — port range validation — is unchanged/out of scope).

### 1. Sent-state accuracy (`tools/diagnostics/live_transport.py`)
- `send_get_controller_version()` now records a typed `send_state` plus an accurate `sent` flag in `evidence`.
- `send_state` vocabulary (the furthest stage actually completed): `not_attempted` → `bind_failed` (socket create + bind) → `send_failed` (sendto raised) → `sent` (sendto returned) → `received` (recvfrom returned) → `validated` (response decoded).
- `sent` is `True` **only** when `sendto` returned. A bind/socket-create failure yields `sent: False` and `send_state: "bind_failed"`; a sendto failure yields `sent: False` and `send_state: "send_failed"`.
- `sock = self._socket_factory()` was moved inside the guarded block, so a socket-creation failure maps to `UNKNOWN` (not an exception). Fail-closed classifications (PASS/FAILED/UNKNOWN) and the command-113-only allowlist are unchanged.
- A decoded `PASS` is explicitly labeled `controller_reported: true` and `physical_execution: "unknown"` — the readback proves the controller *reported* a version, never the programmed flash content.

### 2. Single-use authorization at the transport boundary (`tools/diagnostics/live_transport.py` + `observe_guard.py` + `k6b_observe.py`)
- `Command113Transport` now accepts a single-use `ObserveSession` and **consumes it atomically** (thread-safe) inside `send_get_controller_version()` immediately before any I/O.
- A missing / invalid / already-consumed session yields `UNKNOWN` (reason `authorization absent` / `command blocked` / `endpoint blocked` / `authorization already consumed`) with **no send**.
- `ObserveSession.consume()` is guarded by a `threading.Lock`, so two concurrent callers cannot both consume the same grant (exactly one succeeds).
- The token is **not restored** after an I/O error: a failing send still consumes the authorization, so there is no second attempt.

### Single-use boundary (explicit)
The authorization token is an **in-memory, single-process** object. It enforces single-use within one process only. It is **NOT** a system-wide hardware lock, and a process restart (which re-creates a fresh, un-consumed session) must **NOT** be interpreted as renewed permission. Durable, cross-process authority is deliberately out of scope; any physical command still requires a fresh, separate Product Owner grant.

### Offline regression
- `tools/diagnostics/tests`: **111 passed** (91 baseline + 20 new: send-state accuracy + single-use/concurrency).
- `TrackController5/Python/test_flash_one.py`: **43 OK**; `test_Comm.py`: **5 OK**; `tools/knowledge/tests`: **25 passed**; `tools/preservation_guard.py`: **exit 0**.
