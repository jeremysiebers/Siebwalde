# Baseline 0 — Legacy / Original Recovery Reference

**Designation:** immutable historical reference. **Do not** modify these HEX files or retroactively add semantic version 0.0.0.

## PIC18 TrackAmplifier (legacy)

- File: `release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex`
- SHA-256: `B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D`
- Source association: `928ea7c`
- Runtime checksum (HR11): `0x251F`
- Register map: 12 holding registers (HR0–HR11)
- Bootloader contract: application `0x0800–0x7FFF`, checksum `0x7FFE/0x7FFF`, 16-bit additive little-endian word sum.

## PIC32 Master (legacy)

- File: `release/baseline-0/PIC32-Master-legacy-A90E44.hex`
- SHA-256: `A90E44956A7CA15BFF7439D36FFA9FD0ADC81F8630D0B27A30E31932E3025584`
- Device: PIC32MZ2048EFH144
- Runtime identity: **UNKNOWN** (this HEX is a preserved candidate, not a verified copy of the running image).
- Programming route: MPLAB ICD3.

## C# host (legacy)

- Version: `1.0.0+fe17ebedd33f21717d69e03190d5e22a50d6b9e2` (KIS-09 docs commit; correlated, not binary-proven).

## Recovery qualification

- PIC18: standard Offset bootloader route (C# → PIC32 → RS-422 → PIC18 bootloader).
- PIC32: MPLAB ICD3 (ipecmd at `C:\Program Files\Microchip\MPLABX\v6.20\mplab_platform\mplab_ipe\ipecmd.exe`).
- **Note:** preserving this HEX is a necessary but not sufficient condition for a proven recovery capability — a preserved artifact and a demonstrated recovery are distinct acceptance requirements.

## PIC32 runtime-identity readback (2026-10-18) — QUALIFIED

- ICD3 qualified: `ipecmd -P32MZ2048EFH144 -TPICD3 -I` → target `PIC32MZ2048EFH144` found, Device ID Revision A3, DEVSN0=`ecda3e32`/DEVSN1=`5ac05410`, ICD3 firmware suite `01.56.10`, DFP `PIC32MZ-EF_DFP 1.4.168`.
- Non-destructive flash readback (`-GF`) exported the full image (program `0x1d000000..0x1d1fffff` + boot config + configuration memory) to `PIC32-readback-pfm.hex` (SHA-256 `04AE6A69…`, 6,819,297 bytes).
- **Comparison: 100% match** — the 267,316 bytes of `A90E44` are all present and byte-identical in the readback (0 differing). The readback additionally contains the unprogrammed (erased `0xFF`) flash and boot/config regions not represented in the `A90E44` HEX.
- **Conclusion:** the currently-running PIC32 firmware is byte-identical (over the populated region) to `A90E44`. The archived `A90E44` HEX is a verified recovery image, and the PIC32 recovery route (ICD3 read/program) is now qualified.
- Caveat: readback holds the target in reset; release with `-OK -OL` and allow ~15 s for Ethernet re-init.
