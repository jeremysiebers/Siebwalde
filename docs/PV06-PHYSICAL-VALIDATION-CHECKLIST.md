# Physical Validation 06 — Operator Checklist (prepared, NOT executed)

**Status:** preparation only. Execution requires a separate PO authorization for the physical session (REARM and state-changing init are gated; no Stage-6 track power, no locomotive movement).

**Objective:** demonstrate **repeatable Stage 1–5 initialization after an operator-confirmed REARM**, with continuous host-heartbeat (110) throughout the init. This validates the PR #76 heartbeat-lifecycle correction (master `92301e80`).

**Baseline:** PIC32 firmware **1.0.2** (host watchdog **2000 ms**, unchanged); PIC18 amplifiers 1/3/4/6 (expected checksum `0x0E39`); C# heartbeat **250 ms**; single-client UDP (PIC32 10000 / C# 10001, 82-byte framing). Non-energizing (HR23 bit 0 stays 0; GROUP_READY uses HR23 bit 1 only).

**Evidence discipline:** distinguish **(a) host-transmitted heartbeat** (C# `TrackAppLog` 110 send timestamp) from **(b) PIC32 received/processed heartbeat** (controller log `HOST_HEARTBEAT` handling / armed confirmation). A transmitted 110 does NOT prove the PIC32 processed it. Record the **maximum observed host-side heartbeat gap**; a firmware watchdog worst-case guarantee must NOT be inferred from a host-side measurement.

---

## 0. Pre-session process/session/port ownership (before any hardware)

- [ ] No `SiebwaldeApp` process running (`Get-Process SiebwaldeApp` empty).
- [ ] No orphan heartbeat/session: ports **10000** and **10001** free (`Get-NetTCPConnection`).
- [ ] No competing UDP controller client (no other C# app / Python tooling).
- [ ] Fresh WPF process from the merged master build (includes PR #76 `EnableHeartbeat`).

## 1. Physical safety preflight (operator confirms fresh, per start)

- [ ] No locomotive on the oval; track clear.
- [ ] Track/H-bridge outputs **physically de-energized** (independent check, not inferred from software).
- [ ] Emergency isolation immediately available.
- [ ] Controller + amplifier connections correct.
- [ ] Operator physically present for the full session (in-the-loop).

## 2. PIC32 firmware + global-stop status

- [ ] `QueryControllerVersion` (113) reply: firmware **1.0.2 / protocol 0x01**.
- [ ] `GlobalStopPreconditionStep` (111) reply recorded: state 0/1/2/3.
- [ ] If latched (1/2): record the latch reason, then §3 before proceeding.

## 3. Explicit operator REARM (per cycle; never automatic)

- [ ] Operator confirms physical stop inputs clear.
- [ ] Operator explicitly authorizes "Recover global stop" (112) — one action at a time, explicit confirmation.
- [ ] Post-REARM 111 re-confirmed as **armed (3)**, not still latched.
- [ ] **Fresh operator confirmation required before any REARM in cycle 2.**

## 4. Heartbeat (110) evidence throughout init

- [ ] Record the C# `HOST_HEARTBEAT` **transmission timestamps** during the init (after REARM, through 107/100/106/101/104/105).
- [ ] Record the maximum host-side **heartbeat gap** during the init.
- [ ] Correlate with the PIC32 log: confirm the heartbeat was **received/processed** (armed stays armed; no `Pic32HostLoss` re-latch).
- [ ] Record the global-stop state (111) **after REARM and during MBUS init** — confirm it stays armed (3), does not re-latch.

## 5. Stage 1–3 acknowledgements

- [ ] 107 (RESET) DONE received.
- [ ] 100 (SLAVES_ON) DONE received.
- [ ] 106 (START_DATA_UPLOAD) DONE received — **must NOT time out** (this is the PV05 failure point).

## 6. Fresh HR7 / HR11 / HR21 / HR22 readings

- [ ] For each of amps 1, 3, 4, 6: fresh `HR7` (identity), `HR11` (checksum `0x0E39`), `HR21` (version), `HR22` (patch/protocol).
- [ ] Record `identityOk` / `protocolOk`; flag any `HR22` `0x0000`→`0x0002` inconsistency.

## 7. Stage 4 P1CFG verification

- [ ] `InitializePhase1AmplifiersAsync` P1CFG: each amplifier INVOKE `DownloadAsync` → HR3/HR4/HR5/HR6 write+echo verified → APPLY verified (`INIT_DONE|CONFIG_OK`).
- [ ] Stage `ConfigureValidate` **passed**; no amplifier SKIPPED as "incompatible firmware/protocol".

## 8. Stage 5 P1GR verification (amps 1/3/4/6)

- [ ] `WriteGroupReadyAsync` P1GR: `GROUP_READY` HR23 = 0x0002 (bit 1, non-energizing) to 1/3/4/6.
- [ ] **All four** amplifiers READY; no amplifier-6 rejection.
- [ ] Stage `GlobalReady` **passed**; runtime reaches `StoppedAtGlobalReady`.
- [ ] HR8/HR9/HR1 fresh readback per amplifier at GROUP_READY (HR9 fault value captured).

## 9. Controlled StopAsync + clean teardown

- [ ] WPF Stop while at `StoppedAtGlobalReady`: responsive (bounded, no deadlock).
- [ ] Shutdown duration recorded.
- [ ] Heartbeat stops after Stop (no 110 after teardown).
- [ ] Process terminates; ports 10000/10001 released; no orphan process.
- [ ] Track outputs remain de-energized (operator physically confirms).

## 10. Repeatability gate

- [ ] Run the full cycle **≥ 2 times back-to-back** (clean Stop between), each independently reaching `StoppedAtGlobalReady` **after its own operator-confirmed REARM**, with heartbeat-gap evidence per cycle.
- [ ] Record whether the PIC32 **re-latches** at any point (and the observed gap if so).

---

**STOP conditions:** any energization; any "incompatible"/READY rejection/unexpected HR9 fault; any REARM without operator confirmation; loss of operator visibility; any PIC32 re-latch mid-init.

**Not part of PV06 (separate PO authorization):** Stage-6 track-power enable, any locomotive movement, firmware/watchdog changes.
