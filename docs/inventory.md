# Workspace Inventory

## Workspace Root And Migration Note

The repository root is `C:\Localdata\Siebwalde`, a Git repository (`origin https://github.com/jeremysiebers/Siebwalde.git`). An earlier analysis pass was performed in a pre-migration copy that contained only the three managed .NET directories. The current workspace is broader: it also contains microcontroller firmware, PCB hardware sources, Python tooling, runtime logs, and backup projects. Structure and path statements below were verified against the current root during the migration check. Earlier code-level findings still require revalidation against current source (see the revalidation note at the end of this file).

## Top-Level Directories

| Directory | Classification | Evidence |
| --- | --- | --- |
| `SiebwaldeApp/` | Main solution root and shared active .NET source root. | `SiebwaldeApp/SiebwaldeApp.sln` includes `SiebwaldeApp`, `SiebwaldeApp.Core`, and `SiebwaldeApp.EcosEmu`. |
| `SiebwaldeApp.Core.Host/` | Separate console host for the core track application. | `SiebwaldeApp.Core.Host/SiebwaldeApp.Core.Host.sln` references `../SiebwaldeApp/SiebwaldeApp.Core/SiebwaldeApp.Core.csproj`. |
| `SiebwaldeApp.EcosEmu/` | Separate ECoS emulator host solution plus an old source copy. | `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.sln` references `../SiebwaldeApp/SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.csproj`. |
| `TrackAmplifier4.X/` | Track amplifier firmware (PIC18F25K40, PetitModbus) and its own agent guidance. | Contains `main.c`, `processio.c`, `regulator.c`, `modbus/`, and `AGENT_TRACK_AMPLIFIER*.md`. |
| `TrackAmplifierBootLoader.X/` | Track amplifier bootloader firmware and Python tooling. | Contains `mcc_generated_files/`, `nbproject/`, `Python/`. |
| `TrackBackplane2.X/` | Track backplane slave firmware. | Contains `main.c`, `modbus/`, `mcc_generated_files/`. |
| `TrackController5/` | PIC32MZ track controller firmware, Python tooling, and vendor PDFs. | Contains `firmware/`, `Python/`, `Fat PIC32/`. |
| `ServoController.X/` | RC servo driving firmware. | Contains `main.c` variants, `mcc_generated_files/`, `nbproject/`. |
| `FiddleYard/` | PIC18F97J60 embedded ethernet controller firmware for the Fiddle Yard. | Contains `FiddleYard.c`, `State_Machine.c`, `Mip50_API.c`, `TCPIPConfig.h`. |
| `Faller_Car_ucontroller2.X/` | Faller Car System microcontroller firmware. | Contains `mcc_generated_files/`, `DOC/`, `nbproject/`. |
| `Faller_Car_uControllerBootLoader.X/` | Faller Car System bootloader firmware and Python tooling. | Contains `mcc_generated_files/`, `Python/`. |
| `YardController.X/` | Yard controller firmware. | Contains `main.c`, `bus.c`, `mcc_generated_files/`. |
| `KiCad/` | PCB hardware design sources. | Multiple KiCad project directories such as `TrackAmplifier2/`, `TrackModBusMaster_piggyback/`. |
| `Common_Files/` | Shared ModBus mapping reference material. | `Common_Files/ModBusSlaveMasterMappings/`. |
| `Ecos ESU info/` | Vendor reference material. | 5 files; not line-by-line analyzed. |
| `Backup projects/` | Archived or backup firmware projects. | `ServoConverter.X/`, `TrackControllerPic18.X/`, `YardController_IOX.X/`. |
| `Logging/` | Runtime log output and ECoS locomotive persistence. | `Logging/*.txt`, `Logging/locos.json`. |
| `docs/` | Analysis, product, and handoff documentation. | This directory. |
| `.opencode/` | OpenCode agent definitions and tooling (`node_modules` vendored). | `.opencode/agents/*.md`. |

## Solutions And Projects

| Path | Type | Target framework | Role |
| --- | --- | --- | --- |
| `SiebwaldeApp/SiebwaldeApp.sln` | Visual Studio solution | Mixed | Main solution. |
| `SiebwaldeApp/SiebwaldeApp/SiebwaldeApp.csproj` | Windows desktop app | `net8.0-windows` | WPF application with Windows Forms integration. |
| `SiebwaldeApp/SiebwaldeApp.Core/SiebwaldeApp.Core.csproj` | Class library | `net8.0` | Core/domain logic, track application, Fiddle Yard logic, logging, services. |
| `SiebwaldeApp/SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.csproj` | Class library | `net8.0-windows7.0` | Active ECoS emulator library. |
| `SiebwaldeApp/SiebwaldeApp.Tests/SiebwaldeApp.Tests.csproj` | xUnit test project | `net8.0-windows7.0` | Tests for station-domain concepts; not included in `SiebwaldeApp.sln`. |
| `SiebwaldeApp.Core.Host/SiebwaldeApp.Core.Host.sln` | Visual Studio solution | Mixed | Host solution for `SiebwaldeApp.Core.Host` and `SiebwaldeApp.Core`. |
| `SiebwaldeApp.Core.Host/SiebwaldeApp.Core.Host.csproj` | Console app | `net8.0-windows7.0` | Console host for track initialization. |
| `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.sln` | Visual Studio solution | Mixed | Host solution for `SiebwaldeApp.EcosEmu.Host` and active emulator library. |
| `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.Host/SiebwaldeApp.EcosEmu.Host.csproj` | Console app | `net8.0-windows7.0` | Console host for the ECoS emulator. |
| `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu_old/SiebwaldeApp.EcosEmu.csproj` | Class library | `net9.0` | Potentially obsolete or experimental duplicate, not referenced by discovered solutions. |

## Languages And UI Technologies

- C# is the application language across all active projects.
- XAML is used for WPF pages, controls, styles, and `App.xaml` in `SiebwaldeApp/SiebwaldeApp`.
- Windows Forms is enabled by `UseWindowsForms` in `SiebwaldeApp/SiebwaldeApp/SiebwaldeApp.csproj` and used through Fiddle Yard WinForms integration types.
- XML is used for app settings, `FodyWeavers.xml`, project files, and NuGet package metadata.

## Active Application Source

| Area | Paths | Evidence |
| --- | --- | --- |
| Desktop shell | `SiebwaldeApp/SiebwaldeApp/App.xaml.cs`, `MainWindow.xaml(.cs)`, `ViewModel/`, `Pages/`, `Controls/`, `Styles/` | Included by desktop project and loaded from `App.OnStartup`. |
| Core services/logging | `SiebwaldeApp/SiebwaldeApp.Core/IoC/`, `Services/`, `Logging/`, `File/`, `Async/` | Included by core project and used by core/application model. |
| Track application | `SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/` | `SiebwaldeApplicationModel.StartTrackApplication` composes these types. |
| Fiddle Yard application | `SiebwaldeApp/SiebwaldeApp.Core/Model/FiddleYardApplication/` | `SiebwaldeApplicationModel.StartFYController` creates `FiddleYardController`. |
| Fiddle Yard simulator | `SiebwaldeApp/SiebwaldeApp.Core/Model/FiddleYardSimulator/` | `FiddleYardIOHandle.Init` can attach simulator data instead of real receiver data. |
| Active ECoS emulator | `SiebwaldeApp/SiebwaldeApp.EcosEmu/` | Referenced by `SiebwaldeApp.EcosEmu.Host` and main desktop app project. |

## Entry Points

| Entry point | Path | Behavior |
| --- | --- | --- |
| `App.OnStartup` | `SiebwaldeApp/SiebwaldeApp/App.xaml.cs` | Sets up UI IoC/logging and shows `MainWindow`. |
| `Program.Main` | `SiebwaldeApp.Core.Host/Program.cs` | Wires track communication and initialization in a console host. |
| `Program.Main` | `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.Host/Program.cs` | Starts Koploper external info, track simulator backend, ECoS backend, and TCP server. |
| `SiebwaldeInitPageViewModel.InitTrackController` | `SiebwaldeApp/SiebwaldeApp/ViewModel/SiebwaldeViewModels/SiebwaldeInitPageViewModel.cs` | Calls `IoC.siebwaldeApplicationModel.StartTrackApplication`. |
| `SiebwaldeInitPageViewModel.InitFiddleYardController` | `SiebwaldeApp/SiebwaldeApp/ViewModel/SiebwaldeViewModels/SiebwaldeInitPageViewModel.cs` | Calls `IoC.siebwaldeApplicationModel.StartFYController`. |

## Dependencies

| Dependency | Evidence | Role |
| --- | --- | --- |
| `Ninject` `3.3.6` | `SiebwaldeApp/SiebwaldeApp/SiebwaldeApp.csproj` | UI service locator and view model binding. |
| `Fody` and `PropertyChanged.Fody` | `SiebwaldeApp/SiebwaldeApp/SiebwaldeApp.csproj`, `FodyWeavers.xml` | Property change weaving for the desktop app. |
| `System.Configuration.ConfigurationManager` `8.0.0` | `SiebwaldeApp/SiebwaldeApp/SiebwaldeApp.csproj`, `SiebwaldeApp/SiebwaldeApp.Core/SiebwaldeApp.Core.csproj` | App settings access. |
| `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` | `SiebwaldeApp/SiebwaldeApp.Tests/SiebwaldeApp.Tests.csproj` | Test infrastructure. |

## Configuration And Persistence

| Path or location | Type | Evidence |
| --- | --- | --- |
| `SiebwaldeApp/SiebwaldeApp/App.config` | UI settings | Contains log directory, Fiddle Yard ports, track ports, MIP50 app settings. |
| `SiebwaldeApp/SiebwaldeApp.Core/app.config` | Core settings | Contains `CoreSettings` defaults including `LogDirectory`, Fiddle Yard ports, and track ports. |
| `SiebwaldeApp/SiebwaldeApp.Core/Properties/CoreSettings.Designer.cs` | Generated settings wrapper | Defaults include `LogDirectory`, `FYSendingport`, `FYReceivingport`, `TrckSendingPort`, `TrckReceivingPort`, `UseFakeHardwareAdapters`. |
| `C:\Localdata\Siebwalde\Logging\` | External log directory | Referenced by settings and logging code. |
| `C:\Localdata\Siebwalde\Logging\locos.json` | ECoS locomotive JSON persistence | Used by `JsonLocoRepository` in emulator host. |
| `C:\Localdata\Siebwalde\TrackAmplifier4.X\dist\Offset\production\TrackAmplifier4.X.production.hex` | Firmware input | Hard-coded as `FwPath` in `SiebwaldeApplicationModel` and core host. |

## External Communication

| Component | Endpoint or protocol | Evidence |
| --- | --- | --- |
| Track amplifier controller | UDP to `192.168.1.193:10000`, local port `10001` | `SiebwaldeApplicationModel.StartTrackApplication`, `SiebwaldeApp.Core.Host/Program.cs`, `RawUdpTransport`. |
| Track amplifier protocol | Frame header `0xAA`, slave info `0xFF`, command/control payloads | `PublicEnums.cs`, `TrackCommClientAsync`. |
| Fiddle Yard controller | DNS/name `FIDDLEYARD`, UDP ports `28671` and `28672` | `FiddleYardController`, `CoreSettings`. |
| ECoS emulator | TCP loopback port `15471` | `SiebwaldeApp.EcosEmu.Host/Program.cs`, `EcosEmulatorServer`. |
| Koploper external info | TCP `127.0.0.1:5700` | `KoploperExternalInfoClient`. |

## Tests And Diagnostics

- `SiebwaldeApp/SiebwaldeApp.Tests` contains xUnit tests, test doubles, and `IoCTestBootstrap`.
- The tests appear stale or ahead of source because station-domain symbols referenced by tests were not found in active source.
- `dotnet test ... --no-build --no-restore` failed because the expected test DLL was not present.
- Logging uses `FileLogger`, `DebugLogger`, `ConsoleLogger`, and `BaseLogFactory` in core; emulator diagnostics mostly use `Console.WriteLine`.

## Generated, Vendored, Duplicate, And Experimental Content

| Area | Classification | Evidence |
| --- | --- | --- |
| `bin/`, `obj/` | Generated build output | Contains generated assemblies, assets, runtime configs, and SourceLink files. |
| `.vs/` | Visual Studio IDE output | Contains `.suo`, file indices, and document layout caches. |
| `SiebwaldeApp/packages/` and nested package folders | Vendored NuGet artifacts | Contains Fody, Ninject, and PropertyChanged package files. |
| `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu_old/` | Potentially obsolete or experimental duplicate | Folder name includes `_old`; project targets `net9.0`; no discovered solution references it. Do not delete without user approval. |
| `SiebwaldeApp/SiebwaldeApp.zip` | Archive | Role not inspected. Treat as preserved input, not active source unless proven otherwise. |

## Revalidation Note

The .NET application findings in this file were produced before the workspace was confirmed as the full Git repository. Project references, solution membership, target frameworks, and file paths were re-checked during the migration check, but the following claims still require revalidation against current source before being treated as verified:

- Missing station-domain symbols referenced by `SiebwaldeApp.Tests`.
- Missing `IoC.Kernel` in `SiebwaldeApp.Core.IoC` used by `SiebwaldeApp.Core.Host`.
- The exact set of test failures and build risks, which were derived from a `--no-build --no-restore` check only.

No revalidation build or test was run during the migration check.

