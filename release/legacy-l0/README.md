# Legacy L0 — Combined PIC18 Firmware Image (bootloader + application)

This is the supplemental L0 archival artifact. It is separate from the immutable
historical `release/baseline-0/` (which is NOT modified by this addition).

## Artifact

- **Source path:** `TrackAmplifier4.X/dist/Combined/production/TrackAmplifier4.X.production.unified.hex`
- **Destination path:** `release/legacy-l0/PIC18-TrackAmplifier-legacy-Combined-with-bootloader.hex`
- **Type:** Combined (bootloader + application) Intel HEX, PIC18F25K40, XC8 v2.31
- **Byte length:** 92,127
- **SHA-256:** `66404C8BBFAE24ADCBB15295283840FB09F160B628238CAA06461D56542FFC9B`

## Resolved address-region map

| Region | Range | Meaning |
|---|---|---|
| Reset vector | `0x000000 – 0x000003` | PIC18 `GOTO` (`0xEF0E,0xF000`) |
| High-priority IRQ vector | `0x000008 – 0x00000B` | PIC18 `GOTO` (`0xEF04,0xF004`) |
| Bootloader code | `0x000018 – 0x0007CB` | bootloader body (boot block `0x000000–0x0007FF` write-protected, `WRTB=ON`) |
| Application | `0x000800 – 0x007FFF` | legacy `0x251F` application |
| User-ID (IDLOC) | `0x200000 – 0x20000F` | all `0x0FFF` (blank) |
| Configuration words | `0x300000 – 0x30000B` | `0xFF8C,0xFFFF,0xFF9F,0xFDFF,0xFFFF,0xFFFF` |

## Validation (offline, no hardware)

- Intel HEX: 2,051 records (2048×`00`, 1×`01`, 2×`04`), all checksums valid, EOF present, no malformed/unknown records.
- Application checksum: 16-bit little-endian word-sum over `0x0800..0x7FFC` = **`0x251F`** == stored trailer `0x7FFE=0x1F`/`0x7FFF=0x25`.
- Application region byte-identical to `release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex` (SHA `B848EF04…`) — 0 differences.
- Bootloader region byte-identical to `TrackAmplifierBootLoader.X/dist/No_Configurations/production/TrackAmplifierBootLoader.X.production.hex` (SHA `161AE321…`) — 0 differences. Bootloader byte-provenance confirmed.

## Critical distinction (Offset vs Combined)

- **Offset HEX** (`release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex`): application-only image, used for normal application updates through the PIC32-to-PIC18 bootloader download path.
- **Combined HEX** (this file): bootloader + application, for direct/manual PIC18 programming (e.g. ICD/PICkit).

The Combined HEX must never replace the Offset HEX in the ordinary Siebwalde firmware
deployment path. Do NOT use this Combined image with the standard PIC32 Offset-download
procedure.

## Note on the originally-specified file

The PO originally pointed at
`TrackAmplifier4.X/dist/Combined/production/TrackAmplifier4.X.production.hex` as the
"Combined" image. Offline validation showed that file is the **application-only** build
(byte-identical to the Offset reference, no bootloader region). The true bootloader+app
Combined image is this `…production.unified.hex`. Per PO decision, the unified (true
Combined) image is archived here.

## Provenance

- Legacy firmware source revision: `928ea7cf6061737d26db56580c236347f0f746a8` (identical firmware component trees also in `e845b9d2e97ed92f00e0d9e3e031ed3a6eae2a7b`).
- Application image: legacy `0x251F` (the restored working PIC18 firmware).
- Bootloader: `TrackAmplifierBootLoader.X` (No_Configurations build).
