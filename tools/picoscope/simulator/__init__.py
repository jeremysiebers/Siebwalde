"""Offline simulator fixtures (synthetic, deterministic signals).

Exposes the pure signal generators used by the simulated adapter and by the
offline tests. All functions are deterministic functions of their arguments and
share a single sample clock (``sample_interval_ns``).
"""

from .signals import (  # noqa: F401
    analog_dc,
    analog_pwm,
    analog_sine,
    build_signal,
    digital_edges,
    digital_square,
    digital_uart,
    time_axis_ns,
    time_axis_s,
)

__all__ = [
    "analog_dc",
    "analog_pwm",
    "analog_sine",
    "build_signal",
    "digital_edges",
    "digital_square",
    "digital_uart",
    "time_axis_ns",
    "time_axis_s",
]
