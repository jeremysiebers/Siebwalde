"""Capture records: unique IDs, timestamps, manifests, atomic writes, hashes.

* Host UTC and Europe/Amsterdam local time are recorded separately.
* The scope clock (if any) is recorded as a DIFFERENT, unverified time source.
  The HMO2024 has no verified SCPI clock query, so ``scope_time`` defaults to
  ``null`` with an explicit ``NOT_QUERIED`` status rather than a guessed value.
* Every artifact gets a SHA-256. Manifests are written atomically (temp file +
  ``os.replace``) so a crash never leaves a half-written manifest.
* Evidence directories are never overwritten: ``new_capture_dir`` always
  creates a fresh, unique directory and refuses to reuse an existing path.
"""

from __future__ import annotations

import hashlib
import json
import os
import time
import uuid
from datetime import datetime, timezone
from typing import Dict, Optional

try:
    from zoneinfo import ZoneInfo

    _AMSTERDAM = ZoneInfo("Europe/Amsterdam")
except Exception:  # pragma: no cover - very old Python / missing tzdata
    _AMSTERDAM = timezone.utc

MANIFEST_NAME = "manifest.json"
SCHEMA = "hmo2024.record/1"


class RecordError(Exception):
    pass


def _now() -> datetime:
    return datetime.now(timezone.utc)


def timestamp_fields(dt: Optional[datetime] = None) -> Dict[str, str]:
    """Return ``utc`` and ``amsterdam`` ISO-8601 strings (separate time sources)."""
    dt = dt or _now()
    return {
        "utc": dt.isoformat(),
        "amsterdam": dt.astimezone(_AMSTERDAM).isoformat(),
    }


def capture_id(dt: Optional[datetime] = None) -> str:
    dt = dt or _now()
    stamp = dt.strftime("%Y%m%dT%H%M%S")
    return f"hmo2024-{stamp}Z-{uuid.uuid4().hex[:8]}"


def sha256_of_file(path: str) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def sha256_of_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def atomic_write_bytes(path: str, data: bytes) -> None:
    tmp = path + ".tmp"
    with open(tmp, "wb") as f:
        f.write(data)
    os.replace(tmp, path)


def atomic_write_text(path: str, text: str) -> None:
    atomic_write_bytes(path, text.encode("utf-8"))


def atomic_write_json(path: str, obj) -> None:
    atomic_write_text(path, json.dumps(obj, indent=2, sort_keys=True) + "\n")


def read_json(path: str):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


class RecordManager:
    """Owns a single capture directory and its manifest."""

    def __init__(self, root: str):
        self.root = os.path.abspath(root)

    # -- directory --------------------------------------------------------
    def new_capture_dir(self, cap_id: Optional[str] = None) -> str:
        """Create a fresh capture directory; refuse to reuse an existing path."""
        cap_id = cap_id or capture_id()
        d = os.path.join(self.root, cap_id)
        os.makedirs(d, exist_ok=False)  # raises FileExistsError -> never overwrite
        return d

    # -- manifest ---------------------------------------------------------
    @staticmethod
    def base_manifest(
        cap_id: str,
        port: str,
        baud: int,
        tool: Dict[str, str],
    ) -> Dict:
        return {
            "schema": SCHEMA,
            "capture_id": cap_id,
            "tool": tool,
            "host_time": timestamp_fields(),
            "scope_time": {
                "value": None,
                "status": "NOT_QUERIED",
                "note": "no verified SCPI clock query on HMO2024; not queried",
            },
            "instrument": {"port": port, "baud": baud, "id": None},
            "scpi_commands": [],
            "acquisition": None,
            "hold": None,
            "artifacts": [],
            "flags": [],
        }

    def write_manifest(self, cap_dir: str, manifest: Dict) -> str:
        path = os.path.join(cap_dir, MANIFEST_NAME)
        atomic_write_json(path, manifest)
        return path

    def load_manifest(self, cap_dir: str) -> Dict:
        path = os.path.join(cap_dir, MANIFEST_NAME)
        if not os.path.isfile(path):
            raise RecordError(f"no {MANIFEST_NAME} in {cap_dir}")
        return read_json(path)

    # -- artifacts --------------------------------------------------------
    @staticmethod
    def record_artifact(cap_dir: str, rel_path: str, data: bytes, kind: str) -> Dict:
        """Write an artifact (raw bytes preserved exactly) and return its entry."""
        abs_path = os.path.join(cap_dir, rel_path)
        os.makedirs(os.path.dirname(abs_path), exist_ok=True)
        atomic_write_bytes(abs_path, data)
        return {
            "rel_path": rel_path.replace(os.sep, "/"),
            "kind": kind,
            "bytes": len(data),
            "sha256": sha256_of_bytes(data),
        }
