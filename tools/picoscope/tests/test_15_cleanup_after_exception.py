"""Category 15: cleanup-after-exception."""

import pytest

from tools.picoscope.backend import ownership
from tools.picoscope.backend.api import open_device
from tools.picoscope.backend.errors import DeviceNotFoundError


def test_context_manager_releases_on_exception():
    with pytest.raises(RuntimeError):
        with open_device("SIM0001", backend="simulator") as scope:
            raise RuntimeError("user code failed")
    # The handle's __exit__ closed and released even though the body raised.
    assert scope.closed
    assert not ownership.is_owned("SIM0001")


def test_open_failure_releases_ownership():
    # open_device acquires the token, then adapter.open raises -> token released.
    with pytest.raises(DeviceNotFoundError):
        open_device("SIM_DOES_NOT_EXIST", backend="simulator")
    assert not ownership.is_owned("SIM_DOES_NOT_EXIST")


def test_device_usable_again_after_exception_cleanup():
    with pytest.raises(RuntimeError):
        with open_device("SIM0001", backend="simulator"):
            raise RuntimeError("boom")
    scope = open_device("SIM0001", backend="simulator")
    assert not scope.closed
    scope.close_device()
