"""Siebwalde Knowledge Foundation — read-only Knowledge MCP server.

Exposes a small, purpose-built set of read-only tools over the LadybugDB
knowledge graph. This is deliberately NOT a generic Cypher endpoint (the
upstream ``mcp-server-ladybug`` exposes arbitrary ``query``, which would allow
graph mutation). Two independent read-only layers:

1. Engine-level: the database is opened with ``read_only=True``, so the
   LadybugDB engine itself rejects any write statement
   ("Cannot execute write operations in a read-only database!").
2. Surface-level: tools are fixed Cypher templates that only ever ``MATCH`` /
   ``RETURN``; there is no tool that accepts a raw query, and no file/write/
   shell capability.

Every answer includes the traceable source reference (source_path,
source_location, source_revision) and the assertion status, so an agent can
always re-locate the authoritative source.

Run as a module (see opencode.json ``mcp.knowledge``)::

    python -m tools.knowledge.mcp_server
"""

from __future__ import annotations

import os
import sys
from pathlib import Path

_KNOWLEDGE = Path(__file__).resolve().parent.parent   # tools/knowledge

_LIB = _KNOWLEDGE / "lib"
if (_LIB / "lbug_shared.dll").exists():
    os.environ.setdefault("LBUG_C_API_LIB_PATH", str(_LIB / "lbug_shared.dll"))
if str(_LIB) not in os.environ.get("PATH", ""):
    os.environ["PATH"] = str(_LIB) + os.pathsep + os.environ.get("PATH", "")

from fastmcp import FastMCP  # noqa: E402

import ladybug as lb  # noqa: E402

from .. import schema  # noqa: E402  (relative import: tools.knowledge.schema)

DB_PATH = Path(os.environ.get(
    "SIEBWALDE_KNOWLEDGE_DB",
    str(_KNOWLEDGE / "db" / "siebwalde-knowledge.lbdb"),
))

MAX_ROWS = int(os.environ.get("SIEBWALDE_KNOWLEDGE_MAX_ROWS", "200"))

mcp = FastMCP("knowledge")

_PROVENANCE_COLS = "n.id, n.label, n.status, n.summary, n.source_path, n.source_location, n.source_revision"


def _connect():
    """Open a fresh read-only connection (engine-enforced read-only)."""
    db = lb.Database(str(DB_PATH), read_only=True)
    conn = lb.Connection(db)
    return db, conn


def _rows(conn, cypher: str) -> tuple[list[str], list[list]]:
    res = conn.execute(cypher)
    try:
        cols = res.get_column_names()
    except Exception:
        cols = []
    return cols, res.get_all()


def _to_records(cols: list[str], rows: list[list]) -> list[dict]:
    return [dict(zip(cols, r)) for r in rows]


def _emit(cols: list[str], rows: list[list], note: str = "") -> dict:
    rows = rows[:MAX_ROWS]
    return {
        "count": len(rows),
        "note": note,
        "results": _to_records(cols, rows),
    }


def _search_columns():
    return [
        "id", "label", "type", "status", "summary",
        "source_path", "source_location", "source_revision", "evidence_status",
    ]


def _search_all(query: str) -> dict:
    """Substring search across every node table (summary + label)."""
    q = query.replace("\\", "\\\\").replace("'", "\\'")
    out = []
    db, conn = _connect()
    try:
        for ntype in schema.NODE_TABLES:
            cypher = (
                f"MATCH (n:{ntype}) "
                f"WHERE n.summary CONTAINS '{q}' OR n.label CONTAINS '{q}' "
                f"RETURN n.id, n.label, '{ntype}', n.status, n.summary, "
                f"n.source_path, n.source_location, n.source_revision, n.evidence_status"
            )
            cols, rows = _rows(conn, cypher)
            for r in rows:
                out.append(list(r))
    finally:
        conn.close()
        db.close()
    cols = _search_columns()
    return _emit(cols, out, note=f"substring search for '{query}' across all node types")


@mcp.tool()
def knowledge_search(query: str) -> dict:
    """Search the Siebwalde engineering knowledge graph by substring.

    Matches node summaries and labels across every node type and returns the
    matching assertions with their status and traceable source reference
    (source_path / source_location / source_revision). Read-only.
    """
    if not query or not query.strip():
        return {"count": 0, "note": "empty query", "results": []}
    return _search_all(query.strip())


@mcp.tool()
def knowledge_node(node_id: str) -> dict:
    """Retrieve a single knowledge node by id, with provenance and its direct relationships.

    Returns the node's full property set plus every relationship it participates in.
    Read-only. Use knowledge_search to discover ids.
    """
    node_id = (node_id or "").strip()
    if not node_id:
        return {"count": 0, "note": "empty id", "results": []}
    db, conn = _connect()
    try:
        # Locate the node across all tables (retrieve its properties).
        node = None
        ntype = None
        for t in schema.NODE_TABLES:
            cols, rows = _rows(
                conn,
                f"MATCH (n:{t}) WHERE n.id = '{node_id}' RETURN n.*",
            )
            if rows:
                node = _to_records(cols, rows)[0]
                ntype = t
                break
        if node is None:
            return {"count": 0, "note": f"no node with id '{node_id}'", "results": []}

        # Enumerate relationships (outgoing + incoming) via the schema.
        rels = []
        for rname, (frm, to, _) in schema.REL_TABLES.items():
            cols, rows = _rows(
                conn,
                f"MATCH (a:{frm})-[r:{rname}]->(b:{to}) "
                f"WHERE a.id = '{node_id}' OR b.id = '{node_id}' "
                f"RETURN '{rname}', a.id, b.id",
            )
            for r in rows:
                rels.append({
                    "rel": r[0],
                    "from": r[1],
                    "to": r[2],
                    "dir": "out" if r[1] == node_id else "in",
                })
        return {
            "node": {"type": ntype, "properties": node},
            "relationships": rels,
        }
    finally:
        conn.close()
        db.close()


@mcp.tool()
def knowledge_protocol_contract(command: str) -> dict:
    """Retrieve a protocol command contract: the command, its expected response,
    required precondition state, and the safety invariants that guard it.

    ``command`` may be a command number (e.g. "125") or a name (e.g.
    "ERASE_FLASH"). Read-only.
    """
    command = (command or "").strip()
    if not command:
        return {"count": 0, "note": "empty command", "results": []}
    db, conn = _connect()
    try:
        where = ""
        if command.isdigit():
            where = f"n.command = {command}"
        else:
            q = command.replace("\\", "\\\\").replace("'", "\\'")
            where = f"n.command_name CONTAINS '{q}' OR n.label CONTAINS '{q}'"
        cols, rows = _rows(
            conn,
            f"MATCH (n:ProtocolCommand) WHERE {where} "
            f"RETURN n.id, n.command, n.command_name, n.direction, n.destructive, "
            f"n.description, n.summary, n.status, n.source_path, n.source_location, n.source_revision",
        )
        commands = _to_records(cols, rows)

        contracts = []
        for c in commands:
            cid = c["n.id"]
            detail = {"command": c, "expects_response": [], "requires_state": [], "guarded_by": []}
            # expected response
            _, er = _rows(
                conn,
                f"MATCH (a:ProtocolCommand)-[r:EXPECTS_RESPONSE]->(b:ProtocolCommand) "
                f"WHERE a.id = '{cid}' RETURN b.id, b.command, b.command_name",
            )
            detail["expects_response"] = _to_records(["id", "command", "command_name"], er)
            # required state
            _, rs = _rows(
                conn,
                f"MATCH (a:ProtocolCommand)-[:REQUIRES_STATE]->(s:ProtocolState) "
                f"WHERE a.id = '{cid}' RETURN s.id, s.state_name",
            )
            detail["requires_state"] = _to_records(["id", "state_name"], rs)
            # guarding invariants
            _, gb = _rows(
                conn,
                f"MATCH (a:ProtocolCommand)-[:GUARDED_BY]->(s:SafetyInvariant) "
                f"WHERE a.id = '{cid}' RETURN s.id, s.label, s.statement",
            )
            detail["guarded_by"] = _to_records(["id", "label", "statement"], gb)
            contracts.append(detail)
        return {"count": len(contracts), "results": contracts}
    finally:
        conn.close()
        db.close()


@mcp.tool()
def knowledge_safety_invariants(scope: str = "") -> dict:
    """List safety invariants, optionally filtered by a substring of their
    ``applies_to`` scope (e.g. "flash", "bootloader", "recovery"). Read-only.
    """
    db, conn = _connect()
    try:
        extra = ""
        if scope and scope.strip():
            q = scope.strip().replace("\\", "\\\\").replace("'", "\\'")
            extra = f" WHERE n.applies_to CONTAINS '{q}' OR n.label CONTAINS '{q}'"
        cols, rows = _rows(
            conn,
            f"MATCH (n:SafetyInvariant){extra} "
            f"RETURN n.id, n.label, n.statement, n.applies_to, n.requires_authorization, "
            f"n.status, n.source_path, n.source_location, n.source_revision",
        )
        return _emit(cols, rows, note=f"safety invariants (scope filter: '{scope}')")
    finally:
        conn.close()
        db.close()


@mcp.tool()
def knowledge_firmware_state() -> dict:
    """Report the recorded firmware state per hardware device (RUNS_FIRMWARE edges).

    Includes the assertion status so UNKNOWN states are explicit, plus the
    traceable source reference. Read-only.
    """
    db, conn = _connect()
    try:
        cols, rows = _rows(
            conn,
            "MATCH (d:HardwareDevice)-[r:RUNS_FIRMWARE]->(f:FirmwareArtifact) "
            "RETURN d.id, d.label, d.status, f.id, f.label, f.checksum, f.version, "
            "r.since, d.source_path, d.source_location, d.source_revision",
        )
        return _emit(cols, rows, note="recorded firmware state per device (RUNS_FIRMWARE)")
    finally:
        conn.close()
        db.close()


@mcp.tool()
def knowledge_flashing_paths() -> dict:
    """Compare the proven fleet/single-target flashing paths against the
    experimental single-target primitive path (120..130), including which are
    destructive and which safety invariants guard them. Read-only.
    """
    db, conn = _connect()
    try:
        cols, rows = _rows(
            conn,
            "MATCH (n:ProtocolCommand) "
            "WHERE n.command IN [122, 131, 132, 125, 126, 127, 128, 129, 123] "
            "RETURN n.id, n.command, n.command_name, n.destructive, n.status, "
            "n.summary, n.source_path, n.source_location, n.source_revision",
        )
        return _emit(cols, rows, note="flashing paths: 122/131/132 proven, 123/125-129 experimental primitives")
    finally:
        conn.close()
        db.close()


@mcp.tool()
def knowledge_incidents() -> dict:
    """List recorded incidents and their related hypotheses, with statuses.
    Read-only.
    """
    db, conn = _connect()
    try:
        cols, rows = _rows(
            conn,
            "MATCH (i:Incident) "
            "RETURN i.id, i.label, i.incident_date, i.severity, i.status, i.summary, "
            "i.source_path, i.source_location, i.source_revision",
        )
        return _emit(cols, rows, note="recorded incidents")
    finally:
        conn.close()
        db.close()


@mcp.tool()
def knowledge_unresolved() -> dict:
    """List assertions that are unresolved: status UNKNOWN, HYPOTHESIS, or
    CONTRADICTED, plus explicit contradictions (CONTRADICTED_BY edges). This is
    where the knowledge graph itself says 'not proven'. Read-only.
    """
    db, conn = _connect()
    try:
        out = []
        for t in schema.NODE_TABLES:
            cols, rows = _rows(
                conn,
                f"MATCH (n:{t}) WHERE n.status IN ['UNKNOWN','HYPOTHESIS','CONTRADICTED','INFERRED'] "
                f"RETURN n.id, n.label, '{t}', n.status, n.summary, "
                f"n.source_path, n.source_location, n.source_revision",
            )
            for r in rows:
                out.append(list(r))
        return _emit(
            ["id", "label", "type", "status", "summary", "source_path", "source_location", "source_revision"],
            out, note="unresolved / not-proven assertions (UNKNOWN, HYPOTHESIS, CONTRADICTED, INFERRED)",
        )
    finally:
        conn.close()
        db.close()


@mcp.tool()
def knowledge_conflicts() -> dict:
    """Show conflicting findings: hypotheses marked CONTRADICTED and the
    CONTRADICTED_BY edges that name the contradicting evidence. Read-only.
    """
    db, conn = _connect()
    try:
        cols, rows = _rows(
            conn,
            "MATCH (h:Hypothesis)-[r:CONTRADICTED_BY]->(e:EvidenceSource) "
            "RETURN h.id, h.label, h.hypothesis, e.id, e.label, e.path",
        )
        return _emit(cols, rows, note="conflicting findings (CONTRADICTED_BY)")
    finally:
        conn.close()
        db.close()


@mcp.tool()
def knowledge_schema() -> dict:
    """Return the knowledge graph schema (node tables and relationship tables). Read-only."""
    return {
        "node_tables": list(schema.NODE_TABLES.keys()),
        "relationship_tables": {k: [v[0], v[1]] for k, v in schema.REL_TABLES.items()},
        "assertion_statuses": list(schema.ASSERTION_STATUSES),
        "evidence_statuses": list(schema.EVIDENCE_STATUSES),
    }


@mcp.tool()
def knowledge_tools(query: str = "") -> dict:
    """List engineering tools from the tool-registry knowledge graph, optionally
    filtered by a substring of their name or purpose.

    Returns per tool: id, name, path, lifecycle, preservation_policy,
    migration_candidate, physically_proven, offline_tested, po_decision, and the
    capabilities it provides (PROVIDES_CAPABILITY). Read-only: fixed
    MATCH/RETURN templates over a read-only connection, no raw Cypher.
    """
    db, conn = _connect()
    try:
        extra = ""
        if query and query.strip():
            q = query.strip().replace("\\", "\\\\").replace("'", "\\'")
            extra = f" WHERE n.label CONTAINS '{q}' OR n.purpose CONTAINS '{q}'"
        cols, rows = _rows(
            conn,
            f"MATCH (n:Tool){extra} "
            f"RETURN n.id, n.label, n.path, n.lifecycle, n.preservation_policy, "
            f"n.migration_candidate, n.physically_proven, n.offline_tested, n.po_decision",
        )
        recs = _to_records(cols, rows)
        tools = []
        for r in recs:
            tid = r["n.id"]
            _, caps = _rows(
                conn,
                f"MATCH (t:Tool)-[:PROVIDES_CAPABILITY]->(c:Capability) "
                f"WHERE t.id = '{tid}' RETURN c.id, c.name",
            )
            tools.append({
                "id": tid,
                "name": r["n.label"],
                "path": r["n.path"],
                "lifecycle": r["n.lifecycle"],
                "preservation_policy": r["n.preservation_policy"],
                "migration_candidate": r["n.migration_candidate"],
                "physically_proven": r["n.physically_proven"],
                "offline_tested": r["n.offline_tested"],
                "po_decision": r["n.po_decision"],
                "capabilities": _to_records(["id", "name"], caps),
            })
        return {"count": len(tools), "results": tools}
    finally:
        conn.close()
        db.close()


def main() -> None:
    mcp.run(transport="stdio")


if __name__ == "__main__":
    main()
