"""Category 14: device error handling."""

import pytest

from tools.picoscope.backend.api import open_device
from tools.picoscope.backend.errors import DeviceError


def test_simulated_device_error_raises_with_message():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.device.inject_error("simulated hardware fault")
        with pytest.raises(DeviceError, match="simulated hardware fault"):
            scope.capture_block()


def test_device_error_is_one_shot():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.device.inject_error("boom")
        with pytest.raises(DeviceError):
            scope.capture_block()
        cap = scope.capture_block()
        assert cap.acquisition_status == "OK"


def test_device_still_closable_after_error():
    scope = open_device("SIM0001", backend="simulator")
    scope.configure_analog_channel("A", True, 5.0, "DC")
    scope.device.inject_error("boom")
    with pytest.raises(DeviceError):
        scope.capture_block()
    scope.close_device()
    assert scope.closed
