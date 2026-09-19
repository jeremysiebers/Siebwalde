using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Composes the Koploper translation layer for in-process hosting (option A):
    ///
    /// - the real hardware backend (Koploper commands -> amplifier setpoints, with look-ahead),
    /// - the ECoS backend that Koploper talks to (<see cref="SimpleEcosBackend"/>), which also
    ///   acts as the hardware feedback sink,
    /// - the occupancy path (amplifier status register -> Koploper ECoS sensor events),
    /// - the occupancy bridge attached to amplifier updates.
    ///
    /// Composition order matters: the occupancy provider and real backend are created first,
    /// then the ECoS backend (which needs the real backend), then the bridge (which needs the
    /// ECoS backend as feedback sink).
    /// </summary>
    public sealed class TrackControlIntegration
    {
        private readonly ITrackCommClient _commClient;
        private bool _attached;

        public TrackControlIntegration(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            IBlockPositionProvider blockPositionProvider,
            BlockTopology topology,
            KoploperBlockMap blockMap,
            ILocoRepository? locoRepository = null,
            IHardwareFeedbackSink? feedbackSink = null,
            Func<IReadOnlyDictionary<int, SwitchPosition>>? switchPositionProvider = null,
            Action<string>? log = null)
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            if (variables is null) throw new ArgumentNullException(nameof(variables));
            if (blockPositionProvider is null) throw new ArgumentNullException(nameof(blockPositionProvider));
            if (topology is null) throw new ArgumentNullException(nameof(topology));
            if (blockMap is null) throw new ArgumentNullException(nameof(blockMap));

            var occupancyProvider = new TrackAmplifierOccupancyProvider(
                blockMap,
                section => GetHoldingRegisters(variables, section));

            RealBackend = new TrackAmplifierHardwareBackend(
                blockPositionProvider,
                topology,
                variables,
                log,
                new LookAheadPlanner(topology),
                occupancyProvider,
                switchPositionProvider);

            // When a loco repository is supplied, host the ECoS backend in-process and use it
            // as the feedback sink. Otherwise the caller supplies its own sink (for example a
            // standalone emulator host).
            if (locoRepository is not null)
            {
                EcosBackend = new SimpleEcosBackend(RealBackend, locoRepository, blockPositionProvider);
            }

            var sink = (IHardwareFeedbackSink?)EcosBackend
                       ?? feedbackSink
                       ?? throw new ArgumentException(
                           "Provide either a loco repository (in-process ECoS backend) or a feedback sink.",
                           nameof(feedbackSink));

            Bridge = new TrackAmplifierOccupancyBridge(blockMap, occupancyProvider, sink);
        }

        /// <summary>The real hardware backend (Koploper commands -> amplifier setpoints).</summary>
        public TrackAmplifierHardwareBackend RealBackend { get; }

        /// <summary>The in-process ECoS backend, when a loco repository was supplied.</summary>
        public SimpleEcosBackend? EcosBackend { get; }

        /// <summary>The occupancy bridge (amplifier occupancy -> Koploper sensor events).</summary>
        public TrackAmplifierOccupancyBridge Bridge { get; }

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

            _ = Bridge.EvaluateAsync();
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
            _ = Bridge.EvaluateAsync();
        }

        private static ushort[]? GetHoldingRegisters(TrackApplicationVariables variables, ushort section)
        {
            var item = variables.trackAmpItems.FirstOrDefault(a => a.SlaveNumber == section);
            return item?.HoldingReg;
        }
    }
}
