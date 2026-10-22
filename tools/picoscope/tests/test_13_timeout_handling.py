"""Category 13: timeout handling."""

import pytest

from tools.picoscope.backend.api import open_device
from tools.picoscope.backend.errors import DeviceTimeoutError


def test_simulated_timeout_raises():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.device.inject_timeout()
        with pytest.raises(DeviceTimeoutError):
            scope.capture_block()


def test_timeout_is_one_shot_and_device_remains_usable():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.device.inject_timeout()
        with pytest.raises(DeviceTimeoutError):
            scope.capture_block()
        # The fault is one-shot; the next capture succeeds.
        cap = scope.capture_block()
        assert cap.acquisition_status == "OK"


def test_timeout_is_a_device_error_subtype():
    assert issubclass(DeviceTimeoutError, Exception)
    from tools.picoscope.backend.errors import DeviceError

    assert issubclass(DeviceTimeoutError, DeviceError)
