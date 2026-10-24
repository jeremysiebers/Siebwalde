"""Export/import round-trip tests for the Knowledge Foundation.

Builds a database from the seed, runs LadybugDB ``EXPORT DATABASE`` on it, then
``IMPORT DATABASE`` into a fresh database, and asserts that node/relationship
counts, every node's property values and the provenance fields survive the
round trip unchanged. This proves the export/import path (the raw mechanism
underneath ``snapshot.py`` / ``recover.py``) is lossless.

All databases live in ``tmp_path``; the operational database is never touched.
"""

from __future__ import annotations

import os
import sys
from pathlib import Path

import pytest

_REPO = Path(__file__).resolve().parents[3]          # repository root
_KNOWLEDGE = _REPO / "tools" / "knowledge"

sys.path.insert(0, str(_REPO))
os.environ.setdefault("LBUG_C_API_LIB_PATH", str(_KNOWLEDGE / "lib" / "lbug_shared.dll"))
os.environ["PATH"] = str(_KNOWLEDGE / "lib") + os.pathsep + os.environ.get("PATH", "")

import ladybug as lb  # noqa: E402

from tools.knowledge import _runtime, schema  # noqa: E402
from tools.knowledge.ingest import build_graph  # noqa: E402


def _dump_nodes(db_path: Path) -> dict:
    """Return {table: {id: {property: value}}} for every node (full property set)."""
    db = lb.Database(str(db_path), read_only=True)
    conn = lb.Connection(db)
    try:
        out = {}
        for table in schema.NODE_TABLES:
            res = conn.execute(f"MATCH (n:{table}) RETURN n.*")
            cols = res.get_column_names()
            rows = res.get_all()
            table_nodes = {}
            for row in rows:
                props = {col.split(".", 1)[1]: val for col, val in zip(cols, row)}
                table_nodes[props["id"]] = props
            out[table] = table_nodes
        return out
    finally:
        conn.close()
        db.close()


def _dump_rel_edges(db_path: Path) -> dict:
    """Return {rel_table: [(from_id, to_id), ...]} sorted, for every relationship."""
    db = lb.Database(str(db_path), read_only=True)
    conn = lb.Connection(db)
    try:
        out = {}
        for rel_name, (frm, to, _cols) in schema.REL_TABLES.items():
            res = conn.execute(
                f"MATCH (a:{frm})-[r:{rel_name}]->(b:{to}) RETURN a.id, b.id"
            )
            out[rel_name] = sorted((row[0], row[1]) for row in res.get_all())
        return out
    finally:
        conn.close()
        db.close()


def test_export_import_roundtrip(tmp_path):
    """EXPORT then IMPORT reproduces counts, properties and provenance exactly."""
    source = tmp_path / "source.lbdb"
    build_graph._build(source)

    export_dir = tmp_path / "export"
    export_lit = export_dir.as_posix()

    db = lb.Database(str(source), read_only=True)
    conn = lb.Connection(db)
    try:
        conn.execute(f"EXPORT DATABASE '{export_lit}'")
    finally:
        conn.close()
        db.close()

    assert (export_dir / "schema.cypher").is_file()
    assert (export_dir / "index.cypher").is_file()

    target = tmp_path / "target.lbdb"
    db = lb.Database(str(target))
    conn = lb.Connection(db)
    try:
        conn.execute(f"IMPORT DATABASE '{export_lit}'")
    finally:
        conn.close()
        db.close()

    # counts (nodes + rels, per table) must be identical
    assert _runtime.count_tables(source) == _runtime.count_tables(target)

    # every node's full property set (including provenance) must be identical
    assert _dump_nodes(source) == _dump_nodes(target)

    # every relationship's endpoints must be identical
    assert _dump_rel_edges(source) == _dump_rel_edges(target)


def test_roundtrip_representative_and_provenance(tmp_path):
    """Spot-check representative K6/K7/K8 nodes and provenance after round trip."""
    source = tmp_path / "source.lbdb"
    build_graph._build(source)
    export_dir = tmp_path / "export"
    export_lit = export_dir.as_posix()

    db = lb.Database(str(source), read_only=True)
    conn = lb.Connection(db)
    try:
        conn.execute(f"EXPORT DATABASE '{export_lit}'")
    finally:
        conn.close()
        db.close()

    target = tmp_path / "target.lbdb"
    db = lb.Database(str(target))
    conn = lb.Connection(db)
    try:
        conn.execute(f"IMPORT DATABASE '{export_lit}'")
    finally:
        conn.close()
        db.close()

    checks = [
        ("ProtocolCommand", "command:113", "command_name", "GET_CONTROLLER_VERSION"),
        ("ProtocolCommand", "command:113", "status", "PROVEN_SOURCE"),
        ("ProtocolCommand", "command:113", "source_path", "TrackController5/firmware/src/controller.c"),
        ("Observation", "obs:k6b-cmd113-readback", "status", "PROVEN_HARDWARE"),
        ("Observation", "obs:k6b-cmd113-readback", "source_path", "tools/diagnostics/k6b_observe.py"),
        ("Observation", "obs:k7-pic32-identity-transition", "source_revision", "bd5a9fa2"),
        ("Observation", "obs:k8-slaveryinfo-passive-blocked", "status", "PROVEN_SOURCE"),
        ("HardwareDevice", "device:pic32", "kind", "PIC32"),
        ("FirmwareArtifact", "firmware:pic32-0.1.0", "version", "0.1.0"),
    ]
    for node_type, node_id, prop, expected in checks:
        for db_path in (source, target):
            db = lb.Database(str(db_path), read_only=True)
            conn = lb.Connection(db)
            try:
                rows = conn.execute(
                    f"MATCH (n:{node_type}) WHERE n.id = '{node_id}' RETURN n.{prop}"
                ).get_all()
            finally:
                conn.close()
                db.close()
            assert rows and rows[0][0] == expected, (
                f"{node_type}:{node_id}.{prop} != {expected!r} in {db_path}"
            )
