"""Category 8: successful block capture."""

import numpy as np

from tools.picoscope.backend.api import open_device
from tools.picoscope.backend.model import Capture


def test_successful_block_capture():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.configure_digital_port(0, True, 1000)
        cap = scope.capture_block()
        assert isinstance(cap, Capture)
        assert cap.acquisition_status == "OK"
        assert cap.errors == []
        assert "A" in cap.analog_samples
        assert set(cap.digital_samples) >= {"D0", "D7"}
        assert cap.sample_interval_ns > 0
        assert cap.sample_count > 0


def test_capture_is_deterministic_for_same_config():
    def run():
        scope = open_device("SIM0001", backend="simulator")
        try:
            scope.configure_analog_channel("A", True, 5.0, "DC")
            return scope.capture_block()
        finally:
            scope.close_device()

    a = run()
    b = run()
    assert np.array_equal(a.analog_samples["A"], b.analog_samples["A"])
    assert a.sample_interval_ns == b.sample_interval_ns
    assert a.sample_count == b.sample_count
    # capture_id/timestamp differ (unique per capture).
    assert a.capture_id != b.capture_id


def test_no_enabled_channel_is_rejected():
    from tools.picoscope.backend.errors import InvalidConfigError

    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", False, 5.0, "DC")
        try:
            scope.capture_block()
        except InvalidConfigError:
            pass
        else:
            raise AssertionError("expected InvalidConfigError for no enabled channel")
