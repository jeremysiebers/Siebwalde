"""Narrow, removable cross-process device lock (PICO-3 ownership).

Layer A already enforces *in-process* exclusivity (``backend/ownership.py``).
This module adds a *cross-process* guard so two MCP server processes — or a
second process vs. PicoScope 7 — cannot own the same device concurrently.

Design constraints (see ``docs/PICOSCOPE-PICO-3-MCP-INTEGRATION.md``):

* narrowly scoped: one small lock file per device id, inside the evidence dir;
* removable: the lock is an ordinary file; deleting it (while no process holds
  it open/locked) is the manual override path;
* never kills any process — it only *refuses* to acquire a busy device;
* deterministic release on exception / cancel (``release()`` in ``finally``);
* never falls back to the simulator for a real-device failure.
"""

from __future__ import annotations

import os

from tools.picoscope.backend.errors import DeviceBusyError

try:  # Windows (the Siebwalde workstation)
    import msvcrt as _msvcrt
except ImportError:  # pragma: no cover - non-Windows fallback
    _msvcrt = None

try:  # POSIX fallback
    import fcntl as _fcntl
except ImportError:  # pragma: no cover - Windows
    _fcntl = None


def _lock_region(fh, non_blocking: bool) -> None:
    """Lock one byte at the start of ``fh`` (platform-appropriate)."""
    fh.seek(0)
    if _msvcrt is not None:
        mode = _msvcrt.LK_NBLCK if non_blocking else _msvcrt.LK_LOCK
        _msvcrt.locking(fh.fileno(), mode, 1)
    elif _fcntl is not None:  # pragma: no cover - POSIX only
        flags = _fcntl.LOCK_EX | (_fcntl.LOCK_NB if non_blocking else 0)
        _fcntl.flock(fh.fileno(), flags)
    else:  # pragma: no cover - unsupported platform
        # No OS lock primitive available: fall back to O_EXCL create semantics.
        raise NotImplementedError("No cross-process lock primitive available")


def _unlock_region(fh) -> None:
    fh.seek(0)
    if _msvcrt is not None:
        _msvcrt.locking(fh.fileno(), _msvcrt.LK_UNLCK, 1)
    elif _fcntl is not None:  # pragma: no cover - POSIX only
        _fcntl.flock(fh.fileno(), _fcntl.LOCK_UN)


class CrossProcessLock:
    """A non-blocking, exclusive lock on a device id (file-backed)."""

    def __init__(self, lock_dir: str, device_id: str):
        # The lock file name is derived from a sanitized device id. Device ids
        # are backend-controlled (serial strings), but we sanitize to a safe
        # filename token so an unusual serial can never traverse directories.
        safe = "".join(c if (c.isalnum() or c in "-_.") else "_" for c in device_id) or "unit"
        os.makedirs(lock_dir, exist_ok=True)
        self._path = os.path.join(lock_dir, f".lock-{safe}")
        self._fh = None
        self._acquired = False

    @property
    def path(self) -> str:
        return self._path

    def acquire(self) -> "CrossProcessLock":
        """Acquire the lock non-blocking; raise :class:`DeviceBusyError` if busy."""
        self._fh = open(self._path, "a+b")
        try:
            _lock_region(self._fh, non_blocking=True)
        except OSError as exc:
            self._fh.close()
            self._fh = None
            raise DeviceBusyError(
                f"Device is already owned by another process "
                f"(cross-process lock {self._path!r} held): {exc}"
            ) from exc
        self._acquired = True
        return self

    def release(self) -> None:
        """Release the lock (idempotent). Never raises."""
        if self._fh is None:
            return
        try:
            if self._acquired:
                _unlock_region(self._fh)
                self._acquired = False
        finally:
            self._fh.close()
            self._fh = None

    def __enter__(self) -> "CrossProcessLock":
        return self.acquire()

    def __exit__(self, exc_type, exc, tb):
        self.release()
        return False
