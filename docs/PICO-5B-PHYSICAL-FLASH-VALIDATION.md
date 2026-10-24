# PICO-5B — Physical Flash Validation (live outcome)

Date: 2026-10-23. This is the recorded outcome of the supervised single-target
bootloader flash attempt that `docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md`
prepared. It is the committed provenance anchor for the PICO-5B observations
and the incident in the Knowledge Foundation seed.

**No further flash, reset, recovery, power-cycle or state-changing hardware
operation is authorized.** The flash tooling must not be used for further live
tests until its fail-closed behaviour is independently verified.

## 1. What was executed

The Python single-target driver (`flash_one.py`, branch
`feature/legacy-l2-stage-a-driver`) ran the host→PIC32 firmware download and the
manual single-target primitive sequence against the prototype:

```
250 CLIENT_CONNECTION_REQUEST  -> CONNECTED
103 SLAVE_FW_FLASH             -> FWHANDLERINIT
120 RECEIVE_FW_FILE            -> FWFILEDOWNLOAD/140 STANDBY
141 FW_DATA x480               -> 143 + RECEIVED_CHECKSUM_OK (~1.3 s upload)
121 RECEIVE_CONFIG_WORD        -> 150
151 config word (12 B)         -> 152
123 SELECT_SLAVE               -> (no host ack)
125 ERASE_FLASH                -> RECEIVE_DATA_TIMEOUT (201)
126 WRITE_FLASH                -> RECEIVE_DATA_TIMEOUT (211)
127 WRITE_CONFIG               -> WRITE_FLASH_RECEIVE_DATA_TIMEOUT (211, not 221)
```

The upload (host→PIC32) completed with `RECEIVED_CHECKSUM_OK`. The destructive
commands 125/126/127 each timed out with **no bootloader acknowledgement**.

## 2. Scope-verified observation

The **9→8-bit UART mode switch** was physically captured on the PicoScope D1
line (scope capture `0b20f4005903b43ec12369105852a34a`, 16 ns/sample): after
123 SELECT_SLAVE, the PIC32 switched from 9-bit to 8-bit and emitted
`0x55 0x03 0xE0 0x01 0x55 0xFF` (the ERASE command). This proves the
mode-switching contract and the 8-bit ERASE frame on the wire; it does **not**
prove a bootloader response.

## 3. Why the ERASE did not complete (working explanation)

Command 123 `SELECT_SLAVE` writes `HR11=0` to the boot-init address `0xAA`, but
a running amplifier listens on its assigned Modbus address, so the write is
dropped unless the PIC18 is reset to the boot address. Only the PIC32 was reset,
not the PIC18s. This matches the source-established requirement that command 131
needs a global reset (`EXEC_MBUS_STATE_RESET` 107 + `SLAVES_ON` 100) first
(`docs/firmware-flash-recovery-technical-reference.md` §2.5). This is the best
supported explanation (**inferred**, not directly captured).

## 4. The ASSUME-OK continuation (flagged unsafe)

The driver was run with `--proceed-on-timeout`, which treats the 125–129
`*_RECEIVE_DATA_TIMEOUT` codes as non-fatal and continues past an unconfirmed
ERASE/WRITE. This is **unsafe**: a timeout is not confirmation that a
destructive command executed. No `ASSUME-OK` may be accepted for an unconfirmed
destructive operation. This behaviour is flagged in the Knowledge Foundation as
`safety:no-assume-ok`, and the `--proceed-on-timeout` decision is recorded as
`CONTRADICTED` (superseded by the containment directive).

## 5. Firmware state after the attempt

- The **target amplifier's application state is UNKNOWN**: ERASE/WRITE were sent
  without confirmed bootloader acknowledgements, and the state has not been
  independently re-verified. (The preparation names amplifier address 3; the
  live session referred to amplifier 1. Both are recorded as UNKNOWN pending
  independent re-verification.)
- Amplifiers 4 and 6 remain on legacy `0x251F` (not targeted).
- The operator reported all amplifiers start normally and none is stuck in
  bootloader — consistent with ERASE not having executed, but not a substitute
  for independent re-verification.

## 6. Proven vs unproven

- **PROVEN (hardware):** the 9→8-bit transition and the 8-bit ERASE frame on the
  wire (scope capture `0b20f400…`); the host→PIC32 upload completing with
  `RECEIVED_CHECKSUM_OK`; the 125/126/127 timeout responses (201/211).
- **INFERRED:** the reason the ERASE never executed (PIC18 not power-cycled).
- **UNKNOWN:** the target amplifier's current application state.
