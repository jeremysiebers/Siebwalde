using System;
using System.Collections.Generic;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A synchronous <see cref="IPhysicalSectionOccupancySource"/> that projects physical section
    /// occupancy from the current track-amplifier (ModBus slave) readbacks. Each <see cref="Read"/>
    /// advances a private monotonic sequence and projects the bindings afresh via
    /// <see cref="PhysicalSectionOccupancyProjector"/>; it never caches occupancy between reads.
    /// </summary>
    public sealed class TrackAmplifierPhysicalSectionOccupancySource : IPhysicalSectionOccupancySource
    {
        private readonly string _profileId;
        private readonly IReadOnlyList<LayoutPhysicalAmplifierBinding> _physicalMapping;
        private readonly Func<ushort, TrackAmplifierItem?> _getAmplifierBySlave;
        private readonly Func<long> _sourceGenerationProvider;
        private readonly Func<DateTimeOffset> _clock;
        private readonly TimeSpan _staleAfter;

        private long _sequence;

        public TrackAmplifierPhysicalSectionOccupancySource(
            string profileId,
            IReadOnlyList<LayoutPhysicalAmplifierBinding> physicalMapping,
            Func<ushort, TrackAmplifierItem?> getAmplifierBySlave,
            Func<long>? sourceGenerationProvider = null,
            Func<DateTimeOffset>? clock = null,
            TimeSpan? staleAfter = null)
        {
            _profileId = profileId ?? throw new ArgumentNullException(nameof(profileId));
            _physicalMapping = physicalMapping ?? throw new ArgumentNullException(nameof(physicalMapping));
            _getAmplifierBySlave = getAmplifierBySlave ?? throw new ArgumentNullException(nameof(getAmplifierBySlave));
            _sourceGenerationProvider = sourceGenerationProvider ?? (() => 0);
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
            _staleAfter = staleAfter ?? TrackAmplifierDataFreshness.DefaultStaleAfter;
        }

        /// <summary>
        /// Produces one physical section occupancy observation. The sequence is advanced with
        /// checked arithmetic so a counter overflow cannot silently reuse a previous sequence.
        /// </summary>
        public PhysicalSectionOccupancyObservation Read()
        {
            _sequence = checked(_sequence + 1);

            return PhysicalSectionOccupancyProjector.Project(
                _profileId,
                _physicalMapping,
                _getAmplifierBySlave,
                _sourceGenerationProvider(),
                _sequence,
                _clock(),
                _staleAfter);
        }
    }
}
