# P2-B2-A1 — Single-target isolation proof: Architect review (independent)

**Date:** 2026-10-25
**Reviewer:** Architect (read-only; no code changed, no hardware touched)
**Reviewed artifacts:** `docs/P2-B2-A1-SINGLE-TARGET-ISOLATION-PROOF.md` and commit `f9c084b1` (`TrackController5/Python/flash_one.py`), on branch `docs/p2-b2-a1-pic18-pilot`.

**Method:** every source citation in the proof was re-read directly at the cited file/line, and every claim in the six isolation points was re-derived from source. flash_one.py was read in full and the diff inspected. This is an offline source verification, not hardware evidence.

---

## 1. Source-citation verification (independent re-read)

| # | Claim in proof | Cited location | Verdict | Actual evidence |
|---|---|---|---|---|
| 1 | `ID_PORT = PORTAbits.RA0` | `TrackAmplifier4.X/mcc_generated_files/pin_manager.h:65` | **PASS** | Line 65 reads exactly `#define ID_PORT PORTAbits.RA0`. |
| 2 | `IO_PIN_ENTRY_PORT_PIN = PORTAbits.RA0` | `TrackAmplifierBootLoader.X/mcc_generated_files/bootload.h:40` | **PASS** | Line 40: `#define IO_PIN_ENTRY_PORT_PIN PORTAbits.RA0`. `IO_PIN_ENTRY_RUN_APP = 1` (line 41), `IO_PIN_ENTRY_RUN_BL = 0` (line 42). |
| 3 | App ID pin and bootloader entry pin are the SAME pin | (1)+(2) | **PASS** | Both are `PORTAbits.RA0`. |
| 4 | `Bootload_Required()`: IO-pin (RA0 LOW) OR flash-checksum mismatch | `pic18f_bootload.c:162-199` | **PASS (minor range)** | RA0-LOW branch returns `true` at lines 171-175; checksum-mismatch branch returns `true` at 196-200; `return(false)` at 201, closing brace 202. Function is 162-202, not 162-199 — immaterial. |
| 5 | Bootloader waits `while(IO_PIN_ENTRY_PORT_PIN == IO_PIN_ENTRY_RUN_BL)` | `pic18f_bootload.c:148` | **PASS** | Line 148 exact. |
| 6 | `Amplifier_IsBootloaderWrite` gate (all six terms) | `TrackAmplifier4.X/main.c:33-36` | **PASS (minor range)** | Function is 33-37; the `return` spans 35-36 and contains exactly `Config && Startup_Machine == 1u && MODBUS_ADDRESS == 0xAAu && frame_address == 0xAAu && addr == HR_SW_CHECKSUM && value == 0u`. |
| 7 | Startup `Config=1`, `Startup_Machine=0`, `MODBUS_ADDRESS=0xAA` | `main.c:91,96-105` | **PASS (citation imprecise)** | `MODBUS_ADDRESS = 0xAA` at line 91 (correct). `Config=1` and `Startup_Machine=0` are **static initializers at lines 24-25**, not at 91/96-105. Lines 96-105 are the `while(Config)` loop + `case 0`. Behavior is correct; citation slightly off. |
| 8 | `Startup_Machine 0→1` only when `ID_PORT==0` | `main.c:101-103` | **PASS** | Lines 101-103: `if (ID_PORT == 0){ Startup_Machine = 1; }`. |
| 9 | checksum-zero → `Startup_Machine 2→3` → `RESET()` | `main.c:132-150` | **PASS** | 133-134 (`checksum==0` → `2`), 141-142 (`==2` → `3`), 149-150 (`==1` → `RESET()`). |
| 10 | `HoldingReg0=0x01` → all `AMP_IDx_SET_LAT=1` then only `AMP_ID1_SET_LAT=0` | `TrackBackplane2.X/main.c:107-129` | **PASS** | 110-113 set all ten to 1; 116 `case 0x1: AMP_ID1_SET_LAT = 0;`; 126-129 default re-sets all to 1. |
| 11 | `ModBusSlaveMapping.txt:124-133` ("HoldingReg0 bit N = Slave N enable ... for configuring of ID") | (path elided) | **PASS (path note)** | Lines 124-133 in `Common_Files/ModBusSlaveMasterMappings/ModBusSlaveMapping.txt` list `HoldingReg0, 0..9 = Slave 1..10 enable ... for configuring of ID`. Content correct; the doc omits the `ModBusSlaveMasterMappings/` directory prefix. |
| 12 | `SelectSlave` case 0/2/4/6 behavior | `slavefwhandler.c:1147-1324` | **PASS** | case 0 `Slaves_Disable_Off()` (1180); case 2 `HOLDINGREG0 = SLOT1<<ShiftSlot1` (1199-1211, write at 1207); case 4 `HR11=0` to `SLAVE_INITIAL_ADDR` (1233-1246, writes at 1241-1242); case 6 release `HOLDINGREG0=0` (1266-1279, write at 1275). |
| 13 | `SelectSlave(1)` → backplane 51, `ShiftSlot1=0` | `slavefwhandler.c:1152-1154` + `enums.h:321` | **PASS** | `if (SlaveId>0 && SlaveId<11){ BackplaneId=51; ShiftSlot1=SlaveId-1; }` → `SelectSlave(1)`: 51, 0. `SLOT1 = 0x1` (`enums.h:321`) so `SLOT1<<0 = 0x01`. |
| 14 | `Slaves_Disable_On/Off` = RD1 | `system_config.h:514-516` | **PASS** | Lines 515-516 use `PORT_CHANNEL_D, PORTS_BIT_POS_1` (= RD1). |
| 15 | 107 RESET = `Slaves_Disable_On` + 1 s + ack `MBUS_STATE_RESET/DONE` | `mbus.c:377-394` | **PASS** | 380 `Slaves_Disable_On()`; 390 `> (1*SECONDS)`; 392 ack `(MBUS_STATE_RESET, DONE)`. |
| 16 | 100 SLAVES_ON = `Slaves_Disable_Off` + boot-wait ~0.79 s + ack `MBUS_STATE_SLAVES_ON/DONE` | `mbus.c:289-303` | **PASS** | 291 `Slaves_Disable_Off()`; 299 `> 100000000` ticks; 301 ack `(MBUS_STATE_SLAVES_ON, DONE)`. With `SECONDS = 126000000` (`enums.h:82`), 100000000 ticks ≈ **0.794 s**, so "~0.79 s" is accurate. |
| 17 | `SelectSlave` returns ERROR on `SLAVENOK`, no next-slave escalation | `slavefwhandler.c:1222-1226` | **PASS** | 1222-1226: `SLAVENOK → return_val = ERROR; iSelectSlaveSequencer = 0;` (resets to case 0, does not advance to another slave). |
| 18 | C# init order `ResetAllSlavesStep → … → FlashFwTrackamplifiersStep` | `TrackApplicationRuntimeHost.cs:467-471` | **PASS** | 467 `ResetAllSlavesStep`, 471 `FlashFwTrackamplifiersStep` (468-470 are DataUpload/DetectSlaves/RecoverSlaves in between). |

### Citation errors found (minor, non-blocking)

- **Point 5 of the proof** ("Slot released, SelectSlave case 6") cites **`slavefwhandler.c:1239`**. Line 1239 is `Data.Direction = WRITE;` inside case 4, **not** the slot release. The SelectSlave case 6 release is at **lines 1271-1275** (`Data.RegData0 = 0` at 1275). The substantive claim is correct; only the line number is wrong. (The parallel FlashSequencer case 8 release at `968-977` is correctly cited.)
- `Config=1`/`Startup_Machine=0` are at `main.c:24-25`, not `main.c:91,96-105` (claim #7).
- `Bootload_Required()` spans 162-202 (doc: 162-199); `Amplifier_IsBootloaderWrite` spans 33-37 (doc: 33-36). Cosmetic only.

---

## 2. The six required isolation proofs — independent assessment

**1. Sequence 107→100→103→120/121→123(1)→125-130 correctly ordered. — PASS**
`flash_one.py` `run()` (lines 551-560) calls `_connect()` (250) → `_global_reset()` (107,100) → `_enter_fw_handler()` (103) → `_receive_fw_file()` (120/141) → `_receive_config_word()` (121/151) → `_manual_primitives()` (123→125-129→130) → `_verify_readback()` (100/106/101). Controller routing confirmed (`controller.c:328-367` routes 100/103/107 to MBUS states). The C# historical route confirms reset-before-flash (`TrackApplicationRuntimeHost.cs:467,471`).

**2. Global reset affects 3/4/6 only as startup; firmware unmodified. — PASS (see residual risk R2/R3)**
107/100 toggle only RD1 (`system_config.h:515-516`, `mbus.c:380,291`); no flash write. Correct **provided** amps 3/4/6 see RA0 HIGH at boot and their legacy checksum is intact — see residual risks R2 and R3 below, which the proof does not cover.

**3. 120/121 and 123 cannot select 3/4/6 for ERASE/WRITE. — PASS (see R2)**
120/121 download to PIC32 RAM before any slave selection (confirmed in `_receive_fw_file`/`_receive_config_word`). The 123 bootloader-entry write is gated by `Startup_Machine==1` (RA0 LOW), and `SelectSlave(1)` writes `HoldingReg0=0x01` (only `AMP_ID1_SET_LAT=0`). Correct **provided** no stale slot was already selected — see R2.

**4. Only amp 1 receives firmware-programming operations. — PASS (conditional)**
125-129 are bootloader-UART commands (`slavefwhandler.c:303-307` switches to 8-bit and calls `ERASExFLASH` etc.). The bootloader protocol has no slave addressing — it relies on exactly one amp being in bootloader mode, enforced by the slot-select. This holds under the proof's clean-state premise; R2 is the counter-case.

**5. Amps 3/4/6 return to normal legacy application state. — PASS**
`_verify_readback()` emits 100/106/101 (read-only re-init path; `flash_one.py:821,830,839`), consistent with the standard bring-up. No flash write targets 3/4/6.

**6. Timeout / missing ACK / selection failure cannot flash another amp. — PASS**
`_wait_feedback` raises `FlashAbort` on ERROR (504-507) and on timeout (517); no ASSUME-OK. `_bootloader_step` moves `self.state` to `FAILED_UNKNOWN_HARDWARE_STATE` and re-raises (767-769); no retry loop exists. `SelectSlave` returns ERROR on `SLAVENOK` without advancing slaves (1222-1226). `122` (`EXEC_FW_STATE_FLASH_ALL_SLAVES`, `Enum.py:51`) is never emitted.

---

## 3. flash_one.py correction verification (commit `f9c084b1`)

- **(a) 107/100 emitted before 103 — PASS.** `run()` order (551-560) puts `_global_reset()` (582/589 emit 107/100) before `_enter_fw_handler()` (600 emits 103).
- **(b) Ack predicates correct — PASS.** 107 waits `(MBUS, MBUS_STATE_RESET, DONE)` (584-586); 100 waits `(MBUS, MBUS_STATE_SLAVES_ON, DONE)` (591-593). Both match `mbus.c:392` and `mbus.c:301`.
- **(c) Fail-closed preserved — PASS.** No retry loop; `_wait_feedback` aborts on ERROR and timeout → `FlashAbort`; `_bootloader_step` sets `FAILED_UNKNOWN_HARDWARE_STATE` on abort; no ASSUME-OK flag.
- **(d) No 122/131/132 emitted — PASS.** Full enumeration of `_send_simple`/`_send_payload` call sites (564, 582, 589, 600, 622, 635, 663, 671, 690, 746, 759, 821, 830, 839) contains only 250/107/100/103/120/141/121/151/123/125-129/130/100/106/101. 122/131/132 appear only in comments/docstring.
- **(e) 123 forwards exactly the requested slave id, no escalation — PASS.** Line 690 `_send_simple(EXEC_FW_STATE_SELECT_SLAVE, self.slave_id)`; `self.slave_id` validated 1..50 (439-442); packet layout `[0xAA, cmd, data, 0x55]` puts `slave_id` in `data[0]`; firmware maps it verbatim `fwData.data = EthernetRecvData->data[0]` (151) → `SelectSlave(fwData.data)` (255). No escalation to 122.

---

## 4. Residual risks

### R1 — Hardware wiring link (doc-flagged, corroborated-but-not-net-traced)
Backplane `AMP_IDx_SET` output → amplifier `RA0` (ID pin) is the one element not re-derived in KiCad this session. The proof's software half is fully verified, but the entire isolation argument rests on this physical link (and its polarity: `AMP_IDx_SET_LAT=0` → amp RA0 LOW). This is the doc's own honest flag and remains **the** non-source-verified element. Mitigation: the planned PicoScope capture of the slot-select line before ERASE.

### R2 — Backplane `HoldingReg0` initial state is not established (new finding, inferred from source)
The proof's premise "RA0 is HIGH (no slot selected)" at the 100 power-up depends on the backplane's `HoldingReg0` being `0` at that instant. **107/100 reset only the amplifiers (RD1), not the backplane**, and `flash_one.py` does **not** clear `HoldingReg0` before 107/100.

Inference (not hardware-verified): if a prior operation left `HoldingReg0` non-zero (a stale slot selected), that amplifier boots with RA0 LOW at 100 and enters the bootloader via the **IO-pin path** (`pic18f_bootload.c:171-175`), which is **independent of** the application's `Startup_Machine==1` gate. When `SelectSlave(1)` case 2 later writes `HoldingReg0=0x01`, the stale amp's RA0 goes HIGH, it exits the bootloader `while` loop (148) and **starts running the bootloader**. Because the bootloader is addressless 8-bit UART, a later 125 ERASE could then reach **both** the stale amp and amp 1.

This does not invalidate the source citations or the code correction, but it means the "double isolation" claim is conditional on a clean backplane state that the proof does not establish and the correction does not enforce. Cheap hardening: emit an explicit "release all slots" write (`HoldingReg0=0`) before 107, or make the pre-ERASE PicoScope capture mandatory. Note the fleet 122 path has the same characteristic, so this is not new to flash_one.py, but the proof should state the assumption.

### R3 — Amps 3/4/6 intact-firmware premise (stated, not verified this session)
The proof assumes amps 3/4/6 have intact legacy firmware (checksum valid). If any amp 3/4/6 had a corrupt/missing checksum, it would enter the bootloader via the checksum path after 100 regardless of RA0, and could be reached by 125. The doc states this premise; it is a genuine (low-probability) residual risk, not an overstatement.

---

## 5. Verdict

The source citations are real and correct (three minor line-range/path imprecisions and one wrong line number at "point 5", none affecting substance). All six isolation points PASS under the proof's stated premises. The flash_one.py correction is correct in all five required respects and remains fail-closed.

The proof is **verified as an offline source argument**, with two residual risks that must be accepted or mitigated before any physical flash: the doc-flagged wiring link (R1) and the backplane `HoldingReg0` initial-state gap (R2), which the planned PicoScope capture can both confirm.

**Recommendation:** `ISOLATION_PROOF_VERIFIED` for the source/code level; treat R2 as a product decision (add a pre-107 "release all slots" write, or require the pre-ERASE scope capture) before authorizing a hardware flash.
