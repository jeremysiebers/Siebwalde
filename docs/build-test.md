# Build And Test Notes

## Runtime-Tested Environment

Command executed from the pre-migration workspace (`C:\Users\jerem\Downloads\Test`; the active repository root is now `C:\Localdata\Siebwalde`):

```powershell
dotnet --info
```

Observed environment:

- .NET SDK `9.0.318`.
- MSBuild `17.14.51`.
- OS platform `win-x64`.
- .NET 8 runtime `Microsoft.NETCore.App 8.0.31` is installed.
- .NET 8 Windows Desktop runtime `Microsoft.WindowsDesktop.App 8.0.31` is installed.
- No `global.json` was found.

## Prerequisites

- Windows is required for the WPF/Windows Forms desktop project.
- A .NET SDK capable of building .NET 8 projects is required. The inspected machine has .NET SDK 9.0 and .NET 8 runtimes.
- Network/hardware-dependent flows expect the track controller, Fiddle Yard controller, ECoS/Koploper, or simulator substitutes to be available depending on the host.
- Firmware-dependent track initialization expects `C:\Localdata\Siebwalde\TrackAmplifier4.X\dist\Offset\production\TrackAmplifier4.X.production.hex`.
- Logging and ECoS persistence use `C:\Localdata\Siebwalde\Logging\`.

## Commands Not Fully Executed In This Assignment

The following commands are the appropriate build/test commands based on project files, but they were not fully executed during this documentation-only assignment because `dotnet build` and normal `dotnet test` can update `bin/` and `obj/` outputs.

```powershell
dotnet restore "SiebwaldeApp\SiebwaldeApp.sln"
dotnet build "SiebwaldeApp\SiebwaldeApp.sln" -c Debug
```

```powershell
dotnet build "SiebwaldeApp.Core.Host\SiebwaldeApp.Core.Host.sln" -c Debug
dotnet build "SiebwaldeApp.EcosEmu\SiebwaldeApp.EcosEmu.sln" -c Debug
```

The former test project `SiebwaldeApp/SiebwaldeApp.Tests` was removed on 2026-09-11 (Increment 2) as an obsolete remnant; it was in no solution and could not compile. There is currently no active test project.

## Former Test Check (Historical)

Command executed from the pre-migration workspace (`C:\Users\jerem\Downloads\Test`; the active repository root is now `C:\Localdata\Siebwalde`):

```powershell
dotnet test "SiebwaldeApp\SiebwaldeApp.Tests\SiebwaldeApp.Tests.csproj" --no-build --no-restore -c Debug
```

Result:

- Failed before running tests because `SiebwaldeApp/SiebwaldeApp.Tests/bin/Debug/net8.0-windows7.0/SiebwaldeApp.Tests.dll` was not found.
- This does not prove source tests fail; it only proves there was no existing built test assembly at that path.
- The project has since been removed; this entry is kept as history.

## Build Risks Revalidated Against Current Source (2026-09-11)

These findings were re-checked against the current source in the active repository. All were CONFIRMED as real compile-level or behavioral risks. No build was executed; these are source-inspection results.

- CONFIRMED: `SiebwaldeApp.Core.Host/Program.cs:25` calls `IoC.Kernel.Bind<ILogFactory>()`, but `SiebwaldeApp/SiebwaldeApp.Core/IoC/IoC.cs` exposes only `Logger` and `ConfigureLogger` (no `Kernel`). With `using SiebwaldeApp.Core;`, this is a compile error. The Ninject-style `Kernel` API exists only in the UI `SiebwaldeApp.IoC` (`SiebwaldeApp/SiebwaldeApp/IoC/IoC.cs`), and the host does not reference Ninject.
- CONFIRMED: `SiebwaldeApp/SiebwaldeApp.Tests/Infrastructure/IoCTestBootstrap.cs` uses `using Ninject;` and `IoC.Kernel.Bind/Unbind`, but `SiebwaldeApp.Tests.csproj` references only `SiebwaldeApp.Core` plus xunit (no Ninject). The test project cannot compile.
- CONFIRMED: Station tests reference symbols with no definition in `SiebwaldeApp.Core`: `StationTrack`, `TrainType`, `StationSide`, `TrackApplication`, `TrackMetadata`, `TrackRole`, `ITrackIn`, `ITrackOut`. (`TrackApplicationVariables` and `AmplifierDataEventArgs` exist; the station-domain types do not.)
- CONFIRMED: `SiebwaldeApp.EcosEmu_old` targets `net9.0` and is not referenced by discovered solutions; it is not active build coverage.
- Not re-checked: `BaseLogFactory` members used by the host (for example `LogOutputLevel`) were not verified in this pass.

## Revalidated Initialization Sequencing (2026-09-11)

- CONFIRMED: In `SiebwaldeApp/SiebwaldeApp.Core/Model/SiebwaldeApplicationModel.cs` (lines 169-181) the steps are registered as Connect, ResetAllSlaves, DataUpload, DetectSlaves, RecoverSlaves, FlashFwTrackamplifiers, `InitTrackamplifiersStep`, `SetDefaultPwmSetpointsStep`, `EnableTrackamplifiersStep`.
- CONFIRMED: `InitTrackamplifiersStep` (`.../Initialization/Steps/InitTrackamplifiersStep.cs:65`) returns `InitStepResult.Next("EnableTrackamplifiers")`, so `SetDefaultPwmSetpointsStep` is skipped.
- CONFIRMED: `SetDefaultPwmSetpointsStep` (`.../Steps/SetDefaultPwmSetpointsStep.cs:33,46`) returns `InitStepResult.Next("EnableTrackamplifiersStep")`, while `EnableTrackamplifiersStep.Name` is `"EnableTrackamplifiers"`. If the step were reached, `TrackAmplifierInitializationServiceAsync.InitializeAsync` would throw `Unknown init step: EnableTrackamplifiersStep` and fail initialization.

## Revalidated Configuration Authority (2026-09-11)

- CONFIRMED: `SiebwaldeApplicationModel.cs:23` hard-codes the firmware path, and `:140-142` hard-codes `192.168.1.193`, `10000`, and `10001`.
- CONFIRMED: `SiebwaldeApp.Core.Host/Program.cs:9,59-61` hard-codes the same values independently.
- CONFIRMED: `SiebwaldeApp/SiebwaldeApp.Core/app.config` defines `TrckSendingPort`/`TrckReceivingPort` (`10000`/`10001`), `FYSendingport`/`FYReceivingport` (`28671`/`28672`), and `LogDirectory`, but no IP address and no firmware path. Only the Fiddle Yard path uses `CoreSettings`; the track transport uses the hard-coded values.

## Revalidated Obsolete Remnants (2026-09-11)

- CONFIRMED: `SiebwaldeApp/SiebwaldeApp/App.xaml.cs` contains commented-out Pic18-era code referencing `TrackPic18UdpAdapter`, `YardPic18UdpAdapter`, `UseRealAdaptersOrThrow`, and `IoC.TrackAdapter.TrackIn/Out`.
- CONFIRMED: Station-era UI remnants exist: `StationSettingsPage.xaml.cs`, `StationSettingsPageViewModel.cs` (largely commented), `ApplicationPage.StationSettings`, and `SideMenuViewModel.StationSettingsPage`.
- CONFIRMED: `StartTrackApplication` has a stale XML doc comment mentioning "registering station tracks" that the code does not perform.
- Note: `TrackControllerCommands` and `TrackController` references in Core belong to the active ModBus controller command set, not the Pic18 remnants. They must not be removed as part of remnant cleanup.


## Build Verification After Increment 1 (2026-09-11)

Commands executed from `C:\Localdata\Siebwalde` (Debug, no hardware, no test execution):

```powershell
dotnet build "SiebwaldeApp\SiebwaldeApp.Core\SiebwaldeApp.Core.csproj" -c Debug --nologo
dotnet build "SiebwaldeApp.Core.Host\SiebwaldeApp.Core.Host.csproj" -c Debug --nologo
dotnet build "SiebwaldeApp\SiebwaldeApp.sln" -c Debug --nologo
dotnet build "SiebwaldeApp.EcosEmu\SiebwaldeApp.EcosEmu.sln" -c Debug --nologo
```

Results:

| Build | Before | After |
| --- | --- | --- |
| `SiebwaldeApp.Core` | 0 errors, 94 warnings | 0 errors, 94 warnings |
| `SiebwaldeApp.Core.Host` | 1 error (`IoC` has no `Kernel`) | 0 errors, 94 warnings |
| `SiebwaldeApp.sln` | not measured | 0 errors, 85 warnings |
| `SiebwaldeApp.EcosEmu.sln` | not measured | 0 errors, 0 warnings |

`SiebwaldeApp.Tests` was not built at Increment 1 time; it has since been removed (Increment 2).

## Build Verification After Increment 3 (2026-09-11)

Command executed from `C:\Localdata\Siebwalde` (Debug, no hardware, no test execution):

```powershell
dotnet build "SiebwaldeApp\SiebwaldeApp.sln" -c Debug --nologo
```

Result:

- 0 errors. The WPF project rebuilt after the remnant removal and reported 175 warnings.
- A targeted search found no remaining references to `StationSettingsPage`, `ApplicationPage.StationSettings`, `StationPolicy`, `TrackPic18UdpAdapter`, `YardPic18UdpAdapter`, or `TrackAmplifierItemViewModel` in the WPF app source.

## Build Verification After Increment 4 (2026-09-11)

Commands executed from `C:\Localdata\Siebwalde` (Debug, no hardware, no test execution):

```powershell
dotnet build "SiebwaldeApp.Core.Host\SiebwaldeApp.Core.Host.csproj" -c Debug --nologo
dotnet build "SiebwaldeApp\SiebwaldeApp.sln" -c Debug --nologo
```

Result:

- 0 errors for both.
- A targeted search found no remaining hard-coded `192.168.1.193` or firmware hex path in active startup code, other than the new `CoreSettings` Designer defaults.

## Build Verification After Increment 5A (2026-09-11)

Commands executed from `C:\Localdata\Siebwalde` (Debug, no hardware, no test execution):

```powershell
dotnet build "SiebwaldeApp\SiebwaldeApp.sln" -c Debug --nologo
dotnet build "SiebwaldeApp.Core.Host\SiebwaldeApp.Core.Host.csproj" -c Debug --nologo
```

Result:

- 0 errors for both.
- Host detection is code-inspected only; no live host was contacted. Runtime behavior (ping/TCP results, page updates) is not yet verified.

## External Endpoints And Files

| Purpose | Value |
| --- | --- |
| Track controller target | `192.168.1.193:10000` |
| Track controller local receive port | `10001` |
| Fiddle Yard target name | `FIDDLEYARD` |
| Fiddle Yard send/receive ports | `28671` / `28672` |
| ECoS emulator TCP endpoint | `127.0.0.1:15471` |
| Koploper external info endpoint | `127.0.0.1:5700` |
| Log directory | `C:\Localdata\Siebwalde\Logging\` |
| Locomotive JSON store | `C:\Localdata\Siebwalde\Logging\locos.json` |
| Track amplifier firmware path | `C:\Localdata\Siebwalde\TrackAmplifier4.X\dist\Offset\production\TrackAmplifier4.X.production.hex` |
