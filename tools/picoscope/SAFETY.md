# SAFETY — picoscope measurement-only contract

The `tools/picoscope` package is **measurement-only**. It configures and reads a
PicoScope oscilloscope; it does not drive, program, or alter any device under
test.

## What this package DOES expose

* `discover_devices()` / `open_device()` / `close_device()`
* `get_device_info()`
* `configure_analog_channel(...)` — channel enable, voltage range, coupling
* `configure_digital_port(...)` — digital port enable, logic threshold
* `configure_trigger(...)` — trigger source / threshold / edge / pre-post split
* `capture_block(...)` — one bounded block capture
* `save_capture(...)` / `load_capture(...)` — persist / restore a capture

All of these operate on the *scope* (or the simulator). They never write to any
external system.

## What is explicitly NOT exposed

The package has no code path for, and its public surface intentionally omits:

* **AWG / arbitrary waveform generation / signal generation** (no `SetSigGen*`,
  `SetSignalGenerator`, `SetAWG`, `ArbitraryWaveform`, or equivalent)
* **hardware output commands** (no signal-output / drive / setpoint command)
* **firmware flashing / programming** (no `FlashProgram`, no bootloader write)
* **device reset / reconfiguration** of any target (no `ResetDevice`, no
  target-state change)
* **motion / actuator / locomotive commands** (no Siebwalde control-state change)
* **backplane / ModBus / PIC32-PIC18 control writes** of any kind
* **any write to the Siebwalde C#/firmware/ECoS/Koploper stack**

This absence is also enforced by a static test
(`tests/test_18_no_hardware_output.py`) that scans the public API and the
`ps3000a` adapter source for forbidden operation names and vendor API identifiers.

## Ownership

* `open_device()` returns an **exclusive in-process** handle. A second open of the
  same device raises `DeviceBusyError` — no two handles fight over one unit.
* Ownership is released deterministically on `close_device()` and on the
  context-manager exit path, including exception paths.
* The guard is process-local and has **no** system-wide process killer and no pid
  file. Cross-process collision (e.g. PicoScope 7 running concurrently) is
  addressed in PICO-2 (`PICO-2-PREFLIGHT.md`), not in PICO-1.

## Electrical safety (documented; the Product Owner owns physical work)

These notes are documentation, not authorization. Physical probing is a
Product-Owner activity and requires explicit `LIVE_HARDWARE` authority.

* Observe the 3406D MSO input limits: **±20 V max range**, **±100 V overvoltage
  protection** (per vendor spec; re-verify the exact model datasheet).
* Use a **common ground** between probes and the circuit under test before
  connecting signals.
* Account for probe loading (typical 1 MΩ ∥ ~14 pF); use 10× probes where
  appropriate and keep the configured probe attenuation consistent.
* Choose digital logic thresholds relative to the actual logic levels; an
  incorrect threshold produces misleading logic capture, not a fault.

## Authority

The `ps3000a` adapter is present but **not hardware-validated**. No live capture,
driver install, or hardware action is part of PICO-1. Physical capture is gated
behind PICO-2 and requires the Product Owner present plus `LIVE_HARDWARE`
authority. Software-only simulation evidence must never be presented as proof of
physical behavior.
