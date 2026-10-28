"""Shared runtime helpers for the Knowledge Foundation snapshot/recovery tools.

Keeps the native-library bootstrap (``LBUG_C_API_LIB_PATH`` + ``PATH``) and the
small set of read helpers (table counting, provenance of the build, file
hashing) in one place so ``snapshot.py`` and ``recover.py`` stay deterministic
and cannot drift apart.

This module is intentionally importable two ways, mirroring ``build_graph.py``:

* as a package member (``from tools.knowledge import _runtime``) when the tests
  or the MCP import the knowledge tree as a package, and
* as a standalone top-level module (``import _runtime``) when ``snapshot.py`` /
  ``recover.py`` are executed directly with ``tools/knowledge`` on ``sys.path``.
"""

from __future__ import annotations

import hashlib
import os
import subprocess
from pathlib import Path

_KNOWLEDGE = Path(__file__).resolve().parent          # tools/knowledge
_REPO = _KNOWLEDGE.parent.parent                        # repository root
_LIB = _KNOWLEDGE / "lib"

# Ensure the native lbug library + OpenSSL runtime are discoverable.
if (_LIB / "lbug_shared.dll").exists():
    os.environ.setdefault("LBUG_C_API_LIB_PATH", str(_LIB / "lbug_shared.dll"))
if str(_LIB) not in os.environ.get("PATH", ""):
    os.environ["PATH"] = str(_LIB) + os.pathsep + os.environ.get("PATH", "")

import ladybug as lb  # noqa: E402

try:
    from . import schema  # package import context
    from . import knowledge_seed  # package import context
except ImportError:  # standalone script context (tools/knowledge on sys.path)
    import schema  # type: ignore
    import knowledge_seed  # type: ignore

DB_DIR = _KNOWLEDGE / "db"
DB_PATH = DB_DIR / "siebwalde-knowledge.lbdb"

MANIFEST_NAME = "snapshot-manifest.json"
MANIFEST_VERSION = 1


def cypher_literal(value: str) -> str:
    """Render a string (typically a filesystem path) as a Cypher string literal."""
    escaped = value.replace("\\", "\\\\").replace("'", "\\'")
    return f"'{escaped}'"


def ladybug_version() -> str:
    return lb.__version__


def git_revision() -> str:
    """Return the full git HEAD revision of the repository (or "unknown")."""
    try:
        proc = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            cwd=str(_REPO),
            capture_output=True,
            text=True,
            check=True,
        )
        return proc.stdout.strip()
    except Exception:
        return "unknown"


def sha256_file(path: Path) -> str:
    """Return the lowercase hex SHA-256 digest of a file."""
    h = hashlib.sha256()
    with path.open("rb") as fh:
        for chunk in iter(lambda: fh.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def count_tables(db_path: Path) -> dict:
    """Open a database read-only and return per-table node/relationship counts.

    Returns ``{"nodes": {table: int}, "rels": {table: int}}``. The database is
    opened read-only, so it is never modified.
    """
    db = lb.Database(str(db_path), read_only=True)
    conn = lb.Connection(db)
    try:
        nodes = {}
        for node_type in schema.NODE_TABLES:
            rows = conn.execute(f"MATCH (n:{node_type}) RETURN count(n) AS c").get_all()
            nodes[node_type] = rows[0][0]
        rels = {}
        for rel_name in schema.REL_TABLES:
            rows = conn.execute(f"MATCH ()-[r:{rel_name}]->() RETURN count(r) AS c").get_all()
            rels[rel_name] = rows[0][0]
        return {"nodes": nodes, "rels": rels}
    finally:
        conn.close()
        db.close()


def storage_version(db_path: Path) -> int:
    """Return the LadybugDB on-disk storage version of a database (read-only)."""
    db = lb.Database(str(db_path), read_only=True)
    try:
        return db.get_storage_version()
    finally:
        db.close()


def seed_counts() -> dict:
    """Return the per-table node/relationship counts implied by the seed.

    This is the authoritative "complete publishable knowledge" reference: the
    seed (``knowledge_seed.py``) holds 216 nodes / 161 relationships, which is
    the Git source of truth for what a rebuilt database must contain.
    """
    node_counts: dict[str, int] = {t: 0 for t in schema.NODE_TABLES}
    rel_counts: dict[str, int] = {t: 0 for t in schema.REL_TABLES}
    for node_type, _props in knowledge_seed.NODES:
        node_counts[node_type] += 1
    for rel_name, _f, _t, _p in knowledge_seed.RELS:
        rel_counts[rel_name] += 1
    return {
        "nodes": node_counts,
        "rels": rel_counts,
        "node_total": len(knowledge_seed.NODES),
        "rel_total": len(knowledge_seed.RELS),
    }
