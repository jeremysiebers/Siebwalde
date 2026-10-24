"""Category 1: device discovery."""

from tools.picoscope.backend.api import discover_devices
from tools.picoscope.backend.errors import UnknownBackendError

REQUIRED_KEYS = {"serial", "model", "variant", "driver_version"}


def test_discover_simulator_returns_devices():
    devices = discover_devices(backend="simulator")
    assert isinstance(devices, list)
    assert len(devices) >= 1
    for d in devices:
        assert REQUIRED_KEYS <= set(d.keys())


def test_discover_default_backend_is_simulator():
    assert discover_devices() == discover_devices(backend="simulator")


def test_discover_unknown_backend_raises():
    try:
        discover_devices(backend="bogus")
    except UnknownBackendError:
        pass
    else:
        raise AssertionError("expected UnknownBackendError")


def test_simulator_serial_is_stable_and_unique():
    devices = discover_devices(backend="simulator")
    serials = [d["serial"] for d in devices]
    assert len(serials) == len(set(serials))
    assert "SIM0001" in serials
