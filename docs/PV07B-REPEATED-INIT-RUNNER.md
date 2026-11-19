# PV07-B — Repeated-init runner (hil-d) + amp-6 GROUP_READY diagnosis

**Status:** Developer implementation complete (offline). Live capture NOT executed (authority
discrepancy; see §6). Branch `feature/pv07b-hardware-diagnostics` @ `e386bce0`.

---

## 1. Diagnosis — write order, verify window, firmware accept condition (file:line)

The Stage-5 GROUP_READY failure is reproducible and ordering-sensitive. Source-level facts:

- **Write order.** `GroupReadyStep.ExecuteAsync` writes GROUP_READY to `_requiredAmps` in list
  order (`GroupReadyStep.cs:73-81`); the list is the effective domain, which is sorted ascending
  (`TrackAmplifierGroups.cs:87` → `AllConfigured = _byAddress.Keys.OrderBy(a => a)`), so the order is
  **[1, 3, 4, 6] — amplifier 6 is written LAST**. Config download (`InitializePhase1AmplifiersAsync`,
  `TrackApplicationRuntimeHost.cs:1109`) also iterates the same ascending domain, so amp 6 is also
  configured LAST.

- **Verify window (one-shot + shared deadline).** GROUP_READY is written exactly **once** per amp
  (`GroupReadyStep.cs:73-81`); the single 5 s deadline is set AFTER the write loop
  (`GroupReadyStep.cs:91`), then the verifier only re-reads HR_STATE every ~10 ms
  (`GroupReadyStep.cs:97-106`) and never re-issues the write. A transient reject of amp 6's write is
  therefore never retried — the C# just times out (`GroupReadyStep.cs:123-132`).

- **Firmware accept/reject condition** (`TrackAmplifier4.X/amp_adapter.c:162-168`): GROUP_READY is
  accepted only when `command.enable && status.config_ok && !fault.causes && status.lease_fresh`;
  otherwise it sets `result = AMP_WRITE_RELATIONSHIP` (a Modbus exception 0x03).

- **Lease mechanism.** The PIC32 broadcasts HR1=0x4000 every `MOTION_LEASE_RENEWAL_MS = 100 ms`
  (`enums.h:92`, `slavehandler.c:274-285`); the PIC18 lease expires at `AMP_LEASE_TICKS = 294`
  ticks ≈ 500 ms (`amp_model.h:6`, `amp_core.c:97`). The broadcast is priority in
  `SendNextMessage` and independent of the mailbox, so lease stays fresh while the superloop runs.

## 2. Why amp 6 fails the batch but accepts singly

At the "BEFORE" snapshot amp 6 reports HR1=0xC000 (enable + lease-fresh) and HR8=0xC708
(config_ok | init_done | enable_active | inhibited | STANDBY), i.e. **all four accept conditions
appear satisfied**, yet it stays STANDBY while 1/3/4 go READY. The single-amp GROUP_READY to amp 6
(written alone after the Stage-4 pause) succeeds. This rules out a hard backplane fault and points to
a transient/timing difference: in the batch, amp 6's write is delayed by ~3 mailbox drain cycles
(each = FC16 send + echo wait) while its lease/fault state can drift, and the one-shot write is
rejected at the firmware without any C# retry.

**The decisive discriminator is the PIC32 MBUS event ring (TCP 50000), which the PV07-C
instrumentation records** (`mbus_event.h`: ENQUEUE/GATE_DROP/FC16_SENT/PURGE/TX_DONE/ACK/TIMEOUT/
EXCEPTION). For amp 6's HR23=0x0002 write:
- `FC16_SENT(3) → TX_DONE(5) → ACK(6)` = delivered + accepted (then HR8 should be READY);
- `FC16_SENT(3) → TX_DONE(5) → EXCEPTION(8, val=0x03)` = delivered + **REJECTED** (one of
  enable/config_ok/fault/lease_fresh false at that instant);
- `FC16_SENT(3) → TIMEOUT(7)` / `GATE_DROP(2)` = not delivered (wire/backplane/gate).

This is NOT yet observable from the C# side (HR15 is clobbered by the 100 ms lease renewal,
`amp_adapter.c:26-32`; HR8 alone cannot separate "rejected" from "not delivered"). The live capture
must answer it.

## 3. Implemented (commit `e386bce0`)

- `GlobalStopRecoveryDecision` (Core): pure classifier 111 → ProceedNoRearm (0/3) / Rearm (1) /
  StopFailClosed (2 / unexpected / null). Campaign-only policy; the production
  `GlobalStopPreconditionStep` is unchanged (operator may legitimately re-arm either latch).
- `hil-d` (`RunHilDAsync`): per cycle — read 111 → decide → (REARM once for host-loss only) →
  real Stage 1-5 init → verify all amps READY + record HR1/HR8/HR9/HR22 → controlled Stop →
  per-cycle JSONL. Bounded per-cycle + overall (default 8 h), operator STOP (Ctrl+C + `--stop-file`),
  fail-closed on first deviation. No Stage 6/7, no flash.
- `tools/diagnostics/tcp50000_capture.py`: read-only TCP-50000 MBUS event-ring capture + parse.

## 4. Offline test results

- `GlobalStopRecoveryDecisionTests` — 9 tests PASS (REARM only for 1; STOP for 2 / 4 / 5 / 0xFF / null).
- `DeterministicTrackTransportEndToEndTests.RealMode_LatchedHostInitiated_NeverRearmedByCampaignDecision` —
  PASS: host-initiated latch → no 112, no 107, runtime stays gated then fails closed.
- Full `SiebwaldeApp.Core.Tests` suite: **1264/1264 PASS** (0 failures).
- `SiebwaldeApp.RealInitHilHost` build: **0 errors**.

## 5. Live runbook (NOT executed — requires authority resolution + Integrator/operator)

1. Operator preflight: track/H-bridge de-energized, track clear, E-stop available, no competing UDP
   session, ports free. Controller 192.168.1.193.
2. Start the capture: `python tools/diagnostics/tcp50000_capture.py --out Logging/<run>.log --slave 6`.
3. First a read-only probe to confirm firmware + latch state (113/111): `hil-a`.
4. If 111 = 1 (LatchedHostLoss) and 113 confirms the exception-capture firmware, run the campaign:
   `SiebwaldeApp.RealInitHilHost hil-d --domain 1,3,4,6 --jsonl Logging/<run>.jsonl --max-duration 28800 --stop-file <stop-file>`.
   (Or a single cycle: `hil-b` after a REARM.)
5. Parse the capture for amp 6's `reg=23` event sequence (ACK vs EXCEPTION(0x03) vs TIMEOUT/GATE_DROP).
6. Operator STOP: Ctrl+C or create the stop file. Fail-closed on any unexpected latch/deviation.

## 6. Authority discrepancy (blocking the live capture)

The Active State Manifest (`.opencode/workflow/active-state.json`, 2026-11-16) records the PV07-B
Stage-1-4 authority with **"NO GROUP_READY, NO REARM, NO Stage 5/6"**, while this task's campaign
authorizes REARM (112) + repeated Stage 1-5 init. The recorded authority does not cover the
state-changing GROUP_READY/REARM live work. Per Workflow v1 §11.6 and the Developer contract, the
live V4 execution was therefore NOT performed by the Developer: it needs (a) a valid, recorded
campaign authority (or an explicit new GO) and (b) Integrator-owned V4 execution with operator in
the loop. All software and tooling is prepared and committed.
