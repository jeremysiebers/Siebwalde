# P2-B2-A1 — PIC18 Bootloader Contract, Tool Inventory & Image Verification (Phase A/B investigation)

**Status:** offline source/evidence investigation, 2026-10-25. No hardware was flashed.

## 1. PIC18 bootloader actual contract (source-verified)

The PIC18 bootloader (`TrackAmplifierBootLoader.X/mcc_generated_files/pic18f_bootload.c`)
is the Microchip MCC PIC18 bootloader for `PIC18F25K40`. Its command set:

| Cmd | Value | Name | Returns | Keys |
| --- | --- | --- | --- | --- |
| 0 | READ_VERSION | returns MAJOR=0x01 MINOR=0x00 + device ID + block sizes (25-byte payload) | — |
| 1 | READ_FLASH | data | — |
| 2 | WRITE_FLASH | `COMMAND_SUCCESS=0x01` | EE key 0x55/0xAA |
| 3 | ERASE_FLASH | `COMMAND_SUCCESS=0x01` | EE key 0x55/0xAA |
| 6/7 | READ/WRITE_CONFIG | data / success | 0x55/0xAA |
| 8 | CALC_CHECKSUM | checksumL/H | — |
| 9 | RESET_DEVICE | success + reset after ack | — |

**Boot entry condition** (`Bootload_Required`, pic18f_bootload.c:162-202): the bootloader
enters when EITHER (a) an IO entry pin is asserted, OR (b) the application checksum
mismatches. The checksum is computed over `0x800 .. END_FLASH-2` and compared to the
stored trailer at `END_FLASH-2` (the `0x7FFE/0x7FFF` contract). Application entry is
`goto 0x800` (re-mapped reset vector). **This is why "write HR11/checksum to 0" forces
bootloader entry on the next reset.**

**KEY PO clarification resolved:** the bootloader DOES implement READ_VERSION (returns
0x01.00). But the PIC32 forwarding path `EXEC_FW_STATE_GET_BOOTLOADER_VERSION` (124) is
historically non-functional (never produced a usable response even with a PIC18 genuinely
in bootloader mode) — this is a documented limitation (`flash_one.py` F4/§"no
bootloader-version probe"). The PO's "bootloader may not answer version query" therefore
refers to the PIC32 124 route, NOT a claim that the PIC18 bootloader lacks READ_VERSION.
Verification must NOT depend on a 124 version response.

## 2. PIC32 single-target selection mechanism (source-verified)

`EXEC_FW_STATE_SELECT_SLAVE` (123) → `SelectSlave(SlaveId)` (`slavefwhandler.c:1147-1324`):

1. Maps SlaveId 1 → backplane 51, slot bit 0 (`SLOT1 << 0`).
2. Case 2: writes `SLOT1 << ShiftSlot1` to backplane HOLDINGREG0 — **physically routes
   the RS-422 bus to ONLY slot 0 (amp 1)** via the backplane slot-select register.
3. Case 4: writes HR11=0 to `SLAVE_INITIAL_ADDR` (0xAA broadcast) — reaches **only the
   selected amp** (amps 3/4/6 are physically disconnected by the backplane slot-select),
   forcing its checksum to 0 → bootloader entry on next reset.
4. Case 6: releases the backplane slot (writes 0), so the amp won't re-enter bootloader
   after the reset.

**Isolation proof:** single-target selection is by **physical backplane slot routing**, not
a software address filter alone. Amps 3/4/6 are disconnected from the RS-422 bus before
the bootloader-entry write. No broadcast/global flash selection is active (that is
`EXEC_FW_STATE_FLASH_ALL_SLAVES`=122, which is NOT used).

## 3. Tool inventory (reuse before re-creating)

| Tool | Path | Capability | Verdict |
| --- | --- | --- | --- |
| **flash_one.py** | `TrackController5/Python/flash_one.py` | K3 fail-closed single-target PIC18 flash driver (123→125→126→127→128→129→130 + readback 100/106/101). Emits 123 SELECT_SLAVE, NEVER 122/131/132. Fail-closed: no ASSUME-OK, no retry, `--pause-before-erase` gate, `--dry-run`. | **REUSE (primary tool)** |
| bootload.py | `TrackController5/Python/bootload.py` | HEX parse + GetFileCheckSum + GetConfigData | REUSE (used by flash_one.py) |
| Comm.py | `TrackController5/Python/Comm.py` | legacy UDP transport | REUSE |
| Diagnostics MCP | `tools/diagnostics/` | baseline / pre-post comparison / readback | REUSE (no flash authority) |
| C# FlashFwTrackamplifiersStep | `SiebwaldeApp/.../Steps/FlashFwTrackamplifiersStep.cs` | **uses FLASH_ALL_SLAVES (122) — FLEET-WIDE** | **DO NOT USE** (fleet flash) |
| C# RecoverSlavesStep | `SiebwaldeApp/.../Steps/RecoverSlavesStep.cs` | single-slave bootloader recovery (probes 124 first) | KEEP for recovery; not the primary path |

**The C# `FlashFwTrackamplifiersStep` uses `EXEC_FW_STATE_FLASH_ALL_SLAVES` (122) = fleet-wide.
It MUST NOT be used for the amplifier-1 pilot.** The correct single-target tool is
`flash_one.py` (K3 fail-closed), which drives 123 (SELECT_SLAVE) + the per-slave 125-129
primitives.

## 4. Firmware image verification

| Image | Path | SHA-256 | Embedded checksum | Status |
| --- | --- | --- | --- | --- |
| Target 1.0.0 | `build/firmware/out/f9ae0ed/TrackAmplifier4/Offset/TrackAmplifier4.X.production.hex` | `4F9ED4ECD6B925066D67A9F465022FA5F8DF3716F040F209B58190FE1F305A2A` | **0xE906** (verified via bootload.py) | VERIFIED |
| Legacy 0x251F | `release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex` | `B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D` | 0x251F | VERIFIED |

Both match the knowledge graph (`firmware:pic18-0xE906`, `firmware:pic18-0x251F`).
The 1.0.0 config word is `8cffffff9ffffffdffffffff`. The 0x251F legacy image is the
rollback artifact (byte-reproducible from `source:928ea7c`).

## 5. HR7/HR21/HR22 semantics for the Phase-1 target

`TrackAmplifier4.X/version.h` and `enums.h` define the Phase-1 application register map
(23 registers): HR7 identity, HR11 checksum, HR21 fw-version (MAJOR<<8|MINOR),
HR22 patch/proto (PATCH<<8|PROTOCOL). After a successful flash the application should
report HR7 (Phase-1 identity, e.g. 0xF101), HR11=0xE906, HR21/HR22 with version/protocol
metadata — verified via the SLAVEINFO path (application), NOT via a bootloader version query.
