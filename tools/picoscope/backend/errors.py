"""Exception taxonomy for the picoscope measurement backend.

All backend errors derive from :class:`PicoScopeError` so callers can catch the
whole family with a single ``except``. ``DeviceBusyError`` is re-exported by
``ownership.py`` for convenience.
"""


class PicoScopeError(Exception):
    """Base class for all picoscope backend errors."""


class UnknownBackendError(PicoScopeError):
    """The requested backend name is not recognized."""


class BackendUnavailableError(PicoScopeError):
    """A real backend (e.g. ``ps3000a``) was requested but its native
    library/driver is not available.

    The real backend never silently falls back to the simulator: a real-device
    request that cannot be satisfied raises this error.
    """


class DeviceBusyError(PicoScopeError):
    """The requested device is already owned by another in-process handle."""


class DeviceNotFoundError(PicoScopeError):
    """The requested device id was not found by the backend."""


class DeviceError(PicoScopeError):
    """The device reported an error during an operation."""


class DeviceTimeoutError(DeviceError):
    """A bounded wait (e.g. capture readiness) timed out."""


class InvalidConfigError(PicoScopeError, ValueError):
    """A configuration value was rejected as invalid."""
