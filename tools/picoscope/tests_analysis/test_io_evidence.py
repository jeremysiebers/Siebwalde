"""NPZ+JSON loading, original-evidence preservation, JSON export, determinism."""

from __future__ import annotations

import os

import numpy as np

from tools.picoscope.analysis import fixtures as fx
from tools.picoscope.analysis.io import (
    export_analysis_json,
    extract_analog_channel,
    extract_digital_line,
    jsonable,
    load_analysis_json,
)
from tools.picoscope.analysis.timing import analyze_pwm
from tools.picoscope.analysis.uart import UartConfig, decode_uart
from tools.picoscope.backend.api import load_capture, open_device, save_capture


def _make_capture():
    with open_device("SIM0001") as scope:
        scope.configure_analog_channel("A", True, range_v=5.0, coupling="DC")
        scope.configure_digital_port(0, True, threshold_mv=1000)
        return scope.capture_block(pre_samples=0, post_samples=256)


def test_npz_json_roundtrip(tmp_path):
    cap = _make_capture()
    base = save_capture(cap, str(tmp_path / "cap"))
    assert os.path.isfile(base + ".npz")
    assert os.path.isfile(base + ".json")
    loaded = load_capture(base)
    assert loaded.equals(cap)


def test_original_evidence_preserved(tmp_path):
    cap = _make_capture()
    base = save_capture(cap, str(tmp_path / "cap"))
    npz_before = open(base + ".npz", "rb").read()
    json_before = open(base + ".json", "rb").read()

    loaded = load_capture(base)
    line = extract_digital_line(loaded, "D0")
    chan = extract_analog_channel(loaded, "A")
    # Running analysis must not mutate the loaded arrays or the on-disk evidence.
    _ = analyze_pwm(line, loaded.sample_interval_ns, domain="digital")
    _ = np.asarray(chan).copy()

    assert open(base + ".npz", "rb").read() == npz_before
    assert open(base + ".json", "rb").read() == json_before


def test_analysis_json_export_roundtrip(tmp_path):
    f = fx.fixture_valid_9bit()
    r = decode_uart(f["samples"], UartConfig(mode="9bit", baud=f["baud"], sample_interval_ns=f["sample_interval_ns"]))
    path = str(tmp_path / "analysis.json")
    export_analysis_json(r, path)
    back = load_analysis_json(path)
    assert back["summary"]["frame_count"] == 1
    assert back["frames"][0]["value"] == 0x04
    assert back["frames"][0]["ninth_bit"] == 1


def test_jsonable_handles_numpy_types():
    obj = {"a": np.int64(5), "b": np.float64(1.5), "c": np.array([1, 2, 3]), "d": [np.uint8(1)]}
    clean = jsonable(obj)
    assert clean["a"] == 5 and isinstance(clean["a"], int)
    assert clean["c"] == [1, 2, 3]
    assert clean["d"] == [1]


def test_determinism():
    f = fx.fixture_consecutive_frames()
    cfg = UartConfig(mode="9bit", baud=f["baud"], sample_interval_ns=f["sample_interval_ns"])
    r1 = decode_uart(f["samples"], cfg)
    r2 = decode_uart(f["samples"], cfg)
    assert r1 == r2


def test_export_determinism(tmp_path):
    f = fx.fixture_valid_8bit()
    r = decode_uart(f["samples"], UartConfig(mode="8bit", baud=f["baud"], sample_interval_ns=f["sample_interval_ns"]))
    p1 = str(tmp_path / "a.json")
    p2 = str(tmp_path / "b.json")
    export_analysis_json(r, p1)
    export_analysis_json(r, p2)
    assert open(p1, "rb").read() == open(p2, "rb").read()
