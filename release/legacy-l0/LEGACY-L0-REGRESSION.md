# LEGACY-L0-REGRESSION — Baseline acceptance checklist + rollback

Repeatable acceptance checklist for future increments. Each increment re-runs these against
the previous checkpoint before and after its delta.

## Baseline acceptance checklist

1. PIC32 Ethernet connection (legacy CONNECT handshake ack; no 113/Phase-1 query).
2. All four amplifiers freshly detected (addresses 1, 3, 4, 6; `SlaveDetected=1`).
3. Expected firmware checksum per amplifier: fresh HR11 == `0x251F` (not a cached value).
4. Normal address assignment (no collisions; each amp at its intended address).
5. Normal configuration / INIT completion (INIT/ENABLE DONE acks; no per-slave INIT error).
6. Correct physical RUN/ERROR LED indications (PO physical observation).
7. Safe neutral output state (HR0 = 0 / no PWM; no motion command).
8. Normal communication without unexplained Modbus errors.
9. Original PIC18 Offset firmware upload succeeds (FW file download + config word).
10. Successful original bootloader flashing (GET_VERSION → ERASE → WRITE → CONFIG → CHECKSUM → RESET, all OK).
11. Return to normal 9-bit Modbus after flashing.
12. Re-detection and initialization after flashing.
13. Existing rollback binaries available (Baseline-0 Offset/PIC32 + legacy-l0 Combined).

⚠ The `FlashAllSlavesAuto` DONE ack does NOT prove per-amp success — always confirm via a
fresh per-amp HR11 readback.

## Rollback procedure

- PIC32: `ipecmd -P32MZ2048EFH144 -TPICD3 -F release/baseline-0/PIC32-Master-legacy-A90E44.hex -M` then `-OK -OL`.
- PIC18 (app only, via PIC32 bootloader): legacy Offset download + `FlashAllSlavesAuto` (122) with `release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex`.
- PIC18 (direct/manual): program `release/legacy-l0/PIC18-TrackAmplifier-legacy-Combined-with-bootloader.hex` (bootloader + app) with ICD/PICkit.

## Status of this L0 record

The 13 checks are recorded as the acceptance contract. They are NOT re-executed during L0
where the historical/PO evidence already documents them. Unperformed or unlogged items are
marked as such rather than claimed PASS.
