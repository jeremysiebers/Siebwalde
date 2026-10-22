"""UART decoder tests against the 16 deterministic fixture categories.

These are SYNTHETIC offline tests -- they verify the decoder against known,
self-consistent waveforms. They are NOT hardware validation.
"""

from __future__ import annotations

import pytest

from tools.picoscope.analysis import fixtures as fx
from tools.picoscope.analysis.errors import InvalidAnalysisConfig
from tools.picoscope.analysis.uart import UartConfig, compare_tx_rx, decode_uart


def _decode(samples, baud, si, mode, **kw):
    cfg = UartConfig(mode=mode, baud=baud, sample_interval_ns=si, **kw)
    return decode_uart(samples, cfg)


def _values(result):
    return [f["value"] for f in result["frames"]]


def _ninths(result):
    return [f["ninth_bit"] for f in result["frames"]]


# --- individual categories ---------------------------------------------------

def test_valid_8bit():
    f = fx.fixture_valid_8bit()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert _values(r) == f["expected_values"]
    assert _ninths(r) == f["expected_ninth"]
    assert r["frames"][0]["stop_ok"] is True
    assert r["frames"][0]["frame_error"] is False


def test_valid_9bit():
    f = fx.fixture_valid_9bit()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert _values(r) == f["expected_values"]
    assert _ninths(r) == f["expected_ninth"]
    # raw 9-bit value AND separate ninth-bit flag are both preserved.
    assert r["frames"][0]["raw_value"] == f["expected_raw"][0]
    assert r["frames"][0]["ninth_bit"] == 1
    assert r["frames"][0]["value"] == 0x04


def test_master_address_ninth_set():
    f = fx.fixture_master_address_ninth_set()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert _ninths(r) == [1]
    assert _values(r) == [0x04]


def test_master_data_ninth_clear():
    f = fx.fixture_master_data_ninth_clear()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert _ninths(r) == [0]
    assert _values(r) == [0x03]


def test_pic18_reply_eight_bit():
    f = fx.fixture_pic18_reply_eight_bit()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert _values(r) == [0x04]
    assert _ninths(r) == [None]  # no ninth bit at all in 8-bit mode


def test_consecutive_frames():
    f = fx.fixture_consecutive_frames()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert _values(r) == f["expected_values"]
    assert _ninths(r) == f["expected_ninth"]
    assert all(x["stop_ok"] for x in r["frames"])


def test_idle_gap_grouping():
    f = fx.fixture_idle_gaps()
    cfg = UartConfig(mode=f["mode"], baud=f["baud"], sample_interval_ns=f["sample_interval_ns"],
                     idle_gap_bits=f["idle_gap_bits"])
    r = decode_uart(f["samples"], cfg)
    assert _values(r) == f["expected_values"]
    assert r["groups"] is not None and len(r["groups"]) == 2
    assert [g["word_count"] for g in r["groups"]] == [1, 1]


def test_bad_stop_bit():
    f = fx.fixture_bad_stop_bit()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert len(r["frames"]) == 1
    assert r["frames"][0]["frame_error"] is True
    assert f["expect_error_token"] in r["frames"][0]["errors"]


def test_truncated():
    f = fx.fixture_truncated()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert len(r["frames"]) == 1
    assert r["frames"][0]["truncated"] is True
    assert "truncated" in r["frames"][0]["errors"]


def test_short_noise_pulse_ignored():
    f = fx.fixture_short_noise_pulse()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert r["summary"]["frame_count"] == 0


def test_baud_variation_tolerated():
    f = fx.fixture_baud_variation()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert _values(r) == f["expected_values"]


def test_different_sample_intervals():
    for f in fx.fixture_different_sample_intervals():
        r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
        assert _values(r) == f["expected_values"]


def test_mid_frame_start_not_misread():
    f = fx.fixture_mid_frame_start()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert r["summary"]["frame_count"] == 1
    assert _values(r) == f["expected_values"]


def test_simultaneous_tx_rx():
    f = fx.fixture_simultaneous_tx_rx()
    tx = _decode(f["tx"], f["baud"], f["sample_interval_ns"], f["tx_mode"])
    rx = _decode(f["rx"], f["baud"], f["sample_interval_ns"], f["rx_mode"])
    assert _values(tx) == f["tx_values"]
    assert _ninths(tx) == [1, 0]
    assert _values(rx) == f["rx_values"]
    assert _ninths(rx) == [None]


def test_tx_rx_timing_correlation():
    f = fx.fixture_tx_rx_timing_correlation()
    tx = _decode(f["tx"], f["baud"], f["sample_interval_ns"], "9bit")
    rx = _decode(f["rx"], f["baud"], f["sample_interval_ns"], "8bit")
    pairs = compare_tx_rx(tx["frames"], rx["frames"])
    assert len(pairs) == 1
    delta = pairs[0]["delta_ns"]
    expected = f["offset_samples"] * f["sample_interval_ns"]
    # Within a few samples of the known offset.
    assert abs(delta - expected) <= 4 * f["sample_interval_ns"]


def test_explicit_nine_to_eight_transition():
    f = fx.fixture_nine_to_eight_transition()
    boundary = f["reply_start_sample"]
    # Explicit per-segment modes (no guessing).
    nine = _decode(f["samples"][:boundary], f["baud"], f["sample_interval_ns"], "9bit")
    eight = _decode(f["samples"][boundary:], f["baud"], f["sample_interval_ns"], "8bit")
    assert _values(nine) == [0x04, 0x03]
    assert _ninths(nine) == [1, 0]
    assert _values(eight) == [0x04]
    assert _ninths(eight) == [None]


# --- mode discipline ---------------------------------------------------------

def test_mode_is_required_and_never_guessed():
    # Omitting mode is a TypeError (required field); an invalid mode raises.
    with pytest.raises(InvalidAnalysisConfig):
        UartConfig(mode="7bit", baud=2_000_000, sample_interval_ns=8).validate()
    with pytest.raises(InvalidAnalysisConfig):
        UartConfig(mode="9bit", baud=2_000_000, sample_interval_ns=8, polarity="bogus").validate()


def test_explicit_mode_reports_uncertainty():
    f = fx.fixture_valid_8bit()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"])
    assert any("not independently verified" in u for u in r["uncertainty"])


def test_verified_marker_mode_has_no_mode_uncertainty():
    f = fx.fixture_valid_8bit()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], f["mode"],
                mode_source="verified_marker")
    assert not any("not independently verified" in u for u in r["uncertainty"])


def test_9bit_decode_of_8bit_reply_is_spurious():
    """Decoding an 8-bit reply as 9-bit yields a spurious ninth bit (stop bit).

    This is the reason the mode must be explicit, matching the HMO decoder's
    finding that a 9-bit reading of the PIC18 reply produces a constant ninth=1.
    """
    f = fx.fixture_pic18_reply_eight_bit()
    r = _decode(f["samples"], f["baud"], f["sample_interval_ns"], "9bit")
    # The ninth bit of the spurious 9-bit reading samples the stop bit (1).
    assert r["frames"] and r["frames"][0]["ninth_bit"] == 1
