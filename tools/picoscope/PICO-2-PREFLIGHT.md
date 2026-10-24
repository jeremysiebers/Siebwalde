# PICO-2 Preflight — first physical 3406D MSO capture

**Status: plan only. DO NOT execute as part of PICO-1.** This document is the
preflight plan for the first physical capture on the real PicoScope 3406D MSO. It
requires the Product Owner present, a connected device, installed PicoSDK drivers,
and explicit `LIVE_HARDWARE` authority. Nothing here has been executed.

## Goal

Prove the `ps3000a` path end-to-end on hardware with a **safe known test signal**
(NOT the live Siebwalde controller): open the exact device, configure ≥1 analog
channel and ≥1 digital port, run one digital/edge-triggered block capture,
retrieve samples with a correct `timeIntervalNs`, save + reload (round-trip), and
close cleanly.

## Required SDK / driver versions

* PicoSDK drivers for the 3406D MSO (`ps3000a.dll` + `picoipp.dll`, 64-bit),
  installed by the vendor PicoSDK installer on first device connection. Download
  from picotech.com/downloads; verify the installed version against the vendor
  release notes.
* Windows 64-bit, Python 3.9+, NumPy. (The `picosdk` Python wrapper is **not**
  required by this package — the adapter binds `ps3000a.dll` directly via ctypes —
  but is a useful cross-check for the official example flow.)
* USB 3.0 port for SuperSpeed.

## Pre-checks

* `ps3000a.dll` and `picoipp.dll` present (e.g. `C:\Windows\System32`).
* PicoScope 7 **closed** (or otherwise confirmed not holding the unit) — the
  device is single-owner at the OS/driver level; the in-process guard in PICO-1
  does **not** cover cross-process collision. Verify with the vendor software
  that no other process owns the device.

## Probe precautions

* Confirm a **common ground** between the scope probes and the test signal source.
* Respect input limits (≤ ±20 V range; overvoltage protection ±100 V).
* Use 10× probes where appropriate; keep configured probe attenuation consistent
  with the physical probe.
* Connect the safe test signal (e.g. a signal generator / known PWM source) to an
  analog channel and a digital line — **not** the live Siebwalde controller.

## Procedure (planned)

1. Product Owner present; device connected; drivers installed; PicoScope 7 closed.
2. Connect the safe known test signal to analog channel A and digital port 0.
3. `discover_devices(backend="ps3000a")` → confirm the unit is listed with the
   expected serial and model `3406D MSO`.
4. `open_device("<serial>", backend="ps3000a")`.
5. `configure_analog_channel("A", True, <range>, "DC")`.
6. `configure_digital_port(0, True, <threshold_mv>)`.
7. `configure_trigger(...)` then `capture_block(...)`.
8. Verify `sample_interval_ns` (from `ps3000aGetTimebase2`) and sample counts.
9. `save_capture` then `load_capture` → confirm byte-identical round-trip.
10. `close_device()` → confirm the device is released and can be reopened.

## Expected outputs

* `discover_devices` returns a real device dict (serial, model `3406D MSO`,
  variant, driver version).
* `capture_block` returns a `Capture` with non-empty analog + digital arrays,
  a positive integer `sample_interval_ns`, `sample_count == pre + post`, and
  `acquisition_status == "OK"`.
* `save_capture`/`load_capture` round-trips with `equals() == True`.

## Stop conditions (abort immediately)

* No device found, or a device other than the 3406D MSO is found.
* Any `PICO_STATUS` error from `ps3000aOpenUnit`, `SetChannel`,
  `SetDigitalPort`, `GetTimebase2`, `RunBlock`, `IsReady`, or `GetValues`.
* `ps3000aIsReady` does not report ready within the bounded timeout.
* Probe ground not confirmed, or any suspected overvoltage/overload.
* Unexpected physical behavior of the test signal source.

## Known risks / uncertainties to close in PICO-2

* Exact digital threshold range/grouping and the `logic_level` unit semantics
  (the vendor example hardcodes `10000` while the spec lists ±5 V; the correct mV
  interpretation must be re-checked on-device).
* The D8–D15 port enable (`PS3000A_DIGITAL_PORT1 = 0x81`) behavior.
* The analog+digital simultaneous sampling-rate table (sample-rate drop with
  digital ports enabled).
* Cross-process ownership vs PicoScope 7.

## Evidence convention

The capture-evidence directory convention (`tools/picoscope/captures/` tracked vs
git-ignored) must be decided before the first real capture output is produced.
PICO-1 git-ignores `captures/`; decide whether PICO-2 artifacts are durable
tracked evidence.
