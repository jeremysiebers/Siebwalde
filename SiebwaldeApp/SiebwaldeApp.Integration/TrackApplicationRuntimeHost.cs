using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Comm;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Single in-process coordinator that owns the composition and lifetime of the track-control
    /// runtime: the Core track part (UDP transport -> comm client -> bootloader helpers ->
    /// initialization pipeline -> runtime write loop) and the ECoS host (the server Koploper talks
    /// to, plus the simulator backend and the Koploper external-info client).
    ///
    /// It is the single owner of:
    /// - the runtime lifecycle state machine (<see cref="TrackRuntimeState"/>),
    /// - the per-instance <see cref="CancellationTokenSource"/>,
    /// - the gate that serializes lifecycle operations so no two overlap.
    ///
    /// Start, stop and restart are all async and never block on the calling thread. A stop that
    /// cannot complete cleanly (timeout/fault/partial cleanup) transitions to
    /// <see cref="TrackRuntimeState.Failed"/>, never silently to <see cref="TrackRuntimeState.Stopped"/>.
    /// </summary>
    public sealed class TrackApplicationRuntimeHost : ITrackApplicationRuntime, IAsyncDisposable
    {
        /// <summary>Upper bound for the stop/dispose await.</summary>
        private static readonly TimeSpan StopBudget = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Default window for commanding + observing neutral on start/stop. It must exceed
        /// <see cref="TrackAmplifierDataFreshness.DefaultStaleAfter"/> (2 s) so a freshly
        /// echoed neutral readback stays "current" for the whole observation.
        /// </summary>
        private static readonly TimeSpan DefaultNeutralObserveWindow = TimeSpan.FromSeconds(5);

        private readonly IEcosHostService _ecosHost;
        private readonly IControlTrace? _controlTrace;
        private readonly Func<ITrackTransport>? _transportFactory;
        private readonly TrackAmplifierGroups _amplifierGroups;
        private readonly TimeSpan _neutralObserveWindow;
        private readonly SemaphoreSlim _gate = new(1, 1);

        private CancellationTokenSource? _cts;
        private TrackRuntimeState _state = TrackRuntimeState.Stopped;
        private TrackControlMode? _requestedMode;

        // Track-part fields (moved verbatim from SiebwaldeApplicationModel).
        private TrackApplicationVariables? _trackVariables;
        private TrackCommClientAsync? _trackCommClient;
        private TrackAmplifierBootloaderHelpers? _bootloaderHelpers;
        private SendNextFwDataPacket? _sendNextFwDataPacket;
        private TrackAmplifierInitializationServiceAsync? _trackInitService;
        private TrackControlMain? _trackControlMain;
        private string _loggerInstance = "TrackAppLog";
        private ILogger? _trackApplicationLogging;
        private MovementPermissionController? _movementPermission;

        /// <inheritdoc />
        public event EventHandler<TrackRuntimeStatusChangedEventArgs>? StateChanged;

        /// <inheritdoc />
        public event EventHandler<RuntimeFaultEventArgs>? Faulted;

        /// <inheritdoc />
        public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;

        /// <summary>
        /// Creates the coordinator.
        /// </summary>
        /// <param name="ecosHost">The ECoS host that serves Koploper; owned by the host application and injected here.</param>
        /// <param name="controlTrace">The optional dedicated production control trace.</param>
        /// <param name="transportFactory">
        /// Optional transport factory. When null (production) the real UDP transport is built.
        /// Supplied only by tests so the real-mode composition path can be exercised without a track controller.
        /// </param>
        /// <param name="amplifierGroups">
        /// The configured operational grouping (the observed-neutral domain). Defaults to
        /// <see cref="TrackAmplifierGroups.Empty"/>; production passes
        /// <see cref="CoreConfiguration.BuildTrackAmplifierGroups"/> (shared with the ECoS host).
        /// </param>
        /// <param name="neutralObserveWindow">
        /// Bounded window for commanding + observing neutral on start/stop. Defaults to 5 s.
        /// </param>
        public TrackApplicationRuntimeHost(
            IEcosHostService ecosHost,
            IControlTrace? controlTrace = null,
            Func<ITrackTransport>? transportFactory = null,
            TrackAmplifierGroups? amplifierGroups = null,
            TimeSpan? neutralObserveWindow = null)
        {
            _ecosHost = ecosHost ?? throw new ArgumentNullException(nameof(ecosHost));
            _controlTrace = controlTrace;
            _transportFactory = transportFactory;
            _amplifierGroups = amplifierGroups ?? TrackAmplifierGroups.Empty;
            _neutralObserveWindow = neutralObserveWindow ?? DefaultNeutralObserveWindow;

            // Surface an unexpected background fault from the host while the runtime is Running.
            _ecosHost.Faulted += OnEcosHostFaulted;
        }

        /// <inheritdoc />
        public TrackRuntimeState State => _state;

        /// <inheritdoc />
        public TrackControlMode? ActiveEcosMode => _ecosHost?.Mode;

        /// <inheritdoc />
        public ControlDiagnostics? ControlDiagnostics => _ecosHost?.Diagnostics;

        /// <inheritdoc />
        public bool IsControlPathUnsafe => _ecosHost?.IsUnsafe ?? false;

        /// <inheritdoc />
        public MovementPermissionState MovementPermission
            => _movementPermission?.State ?? MovementPermissionState.NotGranted;

        /// <summary>True while movement is permitted (observed neutral established and not withdrawn).</summary>
        public bool IsMovementSafe => _movementPermission?.IsGranted ?? false;

        /// <inheritdoc />
        public bool ResetControlSafety() => _ecosHost?.ResetSafety() ?? false;

        /// <inheritdoc />
        public IReadOnlyList<TrackAmplifierItem> TrackAmplifiers
        {
            get
            {
                if (_trackVariables == null)
                {
                    return Array.Empty<TrackAmplifierItem>();
                }

                return _trackVariables.GetAmplifierListing();
            }
        }

        /// <inheritdoc />
        public List<TrackAmplifierItem> GetAmplifierListing()
            => _trackVariables?.GetAmplifierListing() ?? new List<TrackAmplifierItem>();

        // ---------------------------------------------------------------------------------
        // Lifecycle
        // ---------------------------------------------------------------------------------

        /// <inheritdoc />
        public async Task StartAsync(TrackControlMode mode, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_state != TrackRuntimeState.Stopped)
                {
                    throw new InvalidOperationException($"Cannot start the track runtime: current state is {_state}.");
                }

                TransitionTo(TrackRuntimeState.Starting);
                _requestedMode = mode;
                _cts = new CancellationTokenSource();

                string? failure = null;
                Exception? fault = null;

                try
                {
                    if (mode == TrackControlMode.Real)
                    {
                        await ComposeTrackPartAsync(_cts.Token).ConfigureAwait(false);
                    }

                    var result = await _ecosHost.StartAsync(mode, _trackCommClient, _trackVariables, _cts.Token).ConfigureAwait(false);

                    if (result is EcosHostStartResult.Started
                        or EcosHostStartResult.AlreadyActive
                        or EcosHostStartResult.Transitioned)
                    {
                        // Real mode: command + observe neutral on the configured domain before
                        // reporting Running. Running does NOT imply movement-safe; the permission
                        // state (and, on failure, the raised fault + diagnostic) carry the truth.
                        if (mode == TrackControlMode.Real)
                        {
                            await EstablishObservedNeutralAsync(_cts.Token).ConfigureAwait(false);
                        }

                        TransitionTo(TrackRuntimeState.Running);
                        return;
                    }

                    failure = $"ECoS host did not start: {result}.";
                }
                catch (Exception ex)
                {
                    fault = ex;
                    failure = ex.Message;
                }

                if (fault is not null)
                {
                    RaiseFault("TrackApplicationRuntimeHost.StartAsync", fault);
                }

                TransitionTo(TrackRuntimeState.Failed, failure);

                using var cleanupCts = new CancellationTokenSource(StopBudget);
                try
                {
                    await StopTrackPartAsync(cleanupCts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    RaiseFault("TrackApplicationRuntimeHost.StartCleanup", ex);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_state == TrackRuntimeState.Stopped)
                {
                    return; // idempotent
                }

                TransitionTo(TrackRuntimeState.Stopping);

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linked.CancelAfter(StopBudget);
                var budget = linked.Token;

                try
                {
                    await StopTrackPartAsync(budget).ConfigureAwait(false);
                    TransitionTo(TrackRuntimeState.Stopped);
                }
                catch (Exception ex)
                {
                    RaiseFault("TrackApplicationRuntimeHost.StopAsync", ex);
                    TransitionTo(TrackRuntimeState.Failed, ex.Message);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <inheritdoc />
        public async Task RestartAsync(CancellationToken ct = default)
        {
            var mode = _requestedMode ?? TrackControlMode.Simulator;

            await StopAsync(ct).ConfigureAwait(false);

            if (_state == TrackRuntimeState.Failed)
            {
                return; // the stop did not prove clean; stay Failed rather than masking it.
            }

            await StartAsync(mode, ct).ConfigureAwait(false);
        }

        /// <summary>Best-effort graceful stop on disposal, bounded so it cannot hang.</summary>
        public async ValueTask DisposeAsync()
        {
            using var cts = new CancellationTokenSource(StopBudget);
            try
            {
                await StopAsync(cts.Token).ConfigureAwait(false);
            }
            catch
            {
                // Best-effort: the process exit remains the final fallback.
            }

            _gate.Dispose();
        }

        // ---------------------------------------------------------------------------------
        // Track-part composition (verbatim move from SiebwaldeApplicationModel.StartTrackApplication)
        // ---------------------------------------------------------------------------------

        private async Task ComposeTrackPartAsync(CancellationToken ct)
        {
            // 0) Setup logging (fresh per start; removed on stop).
            _loggerInstance = "TrackAppLog";

            _trackApplicationLogging = new FileLogger(
                CoreConfiguration.LogDirectory
                + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_"
                + "TrackAppLog.txt",
                _loggerInstance);

            SiebwaldeApp.Core.IoC.Logger.AddLogger(_trackApplicationLogging);

            // 2) Fresh shared variable container (fresh PendingWrites per start).
            _trackVariables = new TrackApplicationVariables();

            // Fresh movement-permission controller, shared by the movement gate (on the variables)
            // and the safety interlock (via the ECoS host). Created NotGranted and only granted
            // after observed neutral. Set BEFORE comm/host start so both paths see the same state.
            _movementPermission = new MovementPermissionController();
            _trackVariables.MovementPermission = _movementPermission;

            // 3) Build low-level Ethernet / Modbus transport.
            var transport = _transportFactory?.Invoke() ?? CreateRealTransport();

            // 4) Communication client on top of the selected transport.
            _trackCommClient = new TrackCommClientAsync(transport, _trackVariables);

            // Re-publish amplifier data so consumers observe the same frames.
            _trackCommClient.AmplifierDataReceived += (_, e) => AmplifierDataReceived?.Invoke(this, e);

            // 5) Bootloader helper objects (fresh per start).
            _bootloaderHelpers = new TrackAmplifierBootloaderHelpers(
                CoreConfiguration.TrackAmplifierFirmwarePath,
                _loggerInstance);

            _sendNextFwDataPacket = new SendNextFwDataPacket(
                _trackCommClient,
                _bootloaderHelpers);

            // 6) Compose initialization steps.
            var steps = new IInitializationStep[]
            {
                new ConnectToEthernetTargetStep(_trackCommClient, _trackVariables, _loggerInstance),
                new ResetAllSlavesStep(_trackCommClient, _trackVariables, _loggerInstance),
                new DataUploadStep(_trackCommClient, _trackVariables, _loggerInstance),
                new DetectSlavesStep(_trackCommClient, _trackVariables, _loggerInstance),
                new RecoverSlavesStep(_trackCommClient, _trackVariables, _sendNextFwDataPacket, _bootloaderHelpers, _loggerInstance),
                new FlashFwTrackamplifiersStep(_trackCommClient, _trackVariables, _sendNextFwDataPacket, _bootloaderHelpers, _loggerInstance),
                new InitTrackamplifiersStep(_trackCommClient, _loggerInstance),
                new SetDefaultPwmSetpointsStep(_trackVariables, _loggerInstance),
                new EnableTrackamplifiersStep(_trackCommClient, _loggerInstance),
            };

            _trackInitService = new TrackAmplifierInitializationServiceAsync(
                _trackCommClient,
                _trackVariables,
                steps,
                _loggerInstance);

            _trackInitService.ProgressChanged += (_, e) =>
            {
                SiebwaldeApp.Core.IoC.Logger.Log($"Track init: {e.StepName} - {e.Message}", _loggerInstance);
            };

            // 7) Create TrackControlMain and hook StatusChanged to start the runtime loop.
            _trackControlMain = new TrackControlMain(
                _loggerInstance,
                _trackCommClient,
                _trackVariables,
                _controlTrace);

            _trackInitService.StatusChanged += (_, status) =>
            {
                SiebwaldeApp.Core.IoC.Logger.Log($"Track init status: {status}", _loggerInstance);

                if (status == InitializationStatus.Completed &&
                    _trackControlMain != null &&
                    _cts != null)
                {
                    _trackControlMain.StartRuntime(_cts.Token);
                }
            };

            // 8) Start communication and run the initialization pipeline.
            await _trackCommClient.StartAsync(true, ct).ConfigureAwait(false);
            await _trackInitService.InitializeAsync(ct).ConfigureAwait(false);

            // A genuine init failure/cancellation never throws from InitializeAsync; observe the
            // status instead so a failed real-mode start is never masked as Running.
            if (_trackInitService.Status != InitializationStatus.Completed)
            {
                throw new InvalidOperationException(
                    $"Track initialization did not complete: {_trackInitService.Status}.");
            }

            SiebwaldeApp.Core.IoC.Logger.Log("Track Application started.", "");
        }

        // ---------------------------------------------------------------------------------
        // Observed-neutral establishment
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Commands neutral to every configured amplifier and waits (bounded) until every one of
        /// them reports a fresh readback of the neutral PWM. On success the movement permission is
        /// granted; on failure it stays NotGranted and a fault + a
        /// <see cref="DiagnosticCode.NeutralNotEstablished"/> diagnostic are surfaced. This never
        /// throws: the caller always proceeds to <see cref="TrackRuntimeState.Running"/> and the
        /// permission state carries the truth.
        /// </summary>
        private async Task EstablishObservedNeutralAsync(CancellationToken ct)
        {
            if (_trackVariables is null)
            {
                return;
            }

            // Fail closed: an empty safety domain means there is nothing to observe, so movement
            // permission must never be granted vacuously. This is the safe consequence of the open
            // group/domain Product Owner decision; it is surfaced as a fault, not a silent grant.
            if (_amplifierGroups.AllConfigured.Count == 0)
            {
                var emptyDomainFault = new InvalidOperationException(
                    "No safety domain configured; movement blocked (observed neutral cannot be established for an empty domain).");

                RaiseFault("TrackApplicationRuntimeHost.EstablishObservedNeutral", emptyDomainFault);

                _ecosHost?.Diagnostics?.Report(new ControlDiagnostic
                {
                    Code = DiagnosticCode.NeutralNotEstablished,
                    Severity = DiagnosticSeverity.Rejected,
                    Subject = "movement permission",
                    Detail = "No safety domain configured; movement blocked."
                });

                return;
            }

            // Command neutral to the whole configured domain. Neutral is always accepted by the
            // gate, so this establishes the safe state without needing permission first.
            foreach (var slave in _amplifierGroups.AllConfigured)
            {
                _trackVariables.SetDesiredAmplifierControl(slave, AmplifierSpeedMapper.NeutralPwm, false);
            }

            var observed = await ObserveNeutralAsync(ct).ConfigureAwait(false);

            if (observed)
            {
                _movementPermission?.Grant();
                return;
            }

            var fault = new InvalidOperationException(
                "Observed neutral was not established for every configured amplifier within the window; movement permission is not granted.");

            RaiseFault("TrackApplicationRuntimeHost.EstablishObservedNeutral", fault);

            _ecosHost?.Diagnostics?.Report(new ControlDiagnostic
            {
                Code = DiagnosticCode.NeutralNotEstablished,
                Severity = DiagnosticSeverity.Rejected,
                Subject = "movement permission",
                Detail = "Observed neutral was not established for every configured amplifier within the window; movement is not permitted."
            });
        }

        /// <summary>
        /// Polls until every configured amplifier reports current data whose PWM field equals the
        /// neutral setpoint, or until the bounded window elapses / cancellation is requested.
        /// </summary>
        private async Task<bool> ObserveNeutralAsync(CancellationToken ct)
        {
            var deadline = DateTimeOffset.UtcNow + _neutralObserveWindow;

            while (DateTimeOffset.UtcNow < deadline)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                if (IsNeutralObserved())
                {
                    return true;
                }

                try
                {
                    await Task.Delay(25, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            return IsNeutralObserved();
        }

        /// <summary>True when every configured amplifier reports fresh, current neutral PWM.</summary>
        private bool IsNeutralObserved()
        {
            if (_trackVariables is null)
            {
                return false;
            }

            var items = _trackVariables.trackAmpItems;
            if (items is null)
            {
                return false;
            }

            foreach (var slave in _amplifierGroups.AllConfigured)
            {
                var item = items.FirstOrDefault(a => a is not null && a.SlaveNumber == slave);
                if (!TrackAmplifierDataFreshness.IsCurrentData(item))
                {
                    return false;
                }

                var registers = item!.HoldingReg;
                if (registers is null || registers.Length == 0)
                {
                    return false;
                }

                if ((registers[TrackAmplifierRegisters.PwmCommand] & 0x03FF) != AmplifierSpeedMapper.NeutralPwm)
                {
                    return false;
                }
            }

            return true;
        }

        private static ITrackTransport CreateRealTransport()
        {
            var rawUdp = new RawUdpTransport(
                CoreConfiguration.TrackControllerIpAddress,
                CoreConfiguration.TrackControllerSendingPort,
                CoreConfiguration.TrackControllerReceivingPort);
            return new RawUdpTrackTransport(rawUdp);
        }

        // ---------------------------------------------------------------------------------
        // Stop / cleanup
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Stops every owned loop/task (runtime loop, coordinator CTS, ECoS host, comm client),
        /// then releases every runtime field and disposes the CTS. Throws when a stop cannot
        /// complete cleanly within the budget, so the caller transitions to
        /// <see cref="TrackRuntimeState.Failed"/> instead of reporting a proven stop.
        ///
        /// Teardown is guaranteed to run: the neutral-observe failure (and any teardown failure)
        /// is surfaced only AFTER every owned resource has been released, so a failed stop never
        /// leaves a stale loop/client/host/field behind.
        /// </summary>
        private async Task StopTrackPartAsync(CancellationToken budget)
        {
            Exception? neutralFault = null;

            // 0) Real-mode safety: withdraw movement permission, then command neutral to the whole
            //    configured domain and observe it BEFORE any teardown. The runtime loop and comm
            //    client must still be running for the neutral command to be written and echoed.
            //    A stop that cannot observe neutral maps to Failed (a Stopped state means "neutral
            //    was commanded AND observed before teardown"), but the fault is thrown only after
            //    teardown has completed so no resource is left running.
            if (_requestedMode == TrackControlMode.Real && _trackVariables is not null)
            {
                _movementPermission?.Withdraw();

                foreach (var slave in _amplifierGroups.AllConfigured)
                {
                    _trackVariables.SetDesiredAmplifierControl(slave, AmplifierSpeedMapper.NeutralPwm, false);
                }

                if (!await ObserveNeutralAsync(budget).ConfigureAwait(false))
                {
                    neutralFault = new InvalidOperationException(
                        "Neutral was not observed for every configured amplifier before teardown; the stop is not proven safe.");
                }
            }

            // Teardown always runs, even when neutral could not be observed or a teardown step
            // fails: the try/finally guarantees every runtime field (and the CTS) is released.
            try
            {
                // 1) Stop the runtime write loop.
                try
                {
                    _trackControlMain?.StopRuntime();
                }
                catch (Exception ex)
                {
                    RaiseFault("TrackControlMain.StopRuntime", ex);
                }

                // 2) Cancel the coordinator CTS.
                try
                {
                    _cts?.Cancel();
                }
                catch (Exception ex)
                {
                    RaiseFault("CancellationTokenSource.Cancel", ex);
                }

                // 3) Stop the ECoS host (bounded). A non-clean result means the stop is not proven.
                var hostResult = await _ecosHost.StopAsync(budget).ConfigureAwait(false);

                // 4) Dispose the comm client, never blocking with .Wait().
                if (_trackCommClient is IAsyncDisposable disposable)
                {
                    await disposable.DisposeAsync().AsTask().WaitAsync(budget).ConfigureAwait(false);
                }

                if (hostResult is not EcosHostStopResult.Stopped and not EcosHostStopResult.AlreadyStopped)
                {
                    throw new InvalidOperationException($"ECoS host stop did not complete cleanly: {hostResult}.");
                }
            }
            finally
            {
                ReleaseRuntimeFields();
            }

            // Surface the neutral-observe failure only after teardown completed, so the caller
            // maps it to Failed with fully-released resources.
            if (neutralFault is not null)
            {
                RaiseFault("TrackApplicationRuntimeHost.StopTrackPartAsync", neutralFault);
                throw neutralFault;
            }
        }

        /// <summary>Removes the per-start logger, nulls every runtime field and disposes the CTS.</summary>
        private void ReleaseRuntimeFields()
        {
            if (_trackApplicationLogging is not null)
            {
                try
                {
                    SiebwaldeApp.Core.IoC.Logger.RemoveLogger(_trackApplicationLogging);
                }
                catch
                {
                    // Best-effort; logging cleanup must never fail a stop.
                }
            }

            _trackControlMain = null;
            _trackCommClient = null;
            _trackInitService = null;
            _trackVariables = null;
            _bootloaderHelpers = null;
            _sendNextFwDataPacket = null;
            _trackApplicationLogging = null;
            _movementPermission = null;

            if (_cts is not null)
            {
                _cts.Dispose();
                _cts = null;
            }
        }

        // ---------------------------------------------------------------------------------
        // Amplifier command surface (verbatim move from SiebwaldeApplicationModel)
        // ---------------------------------------------------------------------------------

        /// <inheritdoc />
        public void SetAmplifierControl(ushort slaveNumber, int pwmSetpoint, bool emoStop)
        {
            if (_trackVariables is null)
                return;

            // The manual/operator path is outside per-loco ownership but can issue real physical
            // control, so it must be visible in the production trace. An invalid or backplane
            // address presented to this command API is an abnormal rejection, not a normal command.
            if (!TrackAmplifierAddress.IsTrackAmplifierAddress(slaveNumber))
            {
                _controlTrace?.Abnormal(
                    "InvalidAmplifierAddress",
                    slaveNumber,
                    "manual SetAmplifierControl rejected: not a physical track amplifier");
                return;
            }

            bool accepted = _trackVariables.SetDesiredAmplifierControl(slaveNumber, pwmSetpoint, emoStop);

            if (accepted)
            {
                _controlTrace?.ManualControl(
                    slaveNumber,
                    pwmSetpoint,
                    TrackApplicationVariables.BuildHr0Value(pwmSetpoint, emoStop),
                    emoStop);
            }
            else
            {
                _controlTrace?.Abnormal(
                    "MovementPermissionNotGranted",
                    slaveNumber,
                    "manual SetAmplifierControl refused: movement permission is not granted.");
            }
        }

        // ---------------------------------------------------------------------------------
        // State + fault plumbing
        // ---------------------------------------------------------------------------------

        private void TransitionTo(TrackRuntimeState state, string? failureReason = null)
        {
            _state = state;
            StateChanged?.Invoke(this, new TrackRuntimeStatusChangedEventArgs(state, failureReason));
        }

        private void RaiseFault(string source, Exception exception)
            => Faulted?.Invoke(this, new RuntimeFaultEventArgs(source, exception));

        /// <summary>
        /// Handles an unexpected background fault from the ECoS host while the runtime is
        /// Running. It transitions to Failed (never masked as Running). During Starting/Stopping/
        /// Stopped the owning lifecycle operation already handles its own failures, so this
        /// handler deliberately ignores faults there.
        /// </summary>
        private void OnEcosHostFaulted(object? sender, RuntimeFaultEventArgs e)
        {
            if (_state != TrackRuntimeState.Running)
            {
                return;
            }

            TransitionTo(TrackRuntimeState.Failed, e.Exception?.Message);
            RaiseFault(e.Source ?? "ECoSHost", e.Exception);
        }
    }
}
