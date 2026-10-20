# LEGACY-L0-BASELINE — Authoritative Working Legacy Snapshot

Status: PO-confirmed fully operational (2026-10-20). This is the new proven development reference. Do NOT change this operating system.

## PIC32 Master

- Archived firmware: `release/baseline-0/PIC32-Master-legacy-A90E44.hex`
- Full SHA-256: `A90E44956A7CA15BFF7439D36FFA9FD0ADC81F8630D0B27A30E31932E3025584`
- Device: `PIC32MZ2048EFH144` (rev A3; ICD3)
- Historical source revision: `928ea7c` (TrackController5), also present in `e845b9d2`.
- Programming method: MPLAB ICD3 (`ipecmd -P32MZ2048EFH144 -TPICD3 -F… -M`).
- Provenance: archived HEX byte-matches the historical device readback over all 267,316 populated bytes (independent audit + Baseline-0 README §32).
- Proven restoration status: **restored + verified** (program/verify/release succeeded; legacy CONNECT handshake confirmed; command 113 correctly returns RECEIVED_UNKNOWN_COMMAND).

## PIC18 Amplifiers (addresses 1, 3, 4, 6)

- Legacy firmware application checksum: **`0x251F`**.
- Offset HEX: `release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex`, SHA-256 `B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D`.
- Combined HEX (bootloader + app): `release/legacy-l0/PIC18-TrackAmplifier-legacy-Combined-with-bootloader.hex`, SHA-256 `66404C8BBFAE24ADCBB15295283840FB09F160B628238CAA06461D56542FFC9B`.
- Offset vs Combined distinction: Offset = app-only (PIC32 bootloader download); Combined = bootloader+app (direct/manual programming).
- Regions: application `0x0800–0x7FFF`; bootloader `0x000000–0x0007CB`; user-ID `0x200000–0x20000F`; config `0x300000–0x30000B`.
- Historical source revision: `928ea7c` (TrackAmplifier4.X).
- Flash/rollback method: legacy `FlashAllSlavesAuto` (command 122) via the PIC32, or direct ICD/PICkit (Combined HEX). Rollback = the archived Offset/Combined HEX.

## Backplane

- Configuration: `Proto_Backplane` (and `default`).
- AMP_ID selection: active-low `AMP_IDx_SET` outputs, one-hot `HR0` slot-select.
- Address convention: backplanes 51–55; slots 1–10 per backplane (10 amplifiers/backplane).
- Original addressed HR0 semantics: HR0 is the slot-select latch, NOT the PWM setpoint (the track PWM neutral is a different register).
- Known broadcast behavior/limitations: the legacy backplane historically processed broadcast writes; the working stack uses the legacy behavior (any broadcast rejection change is a Phase-1 delta, NOT part of this L0 baseline).

## C# Application (legacy, PO-tested)

- Successful-test executable (built during L0): `SiebwaldeApp/SiebwaldeApp/bin/Release/net8.0-windows/SiebwaldeApp.exe` from the **KIS-10 closure** revision `80b7a41` (worktree `C:\Localdata\Siebwalde-legacy-oval`).
- Rationale: the Baseline-0 README's earlier `fe17ebe` correlation predates the `ovaaltje-loop2` topology profile (the prototype oval), which the PO's working layout uses; `80b7a41` is the last revision before the Phase-1 "global emergency stop" (estop, PR #40) and the 113 version query.
- Connection: `TrckIpAddress=192.168.1.193`, `TrckSendingPort=10000`, `TrckReceivingPort=10001` (UDP).
- Firmware/HEX selection: `TrackAmplifierFwPath = C:\Localdata\Siebwalde\TrackAmplifier4.X\dist\Offset\production\TrackAmplifier4.X.production.hex` (legacy `0x251F`).
- Original startup/flash sequence: CONNECT → RESET → SLAVES_ON → DataUpload → Detect → (Flash only if HR11 != file checksum) → Init → Enable.
- Known automatic deployment behavior: `FlashFwTrackamplifiersStep` skips flashing when the amplifier HR11 equals the file checksum (so a matched `0x251F` fleet is not re-flashed).
- Binary provenance: NOT separately SHA-verified against a source commit (rebuilt locally during L0 from `80b7a41`); the source revision `80b7a41` is recorded, not the exe binary hash.

## Hardware acceptance (PO-confirmed observations)

- Complete legacy stack operational: PO-confirmed.
- All four amplifiers restored to legacy `0x251F`: PO-confirmed + fresh HR11 readback evidence.
- Normal Siebwalde initialization successful: PO-confirmed.
- Original Offset modification/rebuild/flash route demonstrated working: PO-confirmed.
- Original 9-bit Modbus / 8-bit bootloader switching demonstrated operational: PO-confirmed.

Distinction: the above are PO/user-confirmed hardware observations. Independently captured logs exist for the PIC32 restore + detection + the controlled startup test (blocked at INIT 104); the physical green RUN LED, locomotive operation, and specific safety tests are NOT separately evidenced in this L0 record and are marked "not separately evidenced" where applicable.
