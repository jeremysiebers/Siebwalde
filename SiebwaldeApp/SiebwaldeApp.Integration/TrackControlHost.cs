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
        private readonly string _externalInfoHost;
        private readonly int _externalInfoPort;
        private readonly int _ecosListenPort;
        private readonly Func<IReadOnlyDictionary<int, SwitchPosition>>? _switchPositionProvider;
        private readonly Action<string>? _log;

        private KoploperExternalInfoClient? _externalInfo;
        private JsonLocoRepository? _locoRepository;
        private SimpleEcosBackend? _ecosBackend;
        private TrackSimulatorBackend? _simulatorBackend;
        private TrackControlIntegration? _integration;
        private EcosEmulatorServer? _server;
        private TrackControlMode? _mode;

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
            int ecosListenPort = DefaultEcosListenPort,
            string koploperExternalInfoHost = "127.0.0.1",
            int koploperExternalInfoPort = DefaultKoploperExternalInfoPort,
            Func<IReadOnlyDictionary<int, SwitchPosition>>? switchPositionProvider = null,
            Action<string>? log = null)
        {
            if (string.IsNullOrWhiteSpace(locoRepositoryPath))
                throw new ArgumentException("A locomotive repository path is required.", nameof(locoRepositoryPath));

            _locoRepositoryPath = locoRepositoryPath;
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _blockMap = blockMap ?? throw new ArgumentNullException(nameof(blockMap));
            _ecosListenPort = ecosListenPort;
            _externalInfoHost = koploperExternalInfoHost;
            _externalInfoPort = koploperExternalInfoPort;
            _switchPositionProvider = switchPositionProvider;
            _log = log;
        }

        /// <summary>
        /// Builds a host from the application configuration. The locomotive repository is
        /// derived from the log directory so no additional setting is needed; it is left
        /// empty on purpose, because Koploper synchronises the locomotives itself.
        /// </summary>
        public static TrackControlHost FromConfiguration(
            Func<IReadOnlyDictionary<int, SwitchPosition>>? switchPositionProvider = null,
            Action<string>? log = null)
            => new(
                Path.Combine(CoreConfiguration.LogDirectory, "locos.json"),
                CoreConfiguration.BuildBlockTopology(),
                CoreConfiguration.BuildKoploperBlockMap(),
                switchPositionProvider: switchPositionProvider,
                log: log);

        /// <inheritdoc />
        public bool IsRunning => _server is not null;

        /// <inheritdoc />
        public TrackControlMode? Mode => _mode;

        /// <inheritdoc />
        public async Task StartAsync(
            TrackControlMode mode,
            ITrackCommClient? commClient,
            TrackApplicationVariables? variables,
            CancellationToken cancellationToken = default)
        {
            if (IsRunning)
            {
                Log($"ECoS host is already running in {_mode} mode; ignoring the {mode} start request.");
                return;
            }

            if (mode == TrackControlMode.Real && (commClient is null || variables is null))
            {
                throw new ArgumentException(
                    "Real mode requires the track communication client and the shared track variables.",
                    nameof(commClient));
            }

            _externalInfo = new KoploperExternalInfoClient(_externalInfoHost, _externalInfoPort);

            _locoRepository = new JsonLocoRepository(_locoRepositoryPath);
            await _locoRepository.LoadAsync(cancellationToken).ConfigureAwait(false);

            if (mode == TrackControlMode.Real)
            {
                _integration = new TrackControlIntegration(
                    commClient!,
                    variables!,
                    _externalInfo,
                    _topology,
                    _blockMap,
                    _locoRepository,
                    feedbackSink: null,
                    _switchPositionProvider,
                    _log);

                _ecosBackend = _integration.EcosBackend
                    ?? throw new InvalidOperationException(
                        "The integration did not create an in-process ECoS backend.");
            }
            else
            {
                _simulatorBackend = new TrackSimulatorBackend(_externalInfo);
                _ecosBackend = new SimpleEcosBackend(_simulatorBackend, _locoRepository, _externalInfo);

                // The simulator needs the ECoS backend as feedback sink, so hook it up
                // before the external-info client starts producing block positions.
                _simulatorBackend.AttachFeedbackSink(_ecosBackend);
            }

            // Order matters: the backend and its feedback sink must exist before the
            // external-info client starts, and the server needs a ready backend.
            _externalInfo.Start();

            _server = new EcosEmulatorServer(_ecosListenPort, new SimpleEcosCommandParser(), _ecosBackend);
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
            TryRun(() => _externalInfo?.Stop(), "stop the Koploper external-info client");

            _server = null;
            _ecosBackend = null;
            _simulatorBackend = null;
            _integration = null;
            _locoRepository = null;
            _externalInfo = null;
            _mode = null;

            if (stoppedMode is not null)
            {
                Log($"ECoS host stopped (was running in {stoppedMode} mode).");
            }
        }

        /// <inheritdoc />
        public void Dispose() => Stop();

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
    }
}
