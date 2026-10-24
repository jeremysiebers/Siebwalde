# K5 — C# Initialization Reference and PICO-5B Bootloader-Transition Comparison

Date: 2026-10-23. This records the offline reconstruction of the original C#
track-amplifier initialization sequence, the automatic firmware-update risk,
and the interpretation of the new Product Owner observation (2026-10-23),
combined with the preserved PICO-5B evidence. **No live hardware was touched.**

## 1. Source-verified C# initialization sequence

`SiebwaldeApp.Core/Model/TrackApplication/Initialization/` (step order via each
step's `InitStepResult.Next(...)` / `Completed()`):

```
ConnectToEthernetTarget -> QueryControllerVersion -> ResetAllSlaves -> DataUpload
    -> DetectSlaves -> FlashFwTrackamplifiers -> InitTrackamplifiers
    -> SetDefaultPwmSetpoints -> EnableTrackamplifiers (Completed)
```

`DetectSlaves` branches to `RecoverSlaves` (recovery loop back to `DetectSlaves`)
on a detection fault, otherwise to `FlashFwTrackamplifiers`.

## 2. Automatic firmware-update risk (FlashFwTrackamplifiersStep)

`Steps/FlashFwTrackamplifiersStep.cs` is a **normal** initialization step that
can flash firmware automatically:

- `State0_DetermineFlashRequirementAsync` compares the HEX file checksum
  (`_bootloaderHelpers.GetFileCheckSum`) against each detected amplifier's
  `HoldingReg[SwChecksum]` (PIC18 HR11). A detected slave `1..50` whose HR11
  mismatches the file is counted as "requires flashing".
- `_fwFlashRequired == 0` → `"0 slaves require flashing, initialization DONE"` →
  `Next("InitTrackamplifiers")` (no flash).
- `_fwFlashRequired > 0` → `EXEC_MBUS_STATE_SLAVE_FW_FLASH` (103) → the fleet
  download (120/141/121/151) → `EXEC_FW_STATE_FLASH_ALL_SLAVES` (122).

Consequence: **if the PICO-5B attempt had erased a PIC18, its HR11 would read
0xFFFF and the next normal C# startup would automatically reflash it.**

## 3. The Product Owner observation (2026-10-23)

The PO reports: normal C# initialization was executed after the unsuccessful
PICO-5B experiment, and all installed amplifiers were observed functioning
afterward. The PO suspects the intended PIC18 reset/bootloader-entry sequence
did not reach/affect the amplifier.

This is recorded as a **PO-REPORTED PHYSICAL OBSERVATION** (not independent
register/capture evidence). It does not by itself prove the firmware was never
erased, modified or restored.

## 4. Distinguishing log evidence (23-10-2026 TrackAppLog)

`Logging/23-10-2026_TrackAppLog.txt` records **three** full initializations
(09:00:33, 13:59:25 and 15:37:09). Each contains:

```
State.DetectSlaves => Slave 1 detected ... Slave 3 / 4 / 6 / 51 detected
State.DetectSlaves => 5 slaves in total detected
State.DetectSlaves => enough slaves detected, proceed to FlashFwTrackamplifiers
State.FlashFwTrackamplifiers => File checksum = 0x251F
State.FlashFwTrackamplifiers => 0 slaves require flashing, initialization DONE
State.InitTrackamplifiers => EXEC_MBUS_STATE_SLAVE_INIT
```

Interpretation (source-supported, log-verified):

- The auto-flash decision step **ran** and reported **0 slaves require flashing**
  with file checksum `0x251F`, i.e. every detected amplifier's HR11 matched
  `0x251F` at that moment (the DETECT step had just refreshed SLAVEINFO).
- **No `EXEC_FW_STATE_FLASH_ALL_SLAVES` (122) was emitted** in either session.
- This is **consistent with the PICO-5B ERASE never executing** (amplifiers
  still `0x251F`) and **inconsistent with** an erased amplifier (whose HR11
  would read `0xFFFF`, forcing an auto-flash).

Caveat: HR11 here is the C#-reported SLAVEINFO readback (via PIC32 → Modbus),
not an independent PicoScope/register capture. It is fresh (DETECT just ran) but
not independently hardware-verified.

## 5. PICO-5B bootloader-transition comparison (7 layers)

Combining K3 (PICO-5B) conclusions with the new log evidence:

| # | Layer | Status |
|---|---|---|
| 1 | PIC32 transmitted an 8-bit bootloader command | PROVEN_HARDWARE (scope D1 capture: 9→8-bit + ERASE frame) |
| 2 | Backplane selected the intended amplifier | UNKNOWN (not independently observed) |
| 3 | PIC18 entered bootloader mode | UNKNOWN |
| 4 | PIC18 received the command | UNKNOWN |
| 5 | PIC18 transmitted the expected reply | UNKNOWN |
| 6 | PIC32 received/interpretted the reply | UNKNOWN |
| 7 | Host received a valid success indication | NOT OBSERVED (only `*_RECEIVE_DATA_TIMEOUT`) |

The **historically working C# path** (ResetAllSlaves → fleet 122) performs a
global reset before the flash, whereas the **experimental Python command-123
path** writes `HR11=0` to the boot address `0xAA` — which a running amplifier
drops (it listens on its assigned address). The new log evidence (0 slaves
requiring flash, all `0x251F`) strengthens the hypothesis that the Python 123
sequence never actually transitioned the PIC18 into bootloader and the ERASE
never executed.

## 6. Proven / hypothesized / unknown after K5

- **PROVEN_HARDWARE (PO-reported):** C# initialization ran 2026-10-23; amplifiers functioned.
- **PROVEN_SOURCE:** the C# init sequence + the automatic firmware-update (flash-on-mismatch) logic.
- **PROVEN_HARDWARE (runtime log):** both init sessions logged `File checksum = 0x251F` + `0 slaves require flashing` and emitted no 122.
- **INFERRED (strengthened):** the PICO-5B ERASE never executed on the PIC18; the intended reset/bootloader entry did not occur (still not an independent register capture).
- **UNKNOWN:** exact fresh HR11 readbacks independent of the C# SLAVEINFO path; whether the C# readback was fully fresh vs any cache; the root cause of the absent bootloader replies.

The application-integrity state of amplifiers 1 and 3 remains **UNKNOWN** in the
strictest sense until a fresh, independent, non-destructive readback is obtained
— but the C# startup evidence now makes "still `0x251F`, never erased" the
best-supported position rather than a bare hypothesis.
