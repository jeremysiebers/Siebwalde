# Handoff

## Current Session Status

This session completed a documentation-only workspace migration check and started the product clarification phase for the Siebwalde repository at `C:\Localdata\Siebwalde` (Git repository, `origin https://github.com/jeremysiebers/Siebwalde.git`).

Completed:

- Verified repository boundaries and top-level structure; all `.sln` files and `ProjectReference` entries resolve.
- Corrected documentation references to the pre-migration directory.
- Created `docs/product.md` and captured the product owner's clarification answers in `human_input.md`.
- Recorded confirmed component roles, requirements, and priorities, plus open questions and assumptions.
- Marked prior .NET code-analysis findings as requiring revalidation.

No source code, generated files, dependencies, or Git history were modified. No builds were run and no hardware was connected.

## Files Updated

Migration check:

- `AGENTS.md`, `docs/README.md`, `docs/inventory.md`, `docs/project-knowledge.md`, `docs/analysis-coverage.md`, `docs/build-test.md`, `docs/architecture.md`, `docs/decisions.md`, `docs/backlog.md`, `docs/handoff.md`.

Product clarification:

- `human_input.md` - cleaned and structured product-owner input plus follow-up questions.
- `docs/product.md` - confirmed purpose, roles, requirements, priorities, open questions.
- `docs/decisions.md` - Koploper authority, TrackControllerPic18.X obsolete, Fiddle Yard deferred, C# cleanup first, iterative architecture.
- `docs/backlog.md` - confirmed priority order and new work items.
- `docs/analysis-coverage.md`, `docs/project-knowledge.md`, `docs/architecture.md` - confirmed component roles.

## Confirmed Component Roles

- Active PC application: `SiebwaldeApp` + `SiebwaldeApp.Core` + `SiebwaldeApp.EcosEmu`.
- `TrackController5` drives the 50 ModBus amplifiers and performs MMDC.
- All 50 amplifiers run `TrackAmplifier4.X` with `TrackAmplifierBootLoader.X`.
- `TrackBackplane2.X` is static backplane ModBus code.
- `TrackControllerPic18.X` is obsolete; C# remnants must be removed.
- `FiddleYard` C# control is deferred; embedded C is in `FiddleYard/`.
- `YardController_IOX.X` will be partly split into C# later.
- Koploper owns driving behavior via the ECoS emulator; C# translates commands and returns occupancy.

## Confirmed Priorities

1. C# cleanup and correctness (first increment): revalidate findings, restore core/IoC separation, remove obsolete remnants, tests/simulation. No firmware changes.
2. Create `docs/application-guide.md`.
3. Koploper translation path on the 4-amplifier test layout, with clearer initialization.
4. Firmware: TrackAmplifier4.X setpoints/parameters/MMDC and TrackController5 MMDC.
5. Fiddle Yard and YardController later.

## Product Clarification Progress

Phase 0 is in progress. Round 1 (2026-09-11) confirmed:

- Refined first-increment scope: C# cleanup and correctness plus the program startup/initialization story; Fiddle Yard stays functional but unchanged; obsolete remnants are inventoried and marked (not yet removed).
- `SiebwaldeInitPage` as the startup page with automatic host detection (FiddleYard, Ethernet ModBus master, Koploper, later YardController), dynamic display, human-readable page logging, and start buttons.
- All hard-coded values become editable/persisted via a new menu -> settings option.
- Engineering standards: unit tests for the window/program model, standard coding standards, and a simulation fallback for undetected hosts (FiddleYard generator now; Koploper simulator from the existing ECoS emulator).
- Fiddle Yard logic/behavior/visualization deferred but must keep working.

Round 2 (2026-09-11) confirmed:

- Host detection for now: ping on host name (FiddleYard, ModBus master, YardController) and TCP connect probe (Koploper). A single uniform detection layer is planned later after C-code changes.
- Settings: belong in the core, use `app.config` if appropriate, with a default button per entity and undo (Ctrl-Z).

Round 3 (2026-09-11) confirmed:

- The Koploper/ECoS protocol and port roles are derived from `Ecos ESU info`, the `SiebwaldeApp.EcosEmu` source, and working test examples, not from the product owner. Koploper connects via its own "make connection" button.
- Locomotive location comes from Koploper via the dedicated port. The block-to-amplifier topology is a user-definable `app.config` input.
- C# must pre-command the next block's amplifier when the current block is free (look-ahead).

Round 4 (2026-09-11) confirmed:

- MMDC split: TrackAmplifier4.X does hardware protection and hiccup; TrackController5 monitors PC/amplifier communication and broadcasts emergency stop; C# handles communication monitoring, alarm/logging, and recovery (possibly manual override).
- Koploper emergency button is a software emergency stop forwarded by broadcast. How amplifier errors reach Koploper still needs ECoS datasheet research.
- The Yard stays hand-operated with main-line <-> Yard handover commands. Faller Car via Koploper is an experiment, otherwise a dedicated C# solution.
- A shuttle line (pendelbaan) exists (max 4, min 1 locomotive between 3 stations); its control ownership is undecided.
- YardController is not yet an init-page host.

Round 5 (2026-09-11) confirmed:

- Test layout: 4 amplifiers; IDs may be in a spreadsheet (candidates under `Backup projects/TrackControllerPic18.X/Doc/`, not yet parsed); no fixed loco coupling; block topology is on paper only.
- Shuttle line: no fixed coupling to other elements; two ModBus amplifiers, with the middle station switching the switch street between them via switches/relays.
- A designer agent (UI WPF/WinForms, panels, Fiddle Yard visu, layout diagnostics/manual override) and an integrator/test agent are needed. Creation is deferred until the clarification phase is closed.

Clarification rounds 1-5 are complete and the phase is closed. The requested agents were created:

- `.opencode/agents/designer.md` (subagent).
- `.opencode/agents/integrator.md` (subagent).
- `project-lead.md` delegation section updated.

Development happens on branch `feature/csharp-cleanup-startup`; a pull request to the default branch happens only after a first successful integration with the agents. Restart OpenCode to load the new agents.

## Revalidation Status

Prior .NET findings were produced before the workspace was confirmed as the full Git repository and were not re-checked against current source. The following require revalidation:

- Missing `IoC.Kernel` in `SiebwaldeApp.Core.IoC` used by `SiebwaldeApp.Core.Host`.
- Missing station-domain symbols referenced by `SiebwaldeApp.Tests`.
- Initialization step sequencing (`SetDefaultPwmSetpointsStep`).
- Hard-coded endpoints and firmware path authority.
- Any build/test success or failure claims.

## Resume Instructions

1. Restart OpenCode from `C:\Localdata\Siebwalde` and select the `project-lead` agent.
2. Continue from `docs/product.md`, `human_input.md`, and `docs/analysis-coverage.md`.
3. Confirm the first increment scope with the product owner, then revalidate the .NET findings against current source.
4. Create `docs/application-guide.md`.
5. Answer the follow-up questions in `human_input.md` before designing the Koploper translation path.
6. Keep communicating with the user in Dutch; keep documentation and agent instructions in English.

## Constraints To Preserve

- Current authorization covers analysis and documentation only.
- Do not modify application source code, dependency files, generated files, or runtime configuration.
- Do not build, flash, or connect to hardware without explicit user authorization.
- Do not modify Git history.
- Record improvements separately as unapproved follow-up work.
- Support durable knowledge with repository-relative paths and exact symbol names.
- Distinguish verified facts, assumptions, proposals, and open questions.

## Documentation Ownership Plan

- Project Lead owns `AGENTS.md`, `docs/README.md`, `docs/product.md`, `human_input.md`, `docs/analysis-coverage.md`, `docs/inventory.md`, `docs/project-knowledge.md`, `docs/decisions.md`, `docs/backlog.md`, and `docs/handoff.md`.
- Architect owns `docs/architecture.md` and reviews implementation summaries for architectural consistency.
- Developer owns `docs/implementation.md` and `docs/build-test.md` and verifies important architectural claims against code.
- Designer owns UI design documentation and (when authorized) WPF/WinForms implementation files.
- Integrator owns test/simulation strategy documentation and (when authorized) test projects and harnesses.

## Remaining Work

1. Create `docs/application-guide.md` (still missing).
2. Revalidate prior .NET code-analysis findings against current source.
3. Continue product clarification: round 2 questions (host detection technique, settings scope, then Koploper protocol/mapping, MMDC split, test layout, YardController split, designer agent).
4. Design and implement the Koploper translation path (later increment).
5. Investigate the additional firmware/hardware/Python source areas in bounded passes.
6. Decide whether to add a designer agent. - DONE (2026-09-11): designer and integrator agents created.
7. Treat all `docs/backlog.md` items as unapproved until the user selects implementation work.
