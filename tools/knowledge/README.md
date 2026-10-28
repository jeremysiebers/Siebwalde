# Siebwalde Knowledge Foundation

A persistent, evidence-backed engineering knowledge system for the Siebwalde
OpenCode agents, built on **LadybugDB** (an embedded graph database) and exposed
through a **read-only Knowledge MCP** server.

The purpose is to stop the team from repeatedly losing or misinterpreting
established technical knowledge (protocol contracts, firmware versions, safety
invariants, prior failures). The knowledge graph is a **structured index** —
the authoritative sources remain the Git tree, the immutable firmware artifacts
and the raw test evidence.

## Source of truth and operational database

- **Git source of truth:** `schema.py` + `knowledge_seed.py` are the complete,
  publishable knowledge (207 nodes / 154 relationships). A fresh database can
  always be rebuilt from these two files alone — no archive, no NAS, no
  pre-existing `.lbdb` is required (proven by
  `tests/test_clean_rebuild_from_seed.py`).
- **Operational database:** `db/siebwalde-knowledge.lbdb` (git-ignored). It is a
  *cache* of the seed, rebuilt by `ingest/build_graph.py`, and may lag the seed
  if it has not been rebuilt after a seed change. The MCP server opens it
  read-only.
- **Supported LadybugDB version:** `0.21.2` (pinned in `requirements.txt`; the
  native runtime DLLs are staged by `setup_lbug.ps1`). Snapshot/recovery are
  validated against this version only — see *Compatibility limits* below.

## Layout

```
tools/knowledge/
  requirements.txt      pinned Python dependencies
  setup_lbug.ps1        stage + verify the native lbug/OpenSSL runtime DLLs
  lib/                  (git-ignored) native runtime: lbug_shared.dll + OpenSSL 3
  schema.py             single-source schema (19 node tables + 21 relationship tables)
  knowledge_seed.py     curated knowledge (207 nodes / 154 relationships, with provenance)
  ingest/build_graph.py deterministic ingestion -> db/siebwalde-knowledge.lbdb
  snapshot.py           EXPORT DATABASE -> verifiable snapshot dir + snapshot-manifest.json
  recover.py            IMPORT DATABASE from a snapshot + fail-closed verification
  _runtime.py           shared read-only helpers (lib bootstrap, counting, hashing)
  mcp_server/server.py  read-only Knowledge MCP (FastMCP; narrow tools, no raw Cypher)
  tests/test_knowledge.py  acceptance + boundary tests
  tests/test_preservation_guard.py  tool-registry preservation guard tests
  tests/test_clean_rebuild_from_seed.py  Git-only reconstruction + duplicate-id failure
  tests/test_export_import_roundtrip.py  lossless EXPORT/IMPORT round-trip
  tests/test_snapshot_recovery.py  snapshot/recover round-trip + fail-closed failures
  db/                   (git-ignored) the persistent local graph database
  snapshots/            (git-ignored) snapshot output directories produced by snapshot.py
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

## Snapshot (export)

`snapshot.py` produces a self-contained, integrity-verifiable snapshot of the
operational database using LadybugDB `EXPORT DATABASE`. It opens the source
**read-only** (never modifies it), records the table inventory + per-table
counts, exports, then writes a `snapshot-manifest.json` with the LadybugDB
version, storage version, source database path, source git revision, counts,
the export file list with SHA-256 digests, and a UTC timestamp.

```powershell
# Default: snapshots tools/knowledge/db/siebwalde-knowledge.lbdb
# into tools/knowledge/snapshots/<UTC-timestamp>/
tools/knowledge/.venv/Scripts/python.exe tools/knowledge/snapshot.py

# Explicit source and output, replacing an existing directory:
tools/knowledge/.venv/Scripts/python.exe tools/knowledge/snapshot.py `
    --db tools/knowledge/db/siebwalde-knowledge.lbdb `
    --out D:\backups\knowledge-2026-10-25 --force
```

Snapshotting is deterministic and idempotent: it refuses to overwrite an
existing snapshot directory unless `--force` is passed. The snapshot directory
is git-ignored (`snapshots/`).

## Restore (recovery)

`recover.py` restores a database from a snapshot directory using `IMPORT
DATABASE`, then verifies the result **fail-closed**: any missing/corrupt file,
schema mismatch, pre-existing target, or post-import mismatch aborts with a
non-zero exit.

```powershell
tools/knowledge/.venv/Scripts/python.exe tools/knowledge/recover.py `
    --snapshot D:\backups\knowledge-2026-10-25 `
    --out tools/knowledge/db/siebwalde-knowledge.lbdb
```

The target path must **not already exist** (recovery never overwrites). After
import it verifies: per-table counts match the manifest, the totals match the
seed (207/154), and representative nodes - including the K6/K7/K8 findings the
stale operational database was missing — plus their provenance fields are
present.

## MCP validation

After a rebuild or restore, confirm the read-only MCP still sees the complete
graph. The test suite does this automatically; manually:

```powershell
tools/knowledge/.venv/Scripts/python.exe -m pytest tools/knowledge/tests -q
```

This runs the acceptance questions (§10) and the MCP read-only surface checks
against the database the MCP opens. If any of `test_k6b_*`, `test_k7_*`,
`test_k8_*` fail, the operational database is stale and must be rebuilt from
the seed (or restored from a snapshot taken after those findings were added).

## Deterministic integrity checks

- Rebuilding from the seed is deterministic: the same `schema.py` +
  `knowledge_seed.py` always produce the same 207 nodes / 154 relationships and
  the same per-table counts (`test_clean_rebuild_from_seed.py`,
  `test_rebuild_is_deterministic`).
- Export/import is lossless: every node's full property set (including
  provenance) and every relationship's endpoints round-trip unchanged
  (`test_export_import_roundtrip.py`).
- Snapshots are self-verifying: `snapshot-manifest.json` records a SHA-256 for
  every exported file, and `recover.py` re-hashes and compares them before
  importing, so a truncated or missing parquet is caught before it can be
  imported (`test_snapshot_recovery.py`).
- A duplicate primary key in the seed fails the build (`test_build_fails_on_duplicate_id`).

## WAL / shadow handling

`snapshot.py` opens the source read-only and uses `EXPORT DATABASE`, which
writes a fresh, consistent export without touching the source database's
write-ahead log or shadow files. The source database is never opened for write
during snapshot or recovery, so no WAL replay is triggered against it. The
snapshot directory contains only the exported parquet/cypher files plus the
manifest (no WAL or shadow state is carried over); recovery re-creates a clean
database via `IMPORT DATABASE`.

## Recovery from a fresh checkout / NAS-only

The Git tree is sufficient. On a machine with no operational database, no
archive and no NAS:

1. Stage the native runtime: `.\tools\knowledge\setup_lbug.ps1`.
2. Rebuild from source (no network, no live sources):

   ```powershell
   tools/knowledge/.venv/Scripts/python.exe tools/knowledge/ingest/build_graph.py
   ```

3. Or, if you have a snapshot directory (e.g. copied off the NAS), restore it
   directly with `recover.py` — no rebuild required.

## Schema migration

`schema.py` is the single source of the schema. To add a node/relationship
type or property, edit `schema.py` (and `knowledge_seed.py` as needed), then
rebuild. Because the graph is rebuilt from source, there is no in-place
migration to maintain; keep `schema.py` as the migration record.

## Compatibility limits

- Snapshots are LadybugDB `0.21.2` exports (storage version 47). A different
  LadybugDB version may change the export format; do not assume a snapshot
  restores under a different version. Re-stage the matching native runtime.
- `recover.py` compares the snapshot's table inventory and totals against the
  current `schema.py` / `knowledge_seed.py`. Restoring a snapshot taken from an
  *older* seed (fewer nodes/tables) with newer code fails closed rather than
  silently restoring incomplete knowledge.
- Snapshot/recovery are offline software operations over the local database
  only; they grant no hardware authority and do not touch C#/firmware.

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
