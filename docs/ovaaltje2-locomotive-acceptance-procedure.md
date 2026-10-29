# Ovaaltje2 Supervised Locomotive Acceptance Procedure

**Status:** prepared 2026-10-29 (PO Addendum 01 §5). **A separate PO GO is required before any locomotive movement.**

This is a practical, operator-level acceptance plan. It preserves the original Siebwalde UI, locomotive control, track mapping and operator behaviour. The software under test is the merged Ovaaltje2-readiness baseline (PR #64, merge `1e266497`), reusing the P2-C-validated production components.

## Safety boundary (fixed)

- **Non-loaded parts (A–D)** may run under the current PO Addendum 01 authorization, but the physical conditions in A1 MUST be established and confirmed by a human operator first (never assumed).
- **Loaded parts (E)** require a **separate PO GO** and a locomotive on the Ovaaltje2 track.
- No firmware flashing. No automatic safety release. Re-arm (112) is always operator-initiated AND operator-confirmed.
- "Physical stop inputs clear" is an **operator confirmation** (there is no software-readable stop sensor); the physical "observed" boundary is V4.

## Part A — Non-loaded physical preflight

| # | Step | Expected result | PASS criterion |
|---|------|-----------------|----------------|
| A1 | Confirm **no locomotive** is on the test track and the **outputs are de-energized** (verified safe electrical/test condition, e.g. track power isolated / H-bridge physically off) | Safe condition confirmed by operator | Operator confirms; not merely assumed |
| A2 | Confirm the **emergency stop** is reachable and functional | E-stop accessible | Operator confirms reachability |
| A3 | Read-only PIC32 identity probe (command 113) | Firmware 1.0.1 / protocol 1 / build 2026-10-26 | `PASS` with matching version |
| A4 | Confirm **no competing controller session** (no active diagnostics session lock) | Session lock free | `preflight` ownership `available` |

## Part B — Normal application cold startup (non-loaded)

| # | Step | Expected result | PASS criterion |
|---|------|-----------------|----------------|
| B1 | Launch the normal WPF app; init page detects the track host | Host detected | Host present; no error |
| B2 | Start the track runtime (Real mode) | PIC32 connection + heartbeat (110) established | Runtime reaches Running; heartbeat frames observed |
| B3 | Observe initial global-stop status | Disarmed/Armed (NOT latched) → init proceeds | Init proceeds through GlobalStopPrecondition |
| B4 | Four-amplifier init: amps **1/3/4/6** detected, identity/compatibility verified, config downloaded + APPLY + verified | CONFIG_OK per amplifier | All four CONFIG_OK (configVerified); no flash performed |
| B5 | Confirm **no unintended output activation** | PWM stays 399 (neutral); HR8 enable-active is NOT treated as physical H-bridge state | No non-neutral HR0; PIC18 inhibited/neutral observed |

## Part C — Previously latched global stop recovery (non-loaded)

| # | Step | Expected result | PASS criterion |
|---|------|-----------------|----------------|
| C1 | Induce/observe a latched global stop (e.g. host-loss from a previous session) | Command 111 reports latched (1 or 2) | 111 = LatchedHostLoss or LatchedHostInitiated |
| C2 | WPF init page shows the latched state + reason | Latched state visible | UI text shows "latched" + reason |
| C3 | Recovery button is available ONLY in the latched condition | Button enabled only when latched | `CanRecoverGlobalStop` true only when latched |
| C4 | Operator authorizes + confirms physical stop inputs clear | Modal confirmation required | Recovery refused until operator OK |
| C5 | Command 112 response confirmed; PIC32 → Armed | 112 emitted, 111 re-read = Armed | `REARM_GLOBAL_STOP` observed; 111 = Armed |
| C6 | C# `GlobalStopController` synchronized to Armed | In-memory state = Armed, reason null | `runtime.GlobalStop.State == Armed` |
| C7 | **No automatic movement authorization** | Movement permission remains NotGranted until observed neutral | `MovementPermission` not granted by re-arm alone |

## Part D — Invalid recovery (non-loaded, fail-closed)

| # | Scenario | Expected result | PASS criterion |
|---|----------|-----------------|----------------|
| D1 | No operator authorization | Recovery stays blocked, no 112 | No 112; init stays gated |
| D2 | Physical-stop-clear NOT confirmed | Recovery refused (fail-closed) | No 112; `BlockedStopInputsNotClear` |
| D3 | Controller recovery unsuccessful / timed out | Recovery does not proceed; no output enable | No 112/107 advance; fail-closed |
| D4 | Communication interrupted during recovery | No accidental enable | No output activation; safe state retained |

Each D case must **fail closed and must not enable outputs**.

## Part E — Loaded locomotive acceptance (REQUIRES SEPARATE PO GO)

Preflight: A1/A2 repeated with the locomotive placed, output energization controlled.

| # | Step | Expected result | PASS criterion |
|---|------|-----------------|----------------|
| E1 | Physical preflight + emergency stop | Locomotive present; e-stop ready | Operator confirms |
| E2 | Normal startup + four-amp init (B2–B4) | Running; CONFIG_OK all four | All four configVerified |
| E3 | Explicit movement authorization | Movement permission granted only after observed neutral (HR0==399 readback) | `MovementPermission == Granted` after neutral observed |
| E4 | First locomotive motion at **low speed** | Locomotive moves at commanded low speed step | Motion observed at low speed |
| E5 | Direction control | Forward and reverse both work | Direction change observed |
| E6 | Acceleration | Speed increases smoothly | Accel observed (no jump) |
| E7 | Controlled deceleration | Speed decreases smoothly | Decel observed |
| E8 | Full stop | Locomotive stops; PWM returns 399 | Stop observed; neutral |
| E9 | Restart | Motion resumes after explicit re-authorization | Re-authorize → motion |
| E10 | Track-section transitions | Locomotive crosses sections; occupancy feedback correct; mapping 1/3/4/6 preserved | Transitions observed; correct feedback |
| E11 | Host-loss behavior | Losing the host latches the global stop; amplifiers inhibit | Global stop latched; outputs neutral |
| E12 | Emergency-stop behavior | E-stop latches stop; recovery requires operator re-arm | Latch + re-arm flow observed |
| E13 | Controlled shutdown | App stops deterministically; outputs neutral; session clean | Stopped; neutral; no orphan process |

## Evidence recording

For every executed step, record: command-level evidence (sent/ACK) and, where physical, the operator-observed state. Never claim a physical stop or physical de-energization from a firmware register readback (V4 boundary). Log the actual application (not only the Diagnostics MCP harness).

## Readiness verdict (for the first supervised physical test)

Recorded after Parts A–D complete: **READY** only when A1–A4, B1–B5, C1–C7 and D1–D4 all pass with the physical-stop-clear and no-output-activation conditions confirmed by the operator.
