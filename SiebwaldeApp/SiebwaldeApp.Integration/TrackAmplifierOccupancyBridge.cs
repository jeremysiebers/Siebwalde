using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Bridges track amplifier occupancy to Koploper ECoS sensor events.
    ///
    /// Each Koploper bezetmelder reflects the occupancy of ITS OWN section (KIS10-06): the bridge
    /// resolves every bezetmelder to its section's amplifier (via the per-section feedback map) and
    /// forwards that section's fresh Clear/Occupied state to the ECoS feedback sink. The block-level
    /// aggregate is NOT substituted for the per-section data; the derived block status remains
    /// available through <see cref="IOccupancyProvider"/> for look-ahead/safety consumers.
    ///
    /// A bezetmelder name such as "1.03" means module 1, point 3; the ECoS sensor id is
    /// (module - 1) * 16 + point, and the bit in feedback module 100 is sensorId - 1.
    /// </summary>
    public sealed class TrackAmplifierOccupancyBridge
    {
        private const int SensorsPerModule = 16;

        private readonly IReadOnlyList<KoploperFeedbackBlock> _feedbackMap;
        private readonly IAmplifierOccupancyProvider _occupancy;
        private readonly IHardwareFeedbackSink _feedbackSink;
        private readonly Dictionary<int, bool> _lastState = new();

        public TrackAmplifierOccupancyBridge(
            IReadOnlyList<KoploperFeedbackBlock> feedbackMap,
            IAmplifierOccupancyProvider occupancy,
            IHardwareFeedbackSink feedbackSink)
        {
            _feedbackMap = feedbackMap ?? throw new ArgumentNullException(nameof(feedbackMap));
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
        /// Evaluates the occupancy of every mapped bezetmelder and forwards changes to Koploper.
        ///
        /// This is event-driven, not polled: call it whenever amplifier data changes
        /// (for example from TrackCommClientAsync.AmplifierDataReceived). Because the
        /// bridge keeps the last known state per bezetmelder, repeated calls without a change are
        /// cheap and produce no ECoS traffic. Call it once after startup to establish the
        /// initial state.
        /// </summary>
        public async Task EvaluateAsync()
        {
            foreach (var block in _feedbackMap)
            {
                foreach (var section in block.Sections)
                {
                    var occupancy = _occupancy.GetAmplifierOccupancy(section.Amplifier);

                    // Never report "free" for a section whose occupancy cannot be confirmed: unknown
                    // is not clear, and a false clear would tell Koploper the section is safe. The
                    // bezetmelder is left out of the change tracking so a later known value still
                    // produces an event.
                    if (occupancy == PhysicalOccupancy.Unknown)
                    {
                        continue;
                    }

                    var occupied = occupancy == PhysicalOccupancy.Occupied;

                    foreach (var bezetmelder in section.Bezetmelders)
                    {
                        if (!TryGetSensorId(bezetmelder, out var sensorId))
                        {
                            continue;
                        }

                        if (_lastState.TryGetValue(sensorId, out var previous) && previous == occupied)
                        {
                            continue;
                        }

                        _lastState[sensorId] = occupied;

                        // Make the change-driven nature visible: this bridge only emits on change, so
                        // a bezetmelder is only logged here when its state actually flipped (or was
                        // first seen).
                        SiebwaldeApp.Core.IoC.Logger.Log(
                            $"block {block.BlockNumber} section {section.SectionId} amp {section.Amplifier} " +
                            $"bezetmelder {bezetmelder} -> {(occupied ? "occupied" : "free")}",
                            "SiebwaldeApp.Integration.OccupancyBridge");

                        await _feedbackSink.OnSensorChangedAsync(sensorId, occupied).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
