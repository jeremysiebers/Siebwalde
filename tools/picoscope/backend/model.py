"""Core data model: device identity, channel/port/trigger configs and Capture.

This module defines the value objects shared by the public API, the simulator
and the (PICO-2) ps3000a adapter. Units are fixed and documented per field:

* analog ranges and trigger thresholds: volts (``float``);
* digital-port logic thresholds: millivolts (``float``);
* sample intervals: integer nanoseconds (``int``);
* sample counts / trigger position: integer sample indices (``int``).
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Optional

import numpy as np

from .errors import InvalidConfigError

#: Analog channel letters for the 3406D MSO.
ANALOG_CHANNELS = ("A", "B", "C", "D")

#: Digital ports: 0 -> D0..D7, 1 -> D8..D15.
DIGITAL_PORTS = (0, 1)

#: Number of digital lines per port.
LINES_PER_PORT = 8

#: Allowed analog couplings.
VALID_COUPLINGS = ("DC", "AC")

#: Allowed trigger edges.
VALID_EDGES = ("rising", "falling", "either")

#: Plausible digital logic threshold window (millivolts), per the ps3000a
#: digital-port spec (~ +/-5 V). Strictly validated offline in PICO-1; the exact
#: on-device range is re-confirmed in PICO-2.
DIGITAL_THRESHOLD_MV_MIN = 0.0  # exclusive: must be > 0
DIGITAL_THRESHOLD_MV_MAX = 5000.0  # inclusive


def digital_lines(port: int) -> tuple[str, ...]:
    """Return the digital line names for a port (``D0..D7`` or ``D8..D15``)."""
    if port not in DIGITAL_PORTS:
        raise InvalidConfigError(f"Invalid digital port {port!r}; expected 0 or 1")
    start = port * LINES_PER_PORT
    return tuple(f"D{i}" for i in range(start, start + LINES_PER_PORT))


@dataclass
class AnalogChannelConfig:
    """Configuration of one analog channel (A..D)."""

    channel: str
    enabled: bool
    range_v: float
    coupling: str

    def validate(self) -> None:
        if self.channel not in ANALOG_CHANNELS:
            raise InvalidConfigError(
                f"Invalid analog channel {self.channel!r}; expected one of {ANALOG_CHANNELS}"
            )
        if self.coupling not in VALID_COUPLINGS:
            raise InvalidConfigError(
                f"Invalid coupling {self.coupling!r}; expected one of {VALID_COUPLINGS}"
            )
        if not isinstance(self.range_v, (int, float)) or not np.isfinite(self.range_v):
            raise InvalidConfigError(f"Analog range_v must be a finite number, got {self.range_v!r}")
        if self.range_v <= 0:
            raise InvalidConfigError(f"Analog range_v must be > 0, got {self.range_v!r}")


@dataclass
class DigitalPortConfig:
    """Configuration of one 8-line digital port (0 -> D0..D7, 1 -> D8..D15)."""

    port: int
    enabled: bool
    threshold_mv: float

    def validate(self) -> None:
        if self.port not in DIGITAL_PORTS:
            raise InvalidConfigError(
                f"Invalid digital port {self.port!r}; expected one of {DIGITAL_PORTS}"
            )
        if not isinstance(self.threshold_mv, (int, float)) or not np.isfinite(self.threshold_mv):
            raise InvalidConfigError(
                f"Digital threshold_mv must be a finite number, got {self.threshold_mv!r}"
            )
        if not (DIGITAL_THRESHOLD_MV_MIN < self.threshold_mv <= DIGITAL_THRESHOLD_MV_MAX):
            raise InvalidConfigError(
                f"Digital threshold_mv must be in ({DIGITAL_THRESHOLD_MV_MIN}, "
                f"{DIGITAL_THRESHOLD_MV_MAX}], got {self.threshold_mv!r}"
            )

    @property
    def lines(self) -> tuple[str, ...]:
        return digital_lines(self.port)


@dataclass
class TriggerConfig:
    """Trigger condition.

    ``source`` is an analog channel (``A``..``D``), a digital line (``D0``..``D15``)
    or ``"EXTERNAL"``. ``threshold`` is in volts. ``edge`` is one of the valid edges.
    """

    source: str
    threshold: float
    edge: str

    def validate(self) -> None:
        if self.edge not in VALID_EDGES:
            raise InvalidConfigError(
                f"Invalid trigger edge {self.edge!r}; expected one of {VALID_EDGES}"
            )
        if not isinstance(self.threshold, (int, float)) or not np.isfinite(self.threshold):
            raise InvalidConfigError(f"Trigger threshold must be a finite number, got {self.threshold!r}")
        valid_sources = set(ANALOG_CHANNELS) | set(digital_lines(0)) | set(digital_lines(1)) | {"EXTERNAL"}
        if self.source not in valid_sources:
            raise InvalidConfigError(f"Invalid trigger source {self.source!r}")


@dataclass
class DeviceIdentity:
    """Identity of the device that produced a capture."""

    serial: str
    model: str
    variant: str
    driver_version: str
    backend: str
    extra: dict = field(default_factory=dict)


@dataclass
class Capture:
    """A complete block capture plus all provenance metadata.

    Raw samples live in ``analog_samples`` (channel letter -> ``float64`` volts
    array) and ``digital_samples`` (line name ``D0..D15`` -> ``uint8`` 0/1
    array). Every array has length ``sample_count`` and shares
    ``sample_interval_ns``.
    """

    capture_id: str
    timestamp: str  # UTC ISO 8601
    device_identity: DeviceIdentity
    analog_channels: list
    digital_ports: list
    sample_interval_ns: int
    sample_count: int
    channel_mapping: dict
    acquisition_status: str
    backend_version: str
    software_version: str
    analog_samples: dict
    digital_samples: dict
    trigger: Optional[TriggerConfig] = None
    trigger_position: Optional[int] = None
    errors: list = field(default_factory=list)
    warnings: list = field(default_factory=list)
    provenance: dict = field(default_factory=dict)

    def _meta_equal(self, other: "Capture") -> bool:
        if not isinstance(other, Capture):
            return False
        return (
            self.capture_id == other.capture_id
            and self.timestamp == other.timestamp
            and self.device_identity == other.device_identity
            and self.analog_channels == other.analog_channels
            and self.digital_ports == other.digital_ports
            and self.sample_interval_ns == other.sample_interval_ns
            and self.sample_count == other.sample_count
            and self.channel_mapping == other.channel_mapping
            and self.acquisition_status == other.acquisition_status
            and self.backend_version == other.backend_version
            and self.software_version == other.software_version
            and self.trigger == other.trigger
            and self.trigger_position == other.trigger_position
            and self.errors == other.errors
            and self.warnings == other.warnings
            and self.provenance == other.provenance
        )

    def equals(self, other: "Capture") -> bool:
        """Full structural equality including raw sample arrays.

        Used for the serialize/load round-trip equality contract. Dataclass
        ``__eq__`` is deliberately replaced so numpy arrays compare correctly.
        """
        if self is other:
            return True
        if not self._meta_equal(other):
            return False
        if set(self.analog_samples) != set(other.analog_samples):
            return False
        if set(self.digital_samples) != set(other.digital_samples):
            return False
        for key in self.analog_samples:
            if not np.array_equal(self.analog_samples[key], other.analog_samples[key]):
                return False
        for key in self.digital_samples:
            if not np.array_equal(self.digital_samples[key], other.digital_samples[key]):
                return False
        return True

    def __eq__(self, other: object) -> bool:
        if not isinstance(other, Capture):
            return NotImplemented
        return self.equals(other)

    def __ne__(self, other: object) -> bool:
        result = self.__eq__(other)
        if result is NotImplemented:
            return result
        return not result

    def summary(self) -> dict:
        """Compact measurement metadata (no raw sample arrays)."""
        return {
            "capture_id": self.capture_id,
            "timestamp": self.timestamp,
            "device": {
                "model": self.device_identity.model,
                "serial": self.device_identity.serial,
                "backend": self.device_identity.backend,
            },
            "sample_interval_ns": self.sample_interval_ns,
            "sample_count": self.sample_count,
            "analog_channels": [
                {
                    "channel": c.channel,
                    "enabled": c.enabled,
                    "range_v": c.range_v,
                    "coupling": c.coupling,
                }
                for c in self.analog_channels
            ],
            "digital_ports": [
                {"port": p.port, "enabled": p.enabled, "threshold_mv": p.threshold_mv}
                for p in self.digital_ports
            ],
            "trigger": None if self.trigger is None else {
                "source": self.trigger.source,
                "threshold": self.trigger.threshold,
                "edge": self.trigger.edge,
            },
            "trigger_position": self.trigger_position,
            "acquisition_status": self.acquisition_status,
            "errors": list(self.errors),
            "warnings": list(self.warnings),
        }
