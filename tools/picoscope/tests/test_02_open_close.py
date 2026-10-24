"""Category 2: open / close lifecycle."""

from tools.picoscope.backend.api import open_device
from tools.picoscope.backend.model import DeviceIdentity


def test_open_returns_handle_with_info():
    with open_device("SIM0001", backend="simulator") as scope:
        assert not scope.closed
        info = scope.get_device_info()
        assert isinstance(info, DeviceIdentity)
        assert info.serial == "SIM0001"
        assert info.backend == "simulator"
        assert info.model == "3406D MSO"


def test_close_is_idempotent():
    scope = open_device("SIM0001", backend="simulator")
    scope.close_device()
    assert scope.closed
    scope.close_device()  # no error on second close
    assert scope.closed


def test_context_manager_closes():
    scope = open_device("SIM0001", backend="simulator")
    with scope:
        assert not scope.closed
    assert scope.closed


def test_reopen_after_close():
    scope = open_device("SIM0001", backend="simulator")
    scope.close_device()
    scope2 = open_device("SIM0001", backend="simulator")
    assert not scope2.closed
    scope2.close_device()
