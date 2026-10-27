# P2-B2-A1 — PIC18 flash route: 0.1.0 vs 1.0.0 source comparison (PO directive)

**Status:** offline source comparison, 2026-10-25. No hardware flashed.

## PO directive (verbatim intent)

Compare the known-working PIC32 L1 0.1.0 (`39577845`) flash/bootloader-entry
implementation against the deployed 1.0.0 (`f9ae0ed9`), establish whether any flash
capability was actually removed/changed during migration (vs experimental
single-target 131/132 that was never merged), and establish whether the historical
proven update sequence can be safely adapted to select amplifier 1 exclusively.

## Result: flash capability was NOT removed or changed

| File (firmware) | 0.1.0 `39577845` vs 1.0.0 `f9ae0ed9` |
| --- | --- |
| `slavefwhandler.c` (122/123/125-130 dispatch + SelectSlave/FlashSequencer/FlashAllSlavesAuto) | **byte-identical (0 diff lines)** |
| `slavebootloaderroutines.c` (ERASE/WRITE/WRITE_CONFIG/CHECK/RESET + bootloader frame I/O) | **byte-identical (0 diff lines)** |
| `mbus.c` (MBUS state machine incl. RESET/SLAVES_ON/SLAVE_FW_FLASH) | 44 diff lines, ALL global-stop safety latch (ISxGLOBALxSTOPxLATCHED), nothing flash-related |
| `enums.h` EXEC_FW_STATE block (120-130) | **identical in both** |

**Both 0.1.0 and 1.0.0 contain exactly 120-130 (122 fleet, 123 SELECT_SLAVE,
124 GET_BOOTLOADER_VERSION, 125-129 primitives, 130 EXIT). Neither contains 131/132.**
The 131/132 "single-target flash" commands live only on unmerged branches
(`4850ce0e` not an ancestor of master). They were never part of any deployed firmware.

## Bootloader-entry mechanism (unchanged, source-verified)

`SelectSlave`/`FlashSequencer` case 6 writes `HR11 = 0` to `SLAVE_INITIAL_ADDR` (0xAA).
The PIC18 application accepts this write ONLY when it is freshly reset into the
unconfigured Config phase at address 0xAA:

```c
// TrackAmplifier4.X/main.c:33-36
bool Amplifier_IsBootloaderWrite(uint8_t frame_address, uint16_t addr, uint16_t value)
{
    return Config && Startup_Machine == 1u && MODBUS_ADDRESS == 0xAAu &&
        frame_address == 0xAAu && addr == HR_SW_CHECKSUM && value == 0u;
}
```

The only reset is the **global RD1 line** (`Slaves_Disable_On/Off`). The historical
proven fleet route (122) worked because the C# init ran `ResetAllSlavesStep`
(107 RESET → 100 SLAVES_ON) first — that global power-cycle boots ALL amps to 0xAA,
after which the backplane slot-select routes only the chosen slot before the HR11=0
bootloader-entry write (`slavefwhandler.c` SelectSlave case 2 → case 4).

C# init ordering confirms this (`TrackApplicationRuntimeHost.cs:467-474`):
`ResetAllSlavesStep` (107+100) → `DetectSlavesStep` → `RecoverSlavesStep` →
`FlashFwTrackamplifiersStep` (122) → `InitTrackamplifiersStep` → `EnableTrackamplifiersStep`.

## Isolation of amp 1 (unchanged mechanism, double-protected)

1. **Physical slot routing**: `SelectSlave` case 2 writes backplane `HOLDINGREG0 =
   SLOT1 << ShiftSlot1` (amp 1 → backplane 51, bit 0), which physically routes the
   RS-422 bus to ONLY slot 0 before any bootloader write.
2. **State gating**: the HR11=0 write is accepted only by an amp in
   `Startup_Machine == 1 && MODBUS_ADDRESS == 0xAA`. Amps 3/4/6, even if reset to
   0xAA, have their ID pins NOT pulled low (slot not selected), so they stay in
   `Startup_Machine == 0` and reject the bootloader-entry write.

## Current physical state (fresh non-destructive readback, 2026-10-25 ~20:00)

Amps 1/3/4/6: `detected=1`, `HR7=0x0000`, `HR11=0x251F`, `mbCommError=IDLE`.

This is the **expected Phase-1 compatibility-gate result** (legacy amps detected but
classified incompatible by the 1.0.0 HR7 identity gate and NOT enabled), NOT bootloader
mode. The observed red/error LED state is consistent with "detected-but-not-enabled",
not with bootloader operation. (The PO is correct that this must not be conflated with
bootloader mode.)

## Conclusion

1. **No flash capability was removed or changed during the 0.1.0 → 1.0.0 migration.**
   The flash source is byte-identical; only the 1.0.0 global-stop safety latch was
   added to `mbus.c`.
2. **131/132 were never in any deployed firmware** — they are experimental, unmerged,
   single-target functionality. The Knowledge Foundation `command:131/132
   PROVEN_HARDWARE` records were proven on an unmerged branch, not on the deployed
   1.0.0.
3. **The historical proven sequence is source-supported and adaptable to amp 1**: the
   fleet 122 route becomes the single-target route by substituting 123 (SELECT_SLAVE)
   for 122 (FLASH_ALL_SLAVES), keeping the SAME preceding global reset (107+100). The
   only difference: 107+100 power-cycles ALL amps (they boot at 0xAA), then the
   backplane slot-select + HR11=0 write reaches ONLY amp 1; amps 3/4/6 are
   re-initialized to neutral afterward (not flashed, not bootloader-entered).

## Recommended next action (for PO decision)

Proceed with the **source-supported single-target sequence** on the deployed firmware:

```
250 CONNECT → 107 RESET → 100 SLAVES_ON → 103 enter FW handler → 120+141xN download
→ 121+151 config → 123 SELECT_SLAVE(1) → 125 ERASE → 126 WRITE → 127 WRITE_CONFIG
→ 128 CHECK → 129 RESET → 130 EXIT → 100/106/101 re-init + readback
```

This requires: (a) a small, independently-tested tooling change (flash_one.py currently
does not emit the 107+100 global reset before 123 — the exact gap that caused PICO-5B);
(b) independent Integrator preflight on the exact sequence; (c) explicit PO
acknowledgment that 107+100 power-cycles amps 3/4/6 (they are re-initialized, not
flashed, not bootloader-entered). No ERASE/WRITE until PO approves.
