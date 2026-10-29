# Ovaaltje2 — Controlled Initialization State Machine (design)

**Status:** design for implementation (PO directive "Ovaaltje2 — Controlled Initialization State Machine"). Software + firmware changes authorized for offline development; NO flash, NO live energization, NO merge without separate PO GO.

## 1. Problem

The normal startup (ENABLE 105 + CONFIG/APPLY + PWM 399) leaves every amplifier `inhibited` (latched) and the H-bridge de-energized (`PWM3EN = 0`), because the ONLY existing inhibit-clear is the non-neutral movement command (`amp_adapter.c`, `requested_pwm != AMP_NEUTRAL`). There is therefore no way to energize the H-bridges at neutral PWM for occupancy detection, and the init pipeline has no operator-visible stages, manual/automatic control, or human-readable log.

## 2. Existing architecture (reuse, do not replace)

- `IInitializationStep` + `InitStepResult` (`Continue/Next/Completed/Error`) — the step contract.
- `TrackAmplifierInitializationServiceAsync` — a linear step runner over a `Dictionary<string, IInitializationStep>`; emits `InitializationProgress`/`InitializationStatus`.
- `TrackApplicationRuntimeHost.ComposeTrackPartAsync` — composes the 11 steps.
- Steps: ConnectToEthernetTarget → QueryControllerVersion → GlobalStopPrecondition → ResetAllSlaves → DataUpload → DetectSlaves → RecoverSlaves → FlashFwTrackamplifiers → InitTrackamplifiers → SetDefaultPwmSetpoints → EnableTrackamplifiers.
- PIC18: `HR_COMMAND(0)`, `HR_CONTROL(1)` (ENABLE 0x8000 / LEASE 0x4000 / APPLY 0x2000 / CLEAR 0x1000 / RESET 0x0800 / DISABLE 0x0400), `HR_STATE(8)`, `HR_FAULT(9)`, `HR_WRITE_RESULT(15)`, `HR_FW_VERSION(21..22)`, `HR_SW_CHECKSUM(11)`. Inhibit set by `AmpCore_Inhibit()` (lease expiry / comm loss / EMO / STOP_REQUEST / DISABLE); cleared only in the HR_COMMAND write path when `renewal_after_inhibit && lease fresh && enable && config_ok && !fault && neutral && requested_pwm != NEUTRAL`. `AmpHw_Enable(config_ok && ((enable && !inhibited) || stopping_active) && !hard)`.

## 3. Seven stages (map onto existing steps; add Stage 6)

| Stage | Id | Content |
|---|---|---|
| 1 Controller connection | `ControllerConnection` | ConnectToEthernetTarget + QueryControllerVersion + GlobalStopPrecondition. No output activation. |
| 2 Firmware/address verification | `FirmwareAddress` | ResetAllSlaves + DataUpload + DetectSlaves + RecoverSlaves + FlashFwTrackamplifiers (flash only under explicit authorization-required state). |
| 3 Amplifier STANDBY | `AmplifierStandby` | Each amp: H-bridge inhibited, WAR LED slow blink, config download permitted, per-amp status visible. |
| 4 CONFIGURE and VALIDATE | `ConfigureValidate` | InitTrackamplifiers + config download + APPLY + verify INIT_DONE/CONFIG_OK + lease/comm/global-stop/fault/neutral checks per amp. |
| 5 GLOBAL READY | `GlobalReady` | All required amps READY (WAR off, RUN slow blink); H-bridge de-energized; no movement. Global readiness = ALL required amps valid. |
| 6 AMP_POWER_ENABLE | `AmpPowerEnable` | Explicit, gated transition READY → TRACK_POWER_ON: clear inhibit at neutral → H-bridge energized at PWM 399, occupancy available, no non-neutral PWM. Distinct from global-stop REARM and movement authority. |
| 7 NORMAL OPERATION | `NormalOperation` | Runtime loop; existing occupancy + movement-authority + safety logic. |

## 4. Firmware mechanism (minimal coordinated extension)

Add a dedicated, safety-gated "track power" request, distinct from ENABLE and movement:

- New register `HR_TRACK_POWER` (23), bit 0 = `TRACK_POWER_ENABLE` (0x0001). Bits 1..15 reserved (must be 0).
- **Wire-contract amendment (requires PO GO for deployment):** register 23 is additive to the frozen R8 map (`NUMBER_OF_HOLDING_PETITREGISTERS` 23→24) and is addressed only via the 108 EXEC_MBUS_SLAVE_DATA_EXCH point path (it is NOT in the 23-register SLAVEINFO poll, so the SLAVEINFO frame is unchanged). `AMP_PROTOCOL_VERSION` is bumped 0x01→0x02 so a track-power-capable firmware advertises 0x02; C# `SupportsTrackPower(hr22) == protocol >= 0x02` gates Stage 6 fail-closed (0x01 stays Phase-1-compatible for config but lacks HR_TRACK_POWER).
- New model flag `track_power_requested`.
- `AmpAdapter_OnWrite(HR_TRACK_POWER)`: set → `track_power_requested = true`; clear → `track_power_requested = false; AmpCore_Inhibit()`.
- `AmpCore_Update`, after the existing fault/inhibit logic, add a guarded energize: when `track_power_requested && command.enable && config_ok && !fault.causes && lease_fresh && ramp_current == AMP_NEUTRAL && applied_pwm == AMP_NEUTRAL` → `inhibited = false` (energize at neutral). Safety overrides (hard fault / EMO / STOP_REQUEST / lease expiry / comm loss / DISABLE) remain unchanged and still `AmpCore_Inhibit()` (they take precedence; track power does NOT bypass a hard fault).
- `AmpHw_Enable` is unchanged: once `inhibited == false` with `enable`, the existing `(enable && !inhibited)` term energizes the bridge at neutral.
- HR_STATE mirrors already expose `inhibited` (0x8000) and `enable_active` (0x0100), so C# can verify per-amp.

Do NOT remove `requested_pwm != AMP_NEUTRAL` from the movement inhibit-clear (that remains the movement-release path). The new track-power path is a separate, gated mechanism.

## 5. One state machine, two execution modes

- A `TrackInitStateMachine` owns the 7 stages and a per-stage status (Pending/Running/Passed/Failed/Blocked), preconditions and postconditions.
- **Automatic mode**: advance stage-by-stage, executing the same stage actions + verification, stopping (fail-closed) on any stage failure.
- **Manual mode**: operator steps to the next permitted stage, retries a failed stage, backtracks through the relevant hardware-safe transition (irreversible flash not auto-undone), aborts, and resumes automatic from the last valid checkpoint.
- Both modes call the SAME stage action + verification code (no parallel test/production paths).

## 6. WPF + operator log

- Extend `SiebwaldeInitPage`: a stage tree (id/name + Pending/Running/Passed/Failed/Blocked + per-amp status + failure reason + recovery action).
- Guarded controls: start automatic, step, retry, abort/stop, track-power enable/disable (where permitted), open amplifier info.
- A Dutch operator log panel translating state-machine events into human-readable messages (e.g. "Amplifier 3 — firmware geverifieerd.", "Veiligheidscontrole mislukt: amplifier 3 lease ongeldig.", "Alle amplifiers READY — trackspanning nog niet ingeschakeld."), correlated with timestamp + session id + stage id + amp address + raw technical log reference.

## 7. Safety invariants (must hold)

- No stage advances without validated prerequisites.
- A failed stage is explicit; previously-green stages are invalidated on comm-loss/lease-expiry/stop/fault.
- Global READY requires the whole required amp set (1/3/4/6); no single-amp false readiness.
- Neutral track-power enable requires explicit authorization + verified safety conditions; produces no non-neutral PWM.
- Stop / lease / fault protections retain precedence; track power never bypasses a hard safety fault.
- No automatic energization on startup/reconnect; no automatic global-stop clear without operator confirmation.
