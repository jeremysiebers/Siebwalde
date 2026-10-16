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
  The existing overall init DONE/NONE notification is unchanged. The host must
  consume this result rather than count repeated SLAVEINFO identity values.
- `slavehandler.c::SendNextMessage` schedules the direct address-0 HR1 `0x4000`
  broadcast when the 100 ms deadline is due and transport is ready, ahead of the
  mailbox. While latched, no renewal or host amplifier write is dispatched.
  Cyclic FC03 polling resumes after stop delivery finishes. HR4..HR20 rotates in
  nine SIP cases; HR20 is a single-register request. SLAVEINFO grows through
  `NUMBER_OF_HOLDING_PETITREGISTERS`, without an added struct field.
- `SERVICExGLOBALxSTOP` owns stop transactions and their `MbCommError` results.
  There are at most three stop-write attempts per amplifier. ACK is not completion:
  each confirmation uses fresh separate FC03 reads of HR8 and HR14, requires
  `INHIBITED && !STOPPING_ACTIVE`, and masks HR14 to its 10-bit applied duty before
  comparing to 399. Applied-readback capability is required. Each attempt has a
  1000 ms software confirmation deadline; transient ramp progress is re-read.
  Unknown/incompatible devices fail without probe-writing them. Fan-out is not
  the bounded lease safety mechanism.
- Delivery reports extend `GLOBAL_STOP_STATE` (111), sender CONTROLLER (10),
  state DONE (7). Existing global messages 0..3 remain unchanged. For messages
  with high bits set, low six bits are amplifier ID 1..50; high bits `0x40` mean
  pending, `0x80` confirmed, `0xc0` failed. Reports occur on initialization and
  terminal outcome; command 111 replays current per-amplifier results after the
  global latch result. Confirmed means firmware-register evidence, not physical
  neutral. No new command number is introduced.
- Command 112 changes LATCHED to ARMED, allowing renewal again. It starts a bounded
  heartbeat grace interval; the next heartbeat renews authority. Missing that
  heartbeat still times out after 2 s. Repeated stop commands remain one-shot
  within each latch epoch. Release authority/physical-input checks belong to C#;
  command 112 itself neither clears PIC18 faults nor restores PWM.

## Executed-test boundary

`test_global_stop.py` extracts unchanged production functions and compiles them
with MSVC `/W4 /WX`; `phase1_cases.h` supplies cases. Timer/UART/ACK delivery is
mocked, but transaction submission, FC03 parsing, compatibility, configure/enable,
poll traversal, latch/watchdog, lease scheduling, stop retry and confirmation
decisions execute production C. Cases cover legacy counter `0x1101 -> 0x1102`,
cached identity rejection, read timeouts, full 21-register traversal, full-mailbox
lease priority, 50 amplifiers with three timeouts each, malformed FC03, confirmation
deadline, unsupported applied capability, EMO, heartbeat rollover and re-arm.

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
