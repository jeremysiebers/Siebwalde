"""Siebwalde K8B session authority — durable, single-use ``LIVE_HARDWARE`` grant.

Encodes the Product Owner / Project Lead scoped grant as a git-ignored,
consumable JSON file under ``Logging/diagnostics-mcp/grants/``. The grant is the
MCP-side *encoding* of the PO's scoped authority; it is NOT a substitute for the
Workflow v1 authority gate and grants no hardware authority by itself.

Model + validation + atomic consume (rename to ``.consumed`` under a lock):

* :func:`load_grant` reads and parses ``<grant_id>.json``.
* :func:`validate_grant` fail-closes on expiry / scope (mode, endpoint,
  commands).
* :func:`consume_grant` atomically renames ``<grant_id>.json`` to
  ``<grant_id>.json.consumed`` exactly once (thread-safe within a process;
  cross-process via the atomicity of ``os.rename``). A consumed grant is never
  accepted again.

Stdlib only (json / datetime / pathlib / threading / os). No socket, no
subprocess, no hardware. The default directory resolves to
``<repo>/Logging/diagnostics-mcp``; tests inject a temporary directory.
"""

from __future__ import annotations

import json
import os
import threading
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Optional

# The default grant root, relative to the repository root. Runtime-only and
# git-ignored: this directory is never part of a commit.
_DEFAULT_ROOT = Path(__file__).resolve().parents[2] / "Logging" / "diagnostics-mcp"

# Suffix appended to a consumed grant's file name.
CONSUMED_SUFFIX = ".consumed"

# The two scoped modes a grant may authorise. ``session`` covers the six-command
# state-changing init; ``read_only`` covers the single command-113 identity probe;
# ``apply_config`` covers the state-changing safe CONFIG/APPLY path (the six init
# commands + HOST_HEARTBEAT 110 + EXEC_MBUS_SLAVE_DATA_EXCH 108).
MODE_SESSION = "session"
MODE_READ_ONLY = "read_only"
MODE_APPLY_CONFIG = "apply_config"

# Any new command that appears on the wire would first have to pass these scoped
# mode names; a grant that does not name a mode can never authorise an action.
KNOWN_MODES = frozenset({MODE_SESSION, MODE_READ_ONLY, MODE_APPLY_CONFIG})


class GrantError(Exception):
    """Base class for every grant rejection (fail-closed)."""


class GrantAbsent(GrantError):
    """No unconsumed grant file exists for the requested grant id."""


class GrantExpired(GrantError):
    """The grant's ``expires_at`` is missing, unparseable, or in the past."""


class GrantConsumed(GrantError):
    """The grant has already been consumed (its ``.consumed`` file exists)."""


class GrantScopeMismatch(GrantError):
    """The grant does not cover the requested mode / endpoint / commands."""


@dataclass(frozen=True)
class Grant:
    """A scoped, single-use ``LIVE_HARDWARE`` grant (immutable snapshot)."""

    grant_id: str
    purpose: str = ""
    target_endpoint: Optional[str] = None
    allowed_commands: frozenset = field(default_factory=frozenset)
    allowed_modes: frozenset = field(default_factory=frozenset)
    issued_by: str = ""
    issued_at: str = ""
    expires_at: str = ""
    max_uses: int = 1
    revision: str = ""

    @classmethod
    def from_dict(cls, data: dict) -> "Grant":
        """Build a Grant from a parsed JSON dict, fail-closed on a missing id."""
        if not isinstance(data, dict):
            raise GrantAbsent("grant is not a JSON object")
        grant_id = data.get("grant_id")
        if not isinstance(grant_id, str) or not grant_id.strip():
            raise GrantAbsent("grant has no grant_id")
        return cls(
            grant_id=grant_id.strip(),
            purpose=str(data.get("purpose") or ""),
            target_endpoint=_optional_str(data.get("target_endpoint")),
            allowed_commands=frozenset(_as_int_list(data.get("allowed_commands"))),
            allowed_modes=frozenset(_as_str_list(data.get("allowed_modes"))),
            issued_by=str(data.get("issued_by") or ""),
            issued_at=str(data.get("issued_at") or ""),
            expires_at=str(data.get("expires_at") or ""),
            max_uses=_as_int(data.get("max_uses"), default=1),
            revision=str(data.get("revision") or ""),
        )

    def to_dict(self) -> dict:
        return {
            "grant_id": self.grant_id,
            "purpose": self.purpose,
            "target_endpoint": self.target_endpoint,
            "allowed_commands": sorted(self.allowed_commands),
            "allowed_modes": sorted(self.allowed_modes),
            "issued_by": self.issued_by,
            "issued_at": self.issued_at,
            "expires_at": self.expires_at,
            "max_uses": self.max_uses,
            "revision": self.revision,
        }


# --------------------------------------------------------------------------- #
# Filesystem layout
# --------------------------------------------------------------------------- #


class GrantDirectory:
    """Filesystem layout for grant artifacts plus the atomic consume lock.

    The lock is a ``threading.Lock`` (in-process). Cross-process safety of the
    consume itself comes from the atomicity of ``os.rename``: exactly one caller
    can move ``<grant_id>.json`` to ``<grant_id>.json.consumed``.
    """

    def __init__(self, root: Optional[object] = None) -> None:
        self.root = Path(root) if root is not None else _DEFAULT_ROOT
        self._lock = threading.Lock()

    @property
    def grants_dir(self) -> Path:
        return self.root / "grants"

    def grant_path(self, grant_id: str) -> Path:
        return self.grants_dir / ("%s.json" % _safe_id(grant_id))

    def consumed_path(self, grant_id: str) -> Path:
        return self.grants_dir / ("%s.json%s" % (_safe_id(grant_id), CONSUMED_SUFFIX))

    def exists(self, grant_id: str) -> bool:
        return self.grant_path(grant_id).exists()

    def is_consumed(self, grant_id: str) -> bool:
        return self.consumed_path(grant_id).exists()

    def write(self, grant: Grant) -> None:
        """Persist a grant (issuance). Used by tests / tooling, never the MCP."""
        self.grants_dir.mkdir(parents=True, exist_ok=True)
        self.grant_path(grant.grant_id).write_text(
            json.dumps(grant.to_dict(), indent=2), encoding="utf-8"
        )

    def load(self, grant_id: str) -> Grant:
        """Load an unconsumed grant, distinguishing consumed vs absent."""
        path = self.grant_path(grant_id)
        if not path.exists():
            if self.consumed_path(grant_id).exists():
                raise GrantConsumed("grant %r is already consumed" % grant_id)
            raise GrantAbsent("grant %r not found" % grant_id)
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            raise GrantAbsent("grant %r is unreadable: %s" % (grant_id, exc)) from None
        return Grant.from_dict(data)

    def consume(self, grant: Grant) -> None:
        """Atomically consume a grant (rename to ``.consumed``) exactly once."""
        src = self.grant_path(grant.grant_id)
        dst = self.consumed_path(grant.grant_id)
        with self._lock:
            if dst.exists():
                raise GrantConsumed("grant %r is already consumed" % grant.grant_id)
            if not src.exists():
                raise GrantAbsent("grant %r not found" % grant.grant_id)
            try:
                os.rename(src, dst)
            except FileNotFoundError:
                # Another process consumed it between the exists() check and the
                # rename (POSIX): fail closed.
                raise GrantConsumed("grant %r is already consumed" % grant.grant_id) from None
            except FileExistsError:
                raise GrantConsumed("grant %r is already consumed" % grant.grant_id) from None


# --------------------------------------------------------------------------- #
# Module-level convenience functions
# --------------------------------------------------------------------------- #

_DEFAULT_DIRECTORY = GrantDirectory()


def load_grant(grant_id: str, directory: Optional[GrantDirectory] = None) -> Grant:
    """Load an unconsumed grant. Raises GrantAbsent / GrantConsumed."""
    return (directory or _DEFAULT_DIRECTORY).load(grant_id)


def validate_grant(
    grant,
    mode: str,
    endpoint: Optional[str] = None,
    allowed_commands: object = (),
):
    """Fail-closed scope validation; returns ``grant`` on success.

    Raises :class:`GrantExpired` or :class:`GrantScopeMismatch`. Checks, in
    order: type, expiry, mode, endpoint, and command coverage.
    """
    if not isinstance(grant, Grant):
        raise GrantScopeMismatch("not a Grant instance")

    # Expiry (fail closed when missing or unparseable).
    now = datetime.now(timezone.utc)
    expires = _parse_timestamp(grant.expires_at)
    if expires is None or expires <= now:
        raise GrantExpired(
            "grant %r is expired (expires_at=%r)" % (grant.grant_id, grant.expires_at)
        )

    # Mode.
    if mode not in grant.allowed_modes:
        raise GrantScopeMismatch(
            "grant %r does not allow mode %r (allowed=%s)"
            % (grant.grant_id, mode, sorted(grant.allowed_modes))
        )

    # Endpoint (only when one is required).
    if endpoint:
        if grant.target_endpoint and grant.target_endpoint != endpoint:
            raise GrantScopeMismatch(
                "grant %r is scoped to endpoint %r, not %r"
                % (grant.grant_id, grant.target_endpoint, endpoint)
            )

    # Command coverage.
    required = set(allowed_commands)
    if required and not required <= set(grant.allowed_commands):
        missing = sorted(required - set(grant.allowed_commands))
        raise GrantScopeMismatch(
            "grant %r does not cover commands %s" % (grant.grant_id, missing)
        )

    return grant


def consume_grant(grant, directory: Optional[GrantDirectory] = None) -> None:
    """Atomically consume a grant exactly once (rename to ``.consumed``)."""
    (directory or _DEFAULT_DIRECTORY).consume(grant)


# --------------------------------------------------------------------------- #
# Small helpers
# --------------------------------------------------------------------------- #


def _parse_timestamp(value: str) -> Optional[datetime]:
    if not isinstance(value, str) or not value.strip():
        return None
    text = value.strip()
    if text.endswith("Z"):
        text = text[:-1] + "+00:00"
    try:
        parsed = datetime.fromisoformat(text)
    except ValueError:
        return None
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed


def _safe_id(grant_id: str) -> str:
    # Guard against path traversal and non-string grant ids.
    if not isinstance(grant_id, str):
        raise GrantAbsent("grant id %r is not a safe file name" % (grant_id,))
    cleaned = "".join(ch for ch in grant_id if ch.isalnum() or ch in "-_.")
    if cleaned != grant_id or not cleaned:
        raise GrantAbsent("grant id %r is not a safe file name" % grant_id)
    return cleaned


def _optional_str(value) -> Optional[str]:
    if value is None:
        return None
    text = str(value).strip()
    return text or None


def _as_int_list(value) -> list:
    if value is None:
        return []
    if not isinstance(value, (list, tuple, set, frozenset)):
        raise GrantScopeMismatch("allowed_commands must be a list of ints")
    out = []
    for item in value:
        if isinstance(item, bool) or not isinstance(item, int):
            raise GrantScopeMismatch("allowed_commands must be a list of ints")
        out.append(item)
    return out


def _as_str_list(value) -> list:
    if value is None:
        return []
    if not isinstance(value, (list, tuple, set, frozenset)):
        raise GrantScopeMismatch("allowed_modes must be a list of strings")
    out = []
    for item in value:
        if not isinstance(item, str):
            raise GrantScopeMismatch("allowed_modes must be a list of strings")
        out.append(item.strip())
    return out


def _as_int(value, default: int = 0) -> int:
    if isinstance(value, bool):
        return default
    if isinstance(value, int):
        return value
    return default
