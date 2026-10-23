# Diagnostic PICO-5B checksum-test image (temporary, OFFLINE only)

## Purpose
Temporary PIC18 TrackAmplifier4 firmware image produced for the PICO-5B bootloader
experiment. Its checksum is intentionally DIFFERENT from the legacy `0x251F` so the
experiment has a distinct image identity. **No hardware was flashed, programmed,
connected or reset.** This is a build artifact only.

## Source change (the ONLY change)
`TrackAmplifier4.X/main.c` line 27, at commit `928ea7c`:

```c
//static uint8_t test = 0xFE;   ->   static uint8_t test = 0xFE;
```

No other source change. `test` is never referenced by any code (dead variable). The
source was restored immediately after the build; `main.c` is back to the commented form.

## Toolchain / configuration
- Source commit: `928ea7cf6061737d26db56580c236347f0f746a8`
- Build configuration: `Offset` (`-mcodeoffset=0x800`, checksum `800-7FFD@7FFE,width=-2,algorithm=2`)
- Compiler: XC8 v2.31 (`C:\Program Files\Microchip\xc8\v2.31`)
- MPLAB X: v6.20 (`C:\Program Files\Microchip\MPLABX\v6.20`), headless `prjMakefilesGenerator` + GNU `make -f nbproject/Makefile-Offset.mk SUB=no .build-conf`
- Device: PIC18F25K40, optimization `-O0`

## Checksum result
| | Value |
|---|---|
| Legacy baseline checksum (0x7FFE/0x7FFF) | `0x251F` |
| This image checksum (0x7FFE/0x7FFF)       | `0x2D00` |

The checksum **changed** (0x251F -> 0x2D00), verified by three independent methods
(linker-embedded word, record-based recompute over 0x800..0x7FFD, and the C# host
row-based algorithm) — all agree.

## Artifact
- File: `TrackAmplifier4.X.production.hex`
- Size: 86 529 bytes
- SHA-256: `ABAEF621C25E1AB046FB98834E7A5DB84D49C0FBEAF49AFEAF62DAAF082D9E53`

## Layout effect (non-minimal, functionally transparent)
The unused static `test` is emitted by XC8 -O0 as: (1) 1 byte of RAM at address 0x51,
(2) a 1-byte ROM initializer `0xFE` in the `idataCOMRAM` psect (data-init template).
This re-links the whole image: all code shifted +2 bytes (1 instruction word), RAM
variables `_Config`/`_PETITMODBUS_SLAVE_ADDRESS`/`_Petit_Rx_CRC16`/`_Petit_Tx_CRC16`
shifted +1 byte, and `_MODBUS_ADDRESS` spilled from COMRAM (0x57) into BANK0 (0xE5).
Program usage 6358 -> 6361 bytes; data 295 -> 296 bytes. `test` is dead (never read/
written), so control behaviour (PWM/Modbus/UART/safety/timing) is semantically
unchanged — the diff is a re-link, not a logic change.

## Baseline reference (NOT overwritten)
`release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex` (SHA-256
`B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D`) is untouched.
