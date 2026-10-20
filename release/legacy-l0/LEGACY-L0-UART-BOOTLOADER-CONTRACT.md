# LEGACY-L0-UART-BOOTLOADER-CONTRACT — 9-bit Modbus / 8-bit bootloader

This is a protected compatibility contract. Any future Phase-1 increment that modifies
these areas must be flagged for explicit PO architecture review. L0 does not change them.

Authoritative source: legacy `928ea7c` (TrackController5 = PIC32 master; TrackAmplifier4.X = PIC18; TrackBackplane2.X).

## 1. Normal application communication (9-bit Modbus)

- PIC32 UART2 is **9-bit** (`U2MODEbits.PDSEL = 0x3`, no parity) — `slavefwhandler.c:48`.
- **Address-marked master transmission:** the address byte carries the ninth bit set; data bytes carry the ninth bit clear.
  - `modbus/PetitModbusPort.c:62-65` `PetitModBus_UART_PutchAddr()` → `UART2_Write9(0x0100u | addr)`.
  - `modbus/PetitModbusPort.c:57-60` `UART2_Write9(b)` for data (ninth bit 0).
- **Master RX reads 9 bits but ignores the mark bit** on receive — `system_interrupt.c:77-80` (`U2RXREG & 0x01FF`), `modbus/PetitModbusPort.c:98-114` (`(void)isAddress`).
- **PIC18 addressed reception:** RX is 9-bit address-detect (`RC1STA = 0xD8`) — `TrackAmplifier4.X/mcc_generated_files/eusart.c:81-82`.
- **PIC18 reply is 8-bit** (`TX9 = 0`, `TX9D = 0`) — `TrackAmplifier4.X/modbus/PetitModbusPort.c:18-24` `EUSART1_Write9()`, hardware init `eusart.c:84-85` (`TX1STA = 0x24`).
  - This **asymmetry** (master 9-bit RX vs amplifier 8-bit TX) is the historically working behavior. Do NOT change it.
- **Modbus response processing / fault handling:** `PetitModbus.c` / `PetitModbusPort.c`; the UART fault path is `_UART2_FAULT_VECTOR` (`system_interrupt.c:130-143`).

## 2. Bootloader communication (8-bit)

- **PIC18 bootloader UART format is 8-bit**, no Modbus address byte; the bootloader stream begins with `bootloader_start_byte = 0x55` (assigned at `slavebootloaderroutines.c:50` et al., transmitted in `SendDataToBootloader()` `:734-774`).
- **PIC32 transition to 8-bit:** `UART2_Config_8bit_Bootloader()` (`PDSEL = 0x0`) — `slavefwhandler.c:64-80`.
- **Exact transition point (FlashSequencer order):**
  1. `Slaves_Disable_Off()` (power the bus) — `slavefwhandler.c:863`.
  2. `fwData.SlaveBootloaderHandlingActive = false;` + `UART2_Config_9bit_Modbus()` — `slavefwhandler.c:884-885`.
  3. **Backplane select:** write HR0 = `SLOT1 << ShiftSlot1` (one-hot) — `slavefwhandler.c:902-907`.
  4. **Bootloader entry:** write HR11 = 0 to `SLAVE_INITIAL_ADDR` (0xAA) — `slavefwhandler.c:937-942`.
  5. **Backplane deselect:** write HR0 = 0 — `slavefwhandler.c:970-975`.
  6. `fwData.SlaveBootloaderHandlingActive = true;` + `UART2_Config_8bit_Bootloader()` — `slavefwhandler.c:985-986`.
  7. **Bootloader ops:** `GET_BOOTLOADER_VERSION` / `ERASE_FLASH` / `WRITE_FLASH` / `WRITE_CONFIG` / `CHECK_CHECKSUM` / `RESET_SLAVE` — `slavefwhandler.c:1003-1088`.
  8. **Return to 9-bit:** `SlaveBootloaderHandlingActive = false;` + `UART2_Config_9bit_Modbus()` — `slavefwhandler.c:1091-1100`.
- The `SlaveBootloaderHandlingActive` flag gates the RX interrupt (9-bit read + `ReceiveInterrupt` when false; 8-bit read + `SLAVE_BOOTLOADER_DATA_RETURN` when true) — `system_config/Production/system_interrupt.c:87-127`; it also halts `PROCESS_PETIT_MODBUS()` while in bootloader mode — `mbus.c:314-316`.

## 3. Backplane AMP_ID selection

- Backplane mapping: amps 1–10 → BP51, 11–20 → BP52, 21–30 → BP53, 31–40 → BP54, 41–50 → BP55; slot `ShiftSlot1 = (slave-1) % 10` — `slavefwhandler.c:886-900`.
- Selection is **one-hot** (`SLOT1 = 0x1`, `SLOT1 << ShiftSlot1`); deselect is HR0 = 0 before the next operation — `slavefwhandler.c:902-907`, `:970-975`, and everywhere (`DetectSlave` `slavestartup.c:276-289`, `ConfigureSlave` `slavestartup.c:516-529`, `SelectSlave` `slavefwhandler.c:1266-1279`).
- Never more than one amplifier selected for a bootloader response at a time.

## 4. FlashAllSlavesAuto selection contract

- Selection predicate (the ONLY flash gate): `MASTER_SLAVE_DATA[SlaveId1].SlaveDetected == true && MASTER_SLAVE_DATA[SlaveId1].HoldingReg[11] != fwData.fwchecksum` — `slavefwhandler.c:751`.
- Walks amplifiers 1..50 sequentially; the intended prototype population is 1/3/4/6.
- ⚠ Known legacy behavior: `FlashAllSlavesAuto` returns `DONE` even when it skips all amplifiers (`slavefwhandler.c:780-811`). A `DONE` ack is NOT proof of flashing; per-amp fresh HR11 readback is the only proof.

## 5. Firmware checksum contract

- Application checksum = 16-bit little-endian word-sum over the image, stored at `0x7FFE/0x7FFF` (app `0x0800–0x7FFF`, checksum slot excluded) — `slavefwhandler.c:571-611`, `TrackAmplifierBootLoaderHelpers.cs`, `bootload.py:27-59`. Legacy = `0x251F`.

## 6. Protected-code inventory (flag any future modification for PO review)

| Area | Paths / functions |
|---|---|
| PIC32 UART config (9/8-bit) | `slavefwhandler.c` `UART2_Config_9bit_Modbus()` / `UART2_Config_8bit_Bootloader()` |
| PIC18 UART config | `TrackAmplifier4.X/modbus/PetitModbusPort.c` `EUSART1_Write9()`; `mcc_generated_files/eusart.c` |
| Peripheral clock / baud | `system_config.h` (PBCLK/PB2DIV); `drv_usart` / UART2 baud generator |
| Modbus frame parsing | `modbus/PetitModbus.c`, `modbus/PetitModbusPort.c` |
| UART interrupt handling | `system_config/Production/system_interrupt.c` (`_UART2_FAULT_VECTOR`, RX dispatch) |
| FlashSequencer / 8↔9-bit switching | `slavefwhandler.c` `FlashSequencer()` / `SelectSlave()` / mode helpers |
| Backplane selection | `slavefwhandler.c` HR0 one-hot select/deselect; `slavestartup.c` `DetectSlave()`/`ConfigureSlave()` |
| Bootloader protocol | `slavebootloaderroutines.c` (8-bit stream, erase/write/config/checksum/reset) |
| Firmware checksum | `slavefwhandler.c` `FwFileDownload()`; `bootload.py` `GetFileCheckSum()` |
