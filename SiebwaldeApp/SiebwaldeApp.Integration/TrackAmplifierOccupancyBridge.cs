using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Bridges track amplifier occupancy to Koploper ECoS sensor events.
    ///
    /// For every Koploper block the bridge reads the occupancy (from the amplifier
    /// sections that cover the block) and forwards changes to the ECoS feedback sink
    /// for each bezetmelder of that block. A Koploper block can have one or more
    /// bezetmelders; the block-to-bezetmelder mapping comes from <see cref="KoploperBlockMap"/>.
    ///
    /// A bezetmelder name such as "1.03" means module 1, point 3; the ECoS sensor id is
    /// (module - 1) * 16 + point, and the bit in feedback module 100 is sensorId - 1.
    /// </summary>
    public sealed class TrackAmplifierOccupancyBridge
    {
        private const int SensorsPerModule = 16;

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

        /// <summary>
        /// Converts a bezetmelder name ("1.03") into an ECoS sensor id.
        /// Module 1, point 3 -> 3; module 2, point 1 -> 17.
        /// </summary>
        public static bool TryGetSensorId(string bezetmelder, out int sensorId)
        {
            sensorId = 0;

            if (string.IsNullOrWhiteSpace(bezetmelder))
            {
                return false;
            }

            var parts = bezetmelder.Split('.');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var module) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var point) ||
                module < 1 || point < 1 || point > SensorsPerModule)
            {
                return false;
            }

            sensorId = ((module - 1) * SensorsPerModule) + point;
            return true;
        }

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

                foreach (var bezetmelder in block.Bezetmelders)
                {
                    if (TryGetSensorId(bezetmelder, out var sensorId))
                    {
                        await _feedbackSink.OnSensorChangedAsync(sensorId, occupied).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
