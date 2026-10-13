using System;
using System.Collections.Generic;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Stage 1 of the physical-source release model: a bounded, deterministic ASYMMETRIC occupancy
    /// deassert filter. The rising edge (Clear → Occupied) is committed IMMEDIATELY so a newly-entered
    /// section is detected without delay; only the falling edge (Occupied → Clear) is debounced, so a
    /// brief bad-contact dropout does not cut a held section. The filtered "Clear" is produced only
    /// after the occupancy has been continuously Clear for <see cref="DefaultClearDebounceMs"/>. An
    /// <see cref="PhysicalOccupancy.Unknown"/> observation passes through and clears any pending
    /// deassert. The clock is injectable so tests are deterministic.
    /// </summary>
    public sealed class PhysicalOccupancyDeassertFilter
    {
        /// <summary>
        /// Provisional default occupancy deassert window (Occupied → Clear must persist this long
        /// before "filtered Clear" is reported). It is deliberately SHORT (100 ms) — just enough to
        /// suppress a bad-contact dropout, never a movement/control budget. Rising edges are never
        /// delayed. Later user-configurable.
        /// </summary>
        public static readonly TimeSpan DefaultClearDebounceMs = TimeSpan.FromMilliseconds(100);

        private readonly TimeSpan _clearDebounceMs;
        private readonly Func<DateTimeOffset> _clock;
        private readonly Dictionary<int, PhysicalOccupancy> _stable = new();
        private readonly Dictionary<int, DateTimeOffset> _clearSince = new();

        public PhysicalOccupancyDeassertFilter(TimeSpan? clearDebounceMs = null, Func<DateTimeOffset>? clock = null)
        {
            _clearDebounceMs = clearDebounceMs ?? DefaultClearDebounceMs;
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        public PhysicalSectionOccupancyObservation Filter(PhysicalSectionOccupancyObservation observation)
        {
            DateTimeOffset now = _clock();
            var sections = new List<PhysicalSectionState>(observation.Sections.Count);

            foreach (PhysicalSectionState state in observation.Sections)
            {
                PhysicalOccupancy occupancy = Deassert(state, now);
                sections.Add(new PhysicalSectionState(
                    state.LogicalSectionId,
                    occupancy,
                    state.ObservedAtUtc,
                    state.Age,
                    state.IsFresh,
                    state.SourceHealth));
            }

            return new PhysicalSectionOccupancyObservation(
                observation.ProfileId,
                observation.SourceGeneration,
                observation.Sequence,
                observation.CapturedAtUtc,
                observation.SourceValid,
                observation.SourceHealth,
                sections);
        }

        private PhysicalOccupancy Deassert(PhysicalSectionState state, DateTimeOffset now)
        {
            int section = state.LogicalSectionId;
            PhysicalOccupancy occ = state.Occupancy;

            // Unknown (stale) is not a Clear<->Occupied transition; pass through and clear any pending deassert.
            if (occ == PhysicalOccupancy.Unknown)
            {
                _clearSince.Remove(section);
                return occ;
            }

            if (!_stable.TryGetValue(section, out PhysicalOccupancy stable))
            {
                // First Clear/Occupied observation: stable immediately.
                _stable[section] = occ;
                _clearSince.Remove(section);
                return occ;
            }

            // Rising edge (make): Clear -> Occupied commits immediately, no debounce.
            if (occ == PhysicalOccupancy.Occupied)
            {
                _stable[section] = PhysicalOccupancy.Occupied;
                _clearSince.Remove(section);
                return PhysicalOccupancy.Occupied;
            }

            // occ == Clear.
            if (stable == PhysicalOccupancy.Clear)
            {
                // Clear -> Clear: stable; no pending deassert.
                _clearSince.Remove(section);
                return PhysicalOccupancy.Clear;
            }

            // Falling edge (break): stable == Occupied, occ == Clear -> debounce the deassert.
            if (_clearSince.TryGetValue(section, out DateTimeOffset since))
            {
                if (now - since >= _clearDebounceMs)
                {
                    // Clear persisted >= window: commit the deassert.
                    _stable[section] = PhysicalOccupancy.Clear;
                    _clearSince.Remove(section);
                    return PhysicalOccupancy.Clear;
                }

                // Still within the window: hold Occupied (dropout not yet trusted).
                return PhysicalOccupancy.Occupied;
            }

            // First Clear after Occupied: start the deassert timer.
            _clearSince[section] = now;
            return PhysicalOccupancy.Occupied;
        }
    }
}
