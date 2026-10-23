"""Category 22: ps3000a digital (MSO) edge-trigger SDK sequence (mocked SDK).

These tests drive :class:`Ps3000aDevice` against a fake native library that
records the exact ctypes call sequence and structures. They never touch hardware
and prove:

* a digital trigger (D0..D15) uses ``ps3000aSetTriggerDigitalPortProperties`` +
  ``ps3000aSetTriggerChannelConditionsV2`` and **never** ``ps3000aSetSimpleTrigger``
  or the EXTERNAL fallback;
* the digital edge -> direction enum mapping (rising=3, falling=4, either=5);
* a disabled / unconfigured digital port raises :class:`InvalidConfigError`;
* a non-OK SDK status propagates as :class:`DeviceError`;
* ``ps3000aGetTriggerTimeOffset64`` refines ``trigger_index`` and the trigger
  state (triggered / timeout / unused) is recorded in the returned Capture.
"""

import ctypes

import pytest

from tools.picoscope.backend.adapters.picosdk import (
    PS3000A_CONDITION_DONT_CARE,
    PS3000A_CONDITION_TRUE,
    PS3000A_DIGITAL_FALLING,
    PS3000A_DIGITAL_RISING,
    PS3000A_DIGITAL_RISING_OR_FALLING,
    Ps3000aDevice,
)
from tools.picoscope.backend.errors import DeviceError, InvalidConfigError


class _FakeFunction:
    """Recording callable with a settable result (PICO_OK by default)."""

    def __init__(self, result=0):
        self.restype = ctypes.c_int32
        self.result = result
        self.calls = []

    def __call__(self, *args):
        self.calls.append(args)
        return self.result


class _IsReadyFunction:
    def __init__(self, ready):
        self.restype = ctypes.c_int32
        self.ready = ready
        self.calls = []

    def __call__(self, handle, ready_ptr):
        self.calls.append(handle)
        ready_ptr._obj.value = 1 if self.ready else 0
        return 0


class _GetTimebase2Function:
    def __init__(self, interval_ns):
        self.restype = ctypes.c_int32
        self.interval_ns = interval_ns
        self.calls = []

    def __call__(self, handle, tb, no_samples, interval_ptr, oversample, max_ptr, seg):
        self.calls.append(tb)
        interval_ptr._obj.value = self.interval_ns
        return 0


class _GetUnitInfoFunction:
    def __init__(self, text="TEST"):
        self.restype = ctypes.c_int32
        self.text = text
        self.calls = []

    def __call__(self, handle, buffer, length, required_ptr, info):
        self.calls.append(info)
        data = self.text.encode("utf-8")
        length_int = length.value
        ctypes.memmove(buffer, data, min(len(data), length_int - 1))
        required_ptr._obj.value = len(data)
        return 0


class _GetTriggerTimeOffsetFunction:
    def __init__(self, offset_ns, units=2, result=0):
        self.restype = ctypes.c_int32
        self.offset_ns = offset_ns
        self.units = units
        self.result = result
        self.calls = []

    def __call__(self, handle, time_ptr, units_ptr, seg):
        self.calls.append(handle)
        if self.result != 0:
            return self.result
        time_ptr._obj.value = self.offset_ns
        units_ptr._obj.value = self.units
        return 0


class _FakeLib:
    """Fake native ps3000a library covering configure + capture entry points."""

    def __init__(self, ready=True, interval_ns=2048.0, offset_ns=None):
        self.ps3000aSetChannel = _FakeFunction()
        self.ps3000aSetDigitalPort = _FakeFunction()
        self.ps3000aSetSimpleTrigger = _FakeFunction()
        self.ps3000aSetTriggerDigitalPortProperties = _FakeFunction()
        self.ps3000aSetTriggerChannelConditionsV2 = _FakeFunction()
        self.ps3000aGetTimebase2 = _GetTimebase2Function(interval_ns)
        self.ps3000aSetDataBuffers = _FakeFunction()
        self.ps3000aRunBlock = _FakeFunction()
        self.ps3000aIsReady = _IsReadyFunction(ready)
        self.ps3000aGetValues = _FakeFunction()
        self.ps3000aGetTriggerTimeOffset64 = _GetTriggerTimeOffsetFunction(offset_ns)
        self.ps3000aGetUnitInfo = _GetUnitInfoFunction()
        self.ps3000aStop = _FakeFunction()
        self.ps3000aCloseUnit = _FakeFunction()


def _device(ready=True, interval_ns=2048.0, offset_ns=None):
    lib = _FakeLib(ready=ready, interval_ns=interval_ns, offset_ns=offset_ns)
    return Ps3000aDevice(lib, handle=7, serial="TEST", adapter=None), lib


def _directions_first(lib):
    """The first ``PS3000A_DIGITAL_CHANNEL_DIRECTIONS`` struct passed to the SDK."""
    ptr = lib.ps3000aSetTriggerDigitalPortProperties.calls[0][1]
    return ptr._obj[0]


def _conditions_first(lib):
    """The first ``PS3000A_TRIGGER_CONDITIONS_V2`` struct passed to the SDK."""
    ptr = lib.ps3000aSetTriggerChannelConditionsV2.calls[0][1]
    return ptr._obj[0]


# -- digital trigger uses the digital SDK calls, never EXTERNAL/analog --------


def test_digital_trigger_uses_digital_sdk_calls_not_simple_trigger():
    device, lib = _device()
    device.configure_digital_port(0, True, 1000)
    device.configure_trigger("D5", 0.0, "rising", 100, 900)

    # The decisive no-fallback invariant: SetSimpleTrigger is NEVER called.
    assert lib.ps3000aSetSimpleTrigger.calls == []
    assert len(lib.ps3000aSetTriggerDigitalPortProperties.calls) == 1
    assert len(lib.ps3000aSetTriggerChannelConditionsV2.calls) == 1

    d = _directions_first(lib)
    assert d.channel == 5
    assert d.direction == PS3000A_DIGITAL_RISING

    c = _conditions_first(lib)
    assert c.digital == PS3000A_CONDITION_TRUE
    # Every non-digital source remains DONT_CARE (the default zero value).
    assert c.channelA == PS3000A_CONDITION_DONT_CARE
    assert c.channelB == PS3000A_CONDITION_DONT_CARE
    assert c.channelC == PS3000A_CONDITION_DONT_CARE
    assert c.channelD == PS3000A_CONDITION_DONT_CARE
    assert c.external == PS3000A_CONDITION_DONT_CARE
    assert c.aux == PS3000A_CONDITION_DONT_CARE
    assert c.pulseWidthQualifier == PS3000A_CONDITION_DONT_CARE


@pytest.mark.parametrize(
    "edge,direction",
    [
        ("rising", PS3000A_DIGITAL_RISING),
        ("falling", PS3000A_DIGITAL_FALLING),
        ("either", PS3000A_DIGITAL_RISING_OR_FALLING),
    ],
)
def test_digital_edge_maps_to_digital_direction(edge, direction):
    device, lib = _device()
    device.configure_digital_port(0, True, 1000)
    device.configure_trigger("D0", 0.0, edge, 0, 100)
    assert _directions_first(lib).direction == direction


@pytest.mark.parametrize("line", ["D0", "D1", "D7", "D8", "D15"])
def test_digital_line_maps_to_channel_enum(line):
    device, lib = _device()
    port = int(line[1:]) // 8
    device.configure_digital_port(port, True, 1000)
    device.configure_trigger(line, 0.0, "rising", 0, 100)
    assert _directions_first(lib).channel == int(line[1:])


# -- unsupported digital trigger raises, never falls back ---------------------


def test_disabled_digital_port_raises_invalid_config():
    device, lib = _device()
    device.configure_digital_port(0, False, 1000)
    with pytest.raises(InvalidConfigError):
        device.configure_trigger("D3", 0.0, "rising", 0, 100)
    assert lib.ps3000aSetTriggerDigitalPortProperties.calls == []
    assert lib.ps3000aSetSimpleTrigger.calls == []  # no EXTERNAL/analog fallback


def test_unconfigured_digital_port_raises_invalid_config():
    device, lib = _device()
    with pytest.raises(InvalidConfigError):
        device.configure_trigger("D10", 0.0, "rising", 0, 100)
    assert lib.ps3000aSetTriggerDigitalPortProperties.calls == []
    assert lib.ps3000aSetSimpleTrigger.calls == []


def test_other_port_disabled_raises_invalid_config():
    # Trigger on D9 (port 1) while only port 0 is enabled.
    device, lib = _device()
    device.configure_digital_port(0, True, 1000)
    device.configure_digital_port(1, False, 1000)
    with pytest.raises(InvalidConfigError):
        device.configure_trigger("D9", 0.0, "rising", 0, 100)


def test_invalid_digital_trigger_channel_rejected():
    device, lib = _device()
    device.configure_digital_port(0, True, 1000)
    with pytest.raises(InvalidConfigError):
        device.configure_trigger("D16", 0.0, "rising", 0, 100)


# -- SDK error propagation ----------------------------------------------------


def test_digital_trigger_sdk_error_propagates_as_device_error():
    device, lib = _device()
    device.configure_digital_port(0, True, 1000)
    lib.ps3000aSetTriggerDigitalPortProperties.result = 0x05  # a non-OK status
    with pytest.raises(DeviceError):
        device.configure_trigger("D0", 0.0, "rising", 0, 100)


def test_digital_trigger_conditions_sdk_error_propagates():
    device, lib = _device()
    device.configure_digital_port(0, True, 1000)
    lib.ps3000aSetTriggerChannelConditionsV2.result = 0x05
    with pytest.raises(DeviceError):
        device.configure_trigger("D0", 0.0, "rising", 0, 100)


# -- analog / EXTERNAL path is unchanged --------------------------------------


def test_analog_trigger_still_uses_simple_trigger():
    device, lib = _device()
    device.configure_analog_channel("A", True, 5.0, "DC")
    device.configure_trigger("A", 1.0, "rising", 100, 900)
    assert lib.ps3000aSetSimpleTrigger.calls
    assert lib.ps3000aSetTriggerDigitalPortProperties.calls == []


def test_external_trigger_still_uses_simple_trigger():
    device, lib = _device()
    device.configure_analog_channel("A", True, 5.0, "DC")
    device.configure_trigger("EXTERNAL", 1.0, "rising", 100, 900)
    assert lib.ps3000aSetSimpleTrigger.calls
    assert lib.ps3000aSetTriggerDigitalPortProperties.calls == []


# -- capture records the actual trigger state + position ----------------------


def _capture(ready=True, offset_ns=None, interval_ns=2048.0, wait_ms=10):
    device, lib = _device(ready=ready, interval_ns=interval_ns, offset_ns=offset_ns)
    device.configure_digital_port(0, True, 1000)
    device.configure_trigger("D0", 0.0, "rising", 100, 900)
    return device.capture_block(trigger_wait_ms=wait_ms), lib


def test_triggered_capture_records_triggered_state_and_index():
    cap, lib = _capture(ready=True, offset_ns=204800, interval_ns=2048.0)
    assert cap.trigger_status == "triggered"
    assert cap.acquisition_status == "OK"
    assert cap.trigger_position == 100
    # offset_ns 204800 / interval 2048 == 100 samples.
    assert cap.trigger_index == 100
    assert lib.ps3000aGetTriggerTimeOffset64.calls


def test_trigger_index_falls_back_to_pre_on_offset_error():
    # The offset entry point returns a non-OK status -> trigger_index == pre.
    device, lib = _device(ready=True, interval_ns=2048.0, offset_ns=None)
    lib.ps3000aGetTriggerTimeOffset64.result = 0x05
    device.configure_digital_port(0, True, 1000)
    device.configure_trigger("D0", 0.0, "rising", 100, 900)
    cap = device.capture_block(trigger_wait_ms=10)
    assert cap.trigger_status == "triggered"
    assert cap.trigger_index == 100


def test_timeout_capture_records_timeout_state():
    cap, lib = _capture(ready=False, wait_ms=1)
    assert cap.trigger_status == "timeout"
    assert cap.acquisition_status == "TIMEOUT"
    assert cap.trigger_position is None
    assert cap.trigger_index is None


def test_triggerless_capture_records_unused_state():
    device, lib = _device(ready=True)
    device.configure_digital_port(0, True, 1000)
    cap = device.capture_block(trigger_wait_ms=10)
    assert cap.trigger_status == "unused"
    assert cap.acquisition_status == "OK"
    assert cap.trigger_position is None
    assert cap.trigger_index is None
