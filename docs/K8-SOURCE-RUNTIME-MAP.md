# K8 — Source-to-Runtime Map, Identity Model & Evidence Reconciliation

**Status:** offline source/evidence synthesis, 2026-10-25. This documents what is SOURCE-ONLY, HISTORICALLY_OBSERVED, and UNKNOWN. It is not a physical measurement of current state.

## 1. Source-to-runtime map (Phase A)

`PIC18 PetitHoldingRegisters[HR] → FC03 read (SIP4) → MASTER_SLAVE_DATA[addr].HoldingReg[reg] → SLAVEINFO frame (0xFF) → C# TrackAmplifierItem.HoldingReg[]`

| Register | PIC18 define (`TrackAmplifier4.X/modbus/General.h`) | C# (`TrackAmplifierRegisters.cs`) | Meaning | 1.0.0-only? |
| --- | --- | --- | --- | --- |
| HR7 | `HR_IDENTITY 7u` (line 18) | `Identity = 7` (line 9) | register-map/identity/capability | yes (legacy reads 0) |
| HR8 | state | `State = 8` | state | — |
| HR9 | fault | `Fault = 9` | fault | — |
| HR10 | validity | `Validity = 10` | validity | — |
| HR11 | `HR_SW_CHECKSUM 11u` (line 22) | `SwChecksum = 11` | application checksum | no (legacy + 1.0.0 both) |
| HR21 | `HR_FW_VERSION 21u` (line 32) | (1.0.0-only) | MAJOR<<8\|MINOR | yes |
| HR22 | `HR_FW_VERSION_PATCH_PROTO 22u` (line 33) | (1.0.0-only) | PATCH<<8\|PROTOCOL | yes |

The C# `FlashFwTrackamplifiersStep.State0_DetermineFlashRequirementAsync` (lines 135–161) compares `amplifier.HoldingReg[SwChecksum]` (HR11) against the HEX file checksum; a mismatch with `SlaveDetected==1 && 0<SlaveNumber<51` increments `_fwFlashRequired`. "0 slaves require flashing" therefore means **every detected amplifier's HR11 matched the file checksum**.

## 2. Historical evidence (Phase B)

The archived `23-10-2026_TrackAppLog.txt` records, at 09:00:37, 13:59:30 and 15:37:14:
- `File checksum = 0x251F` (the legacy Offset HEX checksum).
- `0 slaves require flashing, initialization DONE`.

**Interpretation (HISTORICALLY_OBSERVED, 2026-10-23):** every detected amplifier's HR11 read `0x251F` (legacy) at that time. The log does NOT list per-amplifier HR7/HR21/HR22 values, nor the set of detected slave numbers. Legacy `0x251F` firmware does not implement HR7/HR21/HR22 (they read 0 — 1.0.0-only).

## 3. Amplifier firmware-identity matrix (Phase D)

| Target | PIC18 bus address | HR11 (checksum) | HR7 | HR21/HR22 | Classification |
| --- | --- | --- | --- | --- | --- |
| Amplifier 1 | 1 | `0x251F` (hist., 2026-10-23) | 0/unsupported (legacy) | unsupported (legacy) | **HISTORICALLY_OBSERVED → current UNKNOWN** |
| Amplifier 3 | 3 | `0x251F` (hist.) | 0/unsupported | unsupported | **HISTORICALLY_OBSERVED → current UNKNOWN** |
| Amplifier 4 | 4 | `0x251F` (hist., §2.5) | 0/unsupported | unsupported | HISTORICALLY_OBSERVED |
| Amplifier 6 | 6 | `0x251F` (hist., §2.5) | 0/unsupported | unsupported | HISTORICALLY_OBSERVED |

**No fresh independent readback of HR11 has occurred since 2026-10-23.** A historical checksum is a build identity, not proof of current deployment.

## 4. SLAVEINFO observation constraints (re-affirmed)

SLAVEINFO (0xFF) is unicast to a single connected client, request-driven (command 106 START_DATA_UPLOAD), and the meaningful `SERVICE_TASKS` stream requires command 105 ENABLE (state-changing). There is no passive, state-free UDP listen. A second UDP listener cannot expect to receive traffic addressed to the existing C# client. Binding a socket is not proof of safe passive observation.

## 5. PIC32 provenance (Phase E)

PIC32 firmware 1.0.0 was physically deployed (flash ~2026-10-18 + command-113 confirmed ~2026-10-19); K6 read back 0.1.0/protocol 0/"2026-10-20" (2026-10-24). The 1.0.0 → 0.1.0 transition remains **UNKNOWN** (no captured re-flash evidence). PIC32 identity is separate from PIC18 identity.

## 6. Conclusion for passive K8

Within the passive-only authority (no command 105/106/113, no init, no enable, no reset), the current PIC18 HR7/HR11/HR21/HR22 **cannot be freshly observed**: the C# application is not running, and starting it (or issuing any SLAVEINFO-generating command) is state-changing and NOT authorized. The strongest available evidence is the HISTORICAL 2026-10-23 C# log (all-detected-amps HR11 = 0x251F).

The smallest safe next step to obtain a fresh physical readback is a **scoped active read grant** (see K8 report §G), NOT a passive capture.
