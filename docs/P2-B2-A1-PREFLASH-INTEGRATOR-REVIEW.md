# P2-B2-A1 — Pre-flash integrator review (independent)

**Status:** offline, read-only, independent Integrator review. No hardware touched, no
command emitted, no flash.
**Reviewed revision:** branch `docs/p2-b2-a1-pic18-pilot` @ `2d4a921b`
(`fix(flash_one): release all backplane slots before global reset (R2 hardening)`).
**Scope:** pre-flash preflight of the PIC18 amplifier-1 single-target Phase-1 flash
(`flash_one.py` drives the deployed PIC32 manual primitives 120-130; target = amp 1;
image 1.0.0 checksum 0xE906; amps 3/4/6 must stay 0x251F).

## Method

Every claim was re-read from source at the current revision, not accepted from the
Developer/Architect/PL conclusions. Verified: actual Git state (HEAD `2d4a921b`, clean
tracked tree, only `Logging/` untracked); `flash_one.py` in full; the cited firmware
sources (`mbus.c`, `slavefwhandler.c`, `slavebootloaderroutines.c`, `controller.c`,
`slavestartup.c`, `enums.h`, `mbus.h`, `ethernet.c`); PIC18 application `main.c` +
`pin_manager.h`; bootloader `bootload.h`; backplane `main.c`; `bootload.py` +
`Enum.py`; the two HEX images; and re-ran `test_flash_one` (51 tests).

Branch diff vs merge-base `1a5f0bf5` is only `flash_one.py`, `test_flash_one.py`, and
7 doc files — **no PIC32/PIC18/C# source changes**. The PIC32 firmware source reviewed
here is therefore the deployed firmware source (build `f9ae0ed`), and the driver drives
against it.

---

## Per-item verdicts

### 1. Corrected sequence — **PASS**

`FlashOneDriver.run()` (`flash_one.py:554-565`) emits, in order:

```
250 (CLIENT_CONNECTION_REQUEST)          _connect()
101 (SLAVE_DETECT)                       _release_all_slots()
107 (RESET), 100 (SLAVES_ON)             _global_reset()
103 (SLAVE_FW_FLASH)                     _enter_fw_handler()
120 + 141 x N                            _receive_fw_file()
121 + 151                                _receive_config_word()
123 (data = slave_id), 125,126,127,128,129,130   _manual_primitives()
100, 106, 101                            _verify_readback()
```

This matches the required order exactly (250 → 101 → 107 → 100 → 103 → 120+141xN →
121+151 → 123(slave_id) → 125..130 → 100 → 106 → 101). `123` carries `self.slave_id`
as `data[0]` (`_send_simple(EXEC_FW_STATE_SELECT_SLAVE, self.slave_id)`, line 717).

**Never 122/131/132:** grep of `flash_one.py` shows `122/131/132` appear only in
comments/docstrings, never in a `_send_simple`/`_send_payload` call. The enum source
(`enums.h:120-208`) has no 131/132 (commands end at 130), and `Enum.py` likewise. Tests
`test_forbidden_commands_never_emitted`, `test_release_all_slots_emits_no_forbidden_commands`
assert this. `--dry-run` path (`SelfAckTransport`) self-acks the exact target sequence.

### 2. Ack predicates — **PASS**

| Cmd | Driver predicate (flash_one.py) | Firmware emission | Match |
|---|---|---|---|
| 101 | `MBUS / MBUS_STATE_SLAVE_DETECT / DONE` (:592-596) | `mbus.c:311` `CREATExTASKxSTATUSxMESSAGE(MBUS, MBUS_STATE_SLAVE_DETECT, DONE, NONE)` | ✓ |
| 107 | `MBUS / MBUS_STATE_RESET / DONE` (:610-614) | `mbus.c:392` `(MBUS, MBUS_STATE_RESET, DONE, NONE)` | ✓ |
| 100 | `MBUS / MBUS_STATE_SLAVES_ON / DONE` (:617-621) | `mbus.c:301` `(MBUS, MBUS_STATE_SLAVES_ON, DONE, NONE)` | ✓ |

Enum values match: Python `EnumMbusStatus` (SLAVES_ON=0x02, SLAVE_DETECT=0x04,
RESET=0x0A) == `mbus.h` enum (SLAVES_ON=2, SLAVE_DETECT=4, RESET=10). `TASK_ID MBUS=20`,
`DONE=7` identical in both.

**123 / 130 emit no ack:** `slavefwhandler.c:253-273` (SELECT_SLAVE) only `LOG_Push` +
state change, no `CREATExTASKxSTATUSxMESSAGE`; `:443-450` (EXIT_SLAVExFWxHANDLER) only
resets state, no ack. Driver uses bounded delay for both (lines 718, 774). Note:
`SelectSlave` *does* emit an ERROR status only for an out-of-bounds slave id
(`slavefwhandler.c:1168-1174`), but the driver pre-validates `1..50`
(`flash_one.py:442-445`), so that branch is unreachable; the internal `SLAVENOK` ERROR
returns (`:1222-1226`, `:1255-1259`, `:1290-1294`) emit no status frame — consistent
with "no ack". A 123 failure is caught indirectly: 125 ERASE has a 1 s internal timeout
→ `ERASE_FLASH_RECEIVE_DATA_TIMEOUT` (201) with `task_state=ERROR` → `_wait_feedback`
aborts (see item 3).

125-129 ack codes verified in `slavebootloaderroutines.c`: ERASE→`ERASE_FLASH_RETURNED_OK`
(202, :185-189), WRITE→`WRITE_FLASH_RETURNED_OK` (212, :306-310), WRITE_CONFIG→
`WRITE_CONFIG_RETURNED_OK` (222, :424-428), CHECK→`CHECK_CHECKSUM_CONFIG_RETURNED_OK`
(232, :540-544), RESET→`RESET_SLAVE_OK` (242, :652-656). Driver `_bootloader_step`
waits exactly these (task_id, *_RETURNED_OK, DONE).

### 3. Fail-closed preserved — **PASS**

- `_wait_feedback` (:488-520) aborts immediately on `task_state == ERROR` (:507-510) and
  on timeout (raises `FlashAbort`, :520). No ASSUME-OK path exists.
- `_bootloader_step` (:776-797): on `FlashAbort` moves state to
  `FAILED_UNKNOWN_HARDWARE_STATE` and re-raises (:794-796).
- `FlashState` (:240-255) includes `FAILED_UNKNOWN_HARDWARE_STATE`.
- No retry loop anywhere; `main()` catches `FlashAbort` → `return 1`, no re-send
  (:1142-1145). Timeout budget is bounded (ACK 10 s, bootloader 60 s, FW transfer 120 s).
- `WRITExCONFIG` timeout bug (reports `WRITE_FLASH_RECEIVE_DATA_TIMEOUT` 211, not 221)
  confirmed at `slavebootloaderroutines.c:467-472`; driver correctly aborts on any ERROR
  regardless of code (documented `flash_one.py:116-126`, test
  `test_write_config_211_timeout_code_aborts`).
- Tests assert: missing-ack/returned-NOK/timeout abort for every destructive step,
  `unconfirmed_erase_never_emits_write`, `unconfirmed_write_never_emits_write_config`,
  `no_automatic_recovery_after_failure`, and missing 101/107/100 acks abort before 103.

### 4. Isolation proof (6 points) + R1/R2/R3 — **PASS** (with residual risks, see §Residual)

Independently re-derived the key mechanism:

- `pin_manager.h:65` `#define ID_PORT PORTAbits.RA0` ✓
- `bootload.h:40` `#define IO_PIN_ENTRY_PORT_PIN PORTAbits.RA0`; `:41` RUN_APP=1, `:42`
  RUN_BL=0 ✓ — application ID pin and bootloader IO-entry pin are the SAME pin (RA0).
- `main.c:33-36` `Amplifier_IsBootloaderWrite` = `Config && Startup_Machine==1 &&
  MODBUS_ADDRESS==0xAA && frame_address==0xAA && addr==HR_SW_CHECKSUM && value==0` ✓
  (6-term gate; `main.c:33-37`, return spans 35-36).
- Backplane slot-select `TrackBackplane2.X/main.c:105-133`: on `HoldingReg0` change, sets
  all `AMP_IDx_SET_LAT=1` (:110-113), then `case 0x1: AMP_ID1_SET_LAT=0` (:116); `default`
  (incl. `HoldingReg0==0`) sets all to 1 (:126-129). So `HoldingReg0=0x01` pulls only amp
  1's select line LOW; release (`0`) pulls all HIGH. ✓ (independently re-read, not relying
  on the Architect.)
- `SelectSlave` cases (`slavefwhandler.c:1177-1324`): case 2 select slot (:1199-1211),
  case 4 write HR11=0 to `SLAVE_INITIAL_ADDR` (:1233-1246), case 6 release slot
  (`Data.RegData0=0`, :1266-1279). ✓

**R2 now software-resolved at HEAD:** commit `2d4a921b` added `_release_all_slots()`
(101 SLAVE_DETECT) before 107/100. Mechanism verified: `SLAVExDETECT`→`DetectSlave`
case 7 writes `HoldingReg0=0` to the backplane at the end of each amp iteration
(`slavestartup.c:324-338`, write at :330), releasing every slot; backplane `default`
then drives all `AMP_IDx_SET_LAT=1` (HIGH). This closes the "stale slot → RA0 LOW at
100 power-up" gap the Architect flagged at `f9c084b1`. The isolation proof doc
(`docs/P2-B2-A1-SINGLE-TARGET-ISOLATION-PROOF.md`) already records R2 as RESOLVED
(software).

The 6 isolation points are source-backed and correctly ordered/characterized. R1 (wiring
link) and R3 (intact-firmware premise for 3/4/6) are correctly classified as hardware/
premise residuals, not source facts — see §Residual.

### 5. Image identity — **PASS**

| Image | Path | SHA-256 prefix | Embedded checksum (bootload.py GetFileCheckSum) |
|---|---|---|---|
| Target 1.0.0 | `build/firmware/out/f9ae0ed/TrackAmplifier4/Offset/TrackAmplifier4.X.production.hex` | **4F9ED4EC** (full `4F9ED4ECD6B9…305A2A`) | **0xE906** ✓ |
| Rollback legacy | `release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex` | **B848EF04** (full `B848EF040DBA…C1887D`) | **0x251F** ✓ |

Independently computed: SHA-256 via `Get-FileHash`; embedded checksum via
`flash_one.HexImage` (which wraps `bootload.GetFileCheckSum` verbatim). Both images parse
to 1920 rows / 480 packets; config word `8cffffff9ffffffdffffffff`. Matches
`docs/P2-B2-A1-PIC18-BOOTLOADER-CONTRACT.md`.

### 6. Tests — **PASS**

`python -m unittest test_flash_one` (from `TrackController5/Python`): **Ran 51 tests in
0.624s — OK** (51/51). (PowerShell reported a non-zero exit code purely from unittest
printing to stderr; the unittest result itself is "OK".)

### 7. Mixed-topology safety — **PASS**

`_verify_readback()` (`flash_one.py:830-874`) emits only 100 → 106 → 101. It does **not**
emit 105 (`EXEC_MBUS_STATE_SLAVE_ENABLE`) or 108 (`EXEC_MBUS_SLAVE_DATA_EXCH`) or any
motion/output command; grep confirms `105`/`108`/`ENABLE` appear only in comments
(:845-847). 105 is the enable path (`ENABLExAMPLIFIER` writes `HR1=0x8000`,
`slavestartup.c:646-700`), and 100 (`Slaves_Disable_Off`) re-asserts neutral power only
(:841-847). Amps 3/4/6 are never erased/written (only amp 1's slot is selected), so they
remain 0x251F; the readback re-runs detect/upload (read-only) and reports
`REPORT_SLAVES=(1,3,4,6)` (`flash_one.py:216`).

---

## Non-blocking documentation drift (advisory)

- `flash_one.py` docstring cites "`controller.c:196-259`" for MBUS/CLIENT routing; the
  actual routing switch is at `controller.c:306-384` (the file grew the safety-command
  handler). Citation stale, substance correct.
- The isolation proof and Architect review already list minor line-range imprecisions
  (e.g. proof "point 5" cites `slavefwhandler.c:1239` for the case-6 release, which is at
  :1271-1275). None affect substance. Not blockers.

## Residual risks requiring PO acceptance before hardware flash

1. **R1 — physical backplane `AMP_IDx_SET` → amp `RA0` wiring + polarity (not
   KiCad-traced).** The entire isolation rests on this single physical link and its
   active-low polarity (`AMP_IDx_SET_LAT=0` → amp `RA0` LOW). It is source-corroborated
   (backplane drives the select line; amp/bootloader read `RA0`; amps 1/3/4/6 were
   correctly detected at 1/3/4/6 in K8B/P2-B1, which requires this path), but not
   net-traced. **Requires the planned pre-ERASE PicoScope capture of the slot-select line
   (amp 1 LOW, amps 3/4/6 HIGH) before 125.** This is a physical-validation gate, not a
   preflight source blocker.

2. **R2 (closed in software, physical confirmation optional).** `101` release-all-slots
   before 107/100 resolves the stale-slot gap in source. Residual: `DetectSlave` only
   releases slots on *detected* backplanes (undetected backplane skips case 7,
   `slavestartup.c:246-267`); a genuinely unresponsive backplane cannot hold a
   *verified-selected* stale slot, but this is an inference. Belt-and-braces scope
   capture recommended.

3. **R3 — amps 3/4/6 intact-firmware premise.** If any of 3/4/6 had a corrupt checksum it
   would enter bootloader via the checksum path after 100 regardless of RA0. Currently
   they read 0x251F/detected=1, but this is a premise, not a per-run guarantee.

None of these is a preflight source/artifact failure. They are the physical-honesty items
to close (R1 via scope capture) or accept (R3 premise) at the `LIVE_HARDWARE` +
`FIRMWARE_FLASH` gate.

---

## Verdict

`P2_B2_A1_PREFLASH_INTEGRATOR_PASS`

All seven preflight items PASS at the source/artifact/software-execution level. The driver
emits the corrected single-target sequence with correct ack predicates, is fail-closed
(no ASSUME-OK, no retry, no escalation), never emits 122/131/132, targets only amp 1 via
`SELECT_SLAVE(1)`, and its readback is read-only (no 105/enable/motion). Image identity
(SHA-256 + embedded checksum) and the 51-test suite are independently re-verified. The
branch diff is bounded (driver + tests + docs only; no firmware/C# changes).

This PASS is scoped to **pre-flight (offline) verification** and does **not** authorize
hardware flash. Proceeding to `LIVE_HARDWARE` + `FIRMWARE_FLASH` requires the Project Lead
to hold a valid scoped Workflow v1 grant and the Product Owner to accept/close the
residuals above, in particular the pre-ERASE PicoScope capture for R1.
