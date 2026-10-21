# LEGACY-L2-DETAILED-DESIGN — minimal PIC18 identity/version reporting

Status: DESIGN ONLY (NO implementation, NO flash). Built on the L1 checkpoint (`feature/legacy-l1-version-id` @ `3957784`, PIC32 0.1.0).

## 1. Objective

Add semantic firmware-version reporting to the legacy PIC18, while retaining the existing
working 9-bit Modbus / 8-bit bootloader communication architecture. No Phase-1 safety/protocol
capability is introduced; the version metadata must not mislead any consumer.

## 2. Legacy register map (HR0–HR11, 12 registers)

Source `TrackAmplifier4.X/modbus/General.h` @ `928ea7c` (`NUMBER_OF_HOLDING_PETITREGISTERS = 12`).

| HR | Meaning | Used? |
|---|---|---|
| 0 | PWM command | used |
| 1 | BEMF control | used |
| 2 | status + config-ID bits 0–5 | used |
| 3 | amp status | **free (reserved, never written)** |
| 4 | fuse voltage | used |
| 5 | H-bridge temperature | used |
| 6 | H-bridge current | used |
| 7 | messages-received counter | used (NOT free) |
| 8 | messages-sent counter | used |
| 9 | config-ID/PWM (documented) | **free (dead in firmware)** |
| 10 | accel params | **free (reserved, never written)** |
| 11 | SW checksum (flash-check) | used |

**Free registers: HR3, HR9, HR10.** HR7 is NOT free (it is the message counter).

## 3. Phase-1 HR7 / HR21 / HR22 (reference only)

Phase-1 1.0.0 (`recovery/phase1-stabilization`): `HR7 = 0xF101` (identity),
`HR21 = 0x0100` (major.minor = 1.0), `HR22 = 0x0001` (patch 0 / protocol 0x01), in a
23-register map. This is reference material ONLY — not to be cherry-picked wholesale.

## 4. Minimal-delta options

### Option A — repurpose a free register (RECOMMENDED, no map extension)

- Place identity in `HR3` (free) — e.g. `0xF101` (or a legacy-specific identity).
- If a full semantic version is needed, use `HR9` (major.minor) + `HR10` (patch/protocol).
- Delta: `General.h` (re-name/re-purpose the reserved HRs) + one populate line in `main.c`
  (mirroring the `HR_SW_CHECKSUM` assignment at `main.c:37`). `NUMBER_OF_HOLDING_PETITREGISTERS`
  stays 12; buffers unchanged.
- **Compatibility:** the legacy PIC32 already polls all of HR0–HR11 and forwards all 12 in the
  `SLAVE_INFO` frame (`slavehandler.c:219-268`, `mbus.c:412`, `SLAVE_INFO.HoldingReg[12]`); the
  legacy C# parses 12 registers (`TrackCommClientAsync.cs`). So identity/version in
  HR3/HR9/HR10 is visible to the legacy C# with **zero PIC32/C# change**.

### Option B — extend the map to 23 (NOT recommended)

- `NUMBER_OF_HOLDING_PETITREGISTERS` 12→23 auto-sizes buffers and bounds checks.
- BUT the legacy PIC32 never reads beyond HR11 and the legacy C# parses only 12, so HR12–HR22
  would be invisible without also changing the PIC32 `SLAVE_INFO` struct + C# parse (multi-component,
  violating "keep the working legacy architecture").

**Recommendation: Option A.** It is the smallest compatible delta, keeps the 12-register wire map,
and requires no PIC32/C# change. Caveat: the identity/version live in HR3/HR9/HR10, NOT HR7/HR21/HR22 —
semantically different from Phase-1 and needing mapping in any future Phase-1 convergence.

## 5. Compatibility + checksum impact

- Adding identity/version changes the application bytes → the 16-bit word-sum changes → HR11 ≠ `0x251F`
  (a real checksum mismatch). This is expected: the legacy `FlashAllSlavesAuto` would then see the L2
  amp as "requires flashing."
- No protected-area change: identity/version is application-space (below 0x800). UART/baud/9-bit
  address-detect, interrupts, the static bootloader, and the PIC32 FlashSequencer are untouched.

## 6. Single-amplifier L2 hardware test (within the original architecture)

Because L2 changes the checksum, ALL four amps (1/3/4/6) become eligible (`HR11 != new-checksum`);
the C# auto-flash `FlashFwTrackamplifiersStep` has NO single-slave path and would flash all four via
`EXEC_FW_STATE_FLASH_ALL_SLAVES` (122). **Do NOT use the C# auto-flash.**

Single-amp test via the ORIGINAL manual primitives (legacy `enums.h:160-168`, `slavefwhandler.c:1147+`):

1. `EXEC_FW_STATE_RECEIVE_FW_FILE` (120) + FW rows + checksum
2. `EXEC_FW_STATE_RECEIVE_CONFIG_WORD` (121)
3. `EXEC_FW_STATE_SELECT_SLAVE` (123, data = one target SlaveId) → selects exactly one amp via
   backplane slot + writes `HOLDINGREG11 = 0` to force it into bootloader
4. `EXEC_FW_STATE_ERASE_FLASH` (125) → `WRITE_FLASH` (126) → `WRITE_CONFIG` (127) →
   `CHECK_CHECKSUM` (128) → `SLAVE_RESET` (129)
5. `EXIT_SLAVE_FW_HANDLER` (130) → restore 9-bit Modbus

This is exactly what `FlashSequencer()` does internally, but manually scoped to one `SELECT_SLAVE`
target. The other three amps stay in 9-bit application mode and ignore the 8-bit bootloader traffic.
Fleet-wide programming is prevented by **not issuing command 122**. (Commands 131/132 are Phase-1
additions, NOT in the legacy PIC32.)

## 7. Protected architecture (NO changes)

PIC32/PIC18 UART config, peripheral clocks, baud, 9-bit address-marked communication, legacy PIC18
reply framing, 8-bit bootloader protocol, UART mode-switching, backplane select/deselect,
FlashSequencer, Modbus transaction processing, interrupt config/dispatch, C# legacy startup — all frozen.

## 8. Open PO decisions (before implementation)

1. Register choice: identity-only in HR3, or identity (HR3) + semantic version (HR9/HR10)?
2. Identity/version word encoding (Phase-1-style `0xF101` vs a legacy-specific value; protocol tag `0x00`).

## 9. Offline tests + rollback

- Offline: PIC18 build (XC8 v2.31) + a register-map unit test asserting the new HRs return the
  identity/version and all existing HRs are unchanged; assert no protected file is modified.
- Physical (later, PO-authorized): the single-amp sequence above; verify the target reports the new
  identity/version + HR11 = new checksum; the other three amps remain `0x251F`.
- Rollback: re-program the legacy `0x251F` Offset/Combined via the same single-amp manual path, or
  re-program Baseline-0 PIC32 `A90E4495…` if the PIC32 side is involved.
