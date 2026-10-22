"""Category 19: real ps3000a adapter native-library loading (DLL path).

These tests exercise ``_load_library`` directly. They **never open a device** and
**never call ``ps3000aOpenUnit``**: the "present" case only loads the DLL and
checks that the expected symbols exist. The "absent" case confirms the adapter
still loud-fails with ``BackendUnavailableError`` (no simulator fallback).
"""

import ctypes
import os

import pytest

from tools.picoscope.backend.adapters import picosdk
from tools.picoscope.backend.errors import BackendUnavailableError


def test_load_library_raises_when_dll_absent(monkeypatch):
    """The adapter loud-fails (BackendUnavailableError) when the DLL path is
    absent and the DLL is not on the default ctypes search path."""
    monkeypatch.setattr(picosdk, "_PICO7_DLL_DIR", r"C:\definitely\not\present")

    def _loader_fails(_name):
        raise OSError("no such dll (simulated)")

    monkeypatch.setattr(ctypes, "WinDLL", _loader_fails)
    monkeypatch.setattr(ctypes, "CDLL", _loader_fails)

    with pytest.raises(BackendUnavailableError):
        picosdk._load_library()


def test_load_library_loads_when_dll_dir_present():
    """The adapter loads the DLL from the PicoScope 7 folder when present.

    This is a LoadLibrary only: it asserts the returned object exposes the ps3000a
    entry points, and never calls any device-open function.
    """
    dll_dir = picosdk._PICO7_DLL_DIR
    if not os.path.isdir(dll_dir):
        pytest.skip(f"PicoScope 7 folder not present: {dll_dir}")
    lib = picosdk._load_library()
    assert lib is not None
    for symbol in (
        "ps3000aOpenUnit",
        "ps3000aCloseUnit",
        "ps3000aGetUnitInfo",
        "ps3000aSetChannel",
        "ps3000aSetDigitalPort",
        "ps3000aSetSimpleTrigger",
        "ps3000aGetTimebase2",
        "ps3000aSetDataBuffers",
        "ps3000aRunBlock",
        "ps3000aIsReady",
        "ps3000aGetValues",
        "ps3000aStop",
        "ps3000aChangePowerSource",
    ):
        assert hasattr(lib, symbol), f"expected {symbol!r} in the loaded ps3000a library"


def test_load_library_returns_raw_library_not_device():
    """``_load_library`` returns the raw ctypes library, not an opened device."""
    dll_dir = picosdk._PICO7_DLL_DIR
    if not os.path.isdir(dll_dir):
        pytest.skip(f"PicoScope 7 folder not present: {dll_dir}")
    lib = picosdk._load_library()
    # A raw ctypes library object has the DLL _name; it is NOT a Ps3000aDevice
    # (which would have capture_block/configure_* methods and would require an
    # open handle).
    assert isinstance(lib, ctypes.CDLL)
    assert not hasattr(lib, "capture_block")
    assert not hasattr(lib, "configure_analog_channel")


def test_register_dll_directory_guards_missing_dir(monkeypatch):
    """``_register_dll_directory`` is a no-op when the folder is absent."""
    monkeypatch.setattr(picosdk, "_PICO7_DLL_DIR", r"C:\definitely\not\present")
    before = len(picosdk._dll_directory_handles)
    picosdk._register_dll_directory()
    assert len(picosdk._dll_directory_handles) == before
