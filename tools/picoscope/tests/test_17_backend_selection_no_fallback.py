"""Category 17: simulator vs real backend selection (no silent fallback)."""

import importlib

import pytest

from tools.picoscope.backend.api import discover_devices, open_device
from tools.picoscope.backend.errors import (
    BackendUnavailableError,
    PicoScopeError,
    UnknownBackendError,
)


def test_picosdk_module_imports_without_driver():
    # The adapter module must import cleanly even when ps3000a.dll is absent
    # (import-guarded, lazy library loading).
    mod = importlib.import_module("tools.picoscope.backend.adapters.picosdk")
    assert mod.BACKEND_NAME == "ps3000a"


def test_real_discover_raises_without_driver():
    with pytest.raises(BackendUnavailableError):
        discover_devices(backend="ps3000a")


def test_real_open_raises_without_driver():
    with pytest.raises(BackendUnavailableError):
        open_device("ANY-SERIAL", backend="ps3000a")


@pytest.mark.parametrize("alias", ["picosdk", "real"])
def test_real_backend_aliases_map_to_ps3000a(alias):
    with pytest.raises(BackendUnavailableError):
        discover_devices(backend=alias)


def test_unknown_backend_raises():
    with pytest.raises(UnknownBackendError):
        discover_devices(backend="not-a-backend")


def test_real_backend_never_silently_falls_back_to_simulator():
    """The decisive invariant: the real backend must never return simulated
    devices, and any failure must be loud (raise), never a quiet simulator
    result."""
    sim_serials = {d["serial"] for d in discover_devices(backend="simulator")}
    try:
        real = discover_devices(backend="ps3000a")
    except BackendUnavailableError:
        # Offline (no driver): a loud failure is the required behavior.
        return
    # If a driver were present, the result must be real devices only.
    assert isinstance(real, list)
    real_serials = {d["serial"] for d in real}
    assert not (real_serials & sim_serials)


def test_real_open_failure_is_loud_not_fallback():
    try:
        scope = open_device("DEFINITELY-NOT-A-REAL-SERIAL", backend="ps3000a")
    except BackendUnavailableError:
        return  # offline: loud failure, correct
    except PicoScopeError:
        return  # device not found / error: loud failure, correct
    # If it somehow opened, it must be the real backend, never the simulator.
    assert scope.get_device_info().backend == "ps3000a"
    scope.close_device()
