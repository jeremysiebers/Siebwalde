# K8B — Communication Recovery, Root Cause & Physical Readback

**Status:** physically validated 2026-10-25 (live, scoped K8B authority). This
documents the proven CONNECT/initialization sequence, the confirmed root cause
of the K8 command-250 timeout, and the fresh physical PIC18 register readback.

## 1. Proven CONNECT and initialization sequence (GATE 1..5 PASS)

Executable: `SiebwaldeApp.Koploper.Validation.Host` (`readonly-validate`),
Debug build, on branch `fix/k8b-legacy-12-register-slaveryinfo`.

Reference initialization order (source-mirrored from `ReadOnlyTelemetryValidationHost.EnableSessionAsync`
and the legacy `TrackIOHandle`, NOT an invented sequence):

| Gate | Command | Value | Verified response |
| --- | --- | --- | --- |
| CONNECT | `CLIENT_CONNECTION_REQUEST` | 250 (`0xFA`) | DONE ack `1/1` |
| init 2 | `EXEC_MBUS_STATE_SLAVES_ON` | 100 (`0x64`) | DONE ack `1/1` |
| init 3 | `EXEC_MBUS_STATE_SLAVE_DETECT` | 101 (`0x65`) | DONE ack `1/1` |
| init 4 | `EXEC_MBUS_STATE_SLAVE_INIT` | 104 (`0x68`) | DONE ack `1/1` |
| init 5 | `EXEC_MBUS_STATE_START_DATA_UPLOAD` | 106 (`0x6A`) | DONE ack `1/1` |
| init 6 | `EXEC_MBUS_STATE_SLAVE_ENABLE` | 105 (`0x69`) | DONE ack `1/1` |

Every command was transmitted and acknowledged `1/1` (0 timeouts, 0 rejections,
0 movement writes — registerWrite(108)=0, HR0/PWM/direction/brake/emo/switch/loco
all 0 by construction). Safe shutdown verified via session stop (the guard
stops on any ack timeout; no flash/reset path exists in this host).

## 2. Confirmed root cause of the K8 command-250 timeout

Two stacked causes, both now proven:

1. **Windows Firewall.** The canonical Release exe path carried an inbound
   `Block` rule (from an earlier unapproved dialog) that overrode the `Allow`
   rules; the Debug path and the worktree path were `Allow`. On the first run of
   a *new* executable identity the firewall defaults to block until approved.
   The timeout was therefore host-side, NOT a protocol mismatch.
2. **Decoder layout mismatch.** The deployed PIC32 firmware sends the **legacy
   12-register** `SLAVE_INFO` layout, but the C# decoder expected the Phase-1
   23-register layout. Every SLAVEINFO frame was rejected (footer/length
   mismatch), so the host reported `NO_AMPLIFIERS_DETECTED` despite a healthy,
   data-bearing stream.

## 3. Legacy 12-register SLAVEINFO wire layout (deployed firmware)

`udpTrans_t` = `{ header(1) command(1) data[80] }` → 82 bytes on the wire.
`SLAVE_INFO` (`PetitModbus.h`, `NUMBER_OF_HOLDING_PETITREGISTERS = 12`):

```
[0]  = 0xAA  udpTrans.header          [1] = 0xFF command (SLAVEINFO)
[2]  = 0xAA  SLAVE_INFO.Header         [3] = SlaveNumber
[4]  = SlaveDetected                   [5] = alignment padding
[6..29]  = HoldingReg[0..11]           (12 x uint16 LE)
[30..31] = MbReceiveCounter            [32..33] = MbSentCounter
[34..37] = MbCommError (uint32 LE)     [38] = MbExceptionCode
[39] = SpiCommErrorCounter             [40] = Footer 0x55
[41..81] = uninitialized trailing garbage
```

Phase-1 23-register (`TrackSlaveInfoFrame.Encode`, 63 bytes, simulator) keeps
footer 0x55 at offset 62. The decoder detects the layout by footer position
(40 → legacy, 62 → Phase-1) and maps the legacy 12 registers onto a 23-register
array with HR12..HR22 zeroed.

## 4. Fresh physical PIC18 register readback (2026-10-25)

| Amp | HR11 (checksum) | HR7 (raw, NOT identity) | HR21/HR22 | Status |
| --- | --- | --- | --- | --- |
| 1 | `0x251F` | `0x002D` | unsupported | PHYSICALLY_CONFIRMED (checksum) |
| 3 | `0x251F` | `0x002E` | unsupported | PHYSICALLY_CONFIRMED (checksum) |
| 4 | `0x251F` | `0x002E` | unsupported | PHYSICALLY_CONFIRMED (checksum) |
| 6 | `0x251F` | `0x002E` | unsupported | PHYSICALLY_CONFIRMED (checksum) |

- **HR11 = `0x251F`** is the firmware identity anchor and matches the historical
  `File checksum = 0x251F` reference (2026-10-23). Byte-stable across the
  pre-reset raw capture and the fresh readback, which independently proves the
  decode offsets.
- **HR21/HR22 do not exist** in the legacy 12-register firmware → `UNSUPPORTED`,
  never decoded as a zero-version.
- **HR7 is NOT an identity register in legacy firmware.** The legacy PIC18
  source (`TrackAmplifier4.X/main.c` at `e2164b43`) only references registers
  1/2/11 (control/address/checksum); the Phase-1 identity/version scheme
  (`HR_IDENTITY 7`, `HR_FW_VERSION 21/22`) arrived later. HR7 read `0x6649`
  pre-reset and `0x002D/0x002E` post-reset — a runtime-varying value, so the
  readback's `identity` field is a raw register dump for legacy amps, not a
  firmware identity.

## 5. Reset as an observed prerequisite (NOT a root cause)

The PIC32 was physically reset by the Product Owner before the successful cold
start. This is recorded as an **observed prerequisite** for obtaining a fresh
DETECT-latch (`SlaveDetected=1`) and clean initialization. It is NOT proof of
the original command-250 timeout cause; the firewall rule (host-side) and the
decoder layout mismatch (host-side) remain the evidence-backed causes. The
firewall, session-state and protocol hypotheses remain open only to the extent
documented in the K8 knowledge records.

## 6. Remaining items

- Diagnostics MCP active-operation extension (B1..B5): not yet implemented;
  the MCP is currently offline/read-only.
- Integrator independent review of the decoder fix and this physical readback.
