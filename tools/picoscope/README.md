# picoscope — measurement-only PicoScope tooling (Layer A + simulator)

A small, well-separated Python measurement package for the PicoScope **3406D MSO**
(4 analog + 16 digital channels, `ps3000a` driver). **PICO-1 is offline-only**:
the default backend is a deterministic simulator. The real `ps3000a` backend is
present but **not hardware-validated** and requires the PicoSDK drivers (PICO-2).

No hardware, no drivers, no MCP, no OpenCode, no Siebwalde C#/firmware are needed
to run the simulator, the demo, or the tests.

## Layout

```
tools/picoscope/
  backend/          Layer A — measurement backend (no MCP dependency)
    api.py          discover_devices / open_device / PicoScope handle
    config.py       AcquisitionConfig
    model.py        Capture + channel/port/trigger/device value objects
    serialize.py    save_capture / load_capture (.npz + sidecar JSON)
    ownership.py    in-process ownership guard
    errors.py       exception taxonomy
    adapters/
      picosdk.py    vendor ps3000a ctypes adapter (PICO-2, NOT validated)
      simulator.py  deterministic simulated adapter
  simulator/        synthetic signal fixtures (sine/PWM/DC, square/edges/UART)
  tests/            offline pytest suite (18 categories)
  demo.py           standalone simulated capture + save/reload round-trip
  README.md         this file
  SAFETY.md         measurement-only safety contract
  PICO-2-PREFLIGHT.md  plan for the first physical 3406D capture
```

## Install (simulation only)

Requires Python 3.9+ and NumPy (pytest for the tests). Nothing else.

```powershell
# From the repository root:
python -m venv tools/picoscope/.venv
tools/picoscope/.venv/Scripts/python.exe -m pip install -r tools/picoscope/requirements.txt
# (tests only)
tools/picoscope/.venv/Scripts/python.exe -m pip install pytest
```

Or use any Python with NumPy installed. There is no package-install step; the
package is imported directly from the repository.

## Quick start (simulated)

```python
from tools.picoscope.backend.api import discover_devices, open_device

for d in discover_devices():            # backend="simulator" by default
    print(d["serial"], d["model"])

with open_device("SIM0001") as scope:   # exclusive handle, deterministic cleanup
    scope.configure_analog_channel("A", True, range_v=5.0, coupling="DC")
    scope.configure_digital_port(0, True, threshold_mv=1000)   # D0..D7
    scope.configure_digital_port(1, True, threshold_mv=1200)   # D8..D15
    scope.configure_trigger("A", 0.0, "rising", pre_samples=2000, post_samples=6000)
    capture = scope.capture_block()
    scope.save_capture("captures/demo")
```

### Demo

```
python -m tools.picoscope.demo
# or
python tools/picoscope/demo.py
```

The demo opens the simulated scope, configures ≥1 analog + ≥1 digital port,
captures, saves, reloads, prints compact metadata, and exits with deterministic
cleanup (device closed, ownership released, temp files removed). Requires only
NumPy.

## API

### `discover_devices(backend="simulator") -> list[dict]`

Returns `{serial, model, variant, driver_version}` dicts.

### `open_device(device_id, backend="simulator") -> PicoScope`

Opens an **exclusively owned** handle. Raises `DeviceBusyError` if the device is
already owned in-process.

### `PicoScope`

* `get_device_info() -> DeviceIdentity`
* `configure_analog_channel(channel, enabled, range_v, coupling)` — channel `A..D`, coupling `DC|AC`
* `configure_digital_port(port, enabled, threshold_mv)` — port `0` (D0–D7) / `1` (D8–D15)
* `configure_trigger(source, threshold, edge, pre_samples, post_samples)` — `edge` in `rising|falling|either`
* `capture_block(pre_samples=None, post_samples=None, timebase=None) -> Capture`
* `save_capture(path)` / `load_capture(path)` — remember/restore the current capture
* `close_device()` — idempotent close + ownership release
* context-manager (`with`) and `closed` / `config` / `last_capture` / `device` properties

### Module-level helpers

`save_capture(capture, path)` / `load_capture(path)` are also available at module
level.

### Backends

| name | meaning |
|---|---|
| `simulator` | deterministic simulated device (default) |
| `ps3000a` / `picosdk` / `real` | real vendor driver (PICO-2, NOT validated) |

A real-device request that fails raises (`BackendUnavailableError`,
`DeviceNotFoundError`, `DeviceError`) — it **never** silently falls back to the
simulator.

## Units

* analog ranges / trigger thresholds: **volts**
* digital thresholds: **millivolts**
* sample intervals: **integer nanoseconds**
* sample counts / trigger position: **integer sample indices**
* timestamps: **UTC ISO 8601**

## Capture format

`save_capture(capture, path)` writes two files (see `backend/serialize.py`):

* `path.npz` — raw samples (NumPy): `analog_<CH>` float64 volts, `digital_<LINE>` uint8 0/1
* `path.json` — self-describing sidecar: `format_version`, `capture_id`, `timestamp`,
  `device_identity`, channel/port/trigger config, `sample_interval_ns` (int),
  `sample_count` (int), `trigger_position` (int), `channel_mapping`,
  `acquisition_status`, `errors`/`warnings`, versions, `provenance`, and a
  `samples` descriptor (dtype/shape per array; values live only in the npz).

`load_capture(path)` reconstructs a `Capture` that is `equals()`-equal to the
original (round-trip equality).

## Ownership

`backend/ownership.py` keeps a **process-local** registry (device id → opaque
token). Opening an owned device raises `DeviceBusyError`. Release is
deterministic and token-scoped: `close_device()` (and the context-manager exit,
including exception paths) releases exactly the acquired token. There is no
system-wide process killer and no pid file. Cross-process collision (e.g. vs
PicoScope 7) is a PICO-2 concern — see `PICO-2-PREFLIGHT.md`.

## Measurement-only safety

See `SAFETY.md`. In short: the package exposes read/configure/capture/save/load
only. It exposes **no** AWG / signal-generation, no hardware output command, no
flash, no reset, no motion, no backplane, no Siebwalde control-state change.

## Tests

```
tools/picoscope/.venv/Scripts/python.exe -m pytest tools/picoscope/tests -q
```

The suite covers 18 categories (discovery, open/close, exclusive ownership,
analog config, both digital port groups, digital threshold validation, trigger
config, block capture, sample count/interval integrity, multichannel alignment,
serialization, reload equality, timeout, device error, cleanup-after-exception,
invalid-config rejection, backend selection without fallback, absence of any
hardware-output surface).

## Known limitations

* The real `ps3000a` adapter is **source-backed but not hardware-validated** —
  it has never run against a physical 3406D MSO. Its call flow is transcribed
  from the official `ps3000aBlockMSOExample.py` and `picosdk.ps3000a` enums.
* Exact digital threshold range/grouping, the D8–D15 port enable, and the
  analog+digital simultaneous sampling-rate table must be re-confirmed on-device
  in PICO-2 (the simulator validates `0 < threshold_mv <= 5000` only).
* Cross-process device ownership (vs PicoScope 7) is not handled in PICO-1.
