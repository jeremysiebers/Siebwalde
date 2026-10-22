"""Category 17: simulator vs real backend selection (no silent fallback).

The real ``ps3000a`` backend must never silently fall back to the simulator. The
decisive invariant is exercised here with a *simulated* absent driver (so the
tests never load the real DLL and never attempt to open a device): a real-device
request that cannot be satisfied raises ``BackendUnavailableError``.
"""

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


@pytest.fixture
def absent_driver(monkeypatch):
    """Simulate an environment where the ps3000a native driver cannot be loaded.

    Patches ``_load_library`` to raise and resets the default adapter's memoized
    library so the mock takes effect. This keeps the no-fallback tests offline and
    device-free (they never call ``ps3000aOpenUnit``).
    """
    import tools.picoscope.backend.adapters.picosdk as picosdk

    def _unavailable():
        raise BackendUnavailableError(
            "ps3000a native driver not available (simulated absence)"
        )

    monkeypatch.setattr(picosdk, "_load_library", _unavailable)
    picosdk.default_adapter._lib = None
    return picosdk


def test_real_discover_raises_without_driver(absent_driver):
    with pytest.raises(BackendUnavailableError):
        discover_devices(backend="ps3000a")


def test_real_open_raises_without_driver(absent_driver):
    with pytest.raises(BackendUnavailableError):
        open_device("ANY-SERIAL", backend="ps3000a")


@pytest.mark.parametrize("alias", ["picosdk", "real"])
def test_real_backend_aliases_map_to_ps3000a(absent_driver, alias):
    with pytest.raises(BackendUnavailableError):
        discover_devices(backend=alias)


def test_unknown_backend_raises():
    with pytest.raises(UnknownBackendError):
        discover_devices(backend="not-a-backend")


def test_real_backend_never_silently_falls_back_to_simulator(absent_driver):
    """The decisive invariant: with an absent driver the real backend must fail
    loudly (never return simulated devices)."""
    sim_serials = {d["serial"] for d in discover_devices(backend="simulator")}
    with pytest.raises(BackendUnavailableError):
        discover_devices(backend="ps3000a")


def test_real_open_failure_is_loud_not_fallback(absent_driver):
    try:
        scope = open_device("DEFINITELY-NOT-A-REAL-SERIAL", backend="ps3000a")
    except BackendUnavailableError:
        return  # offline: loud failure, correct
    except PicoScopeError:
        return  # device not found / error: loud failure, correct
    # If it somehow opened, it must be the real backend, never the simulator.
    assert scope.get_device_info().backend == "ps3000a"
    scope.close_device()
