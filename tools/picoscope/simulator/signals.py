"""Synthetic signal fixtures for the offline simulator (deterministic).

Every generator takes ``sample_interval_ns`` (integer nanoseconds) and ``n``
(integer sample count) and returns a NumPy array of length ``n``. Timing is
therefore always self-consistent: sample ``i`` is at ``i * sample_interval_ns``
nanoseconds.

Analog generators return ``float64`` volt arrays; digital generators return
``uint8`` 0/1 arrays.
"""

from __future__ import annotations

import numpy as np


def time_axis_ns(sample_interval_ns: int, n: int) -> np.ndarray:
    """Sample timestamps in nanoseconds (``n`` points, step ``sample_interval_ns``)."""
    return np.arange(n, dtype=np.float64) * float(sample_interval_ns)


def time_axis_s(sample_interval_ns: int, n: int) -> np.ndarray:
    """Sample timestamps in seconds (``n`` points)."""
    return time_axis_ns(sample_interval_ns, n) * 1e-9


def analog_sine(
    freq_hz: float,
    amplitude: float,
    offset: float,
    sample_interval_ns: int,
    n: int,
    phase_deg: float = 0.0,
) -> np.ndarray:
    """Deterministic sine: ``offset + amplitude * sin(2*pi*f*t + phase)``."""
    t = time_axis_s(sample_interval_ns, n)
    return offset + amplitude * np.sin(2.0 * np.pi * freq_hz * t + np.deg2rad(phase_deg))


def analog_pwm(
    freq_hz: float,
    duty: float,
    high_v: float,
    low_v: float,
    sample_interval_ns: int,
    n: int,
    phase_deg: float = 0.0,
) -> np.ndarray:
    """Deterministic PWM (square) signal: ``high_v`` for ``duty`` fraction of period."""
    if not (0.0 < duty < 1.0):
        raise ValueError(f"duty must be in (0,1), got {duty!r}")
    t = time_axis_s(sample_interval_ns, n)
    period_s = 1.0 / freq_hz
    phase_s = (phase_deg / 360.0) * period_s
    pos = np.mod(t + phase_s, period_s)
    return np.where(pos < duty * period_s, high_v, low_v)


def analog_dc(level_v: float, sample_interval_ns: int, n: int) -> np.ndarray:
    """Stable DC level (``level_v`` volts at every sample)."""
    return np.full(n, float(level_v), dtype=np.float64)


def digital_square(
    freq_hz: float,
    sample_interval_ns: int,
    n: int,
    duty: float = 0.5,
    phase_deg: float = 0.0,
) -> np.ndarray:
    """Deterministic digital square wave (0/1)."""
    if not (0.0 < duty < 1.0):
        raise ValueError(f"duty must be in (0,1), got {duty!r}")
    t = time_axis_s(sample_interval_ns, n)
    period_s = 1.0 / freq_hz
    phase_s = (phase_deg / 360.0) * period_s
    pos = np.mod(t + phase_s, period_s)
    return (pos < duty * period_s).astype(np.uint8)


def digital_edges(edge_sample_indices, n: int, initial: int = 0) -> np.ndarray:
    """Digital waveform with known toggling edges at the given sample indices.

    Each index in ``edge_sample_indices`` flips the level. Rising/falling edges
    therefore occur exactly at those indices (starting from ``initial``).
    """
    out = np.full(n, int(initial), dtype=np.uint8)
    level = int(initial)
    for idx in edge_sample_indices:
        if not 0 <= idx < n:
            raise ValueError(f"edge index {idx} out of range [0, {n})")
        level = 1 - level
        out[idx:] = level
    return out


def digital_uart(
    baud: int,
    data: bytes,
    sample_interval_ns: int,
    n: int,
    bits: int = 8,
    parity: str = "none",
    stop_bits: int = 1,
    idle_high: bool = True,
    lsb_first: bool = True,
) -> np.ndarray:
    """Deterministic UART-like waveform for ``data`` at ``baud``.

    Frame = start bit (0) + ``bits`` data bits (LSB-first by default) + optional
    parity + ``stop_bits`` stop bits (1). Idle level is ``idle_high`` (1 by
    default). The returned array is length ``n``; if the encoded frame is shorter
    than ``n`` the remainder is padded with the idle level.
    """
    if not lsb_first:
        raise NotImplementedError("MSB-first UART encoding is not implemented")
    if parity not in ("none", "even", "odd"):
        raise ValueError(f"Unknown parity {parity!r}")

    idle = int(idle_high)
    bits_per_cell = float(sample_interval_ns) * 1e-9 * baud  # samples per bit cell
    if bits_per_cell < 1.0:
        raise ValueError(
            f"sample interval {sample_interval_ns}ns cannot represent {baud} baud"
        )

    bit_cells = []
    for byte in data:
        value = int(byte)
        data_bits = []
        for _ in range(bits):
            data_bits.append(value & 1)
            value >>= 1
        bit_cells.append(0)  # start bit
        bit_cells.extend(data_bits)
        if parity == "even":
            bit_cells.append(sum(data_bits) % 2)
        elif parity == "odd":
            bit_cells.append((sum(data_bits) + 1) % 2)
        bit_cells.extend([1] * int(stop_bits))

    out = np.full(n, idle, dtype=np.uint8)
    sample_pos = 0
    for bit in bit_cells:
        if sample_pos >= n:
            break
        end = int(round(sample_pos + bits_per_cell))
        if end > n:
            end = n
        out[sample_pos:end] = int(bit)
        sample_pos = end
    return out


def build_signal(spec: dict, sample_interval_ns: int, n: int) -> np.ndarray:
    """Build a deterministic signal from a small declarative ``spec`` dict.

    ``spec["type"]`` selects the generator; remaining keys are forwarded. Analog
    kinds return float64 volts; digital kinds return uint8 0/1. Example::

        {"type": "sine", "freq_hz": 1000.0, "amplitude": 1.0, "offset": 0.0}
        {"type": "pwm", "freq_hz": 1000.0, "duty": 0.3, "high_v": 3.3, "low_v": 0.0}
        {"type": "dc", "level_v": 2.5}
        {"type": "square", "freq_hz": 1000.0, "duty": 0.5}
        {"type": "edges", "edge_sample_indices": [100, 250]}
        {"type": "uart", "baud": 2016000, "data": b"\\x55", "bits": 8}
    """
    kind = spec.get("type")
    if kind == "sine":
        return analog_sine(
            freq_hz=spec["freq_hz"],
            amplitude=spec.get("amplitude", 1.0),
            offset=spec.get("offset", 0.0),
            sample_interval_ns=sample_interval_ns,
            n=n,
            phase_deg=spec.get("phase_deg", 0.0),
        )
    if kind == "pwm":
        return analog_pwm(
            freq_hz=spec["freq_hz"],
            duty=spec["duty"],
            high_v=spec.get("high_v", 3.3),
            low_v=spec.get("low_v", 0.0),
            sample_interval_ns=sample_interval_ns,
            n=n,
            phase_deg=spec.get("phase_deg", 0.0),
        )
    if kind == "dc":
        return analog_dc(spec["level_v"], sample_interval_ns, n)
    if kind == "square":
        return digital_square(
            freq_hz=spec["freq_hz"],
            sample_interval_ns=sample_interval_ns,
            n=n,
            duty=spec.get("duty", 0.5),
            phase_deg=spec.get("phase_deg", 0.0),
        )
    if kind == "edges":
        return digital_edges(spec["edge_sample_indices"], n, initial=spec.get("initial", 0))
    if kind == "uart":
        return digital_uart(
            baud=spec["baud"],
            data=spec["data"],
            sample_interval_ns=sample_interval_ns,
            n=n,
            bits=spec.get("bits", 8),
            parity=spec.get("parity", "none"),
            stop_bits=spec.get("stop_bits", 1),
            idle_high=spec.get("idle_high", True),
        )
    raise ValueError(f"Unknown signal type {kind!r}")
