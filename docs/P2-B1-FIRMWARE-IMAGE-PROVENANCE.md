# P2-B1 — Firmware Image Provenance & Rollback-Image Investigation (Phase B/C)

**Status:** offline investigation, 2026-10-25. This documents the verified
identity and provenance of (a) the rollback image (deployed 0.1.0) and (b) the
target 1.0.0 image, before any programming. No hardware was touched.

## 1. Rollback image — RESOLVED (deployed 0.1.0 = L1, NOT Baseline-0 A90E44)

The deployed PIC32 firmware (read back by K6/K8/K8B command 113 as `0.1.0` /
protocol `0` / build `"2026-10-20"`) is the **L1 build**, not the Baseline-0
image. Two distinct legacy PIC32 images exist and must not be conflated:

| Image | Source | Command 113 | Version | Protocol | SHA-256 |
| --- | --- | --- | --- | --- | --- |
| Baseline-0 | `928ea7cf` | ABSENT | (none) | (none) | `A90E4495…` |
| **L1 (deployed)** | `39577845` (child of `928ea7cf`) | PRESENT | 0.1.0 | 0x00 | `EC7E3E6E…` |

Commit `39577845` (`feat(l1): add read-only command-113 semantic firmware-version
identification`, branch `feature/legacy-l1-version-id`) is a **3-file / 46-line
additive delta** over Baseline-0: `enums.h` (+113), `version.h` (0.1.0 / protocol
0x00 / `CTRL_RELEASE_TIMESTAMP "2026-10-20"`), and `controller.c`
(`REPLYxCONTROLLERxVERSION`). No clock/UART/framing/interrupt/Modbus/
FlashSequencer/bootloader change.

The L1 image is archived in Git at
`release/legacy-l1/PIC32-Master-L1-version-0.1.0.hex` (commit `d6b7aaa8`, blob
`3432c9b7`), with `LEGACY-L1-ACCEPTANCE.md` recording: ICD3 program/verify/release
PASS, command-113 reply `0.1.0/protocol 0x00` captured, fresh detection of amps
1/3/4/6 with HR11=0x251F, and PO-confirmed normal operation. Acceptance doc
records the deployed HEX SHA-256 `EC7E3E6E120C2A0A40DD4E13D16ABCBE62A76F8A14F22983C7DB6E399124D2A6`.

**Integrity verification (this session):** the committed Git blob `3432c9b7` has
LF line endings and hashes `D83C7FEA…`; after LF→CRLF normalization it hashes
**`EC7E3E6E120C2A0A40DD4E13D16ABCBE62A76F8A14F22983C7DB6E399124D2A6`** — an exact
match to the acceptance doc. The committed blob is the faithful rollback image;
the on-disk CRLF copy (as produced by MPLAB X on Windows) is byte-identical.

**Conclusion:** the rollback image is `release/legacy-l1/PIC32-Master-L1-version-0.1.0.hex`
(SHA-256 `EC7E3E6E…`). It is a **byte-identical backup** of the deployed firmware
(per the L1 acceptance programming/verify evidence), not merely a functionally
equivalent candidate. `A90E44` (Baseline-0) is NOT the correct rollback target
because it lacks command 113 and predates the deployed L1 firmware.

## 2. Target 1.0.0 image — RESOLVED

| Item | Value |
| --- | --- |
| Source | `f9ae0ed9` (`feat(firmware): semantic versioning + runtime identification`) — `version.h` 1.0.0 / protocol 0x01 / `"2026-10-17"` |
| Built HEX | `build/firmware/out/f9ae0ed/TrackController5/Production/TrackController5.X.production.hex` |
| SHA-256 | `6CEC61D4C28F514D34C22B4537D9D11A2F4ADEA9A7BDB5C2957E3E2674152101` |
| Device | `PIC32MZ2048EFH144` |
| Toolchain | MPLAB X v6.20, XC32 v2.50, DFP `PIC32MZ-EF_DFP 1.4.168`, Harmony |
| Knowledge node | `firmware:pic32-1.0.0` (source_hash `6CEC61D4…`) |

The build outputs `78d9a11` and `f9ae0ed` are byte-identical (`6CEC61D4…`),
confirming the 1.0.0 source tree builds deterministically. The on-disk artifact
matches the knowledge graph's recorded `firmware:pic32-1.0.0` hash.

## 3. Open compatibility risk (Phase D — next)

PIC32 1.0.0 sets `NUMBER_OF_HOLDING_PETITREGISTERS = 23` (Phase-1 SLAVEINFO),
while the deployed legacy PIC18 amplifiers implement the 12-register map. The
compatibility of a 23-register master against 12-register slaves is the primary
Phase D question and is investigated separately.

## 4. Correction to knowledge graph (pending Phase G)

`firmware:pic32-0.1.0` already records the deployed 0.1.0 (SHA `EC7E3E6E`). No
correction needed there. The device `device:amp-1` `RUNS_FIRMWARE` edge still
points at `0xE906` (superseded) despite K8B proving HR11=0x251F — this stale edge
should be corrected in the knowledge seed during Phase G.
