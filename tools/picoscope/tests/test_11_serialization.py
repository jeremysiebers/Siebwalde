"""Category 11: serialization (npz + sidecar JSON)."""

import json
import os

import numpy as np

from tools.picoscope.backend.api import open_device, save_capture


def _make_capture(scope):
    scope.configure_analog_channel("A", True, 5.0, "DC")
    scope.configure_digital_port(0, True, 1000)
    return scope.capture_block()


def test_save_writes_npz_and_json(tmp_path):
    with open_device("SIM0001", backend="simulator") as scope:
        cap = _make_capture(scope)
        base = save_capture(cap, str(tmp_path / "cap"))
    assert os.path.exists(str(tmp_path / "cap.npz"))
    assert os.path.exists(str(tmp_path / "cap.json"))


def test_npz_contains_raw_samples(tmp_path):
    with open_device("SIM0001", backend="simulator") as scope:
        cap = _make_capture(scope)
        save_capture(cap, str(tmp_path / "cap"))
    with np.load(str(tmp_path / "cap.npz")) as data:
        assert "analog_A" in data.files
        assert "digital_D0" in data.files
        assert len(data["analog_A"]) == cap.sample_count


def test_json_metadata_uses_unambiguous_units(tmp_path):
    with open_device("SIM0001", backend="simulator") as scope:
        cap = _make_capture(scope)
        save_capture(cap, str(tmp_path / "cap"))
    with open(str(tmp_path / "cap.json"), encoding="utf-8") as fh:
        meta = json.load(fh)
    assert isinstance(meta["sample_interval_ns"], int)
    assert isinstance(meta["sample_count"], int)
    assert meta["format_version"] == 1
    # Timestamp is a UTC ISO 8601 string.
    assert meta["timestamp"].endswith("Z")
    assert "samples" in meta


def test_sidecar_does_not_embed_raw_arrays(tmp_path):
    with open_device("SIM0001", backend="simulator") as scope:
        cap = _make_capture(scope)
        save_capture(cap, str(tmp_path / "cap"))
    with open(str(tmp_path / "cap.json"), encoding="utf-8") as fh:
        meta = json.load(fh)
    # Raw data lives only in the npz; the JSON stores shapes/dtypes, not values.
    assert "analog_A" not in meta
    assert meta["samples"]["analog"]["A"]["shape"] == [cap.sample_count]
    assert meta["samples"]["digital"]["D0"]["dtype"] == "uint8"
