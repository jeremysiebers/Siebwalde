"""Siebwalde Knowledge Foundation — snapshot/export helper.

Produces a self-contained, integrity-verifiable snapshot of the operational
knowledge database using LadybugDB ``EXPORT DATABASE``. The snapshot directory
contains the raw export (``schema.cypher``, ``index.cypher`` and the
``nodes_*``/``rels_*``/``indices_*``/``indptr_*`` parquet files) plus a
``snapshot-manifest.json`` that records:

* the LadybugDB version and on-disk storage version,
* the source database path,
* the source git revision (``git rev-parse HEAD``),
* the node/relationship table inventory and per-table counts,
* the export file list with SHA-256 digests, and
* a UTC timestamp.

The source database is opened **read-only** and is never modified. The
snapshot is deterministic and idempotent: it refuses to overwrite an existing
snapshot directory unless ``--force`` is given.

Usage::

    python tools/knowledge/snapshot.py [--db DB_PATH] [--out DIR] [--force]

Defaults: the operational database (``tools/knowledge/db/siebwalde-knowledge.lbdb``)
and an output directory ``tools/knowledge/snapshots/<UTC-timestamp>/``.

The counterpart restore tool is ``recover.py``.
"""

from __future__ import annotations

import argparse
import json
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path

_KNOWLEDGE = Path(__file__).resolve().parent
if str(_KNOWLEDGE) not in sys.path:
    sys.path.insert(0, str(_KNOWLEDGE))

try:
    from . import _runtime as rt  # package import context
except ImportError:
    import _runtime as rt  # type: ignore


def snapshot(db_path: Path, out_dir: Path, force: bool = False) -> dict:
    """Export ``db_path`` to ``out_dir`` and write a ``snapshot-manifest.json``.

    Opens the source database read-only (never modifies it), records the table
    inventory + per-table counts before export, runs ``EXPORT DATABASE``, then
    hashes every exported file and writes the manifest. Returns the manifest as
    a dict. Raises ``FileNotFoundError``, ``FileExistsError`` or ``RuntimeError``
    on failure.
    """
    db_path = Path(db_path).resolve()
    out_dir = Path(out_dir).resolve()

    if not db_path.exists():
        raise FileNotFoundError(f"source database does not exist: {db_path}")

    # Deterministic/idempotent: refuse to clobber an existing snapshot.
    if out_dir.exists():
        if not force:
            raise FileExistsError(
                f"snapshot directory already exists: {out_dir} "
                "(pass --force to replace it)"
            )
        if out_dir.is_dir():
            shutil.rmtree(out_dir)
        else:
            out_dir.unlink()

    # EXPORT DATABASE creates the target directory itself (and refuses to write
    # into an existing one), so only ensure the parent directory exists.
    out_dir.parent.mkdir(parents=True, exist_ok=True)

    # Read inventory + counts + storage version on a single read-only connection.
    counts = rt.count_tables(db_path)
    storage = rt.storage_version(db_path)
    node_total = sum(counts["nodes"].values())
    rel_total = sum(counts["rels"].values())

    # Export (read-only connection; EXPORT DATABASE only reads the source).
    db = rt.lb.Database(str(db_path), read_only=True)
    conn = rt.lb.Connection(db)
    try:
        conn.execute(f"EXPORT DATABASE {rt.cypher_literal(str(out_dir))}")
    finally:
        conn.close()
        db.close()

    # Hash every exported file (everything in the fresh dir except the manifest).
    files = []
    for path in sorted(p for p in out_dir.iterdir() if p.is_file()):
        files.append({
            "name": path.name,
            "size": path.stat().st_size,
            "sha256": rt.sha256_file(path),
        })

    manifest = {
        "manifest_version": rt.MANIFEST_VERSION,
        "ladybug_version": rt.ladybug_version(),
        "storage_version": storage,
        "source_db_path": str(db_path),
        "source_revision": rt.git_revision(),
        "timestamp": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "node_tables": sorted(counts["nodes"].keys()),
        "rel_tables": sorted(counts["rels"].keys()),
        "node_counts": counts["nodes"],
        "rel_counts": counts["rels"],
        "totals": {"nodes": node_total, "rels": rel_total},
        "files": files,
    }

    manifest_path = out_dir / rt.MANIFEST_NAME
    manifest_path.write_text(
        json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8"
    )
    return manifest


def main(argv: list[str] | None = None) -> int:
    default_out = (
        _KNOWLEDGE / "snapshots"
        / datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    )
    parser = argparse.ArgumentParser(
        description="Export the knowledge database to a verifiable snapshot directory."
    )
    parser.add_argument("--db", default=str(rt.DB_PATH), help="source database path")
    parser.add_argument("--out", default=str(default_out), help="snapshot output directory")
    parser.add_argument(
        "--force", action="store_true", help="replace an existing snapshot directory"
    )
    args = parser.parse_args(argv)

    try:
        manifest = snapshot(Path(args.db), Path(args.out), force=args.force)
    except (FileExistsError, FileNotFoundError, RuntimeError) as exc:
        print(f"snapshot failed: {exc}", file=sys.stderr)
        return 1

    print(f"Snapshot written to: {args.out}")
    print(f"  nodes: {manifest['totals']['nodes']}  rels: {manifest['totals']['rels']}")
    print(f"  ladybug {manifest['ladybug_version']} (storage {manifest['storage_version']})")
    print(f"  source revision: {manifest['source_revision']}")
    print(f"  files: {len(manifest['files'])}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
