# P2-B2-A1 — Single-Target Isolation + Recovery Analysis (PIC18 Amplifier 1 only)

- Role: Architect
- Scope: read-only source analysis. No code modified, no hardware touched.
- Worktree: `C:\Localdata\Siebwalde` (master `1a5f0bf5`).
- Date: 2026-10-25.

## Verdict summary

| Question | Verdict |
|---|---|
| Single-target isolation | **ISOLATION_VERIFIED** |
| Rollback / recovery path | **AMP1_RECOVERY_PATH_ACCEPTABLE** |

No blocker was found in the single-target isolation design. Two residual items are
flagged as operational/hardware caveats (not source blockers) in §"Caveats".

---

## 1. Exactly one target selected

**Verdict: verified.**

`SelectSlave(SlaveId)` maps the slave id to a backplane + slot bit:

- `slavefwhandler.c:1152-1154`
  ```c
  if( SlaveId > 0  && SlaveId < 11){
      BackplaneId = 51;
      ShiftSlot1  = SlaveId - 1;}
  ```
  For `SlaveId = 1`: `BackplaneId = 51`, `ShiftSlot1 = 0`.

- `slavefwhandler.c:1203-1207` writes the slot-select register:
  ```c
  Data.SlaveAddress  = BackplaneId;          // 51
  Data.StartRegister = HOLDINGREG0;
  Data.RegData0      = (SLOT1 << ShiftSlot1); // SLOT1 << 0 == 0x1
  ```
  `SLOT1 = 0x1` (`enums.h:321`), `TRACKBACKPLANE1 = 51` (`enums.h:322`).

The backplane slot-select semantics are documented in the project's own ModBus
mapping — each bit of backplane `HOLDINGREG0` is a single amplifier enable:

- `Common_Files/ModBusSlaveMasterMappings/ModBusSlaveMapping.txt:124-133`
  ```
  HoldingReg0, 0   Slave 1  enable  ... Enable Slave 1  for configuring of ID
  HoldingReg0, 1   Slave 2  enable
  ...
  HoldingReg0, 9   Slave 10 enable
  ```

So `SLOT1 << 0 == 0x0001` addresses **only** bit 0 = "Slave 1 enable" on
backplane 51. There is no multi-bit mask and no broadcast write in this path.
(`BROADCAST_ADDRESS = 0` exists at `enums.h:328` but is never referenced by
`SelectSlave`.)

> Physical/electrical note: the "backplane physically disconnects the other
> slots" property is asserted by the register map above and by the backplane
> hardware (KiCad). It is confirmed at the firmware-interface and documentation
> level; the electrical isolation itself was not re-verified in this read-only
> session (see Caveats).

---

## 2. No broadcast/global selection

**Verdict: verified.**

- Command identity: `enums.h:200` `EXEC_FW_STATE_FLASH_ALL_SLAVES = 122`,
  `enums.h:201` `EXEC_FW_STATE_SELECT_SLAVE = 123`. They are distinct commands.

- `SelectSlave` performs the slot-select write **before** the bootloader-entry
  write. The state machine order is:
  - `case 2` (`slavefwhandler.c:1199-1210`): `HOLDINGREG0 = SLOT1<<ShiftSlot1` (slot select)
  - `case 3` (`slavefwhandler.c:1215-1231`): wait for backplane comm status
  - `case 4` (`slavefwhandler.c:1233-1245`): `SLAVE_INITIAL_ADDR` write `HOLDINGREG11 = 0`
  - `case 6` (`slavefwhandler.c:1266-1278`): `HOLDINGREG0 = 0` (release slot)

  `SLAVE_INITIAL_ADDR = 0xAA` (`enums.h:327`), `HOLDINGREG11 = 11` (`enums.h:319`).

- The bootloader-entry write (`HR11 = 0` to address `0xAA`) is therefore gated by
  the immediately preceding single-slot selection. Only the selected amplifier's
  `HR11` is forced to `0` (checksum-mismatch → bootloader entry); the other
  amplifiers keep their checksum and remain in application mode.

- After `SelectSlave` releases the slot (`case 6`), the bootloader commands
  125–129 operate over the 8-bit bootloader UART (`UART2_Config_8bit_Bootloader`,
  `slavefwhandler.c:305-306` for ERASE, same pattern for 126–129). Only the
  single amplifier forced into bootloader mode is listening on that 8-bit
  protocol; the others remain in 9-bit ModBus mode. This is the identical
  select→force→release→8-bit-command structure used by the full
  `FlashSequencer` (122) path (`slavefwhandler.c:844-1127`, esp. cases 4/6/8/10),
  so the isolation mechanism is shared with the established design.

---

## 3. No auto multi-target fallback

**Verdict: verified.**

- `SelectSlave` (`slavefwhandler.c:1147-1324`) takes a single `SlaveId` and has
  **no loop over slaves** and no "advance to next slave" logic. Its only
  iteration is the fixed 0→8 case sequence for one slave.

- On a backplane comm failure the function returns `ERROR` and resets its own
  sequencer to 0; it does **not** try the next slave:
  - `slavefwhandler.c:1222-1226` (case 3): `SLAVENOK → return_val = ERROR; iSelectSlaveSequencer = 0`
  - `slavefwhandler.c:1255-1259` (case 5): same
  - `slavefwhandler.c:1290-1294` (case 7): same

- `FlashAllSlavesAuto` (122) is a **separate** code path (`slavefwhandler.c:722-820`,
  with the per-slave loop and `SlaveId1++` advance at `slavefwhandler.c:767-768`),
  and is reachable **only** from `case EXEC_FW_STATE_FLASH_ALL_SLAVES`
  (`slavefwhandler.c:222-224`). `flash_one.py` never emits 122.

---

## 4. Timeout cannot cause target escalation

**Verdict: verified.**

- `FlashOneDriver._wait_feedback` (`flash_one.py:481-513`):
  - aborts on any `taskstate == ERROR` (`flash_one.py:500-503`);
  - aborts on timeout (`flash_one.py:513`);
  - both raise `FlashAbort` (`flash_one.py:229-230`).

- No retry/re-send: `run()` (`flash_one.py:547-556`) is a strictly linear
  sequence of steps with no loop over failure paths.

- `_bootloader_step` (`flash_one.py:719-740`) catches `FlashAbort`, transitions
  `self.state` to `FAILED_UNKNOWN_HARDWARE_STATE` and re-raises
  (`flash_one.py:737-739`) — never "assume OK" and never advance.

- The CLI `main()` catches `FlashAbort` and exits with code 1
  (`flash_one.py:1082-1085`). The slave id is fixed at construction
  (`flash_one.py:417`, validated 1..50 at `flash_one.py:435-438`); there is no
  concept of "next target".

---

## 5. No unrelated PIC18 recovery activation

**Verdict: verified.**

- `flash_one.py` never emits `124` (`EXEC_FW_STATE_GET_BOOTLOADER_VERSION`). The
  only textual occurrences are in docstrings/comments (`flash_one.py:9,136`); no
  `_send_*` call uses it. The docstring states the design decision explicitly
  (`flash_one.py:135-140`): the probe is intentionally omitted.

- `flash_one.py` does not call `RecoverSlavesStep` (a C# type; the Python driver
  has no dependency on it). The reference at `flash_one.py:48,671` is commentary
  only.

- `flash_one.py` never emits `131`/`132`. These command codes do not exist:
  `enums.h` FW-handler commands end at `EXIT_SLAVExFWxHANDLER = 130`
  (`enums.h:208`); grep of `enums.h` for `= 131`/`= 132` returns nothing. The
  driver's emitted set is 250/103/120/141×N/121/151/123/125/126/127/128/129/130
  + readback 100/106/101.

---

## 6. Rollback feasibility

**Verdict: verified — AMP1_RECOVERY_PATH_ACCEPTABLE.**

- Same-path rollback: `flash_one.py` is image-agnostic. The HEX is supplied via
  `--hex` (`flash_one.py:1038-1039`) and parsed generically by `HexImage`
  (`flash_one.py:345-394`) reusing `BootLoader` read/checksum/config parsing.
  The emitted sequence (123→125→126→127→128→129→130) is independent of which HEX
  is loaded. Restoring the legacy image is therefore the exact same path with a
  different `--hex`.

- Artifact identities (executed evidence):
  - Target `build/firmware/out/f9ae0ed/TrackAmplifier4/Offset/TrackAmplifier4.X.production.hex`
    SHA256 `4F9ED4EC…1F305A2A` (matches context `4F9ED4EC…`). Last data record
    `:107FF000…FFFF06E9` ⇒ trailer at 0x7FFE = `0xE906`; config record
    `:0C0000008CFFFFFF9FFFFFFDFFFFFFFF` ⇒ `8cffffff9ffffffdffffffff`.
  - Rollback `release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex`
    SHA256 `B848EF04…1C1887D` (matches context `B848EF04…`). Last data record
    `:107FF000…FFFF1F25` ⇒ trailer `0x251F`; identical config word
    `8cffffff9ffffffdffffffff`.
  - Both files are 86529 bytes and share the same config word; only the firmware
    image (and its 16-bit trailer checksum) differ. This is consistent with the
    observed legacy `HR11 = 0x251F` (checksum) identity.

- Boot entry via checksum mismatch (enables re-flash after a failed update):
  `Bootload_Required()` in `TrackAmplifierBootLoader.X/mcc_generated_files/pic18f_bootload.c:162-200`
  computes the checksum over the application area
  (`NEW_RESET_VECTOR(0x800) .. END_FLASH-2`, `pic18f_bootload.c:182-189`) and
  compares it to the stored trailer at `END_FLASH-2` (`pic18f_bootload.c:190-194`);
  on mismatch it returns `true` → `Run_Bootloader()` (`pic18f_bootload.c:141-153`).
  An interrupted/failed write leaves a mismatched trailer, so the device re-enters
  bootloader on next reset. Application entry is `goto 0x800`
  (`pic18f_bootload.c:158`, `NEW_RESET_VECTOR = 0x800` at `pic18f_bootload.c:96`).

- Bootloader READ_VERSION exists: `READ_VERSION = 0` (`pic18f_bootload.c:51`),
  dispatched at `pic18f_bootload.c:214-215`, and `Get_Version_Data()` returns
  `MINOR_VERSION = 0x00` / `MAJOR_VERSION = 0x01` (`pic18f_bootload.c:88-89`,
  `pic18f_bootload.c:260-263`) — i.e. `0x01.00`.

- Secondary recovery path present in C#: `RecoverSlavesStep` is single-slave and
  probes `EXEC_FW_STATE_GET_BOOTLOADER_VERSION` (124) first
  (`RecoverSlavesStep.cs:170`, branching on OK/NOK at `RecoverSlavesStep.cs:189-226`,
  log "flash sequence for this slave only" at `RecoverSlavesStep.cs:208`). It does
  not emit 123 (it assumes the target is already in bootloader). **Caveat:** per
  the flash_one.py documentation, the PIC32 124 probe is non-functional on this
  legacy hardware (`flash_one.py:135-140`), so this C# path would stall/fail at
  its 124 gate; it is present as a fallback but not the recommended rollback
  route. See Caveats.

---

## 7. Mixed-topology safety (amps 3/4/6 stay legacy, not enabled, not auto-upgraded)

**Verdict: verified.**

- HR7 identity gate: `VERIFYxAMPLIFIERxIDENTITY` (`slavestartup.c:23-59`) reads
  `HR_IDENTITY` (`HOLDINGREG7`, `enums.h:97` / `enums.h:315`) and marks an
  amplifier compatible only when
  (`slavestartup.c:47-49`):
  ```c
  status == SLAVEOK && phase == 3 &&
  identity == first && (identity & 0xffu) == 1u &&
  (identity & 0x0f00u) == 0x0100u && (identity & 0xf000u) != 0
  ```
  A legacy amplifier with `HR7 = 0x0000` fails this gate
  (`(identity & 0xff) == 0 ≠ 1`, and `(identity & 0xf000) == 0`), so
  `AmplifierCompatible[slave]` stays false and
  `MASTER_SLAVE_DATA[slave].HoldingReg[HR_IDENTITY]` is forced to `0`
  (`slavestartup.c:50-51`).

- Enable is gated on compatibility: `ENABLExAMPLIFIER` skips non-compatible
  amplifiers (`slavestartup.c:659-660`:
  `while (EnableSlave <= NUMBER_OF_AMPLIFIERS && !ISxAMPLIFIERxCOMPATIBLE(EnableSlave)) ++EnableSlave;`),
  so legacy amps 3/4/6 are never written `HOLDINGREG1 = 0x8000` (never enabled).

- No automatic upgrade of 3/4/6 from this operation: `flash_one.py` targets only
  `--slave-id 1` and never emits 122. The fleet auto-upgrade exists **only** in
  the C# `FlashFwTrackamplifiersStep`, which ends with
  `EXEC_FW_STATE_FLASH_ALL_SLAVES` (122) (`FlashFwTrackamplifiersStep.cs:425`) and
  iterates over *all* checksum-mismatched detected slaves
  (`FlashFwTrackamplifiersStep.cs:139-154`). That step **MUST NOT be invoked** for
  this increment (operational constraint, not a code defect).

---

## Caveats (not source blockers)

1. **Physical slot isolation** (Req 1/2): confirmed at the firmware-interface and
   documentation level (single-bit `HOLDINGREG0` write to one backplane; register
   map `ModBusSlaveMapping.txt:124-133`). The electrical guarantee that the
   backplane physically disconnects non-selected slots is a hardware property
   (KiCad / `TrackBackplane2.X`) not electrically re-verified in this read-only
   session. Recommend the operator confirm the backplane slot routing before the
   first live ERASE.

2. **C# secondary recovery path** (Req 6): `RecoverSlavesStep` probes 124 first,
   but the PIC32 124 route is documented non-functional on this legacy hardware
   (`flash_one.py:135-140`). It is therefore present but not expected to succeed
   as a live fallback; the reliable rollback is the same `flash_one.py` path with
   the `0x251F` HEX.

3. **Do not use the C# init auto-flash** (Req 7): `FlashFwTrackamplifiersStep`
   emits 122 (fleet). Running the normal C# initialization with a firmware
   mismatch present would flash *all* detected mismatched amplifiers, not just
   amp 1. The P2-B2-A1 procedure must use `flash_one.py --slave-id 1` and must
   not trigger the C# auto-upgrade path.

---

## Evidence classification

- **Direct code evidence** (verified this session, cited file:line above):
  mapping/order in `SelectSlave`; distinct 122 vs 123; absence of 124/131/132
  emission in `flash_one.py`; `_wait_feedback` abort semantics; HR7 identity and
  enable gates; bootloader checksum-mismatch entry and READ_VERSION; HEX trailer
  checksums and config words (executed file hash + tail record reads).
- **Documentation evidence**: backplane `HOLDINGREG0` per-slot enable bits
  (`ModBusSlaveMapping.txt`).
- **Inference**: that the 8-bit bootloader protocol reaches only the single
  forced slave (shared structure with `FlashSequencer`; 8-bit vs 9-bit framing).
- **Assumption / unverified**: electrical single-slot isolation of the backplane
  (hardware, not re-verified); the "124 probe non-functional" claim is taken from
  the flash_one.py design notes and this session's context rather than from a
  source-level proof.
