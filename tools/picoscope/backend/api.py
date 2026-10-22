"""Public Layer A API (no MCP dependency).

Entry points:

* :func:`discover_devices` — list available devices for a backend.
* :func:`open_device` — open an exclusively-owned :class:`PicoScope` handle.
* :class:`PicoScope` — the measurement handle (configure + capture + save/load).
* :func:`save_capture` / :func:`load_capture` — module-level serialize helpers.

Backends: ``"simulator"`` (default) and ``"ps3000a"`` (aliases ``"picosdk"`` /
``"real"``). A real-device request that fails raises; it never falls back to the
simulator.
"""

from __future__ import annotations

from typing import Optional

from .errors import UnknownBackendError
from .model import Capture, DeviceIdentity
from .ownership import acquire, release
from .serialize import load_capture, save_capture  # noqa: F401  (re-exported helpers)
from .adapters import picosdk as _picosdk
from .adapters import simulator as _simulator

__all__ = [
    "discover_devices",
    "open_device",
    "PicoScope",
    "save_capture",
    "load_capture",
]

_BACKEND_ALIASES = {
    "simulator": "simulator",
    "ps3000a": "ps3000a",
    "picosdk": "ps3000a",
    "real": "ps3000a",
}


def _adapter_for(backend: str):
    canonical = _BACKEND_ALIASES.get(backend)
    if canonical is None:
        raise UnknownBackendError(
            f"Unknown backend {backend!r}; expected one of {sorted(set(_BACKEND_ALIASES))}"
        )
    if canonical == "simulator":
        return _simulator.default_adapter
    return _picosdk.default_adapter


def discover_devices(backend: str = "simulator") -> list:
    """List available devices for ``backend`` as a list of dicts.

    Each dict has ``serial``, ``model``, ``variant`` and ``driver_version`` keys.
    For the real ``ps3000a`` backend a missing driver raises
    :class:`BackendUnavailableError` (never a silent simulator fallback).
    """
    adapter = _adapter_for(backend)
    return adapter.discover()


def open_device(device_id, backend: str = "simulator") -> "PicoScope":
    """Open ``device_id`` with exclusive in-process ownership.

    Raises :class:`DeviceBusyError` if the device is already owned, and the
    relevant backend error if opening fails. Ownership is released deterministically
    on :meth:`PicoScope.close_device` and on exception.
    """
    adapter = _adapter_for(backend)
    token = acquire(device_id)
    try:
        raw = adapter.open(device_id)
    except Exception:
        release(token)
        raise
    return PicoScope(adapter=adapter, device_id=device_id, token=token, raw=raw)


class PicoScope:
    """A measurement-only handle to one opened PicoScope.

    Configure analog channels, digital ports and a trigger, then
    :meth:`capture_block` to obtain a :class:`Capture`. Use it as a context
    manager to guarantee deterministic close (and ownership release).
    """

    def __init__(self, adapter, device_id, token, raw):
        self._adapter = adapter
        self._device_id = device_id
        self._token = token
        self._raw = raw
        self._closed = False
        self._last_capture: Optional[Capture] = None

    # -- identity -----------------------------------------------------------
    def get_device_info(self) -> DeviceIdentity:
        self._check_open()
        return self._raw.get_device_info()

    # -- configuration ------------------------------------------------------
    def configure_analog_channel(self, channel, enabled, range_v, coupling):
        """Enable/disable an analog channel (``A``..``D``) with range (V) and coupling."""
        self._check_open()
        self._raw.configure_analog_channel(channel, enabled, range_v, coupling)
        return self

    def configure_digital_port(self, port, enabled, threshold_mv):
        """Enable/disable a digital port (``0``=D0..D7, ``1``=D8..D15) with a logic threshold (mV)."""
        self._check_open()
        self._raw.configure_digital_port(port, enabled, threshold_mv)
        return self

    def configure_trigger(self, source, threshold, edge, pre_samples, post_samples):
        """Set the trigger condition and the pre/post trigger sample split."""
        self._check_open()
        self._raw.configure_trigger(source, threshold, edge, pre_samples, post_samples)
        return self

    # -- acquisition ----------------------------------------------------------
    def capture_block(
        self,
        pre_samples: Optional[int] = None,
        post_samples: Optional[int] = None,
        timebase: Optional[int] = None,
    ) -> Capture:
        """Run one block capture and return a :class:`Capture`."""
        self._check_open()
        cap = self._raw.capture_block(
            pre_samples=pre_samples,
            post_samples=post_samples,
            timebase=timebase,
        )
        self._last_capture = cap
        return cap

    # -- persistence -----------------------------------------------------------
    def save_capture(self, path) -> str:
        """Save the most recent capture to ``<path>.npz`` + ``<path>.json``."""
        if self._last_capture is None:
            raise ValueError("No capture to save; call capture_block() first")
        return save_capture(self._last_capture, path)

    def load_capture(self, path) -> Capture:
        """Load a capture from disk and remember it as the current capture."""
        cap = load_capture(path)
        self._last_capture = cap
        return cap

    # -- lifecycle -----------------------------------------------------------
    def close_device(self):
        """Close the device and release ownership (idempotent)."""
        if self._closed:
            return
        try:
            self._adapter.close(self._raw)
        finally:
            release(self._token)
            self._closed = True

    @property
    def closed(self) -> bool:
        return self._closed

    @property
    def device(self):
        """The backend-specific raw device object (advanced/testing access)."""
        return self._raw

    @property
    def config(self):
        """The current (pending) acquisition configuration on the device."""
        return self._raw.config

    @property
    def last_capture(self) -> Optional[Capture]:
        return self._last_capture

    def _check_open(self):
        if self._closed:
            raise ValueError("Device is closed")

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc, tb):
        self.close_device()
        return False

    def __del__(self):
        # Best-effort release; close_device() is the deterministic path.
        try:
            if not self._closed:
                self.close_device()
        except Exception:
            pass
