# PICOSCOPE PICO-4 — Offline Signal Analysis (Layer B)

Date: 2026-10-22. **OFFLINE** implementation of the Layer B signal-analysis
library (UART decoder + PWM/timing), reusable Siebwalde capture profiles,
deterministic synthetic fixtures, and a read-only MCP analysis tool. No
hardware, no real capture, no firmware/flash/motion/Siebwalde change.

## 1. Branch + scope

- Branch: `feature/picoscope-pico-4` (branched from `feature/picoscope-pico-3`
  @ `48a6ed4`).
- Adds `tools/picoscope/analysis/`, `tools/picoscope/profiles/`,
  `tools/picoscope/tests_analysis/`, a read-only `scope_analyze_capture` MCP
  tool, and this document plus the hardware test procedure
  (`docs/PICOSCOPE-PICO-4-HARDWARE-TEST-PROCEDURE.md`).
- Fixes the PICO-3 interval-independent budget so it is computed from the
  **effective** trigger pre/post split, not the top-level pre/post
  (`tools/picoscope/mcp/service.py`).

## 2. Exact legacy UART framing (evidence-graded)

The directive referenced `docs/LEGACY-L0-UART-BOOTLOADER-CONTRACT.md`. **That
file does not exist in the repository.** The framing below is therefore derived
from the actual firmware source (the authoritative reference) plus the existing
historical decoder, and is graded PROVEN/UNVERIFIED.

### 2.1 PROVEN (source) — master (PIC32) request is 9-bit address-marked

- `TrackController5/firmware/src/slavefwhandler.c`
  `UART2_Config_9bit_Modbus()` sets `U2MODEbits.PDSEL = 0x3` (**9-bit, no
  parity**) and `STSEL = 0` (**1 stop bit**). `UART2_Config_8bit_Bootloader()`
  sets `PDSEL = 0x0` (**8-bit**).
- `TrackController5/firmware/src/modbus/PetitModbusPort.c`
  `UART2_Write9()` writes `w & 0x01FF` to `U2TXREG`.
  `PetitModBus_UART_PutchAddr(addr)` = `UART2_Write9(0x0100u | addr)`
  (**ninth bit = 1**); `PetitModBus_UART_PutchData(b)` = `UART2_Write9((uint16_t)b)`
  (**ninth bit = 0**). `PetitModBus_UART_String()` sends the first byte (Modbus
  address) with the ninth bit set and every following byte with it cleared.
- The master ignores the ninth bit on RX (`ReceiveInterrupt(data, isAddress)`
  discards `isAddress`; the comment states "master does not use 9th bit on RX").

### 2.2 PROVEN (source) — slave (PIC18) reply is 8-bit (TX9=0)

- `TrackAmplifier4.X/mcc_generated_files/eusart.c` `EUSART_Initialize()`:
  `RC1STA = 0xD8` (SPEN + **RX9** + **ADDEN** + CREN) → RX is 9-bit with
  address detect; `TX1STA = 0x24` (**TX9=0**, TXEN=1) → **TX is 8-bit**;
  `BAUD1CON = 0x08` (BRG16=1); `SP1BRGL = 0x07`, `SP1BRGH = 0x00` (BRG=7);
  BRGH hi-speed.
- `TrackAmplifier4.X/modbus/PetitModbusPort.c` `EUSART1_Write9(b, ninthBit)`
  hardcodes `TX9D = 0` and `TX9 = 0` (the `ninthBit` argument is commented out),
  and `EUSART1_WriteData(b)` calls `EUSART1_Write9(b, false)`. So the PIC18
  transmits **8-bit frames with no ninth bit**.
- `TrackAmplifier4.X/mcc_generated_files/interrupt_manager.c`
  `INTERRUPT_InterruptManager` reads `RC1STAbits.RX9D` (ninth bit) then `RCREG`,
  calls `ReceiveInterrupt(data, ninth)`, and the TMR3 inter-character timeout
  re-arms `ADDEN`. `PetitModbusPort.c` `ReceiveInterrupt(data, ninthBit)` accepts
  an address byte only when `ADDEN` is set and the address matches the slave or
  broadcast address.

### 2.3 PROVEN (source) — bootloader is 8-bit, 0x55-prefixed

- PIC32: `UART2_Config_8bit_Bootloader()` (PDSEL=0x0, 1 stop bit).
- PIC18 bootloader: `TrackAmplifierBootLoader.X/mcc_generated_files/eusart.c`
  `RC1STA = 0x90` (RX9=0, ADDEN=0), `TX1STA = 0x24` (TX9=0) → 8-bit both ways.
- `TrackController5/firmware/src/slavebootloaderroutines.c` uses a `0x55` start
  byte and an `0xAA 0x55` unlock sequence; bootloader frames carry no amplifier
  address per frame (see also
  `docs/firmware-flash-recovery-technical-reference.md` §…-bit contracts).

### 2.4 PROVEN (historical capture) — request 9-bit / response 8-bit

`tools/hmo2024/uart/decoder.py` + `README.md` (the P2-A0 reference capture):
D6 = REQUEST (9-bit, 11 bits/frame), D7 = RESPONSE (8-bit, 10 bits/frame). The
CRC is the project's bit-reversed Modbus CRC-16/MODBUS (`bit_reverse16(crc16_modbus(payload))`,
low byte first): request `04 03 00 08 00 02` → `39 A2`, response
`04 03 04 D3 98 00 00` → `1A 68`. Measured ~520 ns (request) / ~495 ns
(response) per bit at 100 ns sampling.

### 2.5 UNVERIFIED

- Physical wiring of D6/D7 to the actual RS-422 request/reply lines.
- Absolute bit position (which wire is D0).
- The exact `Fosc` → baud derivation (BRG=7 gives 2.0 Mbaud at Fosc=64 MHz; the
  2.0161 Mbaud figure is the hardware-observed value, not source-proven).
- The `docs/LEGACY-L0-UART-BOOTLOADER-CONTRACT.md` contract itself (absent).

## 3. UART decoder — API and limitations

Module: `tools/picoscope/analysis/uart.py`.

- `UartConfig(mode, baud, sample_interval_ns, polarity="idle_high",
  lsb_first=True, stop_bits=1, mode_source="explicit", idle_gap_bits=None)` —
  `mode` is required (`"8bit"`/`"9bit"`); the decoder **never guesses** it.
- `decode_uart(samples, config) -> dict` — edge extraction, start-bit detection,
  bit-center sampling (majority-of-3), stop-bit validation, frame-error and
  truncated-frame reporting, optional idle-gap grouping, timestamped words, and
  an `uncertainty` list.
- Per frame: `value` (D0..D7), `ninth_bit` (separate flag; `None` in 8-bit mode),
  and `raw_value` (`(ninth<<8)|value`) — the full 9-bit value is preserved
  alongside the separate ninth-bit flag.
- `decode_uart_8bit` / `decode_uart_9bit` — convenience wrappers.
- `compare_tx_rx(tx_frames, rx_frames, window_ns=None)` — TX/RX timing deltas on
  a shared sample grid (advisory; not a round-trip claim).
- `extract_edges(samples)` — rising/falling edge indices.

**Mode discipline:** the caller passes an explicit mode or a verified mode
marker. `mode_source="explicit"` reports an `uncertainty` note ("mode not
independently verified"); `mode_source="verified_marker"` does not. Decoding an
8-bit reply in 9-bit mode yields a spurious constant ninth=1 (it samples the stop
bit) — the reason guessing is forbidden (matches the HMO decoder finding).

**Limitations:** bit-center sampling is single-grid (majority of 3 adjacent
samples); timing is sample-resolution limited (±1 sample on every edge) and not
calibrated. The decoder does not verify Modbus CRC or group 9-bit telegrams by
the address marker (that is a higher-layer concern, available in
`tools/hmo2024/uart/`); it only groups by idle gap.

## 4. PWM + timing — API and limitations

Module: `tools/picoscope/analysis/timing.py`.

- `analyze_pwm(samples, sample_interval_ns, *, domain="digital", threshold=None)`
  → `PwmMeasurement` (frequency, period, duty, high/low pulse durations,
  rising/falling edge timestamps, complete-cycle count, period/duty jitter,
  `resolution_note`).
- `measure_edges(samples, sample_interval_ns, *, domain, threshold)` — edge
  timestamps + counts only.
- `cross_channel_delta(edges_a, edges_b, sample_interval_ns)` — nearest-edge
  cross-channel time differences.

**Digital vs analog:** `domain="digital"` treats the line as already
thresholded 0/1 (scope threshold). `domain="analog"` derives high/low from a
threshold crossing (default mid-point) on a float64 volt array; analog duty is
threshold-dependent. Both are **sample-resolution limited**, not calibrated.

## 5. Sampling strategy (2.016 Mbaud / ~496 ns/bit)

Bit cell = 1/2.016e6 ≈ 496 ns. Recommended sample intervals (from the PICO-2
hardware table):

| Interval | timebase | samples/bit | note |
|---|---|---|---|
| 64 ns | tb10 | ~7.8 | the old default; marginal for UART |
| 16 ns | tb4 | ~31 | **recommended** UART interval |
| 8 ns | tb3 | ~62 | both-digital floor (PICO-2 §8); not assumed for all channel combos |

Recommend **16 ns (tb4)** as the robust UART default (ample ~31 samples/bit, and
not the both-digital-only floor). 8 ns (tb3) is available for both-digital-port
captures (~62 samples/bit) but MUST NOT be assumed for analog+digital channel
combinations — select the interval from `ps3000aGetTimebase2` for the actual
channel combination. 64 ns is deprecated for UART decode.

## 6. Analyzer API (MCP + library)

### 6.1 Library

`tools/picoscope/analysis/` (standalone; NumPy only; no MCP dependency):

- `uart.decode_uart` / `uart.UartConfig` / `uart.compare_tx_rx`
- `timing.analyze_pwm` / `timing.measure_edges` / `timing.cross_channel_delta`
- `io.extract_digital_line(capture, line)` / `io.extract_analog_channel(capture, channel)`
  (raise `MissingChannel` when absent) / `io.export_analysis_json(payload, path)`
  / `io.jsonable(obj)`
- `fixtures` — deterministic synthetic UART/PWM fixtures (16 UART categories +
  PWM; see `tools/picoscope/tests_analysis/`).

### 6.2 MCP tool `scope_analyze_capture` (read-only)

`scope_analyze_capture(request)` where `request = {capture_id, analyzer,
settings}`. `analyzer` ∈ `{"uart", "pwm_timing"}` (strict allowlist); `settings`
is validated per analyzer against a strict key allowlist. It loads the capture's
`.npz` + sidecar JSON, runs the analyzer, saves detailed evidence as
`<capture_id>.analysis.<analyzer>.json` inside the evidence dir, and returns a
**compact summary** + the analysis path. No device, hardware, arbitrary
path/script, or SDK function. (Exposes the MCP surface from four to five tools;
the PICO-3 documentation remains a historical record of the four-tool state.)

- `uart` settings: `line` (D0..D15), `mode` (`8bit`/`9bit`), `baud`; optional
  `polarity`, `stop_bits`, `mode_source`, `idle_gap_bits`.
- `pwm_timing` settings: `source` (A..D or D0..D15), optional `domain`,
  `threshold` (analog only).

## 7. Siebwalde capture profiles

`tools/picoscope/profiles/siebwalde.py` (config only, no execution):

- **A "legacy Modbus UART"** — PIC32 TX (9-bit) / PIC18 TX (8-bit), response
  timing, idle gaps. 16 ns, 2.016 Mbaud, trigger on D6 falling.
- **B "bootloader transition"** — TX/RX + RESET + AMP_ID, explicit 9→8-bit
  transition.
- **C "amplifier PWM"** — PWM output (analog) + enable/brake timing.

Channel/line assignments (D6/D7, RESET/AMP_ID, PWM/enable/brake) are recorded as
**PROPOSALS**, not physical-wiring proof.

## 8. Test totals (offline, full pytest)

- `tools/picoscope/tests` (Layer A regression): **123 passed**.
- `tools/picoscope/tests_analysis` (Layer B): **53 passed**.
- `tools/picoscope/tests_mcp` (Layer C, incl. budget-fix + analyze tool): **59 passed**.

## 9. GO/NO-GO

**GO.** The offline Layer B analysis library, fixtures, profiles, budget fix and
read-only analysis tool are implemented and self-verified offline. No hardware
was touched. The future controlled hardware measurement is documented (not
executed) in `docs/PICOSCOPE-PICO-4-HARDWARE-TEST-PROCEDURE.md`.
