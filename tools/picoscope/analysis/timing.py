"""Offline PWM + edge/timing analysis (standalone, no MCP dependency).

Two measurement domains are kept distinct:

* **digital** -- a bool/int 0/1 line where the logic level is already decided
  by the scope's threshold; "high" = 1, "low" = 0.
* **analog**  -- a float64 volt array; a threshold crossing decides high/low.
  Duty/period from threshold crossings are threshold-dependent, NOT calibrated
  physical accuracy.

All timing is derived from ``sample_interval_ns`` (integer nanoseconds). Results
are therefore sample-resolution limited (a +/-1 sample uncertainty on every
edge), which the results surface explicitly.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import List, Optional, Sequence

import numpy as np

from .errors import InvalidAnalysisConfig

#: How many bits of precision to keep on ns timestamps (deterministic rounding).
_NS_NDIGITS = 3


@dataclass
class PwmMeasurement:
    """A PWM / periodic-signal measurement."""

    domain: str  # "digital" | "analog"
    sample_interval_ns: int
    frequency_hz: float
    period_ns: float
    duty: float  # high fraction of a complete period, 0..1
    high_pulse_ns: float
    low_pulse_ns: float
    rising_edges_ns: List[float]
    falling_edges_ns: List[float]
    complete_cycles: int
    period_jitter_ns: Optional[float] = None  # stddev of complete periods
    duty_jitter: Optional[float] = None  # stddev of per-cycle duty
    threshold: Optional[float] = None  # analog threshold (None for digital)
    resolution_note: str = field(default="")

    def to_dict(self) -> dict:
        return {
            "domain": self.domain,
            "sample_interval_ns": self.sample_interval_ns,
            "frequency_hz": round(float(self.frequency_hz), _NS_NDIGITS),
            "period_ns": round(float(self.period_ns), _NS_NDIGITS),
            "duty": round(float(self.duty), _NS_NDIGITS),
            "high_pulse_ns": round(float(self.high_pulse_ns), _NS_NDIGITS),
            "low_pulse_ns": round(float(self.low_pulse_ns), _NS_NDIGITS),
            "rising_edges_ns": [round(float(x), _NS_NDIGITS) for x in self.rising_edges_ns],
            "falling_edges_ns": [round(float(x), _NS_NDIGITS) for x in self.falling_edges_ns],
            "complete_cycles": self.complete_cycles,
            "period_jitter_ns": (
                round(float(self.period_jitter_ns), _NS_NDIGITS) if self.period_jitter_ns is not None else None
            ),
            "duty_jitter": (
                round(float(self.duty_jitter), _NS_NDIGITS) if self.duty_jitter is not None else None
            ),
            "threshold": self.threshold,
            "resolution_note": self.resolution_note,
        }


def _level_stream(samples: Sequence[float], domain: str, threshold: Optional[float]) -> np.ndarray:
    """Return a 0/1 high/low stream for digital (identity) or analog (threshold)."""
    arr = np.asarray(samples, dtype=np.float64)
    if domain == "digital":
        return (arr != 0).astype(np.int64)
    if domain == "analog":
        if threshold is None:
            threshold = float(arr.min() + arr.max()) / 2.0
        if not np.isfinite(threshold):
            raise InvalidAnalysisConfig("analog threshold must be finite")
        return (arr >= threshold).astype(np.int64)
    raise InvalidAnalysisConfig(f"domain must be 'digital' or 'analog', got {domain!r}")


def _edges_ns(levels: np.ndarray, interval_ns: int) -> "tuple[List[float], List[float]]":
    rising: List[float] = []
    falling: List[float] = []
    for i in range(1, len(levels)):
        prev, cur = levels[i - 1], levels[i]
        if prev == 0 and cur == 1:
            rising.append(float(i) * interval_ns)
        elif prev == 1 and cur == 0:
            falling.append(float(i) * interval_ns)
    return rising, falling


def analyze_pwm(
    samples: Sequence[float],
    sample_interval_ns: int,
    *,
    domain: str = "digital",
    threshold: Optional[float] = None,
    high_v: Optional[float] = None,
    low_v: Optional[float] = None,
) -> PwmMeasurement:
    """Measure PWM frequency/period/duty and edge timing.

    Args:
        samples: digital 0/1 line (domain="digital") or analog volts
            (domain="analog").
        sample_interval_ns: integer sample interval.
        domain: ``"digital"`` or ``"analog"``.
        threshold: analog high/low threshold (volts). Defaults to the
            mid-point of the data when omitted (analog only).
        high_v, low_v: accepted for API symmetry; only used to document the
            analog swing. They do not affect the measurement.
    """
    if not isinstance(sample_interval_ns, int) or sample_interval_ns <= 0:
        raise InvalidAnalysisConfig(
            f"sample_interval_ns must be a positive int, got {sample_interval_ns!r}"
        )
    levels = _level_stream(samples, domain, threshold)
    rising, falling = _edges_ns(levels, sample_interval_ns)

    if domain == "analog":
        thr = threshold if threshold is not None else float(np.asarray(samples, dtype=np.float64).min() + np.asarray(samples, dtype=np.float64).max()) / 2.0
    else:
        thr = None

    # Complete cycles = rising edge to next rising edge, with at least one
    # falling edge between them.
    periods: List[float] = []
    duties: List[float] = []
    complete_cycles = 0
    # Map rising edges to the following falling edge and next rising edge.
    if len(rising) >= 2:
        # For each rising edge i, find the first falling edge after it, then the
        # first rising edge after that falling edge.
        for i in range(len(rising)):
            r0 = rising[i]
            falls_after = [f for f in falling if f > r0]
            if not falls_after:
                break
            f0 = falls_after[0]
            rises_after = [r for r in rising if r > f0]
            if not rises_after:
                break
            r1 = rises_after[0]
            period = r1 - r0
            high = f0 - r0
            if period > 0:
                periods.append(period)
                duties.append(high / period)
                complete_cycles += 1

    # High/low pulse durations from consecutive alternating edges.
    all_edges = sorted([("rising", t) for t in rising] + [("falling", t) for t in falling], key=lambda e: e[1])
    high_pulses: List[float] = []
    low_pulses: List[float] = []
    for (kind_a, ta), (kind_b, tb) in zip(all_edges, all_edges[1:]):
        dur = tb - ta
        if dur <= 0:
            continue
        if kind_a == "rising":
            high_pulses.append(dur)
        else:
            low_pulses.append(dur)

    if periods:
        period_ns = float(np.mean(periods))
        frequency_hz = 1e9 / period_ns if period_ns > 0 else 0.0
        duty = float(np.mean(duties))
        period_jitter = float(np.std(periods, ddof=1)) if len(periods) >= 2 else 0.0
        duty_jitter = float(np.std(duties, ddof=1)) if len(duties) >= 2 else 0.0
    else:
        period_ns = 0.0
        frequency_hz = 0.0
        duty = 0.0
        period_jitter = None
        duty_jitter = None

    high_pulse_ns = float(np.mean(high_pulses)) if high_pulses else 0.0
    low_pulse_ns = float(np.mean(low_pulses)) if low_pulses else 0.0

    resolution_note = (
        f"timing is sample-resolution limited (+/-1 sample = {sample_interval_ns} ns "
        "per edge); NOT calibrated physical accuracy"
    )

    return PwmMeasurement(
        domain=domain,
        sample_interval_ns=sample_interval_ns,
        frequency_hz=frequency_hz,
        period_ns=period_ns,
        duty=duty,
        high_pulse_ns=high_pulse_ns,
        low_pulse_ns=low_pulse_ns,
        rising_edges_ns=rising,
        falling_edges_ns=falling,
        complete_cycles=complete_cycles,
        period_jitter_ns=period_jitter,
        duty_jitter=duty_jitter,
        threshold=thr,
        resolution_note=resolution_note,
    )


def measure_edges(samples: Sequence[float], sample_interval_ns: int, *, domain: str = "digital", threshold: Optional[float] = None) -> dict:
    """Return rising/falling edge timestamps + counts (no period/duty)."""
    if not isinstance(sample_interval_ns, int) or sample_interval_ns <= 0:
        raise InvalidAnalysisConfig(
            f"sample_interval_ns must be a positive int, got {sample_interval_ns!r}"
        )
    levels = _level_stream(samples, domain, threshold)
    rising, falling = _edges_ns(levels, sample_interval_ns)
    return {
        "sample_interval_ns": sample_interval_ns,
        "domain": domain,
        "rising_edges_ns": [round(float(x), _NS_NDIGITS) for x in rising],
        "falling_edges_ns": [round(float(x), _NS_NDIGITS) for x in falling],
        "rising_edge_count": len(rising),
        "falling_edge_count": len(falling),
        "resolution_note": f"+/-1 sample = {sample_interval_ns} ns per edge (not calibrated)",
    }


def cross_channel_delta(
    edges_a: Sequence[float],
    edges_b: Sequence[float],
    sample_interval_ns: int,
) -> List[dict]:
    """Cross-channel time differences: nearest ``edges_b`` edge to each ``edges_a``.

    Both edge lists are nanosecond timestamps on a shared sample grid (same
    acquisition). Returns a list of ``{"a_ns", "b_ns", "delta_ns"}`` with
    ``delta_ns = b - a`` (nearest, may be negative). Advisory only.
    """
    a = sorted(float(x) for x in edges_a)
    b = sorted(float(x) for x in edges_b)
    out: List[dict] = []
    for ta in a:
        if not b:
            out.append({"a_ns": round(ta, _NS_NDIGITS), "b_ns": None, "delta_ns": None})
            continue
        nearest = min(b, key=lambda tb: abs(tb - ta))
        out.append({
            "a_ns": round(ta, _NS_NDIGITS),
            "b_ns": round(nearest, _NS_NDIGITS),
            "delta_ns": round(nearest - ta, _NS_NDIGITS),
        })
    return out
