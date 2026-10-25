# P2-B2-A1 — Single-target isolation proof (source-verified)

**Status:** offline source proof, 2026-10-25. No hardware flashed. Answers the PO's
remaining ambiguity (RESET+SLAVES_ON brings all amps to 0xAA, yet 3/4/6 do not enter
bootloader) and the six required isolation points.

## The ambiguity, resolved: "all amps to 0xAA" vs "only amp 1 in bootloader"

After a global reset, every PIC18 boots into its **application Config phase at address
0xAA** (`MODBUS_ADDRESS = 0xAA`, `Startup_Machine = 0`). This is **NOT the bootloader**.
The amplifier only transitions into the bootloader when its **RA0 entry pin** is pulled
LOW at boot. That pin is the SAME pin used for slot selection:

| Signal | Application | Bootloader |
| --- | --- | --- |
| RA0 = ID_PORT / entry pin | `ID_PORT = PORTAbits.RA0` (`pin_manager.h:65`) | `IO_PIN_ENTRY_PORT_PIN = PORTAbits.RA0` (`bootload.h:40`) |
| RA0 LOW (slot selected) | Config phase `Startup_Machine=1` (`main.c:101`) | `IO_PIN_ENTRY_RUN_BL = 0` → bootloader (`bootload.h:42`, `pic18f_bootload.c:171`) |
| RA0 HIGH (not selected) | stays `Startup_Machine=0` (waits) | `IO_PIN_ENTRY_RUN_APP = 1` → application |

The backplane drives RA0 per slot via its `HoldingReg0` slot-enable register
(`ModBusSlaveMapping.txt:124-133`), implemented in `TrackBackplane2.X/main.c:107-129`:
writing `HoldingReg0 = 0x01` sets **ALL** `AMP_IDx_SET_LAT = 1` (HIGH) then only
`AMP_ID1_SET_LAT = 0` (LOW). So ONLY amplifier 1 sees RA0 LOW; amps 3/4/6 see RA0 HIGH.

## Full state-transition trace (source-verified)

### 1. Global reset (107 RESET + 100 SLAVES_ON)

- 107 `MBUS_STATE_RESET`: `Slaves_Disable_On()` (RD1 HIGH, all amps held in reset),
  wait 1 s (`mbus.c`).
- 100 `MBUS_STATE_SLAVES_ON`: `Slaves_Disable_Off()` (RD1 LOW, all amps power up),
  boot-wait ~0.79 s (`mbus.c`).
- Every amp powers up: bootloader `Bootload_Required()` runs (`pic18f_bootload.c:141`):
  - RA0 is HIGH (no slot selected) → not bootloader via IO pin.
  - Flash checksum intact (legacy firmware un-erased) → not bootloader via checksum.
  - → **application** runs: `Config=1`, `Startup_Machine=0`, `MODBUS_ADDRESS=0xAA`
    (`main.c:91,96-105`). Amps 3/4/6 sit here indefinitely (no timeout in case 0).

### 2. Backplane slot-select (SelectSlave case 2)

`SelectSlave(1)` case 2 writes backplane `HoldingReg0 = SLOT1 << 0 = 0x01`
(`slavefwhandler.c:1199-1209`). Backplane pulls only `AMP_ID1_SET_LAT = 0`
(`main.c:116`). Amp 1's RA0 goes LOW → amp 1 transitions `Startup_Machine 0 → 1`
(`main.c:101-103`). Amps 3/4/6 keep RA0 HIGH → stay `Startup_Machine 0`.

### 3. Bootloader-entry write (SelectSlave case 4)

`SelectSlave` case 4 writes `HR11 = 0` to `SLAVE_INITIAL_ADDR = 0xAA`
(`slavefwhandler.c:1233-1245`). The application only accepts this as a bootloader-entry
trigger when ALL of: `Config && Startup_Machine==1 && MODBUS_ADDRESS==0xAA &&
frame_address==0xAA && addr==HR_SW_CHECKSUM && value==0`
(`Amplifier_IsBootloaderWrite`, `main.c:33-36`):

- Amp 1: `Startup_Machine==1`, `MODBUS_ADDRESS==0xAA` → **accepts** → `amp_model.checksum=0`
  → `Startup_Machine 2 → 3` (drain replies) → `RESET()` (`main.c:132-150`).
- Amps 3/4/6: `Startup_Machine==0` (≠1) → the write is **rejected** (treated as an
  ordinary register write in the waiting state; no checksum-zero transition).

### 4. Amp 1 enters bootloader

Amp 1 `RESET()` → `Bootload_Required()`: RA0 still LOW (slot not yet released) →
`IO_PIN_ENTRY_PORT_PIN == IO_PIN_ENTRY_RUN_BL` → bootloader required
(`pic18f_bootload.c:171`). Bootloader waits `while(RA0 == LOW)` (`:148`).

### 5. Slot released (SelectSlave case 6)

`SelectSlave` case 6 writes backplane `HoldingReg0 = 0` (`slavefwhandler.c:1239` in
FlashSequencer case 8: `:968-977`). RA0 goes HIGH → bootloader's `while(RA0==LOW)`
exits → bootloader starts communicating (8-bit UART). Amps 3/4/6 remain in application
`Startup_Machine=0`.

## The six required isolation proofs

**1. Sequence 107→100→103→120/121→123(1)→125-130 is correctly ordered.**
VERIFIED: 107/100 are MBUS-level (`controller.c` routes them to `SETxMBUSxSTATE`);
103 enters the FW handler (MBUS_STATE_SLAVE_FW_FLASH); 120/121 download the image into
PIC32 RAM; 123 selects amp 1 + triggers bootloader entry; 125-129 drive the bootloader
(ERASE/WRITE/WRITE_CONFIG/CHECK/RESET); 130 exits. Order matches the C# init that
historically proved fleet 122 (`ResetAllSlavesStep` → `FlashFwTrackamplifiersStep`,
`TrackApplicationRuntimeHost.cs:467-471`).

**2. Global reset affects 3/4/6 only as startup; firmware unmodified.**
VERIFIED: 107/100 toggle only the RD1 reset line (`Slaves_Disable_On/Off`,
`system_config.h:514-516`). No flash write occurs; amps 3/4/6 re-boot their intact
legacy application (`Bootload_Required` returns false: RA0 HIGH + checksum valid) and
wait at 0xAA until re-initialized.

**3. 120/121 and 123 cannot select 3/4/6 for ERASE/WRITE.**
VERIFIED: 120/121 download to PIC32 RAM only (no slave selection). 123's bootloader-
entry write requires `Startup_Machine==1`, which requires the amp's RA0 pulled LOW by
the backplane slot-select; only amp 1's slot is enabled (`HoldingReg0=0x01` →
`AMP_ID1_SET_LAT=0`, all others HIGH). 125-129 are addressed to the selected amp's
bootloader (which is entered only by amp 1).

**4. Only amp 1 receives firmware-programming operations.**
VERIFIED: 125 ERASE / 126 WRITE / 127 WRITE_CONFIG are sent on the bootloader UART path
after slot-select routed only slot 0, and the bootloader is entered only by amp 1.
`flash_one.py` emits `123 (data=slave id)` once with slave_id=1 and NEVER emits 122
(fleet) or 131/132.

**5. Amps 3/4/6 return to normal legacy application state.**
VERIFIED: after 130 EXIT, the post-flash readback 100/106/101 re-runs the normal
bring-up; amps 3/4/6 are re-initialized (address assignment + enable) and return to
their legacy `0x251F` application. Their flash is never erased or written.

**6. Timeout / missing ACK / selection failure cannot flash another amp.**
VERIFIED: `flash_one.py` is fail-closed (K3): any timeout/ERROR on 123/125-129 moves to
`FAILED_UNKNOWN_HARDWARE_STATE` and raises `FlashAbort`; there is NO retry loop, NO
target escalation, NO auto-advance to the next slave (`flash_one.py:96-119, 677-684`).
`SelectSlave` itself returns ERROR on `SLAVENOK` and does NOT try the next slave
(`slavefwhandler.c:1222-1226`). `FlashAllSlavesAuto` (122) is a separate, unreachable
code path from `flash_one.py`.

## Conclusion

The source supports a safe, amp-1-exclusive flash. The only material difference from the
historically-proven fleet 122 route is substituting 123 (SELECT_SLAVE) for 122, keeping
the same preceding 107+100 global reset. Amp 1's RA0 pin being the sole pin pulled LOW
by the backplane slot-select — combined with the application-side `Startup_Machine==1`
gate — is a double isolation: amps 3/4/6 neither enter the bootloader nor accept the
bootloader-entry write.

## Evidence classification (honesty note)

- **Source-verified (software):** all PIC32 `slavefwhandler.c`/`mbus.c`/`enums.h`,
  PIC18 `main.c`/`pin_manager.h`, bootloader `pic18f_bootload.c`/`bootload.h`, and
  backplane `main.c` facts above (with file:line).
- **Corroborated but hardware-level (not net-traced in KiCad this session):** the
  physical wiring backplane `AMP_IDx_SET` output → amplifier `RA0` (ID pin). This is
  corroborated by (a) `ModBusSlaveMapping.txt:124-133` ("HoldingReg0 bit N = Slave N
  enable ... for configuring of ID"), (b) backplane `main.c:107-129` driving
  `AMP_IDx_SET_LAT` from `HoldingReg0`, (c) amplifier reading `ID_PORT=RA0` for Config
  entry, (d) bootloader reading `IO_PIN_ENTRY_PORT_PIN=RA0` for bootloader entry, and
  (e) physical evidence: amps 1/3/4/6 were correctly detected at addresses 1/3/4/6 in
  K8B/P2-B1 (which requires this exact slot-select → ID-pin path to have worked).
  The exact KiCad net label was not re-derived this session (the `.sch` uses
  hierarchical labels); flagging this as the one remaining non-source-verified link,
  mitigable by the planned PicoScope capture of the slot-select line before ERASE.
