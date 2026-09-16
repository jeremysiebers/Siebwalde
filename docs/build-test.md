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

The test project is not included in `SiebwaldeApp/SiebwaldeApp.sln`; run it explicitly when build writes are allowed:

```powershell
dotnet test "SiebwaldeApp\SiebwaldeApp.Tests\SiebwaldeApp.Tests.csproj" -c Debug
```

## Test Check Performed

Command executed from the pre-migration workspace (`C:\Users\jerem\Downloads\Test`; the active repository root is now `C:\Localdata\Siebwalde`):

```powershell
dotnet test "SiebwaldeApp\SiebwaldeApp.Tests\SiebwaldeApp.Tests.csproj" --no-build --no-restore -c Debug
```

Result:

- Failed before running tests because `SiebwaldeApp/SiebwaldeApp.Tests/bin/Debug/net8.0-windows7.0/SiebwaldeApp.Tests.dll` was not found.
- This does not prove source tests fail; it only proves there was no existing built test assembly at that path.

## Expected Build Risks From Code Inspection (Require Revalidation)

These risks were derived from code inspection in the pre-migration workspace. They were not re-checked against current source during the migration check and must be treated as requiring revalidation, not as confirmed build failures.

- `SiebwaldeApp.Core.Host/Program.cs` references `IoC.Kernel`, but `SiebwaldeApp/SiebwaldeApp.Core/IoC/IoC.cs` exposes only `Logger` and `ConfigureLogger`.
- `SiebwaldeApp/SiebwaldeApp.Tests/Infrastructure/IoCTestBootstrap.cs` references `IoC.Kernel` and has `using Ninject`, but the test project does not reference Ninject and active core `IoC` has no `Kernel`.
- Station tests reference symbols that were not found in active source inspection: `StationTrack`, `TrainType`, `TrackApplication`, `StationSide`, `TrackSensor`, `Signal`, `Amplifier`, `TrackBlock`, `TrackMetadata`, `TrackRole`, `ITrackIn`, and `ITrackOut`.
- `SiebwaldeApp.EcosEmu_old` targets `net9.0` and is not referenced by discovered solutions; it should not be treated as active build coverage unless explicitly selected.

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
