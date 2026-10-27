"""Clean-rebuild tests for the Knowledge Foundation.

Prove that a complete knowledge graph (208 nodes / 154 relationships, matching
the curated seed exactly) can be reconstructed from Git-only sources — i.e.
``schema.py`` + ``knowledge_seed.py`` via ``ingest/build_graph.py`` — with no
dependence on the operational ``.lbdb``, any archive, or the NAS. Also prove
that a duplicate primary key in the seed fails the build (fail closed).

These tests never touch the operational database: they build into ``tmp_path``.
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

from tools.knowledge import _runtime, knowledge_seed, schema  # noqa: E402
from tools.knowledge.ingest import build_graph  # noqa: E402


def _expected_counts() -> dict:
    """Per-table node/relationship counts implied by the curated seed."""
    node_counts = {t: 0 for t in schema.NODE_TABLES}
    rel_counts = {t: 0 for t in schema.REL_TABLES}
    for node_type, _props in knowledge_seed.NODES:
        node_counts[node_type] += 1
    for rel_name, _f, _t, _p in knowledge_seed.RELS:
        rel_counts[rel_name] += 1
    return {"nodes": node_counts, "rels": rel_counts}


def test_clean_rebuild_from_seed(tmp_path):
    """Rebuilding from Git-only sources yields exactly the seed (208/154)."""
    db_path = tmp_path / "k.lbdb"
    stats = build_graph._build(db_path)
    assert stats == {"node_count": 208, "rel_count": 154}

    counts = _runtime.count_tables(db_path)
    expected = _expected_counts()

    assert sum(counts["nodes"].values()) == 208
    assert sum(counts["rels"].values()) == 154
    assert len(knowledge_seed.NODES) == 208
    assert len(knowledge_seed.RELS) == 154

    # Per-table node counts must match the seed exactly.
    assert counts["nodes"] == expected["nodes"], (
        f"node counts diverge from seed: {counts['nodes']} != {expected['nodes']}"
    )
    # Per-table relationship counts must match the seed exactly.
    assert counts["rels"] == expected["rels"], (
        f"rel counts diverge from seed: {counts['rels']} != {expected['rels']}"
    )


def test_rebuild_is_deterministic(tmp_path):
    """Two clean rebuilds produce identical per-table counts."""
    p1 = tmp_path / "a" / "k.lbdb"
    p2 = tmp_path / "b" / "k.lbdb"
    build_graph._build(p1)
    build_graph._build(p2)
    c1 = _runtime.count_tables(p1)
    c2 = _runtime.count_tables(p2)
    assert c1 == c2


def test_build_fails_on_duplicate_id(tmp_path, monkeypatch):
    """A seed with a duplicate primary key (same id in one node table) fails."""
    dup = (
        "Observation",
        {"id": "obs:k6b-cmd113-readback", "label": "duplicate", "status": "HYPOTHESIS"},
    )
    monkeypatch.setattr(build_graph.seed, "NODES", build_graph.seed.NODES + [dup])
    with pytest.raises(Exception):
        build_graph._build(tmp_path / "k.lbdb")
