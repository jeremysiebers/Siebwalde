"""Category 12: reload + equality."""

import numpy as np

from tools.picoscope.backend.api import load_capture, open_device, save_capture


def test_roundtrip_equality(tmp_path):
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.configure_analog_channel("B", True, 2.0, "AC")
        scope.configure_digital_port(0, True, 1000)
        scope.configure_digital_port(1, True, 1200)
        scope.configure_trigger("A", 0.5, "rising", pre_samples=300, post_samples=700)
        cap = scope.capture_block()
        save_capture(cap, str(tmp_path / "cap"))

    reloaded = load_capture(str(tmp_path / "cap"))
    assert reloaded.equals(cap)
    assert reloaded is not cap
    assert reloaded.capture_id == cap.capture_id
    assert reloaded.timestamp == cap.timestamp


def test_reloaded_arrays_are_value_equal(tmp_path):
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.configure_digital_port(0, True, 1000)
        cap = scope.capture_block()
        save_capture(cap, str(tmp_path / "cap"))

    reloaded = load_capture(str(tmp_path / "cap"))
    assert np.array_equal(reloaded.analog_samples["A"], cap.analog_samples["A"])
    for line in cap.digital_samples:
        assert np.array_equal(reloaded.digital_samples[line], cap.digital_samples[line])


def test_different_captures_are_not_equal(tmp_path):
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        cap1 = scope.capture_block()
        scope.configure_analog_channel("B", True, 5.0, "DC")
        cap2 = scope.capture_block()
    assert not cap1.equals(cap2)


def test_handle_load_updates_last_capture(tmp_path):
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        cap = scope.capture_block()
        scope.save_capture(str(tmp_path / "cap"))
        loaded = scope.load_capture(str(tmp_path / "cap"))
        assert loaded.equals(cap)
        assert scope.last_capture.equals(cap)
