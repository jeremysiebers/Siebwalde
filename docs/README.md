# Siebwalde Documentation

This directory stores durable analysis and handoff knowledge for future human and AI development sessions.

## Documentation Index

### Current operational

| File | Purpose |
| --- | --- |
| `development-workflow.md` | Normative Siebwalde Development Workflow v1: development, review, validation, evidence, Git, resume and agent-orchestration workflow. |
| `current-state.md` | Compact verified current project snapshot for fast startup by future human and AI sessions. |
| `backlog.md` | Unapproved follow-up work and improvement proposals. |

The operational Active State Manifest for the active increment is local and git-ignored at `.opencode/workflow/active-state.json` (template and validator under `.opencode/workflow/`). It is intentionally not part of this docs index.

### Domain / durable reference

| File | Purpose |
| --- | --- |
| `product.md` | Confirmed project purpose, current documentation scope, and separated open questions. |
| `application-guide.md` | Human-readable guide to the application: system overview, structure, workflows, endpoints, and status. |
| `koploper-interface.md` | Verified Koploper/ECoS interface: ports, commands, position records, and the translation-layer design. |
| `architecture.md` | Existing architecture, boundaries, workflows, lifecycle, concurrency, and cross-component dependencies. |
| `implementation.md` | Implementation details, entry points, execution paths, protocol handling, diagnostics, and code-inspected risks. |
| `inventory.md` | Workspace inventory, project classification, dependencies, configuration, persistence, tests, and generated/vendor areas. |
| `build-test.md` | Verified environment information, build/test commands, prerequisites, and test/build uncertainty. |
| `analysis-coverage.md` | Investigation status by project and meaningful module. |
| `project-knowledge.md` | Durable/historical supporting knowledge; `current-state.md` is the compact current snapshot. |

### Historical

| File | Purpose |
| --- | --- |
| `handoff.md` | Historical session history and resume steps; superseded for current resume by the Active State Manifest. |
| `decisions.md` | Decisions made during the earlier analysis/documentation assignment. |
| `product-clarification.md` | Recorded product-clarification session prompt; not a product specification. |

## Firmware Documentation Outside This Directory

The `TrackAmplifier4.X/` project keeps its own maintained guidance. These files are referenced here and are not duplicated in `docs/`:

- `TrackAmplifier4.X/AGENT_TRACK_AMPLIFIER.md`
- `TrackAmplifier4.X/AGENT_TRACK_AMPLIFIER_COMPACT.md`
- `TrackAmplifier4.X/MODBUS_TRACK_AMPLIFIER_MAPPING.md`
- `TrackAmplifier4.X/TRACK_AMPLIFIER_STATE_MACHINE.md`

## Current Scope

This documentation set started as analysis, documentation and OpenCode agent setup, and has since implemented the C# cleanup and the Increment 6 Koploper/ECoS translation layer (speed/PWM, routing and look-ahead, occupancy, switch mapping, ECoS host lifecycle, divergence/safety/diagnostics), followed by the merged safety-stop reachability increment and the production `ControlTrace`. Application source and configuration in `SiebwaldeApp/` were changed as part of that work; firmware, PCB and Python tooling are inventoried but not deeply analyzed and have not been modified. The most recent work is governance: Siebwalde Development Workflow v1 (Project Lead orchestration, migrated role contracts, normalized permissions, and an Active State checkpoint/resume mechanism) is implemented and **merged to `master`** (PR #5). For the current verified snapshot see `docs/current-state.md`.

For the current state see `docs/current-state.md`; for open work see `docs/backlog.md`, categorised into software follow-up, firmware dependency, physical hardware dependency, and configuration/user-input dependency. `docs/handoff.md` is historical.

## Workspace Location

The repository root is `C:\Localdata\Siebwalde` (Git repository). Earlier documentation was produced in a pre-migration copy; absolute-path references to that copy are superseded by this location.

## Agent Team And Workflow

The project runs Siebwalde Development Workflow v1 (`docs/development-workflow.md`). The human is the Product Owner; the AI Project Lead owns orchestration and may route bounded work to the registered roles.

- `.opencode/agents/project-lead.md`, mode `primary` - Workflow v1 Project Lead (workflow state, routing, evidence).
- `.opencode/agents/developer.md`, mode `subagent` - implementation, tests, build, software self-verification.
- `.opencode/agents/architect.md`, mode `subagent` - architecture/state/lifecycle/interface reasoning.
- `.opencode/agents/integrator.md`, mode `subagent` - independent review and validation.
- `.opencode/agents/designer.md`, mode `subagent` - conditional UI/UX work.

Project permissions are normalized in `opencode.json`. The operational Active State Manifest is `.opencode/workflow/active-state.json` (local, git-ignored). Restart OpenCode from `C:\Localdata\Siebwalde` and select `project-lead`.
