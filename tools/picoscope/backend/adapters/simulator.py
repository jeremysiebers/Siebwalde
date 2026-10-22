"""Deterministic simulated PicoScope backend.

Implements the same public surface as the ps3000a adapter so the rest of the
package (and the offline tests) run without any hardware or native driver. The
simulator is deterministic: identical configurations produce identical samples.

Fault injection (timeout / device error) is a simulator-only testing facility,
exposed as one-shot flags on :class:`SimulatedDevice`:

* :meth:`SimulatedDevice.inject_timeout` — the next ``capture_block`` raises
  :class:`DeviceTimeoutError`;
* :meth:`SimulatedDevice.inject_error` — the next ``capture_block`` raises
  :class:`DeviceError`.
"""

from __future__ import annotations

import uuid
from datetime import datetime, timezone

import numpy as np

from ..config import AcquisitionConfig
from ..errors import DeviceError, DeviceNotFoundError, DeviceTimeoutError
from ..model import (
    Capture,
    DeviceIdentity,
    digital_lines,
)
from ... import version as _version
from ...simulator.signals import build_signal

BACKEND_NAME = "simulator"
DRIVER_VERSION = "simulator-1.0.0"

#: Default simulated inventory (two devices so exclusivity can be exercised).
DEFAULT_DEVICES = (
    {
        "serial": "SIM0001",
        "model": "3406D MSO",
        "variant": "SIMULATOR",
        "driver_version": DRIVER_VERSION,
    },
    {
        "serial": "SIM0002",
        "model": "3406D MSO",
        "variant": "SIMULATOR",
        "driver_version": DRIVER_VERSION,
    },
)

#: Deterministic timebase index -> sample interval (integer nanoseconds).
#: timebase 0 = 2 ns mirrors the 3406D MSO fastest real-time rate (500 MS/s).
DEFAULT_TIMEBASE_NS = {0: 2, 1: 4, 2: 8, 3: 16, 4: 32, 5: 64, 6: 128, 7: 256}

#: Deterministic default signal spec per analog channel (sine, frequency varies
#: by channel index so multichannel captures are visibly distinct yet aligned).
_DEFAULT_ANALOG_SPEC = {
    "A": {"type": "sine", "freq_hz": 1000.0, "amplitude": 1.0, "offset": 0.0},
    "B": {"type": "sine", "freq_hz": 2000.0, "amplitude": 1.0, "offset": 0.0},
    "C": {"type": "sine", "freq_hz": 3000.0, "amplitude": 1.0, "offset": 0.0},
    "D": {"type": "sine", "freq_hz": 4000.0, "amplitude": 1.0, "offset": 0.0},
}

#: Deterministic default signal spec per digital line (square, frequency varies).
_DEFAULT_DIGITAL_SPEC = {
    line: {"type": "square", "freq_hz": 1000.0 + 500.0 * i, "duty": 0.5}
    for i, line in enumerate(digital_lines(0) + digital_lines(1))
}


class SimulatedDevice:
    """A single opened simulated scope handle."""

    def __init__(self, device_info: dict, adapter: "SimulatedAdapter"):
        self._info = dict(device_info)
        self._adapter = adapter
        self._config = AcquisitionConfig()
        self._signal_map = {}  # source name (A..D, D0..D15) -> signal spec dict
        self._inject_timeout = False
        self._inject_error = None
        self._closed = False

    @property
    def config(self) -> AcquisitionConfig:
        """The current (pending) acquisition configuration."""
        return self._config

    # -- identity -----------------------------------------------------------
    def get_device_info(self) -> DeviceIdentity:
        self._check_open()
        return DeviceIdentity(
            serial=self._info["serial"],
            model=self._info["model"],
            variant=self._info["variant"],
            driver_version=self._info["driver_version"],
            backend=BACKEND_NAME,
            extra={},
        )

    # -- configuration ------------------------------------------------------
    def configure_analog_channel(self, channel, enabled, range_v, coupling):
        self._check_open()
        cfg = self._config.add_analog_channel(channel, enabled, range_v, coupling)
        return cfg

    def configure_digital_port(self, port, enabled, threshold_mv):
        self._check_open()
        cfg = self._config.add_digital_port(port, enabled, threshold_mv)
        return cfg

    def configure_trigger(self, source, threshold, edge, pre_samples, post_samples):
        self._check_open()
        trig = self._config.set_trigger(source, threshold, edge, pre_samples, post_samples)
        return trig

    # -- simulator-only test hooks ------------------------------------------
    def set_signal(self, source: str, spec: dict):
        """Override the default signal for an analog channel or digital line."""
        self._signal_map[source] = dict(spec)

    def inject_timeout(self):
        """Make the *next* ``capture_block`` raise :class:`DeviceTimeoutError`."""
        self._inject_timeout = True

    def inject_error(self, message: str):
        """Make the *next* ``capture_block`` raise :class:`DeviceError`."""
        self._inject_error = message

    # -- acquisition ----------------------------------------------------------
    def capture_block(self, pre_samples=None, post_samples=None, timebase=None):
        self._check_open()

        if self._inject_error is not None:
            message = self._inject_error
            self._inject_error = None
            raise DeviceError(message)

        if self._inject_timeout:
            self._inject_timeout = False
            raise DeviceTimeoutError("Simulated capture readiness timed out")

        pre = self._config.pre_samples if pre_samples is None else pre_samples
        post = self._config.post_samples if post_samples is None else post_samples
        tb = self._config.timebase if timebase is None else timebase

        # Build a working config from the requested pre/post/timebase and validate.
        working = AcquisitionConfig(
            analog_channels=list(self._config.analog_channels),
            digital_ports=list(self._config.digital_ports),
            trigger=self._config.trigger,
            pre_samples=pre,
            post_samples=post,
            timebase=tb,
        )
        working.validate()

        interval_ns = self._adapter.resolve_timebase(tb)
        sample_count = pre + post

        analog_samples = {}
        for ch in working.enabled_analog_channels():
            spec = self._signal_map.get(ch.channel) or self._default_analog_spec(ch)
            analog_samples[ch.channel] = build_signal(spec, interval_ns, sample_count)

        digital_samples = {}
        for line in working.enabled_digital_lines():
            spec = self._signal_map.get(line) or _DEFAULT_DIGITAL_SPEC[line]
            digital_samples[line] = build_signal(spec, interval_ns, sample_count)

        trigger_position = pre if working.trigger is not None else None

        channel_mapping = {
            "analog": {ch.channel: i for i, ch in enumerate(working.analog_channels)},
            "digital": {line: i for i, line in enumerate(digital_lines(0) + digital_lines(1))},
        }

        now = datetime.now(timezone.utc)
        return Capture(
            capture_id=uuid.uuid4().hex,
            timestamp=now.isoformat().replace("+00:00", "Z"),
            device_identity=self.get_device_info(),
            analog_channels=list(working.analog_channels),
            digital_ports=list(working.digital_ports),
            sample_interval_ns=interval_ns,
            sample_count=sample_count,
            trigger=working.trigger,
            trigger_position=trigger_position,
            channel_mapping=channel_mapping,
            acquisition_status="OK",
            errors=[],
            warnings=[],
            backend_version=DRIVER_VERSION,
            software_version=_version.__version__,
            provenance={
                "backend": BACKEND_NAME,
                "simulated": True,
                "hardware_validated": False,
                "repo_head": _version.git_commit(),
            },
            analog_samples=analog_samples,
            digital_samples=digital_samples,
        )

    # -- teardown --------------------------------------------------------------
    def close(self):
        self._closed = True

    def _check_open(self):
        if self._closed:
            raise DeviceError("Simulated device is already closed")

    @staticmethod
    def _default_analog_spec(channel_cfg):
        # Scale the default sine amplitude to the configured range.
        spec = dict(_DEFAULT_ANALOG_SPEC[channel_cfg.channel])
        spec["amplitude"] = float(channel_cfg.range_v) * 0.4
        return spec


class SimulatedAdapter:
    """Factory + timebase table for simulated devices."""

    BACKEND_NAME = BACKEND_NAME

    def __init__(self, devices=None, timebase_ns=None):
        self._devices = list(devices) if devices is not None else list(DEFAULT_DEVICES)
        self._timebase_ns = dict(timebase_ns) if timebase_ns is not None else dict(DEFAULT_TIMEBASE_NS)

    def discover(self) -> list:
        return [dict(d) for d in self._devices]

    def resolve_timebase(self, timebase: int) -> int:
        if timebase not in self._timebase_ns:
            raise DeviceError(f"Simulated timebase index {timebase} is not supported")
        return int(self._timebase_ns[timebase])

    def open(self, device_id) -> SimulatedDevice:
        for d in self._devices:
            if d["serial"] == device_id:
                return SimulatedDevice(d, self)
        raise DeviceNotFoundError(f"Simulated device {device_id!r} not found")

    def close(self, device: SimulatedDevice):
        device.close()


#: Module-level default simulator used by the public API when no adapter is
#: supplied. Tests may construct their own :class:`SimulatedAdapter` to avoid
#: sharing state.
default_adapter = SimulatedAdapter()
