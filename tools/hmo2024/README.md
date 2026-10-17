# HMO2024 measurement CLI (Phase-2 A0 tooling)

A small, auditable Python 3 / pyserial capture CLI for the **HAMEG HMO2024**
oscilloscope. This is **software/tooling only** — no production code, no
firmware, no flash. It talks to the scope over a VCP serial link
(`COM3` @ `115200 8N1`, LF terminator).

## Scope / authority

* `discover` / `inventory` / `verify` are **query-only or offline**.
* `capture` is an **explicit action** that arms a single acquisition; it
  requires `--confirm`, refuses to arm if the scope is already running (it
  never silently STOPS an operator's run), and never silently resumes (`RUN`)
  after completion.
* `export` only exports an **already-held** record and never rearms.

## Prerequisites

* Python 3.9+ (developed on 3.13)
* `pyserial` (`pip install pyserial`) — only needed for live `discover` /
  `inventory` / `capture` / `export`; the offline tests and `verify` do not
  need it.

## Usage

```
python tools/hmo2024/hmo_cli.py --help
python tools/hmo2024/hmo_cli.py --version

# Query-only: identify the scope among candidate ports
python tools/hmo2024/hmo_cli.py discover --ports COM3,COM5

# Query-only: read instrument state (no settings changed)
python tools/hmo2024/hmo_cli.py inventory --port COM3

# EXPLICIT ACTION: bounded single acquisition (requires --confirm)
python tools/hmo2024/hmo_cli.py capture --port COM3 --confirm --out-dir tmp

# Export an already-held record WITHOUT rearming
python tools/hmo2024/hmo_cli.py export --port COM3 --record tmp/<capture-id>

# OFFLINE integrity checks + raw->time + measurements (no instrument)
python tools/hmo2024/hmo_cli.py verify --record tmp/<capture-id>
python tools/hmo2024/hmo_cli.py verify --record tmp/<capture-id> --scale 0.5
```

Run the offline tests (no scope attached):

```
cd tools/hmo2024
python -m unittest discover -s tests
```

## Modules

| Module | Responsibility |
|---|---|
| `transport.py` | framed serial transport; definite-length block parsing; bounded timeouts; flush; guaranteed close |
| `scpi.py` | SCPI command registry (verified/empty status) + response parsers |
| `inventory.py` | READ-ONLY instrument state snapshot |
| `acquisition.py` | bounded single-acquisition state machine (explicit action) |
| `records.py` | capture-id, manifests, atomic writes, hashes, timestamps |
| `export.py` | export a held record without rearming (rearm detection) |
| `analyze.py` | OFFLINE integrity checks, raw->time, measurements with uncertainty |
| `cli.py` / `hmo_cli.py` | CLI entry point |

## SCPI command status

`VERIFIED_QUERY` / `VERIFIED_EMPTY` statuses come from the live COM3
characterization. `MANUAL_*` are documented in the HMO72x/202x SCPI
Programmer's Manual but not re-verified live in this increment.

| Short form | Long form | Status | Role |
|---|---|---|---|
| `*IDN?` | `*IDN?` | VERIFIED_QUERY | identity |
| `*OPC?` | `*OPC?` | VERIFIED_QUERY | completion |
| `ACQ:STAT?` | `ACQuire:STATe?` | VERIFIED_QUERY | acquisition-state |
| `ACQ:SRAT?` | `ACQuire:SRATe?` | VERIFIED_QUERY | sample-rate |
| `ACQ:POIN?` | `ACQuire:POINts?` | VERIFIED_EMPTY | acquisition-points (use `CHAN1:DATA:POIN?`) |
| `TIM:SCAL?` | `TIMebase:SCALe?` | VERIFIED_QUERY | timebase |
| `TIM:POS?` | `TIMebase:POSition?` | VERIFIED_QUERY | timebase |
| `TRIG:A:MODE?` | `TRIGger:A:MODE?` | VERIFIED_QUERY | trigger |
| `TRIG:A:SOUR?` | `TRIGger:A:SOURce?` | VERIFIED_QUERY | trigger |
| `TRIG:A:TYPE?` | `TRIGger:A:TYPE?` | VERIFIED_QUERY | trigger |
| `TRIG:A:LEV?` | `TRIGger:A:LEVel1:VALue?` | VERIFIED_QUERY | trigger |
| `TRIG:A:EDGE?` | `TRIGger:A:EDGE?` | VERIFIED_EMPTY | trigger (no bare EDGE query) |
| `CHAN<m>:STAT?` | `CHANnel<m>:STATe?` | VERIFIED_QUERY | channel |
| `CHAN<m>:COUP?` | `CHANnel<m>:COUPling?` | VERIFIED_QUERY | channel |
| `CHAN<m>:SCAL?` | `CHANnel<m>:SCALe?` | VERIFIED_QUERY | channel |
| `CHAN<m>:OFFS?` | `CHANnel<m>:OFFSet?` | VERIFIED_QUERY | channel |
| `CHAN1:PROB?` | `CHANnel1:PROBe?` | VERIFIED_EMPTY | probe (use `PROB1:SET:ATT?`) |
| `PROB<m>:SET:ATT?` | `PROBe<m>:SETup:ATTenuation[:AUTO]?` | VERIFIED_QUERY | probe |
| `POD1:THR?` | `POD<p>:THReshold?` | VERIFIED_QUERY | logic-pod |
| `BUS1:TYPE?` | `BUS<b>:TYPE?` | VERIFIED_QUERY | bus |
| `BUS2:TYPE?` | `BUS<b>:TYPE?` | VERIFIED_QUERY | bus |
| `BUS2:UART:BAUD?` | `BUS<b>:UART:BAUDrate?` | VERIFIED_QUERY | uart |
| `BUS2:UART:SSIZ?` | `BUS<b>:UART:SSIZe?` | VERIFIED_QUERY | uart |
| `BUS2:UART:PAR?` | `BUS<b>:UART:PARity?` | VERIFIED_QUERY | uart |
| `BUS2:UART:SBIT?` | `BUS<b>:UART:SBIT?` | VERIFIED_QUERY | uart |
| `BUS2:UART:SOUR?` | `BUS<b>:UART:SOURce?` | VERIFIED_EMPTY | uart (use `BUS2:UART:DATA:SOUR?`) |
| `BUS2:UART:DATA:SOUR?` | `BUS<b>:UART:DATA:SOURce?` | VERIFIED_QUERY | uart |
| `CHAN1:DATA?` | `CHANnel<m>:DATA?` | VERIFIED_QUERY | waveform (ASCII, LF-terminated) |
| `CHAN1:DATA:HEAD?` | `CHANnel<m>:DATA:HEADer?` | VERIFIED_QUERY | waveform header |
| `CHAN1:DATA:POIN?` | `CHANnel<m>:DATA:POINts?` | VERIFIED_QUERY | point count |
| `HCOP:DATA?` | `HCOPy:DATA?` | VERIFIED_QUERY | hardcopy (binary block, BMP) |
| `SINGle` | `SINGle` / `RUNSingle` | MANUAL_EVENT | acquisition-control |
| `RUN` | `RUN` / `RUNContinous` | MANUAL_EVENT | acquisition-control |
| `STOP` | `STOP` | MANUAL_EVENT | acquisition-control |
| `ACQ:STAT` | `ACQuire:STATe` | MANUAL_SETTING | acquisition-control |

## Measurement / uncertainty notes

* `verify` recomputes every artifact SHA-256 and compares it to the manifest;
  it checks header parse, header-vs-sample count consistency, empty/impossible
  artifacts, BMP magic, and rearm flags.
* `--scale <V/div>` enables measurements. The documented uncertainty model is
  half a quantization step of the 8-bit ADC: `full_scale = scale*8`,
  `step = full_scale/256`, `uncertainty = step/2`. The vertical scale is taken
  from the recorded inventory (not independently re-measured) and **physical
  accuracy is not claimed from software evidence**.

## Time sources

The manifest records **host UTC** and **host Europe/Amsterdam** local time
separately. The scope clock is recorded as a **different, unverified** source:
the HMO2024 has no verified SCPI clock query in this characterization, so
`scope_time` is `null` with status `NOT_QUERIED` rather than a guessed value.

## Evidence directory

Captures are written under `tools/hmo2024/tmp/` by default (git-ignored).
`RecordManager.new_capture_dir` always creates a fresh, unique directory and
raises `FileExistsError` rather than overwriting existing evidence.
