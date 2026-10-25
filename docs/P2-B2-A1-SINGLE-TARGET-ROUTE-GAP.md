# P2-B2-A1 — CRITICAL FINDING: single-target flash route absent from deployed firmware

**Status:** offline source/evidence investigation, 2026-10-25. No hardware was flashed.

## The finding (verified against source)

The Knowledge Foundation records `command:131` (`FLASH_ONE_SLAVE`) and
`command:132` (`FLASH_ONE_SLAVE_BOOTLOADER_ONLY`) as `PROVEN_HARDWARE`
(source revision `1f89c0b`). However:

1. **131/132 are NOT in the deployed PIC32 1.0.0 firmware** (`f9ae0ed`, the build
   P2-B1 physically deployed). `f9ae0ed` `enums.h` defines only 120-130:
   `122 FLASH_ALL_SLAVES`, `123 SELECT_SLAVE`, `124 GET_BOOTLOADER_VERSION`,
   `125 ERASE`, `126 WRITE`, `127 WRITE_CONFIG`, `128 CHECK`, `129 RESET`,
   `130 EXIT`. No 131/132.
2. **The commit that added 132** (`4850ce0e`) is **NOT an ancestor of master**
   (verified via `git merge-base --is-ancestor`). It lives only on unmerged
   branches: `feature/recover-slaves-count-independence`,
   `integration/phase1-firmware-combined`, `recovery/phase1-stabilization`.
3. The current `TrackController5/firmware/src/enums.h` on master (HEAD `1a5f0bf5`)
   also has only 120-130.

**Consequence:** the "proven" single-target route (131/132) is NOT available on the
physical controller. The only single-target path in the deployed firmware is the
manual primitive sequence 123 → 125 → 126 → 127 → 128 → 129 → 130, driven by
`TrackController5/Python/flash_one.py`.

## Why the manual 123 path failed in PICO-5B (and will fail again as-is)

The PIC18 bootloader-entry write is `SelectSlave`/`FlashSequencer` case 6: write
`HR11 = 0` to `SLAVE_INITIAL_ADDR` (0xAA). The PIC18 application only accepts this
write when it is at the unconfigured boot address 0xAA in Config phase:

```c
// TrackAmplifier4.X/main.c:33-36
bool Amplifier_IsBootloaderWrite(uint8_t frame_address, uint16_t addr, uint16_t value)
{
    return Config && Startup_Machine == 1u && MODBUS_ADDRESS == 0xAAu &&
        frame_address == 0xAAu && addr == HR_SW_CHECKSUM && value == 0u;
}
```

A **running** amp (address 1, assigned) drops the 0xAA frame, so bootloader entry
never happens. The technical reference §2.5 L110 states this exactly: "A running amp
answers on its ASSIGNED address (1), not the boot address 0xAA, so the HR11=0
bootloader-entry write is dropped … the flash fails at sub-step 7 'verify bootloader
entry'."

The only reset mechanism is the **global RD1 line** (`Slaves_Disable_On/Off`,
`system_config.h:514-516`), i.e. a global power-cycle of ALL amplifiers. The proven
131 command performed this global reset internally (107 RESET + 100 SLAVES_ON) before
the flash. The manual 123 path (`SelectSlave` case 0 = `Slaves_Disable_Off()` only)
does **not** perform a reset; it assumes the target is already at 0xAA.

## Verified facts (with evidence)

| Fact | Evidence |
| --- | --- |
| 131/132 not in deployed 1.0.0 | `git show f9ae0ed:.../enums.h` (120-130 only) |
| 132 commit unmerged | `4850ce0e` not ancestor of master |
| Bootloader entry requires 0xAA + Config | `TrackAmplifier4.X/main.c:33-44` |
| Reset is global RD1 only | `system_config.h:514-516`; tech ref §2.5 L110 |
| PICO-5B failure = no reset → no bootloader entry | `incident:pico-5b` + tech ref §2.5 L110 |
| flash_one.py emits 123 (no 107/100 reset before it) | `flash_one.py:658-660` + header lines 14-34 |
| 1.0.0 routes 103→FW handler; 120-130 consumed by SLAVExFWxHANDLER | `mbus.c:307-318`, `slavefwhandler.c:145-152` |
| Images verified: 1.0.0=0xE906 (4F9ED4EC), legacy=0x251F (B848EF04) | `bootload.py GetFileCheckSum`; knowledge graph |

## Implications for the execution plan

The deployed firmware has **no clean, proven single-target flash mechanism**:

- **131/132** = proven but unmerged (requires a PIC32 reflash to deploy — PO-forbidden
  in this directive).
- **123 + 125-129** (`flash_one.py`) = present but cannot enter the bootloader on a
  running amp without a preceding global reset; the global reset is a MBUS-level
  command (107/100) that the FW-handler state cannot cleanly survive (the downloaded
  image and FW-handler sequencer live in the FW handler, entered via 103).
- **Global reset** power-cycles ALL amps (3/4/6 included), which intersects the PO's
  hard "leave 3/4/6 untouched" boundary and must be explicitly acknowledged.
- **Physical power-cycle of amp 1 only** would avoid the global reset but requires an
  operator and a strictly-timed host sequence before the PIC32 re-assigns the address.

## Options (for PO decision)

- **A. Global-reset-based single-target flash** (107+100 before 123). Amps 3/4/6 are
  power-cycled (NOT flashed, NOT bootloader-entered) and re-initialized afterward.
  Requires: tooling change to flash_one.py or a supervised driver; rigorous isolation
  re-verification; possibly operator.
- **B. Physical power-cycle of amp 1 only** (operator), then the 123 path. Cleanest
  isolation; requires operator + timed host intervention.
- **C. Rebuild + reflash PIC32 with 131/132** — explicitly PO-forbidden in this
  directive; listed for completeness only.
- **D. Stop before ERASE/WRITE** (BLOCKED_PREFLIGHT).

No destructive programming has occurred. This finding is escalated to the PO before
any erase/write operation.
