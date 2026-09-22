# Implementation Notes

All behavior below is code-inspected unless a section explicitly says runtime-tested.

## Desktop UI

`App.OnStartup` in `SiebwaldeApp/SiebwaldeApp/App.xaml.cs` is the WPF entry point. It calls `ApplicationSetup`, then creates and shows `MainWindow`.

`ApplicationSetup` creates the configured log directory, binds `ILogFactory` and `IFileManager` in `SiebwaldeApp.IoC.Kernel`, calls `IoC.Setup`, and creates `FiddleYardWinFormViewModel`.

`MainWindow.MainWindow` in `SiebwaldeApp/SiebwaldeApp/MainWindow.xaml.cs` sets `DataContext = new WindowViewModel(this)`.

Navigation is handled by these types:

- `ApplicationViewModel` in `SiebwaldeApp/SiebwaldeApp/ViewModel/ApplicationViewmodel.cs` exposes page switching commands.
- `SideMenuViewModel` in `SiebwaldeApp/SiebwaldeApp/ViewModel/SideMenuViewModel.cs` exposes submenu commands.
- `ApplicationPageValueConverter` and `MenuPageValueConverter` create page/control instances based on enum values.
- `BasePage<T>` resolves its view model from `SiebwaldeApp.IoC.Kernel`.

Track and Fiddle Yard startup actions are bound from `SiebwaldeInitPage.xaml` to `SiebwaldeInitPageViewModel`:

- `InitTrackController` calls `IoC.siebwaldeApplicationModel.StartTrackApplication`.
- `InitFiddleYardController` calls `IoC.siebwaldeApplicationModel.StartFYController`.

## Core Application Model

`SiebwaldeApplicationModel` in `SiebwaldeApp/SiebwaldeApp.Core/Model/SiebwaldeApplicationModel.cs` is the main core facade used by the UI.

Important members:

- Events: `InstantiateFiddleYardWinForms`, `FiddleYardShowWinForms`, `FiddleYardShowSettingsWinForms`.
- Controllers: `FYcontroller`, `YDcontroller`, `_trackControlMain`.
- Track lifecycle: `StartTrackApplication`, `StopTrackApplication`.
- Fiddle Yard lifecycle: `StartFYController`.
- Track control API: `SetAmplifierPwm`, `SetAmplifierEmStop`, `SetAmplifierControl`, `GetAmplifierListing`, `TrackAmplifiers`.

`StartTrackApplication` hard-codes:

- PIC32 IP address `192.168.1.193`.
- Target UDP port `10000`.
- Local UDP port `10001`.
- Firmware hex path `C:\Localdata\Siebwalde\TrackAmplifier4.X\dist\Offset\production\TrackAmplifier4.X.production.hex`.

`StopTrackApplication` calls `TrackControlMain.StopRuntime`, cancels `_appCts`, synchronously waits on async disposal through `DisposeAsync().AsTask().Wait()`, nulls track fields, and logs stop completion.

## Track Communication

`ITrackTransport` in `SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Comm/ITrackTransport.cs` defines `OpenAsync`, `CloseAsync`, `SendAsync`, and `ReceiveAsync`.

`RawUdpTransport` in `Comm/RawUdpTransport.cs` wraps `UdpClient`:

- Sends to a fixed remote endpoint.
- Binds a local UDP port for replies.
- Does not filter incoming datagrams by remote endpoint.
- Delays 50 ms and continues on receive-loop exceptions.

`RawUdpTrackTransport` in `Comm/UdpTrackTransport.cs` adapts `IRawUdpTransport` to `ITrackTransport` with a `Channel<byte[]>`.

`TrackCommClientAsync` in `Comm/TrackCommClientAsync.cs` owns communication behavior:

- `StartAsync` opens the transport, starts a receive loop task, and starts `_publishTimer`.
- `SendAsync` builds `[HEADER, Command, Data...]` frames and sends them through `ITrackTransport.SendAsync`.
- `HandleNewData` parses amplifier frames when sender is `Enums.SLAVEINFO`.
- `HandleNewData` parses controller messages into `ReceivedMessage` otherwise.
- `AmplifierDataReceived` is raised for parsed or periodically republished amplifier data.
- `ControlMessageReceived` is raised for parsed control messages.

Protocol constants live in `SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Services/PublicEnums.cs`, including `Enums.HEADER = 0xAA` and `Enums.SLAVEINFO = 0xFF`.

## Track State And Runtime Writes

`TrackApplicationVariables` in `SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Data/TrackApplicationVariables.cs` creates 56 `TrackAmplifierItem` entries with slave numbers `0..55`. `MaxAmplifiers` is `50`, and `trackAmpWriteItems` is initialized for slaves `1..50`.

`PendingWrites` maps slave number to `TrackAmplifierWriteData`. `SetDesiredAmplifierControl` ignores slave `0`, clamps PWM to `0..799`, sets HR0 bits `0..9`, sets bit `15` for emergency stop, and records a pending write.

`TrackControlMain` in `SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Controller/TrackControlMain.cs` starts a 100 ms runtime timer. It consumes `PendingWrites` and sends `TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH` messages.

## Track Initialization

`TrackAmplifierInitializationServiceAsync` in `Initialization/TrackAmplifierInitializationServiceAsync.cs` orchestrates initialization:

- Stores steps by `IInitializationStep.Name`.
- Subscribes to `ITrackCommClient.ControlMessageReceived`.
- Buffers messages in an unbounded `Channel<ReceivedMessage>`.
- Starts at `ConnectToEthernetTarget`.
- Reads the next message with a 100 ms timeout and passes it to `ExecuteAsync`.
- Sets status to `Completed`, `Failed`, or `Cancelled` based on step results or exceptions.

Important code-inspected caveats:

- There is no overall initialization timeout.
- `SetDefaultPwmSetpointsStep` is registered in the WPF startup pipeline but skipped because `InitTrackamplifiersStep` returns `InitStepResult.Next("EnableTrackamplifiers")`.
- `SetDefaultPwmSetpointsStep` returns `InitStepResult.Next("EnableTrackamplifiersStep")`, which does not match `EnableTrackamplifiersStep.Name`.

## Firmware Transfer

`TrackAmplifierBootloaderHelpers` reads the firmware hex file from the configured hard-coded path, extracts firmware data/config word information, and computes checksums.

`SendNextFwDataPacket` sends `TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE` chunks using `ITrackCommClient.SendAsync`. Code inspection found that `SendNextFwDataPacket.Execute` calls `ConfigureAwait(false)` without awaiting the returned task, so send ordering and send failures are not guaranteed by that method.

## Fiddle Yard

`FiddleYardController` in `SiebwaldeApp/SiebwaldeApp.Core/Model/FiddleYardApplication/FiddleYardController.cs` owns communication to target `FIDDLEYARD`:

- `NewPingTarget.TargetFound` checks target availability.
- `NewSender.ConnectUdp` connects to the configured send port.
- `NewReceiver.Start` begins the receive loop when real target mode is active.
- `FYIOHandleTOP` and `FYIOHandleBOT` are initialized and started for both real and simulator modes.

`FiddleYardIOHandle` routes actuator commands either through `NewSender.SendUdp` or `FiddleYardSimulator.CommandToSend`. It routes incoming bytes from `NewReceiver.NewData` or `FiddleYardSimulator.NewData` to `HandleNewData` and updates `FiddleYardIOHandleVariables`.

`FiddleYardApplication` in `FiddleYardApplication.cs` owns a timer-driven state machine. It composes `FiddleYardApplicationVariables`, `FiddleYardMip50`, `FiddleYardAppRun`, `FiddleYardTrainDetection`, and `FiddleYardAppInit`. Its main states include `Idle`, `Start`, `Init`, `Running`, `Stop`, `Reset`, `MIP50Home`, `MIP50Move`, and `TrainDetection`.

`NewReceiver` uses a blocking UDP receive loop without a verified cancellation/stop path. `NewSender.ConnectUdp` catches and ignores connection exceptions.

## ECoS Emulator

`EcosEmulatorServer` in `SiebwaldeApp/SiebwaldeApp.EcosEmu/Server/EcosEmulatorServer.cs` listens on loopback, accepts TCP clients, reads ASCII chunks, splits commands at `)`, parses with `IEcosCommandParser`, and dispatches to `IEcosBackend`.

`SimpleEcosCommandParser.Parse` parses command names, optional object IDs, and comma-separated options. It is intentionally simple and may not handle quoted commas.

`SimpleEcosBackend` in `Backend/SimpleEcosBackend.cs` supports `set`, `get`, `queryObjects`, `request`, `release`, `create`, and `delete`. It maintains runtime dictionaries for locomotives, switches, feedback modules, sensor masks, and sensor state.

`SimpleEcosBackend` also implements `IHardwareFeedbackSink` so simulator/hardware feedback can generate ECoS events. It stores a single `_currentWriter`, so behavior with multiple simultaneous clients is uncertain.

`TrackSimulatorBackend` in `Hardware/TrackSimulatorBackend.cs` simulates blocks, switches, locomotive movement, and occupancy sensors. It starts a background loop from `AttachFeedbackSink`, updates sensors through fire-and-forget calls to `IHardwareFeedbackSink.OnSensorChangedAsync`, and uses Koploper block updates to seed locomotive positions.

`KoploperExternalInfoClient` connects to `127.0.0.1:5700`, parses `0x1B`-separated records, updates locomotive block state, and raises `BlockEntered`.

`JsonLocoRepository` persists locomotive data at `C:\Localdata\Siebwalde\Logging\locos.json`. Missing or empty JSON creates a fresh database; invalid JSON is moved to `.bak` before a new database is created.

## Tests

The former test project `SiebwaldeApp/SiebwaldeApp.Tests` was removed on 2026-09-11 (Increment 2). It contained `StationTrackTests`, `StationSideTests`, `StationControllerTests`, `TestTrackIn`, `TestTrackOut`, and `IoCTestBootstrap`.

It referenced station-domain symbols with no definition in active source (`StationTrack`, `TrainType`, `TrackApplication`, `StationSide`, `TrackSensor`, `Signal`, `Amplifier`, `TrackBlock`, `TrackMetadata`, `TrackRole`, `ITrackIn`, `ITrackOut`) and `IoCTestBootstrap` used the old Ninject `IoC.Kernel`. It was in no solution and could not compile.

The encoded station design intent is archived in `docs/project-knowledge.md`; the source is recoverable from git commit `104c1e6`. The active test project is `SiebwaldeApp/SiebwaldeApp.Core.Tests` (xUnit).

## Code-Inspected Risks

Revalidation status (2026-09-11): the first four items below were re-checked against current source and confirmed. Increment 1 then fixed the host logging and the initialization sequencing; those items are marked FIXED. The remaining items are still code-inspected but not re-verified.

- FIXED (Increment 1): `SiebwaldeApp.Core.Host/Program.cs` now calls `IoC.ConfigureLogger(...)` instead of the non-existent `IoC.Kernel.Bind<ILogFactory>()`. Host builds with 0 errors.
- CONFIRMED: `SiebwaldeApp.Tests` references missing station-domain symbols (`StationTrack`, `TrainType`, `StationSide`, `TrackApplication`, `TrackMetadata`, `TrackRole`, `ITrackIn`, `ITrackOut`) and uses `IoC.Kernel`/Ninject; it cannot compile in the current source state. Not fixed.
- FIXED (Increment 1): `SetDefaultPwmSetpointsStep` now returns `Next("EnableTrackamplifiers")`, matching the registered `EnableTrackamplifiersStep.Name`.
- FIXED (Increment 1): `InitTrackamplifiersStep` now returns `Next("SetDefaultPwmSetpoints")`, so the default-PWM step is no longer skipped.
- Not re-verified: `TrackCommClientAsync` has a comment saying 2 Hz publish while the timer interval is 100 ms.
- Not re-verified: `SendNextFwDataPacket.Execute` does not await `SendAsync`.
- Not re-verified: several Fiddle Yard error paths swallow exceptions or contain TODO/TBD recovery behavior.
- Not re-verified: ECoS emulator multi-client behavior is uncertain because `SimpleEcosBackend` stores one `_currentWriter`.
