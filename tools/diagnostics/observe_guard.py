"""Siebwalde K6-A OBSERVE guard — executable safety enforcement.

This module is the *enforcement* half of the live command-113 transport. It is
the single authority that decides whether a live OBSERVE request may be
performed. Every restriction below is enforced in code, not in a docstring:

* **Command allowlist** = ``{113}`` only. Any other command id (e.g. the
  flash/reset family 122/123-130, or anything else) is ``BLOCKED``.
* **Endpoint** must be the explicit, literal ``192.168.1.193``. Empty / invalid /
  any other address is ``BLOCKED``. There is **no fallback and no DNS** — the
  guard never resolves a hostname and never substitutes a default address.
* **Single-use authorization**: :meth:`ObserveSession.consume` flips
  ``AUTHORIZED_COMMAND_COUNT_REMAINING`` from ``1`` to ``0`` exactly once; a
  second call is ``BLOCKED``.
* **Bounded timeout** (:func:`bound_timeout`); **no implicit retry**; **no
  recovery**; **no flash/reset state-machine invocation**.

This module never imports ``Comm`` / ``DataAquisition`` / ``flash_one`` /
``bootload``, never opens a socket, and never touches hardware. It is the
read-only enforcement gate that the live transport consults before any I/O.
"""

from __future__ import annotations

from typing import Optional

# --------------------------------------------------------------------------- #
# Enforcement vocabulary
# --------------------------------------------------------------------------- #

BLOCKED = "BLOCKED"
ALLOWED = "ALLOWED"

# The single allowed command id. Mirrors TrackController5/firmware/src/enums.h
# ``GET_CONTROLLER_VERSION = 113``.
ALLOWED_COMMANDS = frozenset({113})

# The explicit controller address (source-verified):
#   SiebwaldeApp/SiebwaldeApp.Core/app.config:79
#   SiebwaldeApp/SiebwaldeApp.Core/Properties/CoreSettings.settings:42
REQUIRED_ENDPOINT = "192.168.1.193"

# Bounded timeout. The live transport may wait at most this long for a single
# reply datagram. Anything larger is clamped; non-positive values are rejected.
MAX_TIMEOUT_SECONDS = 5.0
DEFAULT_TIMEOUT_SECONDS = 5.0


# --------------------------------------------------------------------------- #
# Guard exceptions (each maps to a BLOCKED decision)
# --------------------------------------------------------------------------- #


class ObserveGuardError(Exception):
    """Base class for every guard rejection."""


class CommandBlocked(ObserveGuardError):
    """The requested command id is outside the ``{113}`` allowlist."""


class EndpointBlocked(ObserveGuardError):
    """The endpoint is empty/invalid or not the explicit allowed address."""


class AuthorizationBlocked(ObserveGuardError):
    """The session token is missing, invalid, or already consumed."""


# --------------------------------------------------------------------------- #
# Executable checks
# --------------------------------------------------------------------------- #


def validate_command(command_id: object) -> str:
    """Return ``ALLOWED``/``BLOCKED`` for a command id. Only 113 is allowed.

    Booleans are rejected (``bool`` is an ``int`` subclass but is never a valid
    command id). Non-integer / out-of-range values are ``BLOCKED``.
    """
    if isinstance(command_id, bool):
        return BLOCKED
    # Strict: only a real integer command id is ever comparable. Strings,
    # floats, and other coercible types are ambiguous and therefore BLOCKED.
    if not isinstance(command_id, int):
        return BLOCKED
    return ALLOWED if command_id in ALLOWED_COMMANDS else BLOCKED


def validate_endpoint(endpoint: object) -> str:
    """Return ``ALLOWED``/``BLOCKED``. Requires the explicit literal endpoint.

    No fallback, no DNS: an empty, non-string, or non-matching endpoint is
    ``BLOCKED``. The exact string ``192.168.1.193`` is the only allowed value.
    """
    if not isinstance(endpoint, str):
        return BLOCKED
    value = endpoint.strip()
    if not value:
        return BLOCKED
    return ALLOWED if value == REQUIRED_ENDPOINT else BLOCKED


def bound_timeout(timeout: object) -> float:
    """Return a bounded, strictly-positive timeout in ``(0, MAX_TIMEOUT_SECONDS]``.

    Raises ``ValueError`` for a non-numeric or non-positive value. Values above
    the bound are clamped down; there is no implicit retry and no recovery —
    a bounded wait is the only thing a caller is permitted to do.
    """
    try:
        value = float(timeout)  # type: ignore[arg-type]
    except (TypeError, ValueError):
        raise ValueError("timeout must be a number") from None
    if value <= 0:
        raise ValueError("timeout must be strictly positive")
    return min(value, MAX_TIMEOUT_SECONDS)


# --------------------------------------------------------------------------- #
# Single-use authorization session
# --------------------------------------------------------------------------- #


class ObserveSession:
    """A single-use authorization token for one read-only observe command.

    Construction validates (but does not consume) the command/endpoint. The
    token is consumable exactly once: :meth:`consume` flips
    ``AUTHORIZED_COMMAND_COUNT_REMAINING`` from ``1`` to ``0``; any further call
    raises :class:`AuthorizationBlocked`.
    """

    def __init__(
        self,
        command_id: Optional[object] = None,
        endpoint: Optional[object] = None,
        timeout: object = DEFAULT_TIMEOUT_SECONDS,
    ) -> None:
        self.command_id = command_id
        self.endpoint = endpoint
        self.timeout = bound_timeout(timeout)
        # Ownership is decided at construction time (executable, not advisory).
        self._command_status = validate_command(command_id)
        self._endpoint_status = validate_endpoint(endpoint)
        self.AUTHORIZED_COMMAND_COUNT_REMAINING = 1

    def _ensure_authorizable(self) -> None:
        if self._command_status != ALLOWED:
            raise CommandBlocked(
                "command %r is outside the allowlist %r"
                % (self.command_id, sorted(ALLOWED_COMMANDS))
            )
        if self._endpoint_status != ALLOWED:
            raise EndpointBlocked(
                "endpoint %r is not the explicit allowed endpoint %r"
                % (self.endpoint, REQUIRED_ENDPOINT)
            )
        if self.AUTHORIZED_COMMAND_COUNT_REMAINING <= 0:
            raise AuthorizationBlocked("authorization already consumed")

    def consume(self) -> dict:
        """Consume the single-use authorization exactly once.

        Returns an ``ALLOWED`` result the first time; raises a subclass of
        :class:`ObserveGuardError` (BLOCKED) on any subsequent call or when the
        session never held valid ownership.
        """
        self._ensure_authorizable()
        self.AUTHORIZED_COMMAND_COUNT_REMAINING = 0
        return {
            "status": ALLOWED,
            "command_id": self.command_id,
            "endpoint": self.endpoint,
            "timeout": self.timeout,
            "authorized_command_count_remaining": self.AUTHORIZED_COMMAND_COUNT_REMAINING,
        }
