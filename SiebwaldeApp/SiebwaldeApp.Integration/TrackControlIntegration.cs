using System;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Wires the Koploper translation layer together:
    ///
    /// - block topology and Koploper block mapping from configuration,
    /// - the real hardware backend (Koploper commands -> amplifier setpoints, with look-ahead),
    /// - the occupancy path (amplifier status register -> Koploper ECoS sensor events).
    ///
    /// The occupancy path is event-driven: it listens to
    /// <see cref="ITrackCommClient.AmplifierDataReceived"/> and evaluates the occupancy
    /// bridge on each amplifier update, plus once on attach to establish the initial state.
    /// </summary>
    public sealed class TrackControlIntegration
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackAmplifierOccupancyBridge _bridge;
        private bool _attached;

        public TrackControlIntegration(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            IHardwareFeedbackSink feedbackSink,
            IBlockPositionProvider blockPositionProvider,
            BlockTopology topology,
            KoploperBlockMap blockMap,
            Func<System.Collections.Generic.IReadOnlyDictionary<int, SwitchPosition>>? switchPositionProvider = null,
            Action<string>? log = null)
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            if (variables is null) throw new ArgumentNullException(nameof(variables));
            if (feedbackSink is null) throw new ArgumentNullException(nameof(feedbackSink));
            if (blockPositionProvider is null) throw new ArgumentNullException(nameof(blockPositionProvider));
            if (topology is null) throw new ArgumentNullException(nameof(topology));
            if (blockMap is null) throw new ArgumentNullException(nameof(blockMap));

            var occupancyProvider = new TrackAmplifierOccupancyProvider(
                blockMap,
                section => GetHoldingRegisters(variables, section));

            _bridge = new TrackAmplifierOccupancyBridge(blockMap, occupancyProvider, feedbackSink);

            RealBackend = new TrackAmplifierHardwareBackend(
                blockPositionProvider,
                topology,
                variables,
                log,
                new LookAheadPlanner(topology),
                occupancyProvider,
                switchPositionProvider);
        }

        /// <summary>The real hardware backend (Koploper commands -> amplifier setpoints).</summary>
        public TrackAmplifierHardwareBackend RealBackend { get; }

        /// <summary>The occupancy bridge (amplifier occupancy -> Koploper sensor events).</summary>
        public TrackAmplifierOccupancyBridge Bridge => _bridge;

        /// <summary>
        /// Subscribes to amplifier updates and performs one initial occupancy evaluation.
        /// </summary>
        public void Attach()
        {
            if (_attached)
            {
                return;
            }

            _commClient.AmplifierDataReceived += OnAmplifierDataReceived;
            _attached = true;

            _ = _bridge.EvaluateAsync();
        }

        /// <summary>Unsubscribes from amplifier updates.</summary>
        public void Detach()
        {
            if (!_attached)
            {
                return;
            }

            _commClient.AmplifierDataReceived -= OnAmplifierDataReceived;
            _attached = false;
        }

        private void OnAmplifierDataReceived(object? sender, AmplifierDataEventArgs e)
        {
            // Event-driven: no polling. The bridge ignores evaluations without a change.
            _ = _bridge.EvaluateAsync();
        }

        private static ushort[]? GetHoldingRegisters(TrackApplicationVariables variables, ushort section)
        {
            var item = variables.trackAmpItems.FirstOrDefault(a => a.SlaveNumber == section);
            return item?.HoldingReg;
        }
    }
}
