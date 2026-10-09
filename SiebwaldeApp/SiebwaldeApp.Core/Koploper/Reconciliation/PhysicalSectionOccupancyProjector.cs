using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Projects a set of logical-section -&gt; physical-amplifier bindings onto a
    /// <see cref="PhysicalSectionOccupancyObservation"/> using the current track-amplifier
    /// readbacks. Pure and stateless: it rebuilds the projection from scratch each call and never
    /// retains occupancy between calls. A physical occupancy reading carries no locomotive
    /// identity, so the result is always a bare tri-state per section.
    /// </summary>
    public static class PhysicalSectionOccupancyProjector
    {
        /// <summary>
        /// Projects the physical occupancy for each binding in <paramref name="physicalMapping"/>.
        /// <paramref name="getAmplifierBySlave"/> resolves a physical amplifier (ModBus slave)
        /// address to its current <see cref="TrackAmplifierItem"/>, or null when unknown.
        /// <paramref name="staleAfter"/> bounds how old an amplifier frame may be before the section
        /// is treated as unknown. Sections are returned sorted by logical section id.
        /// </summary>
        public static PhysicalSectionOccupancyObservation Project(
            string profileId,
            IReadOnlyList<LayoutPhysicalAmplifierBinding> physicalMapping,
            Func<ushort, TrackAmplifierItem?> getAmplifierBySlave,
            long sourceGeneration,
            long sequence,
            DateTimeOffset capturedAtUtc,
            TimeSpan staleAfter)
        {
            ArgumentNullException.ThrowIfNull(profileId);
            ArgumentNullException.ThrowIfNull(physicalMapping);
            ArgumentNullException.ThrowIfNull(getAmplifierBySlave);

            if (!IsValidMapping(physicalMapping))
            {
                return new PhysicalSectionOccupancyObservation(
                    ProfileId: profileId,
                    SourceGeneration: sourceGeneration,
                    Sequence: sequence,
                    CapturedAtUtc: capturedAtUtc,
                    SourceValid: false,
                    SourceHealth: PhysicalSourceHealth.Unavailable,
                    Sections: Array.Empty<PhysicalSectionState>());
            }

            var sections = new List<PhysicalSectionState>(physicalMapping.Count);
            foreach (LayoutPhysicalAmplifierBinding binding in physicalMapping)
            {
                TrackAmplifierItem? item = getAmplifierBySlave((ushort)binding.PhysicalAmplifier);

                DateTimeOffset observedAtUtc = item?.LastDataReceivedUtc ?? capturedAtUtc;
                TimeSpan age = capturedAtUtc - observedAtUtc;

                PhysicalOccupancy occupancy;
                bool isFresh;
                if (item is not null && TrackAmplifierDataFreshness.IsCurrentData(item, capturedAtUtc, staleAfter))
                {
                    occupancy = TrackAmplifierRegisters.IsOccupied(item.HoldingReg)
                        ? PhysicalOccupancy.Occupied
                        : PhysicalOccupancy.Clear;
                    isFresh = true;
                }
                else
                {
                    occupancy = PhysicalOccupancy.Unknown;
                    isFresh = false;
                }

                sections.Add(new PhysicalSectionState(
                    LogicalSectionId: binding.SectionId,
                    Occupancy: occupancy,
                    ObservedAtUtc: observedAtUtc,
                    Age: age,
                    IsFresh: isFresh,
                    SourceHealth: isFresh ? PhysicalSourceHealth.Healthy : PhysicalSourceHealth.Degraded));
            }

            sections.Sort((a, b) => a.LogicalSectionId.CompareTo(b.LogicalSectionId));

            bool allFresh = sections.All(s => s.IsFresh);
            return new PhysicalSectionOccupancyObservation(
                ProfileId: profileId,
                SourceGeneration: sourceGeneration,
                Sequence: sequence,
                CapturedAtUtc: capturedAtUtc,
                SourceValid: true,
                SourceHealth: allFresh ? PhysicalSourceHealth.Healthy : PhysicalSourceHealth.Degraded,
                Sections: sections);
        }

        /// <summary>
        /// The mapping is valid when it is non-empty and one-to-one: every physical amplifier is in
        /// the authoritative track-amplifier range 1..50, and no section id or physical amplifier
        /// address appears more than once.
        /// </summary>
        private static bool IsValidMapping(IReadOnlyList<LayoutPhysicalAmplifierBinding> physicalMapping)
        {
            if (physicalMapping.Count == 0)
            {
                return false;
            }

            var seenSections = new HashSet<int>();
            var seenAmplifiers = new HashSet<int>();
            foreach (LayoutPhysicalAmplifierBinding binding in physicalMapping)
            {
                if (binding.PhysicalAmplifier < 1 || binding.PhysicalAmplifier > 50)
                {
                    return false;
                }

                if (!seenSections.Add(binding.SectionId))
                {
                    return false;
                }

                if (!seenAmplifiers.Add(binding.PhysicalAmplifier))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
