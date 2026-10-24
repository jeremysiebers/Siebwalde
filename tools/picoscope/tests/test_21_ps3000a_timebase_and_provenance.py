"""Category 21: ps3000a timebase validation + hardware-validated provenance.

Regression tests for the PICO-2 hardware-found regressions:
- the 3406D MSO (driver 2.2.160.7569) rejects timebase 0 with
  PICO_INVALID_TIMEBASE, so an invalid timebase must raise a clear error before
  any SDK call (and the default timebase must be a valid value);
- the stale "NOT hardware-validated (PICO-1)" warning must be gone from the
  Capture provenance (the real path is now hardware-validated in PICO-2).
"""

import inspect

import pytest

from tools.picoscope.backend.adapters import picosdk as _picosdk
from tools.picoscope.backend.adapters.picosdk import Ps3000aDevice
from tools.picoscope.backend.errors import InvalidConfigError


class _TrackCall:
    """Callable with a settable ``.restype`` that records whether it was invoked."""

    def __init__(self):
        self.restype = None
        self.calls = 0

    def __call__(self, *args, **kwargs):
        self.calls += 1
        return 0  # PICO_OK


class _FakeDeviceLib:
    """Minimal fake library for exercising capture_block's pre-SDK validation."""

    def __init__(self):
        self.ps3000aGetTimebase2 = _TrackCall()


def _device():
    return Ps3000aDevice(_FakeDeviceLib(), handle=1, serial="TEST", adapter=None)


def test_invalid_timebase_zero_raises_before_sdk_call():
    device = _device()
    with pytest.raises(InvalidConfigError, match="timebase 0 is invalid"):
        device.capture_block(timebase=0)
    # The clear error must be raised BEFORE ps3000aGetTimebase2 is called.
    assert device._lib.ps3000aGetTimebase2.calls == 0


@pytest.mark.parametrize("tb", [0, 1])
def test_timebase_below_minimum_raises_clear_error(tb):
    device = _device()
    with pytest.raises(InvalidConfigError, match="is invalid for the ps3000a backend"):
        device.capture_block(timebase=tb)
    assert device._lib.ps3000aGetTimebase2.calls == 0


def test_default_timebase_is_valid():
    # With no channels configured, capture_block() proceeds past the timebase
    # check (the default timebase is valid) and fails on the empty-channel
    # validation instead — proving the default is accepted.
    device = _device()
    with pytest.raises(InvalidConfigError, match="At least one analog channel"):
        device.capture_block()


def test_stale_hardware_validation_warning_removed():
    src = inspect.getsource(_picosdk)
    assert "NOT hardware-validated" not in src
    assert "source-backed, unverified" not in src
    assert "hardware-validated 2026-10-22" in src
