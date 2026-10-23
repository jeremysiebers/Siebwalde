"""Siebwalde Knowledge Foundation — deterministic graph builder.

Rebuilds the persistent local knowledge graph from the curated seed
(``knowledge_seed.py``) and the schema (``schema.py``). Deterministic and
idempotent: it removes any previous database directory, creates a fresh
on-disk LadybugDB database, applies the schema, and inserts the seed nodes and
relationships in a fixed order.

Usage::

    python tools/knowledge/ingest/build_graph.py

The database is written to ``tools/knowledge/db/siebwalde-knowledge.lbdb``
(git-ignored). Re-run any time the seed changes; the authoritative sources are
the Git tree, immutable artifacts and raw evidence — this database is only a
structured index.
"""

from __future__ import annotations

import os
import shutil
import sys
from pathlib import Path

_HERE = Path(__file__).resolve().parent          # tools/knowledge/ingest
_KNOWLEDGE = _HERE.parent                         # tools/knowledge
_REPO = _KNOWLEDGE.parent.parent                  # repository root

sys.path.insert(0, str(_KNOWLEDGE))

# Ensure the native lbug library + OpenSSL runtime are discoverable.
_LIB = _KNOWLEDGE / "lib"
if (_LIB / "lbug_shared.dll").exists():
    os.environ.setdefault("LBUG_C_API_LIB_PATH", str(_LIB / "lbug_shared.dll"))
if str(_LIB) not in os.environ.get("PATH", ""):
    os.environ["PATH"] = str(_LIB) + os.pathsep + os.environ.get("PATH", "")

DB_DIR = _KNOWLEDGE / "db"
DB_PATH = DB_DIR / "siebwalde-knowledge.lbdb"

import ladybug as lb  # noqa: E402

import schema  # noqa: E402
import knowledge_seed as seed  # noqa: E402


def _cypher_value(value) -> str:
    """Render a Python value as a Cypher literal."""
    if value is None:
        return "null"
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, int):
        return str(value)
    if isinstance(value, str):
        escaped = value.replace("\\", "\\\\").replace("'", "\\'")
        return f"'{escaped}'"
    raise TypeError(f"unsupported seed value type {type(value)!r}: {value!r}")


def _props_to_cypher(props: dict) -> str:
    return ", ".join(f"{k}: {_cypher_value(v)}" for k, v in props.items())


def _build(db_path: Path) -> dict:
    db_dir = db_path.parent
    if db_dir.exists():
        shutil.rmtree(db_dir)
    db_dir.mkdir(parents=True, exist_ok=True)

    db = lb.Database(str(db_path))
    conn = lb.Connection(db)

    # 1. schema
    for stmt in schema.schema_ddl():
        conn.execute(stmt)

    # 2. nodes (deterministic order)
    node_count = 0
    for node_type, props in seed.NODES:
        cols = _props_to_cypher(props)
        conn.execute(f"CREATE (:{node_type} {{{cols}}})")
        node_count += 1

    # 3. relationships (deterministic order)
    rel_count = 0
    for rel_name, from_id, to_id, rprops in seed.RELS:
        from_type, to_type, _ = schema.REL_TABLES[rel_name]
        rcols = _props_to_cypher(rprops) if rprops else ""
        if rcols:
            conn.execute(
                f"MATCH (a:{from_type} {{id: '{from_id}'}}), "
                f"(b:{to_type} {{id: '{to_id}'}}) "
                f"CREATE (a)-[:{rel_name} {{{rcols}}}]->(b)"
            )
        else:
            conn.execute(
                f"MATCH (a:{from_type} {{id: '{from_id}'}}), "
                f"(b:{to_type} {{id: '{to_id}'}}) "
                f"CREATE (a)-[:{rel_name}]->(b)"
            )
        rel_count += 1

    conn.close()
    db.close()
    return {"node_count": node_count, "rel_count": rel_count}


def _verify(db_path: Path) -> dict:
    db = lb.Database(str(db_path), read_only=True)
    conn = lb.Connection(db)
    counts = {}
    for node_type in schema.NODE_TABLES:
        res = conn.execute(f"MATCH (n:{node_type}) RETURN count(n) AS c")
        counts[node_type] = res.get_all()[0][0]
    conn.close()
    db.close()
    return counts


def main() -> int:
    stats = _build(DB_PATH)
    print(f"Built graph: {stats['node_count']} nodes, {stats['rel_count']} relationships")
    counts = _verify(DB_PATH)
    total = sum(counts.values())
    print(f"Verify node counts (total {total}):")
    for name, c in counts.items():
        print(f"  {name:20s} {c}")
    print(f"Database: {DB_PATH}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
