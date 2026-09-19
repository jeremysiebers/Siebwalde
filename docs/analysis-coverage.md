# Analysis Coverage

This file tracks investigation coverage for the Siebwalde workspace. Status values: Not started, In progress, Investigated, Partially investigated, Excluded.

## Product Clarification Status

The product owner confirmed component roles and priorities on 2026-09-11 (see `docs/product.md` and `human_input.md`). Confirmed at product level: the active C# application is `SiebwaldeApp` + `SiebwaldeApp.Core` + `SiebwaldeApp.EcosEmu`; `TrackController5`, `TrackAmplifier4.X`, `TrackAmplifierBootLoader.X`, and `TrackBackplane2.X` are active; `TrackControllerPic18.X` is obsolete; `FiddleYard` is deferred; `YardController_IOX.X` will be split later. Also confirmed: the Koploper/ECoS protocol is derived from `Ecos ESU info` and code; MMDC responsibility split; Yard handover; Faller Car as an experiment; a shuttle line (pendelbaan). These are product-owner statements; their code-level behavior still requires verification.

## Current Continuation Pass

| Area | Status | Evidence | Remaining gaps |
| --- | --- | --- | --- |
| Original deliverable completeness review | In progress | User requested continuation and gap review after initial investigation. | Complete `docs/application-guide.md`, `docs/product.md`, persistent knowledge workflow updates, subagent cross-checks, and safe build verification. |
| Human-readable application guide | In progress | `docs/application-guide.md` did not exist at the start of this continuation pass. | Create coherent guide with diagrams and verified workflows. |
| Product requirements | In progress | `docs/product.md` did not exist at the start of this continuation pass. | Separate observed implementation, user requirements, priorities, acceptance criteria, ideas, assumptions, and open questions. |
| Build and source-claim verification | In progress | Build artifact generation is now allowed for safe offline verification. | Inspect build targets/scripts, run allowed offline builds/tests without restore/install/hardware connections, and update results. |

## Workspace-Level Coverage

| Area | Status | Evidence | Remaining gaps |
| --- | --- | --- | --- |
| Workspace root | Investigated | `C:\Localdata\Siebwalde` is a Git repository (`origin https://github.com/jeremysiebers/Siebwalde.git`). It contains the managed .NET application plus firmware, hardware, Python tooling, logs, and backups. | None for top-level structure; see additional source areas below. |
| Pre-migration copy | Investigated | `C:\Users\jerem\Downloads\Test` still exists and holds a subset (`AGENTS.md`, `.opencode/`, `docs/`, and the three .NET directories). | Treat as superseded; do not edit as the active workspace. |
| Existing AI instructions | Investigated | Root `AGENTS.md` exists and was updated during the migration check. Existing context file: `SiebwaldeApp/SiebwaldeApp_project_context.md`. | None known. |
| Existing OpenCode configuration | Investigated | `.opencode/agents/*.md` exist; `.opencode/node_modules/` is vendored tooling. | Newly created agents require a new OpenCode session to load. |
| Existing documentation | Investigated | `docs/*.md`; `SiebwaldeApp/SiebwaldeApp_project_context.md`; `TrackAmplifier4.X/*.md`; vendored package readme under `SiebwaldeApp/packages/Fody.6.9.3/package_readme.md`. | `docs/product.md` now created; application guide still missing. |
| Documentation links and project references | Investigated | All discovered `.sln` files and `ProjectReference` entries are relative and resolve from the current root. | None known. |
| Absolute paths to the pre-migration directory | Investigated | Old-path references were found only in `docs/` and were updated. Generated firmware metadata still contains historical absolute paths and is excluded. | Generated MPLAB output intentionally left unchanged. |

## Additional Source Areas (Inventoried, Not Deeply Analyzed)

| Area | Status | Classification | Remaining gaps |
| --- | --- | --- | --- |
| `TrackAmplifier4.X/` | Not started (inventoried) | Track amplifier firmware with its own agent docs. | Deep firmware analysis; align with its own `*.md` guidance. |
| `TrackAmplifierBootLoader.X/` | Not started (inventoried) | Bootloader firmware and Python tooling. | Deep analysis. |
| `TrackBackplane2.X/` | Not started (inventoried) | Backplane slave firmware. | Deep analysis. |
| `TrackController5/` | Not started (inventoried) | PIC32MZ controller firmware, Python tooling, vendor PDFs. | Deep analysis. |
| `ServoController.X/` | Not started (inventoried) | Servo driving firmware. | Deep analysis. |
| `FiddleYard/` | Not started (inventoried) | PIC18F97J60 ethernet controller firmware. | Deep analysis; relates to core `FiddleYardApplication`. |
| `Faller_Car_ucontroller2.X/`, `Faller_Car_uControllerBootLoader.X/` | Not started (inventoried) | Faller Car System firmware and bootloader. | Deep analysis. |
| `YardController.X/` | Not started (inventoried) | Yard controller firmware. | Deep analysis. |
| `KiCad/` | Not started (inventoried) | PCB hardware design sources. | Optional; hardware reference only. |
| `Common_Files/`, `Ecos ESU info/` | Not started (inventoried) | ModBus mappings and vendor reference material. Product owner confirmed `Ecos ESU info` holds the ECoS/Koploper protocol data to derive from. | Derive and document the Koploper/ECoS protocol and port roles from `Ecos ESU info` plus `SiebwaldeApp.EcosEmu` code. |
| `Backup projects/` | Not started (inventoried) | Archived/backup firmware projects. Product owner confirmed `TrackControllerPic18.X` is obsolete and its C# remnants must be removed. | Decide keep/archive policy for the remaining backups. |
| `Logging/` | Partially investigated | Runtime logs and `locos.json`; confirms the runtime path `C:\Localdata\Siebwalde`. | Log content not systematically analyzed. |

## Directories And Projects

| Directory or project | Status | Classification | Evidence | Remaining gaps |
| --- | --- | --- | --- | --- |
| `SiebwaldeApp/` | Investigated | Main application solution and shared source root. | `SiebwaldeApp/SiebwaldeApp.sln` includes `SiebwaldeApp`, `SiebwaldeApp.Core`, and `SiebwaldeApp.EcosEmu`. | Archive file `SiebwaldeApp.zip` role remains uninspected. |
| `SiebwaldeApp/SiebwaldeApp` | Investigated | WPF/Windows Forms desktop application. | `SiebwaldeApp/SiebwaldeApp/SiebwaldeApp.csproj` targets `net8.0-windows`, `UseWPF`, and `UseWindowsForms`; `App.OnStartup` traced. | Detailed page-by-page UI behavior not documented. |
| `SiebwaldeApp/SiebwaldeApp.Core` | Investigated | Core/domain library. | `SiebwaldeApp/SiebwaldeApp.Core/SiebwaldeApp.Core.csproj` targets `net8.0`; track and Fiddle Yard workflows traced. | Some deep Fiddle Yard state branches remain summarized. |
| `SiebwaldeApp/SiebwaldeApp.EcosEmu` | Investigated | ECoS emulator library. | `SiebwaldeApp/SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.csproj` targets `net8.0-windows7.0`; server/backend/simulator traced. | Exact full ECoS command coverage remains summarized. |
| `SiebwaldeApp/SiebwaldeApp.Tests` | Removed | Removed 2026-09-11 (Increment 2) as an obsolete remnant of the abandoned station-in-C# approach. | None; recoverable from git commit `104c1e6`. |
| `SiebwaldeApp.Core.Host/` | Investigated | Console host for core startup and track initialization. | `SiebwaldeApp.Core.Host/SiebwaldeApp.Core.Host.sln` references `SiebwaldeApp.Core`; `Program.Main` traced. | Full build not run; build risk recorded. |
| `SiebwaldeApp.EcosEmu/` | Investigated | Separate emulator host solution plus obsolete or experimental copy. | `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.sln` references host and shared emulator project under `SiebwaldeApp`. | User decision needed for `_old` copy. |
| `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.Host` | Investigated | Console host for ECoS emulator. | `Program.Main` wires external info, loco repository, simulator, backend, and server. | Full build not run. |
| `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu_old` | Partially investigated | Potentially obsolete or experimental emulator source copy. | Folder name includes `_old`; its project targets `net9.0`; not referenced by discovered solution files. | No line-by-line comparison with active emulator library. |
| Generated outputs: `bin/`, `obj/`, `.vs/` | Excluded | Generated build and IDE output. | Paths under `bin`, `obj`, `.vs`; generated assemblies/assets/cache files. | Document role only; no line-by-line analysis planned. |
| Vendored packages: `packages/` | Excluded | Vendored NuGet package contents. | `SiebwaldeApp/packages/*` contains package artifacts and package documentation. | Document dependency role only; no line-by-line analysis planned. |

## Meaningful Modules

| Module | Status | Owner | Evidence | Remaining gaps |
| --- | --- | --- | --- | --- |
| WPF application shell and navigation | Investigated | Project Lead | `SiebwaldeApp/SiebwaldeApp/App.xaml`, `MainWindow.xaml`, `ViewModel/`, `Pages/`, `Controls/`. | Detailed page-by-page UI behavior excluded from this pass. |
| Dependency injection and services | Investigated | Developer | `SiebwaldeApp/SiebwaldeApp/IoC/IoC.cs`, `SiebwaldeApp/SiebwaldeApp.Core/IoC/IoC.cs`, `Services/`, `Logging/`. | Source-level logger unification not designed. |
| Track application communication | Investigated | Developer | `TrackCommClientAsync`, `ITrackTransport`, `RawUdpTransport`, `RawUdpTrackTransport`, `TrackApplicationVariables`. | Runtime hardware behavior not tested. |
| Track amplifier initialization | Investigated | Architect | `TrackAmplifierInitializationServiceAsync`, `IInitializationStep`, initialization step classes. | Runtime hardware behavior not tested. |
| Legacy track control orchestration | Investigated | Architect | `TrackControlMain`, `SiebwaldeApplicationModel`. | Full runtime behavior not tested. |
| Fiddle yard application | Partially investigated | Architect | `FiddleYardController`, `FiddleYardIOHandle`, `FiddleYardApplication`. | Deep state-by-state behavior summarized, not exhaustive. |
| Fiddle yard simulator | Partially investigated | Developer | `SiebwaldeApp/SiebwaldeApp.Core/Model/FiddleYardSimulator`. | Simulator internals summarized indirectly through Fiddle Yard flow. |
| ECoS emulator protocol/server/backend | Investigated | Architect | `EcosEmulatorServer`, `SimpleEcosCommandParser`, `SimpleEcosBackend`, `TrackSimulatorBackend`, `KoploperExternalInfoClient`, `JsonLocoRepository`. | Exact command grammar edge cases not tested. |
| Tests and test doubles | Removed | Project Lead | `SiebwaldeApp/SiebwaldeApp.Tests` was removed 2026-09-11 (obsolete station-domain tests). | New unit tests for the window/program model are still to be added. |

## Revalidation Note

All .NET application findings above were produced before the workspace was confirmed as the full Git repository at `C:\Localdata\Siebwalde`.

Revalidated on 2026-09-11 against current source (source inspection only, no build):

- CONFIRMED: missing `IoC.Kernel` in `SiebwaldeApp.Core.IoC` used by `SiebwaldeApp.Core.Host`.
- CONFIRMED then REMOVED (Increment 2): missing station-domain symbols and `IoC.Kernel`/Ninject usage in `SiebwaldeApp.Tests`; the obsolete project was deleted.
- CONFIRMED: initialization step sequencing (`SetDefaultPwmSetpointsStep` skipped; mismatched next-step name).
- CONFIRMED: hard-coded endpoints and firmware path; `CoreSettings` is not authoritative for the track transport.
- CONFIRMED: Pic18-era commented remnants and station-era UI remnants in the C# project.

Still not re-verified: the Fiddle Yard error paths, `SendNextFwDataPacket` await behavior, `TrackCommClientAsync` publish interval comment, and ECoS emulator multi-client behavior. Build/test execution is still pending authorization.

Increment 1 (2026-09-11) fixed two of the confirmed items: the `SiebwaldeApp.Core.Host` logger setup and the initialization step sequencing. See `docs/build-test.md` for build results.

## Increment 6 Coverage (2026-09-19)

| Area | Status | Evidence | Remaining gaps |
| --- | --- | --- | --- |
| Koploper/ECoS protocol | Investigated | `docs/koploper-interface.md`; live session trace `Logging\19-09-2026_EcosEmuTrace.txt`. | Edge cases; MMDC. |
| Speed -> PWM | Implemented + tested | `AmplifierSpeedMapper`, `AmplifierSpeedMapperTests`. | None known. |
| Routing + look-ahead | Implemented + tested | `BlockTopology`, `LookAheadPlanner`, `TrackAmplifierHardwareBackend`, tests. | Switch branch selection provisional. |
| Koploper block mapping | Implemented + tested | `KoploperBlockMap`, `KoploperBlockMapTests`; oval mapping from the Koploper export. | Real-layout mapping not yet entered. |
| Amplifier occupancy | Implemented + tested | `TrackAmplifierRegisters`, `TrackAmplifierOccupancyProvider`, tests. | Firmware still has a TODO for the occupied flag. |
| Occupancy -> Koploper bridge | Implemented + tested | `TrackAmplifierOccupancyBridge`, tests. | Not yet wired into the running app. |
| App composition (option A) | Completed | `TrackControlIntegration`; `TrackControlHost` owns the ECoS server (15471) and the Koploper external-info client (5700); `SiebwaldeApplicationModel` starts/stops it. | Mode switching while running is intentionally not supported. |
| Unit tests | Implemented | `SiebwaldeApp.Core.Tests`, 193 tests passing. | UI-model and other areas still uncovered. |
| Divergence and safety (item 4) | Completed | `ControlDiagnostics`, `ControlDiagnostic`, `ControlSafetyGuard`, `DivergenceChecker`, `EcosHardwareStopSink`; software-only safety scenario through port 15471. | Real-mode feedback unavailable, so real route checks cannot confirm anything yet. |
| Mapping settings (item 5) | Completed | `BlockTopologyConfig`/`KoploperBlockMapConfig` in both `app.config` files, settings page with undo + reset, line-break-tolerant parsers, blank values fall back to the declared default. | Real-layout values still to be entered. |
| Switch mapping (item 3) | Completed | `SwitchMapping`, `SwitchController`, `SwitchTranslatingHardwareBackend`, `SwitchMapConfig`; proven `3>4@1:0` / `3>5@1:1`; software-only port 15471 switch validation. | Real accessory-decoder output path and the real rest positions are still missing. |
| ECoS host startup wiring | Completed | `TrackControlMode`, `IEcosHostService`, `EcosHostStartResult`, `TrackControlHost`, `TrackControlHostTests`; software-only port 15471 validation; explicit mode-transition semantics. | Not yet exercised against real hardware (by design). |



