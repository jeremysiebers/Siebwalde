# Handoff

## Latest Session (2026-09-19)

Completed on `feature/csharp-cleanup-startup` (all committed and pushed; `git status` clean for tracked files):

- **Increment 6 step 1** (protocol reconnaissance) and **step 2-3**: documented in `docs/koploper-interface.md`.
- **Speed -> PWM**: `AmplifierSpeedMapper` (ECoS 0..127 + direction -> neutral 399, forward 400..799, reverse 398..1, never 0).
- **Routing + look-ahead**: `BlockTopology` extended with switch-conditioned transitions and a no-look-ahead marker (`!`) for station departures; `IOccupancyProvider`; `LookAheadPlanner`; look-ahead wired into `TrackAmplifierHardwareBackend`.
- **Koploper block mapping**: `KoploperBlockMap` (Koploper block -> bezetmelders -> amplifier sections, with reverse lookups). The authoritative oval mapping was extracted from the Koploper HTML export in `Logging\Ovaaltje\`.
- **Occupancy path**: `TrackAmplifierRegisters` (mirrors `TrackAmplifier4.X/modbus/General.h`; HR_STATUS = HoldingReg2, bit 10 = occupied) and `TrackAmplifierOccupancyProvider` (block occupied when any covered section is occupied). `TrackAmplifierOccupancyBridge` forwards occupancy changes to Koploper as ECoS sensor events (event-driven).
- **Composition (option A)**: `TrackControlIntegration` builds the occupancy provider, the real backend, the in-process `SimpleEcosBackend` (when a loco repository is supplied) and the bridge; `Attach()`/`Detach()` subscribe to `ITrackCommClient.AmplifierDataReceived`. The WPF app now references `SiebwaldeApp.Integration`.
- **Editable mapping settings**: `BlockTopologyConfig` and `KoploperBlockMapConfig` user settings (oval defaults), exposed via `CoreConfiguration.BuildBlockTopology()`/`BuildKoploperBlockMap()`, editable on the settings page with undo.
- **Live Koploper session**: emulator trace capture added to the emulator host; verified `create`, `set(id, speedstep[n])`, occupancy events, `[EXT]` position records, the loco sync, and the bezetmelder -> sensor/bit mapping.

Tests: `dotnet test` **98/98 passed**. `SiebwaldeApp.sln` builds with 0 errors.

### Files changed this session

Core (`SiebwaldeApp/SiebwaldeApp.Core`):
- Added: `Model/TrackApplication/Control/IOccupancyProvider.cs`, `KoploperBlockMap.cs`, `LookAheadPlanner.cs`, `TrackAmplifierOccupancyProvider.cs`, `TrackAmplifierRegisters.cs`.
- Modified: `Model/TrackApplication/Control/BlockTopology.cs`, `Configuration/CoreConfiguration.cs`, `Properties/CoreSettings.settings`, `Properties/CoreSettings.Designer.cs`.

Integration (`SiebwaldeApp/SiebwaldeApp.Integration`):
- Added: `TrackAmplifierOccupancyBridge.cs`, `TrackControlIntegration.cs`.
- Modified: `TrackAmplifierHardwareBackend.cs`.

WPF app (`SiebwaldeApp/SiebwaldeApp`):
- Modified: `SiebwaldeApp.csproj` (added Integration reference), `Pages/SiebwaldePages/SiebwaldeSettingsPage.xaml`, `ViewModel/SiebwaldeViewModels/SiebwaldeSettingsPageViewModel.cs`, `ViewModel/TrackViewModels/TrackAmplifierPageViewModel.cs` (now uses the Core register constants).

Emulator host:
- Modified: `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.Host/Program.cs` (console trace capture) - committed earlier in the session.

Tests (`SiebwaldeApp/SiebwaldeApp.Core.Tests`):
- Added: `BlockTopologyRoutingTests.cs`, `KoploperBlockMapTests.cs`, `LookAheadPlannerTests.cs`, `TrackAmplifierHardwareBackendLookAheadTests.cs`, `TrackAmplifierOccupancyBridgeTests.cs`, `TrackAmplifierOccupancyProviderTests.cs`, `TrackControlIntegrationTests.cs`.

Docs:
- Modified: `docs/koploper-interface.md` (large additions), `docs/backlog.md`, `docs/handoff.md`.

### Decisions and assumptions

- **PWM mapping**: neutral 399, forward 400..799, reverse 398..1, never 0 (confirmed by the product owner). Speed steps are 0..127 (from the `ecos-master` library).
- **A Koploper block can have one or more bezetmelders**, not always two; the physical minimum of two applies where precise stopping is required.
- **Bezetmelder = sensor**: bezetmelder `module.point` maps to ECoS sensor id `(module-1)*16 + point`, bit = sensorId-1 in feedback module 100. Verified against the live trace.
- **Occupancy is event-driven, not polled**: the bridge evaluates on `AmplifierDataReceived` and only emits events on change.
- **Do not pre-seed `locos.json`**: Koploper syncs its locos to the central; pre-seeding creates duplicates.
- **Option A**: the WPF app is the composition root and hosts the ECoS backend in-process with the real hardware backend.
- **Assumption**: the current firmware reports a single occupied flag per amplifier section, so all bezetmelders of a Koploper block follow that flag until entry/exit can be distinguished.
- **Assumption**: `TrackAmplifierRegisters` is the single source of truth for the amplifier register layout; the register description is provisional and may change with new firmware.

### Tests and checks performed

- `dotnet test "SiebwaldeApp\SiebwaldeApp.Core.Tests\SiebwaldeApp.Core.Tests.csproj" -c Debug` -> **98 passed, 0 failed**.
- `dotnet build "SiebwaldeApp\SiebwaldeApp.sln" -c Debug` -> **0 errors**.
- Live Koploper session against the emulator: loco sync, driving (`set(id, speedstep[n])`), occupancy events, position records - all verified from the trace.
- No hardware was connected or controlled.

### Incomplete / uncertain

1. **App startup wiring (rest of "2-rest")**: `TrackControlIntegration` is not yet created/started from `SiebwaldeApplicationModel`; the `EcosEmulatorServer` is not yet started in-process by the app; there is no real-vs-simulator mode selection in the app.
2. The two new settings are not yet present in the `App.config` files (defaults currently come from the Designer attributes).
3. **Switch mapping** (real <-> Koploper + default init state) not done; switch addresses for the oval are known (1 and 2) but the branch selection (`3>4` vs `3>5`) is still provisional in the topology config.
4. **Divergence check + ECoS stop + operator diagnostics** not started.
5. The firmware occupied flag has a TODO; live occupancy depends on firmware that populates it (the product owner states the real test firmware already returns it).
6. `TrackApplicationVariables` gives all 56 items the same `HoldingReg` array instance (aliasing) - recorded in the backlog.
7. `Logging/` (runtime logs, traces, `locos.json`, the Koploper HTML exports) is untracked and intentionally not committed.

### Best next step

**Finish the app startup wiring**: in `SiebwaldeApplicationModel` (or a new `EcosEmulatorService`) create `TrackControlIntegration` from `CoreConfiguration.BuildBlockTopology()`/`BuildKoploperBlockMap()` plus the loco repository and `KoploperExternalInfoClient`, start the `EcosEmulatorServer`, call `Attach()`, and add a real-vs-simulator mode selection. Then add the two new settings to `App.config`.

## Latest Session (2026-09-17)

Completed today (all on `feature/csharp-cleanup-startup`, pushed):

- Increments 1-7: host logging + init sequencing fix, obsolete test project removed, Pic18/station remnants removed, configuration centralized (`CoreConfiguration`), editable settings page, host detection + init page UI (status dots, larger log, 10 s re-detection), core unit tests.
- Increment 6 step 1-2c: Koploper/ECoS reconnaissance (`docs/koploper-interface.md`), `AmplifierSpeedMapper` (neutral 399, forward 400..799, reverse 398..1, never 0), `BlockTopology`, and the new non-UI `SiebwaldeApp.Integration` project with `TrackAmplifierHardwareBackend`.
- Live Koploper session captured; emulator host now tees console output to `Logging\<date>_EcosEmuTrace.txt`.

Verified live against Koploper:

- `create(10,name["..."],protocol[DCC28],addr[N],append)` -> emulator assigns ids 1002, 1003, ...
- Driving: `set(<ecosId>, speedstep[<n>])` ramping over time; direction via `set(<ecosId>, dir[...])`.
- Occupancy to Koploper: `TX: <EVENT 100>` + `100 state[0x...]` (module 100, 16 inputs, bitmask).
- `[EXT] Loc N -> Block M` is the **current** block; no destination/route is transmitted (`desc="Route onbekend"`).
- Startup dependency: with an empty loco list Koploper reports 0 locos, sends no `create`, and drives nothing.
- Causality: in the simulator occupancy is derived from Koploper position (inverted); on real hardware the amplifier occupancy is the source.

Known issues / next steps:

1. Duplicate locos exist (seeded 1000/1001 + Koploper-created 1002/1003 for addresses 1/2). Seeding was a temporary workaround; the correct route is to let Koploper create them.
2. Add a block-adjacency / chain list to `BlockTopology` (Koploper sends no destination).
3. Add a switch mapping list (real switch <-> Koploper designation + default init state).
4. Implement the look-ahead fallback (one block ahead) in `TrackAmplifierHardwareBackend`.
5. Feed amplifier occupancy to `IHardwareFeedbackSink.OnSensorChangedAsync` (real-system source).
6. Backend selection: real (`TrackAmplifierHardwareBackend`) vs `TrackSimulatorBackend`.
7. Divergence check with ECoS stop command and diagnostics logging (maybe a dedicated diagnostics agent).
8. Settings-page extension for the mapping tables (product owner will supply a Koploper screenshot).

Tests: `dotnet test` 53/53 passing. `Logging/` (16.4 MB of runtime logs, including `locos.json`) is untracked and intentionally not committed. No pull request has been created yet.

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

Fiddle Yard and the active ModBus `TrackControllerCommands` were not touched. The Page-Removed legacy XAML leftovers (`TrackAmplifierItemView.xaml`, `TrackAmplifierManualControlView.xaml`, `TrackControlView.xaml`) and their empty ViewModels were also removed afterwards, together with their csproj entries.

## Application Guide Created (2026-09-11)

`docs/application-guide.md` now exists and is indexed in `docs/README.md`. It covers the system overview, C# project structure, entry points, IoC, startup, track control, Fiddle Yard, ECoS emulator, the planned Koploper control loop, configuration/endpoints, build/run, current status, and a glossary. It marks planned versus verified behavior.

## Increment 4 Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Added `SiebwaldeApp.Core.CoreConfiguration` as the single source of truth for core configuration values.
- Added `CoreSettings` entries `TrckIpAddress` (`192.168.1.193`) and `TrackAmplifierFwPath`, and corrected `TrckSendingPort`/`TrckReceivingPort` defaults from `60000` to `10000`/`10001`.
- `SiebwaldeApplicationModel` and `SiebwaldeApp.Core.Host/Program.cs` now use `CoreConfiguration` instead of hard-coded values.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.Core.Host`: 0 errors.
- `SiebwaldeApp.sln`: 0 errors.
- No hard-coded track IP or firmware path remains in active startup code.

Note: the WPF `App.config` does not yet carry the `CoreSettings` section; the WPF app relies on the Designer defaults (behavior unchanged). The settings UI (edit/default/undo) is still planned.

## Increment 5A Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Added `SiebwaldeApp.Core/Diagnostics/HostDetection.cs`: ping for FiddleYard (`FIDDLEYARD`) and the TrackController (`CoreConfiguration.TrackControllerIpAddress`), TCP connect for Koploper (`127.0.0.1:5700`).
- Reworked `SiebwaldeInitPageViewModel`: per-host status, human-readable log, `DetectHosts`, `InitAllControllers`, `InitTrackController`, `InitFiddleYardController`, and `InitFiddleYardSimulator` commands; start buttons guarded by detection.
- Reworked `SiebwaldeInitPage.xaml`: Detect button, TrackController/FiddleYard/Koploper status rows, start buttons, and a FiddleYard simulator button.
- `FiddleYardController.StartFiddleYardControllerAsync(bool forceSimulator = false)` and `SiebwaldeApplicationModel.StartFYController(bool forceSimulator = false)` support the operator-activated simulator.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.sln`: 0 errors.
- `SiebwaldeApp.Core.Host`: 0 errors.

Only FiddleYard has a simulator option; Koploper and TrackController do not (per product owner). Host detection was not run against live hosts.

## Increment 5B Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Implemented `SiebwaldeSettingsPage` + `SiebwaldeSettingsPageViewModel`: editable core configuration values, `Save`, `Reload`, per-entity reset (`ResetTrack`, `ResetFiddleYard`, `ResetLogging`), and `Undo` bound to Ctrl-Z.
- Changed the editable settings to User scope with setters (`LogDirectory`, `FYSendingport`, `FYReceivingport`, `TrckSendingPort`, `TrckReceivingPort`, `TrckIpAddress`, `TrackAmplifierFwPath`) and moved their config entries to the `userSettings` sections in the WPF `App.config` and the Core `app.config`.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.sln`: 0 errors.
- `SiebwaldeApp.Core.Host`: 0 errors.

Settings persistence and page behavior are code-inspected only; not runtime-verified.

## Init Page UI Improvements (Designer, 2026-09-11)

Delegated to the Designer agent and verified by the Project Lead:

- Larger logging text (`FontSizeLarge`) in the init page log.
- Colored host status dots (`Styles/Indicators.xaml` + `ValueConverters/HostStatusBrushConverter.cs`): amber while checking, green when present, gray when absent.
- Live "Detecting hosts..." indicator using the existing `SpinningText` style.
- Automatic host re-detection every 10 seconds (`DispatcherTimer`), overlap-guarded, with change-driven logging; timer stopped on page `Unloaded` to avoid leaking the transient view model.
- `App.xaml` merges `Styles/Indicators.xaml`.

Verified by `dotnet build "SiebwaldeApp\SiebwaldeApp.sln" -c Debug`: 0 errors. All referenced resources exist. Visual result not runtime-verified.

## App Run Check And Visual Verification (2026-09-11)

`SiebwaldeApp.exe` (Debug) was launched several times: process `SiebwaldeApp` started, window title `Siebwalde Application`, and `Logging\17-09-2026_SiebwaldeApp.CoreLog.txt` recorded a clean startup (PC MAC `18C04D94A26D`, PC IP `192.168.1.13`). No exceptions.

Verified at runtime:
- Host detection works: TrackController `192.168.1.193` reported Present (ping OK); FiddleYard and Koploper Absent in this environment.
- The TrackController page can manually drive PWM on the 4 detected amplifiers and shows the master/amplifier communication overview.
- The init page was captured as a screenshot (PowerShell window capture) and inspected. The first implementation clipped the button text ("etect hosts", "TrackContr", "art FiddleYa", "leYard simu") because the buttons and status columns were too narrow. Fixed by switching the rows to `Auto`/`*` columns, `MinWidth` buttons with `FontSizeSmall`, and wrapping status text; re-verified visually.

Process note: the Designer agent had no screenshot or visual feedback, which is why it did not catch the clipping. A screenshot workflow (PowerShell `CopyFromScreen` on the app window, then reading the PNG) is available for future UI work.

The app process was stopped afterwards.

## Increment 7 Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Added `SiebwaldeApp/SiebwaldeApp.Core.Tests` (xUnit, `net8.0-windows7.0`), referencing `SiebwaldeApp.Core` and `SiebwaldeApp.EcosEmu`, and added it to `SiebwaldeApp.sln`.
- 23 tests: `TrackApplicationVariables` (PWM clamp 0..799, EmoStop bit 15, slave 0 ignored, pending-write semantics, default PWM setpoints), `TrackAmplifierInitializationServiceAsync` (step chaining, unknown initial/next step, error, Continue-then-Completed), `SimpleEcosCommandParser` (id/options, malformed input, quoted-comma limitation).

Verified: `dotnet test` -> 23/23 passed; `SiebwaldeApp.sln` builds with 0 errors.

Findings recorded while testing:
- A fresh `TrackAmplifierWriteData` starts at `Hr0Value` 0, so requesting PWM 0 on a fresh amplifier is treated as "no change" and is not queued (documented by a test).
- `TrackApplicationVariables` assigns the same `HoldingReg` array instance to all 56 `trackAmpItems` (aliasing); recorded in `docs/backlog.md` for investigation.

Still open: window/UI-model tests, `SendNextFwDataPacket` send-path tests, `SimpleEcosBackend`/`JsonLocoRepository` tests.

## Increment 6 Step 1: Koploper Protocol Reconnaissance (2026-09-11)

Documented in `docs/koploper-interface.md`:

- Port roles resolved: `15471` = `EcosEmulatorServer` (Koploper connects TO C# with ECoS commands); `5700` = Koploper external info (C# connects TO Koploper for locomotive-to-block positions).
- ECoS commands handled: `set`, `get`, `queryObjects`, `request`, `release`, `create`, `delete`. Locomotive control arrives as `set(id, speed[...])` / `set(id, dir[...])` / `set(id, func[i,v])` and forwards to `IHardwareBackend.SetLocoSpeed(address, speed, direction)`.
- Position records: `0x1B`-separated, 5 fields (`&loco`, block, modelTime, pcTime, description).
- Seams: `IHardwareBackend`, `IHardwareFeedbackSink`, `IBlockPositionProvider`.
- Proposed design and open questions recorded (speed range, sensor-id mapping, look-ahead rules, backend replacement).

Next: step 2 (translation-layer design/implementation) and step 3 (4-amplifier test layout). No Koploper runtime test performed yet.

## Resume Instructions

1. Restart OpenCode from `C:\Localdata\Siebwalde` and select the `project-lead` agent.
2. Continue from `docs/product.md`, `human_input.md`, and `docs/analysis-coverage.md`.
3. Increments 1-3 are implemented and verified; propose the next increment (configuration authority/settings UI, or the application guide) for approval.
4. `docs/application-guide.md`. - DONE (2026-09-11): created.
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

1. Create `docs/application-guide.md` (still missing). - DONE (2026-09-11): guide created and indexed.
2. Revalidate prior .NET code-analysis findings against current source. - DONE (2026-09-11): confirmed; see Phase 1 Revalidation Results.
3. Product clarification rounds 1-5. - DONE (2026-09-11): remaining items are research tasks (Koploper protocol, ECoS overload semantics, topology/spreadsheet).
4. Design and implement the Koploper translation path (later increment).
5. Investigate the additional firmware/hardware/Python source areas in bounded passes.
6. Designer and integrator agents. - DONE (2026-09-11): created.
7. Propose the first fix increment for product-owner approval (Core.Host logger, init sequencing, remnant cleanup, test-project decision). - DONE (2026-09-11): Increment 1 (host logger + init sequencing) implemented and verified.
8. Remove obsolete test project. - DONE (2026-09-11): Increment 2 removed `SiebwaldeApp.Tests`; design intent archived.
9. Increment 3 (proposed): remove confirmed Pic18-era and station-era remnants from the WPF app. - DONE (2026-09-11): station-policy feature and commented Pic18 code removed; build verified.
10. Increment 4 (next): configuration authority / settings UI. The application guide is done.11. **Increment 6 (next major, on request): the Koploper translation path.** Protocol reconnaissance -> translation-layer design -> implement on the 4-amplifier test layout. See `docs/backlog.md`, section "Koploper Translation Path". Do not start before the product owner asks.
12. Treat all remaining `docs/backlog.md` items as unapproved until the user selects implementation work.
