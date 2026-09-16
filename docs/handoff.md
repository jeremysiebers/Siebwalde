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

## Phase 1 Revalidation Results (2026-09-11)

Prior .NET findings were revalidated against current source by inspection (no build). All confirmed:

- CONFIRMED: `SiebwaldeApp.Core.Host/Program.cs:25` uses `IoC.Kernel`; `SiebwaldeApp.Core.IoC` has only `Logger`/`ConfigureLogger`. Compile error.
- CONFIRMED: `SiebwaldeApp.Tests` uses `IoC.Kernel`/Ninject and references undefined station-domain symbols; it cannot compile.
- CONFIRMED: `SetDefaultPwmSetpointsStep` is skipped by `InitTrackamplifiersStep` and returns `Next("EnableTrackamplifiersStep")`, which does not match the registered `EnableTrackamplifiers` and would fail initialization.
- CONFIRMED: endpoints (`192.168.1.193`, `10000`, `10001`) and the firmware path are hard-coded in both `SiebwaldeApplicationModel.cs` and `Core.Host/Program.cs`; `CoreSettings` is not authoritative for the track transport.
- CONFIRMED: Pic18-era commented remnants in `App.xaml.cs` and station-era UI remnants (`StationSettingsPage`, `StationSettingsPageViewModel`, `ApplicationPage.StationSettings`, `SideMenuViewModel.StationSettingsPage`). Active `TrackControllerCommands` (ModBus) is not a remnant.

Not re-verified: Fiddle Yard error paths, `SendNextFwDataPacket` await behavior, `TrackClientAsync` publish-interval comment, ECoS multi-client behavior. No build/test was executed.

## Increment 1 Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- `SiebwaldeApp.Core.Host/Program.cs`: replaced `IoC.Kernel.Bind<ILogFactory>()` with `IoC.ConfigureLogger(...)`.
- `InitTrackamplifiersStep`: now returns `Next("SetDefaultPwmSetpoints")`.
- `SetDefaultPwmSetpointsStep`: now returns `Next("EnableTrackamplifiers")`.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.Core`: 0 errors.
- `SiebwaldeApp.Core.Host`: 1 error before, 0 errors after.
- `SiebwaldeApp.sln`: 0 errors.
- `SiebwaldeApp.EcosEmu.sln`: 0 errors.

Step-name chain verified by source inspection. No Fiddle Yard source changed. `SiebwaldeApp.Tests` was not built (it was obsolete and has since been removed in Increment 2).

## Increment 2 Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Removed `SiebwaldeApp/SiebwaldeApp.Tests` (8 tracked files) as an obsolete remnant of the abandoned station-in-C# approach, per product-owner option 1 (archive/remove).
- Removed the leftover generated `bin/`/`obj/` folder of the deleted project.
- Archived the encoded station design intent in `docs/project-knowledge.md`; source recoverable from git commit `104c1e6`.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.sln`: 0 errors after removal.
- `SiebwaldeApp.Core.Host`: 0 errors after removal.

No active project referenced the test project, so builds were unaffected.

## Increment 3 Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Removed the dead station-policy settings feature: `StationSettingsPage.xaml(.cs)`, `StationSettingsPageViewModel.cs`, `ApplicationPage.StationSettings`, the `ApplicationPageValueConverter` case, the `SideMenuViewModel.StationSettingsPage` command, and the `TrackMenu.xaml` menu button.
- Removed the commented Pic18-era block from `App.xaml.cs` (and the two fully-commented amplifier view files `TrackAmplifierItemView.xaml.cs` and `TrackAmplifierItemViewModel.cs`).
- Fixed the stale `StartTrackApplication` XML doc.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.sln`: 0 errors.
- No remaining references to `StationSettingsPage`, `ApplicationPage.StationSettings`, `StationPolicy`, `TrackPic18UdpAdapter`, `YardPic18UdpAdapter`, or `TrackAmplifierItemViewModel` in the WPF app source.

Fiddle Yard and the active ModBus `TrackControllerCommands` were not touched. The Page-Removed legacy XAML leftovers (`TrackAmplifierItemView.xaml`, `TrackAmplifierManualControlView.xaml`, `TrackControlView.xaml`) remain and are tracked in the backlog.

## Resume Instructions

1. Restart OpenCode from `C:\Localdata\Siebwalde` and select the `project-lead` agent.
2. Continue from `docs/product.md`, `human_input.md`, and `docs/analysis-coverage.md`.
3. Increments 1-3 are implemented and verified; propose the next increment (configuration authority/settings UI, or the application guide) for approval.
4. Create `docs/application-guide.md`.
5. Product clarification rounds 1-5 are complete; remaining items are research tasks (Koploper protocol, ECoS overload semantics, topology/spreadsheet).
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
2. Revalidate prior .NET code-analysis findings against current source. - DONE (2026-09-11): confirmed; see Phase 1 Revalidation Results.
3. Product clarification rounds 1-5. - DONE (2026-09-11): remaining items are research tasks (Koploper protocol, ECoS overload semantics, topology/spreadsheet).
4. Design and implement the Koploper translation path (later increment).
5. Investigate the additional firmware/hardware/Python source areas in bounded passes.
6. Designer and integrator agents. - DONE (2026-09-11): created.
7. Propose the first fix increment for product-owner approval (Core.Host logger, init sequencing, remnant cleanup, test-project decision). - DONE (2026-09-11): Increment 1 (host logger + init sequencing) implemented and verified.
8. Remove obsolete test project. - DONE (2026-09-11): Increment 2 removed `SiebwaldeApp.Tests`; design intent archived.
9. Increment 3 (proposed): remove confirmed Pic18-era and station-era remnants from the WPF app. - DONE (2026-09-11): station-policy feature and commented Pic18 code removed; build verified.
10. Increment 4 (candidate): configuration authority / settings UI; or create `docs/application-guide.md`.
11. Treat all remaining `docs/backlog.md` items as unapproved until the user selects implementation work.
