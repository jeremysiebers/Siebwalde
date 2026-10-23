# Siebwalde Knowledge Foundation

A persistent, evidence-backed engineering knowledge system for the Siebwalde
OpenCode agents, built on **LadybugDB** (an embedded graph database) and exposed
through a **read-only Knowledge MCP** server.

The purpose is to stop the team from repeatedly losing or misinterpreting
established technical knowledge (protocol contracts, firmware versions, safety
invariants, prior failures). The knowledge graph is a **structured index** —
the authoritative sources remain the Git tree, the immutable firmware artifacts
and the raw test evidence.

## Layout

```
tools/knowledge/
  requirements.txt      pinned Python dependencies
  setup_lbug.ps1        stage + verify the native lbug/OpenSSL runtime DLLs
  lib/                  (git-ignored) native runtime: lbug_shared.dll + OpenSSL 3
  schema.py             single-source schema (19 node tables + 17 relationship tables)
  knowledge_seed.py     curated initial knowledge (nodes + relationships, with provenance)
  ingest/build_graph.py deterministic ingestion -> db/siebwalde-knowledge.lbdb
  mcp_server/server.py  read-only Knowledge MCP (FastMCP; narrow tools, no raw Cypher)
  tests/test_knowledge.py  acceptance + boundary tests
  tests/test_preservation_guard.py  tool-registry preservation guard tests
  db/                   (git-ignored) the persistent local graph database
```

## One-time setup

```powershell
# 1. project-local venv + pinned dependencies
python -m venv tools/knowledge/.venv
tools/knowledge/.venv/Scripts/python.exe -m pip install -r tools/knowledge/requirements.txt

# 2. stage + verify the native runtime (downloads liblbug-windows-x86_64.zip
#    for LadybugDB v0.21.2, SHA-256-verified; requires the OpenSSL 3 x64 DLLs
#    libcrypto-3-x64.dll / libssl-3-x64.dll to be present in lib/)
.\tools\knowledge\setup_lbug.ps1

# 3. build the knowledge graph
tools/knowledge/.venv/Scripts/python.exe tools/knowledge/ingest/build_graph.py
```

## Rebuild / offline rebuild

The graph is rebuilt deterministically from `schema.py` + `knowledge_seed.py`
(no live sources, no network required):

```powershell
tools/knowledge/.venv/Scripts/python.exe tools/knowledge/ingest/build_graph.py
```

This removes the previous `db/` directory, re-creates the schema, and inserts
the seed nodes/relationships in a fixed order. Re-run after any seed change.

## Query

The knowledge graph is queried through the read-only Knowledge MCP (registered
as `knowledge` in `opencode.json`). Its tools are:

- `knowledge_search(query)` — substring search across all node types, with provenance.
- `knowledge_node(id)` — one node + its direct relationships.
- `knowledge_protocol_contract(command)` — command → expected response / required state / safety guards.
- `knowledge_safety_invariants(scope)` — safety invariants.
- `knowledge_firmware_state()` — per-device firmware (RUNS_FIRMWARE).
- `knowledge_flashing_paths()` — proven vs experimental flashing paths.
- `knowledge_incidents()` — recorded incidents.
- `knowledge_unresolved()` — UNKNOWN / HYPOTHESIS / CONTRADICTED / INFERRED assertions.
- `knowledge_conflicts()` — CONTRADICTED_BY findings.
- `knowledge_schema()` — the graph schema + status vocabularies.
- `knowledge_tools(query="")` — engineering tools from the tool registry (K4-A),
  with lifecycle / preservation_policy / capabilities (PROVIDES_CAPABILITY).

Read-only is enforced twice: the database is opened with `read_only=True`
(the engine rejects writes), and the tools are fixed `MATCH`/`RETURN` templates
(there is no raw-Cypher or file/write/shell capability).

## Tests

```powershell
tools/knowledge/.venv/Scripts/python.exe -m pytest tools/knowledge/tests -q
```

## Backup, export, schema migration

- **Backup:** the database is a single directory `db/siebwalde-knowledge.lbdb`.
  Copy the directory to back it up; restore by copying it back. It is
  git-ignored by design (runtime data, rebuilt deterministically).
- **Export:** the canonical export is the seed itself (`schema.py` +
  `knowledge_seed.py`) plus the rebuilt graph. `build_graph.py` is the
  deterministic, repeatable ingestion path.
- **Schema migration:** `schema.py` is the single source of the schema. To add a
  node/relationship type or property, edit `schema.py` (and `knowledge_seed.py`
  as needed), then rebuild. Because the graph is rebuilt from source, there is
  no in-place migration to maintain; keep `schema.py` as the migration record.

## Status vocabulary

Assertion statuses (per node): `PROVEN_HARDWARE`, `PROVEN_SOURCE`,
`PROVEN_OFFLINE_TEST`, `INFERRED`, `HYPOTHESIS`, `UNKNOWN`, `CONTRADICTED`.

Evidence statuses: `OBSERVED`, `TEST_PASS`, `SOURCE_REF`, `DERIVED`, `UNVERIFIED`.

## Provenance contract

Every assertion node carries `source_path`, `source_location`,
`source_revision`, `source_hash`, `evidence_ref`, `evidence_status` and
`observed_date`, so every answer is traceable to its authoritative source. An
agent must report **UNKNOWN** when the data does not support a conclusion; do
not promote hypotheses to proven facts.
