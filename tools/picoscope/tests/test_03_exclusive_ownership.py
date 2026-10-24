"""Category 3: exclusive ownership."""

import pytest

from tools.picoscope.backend import ownership
from tools.picoscope.backend.api import open_device
from tools.picoscope.backend.errors import DeviceBusyError


def test_second_open_of_owned_device_raises():
    scope = open_device("SIM0001", backend="simulator")
    try:
        with pytest.raises(DeviceBusyError):
            open_device("SIM0001", backend="simulator")
    finally:
        scope.close_device()


def test_different_device_is_not_blocked():
    scope = open_device("SIM0001", backend="simulator")
    try:
        scope2 = open_device("SIM0002", backend="simulator")
        scope2.close_device()
    finally:
        scope.close_device()


def test_ownership_reflects_registry():
    assert "SIM0001" not in ownership.owned_device_ids()
    scope = open_device("SIM0001", backend="simulator")
    assert "SIM0001" in ownership.owned_device_ids()
    assert ownership.is_owned("SIM0001")
    scope.close_device()
    assert "SIM0001" not in ownership.owned_device_ids()


def test_release_is_deterministic_on_exception():
    scope = open_device("SIM0001", backend="simulator")
    try:
        raise RuntimeError("boom")
    except RuntimeError:
        pass
    finally:
        scope.close_device()
    # Ownership was released by close in the finally block.
    assert not ownership.is_owned("SIM0001")
    # And the device can be re-opened.
    scope2 = open_device("SIM0001", backend="simulator")
    scope2.close_device()
