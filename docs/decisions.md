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

## 2026-09-11: Page-Removed Legacy XAML Leftovers Removed

Decision: Removed the three legacy XAML files that were excluded from compilation via `<Page Remove>` (`TrackAmplifierItemView.xaml`, `TrackAmplifierManualControlView.xaml`, `TrackControlView.xaml`), their commented code-behinds, the two empty ViewModels they referenced (`TrackControlViewModel`, `TrackAmplifierManualControlViewModel`), and the corresponding `<None Include>`/`<Page Remove>` csproj entries.

Evidence: The files were unreferenced in active source and their ViewModels were empty classes only used by those XAMLs.

Impact: `SiebwaldeApp.csproj` is simpler; `SiebwaldeApp.sln` builds with 0 errors.

## 2026-09-11: Configuration Centralized Via CoreConfiguration (Increment 4)

Decision: Introduced `SiebwaldeApp.Core.CoreConfiguration` as the single source of truth for core configuration values, backed by `SiebwaldeApp.Core.Properties.CoreSettings`. Startup code in `SiebwaldeApplicationModel` and `SiebwaldeApp.Core.Host/Program.cs` no longer hard-codes the track controller address/ports or the firmware path.

Evidence: Added settings `TrckIpAddress` and `TrackAmplifierFwPath`, and corrected `TrckSendingPort`/`TrckReceivingPort` defaults from `60000` to `10000`/`10001` (the previous defaults were unused). Verified: `SiebwaldeApp.Core.Host` and `SiebwaldeApp.sln` build with 0 errors.

Impact: Behavior is preserved because the new defaults match the previous hard-coded values. The WPF `App.config` does not yet carry the `CoreSettings` section, so the WPF app relies on the Designer defaults. The settings UI (editing, default per entity, undo) is still planned.

## 2026-09-11: Host Detection And Init Page (Increment 5A)

Decision: Added a core `HostDetection` service and wired it into `SiebwaldeInitPage`. FiddleYard and the TrackController are detected with a ping on their host name/IP; Koploper is detected with a TCP connect probe to `127.0.0.1:5700`. The page shows per-host status, a human-readable step/state log, and start buttons that are enabled only for detected hosts.

Evidence: `SiebwaldeApp.Core/Diagnostics/HostDetection.cs`, `SiebwaldeInitPageViewModel`, and `SiebwaldeInitPage.xaml`.

Impact: Only the Fiddle Yard has a simulator option, and it must be activated by the operator via a dedicated button (`InitFiddleYardSimulator`). There is no simulator option for Koploper or the TrackController. The Fiddle Yard simulator forces `FYSimulatorActive = true` in `FiddleYardController.StartFiddleYardControllerAsync(forceSimulator)`. Verified: `SiebwaldeApp.sln` and `SiebwaldeApp.Core.Host` build with 0 errors. The settings UI remains planned.

## 2026-09-11: Settings UI (Increment 5B)

Decision: Implemented the settings page (`SiebwaldeSettingsPage`, reachable from the Siebwalde menu) to edit and persist the core configuration values. The editable settings were changed from Application scope to User scope so they can be written and saved.

Evidence: `SiebwaldeSettingsPageViewModel` provides editable values, `Save`, `Reload`, per-entity reset (`ResetTrack`, `ResetFiddleYard`, `ResetLogging`) and `Undo` (Ctrl-Z). The settings `LogDirectory`, `FYSendingport`, `FYReceivingport`, `TrckSendingPort`, `TrckReceivingPort`, `TrckIpAddress`, and `TrackAmplifierFwPath` are now User-scoped with setters; the config files were updated accordingly.

Impact: Values persist to user settings via `CoreSettings.Default.Save()`. Per-entity defaults come from the settings' `DefaultValue`. Undo restores the previous snapshot. Verified: `SiebwaldeApp.sln` and `SiebwaldeApp.Core.Host` build with 0 errors. Runtime behavior of the page is not yet verified.

## 2026-09-11: Init Page UI Improvements (Designer)

Decision: The Designer agent improved the `SiebwaldeInitPage` presentation: larger logging text, colored host status dots, and a live re-detection indicator.

Evidence: `Styles/Indicators.xaml` (`HostStatusDot`), `ValueConverters/HostStatusBrushConverter.cs` (present/checking/absent brush), updated `SiebwaldeInitPage.xaml`, `SiebwaldeInitPage.xaml.cs`, `SiebwaldeInitPageViewModel.cs`, and `App.xaml` (merged `Indicators.xaml`). All referenced resources exist (`SpinningText` in `Texts.xaml`, `FontSizeLarge` = 20, `WordOrangeBrush`/`WordGreenBrush`/`ForegroundDarkBrush` in `Colors.xaml`).

Impact: Host detection now repeats every 10 seconds via a `DispatcherTimer`, guarded against overlapping passes; automatic passes only log presence changes to avoid flooding the log. The timer is stopped on `Unloaded` so navigating away does not leak the view model. Verified: `SiebwaldeApp.sln` builds with 0 errors. The visual result is not runtime-verified.

## 2026-09-11: Unit Test Project Reintroduced (Increment 7)

Decision: Added a new `SiebwaldeApp.Core.Tests` xUnit project (`net8.0-windows7.0`) referencing `SiebwaldeApp.Core` and `SiebwaldeApp.EcosEmu`, and added it to `SiebwaldeApp.sln`. It replaces the removed obsolete station-domain test project with tests for current behavior.

Evidence: 23 tests covering `TrackApplicationVariables` (PWM clamp 0..799, EmoStop bit 15, slave 0 ignored, pending-write semantics, default PWM setpoints), `TrackAmplifierInitializationServiceAsync` (step chaining, unknown initial/next step, error, Continue-then-Completed), and `SimpleEcosCommandParser` (id/options parsing, malformed input, quoted-comma limitation).

Impact: `dotnet test` passes 23/23 and `SiebwaldeApp.sln` builds with 0 errors. The initialization-service tests would catch the previously fixed step-name defect. Documented behavior found while testing: a fresh `TrackAmplifierWriteData` starts at `Hr0Value` 0, so requesting PWM 0 on a fresh amplifier is treated as "no change" and not queued; also `TrackApplicationVariables` gives all 56 `trackAmpItems` the same `HoldingReg` array instance (aliasing, recorded as a finding).

## 2026-09-19: Koploper Locomotive Sync Replaces Pre-Seeding

Decision: Do not pre-seed `Logging/locos.json`. Koploper synchronizes its locomotive data with the digital central; an empty list prompts that sync. Pre-seeding causes duplicate locomotives for the same decoder address.

Evidence: Product owner explanation plus a live session in which removing `locos.json` let Koploper recreate and populate it.

Impact: `locos.json` is runtime state and stays untracked.

## 2026-09-19: Koploper Terminology And Bezetmelder Mapping

Decision: Keep Koploper terminology distinct. A **Koploper block** is a collection of **bezetmelders** (occupancy detectors) and can span several **amplifier sections**. A block may have one or more bezetmelders; two are the physical minimum where precise stopping is required. `BlockTopology` "blocks" are amplifier sections.

Evidence: Product owner explanation plus the Koploper export `Logging\Ovaaltje\BaanOverzicht_*.html`; bezetmelder `module.point` maps to ECoS sensor id `(module-1)*16 + point`, bit = sensorId-1 in feedback module 100, verified against the live trace.

Impact: `KoploperBlockMap` models block -> bezetmelders -> amplifier sections; the occupancy bridge forwards occupancy per bezetmelder.

## 2026-09-19: Occupancy Is Event-Driven, Not Polled

Decision: The occupancy bridge evaluates on `ITrackCommClient.AmplifierDataReceived` and only emits ECoS sensor events when a block's occupancy changes. No timer poll for occupancy.

Evidence: Amplifier data already arrives as events; polling would only add latency and load.

Impact: `TrackAmplifierOccupancyBridge.EvaluateAsync()` is called from the event handler plus once on attach. A watchdog for stale updates belongs to the diagnostics item.

## 2026-09-19: Amplifier Status Register Is The Occupancy Source

Decision: Occupancy comes from `HoldingReg2` bit 10 (`HR_STATUS_OCCUPIED_BIT`). `TrackAmplifierRegisters` in Core is the single source of truth for the register layout; the WPF amplifier view uses those constants instead of hard-coded bits.

Evidence: `TrackAmplifier4.X/modbus/General.h` and the existing WPF decode (`IsOccupied = HasBit(hr2, 10)`). The register description is provisional and may change with new firmware.

Impact: The register layout is defined in one place; the firmware still has a TODO to populate the occupied flag.

## 2026-09-19: Option A - The App Is The Composition Root

Decision: The WPF app composes the ECoS backend in-process with the real hardware backend; the standalone emulator host remains for simulation.

Evidence: The real backend needs the app's `TrackApplicationVariables`; a separate process would duplicate state. The emulator host must not reference Integration to avoid a cycle.

Impact: `TrackControlIntegration` builds the provider, real backend, in-process `SimpleEcosBackend` and bridge; the app references `SiebwaldeApp.Integration`. App startup wiring is still to be done.

## 2026-09-19: Block Topology And Mapping Are Editable Settings

Decision: `BlockTopologyConfig` and `KoploperBlockMapConfig` are user settings, editable on the settings page with undo.

Evidence: Product owner requirement that the mapping be created and edited by the operator, not hard-coded.

Impact: Defaults match the test oval; `CoreConfiguration.BuildBlockTopology()`/`BuildKoploperBlockMap()` parse them.

## 2026-09-19: Mapping Parsers Accept Line Breaks

Decision: `BlockTopology.Parse` and `KoploperBlockMap.Parse` treat a line break as a separator, and an unprefixed section containing `>` is treated as routes rather than amplifiers.

Evidence: The settings fields are multi-line (`AcceptsReturn="True"`). With `;`/`,`-only splitting, an operator who pressed Enter instead of `;` got a silently empty topology (the amplifier entry consumed the following route text and failed to parse).

Impact: A malformed-but-plausible entry now still yields a usable topology; unparseable entries are ignored rather than corrupting the rest. Parsing stays in Core (`CoreConfiguration`); the WPF view model only binds strings. Covered by `BlockTopologyRoutingTests.LineBreaksSeparateSectionsAndUnprefixedRoutes` and `KoploperBlockMapTests.LineBreaksSeparateEntries`.

## 2026-09-19: The ECoS Host Is Owned By An Integration Service

Decision: `TrackControlHost` (in `SiebwaldeApp.Integration`) owns the ECoS host lifetime, and Core exposes only `IEcosHostService` plus `TrackControlMode`. `SiebwaldeApplicationModel` receives the host through its constructor and starts/stops it; the WPF layer only constructs it in `IoC.Setup()` and calls a start/stop command.

Evidence: `SiebwaldeApp.Core` has no project references, so it cannot see `SiebwaldeApp.Integration` (which references Core and EcosEmu). Core would otherwise need to reference the integration layer, creating a cycle. The Core-internal `TrackCommClientAsync` and `TrackApplicationVariables` are needed to build the real backend, so starting the host from `StartTrackApplication` avoids widening the Core public API.

Impact: Composition, mode selection, ordering and disposal live in Integration and are unit-testable without WPF or hardware (`TrackControlHostTests`). WPF holds no control logic. `IEcosHostService` may be null, so the app still runs without Koploper.

## 2026-09-19: Real Mode Is Implicit, Simulator Mode Is Explicit

Decision: The ECoS host starts in `Real` mode at the end of `StartTrackApplication()`, because real mode requires the track communication client that only exists after the track application starts. `Simulator` mode is a separate operator action ("ECoS simulator" on the init page).

Evidence: Matches the existing Fiddle Yard pattern, where real mode is implied by "Start FiddleYard" and the simulator has its own button. Appending the start call after the existing initialization pipeline leaves the established sequencing untouched.

Impact: Starting an already running host is ignored, so pressing the simulator button after a real start cannot take the server down under Koploper.

## 2026-09-19: Port Roles Are Fixed And Explicit

Decision: 15471 is the ECoS server that Koploper connects to; 5700 is Koploper's external-information server that C# connects to. The directions are opposite and must not be swapped.

Evidence: `EcosEmulatorServer` listens on `IPAddress.Loopback` with port 15471; `KoploperExternalInfoClient(host: "127.0.0.1", port: 5700)` is an outbound client. A live session confirmed C# connecting out to 5700 and Koploper connecting in to 15471.

Impact: Loopback is correct because Koploper runs on the same PC. If Koploper ever moves to another machine, the listen address must become configurable (recorded in the backlog).

## 2026-09-19: Explicit ECoS Host Mode-Transition Semantics

Decision: Real mode is authoritative over the simulator, and every start request reports an explicit outcome instead of being silently ignored.

- requested mode already active -> `AlreadyActive` (idempotent no-op);
- `Real` requested while `Simulator` runs -> stop the simulator cleanly, then start real (`Transitioned`);
- `Simulator` requested while `Real` runs -> `Rejected`, the real host stays untouched;
- `Real` requested without the track communication client or shared variables -> `ArgumentException` **before** any running host is touched;
- no host configured -> `NotAvailable`; start failure -> `Failed`.

Evidence: A silently ignored real-mode request after a successful track start would leave Koploper connected to the simulator while the real layout was live. That state must be impossible to reach without a visible signal.

Impact: `IEcosHostService.StartAsync` returns `Task<EcosHostStartResult>` and `SiebwaldeApplicationModel.ActiveEcosMode` exposes the mode that is really running, which the init page shows as `EcosModeStatus`. If the transition fails, the host releases the port and the external-info client before rethrowing, so a failed switch never leaves a half-started host. `EcosEmulatorServer` sets `ReuseAddress` so the same port can be rebound immediately during a transition.

## 2026-09-19: Blank Mapping Settings Fall Back To The Declared Default

Decision: `CoreConfiguration.BlockTopologyConfig` and `KoploperBlockMapConfig` return the setting's declared default when the persisted user value is null, empty or whitespace. A non-empty value is always used as-is, even when malformed.

Evidence: A legacy `user.config` written before the defaults existed keeps an empty string, which would silently disable the mapping and force the operator to find the Reset button. The declared default is read from `CoreSettings.Default.Properties[name].DefaultValue` so the Designer stays the single source of truth instead of the mapping string being duplicated in code.

Impact: `BuildBlockTopology()`/`BuildKoploperBlockMap()` always see a usable default on a fresh or legacy profile, while a genuinely wrong non-empty value still produces an empty parse so the mistake is visible. The settings page reads the same resolved values, so the page and the runtime cannot disagree. The fallback is logged. Covered by `CoreConfigurationTests`.

## 2026-09-19: One Shared Switch Translation Path

Decision: ECoS switch commands are translated by a single `SwitchController` in front of whichever hardware backend is active, via `SwitchTranslatingHardwareBackend`. Real and simulator mode differ only in the `ISwitchOutput` behind the controller.

Evidence: The simulator previously received switch commands directly while the real backend ignored them, which would have produced two divergent behaviours. The task requires no second control path for simulator mode.

Impact: Mapping, inversion and initialization are implemented and tested once. Adding the real accessory-decoder path later means supplying a different `ISwitchOutput`, not changing the translation. `SwitchMapConfig` is parsed in Core and the controller lives in Integration, keeping the dependency direction intact.

## 2026-09-19: Switch Route Conditions Are Proven, Not Inferred

Decision: The oval route conditions are fixed as `3>4@1:0` and `3>5@1:1`, replacing the earlier provisional, unconditional `3>4`/`3>5`.

Evidence: `Logging\19-09-2026_EcosEmuTrace.txt` repeats the pattern six times per route: loco 1 (which reaches block 4) is preceded by `set(11,switch[1g])` + `set(11,switch[2r])`, loco 2 (which reaches block 5) by `set(11,switch[1r])` + `set(11,switch[2g])`. Both switches are always commanded as a complementary pair, so conditioning the transition on switch 1 alone is correct and sufficient.

Impact: Look-ahead through the passing loop now uses a real switch condition instead of treating both branches as always available. The `@<id>` is the ECoS/Koploper switch address, not the physical address.

## 2026-09-19: Unknown Switch Rest Position Is 'keep', Not A Guess

Decision: The shipped `SwitchMapConfig` default is `switches: 1:1:keep, 2:2:keep`. `keep` means "do not drive this output at initialization" and the switch is then not reported as having a known state.

Evidence: The trace proves which state each route needs, but not the layout's power-on position. Inventing one would create a logical/physical disagreement at startup, which the task forbids.

Impact: Initialization is deterministic (entries with `g`/`r` are driven and recorded; `keep` entries are left alone and stay unknown), and nothing is actuated on hardware that is not wired. The operator sets `g`/`r` per switch once the real rest position is known. A malformed or duplicate entry is recorded in `SwitchMapping.Errors` and logged rather than being turned into a plausible-but-wrong mapping.

## 2026-09-19: SetSwitch Reports Whether The Command Reached An Output

Decision: `IHardwareBackend.SetSwitch` returns `bool`, and the ECoS backend sends a `state[...]` event only when it returns true.

Evidence: A live software-only test showed `set(11,switch[9g])` (an unmapped address) still produced `11 state[0]` for Koploper while nothing moved, i.e. the logical and physical switch state silently disagreed.

Impact: Unmapped addresses (including the signals 51..55, which Koploper commands in the same `switch[...]` form) are ignored without fabricating a state. `TrackAmplifierHardwareBackend.SetSwitch` returns false because the real accessory path is not wired yet, so real-mode switch commands are honestly reported as not applied.

## 2026-09-19: Requested, Commanded And Observed Are Separate

Decision: The control path never collapses "command sent" into "physical state confirmed". `SwitchController` tracks the requested (logical ECoS) position and the commanded (physical) position separately, and only compares against an observed position when `IObservability.SwitchFeedbackAvailable` is true and an `ISwitchObserver` can actually read it.

Evidence: Real switch actuation is not wired and there is no physical switch feedback, so a confirmation would be invented. The simulator, by contrast, can be read back.

Impact: Real mode reports switch state as `StateUnknown`/Warning and never as a confirmation; `ISwitchOutput.IsAvailable` distinguishes "not wired yet" (a known limitation, not a fault) from "the backend refused" (`CommandNotApplied`). Unavailable feedback never creates a fault on its own.

## 2026-09-19: Safety Reactions Reuse The Existing Stop Paths

Decision: A safety stop uses only mechanisms the ECoS path already uses: per locomotive `IHardwareBackend.SetLocoSpeed(address, 0, direction)` (ECoS speed 0 = neutral setpoint), and for an unattributable fault `IHardwareBackend.SetPower(false)` - the same central power-off that Koploper's `set(1,stop)` triggers. `ControlSafetyGuard` is idempotent per fault key so a persistent condition cannot produce a stop storm.

Evidence: The task forbids a second locomotive-control path, and the real backend's `SetLocoSpeed` resolves locomotive -> block -> amplifiers, which is exactly the existing translation.

Impact: A loco-scoped divergence stops only that locomotive, so unrelated trains keep running; only a fault that cannot be attributed to one locomotive stops the whole layout. Safety decisions live in Integration/Core, never in WPF.

## 2026-09-19: Safety Faults Latch Until An Explicit Reset

Decision: The first `StopRequired` diagnostic latches as the root cause and is never cleared automatically - not by a later command and not by a lower-severity diagnostic. Recovery is only an explicit `ResetSafety()`. Warnings are transient and do not latch.

Evidence: Auto-clearing on the next command would hide the reason the layout stopped and could let a locomotive move into an unverified route.

Impact: `ControlDiagnostics` keeps a bounded history (100) plus a separate `LatchedUnsafe`, so current critical state and history are both available to the UI without the critical state being overwritten. A later, different fault is recorded but does not replace the latched root cause.

## 2026-09-19: A Safety Latch Interlocks Movement, It Does Not Freeze Everything

Decision: `ControlSafetyInterlockBackend` decorates the hardware backend already in use, so every ECoS movement command passes the same policy. A loco-scoped latch refuses non-zero movement for that locomotive only; a layout-wide latch refuses it for every locomotive and refuses power-on. Stopping is always allowed, switch commands always pass, and a corrective switch command never unlatches anything.

Evidence: A latch that only prevented repeated stops still let a later Koploper command move the affected locomotive again, which is the gap this closes. Freezing everything would block the corrective switch change that is usually the only way to resolve the divergence.

Impact: `IHardwareBackend.SetPower`/`SetLocoSpeed` return `bool` (the same contract fix as `SetSwitch`), so `SimpleEcosBackend` replies `<END 8 (SAFETY_INTERLOCK)>` and emits no `speed[...]`/`dir[...]` event when a command is refused. Koploper is never told a refused movement succeeded. Rejections are reported once per locomotive per latch (`MovementRejectedBySafety`) to avoid a diagnostic storm.

## 2026-09-19: A Safety Reset Revalidates Before It Clears

Decision: `ControlSafetyGuard.Reset()` revalidates every latched fault through `DivergenceChecker.IsResolved` and refuses the reset while any condition is still present, reporting `ResetRefused`. A refused reset leaves both the latch and the movement interlock in place.

Evidence: Clearing the latch on request would restore movement permission without the underlying condition being fixed, which is exactly the unsafe state the latch exists to prevent.

Impact: Recovery is: correct the condition (for example change the switch) -> explicit reset -> movement allowed again. Reset itself never issues movement, and `ControlDiagnostic.RequiredSwitchPosition` records what the route needed so revalidation does not have to re-derive it.

## 2026-09-19: Real Occupancy Comes From The Existing Amplifier Data Path

Decision: Real-mode occupancy is read from the existing amplifier holding registers through `TrackAmplifierOccupancyProvider`, and real mode reports occupancy as observable once valid amplifier data has been received. No firmware change was made.

Evidence: `TrackAmplifier4.X/processio.c` already sets `HR_STATUS` bit 10 from `g_occ = CMP1_GetOutputStatus()`, the PIC32 master transports the 12 holding registers in the SLAVEINFO frame, `TrackCommClientAsync` stores them into `TrackAmplifierItem.HoldingReg` and raises `AmplifierDataReceived`, and the track-amplifier page already decodes the same bit. The `General.h` "(TODO: implement when occupancy source known)" comment is stale.

Impact: `ModeObservability`'s static `OccupancyAvailable = false` for real mode was wrong and is replaced by `AmplifierOccupancyObservability`, which derives availability from `SlaveDetected` - the existing "a frame was parsed for this amplifier" signal - so no new freshness mechanism and no duplicate occupancy state were introduced.

## 2026-09-19: Unknown Occupancy Is Not Clear

Decision: `IOccupancyProvider` gained `IsBlockOccupancyKnown`. A block is only clear when every amplifier section covering it has valid data. Unknown is never reported to Koploper as free, never selected by look-ahead, and produces `StateUnknown` (Rejected) instead of `OccupancyMismatch` (StopRequired).

Evidence: `HoldingReg` is initialised to zeros, so before the first frame arrives the occupied bit reads false. Treating that as "clear" would tell Koploper a block is safe and let the look-ahead pre-command into an unverified block.

Impact: The occupancy bridge skips blocks whose occupancy is unknown and leaves them out of its change tracking, so a later known value still produces an event. `ControlSafetyGuard.Reset()` also requires the block to be known clear before an `OccupancyMismatch` is considered resolved.

## 2026-09-19: Amplifier Occupancy Freshness Is Derived From The Frame Timestamp

Decision: `TrackAmplifierItem.LastDataReceivedUtc` is stamped when a frame is parsed, and `TrackAmplifierDataFreshness` (default 2 s) decides whether a section's data is current. A section is current only when it is detected and fresh. Stale data is treated as unknown, never as clear, and a stale occupied reading is not promoted to definite occupancy.

Evidence: no existing state proves freshness. `SlaveDetected` is written once and never cleared, `HoldingReg` keeps its last values, `TrackCommClientAsync._publishTimer` republishes `AmplifierDataReceived` every 100 ms for every `SlaveDetected != 0` amplifier regardless of new data, and `ITrackTransport` exposes no connection-loss or health signal. `MbReceiveCounter` is read from the frame, so its increment semantics cannot be verified from C# without a firmware change.

Impact: the previous `SlaveDetected != 0` check was a genuine defect: after communication stopped, a block could stay "known clear" forever based on an old register value, which would tell Koploper a block is safe and let look-ahead pre-command into it. The timestamp is stamped in the same step that stores the registers, so there is still a single source of truth, no new timer, and no hardware polling. Staleness is evaluated when a consumer already runs (the comm client's publish event), so no new polling loop was introduced.

## 2026-09-19: Stale Occupancy Does Not Become Definite Occupancy

Decision: for a multi-section block, a fresh occupied section proves the block occupied even when another section is silent, but a stale occupied reading does not. Stale data yields unknown.

Evidence: a stale "occupied" reading may describe a train that has already left, so promoting it to a definite occupancy would be a false claim; but treating it as clear would be unsafe. Unknown is the honest answer: it blocks look-ahead and reports `StateUnknown` (Rejected) without inventing a stop.

Impact: definite occupancy is never turned into unknown (requirement preserved), and a previously latched `OccupancyMismatch` cannot be reset while its source is stale, because `IsResolved` requires the block to be known clear, which requires fresh data.

## 2026-09-19: Protocol-Specific Speed Is Normalized At The ECoS Boundary

Decision: protocol-specific ECoS speed steps (for example `DCC28`, steps 0..28) are normalized to the existing `0..127` domain at the ECoS/protocol boundary (`SimpleEcosBackend`). The `IHardwareBackend` contract and `AmplifierSpeedMapper` keep operating on normalized `0..127`, so the hardware layer stays protocol-independent. **Implemented 2026-09-20** in `SiebwaldeApp.EcosEmu.ProtocolSpeedNormalizer`, called from `SimpleEcosBackend.HandleSetAsync`.

Evidence: live validation on 2026-09-19 showed the locomotive protocol is `DCC28` and Koploper/ECoS supplied steps `0..28`, while `AmplifierSpeedMapper.ToPwm` scales by 127. Live DCC28 step 24 produced only ~PWM 475 instead of approaching 799, so a full-throttle command uses only part of the usable 400..799 range. The root cause in source is that `SimpleEcosBackend`'s `opt.StartsWith("speed")` branch also matched `speedstep[...]`, so the raw protocol step was passed downstream unchanged.

Impact: the correction did not push DCC28 knowledge into the amplifier/backend layer; the boundary is the only place that knows the protocol. `speed[...]` (already normalized `0..127`) is passed through unchanged so it is not scaled twice; `DCC128` is passed through; a non-zero `speedstep` for an unknown protocol is refused explicitly instead of being scaled with a guessed value. Recorded as fixed in `docs/backlog.md` and `docs/analysis-coverage.md`. **Live full-range verification 2026-09-20:** the full `speedstep[0..28]` ramp on amplifier 1 produced HR0 `399..799` (step 24 -> 109 -> 742, previously 475), the motor responded, and Koploper accepted the normalized echo (`<END 0 (OK)>`, no corrective traffic).

## 2026-09-19: Live Hardware Is An Explicit Delegation Exception

Decision: the single-agent default is unchanged for normal work, but authorized live railway hardware validation is an explicit exception: live test execution and evidence collection are delegated to the `integrator`, while the Project Lead keeps scope, authorization, safety boundaries, sequencing, handoff, backlog and final documentation. The Developer implements and software-verifies only; the Developer must not run the physical motor, start a hardware-driving harness, or declare its own fix physically validated.

Evidence: in the 2026-09-19 live session a standalone harness omitted the production runtime loop, and the harness was left running as a hidden background PID that also left amplifier 1 at PWM 475 instead of neutral on shutdown.

Impact: `.opencode/agents/project-lead.md` and `.opencode/agents/integrator.md` now carry the live-hardware policy (process visibility with PID/command/working directory/session name/manual stop/timeout/neutral procedure, mandatory safe cleanup before handoff, and the budget-exhaustion procedure). `developer.md`, `architect.md` and `designer.md` were deliberately not modified.

## 2026-09-20: Requested Locomotive State Is Owned By The ECoS Model

Decision: the requested/logical locomotive state (`LocoState.Speed`/`LocoState.Direction`) is owned by `SimpleEcosBackend`; physical application through `IHardwareBackend.SetLocoSpeed` is best effort. A valid `dir[...]` always updates the logical direction and emits the `dir[...]` event, and a speed command that cannot reach a physical target (no known block) retains the requested logical speed. Only a real latched safety interlock refuses non-zero movement and leaves the logical state unchanged.

Evidence: targeted live reproduction 2026-09-20 (loco 2 / ECoS id 1001 on unmapped block 0) showed that a direction command was discarded when the hardware backend returned false, so a later `speedstep[1]` used the stale default `Direction = 1` and produced reverse-band PWM 382 instead of forward 416. The single boolean return of `SetLocoSpeed` conflated "safety interlock refused" with "no physical target".

Impact: `SimpleEcosBackend` has one helper (`ApplyNormalizedLocoSpeed`) for speed application and treats the `dir` branch as a logical state change plus a best-effort physical application. `IHardwareBackend` was **not** widened; instead the optional `IMovementSafetyGate` capability (`SiebwaldeApp.EcosEmu`) lets the ECoS backend ask whether non-zero movement is currently blocked, and `ControlSafetyInterlockBackend` implements it. A no-target command now answers `<END 0 (OK)>` and emits its logical `speed[...]`/`dir[...]` event, while a real safety refusal still answers `<END 8 (SAFETY_INTERLOCK)>` with no event. The movement interlock itself (loco/layout latch, stop always allowed, switch commands pass) is unchanged. ECoS events describe logical state, not a claim that a physical amplifier changed. Covered by `SimpleEcosBackendDirectionStateTests`; physical regression validation on the layout remains pending.

## 2026-09-20: Safety Stops Retain Commanded Physical Amplifier Targets

Decision: a safety stop must reach every physical track amplifier a locomotive has been successfully commanded non-neutral on, independent of the current logical block mapping. The implementation adds a retained commanded-actuator ownership model instead of a single "last amplifier" pointer.

- `AmplifierCommandTracker` (Core) keeps, per locomotive, the **set** of physical amplifiers it last commanded non-neutral. A concrete non-neutral command (the queued physical HR0 setpoint, not mere logical intent) adds a target; only a successfully issued neutral command removes it. Moving to another block, a route change, or a lost block mapping never removes a target. Ownership transfers to the most recent locomotive that commands the same amplifier, so one locomotive's stop cannot clear another locomotive's outstanding target; a deliberate global neutralization (`RecordNeutralGlobally`) clears across locomotives.
- `TrackAmplifierHardwareBackend` records every current-block and look-ahead command in the tracker and implements the new `IAmplifierNeutralizer` (`NeutralizeAmplifiers`, `GetKnownPhysicalAmplifiers`). This is the amplifier-centric primitive that is independent of the block mapping and topology.
- `EcosHardwareStopSink.StopLoco` first uses the existing loco path (`SetLocoSpeed(address, 0, 0)`) and then neutralizes every retained outstanding target. It returns a `SafetyStopResult`, and `ISafetyStopSink` now reports that result for both stop methods.
- Stop success means **all required neutral commands were accepted for concrete physical amplifiers**. It never claims observed neutralization: observed physical confirmation stays a separate state, exactly as requested/commanded/observed stay separate elsewhere. A stop that could not be attempted, or that left a required amplifier uncommanded, is not success.
- `ControlSafetyGuard` now inspects the result. If the loco-scoped stop is incomplete, it emits `CommandNotApplied` (or `BackendUnavailable` when no backend exists), keeps the latch, and escalates to the amplifier-centric layout neutralization (`SafetyAction.StopLayoutEscalated`). If the escalation is also incomplete, a second diagnostic records that the safety concern remains latched. The latch is never cleared because a stop call returned.
- Detected-but-unmapped amplifier policy: the safety fallback target set is the authoritative hardware inventory (`SlaveDetected` amplifiers) **plus** the configured topology **plus** every retained outstanding target. A physical track amplifier the application can communicate with is therefore not left outside the strongest neutralization operation just because it is absent from `BlockTopology` (the live case: installed amp 6). `SetPower(false)` keeps its existing topology-only logical semantics; the new primitive is used explicitly by the safety escalation.

Evidence: the physically confirmed defect (`STOP REACHABILITY DEFECT CONFIRMED`, `STOP FAILURE REPORTING DEFECT CONFIRMED`, harness commit `03f5221`) showed amp 1 left at HR0 416 after an A -> B transition while the loco stop neutralized only the resolved amp 3 and the sink still returned `True`. Software fix and regression tests in commit `66d75d0` (`StopReachabilityTests`, 17 tests). The harness was adapted for the new result contract in a separate post-fix revision (`c13d9fc`); the historical defect-reproduction revision `03f5221` is untouched.

Impact: `IHardwareBackend` was **not** changed; the addition is a focused safety interface (`IAmplifierNeutralizer`), a Core tracker, and a richer `ISafetyStopSink` result. Loco-scoped stops remain loco-scoped when they can be delivered; only an incomplete loco stop escalates to the layout. Physical post-fix verification on the layout remains **PENDING** (Integrator). Asynchronous send failure and observed physical confirmation are still not synchronously observable in this architecture, so the tracker tracks commanded state, not confirmed physical state.

## 2026-09-20: Track-Amplifier Physical Device Class Is Authoritative And Central

Decision: the physical device class decides whether track-amplifier PWM/HR0 semantics are legal at all, and it is defined once in `TrackAmplifierAddress` (Core): track amplifiers occupy `1..50`, backplane/configuration modules occupy `51..55`. `TrackAmplifierAddress.IsTrackAmplifierAddress` is used by every path that builds or queues an HR0 command.

The independent Integrator review of the stop-reachability fix (`SOFTWARE REVIEW FAIL`) found a blocking defect: the amplifier-centric fallback derived its target set with logic equivalent to `SlaveNumber != 0 && SlaveDetected != 0`, which included backplane slaves `51..55`. Writing the track-amplifier neutral value `399` (`0x018F`) to a backplane module's HoldingReg0 is invalid: `TrackBackplane2.X/main_proto_backplane.c` uses HR0 `ActValue` as a configuration/enable word (which amplifier IDs are enabled), so `399` would change configuration bits.

Evidence (source): track amplifiers `1..50` — `TrackApplicationVariables.MaxAmplifiers`, `TrackAmplifierWriteData.SlaveNumber` doc, `TrackAmplifierPageViewModel` ("1..50 amplifiers"), `FlashFwTrackamplifiersStep` and `EthernetTargetDataSimulator` (`< 51`), `TrackControlMain` ("data[0] = SlaveAddress (1..50)"); backplane `51..55` — `TrackAmplifierPageViewModel` ("51..55 backplane modules") and `TrackBackplane2.X/main_proto_backplane.c` `Get_ID()` (`MODBUS_ADDRESS = 50 + ID pin`).

Impact: `GetKnownPhysicalAmplifiers` returns only legitimate track amplifiers; `NeutralizeAmplifiers` rejects any non-track address (returned as not commanded, no write); `TrackApplicationVariables.SetDesiredAmplifierControl` — the lowest shared HR0 choke point through which loco commands, look-ahead, safety neutralization and the manual page all pass — refuses non-track addresses; `AmplifierCommandTracker` refuses to retain them; `SetLocoSpeed`/`ApplyLookAhead`/`SetPower(false)`/`InitializeDefaultPwmSetpoints` filter topology and detected amplifiers to the track class. The new test `AmplifierClassificationAndSafetyDomainTests.StrongestNeutralization_ExcludesBackplaneSlaves_AndWritesNoPwmToThem` fails against the reviewed implementation and passes after the correction.

## 2026-09-20: Operational Grouping Is A Separate Concept; Cross-Domain Emergency Policy Is Not Decided Here

Decision: `TrackAmplifierGroups` (Core) models the operational safety domain — `MainRailway`, `MountainRailway`, `Spare` — explicitly, backed by the new `TrackAmplifierGroupsConfig` setting. An amplifier is never inferred as main railway from its address range, from detection state, or from `BlockTopology` membership; an unconfigured legitimate track amplifier stays `Unassigned`. Physical device type, operational group, and logical block mapping are three independent concepts.

Evidence: the planned installation uses the same `1..50` track-amplifier hardware/protocol for the main railway, roughly two mountain-railway amplifiers, and potentially spares; `BlockTopology` only references the main-railway blocks. Deriving physical inventory or the main-railway group from `BlockTopology` or from the address range would silently mis-classify the mountain/spare amplifiers.

Impact: the model and configuration exist now so the complete group/domain configuration can be filled in without a redesign. **The cross-domain safety policy is deliberately not decided**: whether a main-railway failure (`SafetyAction.StopLayoutEscalated`) must also neutralize the mountain railway is a Product Owner/architecture decision and remains **PENDING**. The current strongest physical emergency neutralization (`StopLayout`) still targets every legitimate track amplifier the control path knows about (detected hardware plus configured topology), as it did before; it is not silently narrowed to one domain, and it is not expanded to configured-but-undetected group members. The exact current per-action domain behaviour and the pending decision are recorded in `docs/handoff.md`.

## 2026-09-20: A Required Neutral Target Is Only Established With Fresh Communication

Decision: a neutral value placed in the pending-write queue is not, by itself, an established neutralization. For a required safety target (a retained non-neutral command, or a physically present amplifier in the strongest emergency), the neutral is only treated as established when a neutral command was accepted for a legitimate track amplifier **and** that amplifier's communication is fresh (`TrackAmplifierDataFreshness.IsCurrentData`). A stale or never-seen required target is reported as unresolved, keeps its retained target, and makes `SafetyStopResult.Succeeded` false; the guard keeps the latch and emits `CommandNotApplied`.

Evidence: the protocol provides no neutralization acknowledgement and `TrackApplicationVariables.SetDesiredAmplifierControl` only queues the value; `TrackControlMain` transmits it asynchronously. A stale/comm-down amplifier could otherwise be removed from outstanding tracking after a queue-level "success".

Impact: `IAmplifierNeutralizer.GetAmplifierCommunicationState` exposes `Invalid`/`NeverSeen`/`Stale`/`Fresh`. `EcosHardwareStopSink` treats only `Fresh` as established, and `TrackAmplifierHardwareBackend.RecordCommand` only clears a retained target on a fresh neutral. A topology entry that was never seen remains a best-effort extra (neutral is still queued) and is not reported as a failed required target, so an absent mapped amplifier does not produce a false failure. Requested (software wants neutral), Commanded (neutral accepted at the queue boundary for a fresh legitimate track amplifier), and Observed (a fresh returned register reports neutral) remain distinct; observed confirmation stays asynchronous and is never claimed by `SafetyStopResult`.

## 2026-09-20: A Dedicated Production Control Trace Over The Existing Logging Infrastructure

Decision: normal Koploper/ECoS operation gets **one** dedicated production control-trace logfile, built on the existing logging infrastructure instead of a new framework. A focused `IControlTrace`/`ControlTraceLogger` (Core) is registered on the shared `ILogFactory` as the `ControlTraceLog` component, so it uses the same `BaseLogFactory` prefix, the same date-based filename convention (`{CoreSettings.LogDirectory}{dd-M-yyyy}_ControlTraceLog.txt`) and the same `FileLogger` behaviour/lifecycle as the other component logs. `ControlTraceLogging.Register` is called from the real WPF composition root (`SiebwaldeApp/IoC/IoC.cs`, reached from `App.OnStartup`), so the file is created automatically in production; it is not hidden in tests, the harness or the standalone emulator.

Evidence: the supplemental Integrator review concluded `PRODUCTION TRACE INCOMPLETE` and `OFFLINE PARSING NOT YET PRACTICAL`; many relevant events used `Console.WriteLine`, which a `WinExe` does not persist, and the existing `EcosHost` logging had no production `FileLogger`. A dedicated daily file with a stable `EVENT=`/`key=value` payload makes the causal path (Koploper/ECoS input -> logical decision -> physical command -> safety decision -> neutralization/escalation/result) reconstructable offline.

Impact: one logger instance with focused event methods; string construction is centralized in `ControlTraceFormat` (single-space field separator, `<none>` for null/unknown, comma-separated ordered lists, lower-case booleans, stable enum member names, invariant numeric formatting). Requested (incoming command/speed), Commanded (queue-acceptance boundary or runtime transmission) and Observed (fresh returned register) remain distinct; the trace never fabricates observed state. No correlation id was added (no identity propagates through the control interfaces without invasive changes); millisecond timestamps plus loco/amp/event fields suffice. The existing `FileLogger` limitations (no rotation, not thread-safe, `File.AppendAllText`, silent write failures) are reused and documented; the trace serializes only its own writes, and the trace file is written only by the `ControlTraceLog` instance. `SiebwaldeApp.EcosEmu` now references `SiebwaldeApp.Core` for the trace abstraction; Core has no project references, so the dependency stays acyclic. The historical defect harness `03f5221` is untouched; the post-fix harness revision `01414e1` initializes the same production trace over its own log factory (no file logger by design). Physical post-fix validation remains **PENDING**.
