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
