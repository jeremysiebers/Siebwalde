"""Narrowly-scoped, fully-removable in-process device ownership guard.

This is deliberately *not* a system-wide process killer and does *not* use pid
files or OS-level locks. It keeps a single process-local registry mapping a
device id to an opaque token:

* opening an already-owned device raises :class:`DeviceBusyError`;
* release is deterministic and token-scoped (a handle releases exactly the
  token it acquired);
* the registry can be emptied wholesale with :func:`release_all` (a teardown/
  test helper, not a normal operating path).

The guard protects against two handles in the *same process* fighting over one
device. Cross-process collision handling (e.g. vs PicoScope 7) is a PICO-2
concern and is documented in ``PICO-2-PREFLIGHT.md``.
"""

from __future__ import annotations

import uuid
from dataclasses import dataclass

from .errors import DeviceBusyError

#: Module-level (process-local) registry: device_id -> OwnershipToken.
_registry: dict = {}


@dataclass(frozen=True)
class OwnershipToken:
    """Opaque, unforgeable proof that a device was acquired."""

    device_id: str
    token_id: str


def acquire(device_id: str) -> OwnershipToken:
    """Acquire exclusive ownership of ``device_id``.

    Raises :class:`DeviceBusyError` if the device is already owned.
    """
    if device_id in _registry:
        raise DeviceBusyError(
            f"Device {device_id!r} is already owned by another in-process handle"
        )
    token = OwnershipToken(device_id=device_id, token_id=uuid.uuid4().hex)
    _registry[device_id] = token
    return token


def release(token: OwnershipToken) -> bool:
    """Release ``token`` if (and only if) it is still the registered owner.

    Returns ``True`` if a release actually happened, ``False`` if the token was
    already released or superseded. Idempotent.
    """
    current = _registry.get(token.device_id)
    if current is token:
        del _registry[token.device_id]
        return True
    return False


def is_owned(device_id: str) -> bool:
    return device_id in _registry


def owned_device_ids() -> frozenset:
    return frozenset(_registry.keys())


def release_all() -> int:
    """Release every held token (teardown/test helper). Returns count released."""
    count = len(_registry)
    _registry.clear()
    return count
