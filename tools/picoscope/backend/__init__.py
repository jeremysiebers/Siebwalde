"""PicoScope measurement backend (Layer A).

Public surface: :mod:`tools.picoscope.backend.api`. Supporting modules:
:mod:`config`, :mod:`model`, :mod:`serialize`, :mod:`ownership`,
:mod:`errors`, and the :mod:`adapters` package (``ps3000a`` + ``simulator``).
"""

from .api import (  # noqa: F401
    PicoScope,
    discover_devices,
    load_capture,
    open_device,
    save_capture,
)
from .model import Capture, DeviceIdentity  # noqa: F401
from .config import AcquisitionConfig  # noqa: F401

__all__ = [
    "PicoScope",
    "discover_devices",
    "open_device",
    "save_capture",
    "load_capture",
    "Capture",
    "DeviceIdentity",
    "AcquisitionConfig",
]
