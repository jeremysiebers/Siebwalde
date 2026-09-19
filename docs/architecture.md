# Architecture

## Verified System Shape

The managed .NET application spans three top-level directories. `SiebwaldeApp/` is the main source root. `SiebwaldeApp.Core.Host/` and `SiebwaldeApp.EcosEmu/` are separate host solutions that reference projects under `SiebwaldeApp/`. The workspace root `C:\Localdata\Siebwalde` is broader and also contains microcontroller firmware, PCB hardware sources, Python tooling, runtime logs, and backup projects; see `docs/inventory.md` for the top-level map.

The verified domain is model railway control. Code evidence includes track amplifier control, Fiddle Yard control, ECoS emulator integration, Koploper external information, UDP communication, and hardware/simulator abstractions.

Revalidation note: the architecture findings in this file were produced before the workspace was confirmed as the full Git repository and have not been re-checked against the current source. Project and solution boundaries were re-verified during the migration check; code-level findings remain subject to revalidation.

## Project Boundaries

| Boundary | Responsibility | Key source locations |
| --- | --- | --- |
| Desktop UI | WPF shell, navigation, view models, Fiddle Yard WinForms integration, user-triggered startup actions. | `SiebwaldeApp/SiebwaldeApp/App.xaml.cs`, `MainWindow.xaml`, `ViewModel/`, `Pages/`, `Controls/`. |
| Core library | Track application, track amplifier communication/init/runtime control, Fiddle Yard application, simulator, logging, file/services. | `SiebwaldeApp/SiebwaldeApp.Core/Model/`, `IoC/`, `Services/`, `Logging/`. |
| ECoS emulator library | ECoS TCP server, command parser, backend command handling, hardware abstraction, track simulator, Koploper external info, locomotive JSON persistence. | `SiebwaldeApp/SiebwaldeApp.EcosEmu/`. |
| Core host | Console composition root for track communication and initialization. | `SiebwaldeApp.Core.Host/Program.cs`. |
| Emulator host | Console composition root for ECoS emulator and simulator. | `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.Host/Program.cs`. |

## Confirmed Component Roles And Target Data Flow (Product Owner, 2026-09-11)

Status: confirmed by the product owner; code-level behavior still requires revalidation. See `docs/product.md`.

- `TrackController5` is the ModBus/bootloader master between C# (Ethernet) and the 50 amplifiers, and performs MMDC.
- The 50 amplifiers run `TrackAmplifier4.X` with `TrackAmplifierBootLoader.X`; they execute setpoints/parameters and perform MMDC and occupancy reporting.
- `TrackBackplane2.X` is static backplane ModBus code.
- `TrackControllerPic18.X` is obsolete and its C# remnants are to be removed.
- Koploper owns driving behavior. The ECoS emulator presents the layout to Koploper.

Target control loop (product-owner-described, to be verified against code):

1. Koploper sends per-encoder commands to the emulator/C#.
2. C# translates them per locomotive into setpoints for the amplifier(s) driving that locomotive; setpoints follow the locomotive.
3. Amplifier occupancy returns to C# and onward to Koploper.
4. Koploper returns locomotive-location display information via a dedicated port; C# uses it to select target amplifiers.
5. Koploper switch-street commands must be translated into Fiddle Yard TOP/BOTTOM shift commands; hardware signals move to software.

## Dependency Direction

- `SiebwaldeApp/SiebwaldeApp/SiebwaldeApp.csproj` references `SiebwaldeApp.Core`, `SiebwaldeApp.EcosEmu` and `SiebwaldeApp.Integration`.
- `SiebwaldeApp.Core.Host/SiebwaldeApp.Core.Host.csproj` references `SiebwaldeApp.Core`.
- `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.Host/SiebwaldeApp.EcosEmu.Host.csproj` references `SiebwaldeApp.EcosEmu`.
- `SiebwaldeApp/SiebwaldeApp.Tests` (xUnit, referencing `SiebwaldeApp.Core`) was removed on 2026-09-11 as an obsolete remnant; it was never in the main solution and could not compile.
- The WPF app uses `SiebwaldeApp.Integration` types: `IoC.Setup` constructs `TrackControlHost` and hands it to `SiebwaldeApplicationModel`, which owns the ECoS host lifecycle. WPF initiates operations and presents state; the composition and all control/safety logic live in Core, EcosEmu and Integration.
- `SiebwaldeApp/SiebwaldeApp.Integration/` (`net8.0-windows7.0`) references Core + EcosEmu and holds the composition/adapter layer: `TrackControlHost`, `TrackControlIntegration`, `TrackAmplifierHardwareBackend`, `SwitchController`, `SwitchTranslatingHardwareBackend`, `ControlSafetyInterlockBackend`, `ControlSafetyGuard`, `DivergenceChecker` and `EcosHardwareStopSink`. Core has no project references, so it cannot depend on Integration, EcosEmu or WPF.

## Startup And Lifecycle

### Desktop Application

`App.OnStartup` in `SiebwaldeApp/SiebwaldeApp/App.xaml.cs` calls `ApplicationSetup`, creates `MainWindow`, and shows it. `ApplicationSetup` creates the logging directory, binds `ILogFactory` and `IFileManager` into `SiebwaldeApp.IoC.Kernel`, calls `IoC.Setup`, and creates `FiddleYardWinFormViewModel`.

`IoC.Setup` in `SiebwaldeApp/SiebwaldeApp/IoC/IoC.cs` binds singleton instances of `ApplicationViewModel`, `SideMenuViewModel`, and `SiebwaldeApplicationModel`.

No active `OnExit` override was found. A shutdown path calling `IoC.siebwaldeApplicationModel.StopTrackApplication()` exists only in commented code in `App.xaml.cs`.

### Track Application

The UI path starts from `SiebwaldeInitPageViewModel.InitTrackController`, which calls `IoC.siebwaldeApplicationModel.StartTrackApplication`.

`SiebwaldeApplicationModel.StartTrackApplication` composes these core objects:

- `TrackApplicationVariables` for shared track state.
- `RawUdpTransport` to `192.168.1.193:10000` with local port `10001`.
- `RawUdpTrackTransport` as an `ITrackTransport` adapter.
- `TrackCommClientAsync` as the event-based track communication client.
- `TrackAmplifierBootloaderHelpers` and `SendNextFwDataPacket` for firmware transfer support.
- Initialization steps implementing `IInitializationStep`.
- `TrackAmplifierInitializationServiceAsync` as the initialization orchestrator.
- `TrackControlMain` for runtime amplifier writes after initialization.

After `InitializationStatus.Completed`, `TrackControlMain.StartRuntime` starts a timer-based runtime loop that consumes `TrackApplicationVariables.PendingWrites`.

### Fiddle Yard

The UI path starts from `SiebwaldeInitPageViewModel.InitFiddleYardController`, which calls `SiebwaldeApplicationModel.StartFYController`.

`StartFYController` builds MAC/IP payloads with `NewMAC_IP_Conditioner`, constructs `FiddleYardController`, raises `InstantiateFiddleYardWinForms`, and awaits `FiddleYardController.StartFiddleYardControllerAsync`.

`FiddleYardController` pings target name `FIDDLEYARD`, attempts UDP setup through `NewSender`, starts `NewReceiver` when real hardware is found, or sets simulator mode when the target cannot be reached. It owns `FYIOHandleTOP` and `FYIOHandleBOT`.

`FiddleYardIOHandle` chooses between real receiver data and `FiddleYardSimulator.NewData`, then updates `FiddleYardIOHandleVariables`. `FiddleYardApplication` owns the higher-level state machine.

### ECoS Emulator

`SiebwaldeApp.EcosEmu.Host/Program.Main` constructs `KoploperExternalInfoClient`, `JsonLocoRepository`, `TrackSimulatorBackend`, and `SimpleEcosBackend`. It attaches feedback with `TrackSimulatorBackend.AttachFeedbackSink`, starts Koploper external info, starts `EcosEmulatorServer` on loopback port `15471`, waits for ENTER, and calls `server.Stop`.

`EcosEmulatorServer.Start` starts a `TcpListener` on `IPAddress.Loopback`. `AcceptLoopAsync` accepts clients and starts `HandleClientAsync` for each. Commands are accumulated until `)`, parsed by `SimpleEcosCommandParser.Parse`, and delegated to `SimpleEcosBackend.HandleAsync`.

## Major Workflows

### Track Initialization

`TrackAmplifierInitializationServiceAsync.InitializeAsync` starts at step name `ConnectToEthernetTarget`, reads control messages from an unbounded `Channel<ReceivedMessage>`, calls the current `IInitializationStep.ExecuteAsync`, and moves by `InitStepResult`.

Registered UI startup steps in `SiebwaldeApplicationModel.StartTrackApplication` are:

- `ConnectToEthernetTargetStep`
- `ResetAllSlavesStep`
- `DataUploadStep`
- `DetectSlavesStep`
- `RecoverSlavesStep`
- `FlashFwTrackamplifiersStep`
- `InitTrackamplifiersStep`
- `SetDefaultPwmSetpointsStep`
- `EnableTrackamplifiersStep`

Code-inspected mismatch: `InitTrackamplifiersStep` transitions directly to `EnableTrackamplifiers`, so `SetDefaultPwmSetpointsStep` is registered but skipped. If reached directly, `SetDefaultPwmSetpointsStep` returns `EnableTrackamplifiersStep`, but the registered `EnableTrackamplifiersStep.Name` is `EnableTrackamplifiers`.

### Track Runtime Writes

UI methods `SiebwaldeApplicationModel.SetAmplifierPwm`, `SetAmplifierEmStop`, and `SetAmplifierControl` update desired amplifier control in `TrackApplicationVariables`. `TrackControlMain` consumes pending writes at 10 Hz and sends `TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH` through `ITrackCommClient.SendAsync`.

### ECoS Command Handling

`SimpleEcosBackend.HandleAsync` dispatches `set`, `get`, `queryObjects`, `request`, `release`, `create`, and `delete`. It writes ECoS-like `<REPLY ...>` and `<END ...>` blocks through the provided `TextWriter`.

Power commands for object `1` call `IHardwareBackend.SetPower`. Locomotive speed and direction commands call `IHardwareBackend.SetLocoSpeed`. Switch commands call `IHardwareBackend.SetSwitch` and update switch-state events. Feedback module `100` models 16 occupancy inputs.

### Koploper External Info And Simulation

`KoploperExternalInfoClient` connects to `127.0.0.1:5700`, parses records separated by byte `0x1B`, updates locomotive-to-block state, and raises `BlockEntered`.

`TrackSimulatorBackend` subscribes to `BlockEntered`, tracks locomotives in blocks, simulates movement around an oval with ladder switches, and reports occupancy changes to `IHardwareFeedbackSink.OnSensorChangedAsync`.

## State Ownership

- `SiebwaldeApplicationModel` owns controller fields for Fiddle Yard and track startup, plus track communication/init/runtime objects.
- `TrackApplicationVariables` owns amplifier list, controller messages, and pending amplifier writes.
- `TrackCommClientAsync` owns transport lifecycle and publish/receive events.
- `TrackAmplifierInitializationServiceAsync` owns initialization status, step dictionary, and the control message channel.
- `TrackControlMain` owns the runtime timer for pending writes.
- `FiddleYardController` owns sender/receiver and top/bottom IO handles.
- `FiddleYardApplication` owns Fiddle Yard application state and subprograms.
- `SimpleEcosBackend` owns runtime ECoS object state, feedback module state, switch state, and current client writer.
- `TrackSimulatorBackend` owns simulator locomotive/block/switch state.
- `JsonLocoRepository` owns persistent locomotive data and file writes.

## Concurrency And Background Work

| Component | Mechanism | Notes |
| --- | --- | --- |
| `TrackCommClientAsync` | `Task.Run` receive loop and `System.Timers.Timer` publish timer | Publish interval is 100 ms despite a comment saying 2 Hz. |
| `TrackAmplifierInitializationServiceAsync` | `Channel<ReceivedMessage>` plus async loop | Reads with 100 ms timeout; no overall init timeout. |
| `TrackControlMain` | `System.Timers.Timer` | Runtime loop uses a reentrancy guard. |
| `FiddleYardController` | `Task.Run` | Connection/startup logic runs on a background task. |
| `NewReceiver` | `Task.Factory.StartNew` with blocking UDP receive | No cancellation/stop API was verified. |
| `FiddleYardApplication` | `System.Timers.Timer` | Timer interval is 50 ms; update method locks `ExecuteLock`. |
| `EcosEmulatorServer` | Fire-and-forget accept/client tasks | Does not retain task handles. |
| `KoploperExternalInfoClient` | Background reconnect loop | Reconnect delay is 1 second after errors. |
| `TrackSimulatorBackend` | Background simulation task | Simulation loop runs around 20 Hz. |

## Architectural Constraints

- Several important endpoints and file paths are hard-coded or settings-backed and hardware-specific.
- The codebase mixes newer async interfaces with legacy timers, mutable containers, service locators, and fire-and-forget tasks.
- UI and Core have separate `IoC` types. `SiebwaldeApp.IoC` uses Ninject; `SiebwaldeApp.Core.IoC` exposes only logging configuration.
- Some comments describe intended future behavior or older design and do not always match the active implementation.
- Build health is uncertain and likely currently broken for at least `SiebwaldeApp.Core.Host` because it uses `IoC.Kernel` while active `SiebwaldeApp.Core.IoC` has no `Kernel` member.

## Open Architectural Questions

- Should `SiebwaldeApp.EcosEmu_old` remain as preserved reference material, or can it be archived outside the active workspace later?
- Is the WPF app expected to instantiate or control the ECoS emulator directly, or is the project reference only transitional?
- Which track controller port source is authoritative: hard-coded `10000/10001` in startup code or settings entries in `CoreSettings`?
- Is `SetDefaultPwmSetpointsStep` intended to run before amplifier enablement in the active initialization pipeline?
