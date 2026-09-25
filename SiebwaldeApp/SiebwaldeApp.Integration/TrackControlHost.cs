using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Owns the ECoS host for in-process hosting (option A).
    ///
    /// Responsibilities:
    /// - compose the hardware backend for the selected <see cref="TrackControlMode"/>
    ///   (real track amplifiers or the software simulator),
    /// - host the ECoS backend Koploper talks to and the ECoS server on port 15471,
    /// - own the Koploper external-info connection (port 5700, C# connects out to Koploper),
    /// - start, stop and release everything it created.
    ///
    /// Port roles are deliberately explicit: 15471 is the listener Koploper connects to,
    /// while 5700 is Koploper's external-information server that this host connects to.
    /// The two directions are not interchangeable.
    /// </summary>
    public sealed class TrackControlHost : IEcosHostService, IDisposable
    {
        /// <summary>Port the ECoS emulator listens on for Koploper.</summary>
        public const int DefaultEcosListenPort = 15471;

        /// <summary>Port of the Koploper external-information server.</summary>
        public const int DefaultKoploperExternalInfoPort = 5700;

        private readonly string _locoRepositoryPath;
        private readonly BlockTopology _topology;
        private readonly KoploperBlockMap _blockMap;
        private readonly SwitchMapping _switchMapping;
        private readonly TrackAmplifierGroups _trackAmplifierGroups;
        private readonly string _externalInfoHost;
        private readonly int _externalInfoPort;
        private readonly int _ecosListenPort;
        private readonly Func<IReadOnlyDictionary<int, SwitchPosition>>? _switchPositionProvider;
        private readonly Action<string>? _log;
        private readonly IControlTrace? _controlTrace;

        private static readonly IReadOnlyDictionary<int, SwitchPosition> NoSwitches =
            new Dictionary<int, SwitchPosition>();

        private KoploperExternalInfoClient? _externalInfo;
        private JsonLocoRepository? _locoRepository;
        private SimpleEcosBackend? _ecosBackend;
        private TrackSimulatorBackend? _simulatorBackend;
        private TrackControlIntegration? _integration;
        private EcosEmulatorServer? _server;
        private EcosHardwareStopSink? _stopSink;
        private TrackControlMode? _mode;

        /// <inheritdoc />
        public event EventHandler<RuntimeFaultEventArgs>? Faulted;

        /// <summary>
        /// Creates the host. The topology and block map come from configuration; the
        /// runtime pieces (track communication client and shared variables) are supplied
        /// to <see cref="StartAsync"/> because they only exist once the track application
        /// has started.
        /// </summary>
        public TrackControlHost(
            string locoRepositoryPath,
            BlockTopology topology,
            KoploperBlockMap blockMap,
            SwitchMapping? switchMapping = null,
            int ecosListenPort = DefaultEcosListenPort,
            string koploperExternalInfoHost = "127.0.0.1",
            int koploperExternalInfoPort = DefaultKoploperExternalInfoPort,
            Func<IReadOnlyDictionary<int, SwitchPosition>>? switchPositionProvider = null,
            Action<string>? log = null,
            TrackAmplifierGroups? trackAmplifierGroups = null,
            IControlTrace? controlTrace = null)
        {
            if (string.IsNullOrWhiteSpace(locoRepositoryPath))
                throw new ArgumentException("A locomotive repository path is required.", nameof(locoRepositoryPath));

            _locoRepositoryPath = locoRepositoryPath;
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _blockMap = blockMap ?? throw new ArgumentNullException(nameof(blockMap));
            _switchMapping = switchMapping ?? SwitchMapping.Parse(null);
            _trackAmplifierGroups = trackAmplifierGroups ?? TrackAmplifierGroups.Empty;
            _ecosListenPort = ecosListenPort;
            _externalInfoHost = koploperExternalInfoHost;
            _externalInfoPort = koploperExternalInfoPort;
            _switchPositionProvider = switchPositionProvider;
            _log = log;
            _controlTrace = controlTrace;
        }

        /// <summary>
        /// Builds a host from the application configuration. The locomotive repository is
        /// derived from the log directory so no additional setting is needed; it is left
        /// empty on purpose, because Koploper synchronises the locomotives itself.
        /// </summary>
        public static TrackControlHost FromConfiguration(
            Func<IReadOnlyDictionary<int, SwitchPosition>>? switchPositionProvider = null,
            Action<string>? log = null,
            IControlTrace? controlTrace = null,
            TrackAmplifierGroups? trackAmplifierGroups = null)
            => new(
                Path.Combine(CoreConfiguration.LogDirectory, "locos.json"),
                CoreConfiguration.BuildBlockTopology(),
                CoreConfiguration.BuildKoploperBlockMap(),
                CoreConfiguration.BuildSwitchMap(),
                switchPositionProvider: switchPositionProvider,
                log: log,
                trackAmplifierGroups: trackAmplifierGroups ?? CoreConfiguration.BuildTrackAmplifierGroups(),
                controlTrace: controlTrace);

        /// <inheritdoc />
        public bool IsRunning => _server is not null;

        /// <inheritdoc />
        public TrackControlMode? Mode => _mode;

        /// <summary>
        /// The switch translation for the running host, or null when it is not running. The
        /// same controller is used in real and simulator mode.
        /// </summary>
        public SwitchController? Switches { get; private set; }

        /// <summary>
        /// Diagnostics surface for the running host (bounded history plus the latched unsafe
        /// state), or null when the host is not running. The UI reads this instead of log text.
        /// </summary>
        public ControlDiagnostics? Diagnostics { get; private set; }

        /// <summary>Safety latch state, or null when the host is not running.</summary>
        public ControlSafetyGuard? Safety { get; private set; }

        /// <summary>What the running mode can observe, or null when the host is not running.</summary>
        public IObservability? Observability { get; private set; }

        /// <summary>Divergence checks for routes, or null when the host is not running.</summary>
        public DivergenceChecker? Divergence { get; private set; }

        /// <inheritdoc />
        public bool IsUnsafe => Diagnostics?.IsUnsafe ?? false;

        /// <inheritdoc />
        public async Task<EcosHostStartResult> StartAsync(
            TrackControlMode mode,
            ITrackCommClient? commClient,
            TrackApplicationVariables? variables,
            CancellationToken cancellationToken = default)
        {
            // Already in the requested mode: idempotent, nothing to do.
            if (IsRunning && _mode == mode)
            {
                Log($"ECoS host is already running in {mode} mode.");
                return EcosHostStartResult.AlreadyActive;
            }

            // Real mode outranks the simulator: a successfully started real track application
            // must not stay hidden behind a simulator that was started earlier. The reverse
            // request is refused, because replacing a live real host would take the real
            // layout away from Koploper without the operator asking for it.
            if (IsRunning && _mode == TrackControlMode.Real && mode == TrackControlMode.Simulator)
            {
                Log("Refusing to replace the running real ECoS host with the simulator.");
                return EcosHostStartResult.Rejected;
            }

            // Validate before touching a running host, so a bad request cannot tear down a
            // working host.
            if (mode == TrackControlMode.Real && (commClient is null || variables is null))
            {
                throw new ArgumentException(
                    "Real mode requires the track communication client and the shared track variables.",
                    nameof(commClient));
            }

            var transitioning = IsRunning;
            if (transitioning)
            {
                Log($"Switching the ECoS host from {_mode} to {mode}.");
                Stop();
            }

            try
            {
                await ComposeAndStartAsync(mode, commClient, variables, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Never leave a half-started host behind: release the port and the
                // external-info client before surfacing the failure.
                Stop();
                throw;
            }

            return transitioning ? EcosHostStartResult.Transitioned : EcosHostStartResult.Started;
        }

        /// <summary>
        /// Composes the backend for the requested mode and brings up the ECoS server.
        /// Assumes no host is running.
        /// </summary>
        private async Task ComposeAndStartAsync(
            TrackControlMode mode,
            ITrackCommClient? commClient,
            TrackApplicationVariables? variables,
            CancellationToken cancellationToken)
        {
            _externalInfo = new KoploperExternalInfoClient(_externalInfoHost, _externalInfoPort);

            _locoRepository = new JsonLocoRepository(_locoRepositoryPath);
            await _locoRepository.LoadAsync(cancellationToken).ConfigureAwait(false);

            // Diagnostics and the safety reaction are shared by both modes.
            Diagnostics = new ControlDiagnostics();
            _stopSink = new EcosHardwareStopSink(_log, _controlTrace);
            Safety = new ControlSafetyGuard(_stopSink, Diagnostics, _log, _controlTrace);

            if (mode == TrackControlMode.Real)
            {
                // Occupancy comes from the existing amplifier data path (the master's SLAVEINFO
                // frame -> holding registers), so it becomes observable as soon as valid
                // amplifier data has been received. Switch feedback does not exist on real
                // hardware yet, so it stays unavailable and is never a confirmation.
                Observability = new AmplifierOccupancyObservability(variables!);

                // The physical switch side is not wired to real hardware yet, so the real
                // output deliberately drives nothing and says so. The translation path is
                // still the shared one, so only this sink has to change later.
                Switches = new SwitchController(
                    _switchMapping,
                    new DelegateSwitchOutput(
                        (address, position) =>
                        {
                            Log(
                                $"Physical switch output {address} is not wired to real hardware yet; " +
                                $"{position} was NOT driven.");
                            return false;
                        },
                        isAvailable: false),
                    _log,
                    Diagnostics,
                    Safety,
                    new UnobservableSwitchObserver(),
                    Observability);

                _integration = new TrackControlIntegration(
                    commClient!,
                    variables!,
                    _externalInfo,
                    _topology,
                    _blockMap,
                    _locoRepository,
                    feedbackSink: null,
                    CurrentSwitchPositions,
                    _log,
                    Switches,
                    Safety,
                    Diagnostics,
                    _trackAmplifierGroups,
                    _controlTrace);

                _ecosBackend = _integration.EcosBackend
                    ?? throw new InvalidOperationException(
                        "The integration did not create an in-process ECoS backend.");

                Divergence = new DivergenceChecker(
                    _topology,
                    Switches,
                    _integration.OccupancyProvider,
                    Observability,
                    Diagnostics,
                    Safety,
                    _log);

                _integration.RealBackend.Divergence = Divergence;
                _stopSink.Hardware = _integration.RealBackend;

                // A loco-scoped safety stop must reach the retained physical targets and be able
                // to escalate to amplifier-centric neutralization, not only the current mapping.
                _stopSink.Neutralizer = _integration.RealBackend;
                _stopSink.CommandTracker = _integration.CommandTracker;

                BindSafetyRevalidation();
            }
            else
            {
                // The simulator can observe everything it drives.
                Observability = new ModeObservability
                {
                    SwitchFeedbackAvailable = true,
                    // Occupancy in the simulator is delivered as ECoS sensor events, not through
                    // an IOccupancyProvider, so it is not consulted here yet.
                    OccupancyAvailable = false
                };

                _simulatorBackend = new TrackSimulatorBackend(_externalInfo);
                _simulatorBackend.Faulted += OnPartFaulted;

                // Same translation path as real mode; only the physical sink differs.
                Switches = new SwitchController(
                    _switchMapping,
                    new DelegateSwitchOutput((address, position) =>
                    {
                        _simulatorBackend.SetSwitch(
                            address,
                            position == SwitchPosition.Straight ? 0 : 1,
                            true);
                        return true;
                    }),
                    _log,
                    Diagnostics,
                    Safety,
                    new SimulatorSwitchObserver(_simulatorBackend),
                    Observability);

                // Movement commands pass through the safety interlock so a latched fault cannot
                // be bypassed by a later command from Koploper.
                IHardwareBackend hardware = new SwitchTranslatingHardwareBackend(_simulatorBackend, Switches, _log);
                hardware = new ControlSafetyInterlockBackend(hardware, Safety, Diagnostics, _log);

                _ecosBackend = new SimpleEcosBackend(hardware, _locoRepository, _externalInfo, _controlTrace);

                // The simulator needs the ECoS backend as feedback sink, so hook it up
                // before the external-info client starts producing block positions.
                _simulatorBackend.AttachFeedbackSink(_ecosBackend);
                _stopSink.Hardware = _simulatorBackend;

                Divergence = new DivergenceChecker(
                    _topology,
                    Switches,
                    occupancy: null,
                    Observability,
                    Diagnostics,
                    Safety,
                    _log);

                BindSafetyRevalidation();
            }

            // Give every mapped switch a known state where one is configured. Entries marked
            // 'keep' are intentionally not driven.
            Switches.Initialize();

            // Order matters: the backend and its feedback sink must exist before the
            // external-info client starts, and the server needs a ready backend.
            _externalInfo.Start();

            _server = new EcosEmulatorServer(_ecosListenPort, new SimpleEcosCommandParser(), _ecosBackend);
            _server.Faulted += OnPartFaulted;
            _server.Start();

            _integration?.Attach();

            _mode = mode;
            Log($"ECoS host started in {mode} mode; listening on 127.0.0.1:{_ecosListenPort}.");
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (!IsRunning && _integration is null && _externalInfo is null)
            {
                return;
            }

            var stoppedMode = _mode;

            // Detach first so no occupancy update can reach a backend that is being torn down.
            TryRun(() => _integration?.Detach(), "detach the occupancy bridge");
            TryRun(() => _server?.Stop(), "stop the ECoS server");
            TryRun(() => _simulatorBackend?.Stop(), "stop the simulator backend");
            TryRun(() => _externalInfo?.Stop(), "stop the Koploper external-info client");

            _server = null;
            _ecosBackend = null;
            _simulatorBackend = null;
            _integration = null;
            _locoRepository = null;
            _externalInfo = null;
            _mode = null;
            Switches = null;
            Diagnostics = null;
            Safety = null;
            Observability = null;
            Divergence = null;
            _stopSink = null;

            if (stoppedMode is not null)
            {
                Log($"ECoS host stopped (was running in {stoppedMode} mode).");
            }
        }

        /// <summary>
        /// Gracefully stops the ECoS host: detaches the occupancy bridge, then awaits the graceful
        /// stop of the server, the simulator backend and the external-info client with a bounded
        /// timeout, and releases every field (same set as <see cref="Stop"/>). Reports whether the
        /// clean-stop guarantee was actually established.
        /// </summary>
        public async Task<EcosHostStopResult> StopAsync(CancellationToken ct = default)
        {
            if (!IsRunning && _integration is null && _externalInfo is null)
            {
                return EcosHostStopResult.AlreadyStopped;
            }

            var stoppedMode = _mode;

            // Detach first so no occupancy update can reach a backend that is being torn down.
            try
            {
                _integration?.Detach();
            }
            catch (Exception ex)
            {
                RaiseFault("TrackControlHost.Detach", ex);
            }

            var clean = true;
            var timedOut = false;

            try
            {
                if (_server is not null && !await _server.StopAsync(ct).ConfigureAwait(false))
                {
                    timedOut = true;
                }
            }
            catch (OperationCanceledException)
            {
                timedOut = true;
            }
            catch (Exception ex)
            {
                clean = false;
                RaiseFault("EcosEmulatorServer.StopAsync", ex);
            }

            try
            {
                if (_simulatorBackend is not null && !await _simulatorBackend.StopAsync(ct).ConfigureAwait(false))
                {
                    timedOut = true;
                }
            }
            catch (OperationCanceledException)
            {
                timedOut = true;
            }
            catch (Exception ex)
            {
                clean = false;
                RaiseFault("TrackSimulatorBackend.StopAsync", ex);
            }

            try
            {
                if (_externalInfo is not null && !await _externalInfo.StopAsync(ct).ConfigureAwait(false))
                {
                    timedOut = true;
                }
            }
            catch (OperationCanceledException)
            {
                timedOut = true;
            }
            catch (Exception ex)
            {
                clean = false;
                RaiseFault("KoploperExternalInfoClient.StopAsync", ex);
            }

            // Release every field exactly as the synchronous Stop does.
            _server = null;
            _ecosBackend = null;
            _simulatorBackend = null;
            _integration = null;
            _locoRepository = null;
            _externalInfo = null;
            _mode = null;
            Switches = null;
            Diagnostics = null;
            Safety = null;
            Observability = null;
            Divergence = null;
            _stopSink = null;

            if (stoppedMode is not null)
            {
                Log($"ECoS host stopped (was running in {stoppedMode} mode).");
            }

            if (!clean)
            {
                return EcosHostStopResult.Faulted;
            }

            if (timedOut)
            {
                return EcosHostStopResult.Timeout;
            }

            return EcosHostStopResult.Stopped;
        }

        /// <inheritdoc />
        public void Dispose() => Stop();

        /// <summary>
        /// Explicit recovery: clears the latched safety state so the control path can continue.
        /// A latched fault never clears itself, not even when a later command arrives.
        /// </summary>
        public bool ResetSafety() => Safety?.Reset() ?? false;

        /// <summary>
        /// Binds the divergence checker as the revalidation rule for recovery, so an explicit
        /// reset is refused while the underlying condition is still present.
        /// </summary>
        private void BindSafetyRevalidation()
        {
            if (Safety is not null && Divergence is not null)
            {
                Safety.RevalidationCheck = Divergence.IsResolved;
            }
        }

        private void TryRun(Action action, string description)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log($"Failed to {description}: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            if (_log is not null)
            {
                _log(message);
            }
        }

        private void RaiseFault(string source, Exception exception)
            => Faulted?.Invoke(this, new RuntimeFaultEventArgs(source, exception));

        /// <summary>Re-raises a part's fault so the coordinator observes a single host-level fault stream.</summary>
        private void OnPartFaulted(object? sender, RuntimeFaultEventArgs e)
            => Faulted?.Invoke(this, e);

        /// <summary>
        /// Switch positions for routing and look-ahead. The logical (ECoS) positions come from
        /// the shared switch controller; an explicit provider, when supplied, still wins so a
        /// caller can override the source.
        /// </summary>
        private IReadOnlyDictionary<int, SwitchPosition> CurrentSwitchPositions()
        {
            if (_switchPositionProvider is not null)
            {
                return _switchPositionProvider.Invoke();
            }

            return Switches?.GetLogicalPositions() ?? NoSwitches;
        }
    }
}
