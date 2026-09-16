# Project Knowledge

## Verified Facts

- Siebwalde is a model railway control application based on code evidence in track amplifier, Fiddle Yard, ECoS emulator, Koploper, and simulator components.
- The workspace root is `C:\Localdata\Siebwalde`, a Git repository (`origin https://github.com/jeremysiebers/Siebwalde.git`). It contains the managed .NET application plus microcontroller firmware, PCB hardware sources, Python tooling, runtime logs, and backup projects. See `docs/inventory.md` for the full top-level list.
- The managed .NET application spans three top-level directories: `SiebwaldeApp`, `SiebwaldeApp.Core.Host`, and `SiebwaldeApp.EcosEmu`. All discovered `.sln` files and `ProjectReference` entries are relative and resolve correctly from the current root.
- `SiebwaldeApp` is the main solution and shared source root.
- `SiebwaldeApp.Core.Host` and `SiebwaldeApp.EcosEmu` are host solutions that reference projects under `SiebwaldeApp`.
- Active application projects target .NET 8 variants: `net8.0`, `net8.0-windows`, and `net8.0-windows7.0`.
- `SiebwaldeApp.EcosEmu_old` is not referenced by discovered solution files and should be treated as potentially obsolete or experimental until the user decides its role.
- Existing prior context is in `SiebwaldeApp/SiebwaldeApp_project_context.md`; it contains modernization goals and previous conversation context, not only verified current behavior.
- Project-specific OpenCode agents were created under `.opencode/agents/`, but they require an OpenCode restart before use.
- `TrackAmplifier4.X` maintains its own firmware guidance (`AGENT_TRACK_AMPLIFIER.md`, `AGENT_TRACK_AMPLIFIER_COMPACT.md`, `MODBUS_TRACK_AMPLIFIER_MAPPING.md`, `TRACK_AMPLIFIER_STATE_MACHINE.md`); it is referenced, not duplicated, in `docs/`.

## Product-Owner-Confirmed Context (2026-09-11)

Status: confirmed by the product owner, not independently code-verified. See `docs/product.md` and `human_input.md`.

- The active PC application is `SiebwaldeApp` + `SiebwaldeApp.Core` + `SiebwaldeApp.EcosEmu`, grouped in the `SiebwaldeApp` solution.
- `TrackController5` drives the 50 ModBus amplifiers, translating C# Ethernet commands into ModBus/bootloader commands, and performs MMDC tasks.
- All 50 amplifiers run `TrackAmplifier4.X` with the fixed `TrackAmplifierBootLoader.X`; they are currently "dumb" but must later execute setpoints/parameters and perform MMDC plus occupancy feedback.
- `TrackBackplane2.X` is fixed ModBus code for the 5 backplanes and stays static.
- `TrackControllerPic18.X` is obsolete; its remnants must be removed from the C# project.
- Koploper is the driving-behavior authority via the ECoS emulator; C# translates Koploper commands into amplifier setpoints and feeds occupancy back.
- The Fiddle Yard C# control is deferred and should not be changed for now.
- `YardController_IOX.X` will be partly split into C# later.
- The first development increment is C# cleanup and correctness.
- MMDC split: TrackAmplifier4.X does hardware protection (current/temp cutoff, hiccup, PWM setpoint monitoring); TrackController5 monitors PC/amplifier communication and broadcasts emergency stop; C# handles communication monitoring, alarm/logging, and recovery.
- The Yard stays hand-operated with C#/Koploper handover commands. Faller Car via Koploper is an experiment; otherwise a dedicated C# solution is needed.
- A shuttle line (pendelbaan) exists: max 4, min 1 locomotive between 3 stations (2 end stations with a switch, 1 middle station with fixed direction).
- Koploper controls speed profile and calibration; decoder settings, speed calibration (Koploper), and software acceleration/braking (Koploper) are three distinct concepts. The exact internal braking calculation could not be confirmed from documentation.

## Important Symbols

| Symbol | Responsibility | Path |
| --- | --- | --- |
| `App.OnStartup` | WPF startup. | `SiebwaldeApp/SiebwaldeApp/App.xaml.cs` |
| `SiebwaldeApp.IoC` | UI Ninject service locator. | `SiebwaldeApp/SiebwaldeApp/IoC/IoC.cs` |
| `SiebwaldeApplicationModel` | Main core facade for UI-triggered track and Fiddle Yard startup. | `SiebwaldeApp/SiebwaldeApp.Core/Model/SiebwaldeApplicationModel.cs` |
| `TrackCommClientAsync` | Async/event-based track communication client. | `SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Comm/TrackCommClientAsync.cs` |
| `ITrackTransport` | Transport abstraction for track communication. | `SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Comm/ITrackTransport.cs` |
| `TrackAmplifierInitializationServiceAsync` | Track initialization orchestrator. | `SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Initialization/TrackAmplifierInitializationServiceAsync.cs` |
| `TrackControlMain` | Runtime track amplifier write loop. | `SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Controller/TrackControlMain.cs` |
| `FiddleYardController` | Fiddle Yard connection and top/bottom IO orchestration. | `SiebwaldeApp/SiebwaldeApp.Core/Model/FiddleYardApplication/FiddleYardController.cs` |
| `FiddleYardApplication` | Fiddle Yard application state machine. | `SiebwaldeApp/SiebwaldeApp.Core/Model/FiddleYardApplication/FiddleYardApplication.cs` |
| `EcosEmulatorServer` | ECoS TCP server. | `SiebwaldeApp/SiebwaldeApp.EcosEmu/Server/EcosEmulatorServer.cs` |
| `SimpleEcosBackend` | ECoS command backend and hardware feedback sink. | `SiebwaldeApp/SiebwaldeApp.EcosEmu/Backend/SimpleEcosBackend.cs` |
| `TrackSimulatorBackend` | ECoS hardware simulator backend. | `SiebwaldeApp/SiebwaldeApp.EcosEmu/Hardware/TrackSimulatorBackend.cs` |
| `KoploperExternalInfoClient` | Koploper external info client. | `SiebwaldeApp/SiebwaldeApp.EcosEmu/ExternalInfo/KoploperExternalInfoClient.cs` |

## Assumptions To Recheck

- The repository appears to be mid-migration from legacy timer/service-locator patterns to async abstractions, but the intended migration plan should be confirmed with the user before implementation.
- `SiebwaldeApp.EcosEmu_old` likely exists as an old copy, but deletion or archival is not approved.
- Tests appear stale or ahead of source, but this should be confirmed by a full build/test run when writing `bin/obj` is allowed.
- The .NET code-analysis findings (missing `IoC.Kernel`, missing station-domain symbols, initialization step sequencing, hard-coded endpoints) were produced before the workspace was confirmed as the full Git repository and have not been revalidated against the current source. Treat them as requiring revalidation, not as confirmed true or false.

## Open Questions

- Should future work prioritize build health, track initialization correctness, ECoS emulator reliability, or documentation completeness?
- Should `SiebwaldeApp.Core.Host` be fixed to use `SiebwaldeApp.Core.IoC.ConfigureLogger`, or was another `IoC` intended?
- Should `SetDefaultPwmSetpointsStep` run before `EnableTrackamplifiersStep` in the active pipeline?
- Is the active track controller always `192.168.1.193:10000`, or should `CoreSettings` be authoritative?
- Should the WPF app start or manage the ECoS emulator, or is the project reference only for future use?

## Session Notes

- No source code, dependency files, or runtime configuration were intentionally changed during this assignment.
- `dotnet --info` was run successfully.
- `dotnet test --no-build --no-restore` was run and failed because the test assembly was missing.
- Full build/test commands remain unexecuted in this session.
