# Siebwalde Documentation

This directory stores durable analysis and handoff knowledge for future human and AI development sessions.

## Documentation Index

| File | Purpose |
| --- | --- |
| `application-guide.md` | Human-readable guide to the application: system overview, structure, workflows, endpoints, and status. |
| `product.md` | Confirmed project purpose, current documentation scope, and separated open questions. |
| `analysis-coverage.md` | Investigation status by project and meaningful module. |
| `inventory.md` | Workspace inventory, project classification, dependencies, configuration, persistence, tests, and generated/vendor areas. |
| `architecture.md` | Existing architecture, boundaries, workflows, lifecycle, concurrency, and cross-component dependencies. |
| `implementation.md` | Implementation details, entry points, execution paths, protocol handling, diagnostics, and code-inspected risks. |
| `build-test.md` | Verified environment information, build/test commands, prerequisites, and test/build uncertainty. |
| `project-knowledge.md` | Durable knowledge summary for future sessions, with facts separated from assumptions and open questions. |
| `decisions.md` | Decisions made during this analysis/documentation assignment. |
| `backlog.md` | Unapproved follow-up work and improvement proposals. |
| `handoff.md` | Resume instructions and current progress. |
| `product-clarification.md` | Recorded product-clarification session prompt; not a product specification. |

## Firmware Documentation Outside This Directory

The `TrackAmplifier4.X/` project keeps its own maintained guidance. These files are referenced here and are not duplicated in `docs/`:

- `TrackAmplifier4.X/AGENT_TRACK_AMPLIFIER.md`
- `TrackAmplifier4.X/AGENT_TRACK_AMPLIFIER_COMPACT.md`
- `TrackAmplifier4.X/MODBUS_TRACK_AMPLIFIER_MAPPING.md`
- `TrackAmplifier4.X/TRACK_AMPLIFIER_STATE_MACHINE.md`

## Current Scope

The current assignment authorized analysis, documentation, and initial OpenCode agent setup only. No application source code, dependency files, or runtime configuration were intentionally changed. Firmware, PCB, and Python tooling are inventoried but not deeply analyzed.

## Workspace Location

The repository root is `C:\Localdata\Siebwalde` (Git repository). Earlier documentation was produced in a pre-migration copy; absolute-path references to that copy are superseded by this location.

## Agent Team

Project-specific OpenCode agent definitions were added under `.opencode/agents/`:

- `project-lead.md`, mode `primary`.
- `architect.md`, mode `subagent`.
- `developer.md`, mode `subagent`.
- `designer.md`, mode `subagent`.
- `integrator.md`, mode `subagent`.

OpenCode loads agent files at startup. Restart OpenCode from `C:\Localdata\Siebwalde` and select `project-lead` to use the new team.
