"""Siebwalde Knowledge Foundation — recovery/import helper.

Restores a knowledge database from a snapshot directory produced by
``snapshot.py`` using LadybugDB ``IMPORT DATABASE``, then verifies the result.

Recovery is **fail-closed**: every failure (missing manifest, schema mismatch,
missing or corrupt export file, existing target, import error, or a
count/representative-query mismatch after import) raises and returns a non-zero
exit status. The tool never overwrites an existing database.

Verification after import:

1. per-table node/relationship counts are compared against the manifest's
   recorded counts (faithful restore of what was snapshotted);
2. the totals are also compared against the seed (``knowledge_seed.py``), the
   complete publishable knowledge, so a snapshot that is missing nodes is
   reported rather than silently restored; and
3. a few representative nodes (including the K6/K7/K8 findings that the stale
   operational database was missing) are queried with their provenance, proving
   the data — not just the counts — round-tripped.

Usage::

    python tools/knowledge/recover.py --snapshot DIR --out DB_PATH

The target ``DB_PATH`` must not already exist. The counterpart snapshot tool is
``snapshot.py``.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

_KNOWLEDGE = Path(__file__).resolve().parent
if str(_KNOWLEDGE) not in sys.path:
    sys.path.insert(0, str(_KNOWLEDGE))

try:
    from . import _runtime as rt  # package import context
except ImportError:
    import _runtime as rt  # type: ignore

# Representative integrity checks: (node_table, node_id, property, expected).
# These are the published facts the stale operational DB was missing (K6/K7/K8)
# plus two foundational nodes, each with a provenance field asserted separately.
REPRESENTATIVE_CHECKS = [
    ("ProtocolCommand", "command:113", "command_name", "GET_CONTROLLER_VERSION"),
    ("Observation", "obs:k6b-cmd113-readback", "status", "PROVEN_HARDWARE"),
    ("Observation", "obs:k7-pic32-identity-transition", "status", "PROVEN_HARDWARE"),
    ("Observation", "obs:k7-cmd124-ineligible", "status", "PROVEN_SOURCE"),
    ("Observation", "obs:k7-next-readonly-hr11-slaveryinfo", "status", "PROVEN_SOURCE"),
    ("Observation", "obs:k8-slaveryinfo-passive-blocked", "status", "PROVEN_SOURCE"),
    ("HardwareDevice", "device:pic32", "kind", "PIC32"),
    ("FirmwareArtifact", "firmware:pic32-0.1.0", "version", "0.1.0"),
]

# A couple of provenance spot-checks (node_table, node_id, property, expected).
PROVENANCE_CHECKS = [
    ("ProtocolCommand", "command:113", "source_path", "TrackController5/firmware/src/controller.c"),
    ("Observation", "obs:k6b-cmd113-readback", "source_path", "tools/diagnostics/k6b_observe.py"),
]


def _load_manifest(snapshot_dir: Path) -> dict:
    manifest_path = snapshot_dir / rt.MANIFEST_NAME
    if not manifest_path.is_file():
        raise FileNotFoundError(
            f"snapshot manifest not found: {manifest_path} "
            f"(is this a snapshot produced by snapshot.py?)"
        )
    try:
        return json.loads(manifest_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise ValueError(f"snapshot manifest is not valid JSON: {exc}") from exc


def _check_schema_match(manifest: dict) -> None:
    """Fail closed if the snapshot's table inventory differs from schema.py."""
    manifest_nodes = set(manifest.get("node_tables", []))
    manifest_rels = set(manifest.get("rel_tables", []))
    schema_nodes = set(rt.schema.NODE_TABLES)
    schema_rels = set(rt.schema.REL_TABLES)
    if manifest_nodes != schema_nodes:
        missing = schema_nodes - manifest_nodes
        extra = manifest_nodes - schema_nodes
        raise ValueError(
            f"schema mismatch (node tables): missing={sorted(missing)} extra={sorted(extra)}"
        )
    if manifest_rels != schema_rels:
        missing = schema_rels - manifest_rels
        extra = manifest_rels - schema_rels
        raise ValueError(
            f"schema mismatch (rel tables): missing={sorted(missing)} extra={sorted(extra)}"
        )


def _check_files(snapshot_dir: Path, manifest: dict) -> None:
    """Fail closed if any recorded export file is missing or corrupt."""
    problems = []
    for entry in manifest.get("files", []):
        name = entry.get("name")
        expected_sha = entry.get("sha256")
        path = snapshot_dir / name
        if not path.is_file():
            problems.append(f"missing export file: {name}")
            continue
        actual_sha = rt.sha256_file(path)
        if actual_sha != expected_sha:
            problems.append(f"corrupt export file: {name} (SHA-256 mismatch)")
    if not manifest.get("files"):
        problems.append("manifest lists no export files")
    if problems:
        raise ValueError("snapshot integrity check failed:\n  " + "\n  ".join(problems))


def _verify_import(db_path: Path, manifest: dict) -> list[str]:
    """Return a list of problems after import; empty means verification passed."""
    problems: list[str] = []
    counts = rt.count_tables(db_path)

    # 1. faithful restore: per-table counts must match the manifest.
    for node_type, expected in manifest.get("node_counts", {}).items():
        if counts["nodes"].get(node_type) != expected:
            problems.append(
                f"node table {node_type}: imported {counts['nodes'].get(node_type)}, "
                f"manifest {expected}"
            )
    for rel_name, expected in manifest.get("rel_counts", {}).items():
        if counts["rels"].get(rel_name) != expected:
            problems.append(
                f"rel table {rel_name}: imported {counts['rels'].get(rel_name)}, "
                f"manifest {expected}"
            )

    # 2. completeness: totals must equal the seed (complete publishable knowledge).
    seed = rt.seed_counts()
    imported_nodes = sum(counts["nodes"].values())
    imported_rels = sum(counts["rels"].values())
    if imported_nodes != seed["node_total"]:
        problems.append(
            f"node total {imported_nodes} != seed total {seed['node_total']}"
        )
    if imported_rels != seed["rel_total"]:
        problems.append(
            f"rel total {imported_rels} != seed total {seed['rel_total']}"
        )

    # 3. representative queries (data, not just counts).
    db = rt.lb.Database(str(db_path), read_only=True)
    conn = rt.lb.Connection(db)
    try:
        for node_type, node_id, prop, expected in REPRESENTATIVE_CHECKS:
            rows = conn.execute(
                f"MATCH (n:{node_type}) WHERE n.id = {rt.cypher_literal(node_id)} "
                f"RETURN n.{prop}"
            ).get_all()
            if not rows:
                problems.append(f"representative node missing: {node_type}:{node_id}")
                continue
            if rows[0][0] != expected:
                problems.append(
                    f"{node_type}:{node_id}.{prop} = {rows[0][0]!r} != {expected!r}"
                )
        for node_type, node_id, prop, expected in PROVENANCE_CHECKS:
            rows = conn.execute(
                f"MATCH (n:{node_type}) WHERE n.id = {rt.cypher_literal(node_id)} "
                f"RETURN n.{prop}"
            ).get_all()
            if not rows:
                problems.append(f"provenance node missing: {node_type}:{node_id}")
                continue
            if rows[0][0] != expected:
                problems.append(
                    f"{node_type}:{node_id}.{prop} = {rows[0][0]!r} != {expected!r}"
                )
    finally:
        conn.close()
        db.close()

    return problems


def recover(snapshot_dir: Path, db_path: Path) -> dict:
    """Import a snapshot into a fresh database and verify it.

    Raises on any failure (fail closed). Returns a report dict on success.
    """
    snapshot_dir = Path(snapshot_dir).resolve()
    db_path = Path(db_path).resolve()

    if not snapshot_dir.is_dir():
        raise FileNotFoundError(f"snapshot directory does not exist: {snapshot_dir}")
    if db_path.exists():
        raise FileExistsError(
            f"target database already exists: {db_path} "
            "(recovery refuses to overwrite an existing database)"
        )

    manifest = _load_manifest(snapshot_dir)
    _check_schema_match(manifest)
    _check_files(snapshot_dir, manifest)

    # IMPORT DATABASE (counterpart to snapshot's EXPORT DATABASE).
    db = rt.lb.Database(str(db_path))
    conn = rt.lb.Connection(db)
    try:
        conn.execute(f"IMPORT DATABASE {rt.cypher_literal(str(snapshot_dir))}")
    finally:
        conn.close()
        db.close()

    problems = _verify_import(db_path, manifest)
    if problems:
        raise RuntimeError(
            "recovery verification failed:\n  " + "\n  ".join(problems)
        )

    counts = rt.count_tables(db_path)
    return {
        "snapshot_dir": str(snapshot_dir),
        "target_db": str(db_path),
        "nodes": sum(counts["nodes"].values()),
        "rels": sum(counts["rels"].values()),
        "source_revision": manifest.get("source_revision"),
        "ladybug_version": manifest.get("ladybug_version"),
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Restore a knowledge database from a snapshot directory."
    )
    parser.add_argument("--snapshot", required=True, help="snapshot directory")
    parser.add_argument("--out", required=True, help="target database path (must not exist)")
    args = parser.parse_args(argv)

    try:
        report = recover(Path(args.snapshot), Path(args.out))
    except (FileNotFoundError, FileExistsError, ValueError, RuntimeError) as exc:
        print(f"recovery failed: {exc}", file=sys.stderr)
        return 1

    print(f"Recovered database: {report['target_db']}")
    print(f"  nodes: {report['nodes']}  rels: {report['rels']}")
    print(f"  source revision: {report['source_revision']}")
    print(f"  ladybug {report['ladybug_version']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
