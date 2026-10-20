# PHASE-1 LEGACY-MINIMAL RECOVERY — STABILIZATION REPORT

Date: 2026-10-20. This is the consolidated offline deliverable for the legacy-first minimal-delta strategy. No hardware was flashed, reset, or connected during its preparation. This document is durable evidence, not a hardware authorization.

## 1. Current repository / worktree identities

| Worktree / branch | HEAD | Dirty | Role |
|---|---|---|---|
| `C:\Localdata\Siebwalde` `docs/firmware-flash-recovery-reference` | `d7167e85…` | dirty | docs / reference |
| `C:\Localdata\Siebwalde-integration` `integration/phase1-firmware-combined` | `0f8cd81a…` | dirty (7 tracked + 14 untracked) | prior integration candidate (superseded) |
| `C:\Localdata\Siebwalde-legacy-minimal` `recovery/phase1-legacy-minimal` | `928ea7cf…` (L) | clean | legacy source reference |
| `C:\Localdata\Siebwalde-stabilization` `recovery/phase1-stabilization` | `7afe86f…` | clean | **stabilization candidate** |
| `C:\Localdata\bootrec\TrackController5\firmware` | non-Git | n/a | experimental PIC32 build (excluded) |
| `C:\Localdata\Siebwalde-Firmware` | `e845b9d2…` (F, detached) | clean | F1 restoration reference |

## 2. Exact current experimental changes (to be excluded from the candidate)

Confirmed present only in the dirty worktrees / non-Git `bootrec` tree, NOT in the stabilization candidate:

1. **PIC32 peripheral bus 2 clock 84→63 MHz** — `bootrec/…/system_config.h:86` (`63000000`, PB2DIV 3→4) + `sys_clk_pic32mz.c` (TEMPORARY-DIAGNOSTIC baud-align). Category D.
2. **PIC32 fault-ISR diagnostics** — `bootrec/…/system_interrupt.c` reads `U2RXREG` + `LOG_Printf("DIAG-FE …")`; `slavecommhandler.c` `DIAG-WRITE` HR3–6; `PetitModbus.c` `DIAG-READ`. Category D.
3. **PIC18 9-bit TX reply framing** — `TrackAmplifier4.X/modbus/PetitModbusPort.c` `EUSART1_Write9` `TX9=1` → checksum `0xCD1B`. Category D.
4. **C# config-verification workarounds** — `0f8cd81` (HR15→value+HR8, `Timeout 2s→20s`) + dirty W (per-register 3×750 ms retry, HR22 read + `Compatible` freshness coupling + `2s→5s`, `Phase1Failure` clearing, flaky-write test). Category D/E.

## 3. Proven legacy component references

- L = `928ea7cf6061737d26db56580c236347f0f746a8` (legacy source: PIC18/PIC32/backplane).
- F = `e845b9d2e97ed92f00e0d9e3e031ed3a6eae2a7b` (F1 restoration; component trees == L + 5 guidance docs).
- Legacy PIC18 Offset HEX: `B848EF04…`, checksum `0x251F`.
- Legacy PIC32 HEX: `A90E4495…`; archived candidate matches all 267,316 populated bytes of the historical readback (`04AE6A69…`) — binary reference verified; exact binary→source provenance UNPROVEN (G).

## 4. PIC32 clock configuration comparison

- Integration I: `SYS_CLK_BUS_PERIPHERAL_2 = 84000000` (84 MHz), PB2DIV=3.
- Experimental B (`bootrec`): 63 MHz, PB2DIV=4 (TEMPORARY-DIAGNOSTIC baud-align).
- **Stabilization candidate: 84 MHz (restored).** PB2 feeds only UART2; the 63 MHz experiment altered the amplifier link timing without measured justification. No clock change in the candidate.

## 5. PIC32 UART/ISR comparison

- Legacy/committed PIC32 UART2: **9-bit (`PDSEL=3`) for BOTH master transmission (address-marked request, D8=1 on the address byte) and receive**, 2 Mbaud. The PIC18 amplifier's reply is 8-bit TX (`TX9=0`, `TX9D=0`) — the historical asymmetry (master 9-bit RX vs amplifier 8-bit TX), present in L and NOT a new Phase-1 defect. `PetitModbusIsReady()` readiness.
- Experimental B added: fault-ISR `U2RXREG` sampling + formatted `DIAG-*` logging, and the 63 MHz clock.
- **Stabilization candidate: no ISR diagnostics, no `U2RXREG` consumption in the fault path, 84 MHz.** The guard fix `346d962` (`if (!PetitModbusIsReady()) break;` before the HR2 address write) is the only UART-path change retained.

## 6. PIC18 interrupt and UART comparison

- `interrupt_manager.c` is **identical** legacy→current. IPEN=0; RX/TMR3/TMR4 use software dispatch order (no distinct hardware priority levels). No evidence Phase-1 removed a high-priority UART interrupt.
- **No interrupt-priority change in the candidate.** The experimental amplifier 9-bit reply TX (`TX9=1`, `0xCD1B`) is REVERTED — the legacy amplifier 8-bit reply TX (`TX9=0`) is the baseline policy. The PIC32 master transmission remains 9-bit address-marked in the candidate (unchanged from legacy).

## 7. PIC18 safety delta and ServiceTick analysis

- Phase-1 safety core retained (A/B): `amp_core.c` MPL lease/fast-stop/watchdog/E-stop/PWM-clamp/brake; `amp_adapter.c` lease renewal + identity encode; `regulator.c` PWM limits.
- `Amplifier_ServiceTick` (cooperative tick in config+run loops) retained; its modulo-256 backlog counter has no established WCET bound (audit §4). This is a tracked open question (G/worst-case-latency), not a proven failure, and is NOT changed in the candidate.

## 8. PIC32 Modbus minimal corrective changes

- KEEP `346d962`: `PetitModbusIsReady()` guard before the HR2 address write (genuine transaction-acceptance fix — `SENDxPETITxMODBUS` silently drops a busy-transmitter request).
- REVERT `06b54be`: case-4 `SLAVE_DATA_TIMEOUT`→retry (re-issues the write, masks an unproven transient timeout, introduces a re-addressing/duplicate-write race).
- Deferred inherited defects: R1 (transaction ownership released before CRC), R2 (raw MbCommError codes) — deferrable; recorded for the full audit.

## 9. Bootloader 131/132 — disabled / recovery status

- Commands 131 (single-target flash) and 132 (bootloader-only recovery) are **RETAINED in source but FROZEN** — not usable for recovery until resolved:
  - R3: bootloader response accumulation lacks a strict 74-byte object boundary (overflow risk).
  - R5: 131/132 lack firmware-side local target-range validation.
- No 131/132 recovery is authorized until R3/R5 are resolved (or conclusively bounded) and a single, non-contending target is established. 132 is a destructive recovery operation, not a read-only probe.

## 10. Backplane broadcast and selection status

- KEEP: minimal broadcast rejection in `TrackBackplane2.X/modbus/PetitModbus.c` (dispatch accepts only `PETITMODBUS_SLAVE_ADDRESS`; broadcast FC06/FC16 cannot write HR0 or drive `AMP_IDx_SET`).
- Preserved unchanged: `Set_Amplifier` addressed HR0 → active-low `AMP_IDx_SET_LAT` selection (default + Proto_Backplane).
- Backplane deployment requires the correct programming/rollback method, separately authorized (offline build does not prove the running backplane is updated).

## 11. C# startup / configuration-verification status

- KEEP: `FirmwareContract` (fail-closed identity/version), `AmplifierPhase1Safety` + ProtocolVersion gate, 23-register map, PR #48 `DeploymentAuthorization` fail-closed, `FlashOneAmplifierCommand` (131/132 frozen).
- REVERT: the config-verification workarounds — HR15→value+HR8 replacement, `Timeout 2s→20s`, per-register 3×750 ms retry, HR22 read + `Compatible` freshness coupling + `2s→5s`, `Phase1Failure` clearing, flaky-write test. (The legacy config path verifies via HR15; the HR15-overwrite-by-lease-renewal interaction must be re-derived with fresh, transaction-specific proof rather than masked with retries/waits/cached flags.)
- No normal-startup implicit flashing; version mismatch is NOT flash authorization.

## 12. Per-component KEEP/REVERT matrix (summary)

| Component | KEEP | REVERT |
|---|---|---|
| PIC18 | versioning, safety core, 23-reg contract, identity gate, measurements, tests | 9-bit TX (`0xCD1B`) |
| PIC32 | version, global-stop/heartbeat, identity gate, guard `346d962`, 131/132 (frozen), readiness/bounds, RX-timeout restart | case-4 retry `06b54be`, 63 MHz clock, ISR diagnostics |
| Backplane | broadcast rejection + addressed HR0 selection | — |
| C# | FirmwareContract, Phase1Safety+protocol gate, 23-reg map, PR #48, 131/132 (frozen) | HR15→value+HR8, 20s/5s/retry workarounds, freshness coupling, Phase1Failure clearing, hmo2024 tooling |

## 13. Source commits and full artifact hashes

- Stabilization candidate: `recovery/phase1-stabilization` @ `7afe86f` = `346d962` (guard) + `7afe86f` (backplane broadcast rejection).
- Built firmware (offline):
  - PIC18 Offset `0xE906` — SHA-256 `4F9ED4ECD6B925066D67A9F465022FA5F8DF3716F040F209B58190FE1F305A2A`.
  - PIC32 Production (guard fix) — SHA-256 `A8C67978BD00DE0354C09164FFBDB6676F9DA721BE06335CC04B647E38BB8F88`.
  - Backplane default — SHA-256 `4C4CA0D795B03DBFA685CF15E3535432C1250C2AA8B8FD58BDF838B7E39DB051`.
  - Backplane Proto_Backplane — SHA-256 `F5F9A4E689A070D9282C38634621B595FAD7D4C8265F9F7AD72C041DC1DE80EE`.
- Immutable Baseline-0: PIC18 `B848EF04…`/`0x251F`, PIC32 `A90E4495…` (unchanged).

## 14. Tests actually executed (offline)

- Backplane broadcast-rejection regression (`test_broadcast_rejection.py`): PASS.
- C# `SiebwaldeApp.sln -c Release`: 0 errors; `SiebwaldeApp.Core.Tests` 1124 passed / 0 failed / 0 skipped.
- Firmware builds (PIC18/PIC32/backplane): exit 0, checksums verified.

## 15. Independent Integrator decision

The offline candidate build/test is PASS; the Architect classification is DESIGN_ACCEPTABLE. (A fresh Integrator re-review of the final `7afe86f` candidate is the next gate before any hardware step.)

## 16. Proposed first hardware validation round

Target: establish device state non-destructively, then verify the minimal candidate restores stable startup — NO locomotive motion.

1. Establish the current per-amplifier state (non-destructive readback; do NOT treat "undetected" as "bootloader").
2. Flash the PIC32 guard-fix image (`A8C67978…`) via ICD3 (approved target + artifact verification).
3. Flash the four PIC18 `0xE906` images (if any amp is in a recoverable state) via the qualified route, one at a time, only after each target is confirmed non-contending.
4. Run the config-download harness and confirm all four amps reach `HR_STATE` InitDone+ConfigOk with no framing error.
5. Backplane broadcast rejection is an offline-verified change; its hardware deployment needs a separately qualified backplane programming method.

## 17. Exact physical intervention required

- PO power-cycle / isolate the amplifier backplane (all amps are currently unresponsive to normal 9-bit DETECT after the interrupted 131 re-init; this state is NOT proven to be bootloader mode).
- PO probe/wiring placement if wire-level framing evidence is required (only after the clock/framing/ISR experiments are reverted and the baseline behavior is re-established).
- PO physical supervision for any later locomotive test.

## 18. Full legacy rollback plan (prepared, not executed)

Target = the historically working full stack (identified as LEGACY MODE, not Phase-1):

1. PIC32: ICD3-program the archived `A90E4495…` Baseline-0 HEX.
2. PIC18: program the archived `B848EF04…`/`0x251F` HEX (per amp, via the qualified bootloader route after single-target isolation).
3. Backplane: identify the appropriate historically working `default`/`Proto_Backplane` image + mapping.
4. C#: the compatible historical runtime (no Phase-1 protocol gate; no implicit flashing).
5. Per-device order; qualified individual recovery if multiple bootloaders could respond; final readback + safe startup verification.
6. Record any missing Phase-1 protection capability before any locomotive is allowed to run.

Executing the fallback requires a separate explicit PO authorization.

## 19. Conditions requiring immediate fallback

- Newly observed material safety hazard, unknown target state, or unreliable recovery path.
- Two controlled hardware iterations fail to achieve demonstrable progress toward stable communication/initialization.
- Any flash/recovery path that cannot establish a single, non-contending, validated target.

## 20. Remaining unresolved risks

- Hardware causation of the config-APPLY/framing failure (the experimental clock/9-bit-TX/ISR changes were reverted; the baseline behavior must be re-observed before any further timing/framing conclusion).
- Exact PIC32 `A90E44` binary→source provenance (G).
- `Amplifier_ServiceTick` worst-case execution time (modulo-256 backlog counter) — unproven.
- R1/R2 (deferrable) and R3/R5 (recovery-path blockers, must resolve before 131/132).
- Whether the master ever emits a broadcast reaching a writable backplane HR0 path (the broadcast rejection is defensive; hardware deployment unverified).
