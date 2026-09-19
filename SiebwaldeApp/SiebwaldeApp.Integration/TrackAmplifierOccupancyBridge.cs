using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Bridges track amplifier occupancy to Koploper ECoS sensor events.
    ///
    /// For a Koploper block N the bezetmelders are sensors 2N-1 and 2N (bit = sensorId - 1
    /// in feedback module 100). The current amplifier firmware reports a single occupied
    /// flag per section, so both bezetmelders of a block follow that flag until the
    /// firmware can distinguish entry and exit.
    /// </summary>
    public sealed class TrackAmplifierOccupancyBridge
    {
        private readonly KoploperBlockMap _blockMap;
        private readonly IOccupancyProvider _occupancy;
        private readonly IHardwareFeedbackSink _feedbackSink;
        private readonly Dictionary<int, bool> _lastState = new();

        public TrackAmplifierOccupancyBridge(
            KoploperBlockMap blockMap,
            IOccupancyProvider occupancy,
            IHardwareFeedbackSink feedbackSink)
        {
            _blockMap = blockMap ?? throw new ArgumentNullException(nameof(blockMap));
            _occupancy = occupancy ?? throw new ArgumentNullException(nameof(occupancy));
            _feedbackSink = feedbackSink ?? throw new ArgumentNullException(nameof(feedbackSink));
        }

        /// <summary>Sensor id of the first bezetmelder of a Koploper block.</summary>
        public static int EnterSensor(int koploperBlock) => (2 * koploperBlock) - 1;

        /// <summary>Sensor id of the last bezetmelder of a Koploper block.</summary>
        public static int ExitSensor(int koploperBlock) => 2 * koploperBlock;

        /// <summary>
        /// Reads the occupancy of every mapped block and forwards changes to Koploper.
        /// </summary>
        public async Task PollAsync()
        {
            foreach (var block in _blockMap.Blocks)
            {
                var occupied = _occupancy.IsBlockOccupied(block.Number);

                if (_lastState.TryGetValue(block.Number, out var previous) && previous == occupied)
                {
                    continue;
                }

                _lastState[block.Number] = occupied;

                await _feedbackSink.OnSensorChangedAsync(EnterSensor(block.Number), occupied).ConfigureAwait(false);
                await _feedbackSink.OnSensorChangedAsync(ExitSensor(block.Number), occupied).ConfigureAwait(false);
            }
        }
    }
}
