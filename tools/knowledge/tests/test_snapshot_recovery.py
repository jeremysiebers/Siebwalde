"""Snapshot/recovery tests for the Knowledge Foundation.

Covers the ``snapshot.py`` -> ``recover.py`` round trip on a temporary database
and the fail-closed failure modes: missing export file, corrupted (truncated)
parquet, schema mismatch, missing native library, refusing to overwrite an
existing snapshot, and refusing to import over an existing database.

Every test uses ``tmp_path``; the operational database is never opened.
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
from pathlib import Path

import pytest

_REPO = Path(__file__).resolve().parents[3]          # repository root
_KNOWLEDGE = _REPO / "tools" / "knowledge"

sys.path.insert(0, str(_REPO))
os.environ.setdefault("LBUG_C_API_LIB_PATH", str(_KNOWLEDGE / "lib" / "lbug_shared.dll"))
os.environ["PATH"] = str(_KNOWLEDGE / "lib") + os.pathsep + os.environ.get("PATH", "")

import ladybug as lb  # noqa: E402

from tools.knowledge import _runtime, snapshot, recover  # noqa: E402
from tools.knowledge.ingest import build_graph  # noqa: E402


def _build_source(tmp_path: Path) -> Path:
    db_path = tmp_path / "source.lbdb"
    build_graph._build(db_path)
    return db_path


def _dir_digest(path: Path) -> dict:
    """Return {name: sha256} for every file in a directory (recursively)."""
    out = {}
    for p in sorted(path.rglob("*")):
        if p.is_file():
            out[str(p.relative_to(path))] = _runtime.sha256_file(p)
    return out


def test_snapshot_recovery_roundtrip(tmp_path):
    """snapshot.py then recover.py reproduce the seed graph in a fresh DB."""
    source = _build_source(tmp_path)
    snap_dir = tmp_path / "snap"

    manifest = snapshot.snapshot(source, snap_dir)

    # manifest metadata
    assert manifest["ladybug_version"] == "0.21.2"
    assert manifest["source_revision"] == _runtime.git_revision()
    assert manifest["totals"] == {"nodes": 207, "rels": 154}
    assert manifest["source_db_path"] == str(source.resolve())
    assert manifest["files"], "manifest lists no export files"
    for entry in manifest["files"]:
        assert entry["name"] and entry["sha256"] and entry["size"] >= 0

    # snapshot dir contains the export + the manifest
    assert (snap_dir / "schema.cypher").is_file()
    assert (snap_dir / "index.cypher").is_file()
    assert (snap_dir / "snapshot-manifest.json").is_file()

    target = tmp_path / "target.lbdb"
    report = recover.recover(snap_dir, target)

    assert report["nodes"] == 207
    assert report["rels"] == 154
    assert _runtime.count_tables(target) == _runtime.count_tables(source)

    # a representative node survived with correct provenance
    db = lb.Database(str(target), read_only=True)
    conn = lb.Connection(db)
    try:
        rows = conn.execute(
            "MATCH (n:ProtocolCommand) WHERE n.id = 'command:113' "
            "RETURN n.command_name, n.source_path"
        ).get_all()
    finally:
        conn.close()
        db.close()
    assert rows[0] == ["GET_CONTROLLER_VERSION", "TrackController5/firmware/src/controller.c"]


def test_snapshot_does_not_modify_source_db(tmp_path):
    """snapshot.py opens the source read-only and never modifies it."""
    source = tmp_path / "src" / "source.lbdb"
    build_graph._build(source)
    before = _dir_digest(source.parent)  # only the source db directory
    snapshot.snapshot(source, tmp_path / "snap")
    after = _dir_digest(source.parent)
    assert before == after


def test_snapshot_refuses_overwrite_without_force(tmp_path):
    source = _build_source(tmp_path)
    snap_dir = tmp_path / "snap"
    snapshot.snapshot(source, snap_dir)
    with pytest.raises(FileExistsError):
        snapshot.snapshot(source, snap_dir)
    # with --force it replaces the directory and still succeeds
    manifest = snapshot.snapshot(source, snap_dir, force=True)
    assert manifest["totals"] == {"nodes": 207, "rels": 154}


def test_recover_refuses_existing_target(tmp_path):
    source = _build_source(tmp_path)
    snap_dir = tmp_path / "snap"
    snapshot.snapshot(source, snap_dir)
    existing = tmp_path / "existing.lbdb"
    existing.mkdir()  # pre-existing path (any content) must be refused
    with pytest.raises(FileExistsError):
        recover.recover(snap_dir, existing)


def test_recover_missing_export_file(tmp_path):
    """A snapshot missing an export file fails recovery (fail closed)."""
    source = _build_source(tmp_path)
    snap_dir = tmp_path / "snap"
    snapshot.snapshot(source, snap_dir)
    (snap_dir / "nodes_Observation.parquet").unlink()
    with pytest.raises(ValueError, match="missing export file"):
        recover.recover(snap_dir, tmp_path / "target.lbdb")


def test_recover_corrupted_parquet(tmp_path):
    """A truncated parquet file fails recovery (fail closed)."""
    source = _build_source(tmp_path)
    snap_dir = tmp_path / "snap"
    snapshot.snapshot(source, snap_dir)
    parquet = snap_dir / "nodes_Observation.parquet"
    parquet.write_bytes(parquet.read_bytes()[:10])  # truncate -> different SHA-256
    with pytest.raises(ValueError, match="corrupt export file"):
        recover.recover(snap_dir, tmp_path / "target.lbdb")


def test_recover_schema_mismatch(tmp_path):
    """A snapshot whose table inventory differs from schema.py fails recovery."""
    source = _build_source(tmp_path)
    snap_dir = tmp_path / "snap"
    snapshot.snapshot(source, snap_dir)

    manifest_path = snap_dir / "snapshot-manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    manifest["node_tables"] = [t for t in manifest["node_tables"] if t != "Observation"]
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    with pytest.raises(ValueError, match="schema mismatch"):
        recover.recover(snap_dir, tmp_path / "target.lbdb")


def test_recover_missing_manifest(tmp_path):
    """A directory that is not a snapshot (no manifest) fails recovery."""
    not_a_snapshot = tmp_path / "not-a-snapshot"
    not_a_snapshot.mkdir()
    with pytest.raises(FileNotFoundError, match="manifest not found"):
        recover.recover(not_a_snapshot, tmp_path / "target.lbdb")


def test_missing_native_lib_fails():
    """Loading the native C API with a missing library fails in a clean process."""
    # __version__ forces the C-API module to load, which is where the DLL is dlopen'd.
    code = "import ladybug; print(ladybug.__version__)"
    env = dict(os.environ)
    env["LBUG_C_API_LIB_PATH"] = str(_KNOWLEDGE / "lib" / "definitely-missing-lbug.dll")
    libdir = os.path.normcase(os.path.abspath(str(_KNOWLEDGE / "lib")))
    env["PATH"] = os.pathsep.join(
        p for p in env.get("PATH", "").split(os.pathsep)
        if p and os.path.normcase(os.path.abspath(p)) != libdir
    )
    proc = subprocess.run(
        [sys.executable, "-c", code], capture_output=True, text=True, env=env
    )
    assert proc.returncode != 0


def test_native_lib_present_control():
    """Positive control: with the valid native library, the C API loads."""
    code = "import ladybug; print(ladybug.__version__)"
    proc = subprocess.run(
        [sys.executable, "-c", code], capture_output=True, text=True, env=dict(os.environ)
    )
    assert proc.returncode == 0, proc.stderr
    assert proc.stdout.strip() == "0.21.2"
