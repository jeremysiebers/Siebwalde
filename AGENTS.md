# Siebwalde AI Contributor Instructions

## Reading Order

Startup (current-reading):
1. `AGENTS.md` for shared rules, the project map, and the mandatory workflow reference.
2. `docs/development-workflow.md` for the normative Siebwalde Development Workflow v1.
3. `docs/current-state.md` for the compact verified current project snapshot.
4. `.opencode/workflow/active-state.json` if present, for the operational Active State Manifest of the active increment (checkpoint/resume state; local and git-ignored, not durable project truth).
5. Actual Git reality (`git branch --show-current`, `git rev-parse HEAD`, `git status`) before trusting any recorded state.
6. `docs/product.md` for confirmed project purpose, scope, and open questions.
7. `docs/README.md` for the documentation index.

Targeted / domain reference (read only as needed):
- `docs/koploper-interface.md`, `docs/architecture.md`, `docs/implementation.md`, `docs/inventory.md`, `docs/build-test.md`.

Historical / durable history (read only when relevant):
- `docs/handoff.md` (session history and resume steps), `docs/decisions.md`, `docs/backlog.md`, `docs/analysis-coverage.md`.

## Primary Agents

- `project-lead` — normal entry point for Siebwalde engineering increments; owns the Workflow v1 state machine and orchestration. It is configured as the default primary agent in `opencode.json`.
- `plan` — optional exploratory analysis and planning mode; not an active Workflow v1 development increment.
- `build` — optional deliberate direct OpenCode operation; not the normal Workflow v1 development path.

See `docs/development-workflow.md` for the full orchestration model.

## Project Map

The workspace root is a Git repository (`https://github.com/jeremysiebers/Siebwalde.git`) at `C:\Localdata\Siebwalde`. It is broader than the managed .NET application: it also contains microcontroller firmware, PCB hardware sources, Python tooling, runtime logs, and backup projects.

Managed .NET application (analysis focus):

- `SiebwaldeApp/` is the main solution root. `SiebwaldeApp/SiebwaldeApp.sln` includes the desktop UI, core library, and active ECoS emulator library.
- `SiebwaldeApp.Core.Host/` is a separate console host solution that references `SiebwaldeApp/SiebwaldeApp.Core`.
- `SiebwaldeApp.EcosEmu/` is a separate emulator host solution that references `SiebwaldeApp/SiebwaldeApp.EcosEmu` and also contains an unreferenced `SiebwaldeApp.EcosEmu_old` copy.

Embedded firmware and hardware (inventoried, not deeply analyzed):

- `TrackAmplifier4.X/` track amplifier firmware; contains its own agent guidance (`AGENT_TRACK_AMPLIFIER.md`, `AGENT_TRACK_AMPLIFIER_COMPACT.md`, `MODBUS_TRACK_AMPLIFIER_MAPPING.md`, `TRACK_AMPLIFIER_STATE_MACHINE.md`).
- `TrackAmplifierBootLoader.X/`, `TrackBackplane2.X/`, `TrackController5/`, `ServoController.X/`, `FiddleYard/`, `Faller_Car_ucontroller2.X/`, `Faller_Car_uControllerBootLoader.X/`, `YardController.X/` microcontroller firmware and tooling.
- `KiCad/` PCB designs; `Common_Files/` ModBus slave/master mappings; `Ecos ESU info/` vendor reference material; `Backup projects/` archived/backup firmware projects.

Supporting areas:

- `Logging/` runtime logs and `locos.json` persistence; `docs/` analysis and handoff documentation; `.opencode/` agent definitions and tooling.

See `docs/inventory.md` for the full inventory and `docs/analysis-coverage.md` for investigation status.

## Commands And Prerequisites

- Verified environment: `dotnet` SDK `9.0.318` with the .NET 8 and .NET 8 Windows Desktop runtimes on Windows (verified in `C:\Localdata\Siebwalde`).
- Build/test commands are documented in `docs/build-test.md` and are run routinely. The current verified baseline is Debug `482/482`, Release `482/482`, and the `SiebwaldeApp.StopReachabilityHarness` build `0 errors / 0 warnings` (see `docs/current-state.md`).
- The active test project is `SiebwaldeApp/SiebwaldeApp.Core.Tests/SiebwaldeApp.Core.Tests.csproj` (xUnit; 482 tests). The former `SiebwaldeApp/SiebwaldeApp.Tests` was removed on 2026-09-11 as an obsolete remnant of the abandoned station-in-C# approach; its design intent is archived in `docs/project-knowledge.md`.
- Firmware projects use Microchip MPLAB X / XC compilers and Python tooling. Do not build, flash, or connect to hardware without explicit authorization.

## Constraints

- Communicate with the user in Dutch. Write documentation and agent instructions in English.
- Do not modify application source code, dependency files, or runtime configuration unless the user explicitly authorizes implementation work.
- Keep proposed improvements in `docs/backlog.md` until approved.
- Support durable facts with repository-relative paths and exact symbol names.
- Distinguish verified facts, assumptions, proposals, and open questions.
- Do not document generated output or vendored packages line by line.
- Create new temporary Git worktrees/test repositories only under `C:\Localdata\Siebwalde_temp\` (never directly under `C:\Localdata` or inside the canonical repo). See `docs/development-workflow.md` §10.14 "Temporary repositories, Git worktrees, and workspace lifecycle" for the full convention, lifecycle, preservation, and exception rules.

## Knowledge Maintenance

- Update `docs/analysis-coverage.md` whenever investigation status changes.
- Update `docs/handoff.md` before ending a session or when a material uncertainty remains.
- Keep `AGENTS.md` concise and link to detailed docs instead of duplicating the knowledge base.
- The project-specific OpenCode agents live in `.opencode/agents/`. Restart OpenCode before relying on newly created or changed agent definitions.

## Knowledge Use (Knowledge MCP)

The project exposes a **read-only Knowledge MCP** (server `knowledge`, tools `knowledge_*`) over a persistent LadybugDB engineering-knowledge graph (see `docs/SIEBWALDE-KNOWLEDGE-AND-DIAGNOSTICS-ARCHITECTURE.md` and `tools/knowledge/README.md`). This section is the single, centralized knowledge-use contract; each role file adds only its role-specific obligations and points back here.

For work involving **firmware, hardware protocols, bootloader, PIC32, PIC18, backplane, C# initialization, recovery or safety-critical behavior**, retrieve relevant engineering knowledge through the Knowledge MCP *before* implementing or approving a change. Determine:

- the existing proven baseline;
- the relevant protocol and safety invariants;
- previous successful procedures;
- previous failed approaches;
- experimental versus physically-proven functionality;
- known contradictions;
- current unresolved uncertainties;
- the traceable source-evidence references.

Rules:

- The graph is a **structured evidence index**, not a source of truth. Git source, immutable firmware artifacts and raw test evidence remain authoritative.
- **Critical claims must be verified against original source, immutable artifacts or recorded hardware evidence** — never accept a graph summary as final proof.
- Never promote a hypothesis to a proven fact merely because it appears in the graph.
- Never interpret a firmware artifact's identity (a file path, build header, version string or checksum) as proof of deployed physical firmware integrity.
- If knowledge is unavailable or stale, **fall back to authoritative repository sources and explicitly report the limitation**. Do not let unavailable knowledge silently authorize a risky action.
- The Knowledge MCP is **read-only and advisory**; it is not a substitute for deterministic protocol and safety enforcement, and it grants no hardware authority.
- An agent must report **UNKNOWN** when the data does not support a conclusion; do not invent knowledge to fill a gap.

## Knowledge Capture

All agents MUST surface significant engineering findings — protocol contracts, firmware identities, safety invariants, proven or failed procedures, and corrections to earlier conclusions — to the Project Lead as they are identified or confirmed during an increment. Persistence follows the centralized capture workflow in `docs/development-workflow.md` §14.12 "Engineering Knowledge Capture and Knowledge Foundation Synchronization" (classify → duplicate check → provenance-bearing seed entry → deterministic rebuild → controlled operational sync → MCP readback). Role files add only role-specific obligations and MUST NOT duplicate this contract.

## Tool registry (reuse before re-creating)

Before creating new engineering tooling or replacing established tooling, search the tool registry `tools/tool-registry.yaml` (and the Knowledge MCP `knowledge_tools`) for an existing reusable component. Accepted Siebwalde MCP capabilities (`picoscope`, `knowledge`) and protected/historical tools are preserved by Product Owner policy: deletion, retirement or destructive replacement requires an explicit PO decision, and migration does not imply deletion. Reuse the existing implementation rather than duplicating firmware state machines, protocol parsers or instrument drivers.
