"""PWM + edge/timing analysis tests (synthetic, not hardware validation)."""

from __future__ import annotations

import numpy as np
import pytest

from tools.picoscope.analysis import fixtures as fx
from tools.picoscope.analysis.errors import InvalidAnalysisConfig
from tools.picoscope.analysis.timing import (
    analyze_pwm,
    cross_channel_delta,
    measure_edges,
)


def test_pwm_digital():
    f = fx.fixture_pwm_digital()
    m = analyze_pwm(f["samples"], f["sample_interval_ns"], domain=f["domain"])
    assert m.domain == "digital"
    assert m.complete_cycles >= 3  # 5 tiled cycles -> 4 rising edges -> 3 complete periods
    assert m.frequency_hz == pytest.approx(f["freq_hz"], rel=0.02)
    assert m.duty == pytest.approx(f["duty"], abs=0.02)
    assert m.high_pulse_ns > 0 and m.low_pulse_ns > 0
    assert len(m.rising_edges_ns) >= 3
    assert len(m.falling_edges_ns) >= 3


def test_pwm_analog():
    f = fx.fixture_pwm_analog()
    m = analyze_pwm(f["samples"], f["sample_interval_ns"], domain=f["domain"])
    assert m.domain == "analog"
    assert m.frequency_hz == pytest.approx(f["freq_hz"], rel=0.02)
    assert m.duty == pytest.approx(f["duty"], abs=0.02)
    assert m.threshold is not None  # default midpoint
    assert "sample-resolution" in m.resolution_note


def test_pwm_analog_explicit_threshold():
    f = fx.fixture_pwm_analog()
    m = analyze_pwm(f["samples"], f["sample_interval_ns"], domain="analog", threshold=1.5)
    assert m.threshold == 1.5
    assert m.frequency_hz == pytest.approx(f["freq_hz"], rel=0.02)


def test_measure_edges_counts():
    f = fx.fixture_pwm_digital()
    r = measure_edges(f["samples"], f["sample_interval_ns"], domain="digital")
    assert r["rising_edge_count"] >= 4
    assert r["falling_edge_count"] >= 4
    assert len(r["rising_edges_ns"]) == r["rising_edge_count"]


def test_cross_channel_delta():
    a = [100.0, 1000.0, 2000.0]
    b = [120.0, 1020.0]
    deltas = cross_channel_delta(a, b, 8)
    assert deltas[0]["delta_ns"] == pytest.approx(20.0)
    assert deltas[1]["delta_ns"] == pytest.approx(20.0)


def test_invalid_domain_rejected():
    with pytest.raises(InvalidAnalysisConfig):
        analyze_pwm(np.array([0, 1, 0, 1], dtype=np.uint8), 8, domain="bogus")


def test_invalid_sample_interval_rejected():
    with pytest.raises(InvalidAnalysisConfig):
        analyze_pwm(np.array([0, 1, 0, 1], dtype=np.uint8), 0, domain="digital")


def test_no_edges_yields_zero_measurement():
    flat = np.zeros(100, dtype=np.uint8)
    m = analyze_pwm(flat, 8, domain="digital")
    assert m.complete_cycles == 0
    assert m.frequency_hz == 0.0
