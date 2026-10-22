"""Invalid config, missing channel, truncated/corrupted capture, resource bounds."""

from __future__ import annotations

import os

import numpy as np
import pytest

from tools.picoscope.analysis.errors import InvalidAnalysisConfig, MissingChannel
from tools.picoscope.analysis.io import extract_analog_channel, extract_digital_line
from tools.picoscope.analysis.uart import UartConfig, decode_uart
from tools.picoscope.backend.api import load_capture, open_device, save_capture


# --- invalid config ----------------------------------------------------------

def test_invalid_mode_rejected():
    with pytest.raises(InvalidAnalysisConfig):
        UartConfig(mode="10bit", baud=1_000_000, sample_interval_ns=8).validate()


def test_nonpositive_baud_rejected():
    with pytest.raises(InvalidAnalysisConfig):
        UartConfig(mode="8bit", baud=0, sample_interval_ns=8).validate()


def test_nonpositive_sample_interval_rejected():
    with pytest.raises(InvalidAnalysisConfig):
        UartConfig(mode="8bit", baud=1_000_000, sample_interval_ns=-4).validate()


def test_msb_first_rejected():
    with pytest.raises(InvalidAnalysisConfig):
        UartConfig(mode="8bit", baud=1_000_000, sample_interval_ns=8, lsb_first=False).validate()


def test_invalid_polarity_rejected():
    with pytest.raises(InvalidAnalysisConfig):
        UartConfig(mode="8bit", baud=1_000_000, sample_interval_ns=8, polarity="mid").validate()


# --- missing channel ---------------------------------------------------------

def test_missing_digital_line_raises():
    with open_device("SIM0001") as scope:
        scope.configure_digital_port(0, True, threshold_mv=1000)
        cap = scope.capture_block(post_samples=64)
    with pytest.raises(MissingChannel):
        extract_digital_line(cap, "D9")  # only D0..D7 enabled


def test_missing_analog_channel_raises():
    with open_device("SIM0001") as scope:
        scope.configure_analog_channel("A", True, range_v=5.0, coupling="DC")
        cap = scope.capture_block(post_samples=64)
    with pytest.raises(MissingChannel):
        extract_analog_channel(cap, "B")


# --- truncated / corrupted capture ------------------------------------------

def test_truncated_capture_flagged():
    # A capture whose waveform is cut mid-frame yields a truncated frame.
    from tools.picoscope.analysis import fixtures as fx
    f = fx.fixture_truncated()
    r = decode_uart(f["samples"], UartConfig(mode="9bit", baud=f["baud"], sample_interval_ns=f["sample_interval_ns"]))
    assert any(fr["truncated"] for fr in r["frames"])


def test_corrupted_json_sidecar_raises(tmp_path):
    cap = _make_capture()
    base = save_capture(cap, str(tmp_path / "cap"))
    with open(base + ".json", "w", encoding="utf-8") as fh:
        fh.write("{not valid json")
    with pytest.raises(Exception):
        load_capture(base)


def test_corrupted_npz_raises(tmp_path):
    cap = _make_capture()
    base = save_capture(cap, str(tmp_path / "cap"))
    with open(base + ".npz", "wb") as fh:
        fh.write(b"this is not an npz archive")
    with pytest.raises(Exception):
        load_capture(base)


def test_unsupported_format_version_raises(tmp_path):
    import json
    cap = _make_capture()
    base = save_capture(cap, str(tmp_path / "cap"))
    with open(base + ".json", "r", encoding="utf-8") as fh:
        meta = json.load(fh)
    meta["format_version"] = 999
    with open(base + ".json", "w", encoding="utf-8") as fh:
        json.dump(meta, fh)
    with pytest.raises(ValueError):
        load_capture(base)


# --- resource bounds ---------------------------------------------------------

def test_very_large_input_bounded_by_array_length():
    """The decoder does not loop forever on a long idle line (bounded scan)."""
    samples = np.ones(100_000, dtype=np.uint8)
    r = decode_uart(samples, UartConfig(mode="8bit", baud=2_000_000, sample_interval_ns=8))
    assert r["summary"]["frame_count"] == 0


def _make_capture():
    with open_device("SIM0001") as scope:
        scope.configure_analog_channel("A", True, range_v=5.0, coupling="DC")
        return scope.capture_block(post_samples=64)
