using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Comm;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;

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

        /// <summary>
        /// Stage 1 — occupancy deassert filter window (later user-configurable). A physical
        /// Occupied → Clear transition only becomes "filtered Clear" after remaining continuously
        /// Clear for this long (100 ms). Rising edges stay immediate; this is not a safety/control
        /// budget.
        /// </summary>
        private static readonly TimeSpan OccupancyClearDebounceMs = PhysicalOccupancyDeassertFilter.DefaultClearDebounceMs;

        /// <summary>
        /// Stage 2 — post-clear release tail hold (later user-configurable). After a departed source
        /// becomes filtered Clear, it is held driven for this long (500 ms) before it is released
        /// (HR0=399). It only delays releasing the OLD source; the target is already powered.
        /// </summary>
        private static readonly TimeSpan SourceReleaseTailHoldMs = LocomotiveCarrierController.DefaultSourceReleaseTailHold;

        private readonly IEcosHostService _ecosHost;
        private readonly IControlTrace? _controlTrace;
        private readonly Func<ITrackTransport>? _transportFactory;
        private readonly Func<FlashOneAmplifierRunner>? _flashRunnerFactory;
        private readonly TrackAmplifierGroups _amplifierGroups;
        private readonly TrackSimulatorConfig? _simulatorConfig;
        private readonly LayoutProfile? _fullSimulationProfile;
        private readonly TimeSpan _neutralObserveWindow;
        private readonly SemaphoreSlim _gate = new(1, 1);

        private CancellationTokenSource? _cts;
        private TrackRuntimeState _state = TrackRuntimeState.Stopped;
        private TrackControlMode? _requestedMode;
        private LayoutProfile? _activeFullSimulationProfile;
        private ISimulatedTrackIo? _simulatedTrackIo;
        private DeterministicMovementSimulator? _movementSimulator;
        private MovementSimulationAdapter? _movementSimulationAdapter;

        // Cross-process controller-session ownership: the exclusive lease held only while the REAL
        // UDP transport is composed (no injected transport factory + Real mode). Null when not
        // running, in FullSimulation/Simulator, or when a test/harness injected a transport factory.
        private TrackControllerSessionLease? _controllerSessionLease;

        // Track-part fields (moved verbatim from SiebwaldeApplicationModel).
        private TrackApplicationVariables? _trackVariables;
        private TrackCommClientAsync? _trackCommClient;
        private TrackAmplifierBootloaderHelpers? _bootloaderHelpers;
        private SendNextFwDataPacket? _sendNextFwDataPacket;
        private TrackInitStateMachine? _trackInitStateMachine;
        private readonly FirmwareDeploymentAuthorization _firmwareDeploymentAuthorization = new();
        private TrackControlMain? _trackControlMain;
        private string _loggerInstance = "TrackAppLog";
        private ILogger? _trackApplicationLogging;
        private MovementPermissionController? _movementPermission;

        // Neutral prerequisite (concept 1): true once a safe neutral PWM state has been commanded
        // AND observed (command echo AND applied PWM) on every configured amplifier. This is the
        // separate, Stage-6-independent "neutral verified" fact; movement permission (concept 3) is
        // granted only AFTER Stage 6 (track power) has also been authorized + executed. Reset per start.
        private bool _neutralVerified;
        private Kis09PassiveObserver? _kis09PassiveObserver;
        private Kis09PassiveObserverConsumer? _kis09PassiveConsumer;
        private LocomotiveCarrierFeeder? _kis09CarrierFeeder;
        public GlobalStopController GlobalStop { get; } = new();
        private GlobalStopMonitor? _globalStopMonitor;
        private GlobalStopConfirmationState? _stopConfirmation;
        private Task? _globalStopTask;
        private CarrierTimingTrace? _carrierTimingTrace;

        // Operator-controlled global-stop recovery gate (startup precondition). Both are fail-closed
        // and re-armed fresh per start; recovery never proceeds without an explicit operator action
        // (AuthorizeGlobalStopRecovery) AND positive physical-stop-clear evidence.
        private volatile bool _operatorRecoveryAuthorized;
        private Func<bool> _physicalStopInputsClear = () => false;

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
        /// <param name="simulatorConfig">
        /// Optional simulator configuration used when <see cref="TrackControlMode.FullSimulation"/>
        /// is started without an explicit transport factory. Defaults to a periodic-SLAVEINFO
        /// configuration over the default detected slaves {1,2,3}.
        /// </param>
        /// <param name="fullSimulationProfile">
        /// Optional layout profile that drives the FullSimulation movement simulator and (via its
        /// detected slaves) the simulator transport config. When null, FullSimulation runs without
        /// the deterministic movement simulator (the pre-existing config-driven transport only).
        /// </param>
        public TrackApplicationRuntimeHost(
            IEcosHostService ecosHost,
            IControlTrace? controlTrace = null,
            Func<ITrackTransport>? transportFactory = null,
            TrackAmplifierGroups? amplifierGroups = null,
            TimeSpan? neutralObserveWindow = null,
            TrackSimulatorConfig? simulatorConfig = null,
            LayoutProfile? fullSimulationProfile = null,
            Func<FlashOneAmplifierRunner>? flashRunnerFactory = null)
        {
            _ecosHost = ecosHost ?? throw new ArgumentNullException(nameof(ecosHost));
            if (ecosHost is TrackControlHost controlHost) controlHost.GlobalStop = GlobalStop;
            GlobalStop.Latched += () => _movementSimulator?.SetPower(false);
            // Comm/host-authority fault recovery: when the global stop latches, the init machine
            // (if composed) transitions to RecoveryHold and invalidates the now-stale stage evidence.
            // The desired-setpoint neutralization itself is performed by
            // TrackApplicationVariables.ApplyGlobalStop, subscribed when the variables bind this
            // controller; EnterRecoveryHold guards the reason (comm/host-authority only) and the
            // run progress (must have reached NormalOperation), so an operator/Koploper stop or a
            // still-starting run is a no-op.
            GlobalStop.Latched += OnGlobalStopLatched;
            _controlTrace = controlTrace;
            _transportFactory = transportFactory;
            _flashRunnerFactory = flashRunnerFactory;
            _amplifierGroups = amplifierGroups ?? TrackAmplifierGroups.Empty;
            _neutralObserveWindow = neutralObserveWindow ?? DefaultNeutralObserveWindow;
            _simulatorConfig = simulatorConfig;
            _fullSimulationProfile = fullSimulationProfile;

            // Surface an unexpected background fault from the host while the runtime is Running.
            _ecosHost.Faulted += OnEcosHostFaulted;
        }

        /// <inheritdoc />
        public TrackRuntimeState State => _state;

        /// <inheritdoc />
        public TrackControlMode? LastRequestedMode => _requestedMode;

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
        public ISimulatedTrackIo? SimulatedTrackIo => _simulatedTrackIo;

        /// <inheritdoc />
        public IMovementSimulation? MovementSimulation => _movementSimulator;

        /// <inheritdoc />
        public string? ActiveProfileName => EffectiveFullSimulationProfile?.Name;

        /// <inheritdoc />
        public LayoutProfile? ActiveFullSimulationProfile => EffectiveFullSimulationProfile;

        /// <inheritdoc />
        public IReadOnlyList<int> PhysicalAmplifierBinding
            => EffectiveFullSimulationProfile is { } profile
                ? profile.ToPhysicalDomain().Select(address => (int)address).ToArray()
                : Array.Empty<int>();

        /// <summary>
        /// The profile that drives FullSimulation: the profile last selected via
        /// <see cref="SelectFullSimulationProfile"/>, or the constructor-supplied default when none
        /// was selected.
        /// </summary>
        private LayoutProfile? EffectiveFullSimulationProfile
            => _activeFullSimulationProfile ?? _fullSimulationProfile;

        /// <inheritdoc />
        public void SelectFullSimulationProfile(LayoutProfile profile)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            // The host guards "not running"; setting the host first keeps this selection atomic.
            _ecosHost.SetProfile(profile);
            _activeFullSimulationProfile = profile;
        }

        /// <inheritdoc />
        public List<TrackAmplifierItem> GetAmplifierListing()
            => _trackVariables?.GetAmplifierListing() ?? new List<TrackAmplifierItem>();

        /// <summary>
        /// The shared track-application variables container (the same container the control stack and
        /// comm client use). Read-only exposure for a passive KIS-09 observer; it does NOT expose the
        /// comm client or any write path. Null before the track part is composed.
        /// </summary>
        public TrackApplicationVariables? TrackVariables => _trackVariables;

        /// <inheritdoc />
        public IReadOnlyList<TrackInitStageStatus> InitStages
            => _trackInitStateMachine?.Stages ?? Array.Empty<TrackInitStageStatus>();

        /// <inheritdoc />
        public event EventHandler<TrackInitStageStatusChangedEventArgs>? InitStageStatusChanged;

        /// <inheritdoc />
        public bool TrackPowerAuthorized => _trackInitStateMachine?.TrackPowerAuthorized ?? false;

        /// <inheritdoc />
        public bool TrackPowerOn => _trackInitStateMachine?.TrackPowerOn ?? false;

        /// <inheritdoc />
        public InitializationStatus RunStatus => _trackInitStateMachine?.RunStatus ?? InitializationStatus.Idle;

        /// <inheritdoc />
        public void AuthorizeTrackPowerEnable() => _trackInitStateMachine?.AuthorizeTrackPowerEnable();

        /// <inheritdoc />
        public async Task<bool> EnableTrackPowerAsync(CancellationToken ct = default)
        {
            // Fail-closed: Stage 6 (track power) requires the initialization state machine AND the
            // neutral prerequisite (concept 1) before it may energize. The grant (concept 3) happens
            // only AFTER the state machine has confirmed track power ON — the state machine itself
            // stays decoupled from the movement-permission grant.
            if (_trackInitStateMachine is null)
            {
                return false;
            }

            if (!_neutralVerified)
            {
                return false;
            }

            var success = await _trackInitStateMachine.EnableTrackPowerAsync(ct).ConfigureAwait(false);
            if (success)
            {
                _movementPermission?.Grant();
            }

            return success;
        }

        /// <inheritdoc />
        public Task<bool> DisableTrackPowerAsync(CancellationToken ct = default)
            => _trackInitStateMachine?.DisableTrackPowerAsync(ct) ?? Task.FromResult(true);

        /// <inheritdoc />
        public Task<TrackInitStageState> StepInitAsync(CancellationToken ct = default)
            => _trackInitStateMachine?.StepAsync(ct) ?? Task.FromResult(TrackInitStageState.Pending);

        /// <inheritdoc />
        public Task<TrackInitStageState> RetryInitAsync(CancellationToken ct = default)
            => _trackInitStateMachine?.RetryAsync(ct) ?? Task.FromResult(TrackInitStageState.Pending);

        /// <inheritdoc />
        public void BacktrackInit() => _trackInitStateMachine?.Backtrack();

        /// <summary>
        /// Re-validates a run that entered <see cref="InitializationStatus.RecoveryHold"/> after a
        /// comm/host-authority fault. It re-runs the existing automatic initialization path (which
        /// re-issues the Stage 1 connect (250), controller-version (113) and global-stop precondition
        /// (111), re-confirms Stages 2/3 via fresh readback, and reaches the NormalOperation step),
        /// then re-runs Stage 4 (config APPLY + verify) and Stage 5 (GROUP_READY + verify) through the
        /// production Phase-1 host path, and STOPS before Stage 6 (track power, operator-gated). No
        /// second execution path is used: the same step dictionary and the same
        /// <see cref="InitializePhase1AmplifiersAsync"/> / <see cref="WriteGroupReadyAsync"/> are reused.
        ///
        /// The reconnect does NOT clear a latched global stop: the Stage 1
        /// <see cref="GlobalStopPreconditionStep"/> keeps blocking until an explicit operator
        /// <see cref="AuthorizeGlobalStopRecovery"/> re-arms the controller (and the physical stop
        /// inputs are confirmed clear). Movement permission is NOT re-granted and neutral is NOT
        /// re-observed here (it stays NotGranted until Stage 6 is re-authorized + executed, and the
        /// sticky latch keeps the movement gate fail-closed regardless). Returns the resulting
        /// <see cref="InitializationStatus"/>: <see cref="InitializationStatus.StoppedAtGlobalReady"/>
        /// on a healthy revalidation, or a non-StoppedAtGlobalReady status when an amplifier is
        /// missing/incompatible (stages 4/5 fail closed).
        /// </summary>
        public async Task<InitializationStatus> RevalidateAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_trackInitStateMachine is null || _trackCommClient is null)
                {
                    return InitializationStatus.Idle;
                }

                // Re-run stages 1..7 (automatic). Stage 1 re-issues the 250 reconnect + 113 version +
                // 111 global-stop precondition (which blocks on a still-latched controller until
                // operator recovery); Stages 2/3 are re-confirmed by fresh readback; the run reaches
                // the NormalOperation step and defers completion to host finalization of stages 4/5.
                // The operator-gated Stage 6 is never auto-run.
                await _trackInitStateMachine.RunAutomaticAsync(ct).ConfigureAwait(false);

                if (_trackInitStateMachine.IsFailed ||
                    _trackInitStateMachine.RunStatus == InitializationStatus.Cancelled)
                {
                    return _trackInitStateMachine.RunStatus;
                }

                // Re-run Stage 4 (config APPLY + verify) and Stage 5 (GROUP_READY + verify) via the
                // production Phase-1 host path (fail-closed per amplifier).
                await InitializePhase1AmplifiersAsync(ct).ConfigureAwait(false);

                var domain = EffectiveDomain();
                var allReady = AllDomainAmplifiersConfigured(domain);
                if (allReady)
                {
                    _trackInitStateMachine.MarkStagePassed(TrackInitStage.ConfigureValidate);
                    var groupReadyOk = await WriteGroupReadyAsync(domain, ct).ConfigureAwait(false);
                    if (groupReadyOk)
                    {
                        _trackInitStateMachine.MarkStagePassed(TrackInitStage.GlobalReady);
                    }
                    else
                    {
                        _trackInitStateMachine.MarkStageFailed(
                            TrackInitStage.GlobalReady,
                            "GROUP_READY rejected by one or more required amplifiers.",
                            "Check the rejecting amplifier's local conditions (enable/config/fault/lease) and re-run.");
                    }
                }
                else
                {
                    const string recovery =
                        "Re-run the startup or check the affected amplifier's identity/config.";
                    _trackInitStateMachine.MarkStageFailed(
                        TrackInitStage.ConfigureValidate,
                        "One or more required amplifiers did not complete configuration.",
                        recovery);
                    _trackInitStateMachine.MarkStageFailed(
                        TrackInitStage.GlobalReady,
                        "Not all required amplifiers are ready (config_ok + no Phase1Failure + identity/compatibility verified).",
                        recovery);
                }

                return _trackInitStateMachine.RunStatus;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <inheritdoc />
        public bool FirmwareDeploymentAuthorized => _firmwareDeploymentAuthorization.Authorized;

        /// <inheritdoc />
        public Task<IReadOnlyList<FlashOneRunResult>> AuthorizeFirmwareDeploymentAsync(
            IReadOnlyCollection<byte> targetAmps,
            string expectedSha256Hex,
            CancellationToken ct = default)
            => AuthorizeFirmwareDeploymentAsync(targetAmps, expectedSha256Hex, hexPath: null, ct);

        /// <summary>
        /// Overload for offline tests that need a specific firmware artifact path (the production
        /// default is <see cref="CoreConfiguration.TrackAmplifierFirmwarePath"/>).
        /// </summary>
        public async Task<IReadOnlyList<FlashOneRunResult>> AuthorizeFirmwareDeploymentAsync(
            IReadOnlyCollection<byte> targetAmps,
            string expectedSha256Hex,
            string? hexPath,
            CancellationToken ct = default)
        {
            var results = new List<FlashOneRunResult>();

            if (targetAmps is null || targetAmps.Count == 0)
            {
                results.Add(FlashOneRunResult.Fail("No target amplifier set supplied.", 0));
                return results;
            }

            if (string.IsNullOrWhiteSpace(expectedSha256Hex))
            {
                results.Add(FlashOneRunResult.Fail(
                    "An exact expected firmware SHA-256 is required before any deployment.", 0));
                return results;
            }

            // The explicit operator action authorizes exactly ONE bounded deployment for the given
            // targets. It is the ONLY way the authorization flag becomes true: automatic/manual
            // Step/Retry/Backtrack and bootloader recovery cannot set it.
            _firmwareDeploymentAuthorization.Authorize();

            try
            {
                // Reuse the proven command-131 deployment runner (never 122). It re-validates the
                // firmware SHA-256 (FirmwareArtifactVerifier.Validate) and acquires the
                // controller-session lease, so this path requires BOTH the explicit authorization
                // AND the existing session-lease ownership.
                var path = string.IsNullOrWhiteSpace(hexPath)
                    ? CoreConfiguration.TrackAmplifierFirmwarePath
                    : hexPath;

                var runner = CreateFlashRunner();
                foreach (var amp in targetAmps.Distinct())
                {
                    if (!TrackAmplifierAddress.IsTrackAmplifierAddress(amp))
                    {
                        results.Add(FlashOneRunResult.Fail(
                            $"Amplifier address {amp} is outside the valid range 1..50.", amp));
                        continue;
                    }

                    results.Add(await runner.RunAsync(path, expectedSha256Hex, amp, ct).ConfigureAwait(false));
                }
            }
            finally
            {
                // One-shot: the authorization is consumed whether the deployment succeeded or not.
                _firmwareDeploymentAuthorization.Reset();
            }

            return results;
        }

        /// <summary>
        /// Builds the command-131 deployment runner. Production uses the real UDP transport and the
        /// real ownership preflight; tests inject a runner with a scripted transport and a
        /// permissive preflight via the optional <c>flashRunnerFactory</c> constructor argument.
        /// </summary>
        private FlashOneAmplifierRunner CreateFlashRunner()
            => _flashRunnerFactory?.Invoke()
               ?? new FlashOneAmplifierRunner(_transportFactory);

        /// <summary>The passive KIS-09 observer (KIS-07 shadow + KIS-08 reconciliation over the production variables), or null.</summary>
        public Kis09PassiveObserver? Kis09PassiveObserver => _kis09PassiveObserver;

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

                // Link the caller's cancellation token into the runtime CTS so a cancelled start
                // (for example a bounded timeout while the controller is silent) tears the in-flight
                // composition down deterministically instead of leaving the init machine stuck while
                // holding the lifecycle gate. In production the caller passes CancellationToken.None,
                // so only StopAsync cancels the runtime once it is running.
                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                string? failure = null;
                Exception? fault = null;

                try
                {
                    if (mode.IsFullTrackChain())
                    {
                        await ComposeTrackPartAsync(mode, _cts.Token).ConfigureAwait(false);
                    }

                    var result = await _ecosHost.StartAsync(
                        mode,
                        _trackCommClient,
                        _trackVariables,
                        _cts.Token,
                        _movementSimulator).ConfigureAwait(false);

                    if (result is EcosHostStartResult.Started
                        or EcosHostStartResult.AlreadyActive
                        or EcosHostStartResult.Transitioned)
                    {
                        // Start the movement simulation once the ECoS host is up, so simulated
                        // occupancy flows through the real transport -> comm -> occupancy path.
                        _movementSimulationAdapter?.Start(_cts.Token);

                        // Full-chain mode: identity-check + config-download + APPLY + verify each
                        // domain amplifier (fail-closed), then command + observe neutral (command
                        // echo AND applied PWM) before reporting Running.
                        if (mode.IsFullTrackChain())
                        {
                            await InitializePhase1AmplifiersAsync(_cts.Token).ConfigureAwait(false);

                            // Stages 4 (configure + validate) and 5 (global ready) are host-verified
                            // against the SAME readiness source: every required amplifier must have
                            // completed config download + APPLY + readback (ConfigurationVerified)
                            // with no Phase1Failure (identity/protocol compatibility verified). The
                            // step runner never auto-passes these two stages. Finalizing them here
                            // (before commanding neutral) lets the init run COMPLETE first, so the
                            // runtime write loop is running when neutral is commanded and observed.
                            var domain = EffectiveDomain();
                            var allReady = AllDomainAmplifiersConfigured(domain);
                            if (allReady)
                            {
                                _trackInitStateMachine?.MarkStagePassed(TrackInitStage.ConfigureValidate);

                                // Stage 5: issue the C#-decided GROUP_READY transition and verify
                                // every required amplifier accepted it. GROUP_READY never energizes.
                                var groupReadyOk = await WriteGroupReadyAsync(domain, _cts.Token)
                                    .ConfigureAwait(false);
                                if (groupReadyOk)
                                {
                                    _trackInitStateMachine?.MarkStagePassed(TrackInitStage.GlobalReady);
                                }
                                else
                                {
                                    _trackInitStateMachine?.MarkStageFailed(
                                        TrackInitStage.GlobalReady,
                                        "GROUP_READY rejected by one or more required amplifiers.",
                                        "Check the rejecting amplifier's local conditions (enable/config/fault/lease) and re-run.");
                                }
                            }
                            else
                            {
                                const string recovery =
                                    "Re-run the startup or check the affected amplifier's identity/config.";
                                _trackInitStateMachine?.MarkStageFailed(
                                    TrackInitStage.ConfigureValidate,
                                    "One or more required amplifiers did not complete configuration.",
                                    recovery);
                                _trackInitStateMachine?.MarkStageFailed(
                                    TrackInitStage.GlobalReady,
                                    "Not all required amplifiers are ready (config_ok + no Phase1Failure + identity/compatibility verified).",
                                    recovery);
                            }

                            // Start the runtime write loop now (idempotent), BEFORE commanding +
                            // observing neutral. It must run in BOTH the non-energized
                            // (StoppedAtGlobalReady) and energized paths so the neutral command is
                            // actually written and echoed. The Completed -> StartRuntime handler
                            // below remains for the (legacy/idempotent) completion path.
                            _trackControlMain?.StartRuntime(_cts.Token);

                            // Command + observe neutral (command echo AND applied PWM) only AFTER
                            // stages 1..5 are finalized and the init run stopped at GlobalReady.
                            // Establishing observed neutral here sets the neutral-verified flag
                            // (concept 1) but does NOT grant movement permission (concept 3) — that
                            // happens only after Stage 6 is authorized + executed.
                            await EstablishObservedNeutralAsync(_cts.Token).ConfigureAwait(false);
                        }

                        // Phase 3: feed the ownership-aware carrier from the KIS-09 passive observer
                        // (same shared variables/shadow) and route its escalation flags into the
                        // existing safety latch. No second comm client.
                        if (mode.IsFullTrackChain() &&
                            _kis09PassiveObserver is { } carrierObserver &&
                            _ecosHost is TrackControlHost carrierHost &&
                            carrierHost.Carrier is { } carrier &&
                            carrierHost.Safety is { } carrierSafety)
                        {
                            _kis09CarrierFeeder = new LocomotiveCarrierFeeder(
                                carrierObserver,
                                carrier,
                                decision => CarrierEscalation.Route(decision, carrierSafety, carrier.AuthorityLifecycle),
                                clearDebounceMs: OccupancyClearDebounceMs,
                                timingTrace: _carrierTimingTrace);
                            _kis09CarrierFeeder.Start(_cts.Token);
                        }

                        TransitionTo(TrackRuntimeState.Running);
                        _globalStopMonitor?.EstablishLink(DateTimeOffset.UtcNow);
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

        private async Task ComposeTrackPartAsync(TrackControlMode mode, CancellationToken ct)
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
            _trackVariables.GlobalStop = GlobalStop;

            // Fresh movement-permission controller, shared by the movement gate (on the variables)
            // and the safety interlock (via the ECoS host). Created NotGranted and only granted
            // after observed neutral AND Stage 6 (track power) has been authorized + executed.
            // Set BEFORE comm/host start so both paths see the same state.
            _movementPermission = new MovementPermissionController();
            _trackVariables.MovementPermission = _movementPermission;

            // Fresh neutral-verified flag per start: never carried over from a previous run.
            _neutralVerified = false;

            // 2b) Cross-process controller-session ownership. The lease is held ONLY for the REAL
            //     UDP transport (no injected transport factory + Real mode): that is the sole path
            //     that binds the real controller port (10001), which must be exclusive across
            //     processes (the primary authority, closing the preflight-to-bind race).
            //     FullSimulation and Simulator use software transports / no track transport, and any
            //     injected in-process transport (tests, harnesses) binds no real controller port, so
            //     all of those skip the lease — acquiring it there would only add a shared-mutable
            //     cross-process race with no real port to protect. A failed acquisition (another
            //     session owns the controller) throws, which StartAsync surfaces as an init fault and
            //     a Failed state — the start fails safely before any bind/send.
            if (_transportFactory is null && mode == TrackControlMode.Real)
            {
                _controllerSessionLease = TrackControllerSessionLease.TryAcquire(
                    TrackControllerSessionLease.WpfOwnerName, out var blockingOwner)
                    ?? throw new InvalidOperationException(
                        $"The track-controller session is owned by {(blockingOwner ?? "another process")}. " +
                        "Close it before starting the SiebwaldeApp control application.");
            }

            // 3) Build low-level Ethernet / Modbus transport.
            var transport = _transportFactory?.Invoke()
                ?? (mode == TrackControlMode.FullSimulation ? CreateSimulatorTransport() : CreateRealTransport());

            // 4) Communication client on top of the selected transport.
            _trackCommClient = new TrackCommClientAsync(transport, _trackVariables);
            _globalStopMonitor = new GlobalStopMonitor(_trackCommClient, _trackVariables);
            _stopConfirmation = new GlobalStopConfirmationState(_trackCommClient);

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
            // Fresh per start: a latched global stop must be re-armed only by an explicit operator
            // action (see AuthorizeGlobalStopRecovery), never automatically.
            _operatorRecoveryAuthorized = false;
            _physicalStopInputsClear = () => false;

            // Fresh, session-scoped, in-memory firmware-deployment authorization. It starts
            // unauthorized and is never persisted, so a restart can never reuse a prior grant.
            _firmwareDeploymentAuthorization.Reset();

            var steps = new IInitializationStep[]
            {
                new ConnectToEthernetTargetStep(_trackCommClient, _trackVariables, _loggerInstance),
                new QueryControllerVersionStep(_trackCommClient, _trackVariables, _loggerInstance),
                new GlobalStopPreconditionStep(
                    _trackCommClient,
                    operatorPermitRecovery: () => _operatorRecoveryAuthorized,
                    physicalStopInputsClear: () => _physicalStopInputsClear(),
                    onRearmConfirmed: () => GlobalStop.ReArm(),
                    loggerInstance: _loggerInstance),
                new ResetAllSlavesStep(_trackCommClient, _trackVariables, _loggerInstance),
                new DataUploadStep(_trackCommClient, _trackVariables, _loggerInstance),
                new DetectSlavesStep(_trackCommClient, _trackVariables, _loggerInstance),
                new RecoverSlavesStep(_trackCommClient, _trackVariables, _sendNextFwDataPacket, _bootloaderHelpers, _loggerInstance,
                    firmwareDeployment: _firmwareDeploymentAuthorization),
                new FlashFwTrackamplifiersStep(_trackCommClient, _trackVariables, _sendNextFwDataPacket, _bootloaderHelpers, _loggerInstance,
                    firmwareDeployment: _firmwareDeploymentAuthorization),
                new InitTrackamplifiersStep(_trackCommClient, _loggerInstance),
                new SetDefaultPwmSetpointsStep(_trackVariables, _loggerInstance),
                new EnableTrackamplifiersStep(_trackCommClient, _loggerInstance),
            };

            _trackInitStateMachine = new TrackInitStateMachine(
                _trackCommClient,
                _trackVariables,
                steps,
                requiredAmps: () => EffectiveDomain(),
                _loggerInstance,
                firmwareDeployment: _firmwareDeploymentAuthorization);

            _trackInitStateMachine.LogEvent += (_, e) =>
            {
                SiebwaldeApp.Core.IoC.Logger.Log(
                    $"TrackInit[{e.Stage}] {e.Message}", _loggerInstance);
            };

            _trackInitStateMachine.StageStatusChanged += (_, e) =>
            {
                InitStageStatusChanged?.Invoke(this, e);
            };

            // 6b) Carrier timing trace: a non-blocking JSONL diagnostic for the carrier-chain
            //     latency. Best-effort; created before the runtime writer so the write latency hook
            //     is available.
            _carrierTimingTrace = new CarrierTimingTrace(
                Path.Combine(CoreConfiguration.LogDirectory, "carrier-timing.jsonl"));

            // 7) Create TrackControlMain and hook RunStatusChanged to start the runtime loop.
            _trackControlMain = new TrackControlMain(
                _loggerInstance,
                _trackCommClient,
                _trackVariables,
                _controlTrace,
                onAmplifierWrite: _carrierTimingTrace.RecordWrite);

            _trackInitStateMachine.RunStatusChanged += (_, status) =>
            {
                SiebwaldeApp.Core.IoC.Logger.Log($"Track init status: {status.Status}", _loggerInstance);

                if (status.Status == InitializationStatus.StoppedAtGlobalReady)
                {
                    // The non-energized test scope (stages 1..5 through GlobalReady) succeeded, but
                    // the operator-gated Stage 6 (track power) was deliberately not performed, so
                    // the full seven-stage operating sequence has NOT run. Never start the live
                    // runtime loop here and never treat this as a failure.
                    SiebwaldeApp.Core.IoC.Logger.Log(
                        "Track init stopped at GlobalReady (non-energized): test scope complete.", _loggerInstance);
                    return;
                }

                if (status.Status == InitializationStatus.Completed &&
                    _trackControlMain != null &&
                    _cts != null)
                {
                    _trackControlMain.StartRuntime(_cts.Token);
                }
            };

            // 8) Start communication and run the initialization state machine (automatic mode).
            await _trackCommClient.StartAsync(true, ct).ConfigureAwait(false);
            _globalStopTask = MonitorGlobalStopAsync(ct);
            await _trackInitStateMachine.RunAutomaticAsync(ct).ConfigureAwait(false);

            // A genuine init failure/cancellation never throws from RunAutomaticAsync; observe the
            // status instead so a failed real-mode start is never masked as Running. A run that
            // reached NormalOperation but is still awaiting host finalization of stages 4/5 (and the
            // operator-gated Stage 6) is NOT a failure: it stays Running until the host finalizes
            // stages 4/5, at which point it becomes Completed (if Stage 6 also passed) or
            // StoppedAtGlobalReady (test scope complete, non-energized). Neither of those is a
            // failure, so only Failed/Cancelled throw here.
            if (_trackInitStateMachine.IsFailed ||
                _trackInitStateMachine.RunStatus == InitializationStatus.Cancelled)
            {
                throw new InvalidOperationException(
                    $"Track initialization did not complete: {_trackInitStateMachine.RunStatus}.");
            }

            // 9) Passive KIS-09 observer: KIS-07 shadow + KIS-08 reconciliation reading the SAME
            //    TrackApplicationVariables container the control stack uses. No second comm client.
            if (_trackVariables is not null && EffectiveFullSimulationProfile is { } passiveProfile)
            {
                _kis09PassiveObserver = Kis09PassiveObserverFactory.Create(_trackVariables, passiveProfile);

                // 10) Bounded background consumer that drives the observer and writes the passive JSONL trace.
                _kis09PassiveConsumer = new Kis09PassiveObserverConsumer(
                    _kis09PassiveObserver,
                    Path.Combine(CoreConfiguration.LogDirectory, "kis09-passive.jsonl"));
                _kis09PassiveConsumer.Start(_cts.Token);
            }

            SiebwaldeApp.Core.IoC.Logger.Log(
                $"Carrier physical-source release (two-stage): " +
                $"OccupancyClearDebounceMs={OccupancyClearDebounceMs.TotalMilliseconds} ms, " +
                $"SourceReleaseTailHoldMs={SourceReleaseTailHoldMs.TotalMilliseconds} ms.",
                "");

            SiebwaldeApp.Core.IoC.Logger.Log("Track Application started.", "");
        }

        // ---------------------------------------------------------------------------------
        // Observed-neutral establishment
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Phase-1 identity + protocol gate + config download + APPLY + readback verification for
        /// every domain amplifier (AC6 / AC2/AC3). An amplifier whose HR7 identity is incompatible
        /// is left with <c>Phase1Failure = "incompatible firmware"</c>, and an amplifier with a
        /// compatible HR7 identity but a mismatched/absent HR22 PROTOCOL_VERSION is left with
        /// <c>Phase1Failure = "incompatible protocol version"</c>; either is never configured,
        /// enabled or granted drive. A compatible amplifier has its config set downloaded and
        /// APPLYed, and only becomes <see cref="TrackAmplifierItem.ConfigurationVerified"/> once
        /// <c>HR_STATE</c> reports <c>INIT_DONE</c>+<c>CONFIG_OK</c> with the config echoed back.
        /// Failures are reported per amplifier (never thrown), and the grant path blocks on them.
        /// </summary>
        private async Task InitializePhase1AmplifiersAsync(CancellationToken ct)
        {
            // Diagnostic-only correlation for the whole Phase-1 config run (stages 4/5 host path).
            // The downloader receives the same sink, so host and downloader lines share the ID.
            string correlationId = "P1CFG-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            void Log(string message) =>
                SiebwaldeApp.Core.IoC.Logger.Log($"[{correlationId}] {message}", _loggerInstance, LogLevel.Informative);

            if (_trackVariables is null || _trackCommClient is null)
            {
                Log("SKIPPED (track variables or comm client not composed)");
                return;
            }

            var domain = EffectiveDomain();
            if (domain.Count == 0)
            {
                Log("SKIPPED (empty effective domain)");
                return;
            }

            Log($"ENTRY domain=[{string.Join(",", domain)}]");

            // The 111 stop-confirmation payload is decoded against the physical detected-amplifier
            // list in ascending slave-address order (the PIC32 packs StopDelivery[i] != STOP_UNUSED,
            // i.e. SlaveDetected, ascending) — NOT the effective/configured domain.
            _stopConfirmation?.SetDetectedAmplifiers(
                _trackVariables.trackAmpItems
                    .Where(a => TrackAmplifierAddress.IsTrackAmplifierAddress(a.SlaveNumber) && a.SlaveDetected != 0)
                    .OrderBy(a => a.SlaveNumber)
                    .Select(a => a.SlaveNumber)
                    .ToArray());

            // Re-establish a fresh, genuine protocol readback for every domain amplifier before the
            // identity/config gate: SetDefaultPwmSetpointsStep clears ProtocolReadbackObserved, so a
            // fresh read is required to re-prove "detected + fresh + genuine" before checking HR7.
            await RefreshReadbackAsync(domain, ct).ConfigureAwait(false);

            var downloader = new AmplifierConfigurationDownloader(_trackCommClient, Log);
            var config = new AmplifierConfiguration();

            foreach (var slave in domain)
            {
                if (ct.IsCancellationRequested)
                {
                    Log($"SKIPPED (cancellation requested before slave {slave})");
                    return;
                }

                var item = _trackVariables.trackAmpItems[slave];
                var freshnessAge = item.LastDataReceivedUtc is DateTimeOffset received
                    ? DateTimeOffset.UtcNow - received
                    : (TimeSpan?)null;

                // Diagnostic: raw identity/version registers + individual compatibility, captured for
                // every amplifier at the compatibility decision point (reads cached HoldingReg only —
                // no new controller transaction). A fresh timestamp does NOT prove every register was
                // refreshed in the same SLAVEINFO cycle: HR7/HR22 == 0x0000 indicates an unpopulated/
                // legacy/partial acquisition, which must be distinguished from a genuinely
                // incompatible amplifier.
                var regs = item.HoldingReg;
                ushort hr7 = regs[TrackAmplifierRegisters.Identity];
                ushort hr11 = regs[TrackAmplifierRegisters.SwChecksum];
                ushort hr21 = regs[TrackAmplifierRegisters.FwVersionMajorMinor];
                ushort hr22 = regs[TrackAmplifierRegisters.FwVersionPatchProtocol];
                bool identityOk = TrackAmplifierRegisters.HasCompatibleIdentityFields(hr7);
                bool protocolOk = TrackAmplifierRegisters.HasCompatibleProtocolVersion(hr22);

                Log(
                    $"slave={slave} detected={item.SlaveDetected} protocolObserved={item.ProtocolReadbackObserved} " +
                    $"freshAgeMs={(freshnessAge?.TotalMilliseconds ?? -1):F0} seq={item.ProtocolReadbackSequence} " +
                    $"HR7=0x{hr7:X4} HR11=0x{hr11:X4} HR21=0x{hr21:X4} HR22=0x{hr22:X4} " +
                    $"identityOk={identityOk} protocolOk={protocolOk}");

                if (item.SlaveDetected == 0)
                {
                    item.Phase1Failure ??= "not detected";
                    item.ConfigurationVerified = false;
                    Log($"slave={slave} SKIPPED not detected");
                    continue;
                }

                if (!AmplifierPhase1Safety.Compatible(item))
                {
                    // Identity/protocol fail-closed: never download, never enable, never grant for
                    // this amp. The reason distinguishes a legacy/unknown identity from a Phase-1
                    // identity carrying a mismatched HR22 PROTOCOL_VERSION. The raw registers and the
                    // individual compatibility results were already logged above at the decision point.
                    item.Phase1Failure ??= AmplifierPhase1Safety.IncompatibilityReason(item)
                        ?? AmplifierPhase1Safety.IncompatibleFirmware;
                    item.ConfigurationVerified = false;
                    Log($"slave={slave} SKIPPED incompatible: {item.Phase1Failure}");
                    continue;
                }

                Log($"slave={slave} INVOKE DownloadAsync");
                var ok = await downloader.DownloadAsync(item, config, ct).ConfigureAwait(false);
                if (!ok)
                {
                    item.ConfigurationVerified = false;
                }

                Log(
                    $"slave={slave} DownloadAsync result={ok} " +
                    $"ConfigurationVerified={item.ConfigurationVerified} " +
                    $"Phase1Failure={item.Phase1Failure ?? "(null)"}");
            }

            Log("EXIT");
        }

        /// <summary>Issues a fresh FC03 read to every domain amplifier and waits (bounded) until each
        /// has a genuine, NEW protocol readback, re-proving detected + fresh + genuine evidence.</summary>
        private async Task RefreshReadbackAsync(IReadOnlyCollection<byte> domain, CancellationToken ct)
        {
            // Capture the sequence BEFORE issuing the read, so "fresh" requires a NEW readback rather
            // than a previously-observed one. This matters on revalidation (RecoveryHold): a prior run
            // may already have ProtocolReadbackObserved set, and a stale timestamp would otherwise be
            // mistaken for a fresh, genuine readback before the identity/config gate.
            var sequenceBefore = domain.ToDictionary(
                slave => slave,
                slave => _trackVariables!.trackAmpItems[slave].ProtocolReadbackSequence);

            foreach (var slave in domain)
            {
                await _trackCommClient!.SendAsync(
                    AmplifierConfigurationDownloader.Exchange(slave, TrackAmplifierRegisters.Identity, 0, read: true),
                    ct).ConfigureAwait(false);
            }

            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(2);
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                if (domain.All(s => _trackVariables!.trackAmpItems[s].ProtocolReadbackSequence > sequenceBefore[s]))
                {
                    return;
                }

                await Task.Delay(10, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// True when every domain amplifier has passed the identity gate and completed config
        /// download + APPLY + readback verification. Movement permission must never be granted
        /// while any domain amplifier is unconfigured (fail closed).
        /// </summary>
        private bool AllDomainAmplifiersConfigured(IReadOnlyCollection<byte> domain)
        {
            if (_trackVariables is null)
            {
                return false;
            }

            foreach (var slave in domain)
            {
                var item = _trackVariables.trackAmpItems[slave];
                if (!item.ConfigurationVerified || item.Phase1Failure is not null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Issues the C#-decided GROUP_READY transition (HR_TRACK_POWER bit 1) to every required
        /// amplifier and verifies each accepted it (its HR_STATE left STANDBY). GROUP_READY never
        /// energizes; TRACK_POWER_ENABLE (bit 0, Stage 6) is the only energize path. Returns false
        /// (fail-closed) when any amplifier rejects, so Stage 6 stays blocked.
        /// </summary>
        private async Task<bool> WriteGroupReadyAsync(IReadOnlyCollection<byte> domain, CancellationToken ct)
        {
            if (_trackCommClient is null || _trackVariables is null)
            {
                return false;
            }

            // Vacuously group-ready: an empty domain has no amplifiers to mark ready, so Stage 5
            // passes (stages 4/5 are vacuous for an empty domain). Movement is still fail-closed
            // separately in EstablishObservedNeutralAsync (an empty domain cannot observe neutral).
            if (domain.Count == 0)
            {
                return true;
            }

            // Diagnostic-only correlation for the Stage-5 GROUP_READY verification. The sink writes
            // to the TrackAppLog file via the existing logging infrastructure.
            string correlationId = "P1GR-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            void Log(string message) =>
                SiebwaldeApp.Core.IoC.Logger.Log($"[{correlationId}] {message}", _loggerInstance, LogLevel.Informative);

            var step = new GroupReadyStep(_trackCommClient, _trackVariables, domain.ToArray(), _loggerInstance, log: Log);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var result = await step.ExecuteAsync(null, ct).ConfigureAwait(false);
                if (result.Kind == InitStepResultKind.Completed)
                {
                    return true;
                }

                if (result.Kind == InitStepResultKind.Error)
                {
                    return false;
                }

                await Task.Delay(10, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Commands neutral to every configured amplifier and waits (bounded) until every one of
        /// them reports a fresh readback of the neutral PWM (command echo AND applied PWM). On
        /// success it records the neutral-verified fact (<see cref="_neutralVerified"/> = true) —
        /// the Stage-6-independent prerequisite (concept 1). It does NOT grant movement permission
        /// (concept 3); that happens only after Stage 6 is authorized + executed. On failure it
        /// stays fail-closed (no neutral-verified, no permission) and a fault + a
        /// <see cref="DiagnosticCode.NeutralNotEstablished"/> diagnostic are surfaced. This never
        /// throws: the caller always proceeds to <see cref="TrackRuntimeState.Running"/> and the
        /// neutral-verified/permission state carries the truth.
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
            // (In FullSimulation the default domain is the simulator's detected slaves, so an empty
            // configured group still yields a non-empty effective domain.)
            var domain = EffectiveDomain();
            if (domain.Count == 0)
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
            foreach (var slave in domain)
            {
                _trackVariables.SetDesiredAmplifierControl(slave, AmplifierSpeedMapper.NeutralPwm, false);
            }

            var observed = await ObserveNeutralAsync(ct).ConfigureAwait(false);
            var configured = AllDomainAmplifiersConfigured(domain);

            if (observed && configured)
            {
                // Concept 1 (neutral verified) is now satisfied. Movement permission (concept 3) is
                // NOT granted here: it is granted only after Stage 6 (track power) is authorized and
                // executed. The fail-closed branch below is unchanged.
                _neutralVerified = true;
                return;
            }

            var fault = new InvalidOperationException(
                "Observed neutral (command echo + applied PWM) was not established and config APPLY verified for every configured amplifier within the window; movement permission is not granted.");

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
        /// Polls until every configured amplifier reports a genuine, current, protocol SLAVEINFO
        /// readback whose PWM field equals the neutral setpoint, or until the bounded window elapses /
        /// cancellation is requested.
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

        /// <summary>
        /// True when every configured amplifier reports a genuine, current, protocol SLAVEINFO
        /// readback of the neutral PWM. The protocol-readback marker is required, so an in-memory
        /// default value (for example the 399 written by <c>SetDefaultPwmSetpointsStep</c>) is never
        /// mistaken for a protocol observation just because a stale timestamp is still fresh.
        /// </summary>
        /// <summary>
        /// True when every configured amplifier reports a genuine, current, protocol SLAVEINFO
        /// readback of the neutral PWM, both as the command echo (HR0) AND as the applied output
        /// (HR14). The applied-PWM requirement is the "applied evidence" of the Phase-1 movement
        /// gate: the command echo alone is never treated as applied/observed. The protocol-readback
        /// marker is required, so an in-memory default value (for example the 399 written by
        /// <c>SetDefaultPwmSetpointsStep</c>) is never mistaken for a protocol observation just
        /// because a stale timestamp is still fresh.
        /// </summary>
        private bool IsNeutralObserved()
        {
            if (_trackVariables is null)
            {
                return false;
            }

            var domain = EffectiveDomain();
            return TrackAmplifierNeutralObservation.AreAllObservedNeutral(
                    _trackVariables.trackAmpItems,
                    domain) &&
                TrackAmplifierNeutralObservation.AreAllAppliedNeutral(
                    _trackVariables.trackAmpItems,
                    domain);
        }

        private static ITrackTransport CreateRealTransport()
        {
            var rawUdp = new RawUdpTransport(
                CoreConfiguration.TrackControllerIpAddress,
                CoreConfiguration.TrackControllerSendingPort,
                CoreConfiguration.TrackControllerReceivingPort);
            return new RawUdpTrackTransport(rawUdp);
        }

        /// <summary>
        /// Builds the deterministic simulator transport for <see cref="TrackControlMode.FullSimulation"/>,
        /// retains it as the simulated-amplifier I/O surface, and returns it. When a layout profile
        /// is supplied it also creates the deterministic movement simulator and its occupancy adapter.
        /// </summary>
        private ITrackTransport CreateSimulatorTransport()
        {
            var profile = EffectiveFullSimulationProfile;

            // The selected/default profile is the single source of truth for the FullSimulation
            // detected slaves; the constructor-supplied config is only a fallback when no profile
            // is present.
            var config = profile is not null
                ? new TrackSimulatorConfig(
                    detectedSlaves: profile.DetectedSlaves,
                    periodicSlaveInfoInterval: TimeSpan.FromMilliseconds(500), emitGlobalStopArmedStatus: true)
                : _simulatorConfig;

            config ??= DefaultFullSimulationConfig();

            var transport = new DeterministicTrackTransport(config);
            _simulatedTrackIo = transport;

            if (profile is not null)
            {
                // The movement simulator resolves switch-conditional routes (e.g. 3>4@1:0 vs
                // 3>5@1:1) from the host's live switch controller, so the selected passing-loop
                // branch follows the actual (commanded) switch position.
                _movementSimulator = new DeterministicMovementSimulator(
                    profile,
                    switchPositions: () => _ecosHost.GetSwitchPositions());
                _movementSimulationAdapter = new MovementSimulationAdapter(
                    _movementSimulator,
                    profile,
                    () => _simulatedTrackIo);
            }

            return transport;
        }

        /// <summary>
        /// The default FullSimulation transport config: the default detected slaves {1,2,3} with a
        /// 500 ms cyclic SLAVEINFO refresh so injected occupancy/status stays fresh.
        /// </summary>
        private static TrackSimulatorConfig DefaultFullSimulationConfig()
            => new TrackSimulatorConfig(periodicSlaveInfoInterval: TimeSpan.FromMilliseconds(500), emitGlobalStopArmedStatus: true);

        /// <summary>
        /// The effective observed-neutral domain: the configured amplifier groups when non-empty,
        /// else (FullSimulation only) the simulator's detected slaves, else (Real only) the profile's
        /// physical binding, else empty. Real mode with an unconfigured domain and no physical
        /// binding therefore stays fail-closed.
        /// </summary>
        private IReadOnlyCollection<byte> EffectiveDomain()
        {
            if (_amplifierGroups.AllConfigured.Count > 0)
            {
                return _amplifierGroups.AllConfigured.Select(a => (byte)a).ToArray();
            }

            if (_requestedMode == TrackControlMode.FullSimulation && _simulatedTrackIo is not null)
            {
                return _simulatedTrackIo.DetectedSlaves;
            }

            if (_requestedMode == TrackControlMode.Real)
            {
                var physicalDomain = EffectiveFullSimulationProfile?.ToPhysicalDomain();
                if (physicalDomain is not null && physicalDomain.Count > 0)
                {
                    return physicalDomain;
                }
            }

            return Array.Empty<byte>();
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

            // 0) Full-chain safety: withdraw movement permission, then command neutral to the whole
            //    configured domain and observe it BEFORE any teardown. The runtime loop and comm
            //    client must still be running for the neutral command to be written and echoed.
            //    A stop that cannot observe neutral maps to Failed (a Stopped state means "neutral
            //    was commanded AND observed before teardown"), but the fault is thrown only after
            //    teardown has completed so no resource is left running.
            if (_requestedMode is TrackControlMode requestedMode
                && requestedMode.IsFullTrackChain()
                && _trackVariables is not null)
            {
                _movementPermission?.Withdraw();

                foreach (var slave in EffectiveDomain())
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
                // 0) Stop the movement simulation timer. Stop only halts; it emits no occupancy.
                try
                {
                    _movementSimulationAdapter?.Stop();
                }
                catch (Exception ex)
                {
                    RaiseFault("MovementSimulationAdapter.Stop", ex);
                }

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
                if (_globalStopTask is not null)
                {
                    try { await _globalStopTask.WaitAsync(budget).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (_cts?.IsCancellationRequested == true) { }
                }
                _globalStopMonitor?.Dispose();
                var hostResult = await _ecosHost.StopAsync(budget).ConfigureAwait(false);

                // 4) Dispose the comm client, never blocking with .Wait().
                if (_trackCommClient is IAsyncDisposable disposable)
                {
                    await disposable.DisposeAsync().AsTask().WaitAsync(budget).ConfigureAwait(false);
                }

                // 5) Stop the passive KIS-09 consumer (bounded).
                if (_kis09PassiveConsumer is not null)
                {
                    try
                    {
                        await _kis09PassiveConsumer.StopAsync().WaitAsync(budget).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        RaiseFault("Kis09PassiveObserverConsumer.Stop", ex);
                    }
                }

                // 6) Stop the ownership-aware carrier feeder (bounded).
                if (_kis09CarrierFeeder is not null)
                {
                    try
                    {
                        await _kis09CarrierFeeder.StopAsync().WaitAsync(budget).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        RaiseFault("LocomotiveCarrierFeeder.Stop", ex);
                    }
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
            _globalStopTask = null;
            _globalStopMonitor = null;
            _stopConfirmation?.Dispose();
            _stopConfirmation = null;
            _trackCommClient = null;
            _trackInitStateMachine = null;
            _kis09PassiveConsumer = null;
            _kis09CarrierFeeder = null;
            _carrierTimingTrace = null;
            _kis09PassiveObserver?.Dispose();
            _kis09PassiveObserver = null;
            _trackVariables?.DetachGlobalStop();
            _trackVariables = null;
            _bootloaderHelpers = null;
            _sendNextFwDataPacket = null;
            _trackApplicationLogging = null;
            _movementPermission = null;
            _simulatedTrackIo = null;
            _movementSimulator = null;
            _movementSimulationAdapter = null;

            // Release the cross-process controller-session lease exactly once (the lease's Dispose
            // is itself idempotent; nulling here also makes any later stop a no-op).
            _controllerSessionLease?.Dispose();
            _controllerSessionLease = null;

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

        public void EmergencyStop() => GlobalStop.Latch(GlobalStopReason.Operator);

        /// <summary>
        /// Release authority (R8-2 / Amendment A2 item 5): confirms physical stop inputs clear,
        /// emits <c>REARM_GLOBAL_STOP</c> (112), then a fresh valid non-neutral command — never a
        /// restore of a previous PWM. Release is refused while any amplifier is stop-unconfirmed or
        /// when physical stop inputs are not confirmed clear. By default no physical evidence is
        /// assumed (fail closed); a caller may inject a V4 evidence source via
        /// <paramref name="physicalStopInputsClear"/>.
        /// </summary>
        public async Task<AmplifierReleaseResult> ReleaseMovementAsync(
            ushort slaveNumber,
            int pwmSetpoint,
            Func<bool>? physicalStopInputsClear = null,
            CancellationToken ct = default)
        {
            if (_trackCommClient is null || _trackVariables is null)
            {
                return new AmplifierReleaseResult(AmplifierReleaseResultKind.Failed, "runtime is not running.");
            }

            var authority = new TrackAmplifierReleaseAuthority(
                _trackCommClient,
                physicalStopInputsClear ?? (() => false),
                allAmplifiersConfirmed: () => _stopConfirmation?.AreAllConfirmed ?? false);

            return await authority.ReleaseAsync(slaveNumber, pwmSetpoint, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Explicit operator action that authorizes a global-stop re-arm during startup recovery.
        /// Without this call the <see cref="GlobalStopPreconditionStep"/> keeps blocking and never
        /// emits <c>REARM_GLOBAL_STOP</c> (112). The physical-stop-clear evidence source must be
        /// supplied; when null it defaults to fail-closed (never clear), so recovery cannot proceed
        /// without positive physical evidence. Never auto-releases a latched controller.
        /// </summary>
        public void AuthorizeGlobalStopRecovery(Func<bool>? physicalStopInputsClear = null)
        {
            _physicalStopInputsClear = physicalStopInputsClear ?? (() => false);
            _operatorRecoveryAuthorized = true;
        }

        /// <inheritdoc />
        public GlobalStopStateView GlobalStopState
            => new(GlobalStop.State, GlobalStop.Reason, _operatorRecoveryAuthorized && GlobalStop.IsLatched);

        private async Task MonitorGlobalStopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await _globalStopMonitor!.TickAsync(DateTimeOffset.UtcNow, ct).ConfigureAwait(false);
                    await Task.Delay(50, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex)
            {
                GlobalStop.Latch(GlobalStopReason.Pic32Lost);
                RaiseFault("GlobalStopMonitor", ex);
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

        /// <summary>
        /// Called (synchronously) when the process-lifetime <see cref="GlobalStopController"/> latches.
        /// The desired-setpoint neutralization (PendingWrites clear + neutral re-queue) is performed by
        /// <see cref="TrackApplicationVariables.ApplyGlobalStop"/> (subscribed when the variables bind
        /// this controller). Here the init machine transitions to
        /// <see cref="InitializationStatus.RecoveryHold"/>; <see cref="TrackInitStateMachine.EnterRecoveryHold"/>
        /// itself guards the reason (comm/host-authority only) and the run progress (must have reached
        /// NormalOperation), so an operator/Koploper stop or a still-starting run is a no-op.
        ///
        /// For a comm/host-authority fault (the same reasons that put the init machine into
        /// RecoveryHold) the host-level movement authority and cached neutral evidence are also
        /// invalidated here: movement permission is withdrawn (so clearing the sticky latch later can
        /// never make a pre-fault grant effective again) and the neutral-verified flag is reset (so a
        /// future Stage 6 cannot rely on pre-fault neutral evidence). An operator/Koploper stop does
        /// neither — the latch alone already gates movement and is not a comm fault.
        /// </summary>
        private void OnGlobalStopLatched()
        {
            _trackInitStateMachine?.EnterRecoveryHold();

            // Mirror the reason gate in TrackInitStateMachine.EnterRecoveryHold: only a comm/host-
            // authority fault invalidates the host-level movement authority and cached neutral
            // evidence. An operator/Koploper latch does not withdraw movement permission (the sticky
            // latch gates movement until re-armed; the fault latch additionally invalidates authority).
            if (GlobalStop.Reason is GlobalStopReason.Pic32Lost
                or GlobalStopReason.Pic32HostLoss
                or GlobalStopReason.Pic32HostInitiated)
            {
                // Withdraw only a previously-granted permission (Granted -> Withdrawn). A permission
                // that was never granted stays NotGranted, preserving the "never established" vs
                // "established then revoked" distinction the movement gate relies on.
                if (_movementPermission is { IsGranted: true })
                {
                    _movementPermission.Withdraw();
                }

                _neutralVerified = false;
            }
        }
    }
}
