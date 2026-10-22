"""Deterministic synthetic fixtures for offline decoder/analyzer tests.

Every fixture is a pure function of its inputs -- no randomness, no hardware, no
MCP. Each UART fixture returns a dict with the waveform (``samples``), the
``sample_interval_ns``, the ``baud``, the explicit ``mode`` and the expected
decode properties, so tests can assert decoder behaviour against a known truth.

Bit placement uses fractional cumulative sample positions (``round(k*spp)`` for
bit ``k``) so the on-wire frame length exactly matches what the decoder expects
(``round(total_bits*spp)``). This keeps back-to-back frames aligned.

These fixtures are SYNTHETIC test data. They do NOT constitute hardware
validation and MUST NOT be presented as such.
"""

from __future__ import annotations

from typing import Dict, List, Optional

import numpy as np

#: Project reference baud (see docs/PICOSCOPE-PICO-4-SIGNAL-ANALYSIS.md).
REF_BAUD = 2_016_000


def _idle(polarity: str) -> int:
    return 1 if polarity == "idle_high" else 0


def _frame_levels(value: int, ninth: Optional[int], mode: str, polarity: str, stop_bits: int) -> List[int]:
    """Bit levels for one frame: start, data bits, (ninth), stop bit(s)."""
    idle = _idle(polarity)
    start = 1 - idle
    bits = [start]
    for k in range(8):
        bits.append((value >> k) & 1)
    if mode == "9bit":
        bits.append(1 if ninth else 0)
    bits.extend([idle] * stop_bits)
    return bits


def _emit_levels(levels: List[int], spp: float, idle: int) -> np.ndarray:
    """Render a level list into a sample array using fractional bit boundaries."""
    total = len(levels)
    n = int(round(total * spp))
    out = np.full(n, idle, dtype=np.uint8)
    for k, lvl in enumerate(levels):
        lo = int(round(k * spp))
        hi = int(round((k + 1) * spp))
        if hi > lo:
            out[lo:hi] = lvl
    return out


def build_word_waveform(
    words: List[dict],
    baud: int,
    sample_interval_ns: int,
    *,
    mode: str,
    polarity: str = "idle_high",
    stop_bits: int = 1,
    idle_gap_samples: int = 0,
    lead_idle_bits: float = 1.0,
) -> np.ndarray:
    """Build a digital 0/1 waveform for ``words`` at ``baud``.

    Each word is a dict with ``value`` (0..255) and optional ``ninth`` (0/1, used
    in 9-bit mode). ``idle_gap_samples`` inserts that many idle samples between
    consecutive words; ``lead_idle_bits`` prepends that many idle bit-periods.
    """
    idle = _idle(polarity)
    spp = 1e9 / float(baud) / float(sample_interval_ns)
    out: List[int] = [idle] * int(round(spp * lead_idle_bits))
    first = True
    for w in words:
        if not first:
            out.extend([idle] * int(idle_gap_samples))
        first = False
        levels = _frame_levels(int(w["value"]), w.get("ninth"), mode, polarity, stop_bits)
        out.extend(_emit_levels(levels, spp, idle).tolist())
    return np.asarray(out, dtype=np.uint8)


def _word(value: int, ninth: Optional[int] = None) -> dict:
    d: dict = {"value": value}
    if ninth is not None:
        d["ninth"] = ninth
    return d


# ---------------------------------------------------------------------------
# UART fixture categories (16)
# ---------------------------------------------------------------------------

def fixture_valid_8bit() -> dict:
    baud, si = 2_000_000, 8
    value = 0x55
    samples = build_word_waveform([_word(value)], baud, si, mode="8bit")
    return {"name": "valid_8bit", "samples": samples, "sample_interval_ns": si, "baud": baud,
            "mode": "8bit", "expected_values": [value], "expected_ninth": [None]}


def fixture_valid_9bit() -> dict:
    baud, si = 2_000_000, 8
    value, ninth = 0x04, 1
    samples = build_word_waveform([_word(value, ninth)], baud, si, mode="9bit")
    return {"name": "valid_9bit", "samples": samples, "sample_interval_ns": si, "baud": baud,
            "mode": "9bit", "expected_values": [value], "expected_ninth": [ninth],
            "expected_raw": [value | (ninth << 8)]}


def fixture_master_address_ninth_set() -> dict:
    baud, si = REF_BAUD, 8
    value, ninth = 0x04, 1  # Modbus slave address byte, ninth = 1 (address)
    samples = build_word_waveform([_word(value, ninth)], baud, si, mode="9bit")
    return {"name": "master_address_ninth_set", "samples": samples, "sample_interval_ns": si,
            "baud": baud, "mode": "9bit", "expected_values": [value], "expected_ninth": [1]}


def fixture_master_data_ninth_clear() -> dict:
    baud, si = REF_BAUD, 8
    value, ninth = 0x03, 0  # function code byte, ninth = 0 (data)
    samples = build_word_waveform([_word(value, ninth)], baud, si, mode="9bit")
    return {"name": "master_data_ninth_clear", "samples": samples, "sample_interval_ns": si,
            "baud": baud, "mode": "9bit", "expected_values": [value], "expected_ninth": [0]}


def fixture_pic18_reply_eight_bit() -> dict:
    """PIC18 reply is 8-bit (TX9=0): no ninth bit is transmitted at all."""
    baud, si = REF_BAUD, 8
    value = 0x04
    samples = build_word_waveform([_word(value)], baud, si, mode="8bit")
    return {"name": "pic18_reply_eight_bit", "samples": samples, "sample_interval_ns": si,
            "baud": baud, "mode": "8bit", "expected_values": [value], "expected_ninth": [None]}


def fixture_consecutive_frames() -> dict:
    """A 9-bit telegram: address (ninth=1) then data bytes (ninth=0)."""
    baud, si = 2_000_000, 8
    words = [_word(0x04, 1), _word(0x03, 0), _word(0x00, 0), _word(0x08, 0)]
    samples = build_word_waveform(words, baud, si, mode="9bit")
    return {"name": "consecutive_frames", "samples": samples, "sample_interval_ns": si,
            "baud": baud, "mode": "9bit", "expected_values": [0x04, 0x03, 0x00, 0x08],
            "expected_ninth": [1, 0, 0, 0]}


def fixture_idle_gaps() -> dict:
    baud, si = 2_000_000, 8
    spp = 1e9 / baud / si
    gap = int(round(spp * 40))  # 40-bit idle gap between words
    words = [_word(0x04, 1), _word(0x03, 0)]
    samples = build_word_waveform(words, baud, si, mode="9bit", idle_gap_samples=gap)
    return {"name": "idle_gaps", "samples": samples, "sample_interval_ns": si, "baud": baud,
            "mode": "9bit", "expected_values": [0x04, 0x03], "expected_ninth": [1, 0],
            "gap_samples": gap, "idle_gap_bits": 20.0}


def fixture_bad_stop_bit() -> dict:
    """A frame whose stop bit is forced low (framing error)."""
    baud, si = 2_000_000, 8
    spp = 1e9 / baud / si
    idle = 1
    value, ninth = 0x04, 1
    levels = _frame_levels(value, ninth, "9bit", "idle_high", 1)
    levels = levels[:-1] + [0]  # stop bit low -> framing error
    lead = [idle] * int(round(spp))
    samples = np.asarray(lead + _emit_levels(levels, spp, idle).tolist(), dtype=np.uint8)
    return {"name": "bad_stop_bit", "samples": samples, "sample_interval_ns": si, "baud": baud,
            "mode": "9bit", "expect_frame_error": True, "expect_error_token": "stop_bit"}


def fixture_truncated() -> dict:
    """A frame cut off before its stop bit completes."""
    baud, si = 2_000_000, 8
    spp = 1e9 / baud / si
    idle = 1
    value, ninth = 0x04, 1
    levels = _frame_levels(value, ninth, "9bit", "idle_high", 1)
    lead = [idle] * int(round(spp))
    body = _emit_levels(levels, spp, idle).tolist()
    # Drop the trailing stop-bit run (truncate mid-stop).
    samples = np.asarray(lead + body[:-int(round(spp))], dtype=np.uint8)
    return {"name": "truncated", "samples": samples, "sample_interval_ns": si, "baud": baud,
            "mode": "9bit", "expect_truncated": True}


def fixture_short_noise_pulse() -> dict:
    """A 2-sample low glitch in an idle line (not a valid start bit)."""
    si = 8
    samples = np.ones(600, dtype=np.uint8)
    samples[300] = 0
    samples[301] = 0
    return {"name": "short_noise_pulse", "samples": samples, "sample_interval_ns": si,
            "baud": 2_000_000, "mode": "8bit", "expected_frame_count": 0}


def fixture_baud_variation() -> dict:
    """Waveform built at REF_BAUD; decoded at a slightly different baud."""
    build_baud, si = REF_BAUD, 8
    value = 0x55
    samples = build_word_waveform([_word(value)], build_baud, si, mode="8bit")
    decode_baud = 2_000_000  # ~0.8% slow
    return {"name": "baud_variation", "samples": samples, "sample_interval_ns": si,
            "baud": decode_baud, "build_baud": build_baud, "mode": "8bit",
            "expected_values": [value]}


def fixture_different_sample_intervals() -> List[dict]:
    """The same word sampled at three different intervals decodes identically."""
    baud = 2_000_000
    value = 0x3C
    out = []
    for si in (8, 16, 64):
        samples = build_word_waveform([_word(value)], baud, si, mode="8bit")
        out.append({"name": f"sample_interval_{si}ns", "samples": samples,
                    "sample_interval_ns": si, "baud": baud, "mode": "8bit",
                    "expected_values": [value]})
    return out


def fixture_mid_frame_start() -> dict:
    """A data byte with a 0 bit mid-frame must not be read as a new start."""
    baud, si = 2_000_000, 8
    value = 0x01  # D0=1 only; D1..D7 = 0 => long low run mid-frame
    samples = build_word_waveform([_word(value)], baud, si, mode="8bit")
    return {"name": "mid_frame_start", "samples": samples, "sample_interval_ns": si,
            "baud": baud, "mode": "8bit", "expected_values": [value],
            "expected_frame_count": 1}


def fixture_simultaneous_tx_rx() -> dict:
    """Two independent lines (TX 9-bit request, RX 8-bit reply) share a grid."""
    baud, si = 2_000_000, 8
    tx = build_word_waveform([_word(0x04, 1), _word(0x03, 0)], baud, si, mode="9bit")
    rx = build_word_waveform([_word(0x04)], baud, si, mode="8bit")
    n = max(len(tx), len(rx))
    tx = np.pad(tx, (0, n - len(tx)), constant_values=1)
    rx = np.pad(rx, (0, n - len(rx)), constant_values=1)
    return {"name": "simultaneous_tx_rx", "tx": tx, "rx": rx, "sample_interval_ns": si,
            "baud": baud, "tx_mode": "9bit", "rx_mode": "8bit",
            "tx_values": [0x04, 0x03], "rx_values": [0x04]}


def fixture_tx_rx_timing_correlation() -> dict:
    """RX reply starts a known number of samples after the TX request."""
    baud, si = 2_000_000, 8
    offset_samples = 1500
    tx = build_word_waveform([_word(0x04, 1)], baud, si, mode="9bit")
    rx_body = build_word_waveform([_word(0x04)], baud, si, mode="8bit")
    # Place RX after an idle run of offset_samples.
    rx = np.concatenate([np.ones(offset_samples, dtype=np.uint8), rx_body])
    n = max(len(tx), len(rx))
    tx = np.pad(tx, (0, n - len(tx)), constant_values=1)
    rx = np.pad(rx, (0, n - len(rx)), constant_values=1)
    return {"name": "tx_rx_timing_correlation", "tx": tx, "rx": rx, "sample_interval_ns": si,
            "baud": baud, "offset_samples": offset_samples}


def fixture_nine_to_eight_transition() -> dict:
    """Explicit 9->8-bit transition: a 9-bit request, idle gap, 8-bit reply."""
    baud, si = 2_000_000, 8
    spp = 1e9 / baud / si
    gap = int(round(spp * 30))
    nine = build_word_waveform([_word(0x04, 1), _word(0x03, 0)], baud, si, mode="9bit")
    eight = build_word_waveform([_word(0x04)], baud, si, mode="8bit")
    samples = np.concatenate([nine, np.ones(gap, dtype=np.uint8), eight])
    # Boundary sample index where the 8-bit reply starts (for explicit per-segment decode).
    return {"name": "nine_to_eight_transition", "samples": samples, "sample_interval_ns": si,
            "baud": baud, "reply_start_sample": len(nine) + gap}


# ---------------------------------------------------------------------------
# PWM fixtures
# ---------------------------------------------------------------------------

def fixture_pwm_digital() -> dict:
    """A clean digital square wave (1 kHz, 30% duty) at 8 ns/sample."""
    si = 8
    freq = 1000.0
    duty = 0.3
    period_ns = 1e9 / freq
    period_samples = int(round(period_ns / si))
    high = int(round(period_samples * duty))
    low = period_samples - high
    cycle = np.concatenate([np.ones(high, dtype=np.uint8), np.zeros(low, dtype=np.uint8)])
    samples = np.tile(cycle, 5)
    return {"name": "pwm_digital", "samples": samples, "sample_interval_ns": si,
            "domain": "digital", "freq_hz": freq, "duty": duty}


def fixture_pwm_analog() -> dict:
    """An analog PWM (3.3 V high / 0 V low, 2 kHz, 50% duty) at 16 ns/sample."""
    si = 16
    freq = 2000.0
    duty = 0.5
    high_v, low_v = 3.3, 0.0
    period_ns = 1e9 / freq
    period_samples = int(round(period_ns / si))
    high = int(round(period_samples * duty))
    low = period_samples - high
    cycle = np.concatenate([np.full(high, high_v), np.full(low, low_v)])
    samples = np.tile(cycle, 4).astype(np.float64)
    return {"name": "pwm_analog", "samples": samples, "sample_interval_ns": si,
            "domain": "analog", "freq_hz": freq, "duty": duty, "high_v": high_v, "low_v": low_v}


def bits_per_cell(baud: int, sample_interval_ns: int) -> float:
    """Samples per bit cell for the given baud + sample interval."""
    return 1e9 / float(baud) / float(sample_interval_ns)
