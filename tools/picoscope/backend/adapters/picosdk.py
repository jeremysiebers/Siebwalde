"""Vendor-specific ``ps3000a`` ctypes adapter (PICO-2 hardware path).

All ``ps3000a``/ctypes calls are isolated in this module. The module imports and
is introspectable **without** the native driver present: the library is loaded
lazily and a missing ``ps3000a.dll`` raises :class:`BackendUnavailableError`.

.. note::

    Hardware-validated in PICO-2 against a real 3406D MSO (driver 2.2.160.7569)
    on 2026-10-22. The call flow is transcribed from the official
    ``picosdk-python-wrappers`` ``ps3000aExamples/ps3000aBlockMSOExample.py``, and
    the ctypes prototypes/constants were verified against ``picosdk`` 1.1
    (``picosdk.ps3000a`` / ``picosdk.constants``), with genuine parameter-type /
    pointer / return-status defects corrected.

    A real-device request that cannot be satisfied raises — it **never** silently
    falls back to the simulator.
"""

from __future__ import annotations

import ctypes
import os
import sys
from datetime import datetime, timezone

import numpy as np

from ..config import AcquisitionConfig
from ..errors import (
    BackendUnavailableError,
    DeviceError,
    DeviceNotFoundError,
    InvalidConfigError,
)
from ..model import Capture, DeviceIdentity
from ... import version as _version

BACKEND_NAME = "ps3000a"

#: PicoScope 7 install folder containing the vendor ``ps3000a.dll`` (and its
#: ``picoipp.dll`` dependency). The DLL is NOT on the default ctypes search path
#: (it is not in System32/SysWOW64), so this folder is registered as a DLL search
#: directory before ctypes loads the driver.
_PICO7_DLL_DIR = r"C:\Program Files\Pico Technology\PicoScope 7 T&M Stable"

# --------------------------------------------------------------------------- #
# Constants transcribed from the official picosdk.ps3000a wrapper (source-backed).
# --------------------------------------------------------------------------- #
PS3000A_MAX_CHANNELS = 4

PS3000A_CHANNEL_A = 0
PS3000A_CHANNEL_B = 1
PS3000A_CHANNEL_C = 2
PS3000A_CHANNEL_D = 3
PS3000A_CHANNEL_EXTERNAL = 4

PS3000A_AC = 0
PS3000A_DC = 1

PS3000A_DIGITAL_PORT0 = 0x80
PS3000A_DIGITAL_PORT1 = 0x81

# Trigger threshold directions.
PS3000A_RISING = 2
PS3000A_FALLING = 3
PS3000A_RISING_OR_FALLING = 4

# Analog ranges (enum value -> full-scale volts).
PS3000A_RANGES = {
    0: 0.01, 1: 0.02, 2: 0.05, 3: 0.1, 4: 0.2, 5: 0.5,
    6: 1.0, 7: 2.0, 8: 5.0, 9: 10.0, 10: 20.0, 11: 50.0,
}

# Unit-info selectors (PICO_INFO enum values, verified against picosdk 1.1).
PS3000A_INFO_DRIVER_VERSION = 0x00000000
PS3000A_INFO_USB_VERSION = 0x00000001
PS3000A_INFO_HARDWARE_VERSION = 0x00000002
PS3000A_INFO_VARIANT_INFO = 0x00000003
PS3000A_INFO_BATCH_AND_SERIAL = 0x00000004

# PICO_STATUS subset (verified against picosdk 1.1 picosdk.constants.PICO_STATUS).
_PICO_OK = 0x00000000
_PICO_NOT_FOUND = 0x03  # PICO_NOT_FOUND (unit not found)
# "Success-with-warning" open statuses: the unit opened, but must be switched to
# an appropriate power source via ps3000aChangePowerSource before use.
_PICO_POWER_SUPPLY_NOT_CONNECTED = 0x0000011A  # 282
_PICO_USB3_0_DEVICE_NON_USB3_0_PORT = 0x0000011E  # 286

#: ADC full-scale count for volts<->ADC conversions (matches _adc_to_volts).
#: The official wrapper's maximum_value() reports 32767; the exact max-ADC value
#: is a hardware-calibration detail re-confirmed on-device in PICO-2.
_ADC_MAX = 32512.0

#: Digital port input full-scale (volts) for logic-level threshold conversion.
#: The exact digital threshold unit semantics are re-confirmed in PICO-2
#: (see PICO-2-PREFLIGHT.md "Known risks / uncertainties").
_DIGITAL_FULL_SCALE_V = 5.0

#: External trigger input full-scale (volts). Hardware-dependent; re-confirmed
#: on-device in PICO-2.
_EXTERNAL_TRIGGER_FULL_SCALE_V = 20.0

#: Minimum valid ps3000a timebase index (observed on the 3406D MSO, driver
#: 2.2.160.7569): timebase 0 returns PICO_INVALID_TIMEBASE (0xE); tb>=2 is valid.
PS3000A_MIN_TIMEBASE = 2

# The 3406D MSO is a ps3000a device (4 analog + 16 digital, 2 ports of 8).
DIGITAL_PORT_ENUM = {0: PS3000A_DIGITAL_PORT0, 1: PS3000A_DIGITAL_PORT1}
EDGE_DIRECTION = {"rising": PS3000A_RISING, "falling": PS3000A_FALLING, "either": PS3000A_RISING_OR_FALLING}

# --------------------------------------------------------------------------- #
# Digital (MSO) trigger enums + structures (verified against picosdk 1.1).
# --------------------------------------------------------------------------- #
#: Digital channel enum: ``PS3000A_DIGITAL_CHANNEL_N`` == N (D0 == 0 ... D15 == 15).
#: The digital line ``"Dn"`` therefore maps directly to channel ``n``.

#: ``PICO_DIGITAL_DIRECTION`` / ``PS3000A_DIGITAL_DIRECTION`` enum values.
PS3000A_DIGITAL_DONT_CARE = 0
PS3000A_DIGITAL_LOW = 1
PS3000A_DIGITAL_HIGH = 2
PS3000A_DIGITAL_RISING = 3
PS3000A_DIGITAL_FALLING = 4
PS3000A_DIGITAL_RISING_OR_FALLING = 5

#: Digital edge -> digital trigger direction enum (NOT the analog threshold
#: direction used by ``ps3000aSetSimpleTrigger``; the digital values differ).
DIGITAL_EDGE_DIRECTION = {
    "rising": PS3000A_DIGITAL_RISING,
    "falling": PS3000A_DIGITAL_FALLING,
    "either": PS3000A_DIGITAL_RISING_OR_FALLING,
}

#: ``PS3000A_TRIGGER_STATE`` (trigger condition) enum values.
PS3000A_CONDITION_DONT_CARE = 0
PS3000A_CONDITION_TRUE = 1
PS3000A_CONDITION_FALSE = 2

#: ``PS3000A_TIME_UNITS`` -> seconds-per-unit, used to convert the hardware
#: trigger time offset returned by ``ps3000aGetTriggerTimeOffset64``.
PS3000A_TIME_UNITS_S = {
    0: 1e-15,  # FS
    1: 1e-12,  # PS
    2: 1e-9,   # NS
    3: 1e-6,   # US
    4: 1e-3,   # MS
    5: 1.0,    # S
}


class PS3000A_DIGITAL_CHANNEL_DIRECTIONS(ctypes.Structure):
    """``PS3000A_DIGITAL_CHANNEL_DIRECTIONS`` (``_pack_ = 1``)."""

    _pack_ = 1
    _fields_ = [("channel", ctypes.c_int32), ("direction", ctypes.c_int32)]


class PS3000A_TRIGGER_CONDITIONS_V2(ctypes.Structure):
    """``PS3000A_TRIGGER_CONDITIONS_V2`` (``_pack_ = 1``)."""

    _pack_ = 1
    _fields_ = [
        ("channelA", ctypes.c_uint32),
        ("channelB", ctypes.c_uint32),
        ("channelC", ctypes.c_uint32),
        ("channelD", ctypes.c_uint32),
        ("external", ctypes.c_uint32),
        ("aux", ctypes.c_uint32),
        ("pulseWidthQualifier", ctypes.c_uint32),
        ("digital", ctypes.c_uint32),
    ]


def _library_name() -> str:
    return "ps3000a.dll" if sys.platform == "win32" else "libps3000a.so"


# DLL-search-directory handles registered by _register_dll_directory, held for
# the process lifetime so the driver and its dependencies stay resolvable.
_dll_directory_handles = []


def _register_dll_directory():
    """Register the PicoScope 7 folder as a DLL search directory (if present).

    ``ps3000a.dll`` (and its ``picoipp.dll`` dependency) live in the PicoScope 7
    install folder, which is NOT on the default ctypes search path. Uses
    :func:`os.add_dll_directory` guarded by a directory-exists check.
    """
    add_dll_directory = getattr(os, "add_dll_directory", None)
    if add_dll_directory is None or not os.path.isdir(_PICO7_DLL_DIR):
        return
    try:
        _dll_directory_handles.append(add_dll_directory(_PICO7_DLL_DIR))
    except OSError:  # pragma: no cover - platform/environment dependent
        pass


def _load_library():
    """Lazily load the native ps3000a driver.

    Raises :class:`BackendUnavailableError` if the driver is not installed. No
    fallback to the simulator ever happens here.
    """
    name = _library_name()
    _register_dll_directory()
    last_error = None
    for loader in (getattr(ctypes, "WinDLL", None), ctypes.CDLL):
        if loader is None:
            continue
        try:
            lib = loader(name)
        except (OSError, AttributeError) as exc:  # pragma: no cover - DLL-dependent
            last_error = exc
            continue
        return lib
    raise BackendUnavailableError(
        f"ps3000a native driver ({name!r}) is not available: {last_error}. "
        "Install the PicoSDK drivers (PICO-2 preflight). No simulator fallback."
    )


class Ps3000aDevice:
    """A single opened ps3000a unit handle (hardware-validated in PICO-2)."""

    def __init__(self, library, handle, serial, adapter):
        self._lib = library
        self._handle = handle
        self._serial = serial
        self._adapter = adapter
        self._config = AcquisitionConfig()
        self._closed = False

    @property
    def config(self) -> AcquisitionConfig:
        """The current (pending) acquisition configuration."""
        return self._config

    # -- identity -----------------------------------------------------------
    def get_device_info(self) -> DeviceIdentity:
        info = self._get_unit_info(PS3000A_INFO_BATCH_AND_SERIAL, 256)
        driver = self._get_unit_info(PS3000A_INFO_DRIVER_VERSION, 64)
        variant = self._get_unit_info(PS3000A_INFO_VARIANT_INFO, 64)
        return DeviceIdentity(
            serial=self._serial,
            model="3406D MSO",
            variant=variant.strip() or "unknown",
            driver_version=driver.strip() or "unknown",
            backend=BACKEND_NAME,
            extra={"unit_info": info.strip()},
        )

    def _get_unit_info(self, info_enum: int, length: int) -> str:
        # ps3000aGetUnitInfo(handle, info_string, stringLength, requiredSize, info)
        func = self._lib.ps3000aGetUnitInfo
        func.restype = ctypes.c_int32
        buffer = ctypes.create_string_buffer(length)
        required = ctypes.c_int16()
        status = func(self._handle, buffer, ctypes.c_int16(length), ctypes.byref(required), ctypes.c_int32(info_enum))
        if status != _PICO_OK:
            return ""
        return buffer.value.decode("utf-8", errors="replace")

    # -- configuration ------------------------------------------------------
    def configure_analog_channel(self, channel, enabled, range_v, coupling):
        ch_enum = {"A": PS3000A_CHANNEL_A, "B": PS3000A_CHANNEL_B,
                   "C": PS3000A_CHANNEL_C, "D": PS3000A_CHANNEL_D}[channel]
        range_enum = self._nearest_range(range_v)
        coup = PS3000A_DC if coupling == "DC" else PS3000A_AC
        cfg = self._config.add_analog_channel(channel, enabled, range_v, coupling)
        # ps3000aSetChannel(handle, channel, enabled, coupling, range, analog_offset)
        func = self._lib.ps3000aSetChannel
        func.restype = ctypes.c_int32
        status = func(
            self._handle,
            ctypes.c_int32(ch_enum),
            ctypes.c_int16(1 if enabled else 0),
            ctypes.c_int32(coup),
            ctypes.c_int32(range_enum),
            ctypes.c_float(0.0),
        )
        self._check_status(status, "ps3000aSetChannel")
        return cfg

    def configure_digital_port(self, port, enabled, threshold_mv):
        cfg = self._config.add_digital_port(port, enabled, threshold_mv)
        # ps3000aSetDigitalPort(handle, port, enabled, logicLevel)
        #   logicLevel is an int16 ADC count (NOT a float voltage); the digital
        #   input full-scale is ~±5 V. Exact unit semantics re-confirmed in PICO-2.
        func = self._lib.ps3000aSetDigitalPort
        func.restype = ctypes.c_int32
        logic_level = self._volts_to_adc_count(threshold_mv / 1000.0, _DIGITAL_FULL_SCALE_V)
        status = func(
            self._handle,
            ctypes.c_int32(DIGITAL_PORT_ENUM[port]),
            ctypes.c_int16(1 if enabled else 0),
            ctypes.c_int16(logic_level),
        )
        self._check_status(status, "ps3000aSetDigitalPort")
        return cfg

    def configure_trigger(self, source, threshold, edge, pre_samples, post_samples):
        trig = self._config.set_trigger(source, threshold, edge, pre_samples, post_samples)
        if source == "EXTERNAL":
            self._set_simple_trigger(PS3000A_CHANNEL_EXTERNAL, threshold, edge,
                                     _EXTERNAL_TRIGGER_FULL_SCALE_V)
        elif source in ("A", "B", "C", "D"):
            src_enum = {"A": PS3000A_CHANNEL_A, "B": PS3000A_CHANNEL_B,
                        "C": PS3000A_CHANNEL_C, "D": PS3000A_CHANNEL_D}[source]
            range_v = next(
                (c.range_v for c in self._config.analog_channels if c.channel == source),
                _EXTERNAL_TRIGGER_FULL_SCALE_V,
            )
            self._set_simple_trigger(src_enum, threshold, edge, range_v)
        else:
            # Digital-source trigger (D0..D15): use the two digital-trigger SDK
            # calls. There is deliberately NO fallback to EXTERNAL/analog — an
            # unsupported digital trigger (e.g. its port is not enabled) raises.
            self._set_digital_trigger(source, edge)
        return trig

    def _set_simple_trigger(self, src_enum: int, threshold: float, edge: str, range_v: float):
        """Analog / EXTERNAL trigger via ``ps3000aSetSimpleTrigger`` (unchanged)."""
        # ps3000aSetSimpleTrigger(handle, enabled, source, threshold, direction, delay, auto_trigger_ms)
        #   threshold is an int16 ADC count (NOT a float voltage), converted from
        #   the source channel's configured range.
        func = self._lib.ps3000aSetSimpleTrigger
        func.restype = ctypes.c_int32
        threshold_adc = self._volts_to_adc_count(threshold, range_v)
        status = func(
            self._handle,
            ctypes.c_int16(1),
            ctypes.c_int32(src_enum),
            ctypes.c_int16(threshold_adc),
            ctypes.c_int32(EDGE_DIRECTION[edge]),
            ctypes.c_int32(0),
            ctypes.c_int16(0),
        )
        self._check_status(status, "ps3000aSetSimpleTrigger")

    def _set_digital_trigger(self, source: str, edge: str):
        """Digital edge trigger (D0..D15) via the digital-trigger SDK calls.

        Official ps3000a MSO block sequence:

        1. the digital port is already enabled via ``ps3000aSetDigitalPort``;
        2. ``ps3000aSetTriggerDigitalPortProperties(handle, [DIRECTIONS{channel=Dn,
           direction=RISING|FALLING|RISING_OR_FALLING}], 1)``;
        3. ``ps3000aSetTriggerChannelConditionsV2(handle, [CONDITIONS_V2{all
           DONT_CARE, digital=CONDITION_TRUE}], 1)``.

        There is no ``ps3000aSetDigitalTrigger`` entry point. If the target digital
        port is not enabled this raises :class:`InvalidConfigError` — it never falls
        back to EXTERNAL or an analog channel.
        """
        line_index = int(source[1:])  # "D0".."D15" -> 0..15
        port = line_index // 8
        direction = DIGITAL_EDGE_DIRECTION[edge]

        port_cfg = next((p for p in self._config.digital_ports if p.port == port), None)
        if port_cfg is None or not port_cfg.enabled:
            raise InvalidConfigError(
                f"Digital trigger source {source!r} requires digital port {port} "
                "to be enabled; refusing to fall back to EXTERNAL/analog"
            )

        # ps3000aSetTriggerDigitalPortProperties(handle, directions*, int16 nDirections)
        directions = (PS3000A_DIGITAL_CHANNEL_DIRECTIONS * 1)()
        directions[0].channel = line_index
        directions[0].direction = direction
        set_props = self._lib.ps3000aSetTriggerDigitalPortProperties
        set_props.restype = ctypes.c_int32
        status = set_props(self._handle, ctypes.byref(directions), ctypes.c_int16(1))
        self._check_status(status, "ps3000aSetTriggerDigitalPortProperties")

        # ps3000aSetTriggerChannelConditionsV2(handle, conditions*, int16 nConditions)
        conditions = (PS3000A_TRIGGER_CONDITIONS_V2 * 1)()
        conditions[0].digital = PS3000A_CONDITION_TRUE
        # All other sources stay DONT_CARE (the default zero value).
        set_conditions = self._lib.ps3000aSetTriggerChannelConditionsV2
        set_conditions.restype = ctypes.c_int32
        status = set_conditions(self._handle, ctypes.byref(conditions), ctypes.c_int16(1))
        self._check_status(status, "ps3000aSetTriggerChannelConditionsV2")

    # -- acquisition ----------------------------------------------------------
    def capture_block(self, pre_samples=None, post_samples=None, timebase=None, trigger_wait_ms=None):
        # Transcribed from ps3000aBlockMSOExample.py flow.
        pre = self._config.pre_samples if pre_samples is None else pre_samples
        post = self._config.post_samples if post_samples is None else post_samples
        tb = self._config.timebase if timebase is None else timebase
        wait_s = (5000 if trigger_wait_ms is None else trigger_wait_ms) / 1000.0

        # The 3406D MSO rejects timebase 0 (PICO_INVALID_TIMEBASE); validate the
        # timebase before any SDK call so the failure is clear, not a raw PICO_STATUS.
        if tb < PS3000A_MIN_TIMEBASE:
            raise InvalidConfigError(
                f"timebase {tb} is invalid for the ps3000a backend "
                f"(valid: >= {PS3000A_MIN_TIMEBASE}; observed PICO_INVALID_TIMEBASE "
                f"on the 3406D MSO at timebase 0)"
            )

        working = AcquisitionConfig(
            analog_channels=list(self._config.analog_channels),
            digital_ports=list(self._config.digital_ports),
            trigger=self._config.trigger,
            pre_samples=pre,
            post_samples=post,
            timebase=tb,
        )
        working.validate()

        # ps3000aGetTimebase2(handle, timebase, noSamples, timeIntervalNs, oversample, maxSamples, segmentIndex)
        interval_ns = ctypes.c_float()
        max_samples = ctypes.c_int32()
        func = self._lib.ps3000aGetTimebase2
        func.restype = ctypes.c_int32
        status = func(
            self._handle,
            ctypes.c_int32(tb),
            ctypes.c_int32(pre + post),
            ctypes.byref(interval_ns),
            ctypes.c_int16(1),
            ctypes.byref(max_samples),
            ctypes.c_int32(0),
        )
        self._check_status(status, "ps3000aGetTimebase2")
        interval_ns_int = int(round(interval_ns.value))

        sample_count = pre + post
        analog_samples = {}
        for ch in working.enabled_analog_channels():
            ch_enum = {"A": PS3000A_CHANNEL_A, "B": PS3000A_CHANNEL_B,
                       "C": PS3000A_CHANNEL_C, "D": PS3000A_CHANNEL_D}[ch.channel]
            # A NULL bufferMin makes the driver return *unsigned* ADC data, which
            # would break the signed _adc_to_volts interpretation; allocate and
            # keep a min buffer alive (stored beside the max buffer) through GetValues.
            buf = (ctypes.c_int16 * sample_count)()
            buf_min = (ctypes.c_int16 * sample_count)()
            setbuf = self._lib.ps3000aSetDataBuffers
            setbuf.restype = ctypes.c_int32
            status = setbuf(
                self._handle,
                ctypes.c_int32(ch_enum),
                ctypes.byref(buf),
                ctypes.byref(buf_min),
                ctypes.c_int32(sample_count),
                ctypes.c_int32(0),
                ctypes.c_int32(0),
            )
            self._check_status(status, "ps3000aSetDataBuffers")
            analog_samples[ch.channel] = (buf, buf_min)

        digital_bufs = {}
        for port in working.enabled_digital_ports():
            # Digital data is 16-bit bit-packed and unsigned; a NULL bufferMin is
            # valid (the driver writes the raw word to bufferMax only).
            buf = (ctypes.c_int16 * sample_count)()
            setbuf = self._lib.ps3000aSetDataBuffers
            setbuf.restype = ctypes.c_int32
            status = setbuf(
                self._handle,
                ctypes.c_int32(DIGITAL_PORT_ENUM[port.port]),
                ctypes.byref(buf),
                ctypes.POINTER(ctypes.c_int16)(),
                ctypes.c_int32(sample_count),
                ctypes.c_int32(0),
                ctypes.c_int32(0),
            )
            self._check_status(status, "ps3000aSetDataBuffers (digital)")
            digital_bufs[port.port] = buf

        # ps3000aRunBlock(handle, preSamples, postSamples, timebase, oversample,
        #                 timeIndisposedMs, segmentIndex, callback, pParameter)
        run = self._lib.ps3000aRunBlock
        run.restype = ctypes.c_int32
        status = run(
            self._handle,
            ctypes.c_int32(pre),
            ctypes.c_int32(post),
            ctypes.c_int32(tb),
            ctypes.c_int16(1),
            None,
            ctypes.c_int32(0),
            None,
            None,
        )
        self._check_status(status, "ps3000aRunBlock")

        # Bounded readiness poll (ps3000aIsReady). Returns False (rather than
        # raising) when no trigger arrives within the bounded wait, so the actual
        # trigger state can be recorded in the returned Capture metadata.
        ready = self._wait_ready(timeout_s=wait_s)

        # ps3000aGetValues(handle, startIndex, noOfSamples, downSampleRatio, mode, segmentIndex, overflow)
        get_values = self._lib.ps3000aGetValues
        get_values.restype = ctypes.c_int32
        overflow = ctypes.c_int16()
        no_samples = ctypes.c_int32(sample_count)
        status = get_values(
            self._handle,
            ctypes.c_int32(0),
            ctypes.byref(no_samples),
            ctypes.c_int32(1),
            ctypes.c_int32(0),
            ctypes.c_int32(0),
            ctypes.byref(overflow),
        )
        self._check_status(status, "ps3000aGetValues")

        analog_out = {}
        for ch, (buf, _buf_min) in analog_samples.items():
            analog_out[ch] = self._adc_to_volts(np.frombuffer(buf, dtype=np.int16).astype(np.float64), ch)

        digital_out = {}
        for port, buf in digital_bufs.items():
            # splitMSODataFast-equivalent: each 16-bit word packs 8 digital lines.
            words = np.frombuffer(buf, dtype=np.int16).astype(np.uint16)
            for line_index in range(8):
                line_name = f"D{port * 8 + line_index}"
                digital_out[line_name] = ((words >> line_index) & 1).astype(np.uint8)

        # Record the ACTUAL trigger state and position.
        if working.trigger is None:
            trigger_status = "unused"
            acquisition_status = "OK"
            trigger_position = None
            trigger_index = None
        elif ready:
            trigger_status = "triggered"
            acquisition_status = "OK"
            trigger_position = pre
            trigger_index = self._trigger_sample_index(pre, interval_ns_int)
        else:
            trigger_status = "timeout"
            acquisition_status = "TIMEOUT"
            trigger_position = None
            trigger_index = None

        return Capture(
            capture_id=os.urandom(16).hex(),
            timestamp=datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
            device_identity=self.get_device_info(),
            analog_channels=list(working.analog_channels),
            digital_ports=list(working.digital_ports),
            sample_interval_ns=interval_ns_int,
            sample_count=sample_count,
            trigger=working.trigger,
            trigger_position=trigger_position,
            trigger_status=trigger_status,
            trigger_index=trigger_index,
            channel_mapping={"analog": {ch: i for i, ch in enumerate(analog_out)},
                             "digital": {line: i for i, line in enumerate(digital_out)}},
            acquisition_status=acquisition_status,
            errors=[],
            warnings=[],
            backend_version="ps3000a (hardware-validated 2026-10-22)",
            software_version=_version.__version__,
            provenance={"backend": BACKEND_NAME, "hardware_validated": True,
                        "repo_head": _version.git_commit()},
            analog_samples=analog_out,
            digital_samples=digital_out,
        )

    def _wait_ready(self, timeout_s: float) -> bool:
        import time
        func = self._lib.ps3000aIsReady
        func.restype = ctypes.c_int32
        ready = ctypes.c_int16()
        deadline = time.monotonic() + timeout_s
        while True:
            status = func(self._handle, ctypes.byref(ready))
            self._check_status(status, "ps3000aIsReady")
            if ready.value:
                return True
            if time.monotonic() >= deadline:
                # No trigger within the bounded wait: report False (timeout) rather
                # than raising, so capture_block can record trigger_status="timeout".
                return False
            time.sleep(0.001)

    def _trigger_sample_index(self, pre: int, interval_ns: int) -> int:
        """Actual trigger sample index, refined from the hardware offset if possible.

        ``ps3000aGetTriggerTimeOffset64`` reports the trigger point relative to the
        start of the capture; converted to samples this is the actual trigger index.
        On failure (or absent entry point) the nominal ``pre`` is returned.
        """
        func = getattr(self._lib, "ps3000aGetTriggerTimeOffset64", None)
        if func is None or interval_ns <= 0:
            return pre
        func.restype = ctypes.c_int32
        time_value = ctypes.c_int64()
        time_units = ctypes.c_int32()
        status = func(self._handle, ctypes.byref(time_value), ctypes.byref(time_units), ctypes.c_uint32(0))
        if status != _PICO_OK:
            return pre
        sec_per_unit = PS3000A_TIME_UNITS_S.get(time_units.value)
        if sec_per_unit is None:
            return pre
        offset_ns = time_value.value * sec_per_unit * 1e9
        return int(round(offset_ns / interval_ns))

    def _nearest_range(self, range_v: float) -> int:
        best = min(PS3000A_RANGES.items(), key=lambda kv: abs(kv[1] - range_v))
        return best[0]

    @staticmethod
    def _volts_to_adc_count(volts: float, full_scale_v: float) -> int:
        """Convert a voltage to a ps3000a int16 ADC count (bipolar, ±full scale)."""
        value = int(round(volts / full_scale_v * _ADC_MAX))
        return max(-32768, min(32767, value))

    def _adc_to_volts(self, adc: np.ndarray, channel: str) -> np.ndarray:
        # Convert raw ADC counts to volts using the channel's configured range.
        range_v = next((c.range_v for c in self._config.analog_channels if c.channel == channel), 1.0)
        return adc.astype(np.float64) / _ADC_MAX * range_v

    def _check_status(self, status: int, operation: str):
        if status != _PICO_OK:
            raise DeviceError(f"{operation} failed with PICO_STATUS {status:#x}")

    # -- teardown --------------------------------------------------------------
    def close(self):
        if self._closed:
            return
        stop = self._lib.ps3000aStop
        stop.restype = ctypes.c_int32
        stop(self._handle)
        close = self._lib.ps3000aCloseUnit
        close.restype = ctypes.c_int32
        close(self._handle)
        self._closed = True


class Ps3000aAdapter:
    """Discovery/open factory for the real ps3000a backend."""

    BACKEND_NAME = BACKEND_NAME

    def __init__(self):
        self._lib = None

    def _ensure_library(self):
        if self._lib is None:
            self._lib = _load_library()
        return self._lib

    @staticmethod
    def _resolve_open_status(lib, handle, status) -> int:
        """Handle power-source warning statuses returned by ``ps3000aOpenUnit``.

        ``ps3000aOpenUnit`` may return 282 (PICO_POWER_SUPPLY_NOT_CONNECTED) or
        286 (PICO_USB3_0_DEVICE_NON_USB3_0_PORT) as a *success-with-warning*: the
        unit is open, but must be switched to an appropriate power source before
        use. This resolves that via ``ps3000aChangePowerSource`` and returns
        ``PICO_OK`` on success.
        """
        if status not in (_PICO_POWER_SUPPLY_NOT_CONNECTED, _PICO_USB3_0_DEVICE_NON_USB3_0_PORT):
            return status
        change = lib.ps3000aChangePowerSource
        change.restype = ctypes.c_int32
        change_status = change(ctypes.c_int16(handle.value), ctypes.c_uint32(status))
        if change_status != _PICO_OK:
            return change_status
        return _PICO_OK

    def discover(self) -> list:
        lib = self._ensure_library()  # raises BackendUnavailableError if absent
        # Canonical non-owning enumeration. ``ps3000aOpenUnit`` always opens the
        # FIRST unit, so the previous open/close loop returned the same serial up
        # to 8 times. ``ps3000aEnumerateUnits`` reports each connected unit's
        # serial exactly once WITHOUT opening (or briefly owning) any handle.
        # The driver (2.2.160.7569) rejects a NULL serials pointer with
        # PICO_NULL_POINTER (0x16), so a generously-sized buffer is allocated up
        # front and passed on every call (never NULL).
        enumerate_units = lib.ps3000aEnumerateUnits
        enumerate_units.restype = ctypes.c_int32

        # A serial is ~10-12 chars; 256 bytes covers many units (comma-separated + null).
        buffer_len = 256
        serials = ctypes.create_string_buffer(buffer_len)
        count = ctypes.c_int16(0)
        serial_length = ctypes.c_int16(buffer_len)

        # First pass: report the unit count (buffer stays non-NULL).
        status = enumerate_units(ctypes.byref(count), serials, ctypes.byref(serial_length))
        if status != _PICO_OK:
            raise DeviceError(f"ps3000aEnumerateUnits failed with PICO_STATUS {status:#x}")

        if count.value <= 0:
            return []

        # Second pass (same non-NULL buffer) collects the comma-separated serial list.
        serial_length.value = buffer_len
        status = enumerate_units(ctypes.byref(count), serials, ctypes.byref(serial_length))
        if status != _PICO_OK:
            raise DeviceError(f"ps3000aEnumerateUnits failed with PICO_STATUS {status:#x}")

        serial_list = serials.value.decode("utf-8", errors="replace")
        devices = []
        for serial in (s.strip() for s in serial_list.split(",")):
            if not serial:
                continue
            devices.append({
                "serial": serial,
                "model": "3406D MSO",
                # variant/driver_version require an open handle (ps3000aGetUnitInfo),
                # which is owning; discovery is deliberately non-owning, so these
                # stay empty here and are populated by open()/get_device_info().
                "variant": "",
                "driver_version": "",
            })
        return devices

    def open(self, device_id) -> Ps3000aDevice:
        lib = self._ensure_library()  # raises BackendUnavailableError if absent
        handle = ctypes.c_int16()
        open_unit = lib.ps3000aOpenUnit
        open_unit.restype = ctypes.c_int32
        serial_bytes = None if device_id is None else device_id.encode("utf-8")
        status = open_unit(ctypes.byref(handle), serial_bytes)
        status = self._resolve_open_status(lib, handle, status)
        if status == _PICO_NOT_FOUND:
            raise DeviceNotFoundError(f"ps3000a device {device_id!r} not found")
        if status != _PICO_OK:
            raise DeviceError(f"ps3000aOpenUnit failed with PICO_STATUS {status:#x}")
        return Ps3000aDevice(lib, handle.value, device_id, self)

    def close(self, device: Ps3000aDevice):
        device.close()


default_adapter = Ps3000aAdapter()
