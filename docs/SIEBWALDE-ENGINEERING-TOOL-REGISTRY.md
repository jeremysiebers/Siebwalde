# Siebwalde Engineering Tool Registry and Preservation Policy

Date: 2026-10-23. This document records the Product Owner's engineering-tool
preservation policy (K4 §4) and the machine-readable tool registry.

## 1. Preservation policy (PO decisions)

**A. Accepted Siebwalde MCP components** are automatically **KEEP**. All accepted
and operational MCP capabilities must be preserved through future development.
Routine maintenance, bug fixes and compatible extensions are permitted under
workflow authorization; deletion, retirement, destructive replacement or loss of
a proven capability requires an explicit PO decision.

**B. Other valuable engineering tools** — the Project Lead may discover, inspect,
test, document and propose migration, but must consult the PO before deleting,
retiring or destructively replacing a valuable tool. Uncertain value defaults to
`PRESERVE_PENDING_REVIEW`.

**C. Migration does not imply deletion.** Moving functionality into the future
Siebwalde Diagnostics MCP does not authorize removing the original; the original
stays until functional equivalence is demonstrated, independently reviewed and
explicitly accepted for retirement by the PO.

**D. Historical tools and evidence** that produced accepted hardware evidence are
preserved for reproducibility and fallback, even when a newer tool is preferred.

## 2. Registry

The machine-readable registry is `tools/tool-registry.yaml` (format v1). Each
record carries factual technical properties (path, language, entry points,
capabilities, side-effect class) and, separately, the PO policy
(`preservation_policy`, `po_decision`). Lifecycle statuses: `ACTIVE`,
`PROTECTED`, `HISTORICAL`, `MAINTENANCE_ONLY`, `EXPERIMENTAL`. Preservation
policies: `KEEP_ACTIVE_MCP`, `KEEP_PROTECTED`, `KEEP_HISTORICAL`,
`PRESERVE_PENDING_REVIEW`. Validation is never invented: `offline_tested` /
`physically_proven` / `source_supported` reflect recorded evidence only.

### Registered tools

| id | path | lifecycle | policy | offline | physically-proven |
|---|---|---|---|---|---|
| `picoscope-mcp` | tools/picoscope/ | ACTIVE | KEEP_ACTIVE_MCP | yes | yes |
| `knowledge-foundation` | tools/knowledge/ | ACTIVE | KEEP_ACTIVE_MCP | yes | no (offline) |
| `firmware-tooling` | tools/firmware/ | PROTECTED | KEEP_PROTECTED | yes | no |
| `hmo2024-tooling` | tools/hmo2024/ | HISTORICAL | KEEP_HISTORICAL | yes | yes |
| `flash-one-driver` | TrackController5/Python/flash_one.py | PROTECTED | KEEP_PROTECTED | yes | no (experimental) |
| `legacy-python-cli` | TrackController5/Python/ | PROTECTED | KEEP_PROTECTED | yes | no |
| `faller-car-bootload` | Faller_Car_uControllerBootLoader.X/Python/bootload.py | HISTORICAL | PRESERVE_PENDING_REVIEW | no | no |
| `integrator-checksum-helper` | tmp/integrator_checksum.py | HISTORICAL | PRESERVE_PENDING_REVIEW | no | no |

Additional engineering tooling inventoried but not individually promoted to
registry records (they are part of firmware projects or generated artifacts):
the PIC32/PIC18 firmware projects themselves (`TrackController5/`,
`TrackAmplifier4.X/`, `TrackAmplifierBootLoader.X/`, backplane/controller
projects), the C# test harness + deterministic simulator
(`SiebwaldeApp.Core.Tests`, `SiebwaldeApp.StopReachabilityHarness`,
`DeterministicTrackTransport`), and archived firmware under `Backup projects/`.
These remain preserved as part of the repository and are not deletion targets.

## 3. Preservation guard

`tools/preservation_guard.py` is a read-only, offline-checkable guard. It parses
the registry and asserts, against the actual repository:

- every `KEEP*` tool root `path` exists on disk;
- the two accepted MCP components (`picoscope`, `knowledge`) remain registered
  and enabled in `opencode.json`.

It exits non-zero with a machine-readable violation line
(`MISSING_TOOL_ROOT <id> <path>` / `MCP_NOT_REGISTERED <name>` /
`REGISTRY_UNPARSEABLE`) when a protected root is missing or an MCP is
unregistered. It never deletes, moves or modifies any tool. Routine source
modifications and reviewed bug fixes remain possible — the guard blocks only
unauthorized *removal/retirement* of protected roots, not normal edits.

### Deprecation requires

1. explicit identification of the affected tool;
2. evidence of replacement suitability;
3. impact analysis;
4. independent review;
5. an explicit PO disposition.

## 4. Knowledge Foundation integration

Tool knowledge is ingested deterministically into the LadybugDB graph (new
additive node/relationship tables: `Tool`, `Capability`, `Dependency`,
`TestEvidence`, `HardwareInterface`, `MCPService`, `MigrationDecision`, and
`PROVIDES_CAPABILITY` / `DEPENDS_ON` / `CONNECTS_TO` / `VALIDATED_BY` /
`EXPOSED_VIA_MCP` / `REUSES` / `MIGRATION_CANDIDATE_FOR` /
`PRESERVED_BY_POLICY`). The read-only Knowledge MCP gains a `knowledge_tools`
tool. No undocumented assumption becomes `PROVEN_HARDWARE`; large captures and
native binaries stay out of the graph.
