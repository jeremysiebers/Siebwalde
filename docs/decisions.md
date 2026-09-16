# Decisions

## 2026-09-10: Documentation-Only Scope Preserved

Decision: The assignment was treated as analysis, documentation, and initial OpenCode agent setup only.

Evidence: User explicitly prohibited modifying application source code, dependencies, or runtime configuration.

Impact: Code-inspected issues were recorded in documentation and `docs/backlog.md`; no implementation fixes were made.

## 2026-09-10: Omit Explicit Agent Model IDs

Decision: The new OpenCode agent definitions omit `model` in YAML frontmatter.

Evidence: The user requested use of the configured model and no existing `opencode.json` or `opencode.jsonc` was found in the project.

Impact: Agents inherit the configured OpenCode model instead of hard-coding a provider/model ID.

## 2026-09-10: Named Agents Require Restart

Decision: Do not claim the newly created `project-lead`, `architect`, or `developer` agents ran in the current session.

Evidence: OpenCode loads agent files at startup and no pre-existing `.opencode` agent definitions were present before this assignment.

Impact: `docs/handoff.md` contains exact restart and resume instructions.

## 2026-09-10: Generated And Vendored Content Excluded From Deep Analysis

Decision: `bin/`, `obj/`, `.vs/`, and vendored package directories are documented by role only.

Evidence: These areas contain generated build/IDE output or third-party package artifacts.

Impact: Analysis focuses on maintained source and project configuration.

## 2026-09-10: Full Build/Test Not Executed

Decision: Full `dotnet build` and normal `dotnet test` commands were not executed during this assignment.

Evidence: They can update generated `bin/` and `obj/` outputs, while the assignment is documentation-only.

Impact: Build/test commands are documented as proposed. A non-building `dotnet test --no-build --no-restore` check was performed and recorded.

## 2026-09-11: Workspace Migration To The Git Repository Root

Decision: The active repository root is `C:\Localdata\Siebwalde` (Git repository `https://github.com/jeremysiebers/Siebwalde.git`). The earlier pre-migration copy at `C:\Users\jerem\Downloads\Test` is superseded.

Evidence: The current root is a Git repository containing the managed .NET application plus firmware, hardware, Python tooling, logs, and backup projects. All discovered `.sln` files and `ProjectReference` entries are relative and resolve correctly from the current root.

Impact: Documentation absolute-path references to the pre-migration directory were updated. The workspace is broader than the managed .NET application, so the repository map, inventory, and coverage now include the additional source areas at inventory level only.

## 2026-09-11: Existing Code-Analysis Findings Require Revalidation

Decision: Prior .NET code-analysis findings are marked as requiring revalidation against the current source instead of being treated as confirmed.

Evidence: Those findings were produced before the workspace was confirmed as the full Git repository, and no build or test was run during the migration check.

Impact: `docs/analysis-coverage.md`, `docs/inventory.md`, `docs/build-test.md`, `docs/architecture.md`, and `docs/project-knowledge.md` carry revalidation notes. No findings were declared false or verified.

## 2026-09-11: Migration Check Is Documentation-Only

Decision: The migration check changed documentation only and did not modify source code, generated files, dependencies, or Git history, and did not run builds or connect to hardware.

Evidence: User instruction for this task.

Impact: Generated MPLAB output that still contains historical absolute paths was intentionally left unchanged.

## 2026-09-11: Koploper Is The Driving-Behavior Authority

Decision: Driving behavior is configured in Koploper. The ECoS/ESU emulator (`SiebwaldeApp.EcosEmu`) is used to present the layout to Koploper, and C# translates Koploper commands into amplifier setpoints.

Evidence: Product owner statement during the clarification session. Koploper knows which locomotive is where and returns locomotive-location information via a dedicated port.

Impact: C# is a translation and feedback layer between Koploper and the track hardware. Setpoint control must follow the locomotive. Occupancy flows amplifier -> C# -> Koploper.

## 2026-09-11: TrackControllerPic18.X Is Obsolete

Decision: `TrackControllerPic18.X` (in `Backup projects/`) is obsolete, and its remnants must be removed from the C# project.

Evidence: Product owner statement; its intended role was replaced by the ECoS emulator plus Koploper driving behavior.

Impact: Removal is unapproved implementation work and is tracked in `docs/backlog.md`; no code was removed during this phase.

## 2026-09-11: Fiddle Yard Deferred

Decision: The current C# Fiddle Yard initialization and control is left unchanged for now. The embedded C in `FiddleYard/` is not part of the first increment.

Evidence: Product owner statement.

Impact: Cleanup explicitly excludes the Fiddle Yard. Later, switch-street commands from Koploper must be translated to Fiddle Yard TOP/BOTTOM shift commands.

## 2026-09-11: C# Cleanup Is The First Increment

Decision: The first development increment is C# cleanup and correctness, not firmware work.

Evidence: Product owner statement that the C# side can be brought in order first (modern standards, unit testing, simulations).

Impact: Priorities recorded in `docs/product.md`. Revalidation of the prior .NET findings, restoring core/IoC separation, and removing obsolete remnants come before TrackAmplifier4.X/TrackController5 work.

## 2026-09-11: Architecture Evolves Iteratively With The Product Owner

Decision: The architecture will be developed through human-AI interaction, with questions and iteration, and improved continuously.

Evidence: Product owner statement.

Impact: Clarification questions are maintained in `human_input.md` and `docs/product.md`. Product knowledge is persisted incrementally rather than finalized in one pass.

## 2026-09-11: SiebwaldeInitPage Is The Startup And Host-Detection Page

Decision: On application start, `SiebwaldeInitPage` loads and automatically detects the expected hosts, displays their presence dynamically, and shows human-readable step/state logging. Detailed logging goes to log files.

Evidence: Product owner Round 1 answer.

Impact: Expected hosts are FiddleYard, Ethernet ModBus master (TrackController5), Koploper, and later YardController. Host functionality is started by an operator via a start button during the development phase.

## 2026-09-11: Hard-Coded Values Move To A Settings UI

Decision: All hard-coded values become editable and persisted through a new menu -> settings option that matches the current test/program files.

Evidence: Product owner Round 1 answer.

Impact: The exact settings set and storage location remain open questions. This supersedes the earlier open question about configuration authority.

## 2026-09-11: Simulation Fallback For Undetected Hosts

Decision: When a host is not detected, it must be possible to start it in simulation mode.

Evidence: Product owner Round 1 answer.

Impact: The existing FiddleYard data-generator simulation is reused/adapted later. A Koploper simulator should be built on the existing C# ECoS emulator, with manual user control of trains and commands.

## 2026-09-11: Fiddle Yard Must Keep Working During Cleanup

Decision: Fiddle Yard logic, behavior, and visualization are out of scope for the first increment but must remain functional. IoC improvements may later be applied there, with behavior validation.

Evidence: Product owner Round 1 answer.

Impact: Cleanup must not break the Fiddle Yard. A validation step is required when IoC changes touch it.

## 2026-09-11: Initial Host Detection Uses Ping And TCP Connect

Decision: For now, detect FiddleYard, the Ethernet ModBus master, and YardController with a ping on host name, and detect Koploper with a TCP connect probe. A single uniform detection layer is planned later, after the C code is adapted.

Evidence: Product owner Round 2 answer.

Impact: Detection is intentionally simple for the first increment. The uniform detection layer is future work and depends on firmware changes.

## 2026-09-11: Configuration Settings Live In Core And Use app.config

Decision: Configuration settings belong in the core (not the visualization). `app.config` is used if it is an appropriate method. The settings UI must offer a default button per entity and an undo (for example Ctrl-Z).

Evidence: Product owner Round 2 answer.

Impact: The settings UI is a thin layer over a core configuration service. The exact value list and undo scope remain open. Feasibility of `app.config` for persisted user settings still requires a design check.

## 2026-09-11: Koploper Protocol Is Derived From Code And Data Files

Decision: The exact Koploper/ECoS protocol and port roles are derived from `Ecos ESU info`, the `SiebwaldeApp.EcosEmu` source, and working test examples. They are not supplied by the product owner.

Evidence: Product owner Round 3 answer.

Impact: Protocol verification becomes a bounded investigation task. The emulator is the integration point that Koploper connects to.

## 2026-09-11: Block Topology Is User-Configurable

Decision: The block-to-amplifier chaining (for example block 1 = amplifier 1 -> block 2 = amplifier 2) is a user-definable configuration input in `app.config`. Locomotive location comes from Koploper via the dedicated port.

Evidence: Product owner Round 3 answer.

Impact: The C# layer maps Koploper block/location data to amplifiers using this configuration. Determining the topology may also require analysis of the layout.

## 2026-09-11: Look-Ahead Amplifier Commanding

Decision: When a block is free, C# must already command the next block's amplifier so the locomotive can cross smoothly.

Evidence: Product owner Round 3 answer.

Impact: The translation layer needs block adjacency information and a look-ahead step in addition to the current-block command.

## 2026-09-11: MMDC Responsibility Split

Decision: TrackAmplifier4.X performs hardware protection (current/temperature cutoff, hiccup auto-restart, PWM setpoint monitoring) and reports errors; TrackController5 monitors PC communication (emergency-stop broadcast) and amplifier communication; the C# layer handles communication monitoring, alarm/logging, and recovery, possibly with manual override.

Evidence: Product owner Round 4 answer.

Impact: Fail-safe protection stays in the embedded layer. How amplifier errors are surfaced to Koploper still requires research into ECoS overload/stop semantics.

## 2026-09-11: Emergency Stop Uses A Broadcast To All Track Amps

Decision: Loss of PC communication triggers an emergency stop via broadcast to all track amps. Koploper's emergency button is a software emergency-stop command forwarded the same way.

Evidence: Product owner Round 4 answer.

Impact: TrackController5 owns the broadcast; C# relays the Koploper emergency command.

## 2026-09-11: Yard Handover Between Manual Operation And Koploper

Decision: The Yard stays hand-operated. Main line -> Yard handover is requested by the operator, read by C#, and passed to Koploper, which shunts a train to a dead-end track; the operator then takes control via RC. Yard -> main line handover parks a locomotive on a block and hands it to the C#/Koploper-known amplifier, with the operator indicating the locomotive in Koploper.

Evidence: Product owner Round 4 answer.

Impact: C# needs handover commands/occupancy actions toward Koploper and switch control.

## 2026-09-11: Faller Car Via Koploper Is An Experiment

Decision: Driving the Faller Car System from Koploper is an experiment to test configuration benefits. If it does not pay off, a dedicated C# solution is built.

Evidence: Product owner Round 4 answer.

Impact: Faller Car work is exploratory and not committed scope.

## 2026-09-11: Shuttle Line Is A Separate Future Capability

Decision: A shuttle line (pendelbaan) exists with a maximum of 4 and a minimum of 1 locomotive between 3 stations (2 end stations with a switch, 1 middle station with fixed direction). Its control approach is not yet decided.

Evidence: Product owner Round 4 answer.

Impact: Recorded as a future capability; control ownership (Koploper vs C#) remains open.

## 2026-09-11: YardController Is Not Yet An Init-Page Host

Decision: YardController does not need to be an expected host on the init page yet; it comes later.

Evidence: Product owner Round 4 answer.

Impact: The first increment host-detection list is FiddleYard, Ethernet ModBus master, and Koploper.

## 2026-09-11: Shuttle Line Uses Two ModBus Amplifiers

Decision: The shuttle line has no fixed coupling to other layout elements. Two ModBus amplifiers are assigned to it, and the middle station switches the switch street between them using switches and relays. Block boundaries and chaining exist on paper only.

Evidence: Product owner Round 5 answer.

Impact: The shuttle line needs its own block topology definition. Amplifier IDs may be in candidate spreadsheets under `Backup projects/TrackControllerPic18.X/Doc/`.

## 2026-09-11: Designer And Integrator/Test Agents Are Needed

Decision: A designer agent is needed for UI (WPF/WinForms) visual design, panels, Fiddle Yard visualization, and a possible whole-layout diagnostics/manual-override view. An integrator/test agent is also considered necessary.

Evidence: Product owner Round 5 answer.

Impact: Agent definitions must not be changed during the clarification phase (`docs/product-clarification.md`). Creation is deferred until the phase is closed and the roles are agreed.

## 2026-09-11: Designer And Integrator Agents Created

Decision: The clarification phase is closed and the two requested agents are created: `designer` (UI WPF/WinForms design, panels, Fiddle Yard visualization, layout diagnostics/manual override) and `integrator` (unit/integration tests, simulation harnesses, host-detection and end-to-end verification). `project-lead.md` now lists both.

Evidence: Product owner approval.

Impact: Both are `subagent` mode with `edit: ask` and `bash: ask`. OpenCode must be restarted before they are available.

## 2026-09-11: Development Happens On A Feature Branch

Decision: All code changes and commits are made on the branch `feature/csharp-cleanup-startup`. A pull request to the default branch happens only after a first successful integration/adaptation/coupling with the agents.

Evidence: Product owner instruction.

Impact: `master` stays stable. The pre-existing branch `SiebwaldeApp-stationcontrol` is unrelated and left untouched.

## 2026-09-11: Prior .NET Findings Revalidated And Confirmed

Decision: The prior .NET findings were revalidated against current source by inspection (no build). Confirmed: missing `IoC.Kernel` in `SiebwaldeApp.Core.IoC` used by `SiebwaldeApp.Core.Host`; missing station-domain symbols and Ninject/`IoC.Kernel` usage in `SiebwaldeApp.Tests`; `SetDefaultPwmSetpointsStep` skipped and returning a mismatched next-step name; hard-coded endpoints/firmware path with `CoreSettings` not authoritative for the track transport; Pic18-era commented remnants and station-era UI remnants.

Evidence: Source inspection on 2026-09-11 in the active repository (see `docs/build-test.md`).

Impact: These move from "requires revalidation" to "confirmed" in the documentation. Fixes remain unapproved implementation work. Still not re-verified: Fiddle Yard error paths, `SendNextFwDataPacket` await behavior, `TrackCommClientAsync` publish comment, ECoS multi-client behavior.

## 2026-09-11: Increment 1 Implemented And Verified

Decision: Implemented the first fix increment on `feature/csharp-cleanup-startup`: (1) `SiebwaldeApp.Core.Host/Program.cs` now uses `IoC.ConfigureLogger(...)`; (2) `InitTrackamplifiersStep` now returns `Next("SetDefaultPwmSetpoints")`; (3) `SetDefaultPwmSetpointsStep` now returns `Next("EnableTrackamplifiers")`.

Evidence: `dotnet build` results recorded in `docs/build-test.md`. `SiebwaldeApp.Core` and `SiebwaldeApp.Core.Host` build with 0 errors; `SiebwaldeApp.sln` and `SiebwaldeApp.EcosEmu.sln` also build with 0 errors. Step-name chain verified by source inspection.

Impact: The host compile break and the initialization sequencing defect are resolved. No Fiddle Yard source was changed. `SiebwaldeApp.Tests`, remnant removal, and the configuration-authority refactor remain open.

## 2026-09-11: Obsolete Test Project Removed (Increment 2)

Decision: `SiebwaldeApp/SiebwaldeApp.Tests` is removed as an obsolete remnant of the abandoned station-in-C# approach. Option 1 (archive/remove) was chosen by the product owner.

Evidence: The project references a station domain model that has no definition anywhere in the active C# source (`TrackApplication`, `StationSide`, `StationTrack`, `TrackSensor`, `TrackBlock`, `TrackMetadata`, `TrackRole`, `TrainType`, `Signal`, `Amplifier`, `ITrackIn`, `ITrackOut`) and uses the old Ninject `IoC.Kernel` API. It is in no solution and cannot compile.

Impact: Files remain recoverable from git history (added in commit `104c1e6` "Rename to App", 2025-11-24). The encoded station design intent is recorded in `docs/project-knowledge.md`. No active project referenced the test project, so builds are unaffected.

## 2026-09-11: Pic18/Station Remnants Removed From The WPF App (Increment 3)

Decision: Removed the dead station-policy settings feature and the commented Pic18-era code from the WPF application.

Evidence: `StationSettingsPage.xaml(.cs)` bound to `TopPolicy`/`BottomPolicy`, which were commented out in `StationSettingsPageViewModel`, so the page was unbound; it was reachable from `TrackMenu.xaml`. `App.xaml.cs` had a large commented Pic18 block referencing `TrackPic18UdpAdapter`/`YardPic18UdpAdapter`.

Impact: Deleted `StationSettingsPage.xaml`, `StationSettingsPage.xaml.cs`, `StationSettingsPageViewModel.cs`, the commented `TrackAmplifierItemView.xaml.cs` and `TrackAmplifierItemViewModel.cs`. Removed `ApplicationPage.StationSettings`, its converter case, the `SideMenuViewModel.StationSettingsPage` command, and the `TrackMenu.xaml` button. Removed the commented Pic18 block from `App.xaml.cs`. Fixed the stale `StartTrackApplication` XML doc. `SiebwaldeApp.sln` builds with 0 errors and no references to the removed symbols remain. Fiddle Yard and the active ModBus `TrackControllerCommands` are untouched.
