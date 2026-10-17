# PIC32 Phase-1 software verification

Run from the repository root:

```powershell
python TrackController5/firmware/src/tests/test_global_stop.py
powershell -ExecutionPolicy Bypass -File tools/firmware/build-firmware.ps1 -Project TrackController5 -Configuration Production
```

The build harness requires a clean tracked tree for a successful overall exit.
Toolchain: XC32 2.50. Build output and artifact hashes are recorded by that
harness outside the source tree. No command above flashes or connects hardware.

## Implementation and host integration

- `slavestartup.c::ConfigureSlave` selects the slot and executes
  `VERIFYxAMPLIFIERxIDENTITY` before the HR2 address write. Two separate successful
  FC03 HR7 readbacks must agree, with version 1, class 1 and nonzero capabilities.
  Failure skips address assignment; enable is unicast to compatible amplifiers.
  `ISxAMPLIFIERxCOMPATIBLE` is the master-owned result, not a cached identity guess.
- Identity notifications use the existing task-status envelope:
  sender MBUS (20), command `EXEC_MBUS_STATE_SLAVE_INIT` (104), state BUSY (5),
  message `0x80 | amplifier_id` means two-read compatible. State ERROR (9),
  message `amplifier_id` means blocked (also emitted on address-assignment failure).
  The existing overall init DONE/NONE notification is unchanged. Per Amendment
  A2 these notifications are supplementary diagnostics, not a required identity
  wire extension: C# checks HR7 and HR_STATE from SLAVEINFO independently. Only
  the PIC32 fresh-read result authorizes the master's per-amplifier ENABLE.
- `slavehandler.c::SendNextMessage` schedules the direct address-0 HR1 `0x4000`
  broadcast when the 100 ms deadline is due and transport is ready, ahead of the
  mailbox. While latched, no renewal or host amplifier write is dispatched.
  Cyclic FC03 polling resumes after stop delivery finishes. HR4..HR22 rotates in
  ten SIP cases; HR22 is a single-register request. SLAVEINFO grows through
  `NUMBER_OF_HOLDING_PETITREGISTERS`, without an added struct field.
- `SERVICExGLOBALxSTOP` owns stop transactions and their `MbCommError` results.
  There are at most three stop-write attempts per amplifier. ACK is not completion:
  each confirmation uses fresh separate FC03 reads of HR8 and HR14, requires
  `INHIBITED && !STOPPING_ACTIVE`, and masks HR14 to its 10-bit applied duty before
  comparing to 399. Applied-readback capability is required. Each attempt has a
  1000 ms software confirmation deadline; transient ramp progress is re-read.
  Unknown/incompatible devices fail without probe-writing them. Fan-out is not
  the bounded lease safety mechanism.
- Delivery reports extend `GLOBAL_STOP_STATE` (111) using Amendment A2's packed
  result array (layout below), replacing the earlier high-bit message encoding.
  A complete snapshot is emitted when latching, after each terminal outcome, and
  on command-111 queries while latched. Confirmed means firmware-register evidence,
  not physical neutral. No new command number is introduced.
- Command 112 changes LATCHED to ARMED, allowing renewal again. It starts a bounded
  heartbeat grace interval; the next heartbeat renews authority. Missing that
  heartbeat still times out after 2 s. Repeated stop commands remain one-shot
  within each latch epoch. Release authority/physical-input checks belong to C#;
  command 112 itself neither clears PIC18 faults nor restores PWM.

## A2 command-111 byte layout

The existing UDP envelope is unchanged: `udpTrans_t` is 82 bytes (header, sender,
80 data bytes), sent by `ethernet.c::ETHERNET_Tasks`. All unused bytes/bits are zero.

| UDP byte | Struct field | Value |
| --- | --- | --- |
| 0 | `header` | `0xAA` |
| 1 | `command` (status sender) | CONTROLLER = 10 |
| 2 | `data[0]` | GLOBAL_STOP_STATE = 111 |
| 3 | `data[1]` | DONE = 7 |
| 4 | `data[2]` | Existing latch state: 1 host-loss, 2 host-initiated |
| 5 | `data[3]` | Detected-amplifier count N, 0..50 |
| 6..18 | `data[4..16]` | Up to 13 packed result bytes; only ceil(N/4) used |
| remaining | remaining `data[]` | Zero |

The detected list is captured at the latch edge, ascending amplifier ID 1..50;
master 0, backplanes 51..55, and undetected slots are excluded. Entry index i
(zero-based) occupies bits `2*(i%4)..2*(i%4)+1` of `data[4+i/4]`. Thus the first
entry uses the least-significant two bits, then the next higher pair, and so on
(little-endian packing). Codes: **0 confirmed, 1 pending, 2 failed, 3 reserved**.
Queue/write-wait/ACK/readback-in-progress states all encode pending. Incompatible
detected amplifiers encode failed without a probe write. Unused final pairs are
zero and MUST NOT be decoded as entries; N determines the array's extent.

Example: detected IDs `[1,3,7,20,50]` with results
`[confirmed,pending,failed,confirmed,failed]` produce `data[3..5] = 05 24 02`.
Fifty pending entries produce twelve `55` bytes plus `05`; fifty failed entries
produce twelve `AA` bytes plus `0A`. The list remains stable during that latch
epoch even if detection flags change. C# must use the same detected-list snapshot
and treat count/list mismatch or reserved code 3 as non-confirmed, not safe exit.
Disarmed/armed reports retain the existing message 0/3 and zero payload; they are
not latched stop-confirmation evidence. N=0 reports no amplifier evidence.

## Bootloader-entry preservation

`slavefwhandler.c::FlashSequencer`, case 6, remains unchanged from base `6436c95`:
it writes HR11=0 at `SLAVE_INITIAL_ADDR` (0xAA) before bootloader handling. The
runtime compatibility guard in `SLAVExCOMMUNICATIONxHANDLER` applies only to
addresses 1..50, so this existing pre-runtime write still reaches FC16 submission.
The harness executes that write with no compatible runtime amplifiers and checks
the submitted address, function, register and zero word. This is transport-path
evidence only, not execution of a real bootloader or a flash operation.

## Executed-test boundary

`test_global_stop.py` extracts unchanged production functions and compiles them
with MSVC `/W4 /WX`; `phase1_cases.h` supplies cases. Timer/UART/ACK delivery is
mocked, but transaction submission, FC03 parsing, compatibility, configure/enable,
poll traversal, latch/watchdog, lease scheduling, stop retry and confirmation
decisions execute production C. Cases cover legacy counter `0x1101 -> 0x1102`,
cached identity rejection, read timeouts, full 23-register traversal, full-mailbox
lease priority, 50 amplifiers with three timeouts each, malformed FC03, confirmation
deadline, unsupported applied capability, EMO, heartbeat rollover and re-arm. A2
cases cover sparse/empty/full detected lists, every result code, final-byte padding,
zero-filled tail, full snapshot queries and suppression after re-arm.

Two supporting transport fixes are required by these paths:
`SENDxPETITxMODBUS` uses the outgoing address (not the previous RX address) when
incrementing sent counters, preventing an out-of-bounds master-array write during
initial-address identity reads. Invalid FC03 byte counts/ranges terminate NOK
rather than releasing the transmitter while leaving communication BUSY forever.

AC6 is proven only for the PIC32 identity/enable gate. AC5/AC8 are proven only for
the PIC32 heartbeat/latch/renewal/re-arm and delivery decisions. PIC18 watchdog,
lease expiry, ramp timing, real UART timing, complete host integration, and physical
neutral remain unverified here. R2 independent review and V3 combined integration
are still required. No flash, hardware work, physical timing claim or self-acceptance.
