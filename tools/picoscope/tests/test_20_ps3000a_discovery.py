"""Category 20: real ps3000a discovery enumeration (no duplicates, non-owning).

Regression test for the F1 discovery defect: the previous ``discover()`` looped
``ps3000aOpenUnit(handle, None)`` up to 8 times, which always re-opened the FIRST
unit, so a single device produced 8 duplicate entries for the same serial. The
fix uses the canonical non-owning ``ps3000aEnumerateUnits``.

These tests mock the native library and never touch hardware: ``ps3000aOpenUnit``
is instrumented to prove discovery never opens (and therefore never owns) a unit,
and ``ps3000aEnumerateUnits`` mirrors the real driver's NULL rejection so the
tests fail if discovery ever passes a NULL serials pointer (PICO_NULL_POINTER).
"""

import ctypes

import pytest

from tools.picoscope.backend import ownership
from tools.picoscope.backend.adapters.picosdk import Ps3000aAdapter
from tools.picoscope.backend.errors import DeviceError

_PICO_NULL_POINTER = 0x16  # the status the real driver returns for a NULL serials


class _FakeEnumerateUnits:
    """ctypes-shaped stand-in for ``lib.ps3000aEnumerateUnits``.

    Mirrors the real driver (2.2.160.7569): a NULL ``serials`` pointer returns
    PICO_NULL_POINTER (0x16). With a non-NULL buffer it writes the comma-separated
    serial list and the unit count into the provided ctypes output parameters.
    """

    def __init__(self, serials):
        self._serials = list(serials)
        self.restype = None

    def __call__(self, count_ptr, serials, serial_length_ptr):
        if serials is None:
            return _PICO_NULL_POINTER  # discovery must never trigger this
        count = count_ptr._obj            # the c_int16 passed via ctypes.byref
        serial_length = serial_length_ptr._obj
        data = ",".join(self._serials).encode("utf-8")
        ctypes.memmove(serials, data, len(data))
        count.value = len(self._serials)
        serial_length.value = len(data)
        return 0  # PICO_OK


class _FakeLib:
    """Minimal fake native library exposing only the discovery entry point.

    ``ps3000aOpenUnit`` is instrumented to record calls so tests can prove that
    discovery is non-owning (it must never open a unit).
    """

    def __init__(self, serials):
        self.ps3000aEnumerateUnits = _FakeEnumerateUnits(serials)
        self.open_unit_calls = 0

    def ps3000aOpenUnit(self, *args, **kwargs):
        self.open_unit_calls += 1
        return 0  # PICO_OK (never expected to be reached during discovery)


def _adapter_with(serials):
    adapter = Ps3000aAdapter()
    adapter._lib = _FakeLib(serials)
    return adapter


def test_discover_single_unit_returns_exactly_once_and_non_owning():
    adapter = _adapter_with(["JY908/0278"])
    devices = adapter.discover()

    assert [d["serial"] for d in devices] == ["JY908/0278"]
    assert len(devices) == 1  # exactly once, no duplicates
    assert devices[0]["model"] == "3406D MSO"
    assert devices[0]["variant"] == ""
    assert devices[0]["driver_version"] == ""

    # Discovery is non-owning: never opened a unit, never acquired ownership.
    assert adapter._lib.open_unit_calls == 0
    assert ownership.owned_device_ids() == frozenset()


def test_discover_uses_non_null_buffer_no_null_pointer():
    # The mock returns PICO_NULL_POINTER for a NULL serials pointer; a successful
    # discover() therefore proves a non-NULL buffer was passed on every call.
    adapter = _adapter_with(["JY908/0278"])
    devices = adapter.discover()
    assert [d["serial"] for d in devices] == ["JY908/0278"]


def test_discover_multiple_units_returns_each_exactly_once():
    adapter = _adapter_with(["AQ005/139", "VDR61/356", "ZOR14/107"])
    devices = adapter.discover()

    serials = [d["serial"] for d in devices]
    assert serials == ["AQ005/139", "VDR61/356", "ZOR14/107"]
    assert len(serials) == len(set(serials)) == 3
    assert adapter._lib.open_unit_calls == 0


def test_discover_zero_units_returns_empty():
    adapter = _adapter_with([])
    assert adapter.discover() == []
    assert adapter._lib.open_unit_calls == 0


def test_discover_raises_device_error_on_enumerate_failure():
    class _ErrorEnumerateUnits:
        restype = None

        def __call__(self, *args):
            return 0x05  # a non-OK PICO_STATUS

    adapter = Ps3000aAdapter()
    adapter._lib = _FakeLib([])
    adapter._lib.ps3000aEnumerateUnits = _ErrorEnumerateUnits()

    with pytest.raises(DeviceError):
        adapter.discover()
