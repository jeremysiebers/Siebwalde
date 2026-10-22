"""Explicit acquisition configuration.

:class:`AcquisitionConfig` bundles everything needed to run one block capture:
analog channel configs, digital port configs, the trigger, the pre/post trigger
sample split, and the timebase (which resolves to a sample interval in ns).
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Optional

from .errors import InvalidConfigError
from .model import (
    AnalogChannelConfig,
    DigitalPortConfig,
    TriggerConfig,
)


@dataclass
class AcquisitionConfig:
    """A full block-capture request.

    ``pre_samples`` and ``post_samples`` are integer sample counts; together with
    ``sample_interval_ns`` they define the capture window. ``timebase`` is the
    backend timebase index (0 = fastest); ``sample_interval_ns`` is its resolved
    value in integer nanoseconds when known.
    """

    analog_channels: list = field(default_factory=list)
    digital_ports: list = field(default_factory=list)
    trigger: Optional[TriggerConfig] = None
    pre_samples: int = 0
    post_samples: int = 1000
    timebase: int = 0
    sample_interval_ns: Optional[int] = None

    def validate(self) -> None:
        """Reject structurally invalid configurations early and loudly."""
        if not isinstance(self.pre_samples, int) or self.pre_samples < 0:
            raise InvalidConfigError(
                f"pre_samples must be an int >= 0, got {self.pre_samples!r}"
            )
        if not isinstance(self.post_samples, int) or self.post_samples <= 0:
            raise InvalidConfigError(
                f"post_samples must be an int > 0, got {self.post_samples!r}"
            )
        if not isinstance(self.timebase, int) or self.timebase < 0:
            raise InvalidConfigError(f"timebase must be an int >= 0, got {self.timebase!r}")
        if self.sample_interval_ns is not None and (
            not isinstance(self.sample_interval_ns, int) or self.sample_interval_ns <= 0
        ):
            raise InvalidConfigError(
                f"sample_interval_ns must be a positive int when set, got {self.sample_interval_ns!r}"
            )

        for ch in self.analog_channels:
            ch.validate()
        for port in self.digital_ports:
            port.validate()
        if self.trigger is not None:
            self.trigger.validate()

        enabled_analog = [c.channel for c in self.analog_channels if c.enabled]
        enabled_lines = [
            line for p in self.digital_ports if p.enabled for line in p.lines
        ]
        if not enabled_analog and not enabled_lines:
            raise InvalidConfigError(
                "At least one analog channel or digital port must be enabled"
            )

        if self.trigger is not None and self.trigger.source != "EXTERNAL":
            if self.trigger.source not in enabled_analog and self.trigger.source not in enabled_lines:
                raise InvalidConfigError(
                    f"Trigger source {self.trigger.source!r} is not an enabled channel/line"
                )

    def enabled_analog_channels(self) -> list:
        return [c for c in self.analog_channels if c.enabled]

    def enabled_digital_ports(self) -> list:
        return [p for p in self.digital_ports if p.enabled]

    def enabled_digital_lines(self) -> list:
        lines = []
        for p in self.enabled_digital_ports():
            lines.extend(p.lines)
        return lines

    def add_analog_channel(self, channel: str, enabled: bool, range_v: float, coupling: str):
        cfg = AnalogChannelConfig(channel=channel, enabled=enabled, range_v=range_v, coupling=coupling)
        cfg.validate()
        # Replace an existing entry for the same channel (idempotent configure).
        self.analog_channels = [c for c in self.analog_channels if c.channel != channel] + [cfg]
        return cfg

    def add_digital_port(self, port: int, enabled: bool, threshold_mv: float):
        cfg = DigitalPortConfig(port=port, enabled=enabled, threshold_mv=threshold_mv)
        cfg.validate()
        self.digital_ports = [p for p in self.digital_ports if p.port != port] + [cfg]
        return cfg

    def set_trigger(
        self,
        source: str,
        threshold: float,
        edge: str,
        pre_samples: int,
        post_samples: int,
    ):
        trig = TriggerConfig(source=source, threshold=threshold, edge=edge)
        trig.validate()
        self.trigger = trig
        self.pre_samples = pre_samples
        self.post_samples = post_samples
        return trig
