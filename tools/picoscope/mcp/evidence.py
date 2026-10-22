"""Dedicated, server-owned evidence storage with strict path safety.

The MCP layer never writes to a client-supplied path. Capture IDs are generated
server-side (a UUID hex string); every artifact path is derived from that ID and
resolved under a single dedicated evidence directory. Defense-in-depth checks
guard against directory traversal and symlink escape even though clients cannot
supply arbitrary paths.
"""

from __future__ import annotations

import os
import re
import uuid

#: Capture IDs are server-generated UUID hex strings (32 lowercase hex chars).
#: Any other string is rejected before it is used to build a filesystem path.
_CAPTURE_ID_RE = re.compile(r"^[0-9a-f]{32}$")


class EvidenceError(ValueError):
    """A capture id or artifact path was rejected by the evidence store."""


class EvidenceStore:
    """Owns one dedicated evidence directory and validates all capture ids."""

    def __init__(self, root_dir: str):
        # Resolve once, so containment checks compare two fully-resolved paths.
        self.root = os.path.realpath(os.path.abspath(root_dir))
        os.makedirs(self.root, exist_ok=True)

    # -- ids ----------------------------------------------------------------
    @staticmethod
    def new_capture_id() -> str:
        """Generate a fresh, unguessable capture id (server-side only)."""
        return uuid.uuid4().hex

    @staticmethod
    def validate_capture_id(capture_id: str) -> str:
        """Validate and return the capture id, or raise :class:`EvidenceError`.

        The id must be exactly a 32-char lowercase hex string. This is the
        single choke point that guarantees no directory traversal is possible
        (``/``, ``\\``, ``..`` and friends all fail the regex).
        """
        if not isinstance(capture_id, str) or not _CAPTURE_ID_RE.match(capture_id):
            raise EvidenceError(
                f"Invalid capture_id {capture_id!r}; expected a 32-char hex uuid"
            )
        return capture_id

    # -- paths --------------------------------------------------------------
    def base_path(self, capture_id: str) -> str:
        """Return the extension-less artifact base path for a capture id."""
        self.validate_capture_id(capture_id)
        candidate = os.path.join(self.root, capture_id)
        resolved = os.path.realpath(os.path.abspath(candidate))
        self._assert_contained(resolved)
        return resolved

    def artifact_paths(self, capture_id: str) -> dict:
        """Return the ``{"npz": ..., "json": ...}`` artifact paths for an id."""
        base = self.base_path(capture_id)
        return {"npz": base + ".npz", "json": base + ".json"}

    def _assert_contained(self, resolved: str) -> None:
        # Defense-in-depth: the resolved artifact path must live under the
        # dedicated evidence dir. Guards against a symlinked evidence dir or a
        # (hypothetical) future id validation regression.
        try:
            common = os.path.commonpath([self.root, resolved])
        except ValueError:  # different drives on Windows
            raise EvidenceError("Artifact path escapes the evidence directory")
        if common != self.root:
            raise EvidenceError("Artifact path escapes the evidence directory")
