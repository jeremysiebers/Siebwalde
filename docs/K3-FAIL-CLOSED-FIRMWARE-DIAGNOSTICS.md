# K3 — Fail-Closed Firmware Diagnostics and Protocol Verification

Date: 2026-10-23. OFFLINE_ONLY. This increment corrects the unsafe
firmware-download behavior exposed by the PICO-5B incident. No hardware command,
flash, reset, recovery, live acquisition or firmware/C# change was performed.

The corrected tooling is the offline single-target flash driver
`TrackController5/Python/flash_one.py` (and its unittest suite
`TrackController5/Python/test_flash_one.py`).

## 1. Protocol and acknowledgement matrix

Reconstructed from `TrackController5/firmware/src/slavefwhandler.c`,
`slavebootloaderroutines.c` and `enums.h` (PIC32 firmware), and the driver.
"Host ACK" = the PIC32 sends the host a status frame that proves completion.
"Externally verifiable" = the result is observable by a later read-only command
or readback, independent of the ack.

| Cmd | Name | Host request | PIC32 internal op | Success ack (taskid/command/state) | Error/timeout | NOK | Host ACK | Externally verifiable |
|---|---|---|---|---|---|---|---|---|
| 250 | CLIENT_CONNECTION_REQUEST | `AA FA 00 55` | controller CONNECT | CONTROLLER(10)/CONNECTED(6)/DONE(7) | — | — | yes | — |
| 103 | SLAVE_FW_FLASH | `AA 67 00 55` | enter FW handler | FWHANDLER(30)/FWHANDLERINIT(31)/CONNECTED(6) | — | — | yes | — |
| 120 | RECEIVE_FW_FILE | `AA 78 00 55` | FwFileDownload | FWFILEDOWNLOAD(32)/140/DONE | RECEIVED_WRONG_COMMAND | — | yes | — |
| 141 | FW_DATA ×N | `AA 8D <64B>` | FwFileDownload | FWFILEDOWNLOAD(32)/140/DONE per packet | — | — | yes | — |
| (last 141) | | | | FWFILEDOWNLOAD/143 + RECEIVED_CHECKSUM_OK(104) | RECEIVED_CHECKSUM_NOK(105) | — | yes | — |
| 121 | RECEIVE_CONFIG_WORD | `AA 79 00 55` | ConfigWordDownload | FWCONFIGWORDDOWNLOAD(33)/150/DONE | — | — | yes | — |
| 151 | FW_CONFIG_WORD | `AA 97 <12B>` | ConfigWordDownload | FWCONFIGWORDDOWNLOAD/152/DONE | — | — | yes | — |
| 123 | SELECT_SLAVE | `AA 7B <id> 55` | SelectSlave (force bootloader) | **none** (LOG_Push only) | — | — | **no** | no (pre-ERASE only) |
| 124 | GET_BOOTLOADER_VERSION | `AA 7C 00 55` | GETxBOOTxLOADERxVERSION | GET_BOOTLOADER_VERSION(192)/194/DONE | 193 (timeout) / 180/181 | 195 | yes | no (never observed working) |
| 125 | ERASE_FLASH | `AA 7D 00 55` | ERASExFLASH | ERASE_FLASH(200)/202/DONE | 201 (timeout) / 180/181 | 203 | yes | checksum→0xFFFF (bootloader re-entry) |
| 126 | WRITE_FLASH | `AA 7E 00 55` | WRITExFLASH | WRITE_FLASH(210)/212/DONE | 211 (timeout) / 180/181 | 213 | yes | via 128 checksum |
| 127 | WRITE_CONFIG | `AA 7F 00 55` | WRITExCONFIG | WRITE_CONFIG(220)/222/DONE | **211 (bug, see below)** / 180/181 | 223 | yes | via 128 checksum |
| 128 | CHECK_CHECKSUM | `AA 80 00 55` | CHECKxCHECKSUM | CHECK_CHECKSUM_CONFIG(230)/232/DONE | 231 (timeout) / 180/181 | 233 | yes | the checksum match itself |
| 129 | SLAVE_RESET | `AA 81 00 55` | RESETxSLAVE | RESET_SLAVE(240)/242/DONE | 241 (timeout) / 180/181 | 243 | yes | app restart (9-bit Modbus) |
| 130 | EXIT_SLAVExFWxHANDLER | `AA 82 00 55` | exit handler | **none** (state-only) | — | — | **no** | no |
| 100/106/101 | SLAVES_ON / START_DATA_UPLOAD / SLAVE_DETECT | — | MBUS | MBUS(20)/state/DONE | — | — | yes | SLAVEINFO HR readback |

Bootloader internal timeouts (PIC32→PIC18, source): ERASE/WRITE/WRITE_CONFIG/
CHECK_CHECKSUM use a 1 s window; RESET uses 100 ms. On expiry the routine emits
`*_RECEIVE_DATA_TIMEOUT` with `taskstate=ERROR`.

### Confirmed firmware bug (WRITE_CONFIG timeout code)

`WRITExCONFIG` (`slavebootloaderroutines.c` timeout branch) reports
`WRITE_FLASH_RECEIVE_DATA_TIMEOUT` (211) as its TASK_COMMAND instead of
`WRITE_CONFIG_RECEIVE_DATA_TIMEOUT` (221). Its LOG line correctly says
"WRITE_CONFIG_RECEIVE_DATA_TIMEOUT" but the emitted code is 211. The corrected
timeout-code set is therefore **201 / 211 / 211 (for 127) / 231 / 241**. This
bug is diagnostic-only: K3 aborts on *any* `taskstate==ERROR`, so it does not
affect fail-closed behavior.

## 2. Fail-closed state contract

`FlashState` (in `flash_one.py`):

| State | Meaning | Evidence level reached |
|---|---|---|
| `PRE_ERASE` | before command 125 | nothing destructive sent |
| `ERASE_REQUESTED_UNCONFIRMED` | 125 sent, no ack yet | command sent |
| `ERASE_CONFIRMED` | 125 acked `*_RETURNED_OK` | command acknowledged |
| `WRITE_IN_PROGRESS` | 126 sent | command sent |
| `PROGRAMMING_UNCONFIRMED` | 126/127/128/129 in flight, not yet verified | command(s) sent/acked |
| `VERIFIED_COMPLETE` | 128 checksum OK + 129 reset OK | operation verified |
| `FAILED_UNKNOWN_HARDWARE_STATE` | any timeout / ERROR / missing ack on a destructive command | unknown — fail closed |

Rules:

- A transition to a higher state requires the matching `*_RETURNED_OK`/`DONE`
  ack. "Command sent", "command acknowledged", "physical response observed"
  and "operation verified" are distinct evidence levels and are never
  automatically promoted.
- Any timeout, `*_RETURNED_NOK`, `BOOTLOADER_START_BYTE_ERROR`,
  `BOOTLOADER_DATA_RECEIVE_ERROR`, unexpected frame, or missing ack on a
  destructive command (125-129) moves the state to
  `FAILED_UNKNOWN_HARDWARE_STATE` and raises `FlashAbort`. The driver never
  continues to the next destructive stage on an unverified success assumption.
- No auto-retry, no auto-reset, no auto-recovery.
- The removed `--proceed-on-timeout` (ASSUME-OK) behavior is gone; a
  `*_RECEIVE_DATA_TIMEOUT` is **never** converted into a successful operation.

## 3. PICO-5B incident evidence assessment

Reviewed against the preserved PicoScope capture (`0b20f400…`) and the live
host log recorded in `docs/PICO-5B-PHYSICAL-FLASH-VALIDATION.md`.

| Question | Assessment |
|---|---|
| PIC32 transmitted a bootloader request? | **PROVEN_HARDWARE** — the D1 scope capture shows the 9→8-bit UART switch and the 8-bit ERASE frame `0x55 0x03 0xE0 0x01 0x55 0xFF` after 123. |
| Correct UART mode was active? | **PROVEN_HARDWARE** — the 9→8-bit transition is captured. |
| PIC18 received or responded to the request? | **UNKNOWN** — no capture of a PIC18 reply; the PIC18 was not independently observed. |
| The response returned to the PIC32? | **UNKNOWN** — the host saw only `*_RECEIVE_DATA_TIMEOUT` (201/211), which means the PIC32 did not receive a bootloader reply in time. |
| RS-485 hardware failed? | **NOT CONCLUDED** — host timeouts alone do not prove a hardware fault. The source-established, better-supported explanation is that the PIC18 never entered bootloader (123 writes HR11=0 to the boot address 0xAA, which a running amplifier drops because it listens on its assigned address; only the PIC32 was reset, not the PIC18). This remains `INFERRED`. |

Last actually-verified state before the destructive step: the host→PIC32
firmware download completed (`RECEIVED_CHECKSUM_OK`); `SELECT_SLAVE` (123) was
sent; the ERASE frame was emitted on the wire in 8-bit mode. The target
amplifier's application state after the attempt is **UNKNOWN** (never
independently re-verified).

## 4. Remaining unsupported / unverifiable protocol operations

- **Bootloader entry** (123 → the PIC18 actually entering bootloader) has no
  host acknowledgement; it is only inferable from a later 125-129 ack or a
  scope observation. The driver does not treat "123 sent" as "bootloader
  entered".
- **124 GET_BOOTLOADER_VERSION** is source-validated but has never returned a
  reply on the prototype; it is therefore not usable as a bootloader-presence
  probe and is not emitted by the driver.
- **Physical target isolation** cannot be enforced in software: the 8-bit
  bootloader protocol is unaddressed, so exclusive-target erase/write requires
  physically unplugging non-target amplifiers (a human/operator precondition,
  not a driver guarantee).
- **Completion verification without a bootloader ack**: if the prototype
  PIC18's RS-422 reply path prevents 202/212/222/232/242 from reaching the
  PIC32, then even a genuinely-executed ERASE/WRITE cannot be confirmed by the
  current protocol; the driver then fails closed (FAILED_UNKNOWN_HARDWARE_STATE)
  rather than inventing success. Closing this gap would require firmware or
  hardware change, which is out of K3 scope.

## 5. Future Diagnostics MCP compatibility

The corrected driver exposes a typed, machine-readable state (`FlashOneDriver.state`
→ `FlashState` string) and raises `FlashAbort` with an explicit reason; the
state machine distinguishes sent / acknowledged / verified and is independent
of any MCP. A future Diagnostics Core can consume `state` + the `*_RETURNED_OK`/
timeout classification without importing the MCP, and must not expose raw
unrestricted hardware commands to AI agents.
