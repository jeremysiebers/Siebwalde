# Siebwalde — Knowledge and Diagnostics Architecture

**Status:** Knowledge Foundation (K0/K1) delivered; Diagnostics MCP is a design
proposal only (STOP before live diagnostics implementation).

**Scope:** a persistent, evidence-backed engineering knowledge system shared by
the Siebwalde OpenCode agents, built on LadybugDB and exposed through a
read-only Knowledge MCP. This document records the exact software versions,
repository paths, Git commits, and the future Diagnostics MCP design.

---

## 1. Exact software and versions

| Component | Version | Notes |
|---|---|---|
| Python | 3.13.12 | system interpreter at `C:\Users\jerem\AppData\Local\Programs\Python\Python313` |
| LadybugDB (`ladybug`) | **0.21.2** | embedded graph DB (Kuzu successor), on-disk `.lbdb`, no server |
| `fastmcp` | 4.1.3 | MCP framework (matches `tools/picoscope`) |
| `mcp` | 2.3.0 | MCP SDK (pulled by fastmcp) |
| `tabulate` | 0.10.0 | result pretty-printing (CLI) |
| `pytest` | 9.1.1 | test runner (offline) |

Pinned in `tools/knowledge/requirements.txt`.

### Native runtime (Windows x64)

The `ladybug` Python wheel (`cp313-win_amd64`) links the native `lbug` C API
shared library, which is **not** bundled. The matching release asset and the
OpenSSL 3 runtime it imports are staged in `tools/knowledge/lib/` (git-ignored,
reproducible via `tools/knowledge/setup_lbug.ps1`):

| File | SHA-256 | Source |
|---|---|---|
| `lbug_shared.dll` | `F827CF829A1EA5E301A7E2200E28EC3927E0FFE51ADB523DACE891118B650D74` | `liblbug-windows-x86_64.zip` @ LadybugDB `v0.21.2` (zip SHA-256 `8A1A6B68ACBAEE7FAD8C0EC797EADEFCE1F0866EA75DB45239BCF41291A49C79`) |
| `libssl-3-x64.dll` | `FA19F63369F780F42A5EE90890448EA3A24A3BF031191BF7B2EAD20DBD6D34D5` | OpenSSL 3.0.x Win64 distribution |
| `libcrypto-3-x64.dll` | `52DCBE7EE8365E6BFEAF6EE4B541DDCA69C5A93A04C593B28DADCDAE5515F47F` | OpenSSL 3.0.x Win64 distribution |

---

## 2. Repository layout and Git

The knowledge system lives entirely under `tools/knowledge/` (plus the
project-scoped MCP entry in `opencode.json` and this document). The runtime
database and the native DLLs are git-ignored; only source, schema, seed,
ingestion, server and tests are committed.

- Branch: dedicated `feature/k0k1-knowledge-foundation` (not merged to master).
- Base revision: `feature/pico-5b-plan` @ `1f89c0b` (the PICO-5B incident docs
  and the authoritative `docs/firmware-flash-recovery-technical-reference.md`
  are read from this revision for the seed).

---

## 3. LadybugDB foundation

- **Embedded, single-file-directory** database (`db/siebwalde-knowledge.lbdb`),
  no database server. Write-Ahead Log + checkpoints; on-disk persistence.
- **Read-only mode is engine-enforced.** `lb.Database(path, read_only=True)`
  rejects any write statement with
  `Cannot execute write operations in a read-only database!`. This is the
  primary read-only boundary for the Knowledge MCP.
- **Concurrency:** the first release uses a controlled **build phase** (a
  single read-write `build_graph.py`) followed by **read-only agent access**
  (each MCP call opens its own short-lived read-only connection, then closes
  it). This avoids conflicting read-write/read-only processes and needs no
  permanent database service. If continuous updates are later required, a
  single-owner ingestion/query service is proposed as a separate increment.

---

## 4. Typed property graph schema

Single source: `tools/knowledge/schema.py`.

Node tables (12): `HardwareDevice`, `FirmwareArtifact`, `SourceRevision`,
`ProtocolCommand`, `ProtocolState`, `SafetyInvariant`, `TestExecution`,
`Observation`, `Incident`, `Hypothesis`, `Decision`, `EvidenceSource`.

Relationship tables (9): `RUNS_FIRMWARE`, `IMPLEMENTED_BY`, `REQUIRES_STATE`,
`EXPECTS_RESPONSE`, `GUARDED_BY`, `OBSERVED_IN`, `PROVEN_BY`,
`CONTRADICTED_BY`, `SUPERSEDES`.

Every node table carries a shared provenance contract: `id` (primary key),
`label`, `status`, `summary`, `source_path`, `source_location`,
`source_revision`, `source_hash`, `evidence_ref`, `evidence_status`,
`observed_date`, `superseded`.

Assertion statuses: `PROVEN_HARDWARE`, `PROVEN_SOURCE`, `PROVEN_OFFLINE_TEST`,
`INFERRED`, `HYPOTHESIS`, `UNKNOWN`, `CONTRADICTED`. Hypotheses are never
automatically promoted to proven facts.

LadybugDB requires fixed relationship endpoints, so `PROVEN_BY`,
`CONTRADICTED_BY` and `SUPERSEDES` are typed for their primary use
(artifacts/hypotheses); general assertion provenance is carried as node
properties.

---

## 5. Deterministic ingestion

`tools/knowledge/ingest/build_graph.py` rebuilds the graph deterministically:
delete `db/` → create schema → insert `knowledge_seed.py` nodes in fixed order →
insert relationships in fixed order. Idempotent and reproducible; no live
sources, no network, no generated-AI-summary-as-fact.

The seed (`tools/knowledge/knowledge_seed.py`) curates the domains that
previously caused failures:

- **Legacy PIC32/PIC18 communication** — 9-bit address-marked application UART
  (`PDSEL 0x3`, ~2.0161 Mbaud) vs 8-bit unaddressed bootloader UART
  (`PDSEL 0x0`, `0x55` prefix); the 9→8-bit transition (physically captured).
- **Flashing** — proven C# fleet path (122) and single-target recovery (131/132)
  vs the experimental single-target primitives (120→121→123→125→126→127→128→
  129→130); bootloader acks (202/212/222/232/242); the ERASE destructive
  boundary; physical target isolation.
- **Firmware versions** — L0 Baseline 0 (PIC32 `A90E44`, PIC18 `0x251F`), L1
  PIC32 `0.1.0`, PIC18 1.0.0 `0xE906`, L2 `0x195A` (not deployed), diagnostic
  `0x2D00`, rollback artifacts with SHA-256.
- **PICO-5B incident** — commands sent, timeouts received, the unsafe
  `--proceed-on-timeout` (ASSUME-OK) continuation, and the UNKNOWN target
  firmware state, with proven observations separated from hypotheses.

---

## 6. Knowledge MCP (read-only)

The upstream `mcp-server-ladybug` (v0.1.3) was evaluated and **not** reused
directly: it exposes a single generic `query` tool that passes arbitrary Cypher
to the database — which would allow `CREATE`/`DELETE`/`SET` graph mutation by
any agent. Its own `DatabaseClient` also opens the database read-write.

Instead, `tools/knowledge/mcp_server/server.py` is a purpose-built FastMCP
server with **two independent read-only layers**:

1. **Engine:** `lb.Database(path, read_only=True)` — the engine rejects writes.
2. **Surface:** ten fixed `MATCH`/`RETURN` tools (search, node,
   protocol-contract, safety-invariants, firmware-state, flashing-paths,
   incidents, unresolved, conflicts, schema). No raw-Cypher tool, no file/write/
   shell capability, bounded result rows.

Every answer carries `source_path` / `source_location` / `source_revision` and
the assertion status, so an agent can always re-locate the authoritative
source. The server is registered project-scoped in `opencode.json`
(`mcp.knowledge`), reversible by removing the entry; the global configuration
is unchanged, and the existing PicoScope MCP is untouched.

---

## 7. Mandatory agent knowledge workflow

Before PIC32/PIC18, bootloader, firmware-download or C# initialization work, the
responsible agent must retrieve the applicable contract from the Knowledge MCP
and identify: the proven baseline, the protocol/state-machine invariants, known
hazards, prior failed approaches, supporting evidence, and unresolved
uncertainty. An independent Integrator review validates these claims against
source evidence. The knowledge system is advisory evidence, not a substitute
for deterministic protocol and safety enforcement.

---

## 8. Acceptance tests

`tools/knowledge/tests/test_knowledge.py` (17 tests) covers the ten directive
questions plus missing knowledge, contradictory evidence, superseded
decisions, database restart persistence, deterministic rebuild, read-only
enforcement, multiple readers, bounded results, and the no-write MCP surface.

Summary of the ten answers (each asserted with source references):

1. **Physically proven UART modes:** 9-bit application (`PROVEN_HARDWARE`) and
   8-bit bootloader (`PROVEN_HARDWARE`).
2. **Fleet vs single-target:** fleet 122 and single-target recovery 131/132 are
   `PROVEN_HARDWARE`; the 125/126 primitives are destructive and
   `PROVEN_SOURCE` only.
3. **PICO-5B failure:** ERASE/WRITE/WRITE_CONFIG timed out without bootloader
   acks; `--proceed-on-timeout` (ASSUME-OK) continued past unconfirmed
   destructive operations.
4. **ERASE timeout ⇒ WRITE?** No — `safety:no-assume-ok` (a timeout is not
   confirmation of a destructive operation).
5. **Amplifier 3 state:** `UNKNOWN` (ERASE/WRITE sent without confirmed acks).
6. **Amp-3 backplane mapping:** address 3 → `HR0 0x04` → `AMP_ID7` → `RA3` →
   `J21` pin 7 (off-by-N: slot N → ID(N+4)).
7. **Proven vs experimental artifacts:** 0x251F/0xE906/A90E44/1.0.0 are
   `PROVEN_HARDWARE`; 0x2D00 is `PROVEN_OFFLINE_TEST`; 0x195A is
   `PROVEN_SOURCE` (never deployed).
8. **Contradicts "bootloader never worked":** the technical reference (§2.5)
   documents fleet 122 and single-target 131/132 succeeding.
9. **Operations requiring authorization:** flash/reset/recovery/power-cycle
   (`safety:authorization-required`).
10. **Unknown recovery state:** report `UNKNOWN` and STOP
    (`safety:unknown-state`).

---

## 9. Durable data ownership and provenance

Git source, immutable firmware artifacts and raw test evidence remain
authoritative. LadybugDB is a structured index. Large waveform captures and raw
logs stay out of the database; the graph stores verified references and hashes
(`evidence:scope-0b20f400`, `evidence:rollback-0x251F`, …). Rebuild is
deterministic from the seed. Backup/export/migration are documented in
`tools/knowledge/README.md`.

---

## 10. Diagnostics MCP design (proposal — NOT implemented)

This section is a design for a future increment; K0/K1 stops before any live
hardware-control implementation.

Three controlled hardware-access levels, reusing the existing C#/Python
implementations (no new independent firmware state machine):

- **OBSERVE** — read-only diagnostics: device identity/version, status
  registers (e.g. HR7/HR11 readback via the `--read-hr` CLI), stored traces and
  measurements. No state change.
- **TEST** — guarded initialization steps and protocol experiments (e.g. the
  non-destructive bootloader-version probe 124) within an explicitly authorized
  session.
- **FLASH/RECOVERY** — supervised target-specific firmware operations: mandatory
  artifact SHA-256 verification, physical-isolation requirements, independent
  state checks, and an explicit ERASE authorization gate (the PO's `AUTHORIZE
  ERASE`).

Guarantees:

- The **core enforces safety internally** — an MCP permission alone is
  insufficient; the underlying operations must fail closed.
- **No ASSUME-OK** for unconfirmed destructive operations (the PICO-5B lesson).
- **Alternative access accounted for:** raw UDP or shell access can bypass MCP
  restrictions, so the safety invariants must hold in the core, not only at the
  MCP boundary.
- **Human authority gates** for live hardware, flash and recovery remain at the
  Product Owner (non-transitive, revision-bound).

Recommended next increment order: (1) OBSERVE read-only core on the existing
`--read-hr`/version-readback path with an independent read-only boundary test;
(2) TEST with a bounded protocol-experiment session model; (3) FLASH/RECOVERY
only after the fail-closed behaviour of the Python flash tooling is
independently verified (see the containment directive).

---

## 11. Independent review

See the Integrator review record for the independent verdict (§12 of the
directive: persistence, provenance, fact/hypothesis separation, read-only MCP,
agent discovery, reproducible ingestion, database ownership, bootloader
contracts, incident handling, no hardware access, no unauthorized firmware/C#
changes).
