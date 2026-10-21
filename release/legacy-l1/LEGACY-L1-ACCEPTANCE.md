# LEGACY-L1-ACCEPTANCE — L1 NORMAL OPERATION PASS

Date: 2026-10-21. This records the L1 checkpoint and the PO's physical hardware acceptance.

## L1 firmware identity

- Source branch: `feature/legacy-l1-version-id`
- Source commit: `39577845d1ecccbd0c1699af88d41ce43cd7dcdd` (base `928ea7c`)
- Firmware version: `0.1.0`
- Protocol tag: `0x00`
- Deployed PIC32 HEX SHA-256: `EC7E3E6E120C2A0A40DD4E13D16ABCBE62A76F8A14F22983C7DB6E399124D2A6`
- Change: 3 files / 46 insertions (`TrackController5/firmware/src/{controller.c,enums.h,version.h}`) — additive read-only `GET_CONTROLLER_VERSION` (113).

## Acceptance status (evidence classification)

| Item | Status | Evidence |
|---|---|---|
| PIC32 L1 programming + verify | PASS | ICD3 program/verify/release `Operation Succeeded` (Integrator) |
| Command 113 → `0.1.0` / protocol `0x00` | PASS | exact 82-byte reply captured (Integrator) |
| Fresh detection of amps 1/3/4/6 | PASS | `SlaveDetected=1`, `HR11=0x251F` (Integrator) |
| Normal Siebwalde initialization | PASS | PO physical confirmation |
| Functional Koploper integration | PASS | PO physical confirmation |
| Actual locomotive operation | PASS | PO physical confirmation (layout operational) |
| L1 bootloader (Offset flash) regression | **DEFERRED / NOT YET TESTED** | not exercised under L1 PIC32 |

**L1 NORMAL OPERATION — PASS** (PO-confirmed, plus independently captured CONNECT/113/detection diagnostics).

Distinction: items 4–6 are PO-reported physical observations (not independently captured logs). Items 1–3 are independently captured by the Integrator. No individual locomotive test measurement, LED observation, or safety measurement is invented here beyond what was explicitly reported.

## Bootloader regression — deferred

The L1 change did not touch the 9-bit/8-bit bootloader switching, the FlashSequencer, or the Offset download path. The legacy Offset flashing path was already exercised on the restored L0 stack. Per PO decision, no additional PIC18 flash was performed to close L1 (the checksum-selective `FlashAllSlavesAuto` would skip matching `0x251F` amplifiers, so an identical-image upload would not prove the flash path). The bootloader regression under the L1 PIC32 is **DEFERRED / NOT YET TESTED**, to be exercised as part of a separately approved L2 hardware increment. This does not invalidate the L1 normal-operation checkpoint.

## PIC18 amplifiers (unchanged)

- Addresses 1, 3, 4, 6 — legacy firmware checksum `0x251F` (fresh-detection verified).

## C# (unchanged)

- Legacy-compatible application, reference source `80b7a41`.

## Rollback

- Re-program Baseline-0 PIC32 `release/baseline-0/PIC32-Master-legacy-A90E44.hex` (SHA-256 `A90E44956A7CA15BFF7439D36FFA9FD0ADC81F8630D0B27A30E31932E3025584`) via ICD3.

## Known limitation (documented, accepted)

The PIC32 whole-image build is not byte-reproducible across time because of the inherited `__DATE__`/`__TIME__` log string in `controller.c:156` (present in the legacy base, not introduced by L1). The L1 `CTRL_RELEASE_TIMESTAMP "2026-10-20"` is deterministic. The deployed `EC7E3E6E…` binary is the authoritative hardware-tested image; it must not be replaced by a rebuild.
