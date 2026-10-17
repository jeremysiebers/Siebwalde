# Siebwalde offline 9-bit UART decoder (`tools/hmo2024/uart/`)

Software/tooling only.  Decodes the captured POD digital channels of the
Siebwalde amplifier digital bus (P2-A0 capture set) into UART frames and
Modbus-RTU telegrams, and verifies the project's **bit-reversed Modbus
CRC-16/MODBUS** on-wire convention.  No production code, no firmware, no flash,
no live hardware, no train commands.

## What it decodes

From a `pod1.raw` (6000 comma-separated 8-bit POD values, one per sample at
~100 ns) plus the shared `ch1.header.raw` time base:

* `D6` = **REQUEST** direction (PIC32 master's transmitted request) — **9-bit
  UART**, 11 bits/frame `[START][D0..D7][D8=address marker][STOP]`.
* `D7` = **RESPONSE** direction (PIC18 slave's response) — **8-bit UART**,
  10 bits/frame `[START][D0..D7][STOP]` (no 9th bit).

> **Verified, not assumed.**  The task brief described 9-bit framing for both
> directions; the firmware and the captured data show the response is 8-bit.
> See `Framing evidence` below.

## Layout

| File | Responsibility |
|---|---|
| `crc.py` | standard Modbus CRC-16/MODBUS + 16-bit bit-reversal + on-wire (lo,hi) convention |
| `decoder.py` | POD/header/CH1 readers, bit-period measurement, frame/telegram decode, CRC verify, report |
| `cli.py` | `python -m uart.cli ...` entry point (text or `--json`) |
| `tests/` | `unittest` fixtures: CRC, 9-bit, 8-bit, malformed CRC, incomplete frame, bit-jitter, real capture |

## Usage

```text
cd tools/hmo2024
python -m uart.cli captures/REF-AMP01-003-DIGITAL/pod1.raw \
    captures/REF-AMP01-003-DIGITAL/ch1.header.raw \
    --ch1 captures/REF-AMP01-003-DIGITAL/ch1.raw          # optional correlation
python -m uart.cli ... --json                              # machine-readable
```

Run the tests (no scope attached):

```text
cd tools/hmo2024
python -m unittest discover -s uart/tests -t . -v
```

## CRC convention (confirmed against the capture)

On-wire CRC = `bit_reverse16(crc16_modbus(payload))`, sent **low byte first,
high byte second**, where `payload` = every byte except the two trailing CRC
bytes.  This matches both firmware builds (`TrackAmplifier4.X` PIC18 `CRC_HW`
no-reverse and `TrackController5` PIC32 `CRC_LOOKUP`+`CRC_HW_REVERSE`) and the
captured bytes:

* Request `04 03 00 08 00 02` → CRC `39 A2` (standard would be `45 9C`).
* Response `04 03 04 D3 98 00 00` → CRC `1A 68` (standard would be `16 58`).

## Framing evidence (request 9-bit / response 8-bit)

* **Master (PIC32)** `TrackController5/firmware/src/slavefwhandler.c`:
  `UART2_Config_9bit_Modbus()` sets `PDSEL=0b11` (9-bit, no parity), and
  `modbus/PetitModbusPort.c` `PetitModBus_UART_PutchAddr` sends the address byte
  with the 9th bit set.  → request is 9-bit with an address marker.
* **Slave (PIC18)** `TrackAmplifier4.X/modbus/PetitModbusPort.c`
  `EUSART1_Write9` hardcodes `TX9D=0; TX9=0`, and `mcc_generated_files/eusart.c`
  records `TX9 8-bit`.  → the slave transmits 8-bit (no 9th bit).
* **Captured data** confirms it: in the response, the run following the
  function byte's D0,D1 is 30 samples = 6 bits (D2..D7) with **no D8 bit**, and
  a 9-bit reading of the response yields a spurious constant D8=1 (it samples
  the stop bit).

## Timing / uncertainty

* Sample period from the header: `dt ≈ 100.0 ns` (10 MSa/s).
* Nominal bit period: 496.0 ns (~4.96 samples) at the stated 2.0161 Mbaud.
* Measured (reference capture): request ≈ 5.2 samples (≈ 520 ns), response ≈
  5.0 samples (≈ 495 ns) — the master runs ~5% slower than the slave, which is
  within UART receiver tolerance but worth noting.
* Quantization: ±1 sample = ±100 ns on any single edge; the bit-center sampling
  point (majority of 3 adjacent samples) has ±50 ns placement uncertainty.  At
  ~5 samples/bit this is ~20% of a bit on a single bit, but the full-telegram
  CRC still validates, proving the decode succeeded for this clean capture.

## Sufficiency assessment (5 samples/bit)

5 samples/bit at 100 ns was **sufficient for this specific clean capture** — the
request and response both decode to exactly one CRC-`VALID` telegram with no
bit errors.  However it is **marginal**: a single bit is only ~5 samples, and
edge jitter of ±1 sample is ~±20% of a bit, so bit-center placement has little
margin.  Combined with the observed ~5% master/slave baud mismatch, a noisier or
longer capture could cross into bit errors/ambiguous framing.  For robust
decoding of real-world traffic a higher-resolution single acquisition is
recommended (e.g. 100 MSa/s → 10 ns/sample ≈ 50 samples/bit over a shorter
window).

## CH1 correlation

When `ch1.raw` is supplied, each decoded frame is time-stamped on the same
header grid as CH1 and the nearest CH1 threshold crossing is reported.  This is
**same-acquisition shared-time-base** only — no cross-acquisition or physical
claims are made.
