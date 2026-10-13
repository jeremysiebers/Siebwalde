using System;
using System.Collections.Generic;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A PURE, STATELESS helper that reconstructs a plausible two-section physical footprint for a
    /// locomotive at startup/recovery, when the carrier has no remembered footprint. It answers ONLY
    /// "which physical sections are plausibly occupied by locomotive A" — it NEVER produces drive
    /// authority or a PWM target (see the ownership-aware carrier). Movement still requires valid
    /// logical authority (KIS-07 shadow) plus retained drive state plus speed &gt; 0.
    ///
    /// Reconstruction is FAIL-CLOSED: it returns an empty footprint unless every listed condition is
    /// met for a candidate pair. A partial or ambiguous match produces no attribution.
    /// </summary>
    public static class PhysicalFootprintReconstructor
    {
        public static IReadOnlyDictionary<int, int> Reconstruct(
            KoploperLogicalSectionShadowObservation? shadow,
            PhysicalSectionOccupancyObservation? physical,
            BlockTopology topology,
            IReadOnlyList<LayoutPhysicalAmplifierBinding> mapping,
            IReadOnlyDictionary<int, int> identity)
        {
            ArgumentNullException.ThrowIfNull(topology);
            ArgumentNullException.ThrowIfNull(mapping);
            ArgumentNullException.ThrowIfNull(identity);

            var footprint = new Dictionary<int, int>();

            // Fail closed: no valid shadow or no valid physical source means no reconstruction.
            if (shadow is null || !shadow.ShadowValid || physical is null || !physical.SourceValid)
            {
                return footprint;
            }

            var physicalBySection = new Dictionary<int, PhysicalSectionState>();
            foreach (PhysicalSectionState state in physical.Sections)
            {
                physicalBySection[state.LogicalSectionId] = state;
            }

            // Section -> amp (and amp -> sections) from the physical mapping.
            var sectionToAmp = new Dictionary<int, int>();
            var ampToSections = new Dictionary<int, List<int>>();
            foreach (LayoutPhysicalAmplifierBinding binding in mapping)
            {
                if (!sectionToAmp.ContainsKey(binding.SectionId))
                {
                    sectionToAmp[binding.SectionId] = binding.PhysicalAmplifier;
                }

                if (!ampToSections.TryGetValue(binding.PhysicalAmplifier, out List<int>? list))
                {
                    list = new List<int>();
                    ampToSections[binding.PhysicalAmplifier] = list;
                }

                if (!list.Contains(binding.SectionId))
                {
                    list.Add(binding.SectionId);
                }
            }

            // Logical ownership per section: Occupied resolves first, then Reserved fills only the
            // still-unowned sections (the same precedence as the projector, so the two never disagree).
            var sectionOwner = new Dictionary<int, int>();
            var occupiedBy = new Dictionary<int, int>();
            var reservedBy = new Dictionary<int, List<int>>();

            foreach (LogicalLocomotiveShadow loco in shadow.Locomotives)
            {
                if (!identity.ContainsKey(loco.InternalLocomotiveId))
                {
                    continue;
                }

                if (loco.OccupiedLogicalSection is int occ)
                {
                    occupiedBy[occ] = loco.InternalLocomotiveId;
                    sectionOwner[occ] = loco.InternalLocomotiveId;
                }

                foreach (int r in loco.ReservedLogicalSections ?? Array.Empty<int>())
                {
                    if (!reservedBy.TryGetValue(r, out List<int>? owners))
                    {
                        owners = new List<int>();
                        reservedBy[r] = owners;
                    }

                    owners.Add(loco.InternalLocomotiveId);
                }
            }

            foreach (LogicalLocomotiveShadow loco in shadow.Locomotives)
            {
                if (!identity.ContainsKey(loco.InternalLocomotiveId))
                {
                    continue;
                }

                foreach (int r in loco.ReservedLogicalSections ?? Array.Empty<int>())
                {
                    if (!sectionOwner.ContainsKey(r))
                    {
                        sectionOwner[r] = loco.InternalLocomotiveId;
                    }
                }
            }

            // A section named in a shadow conflict must never participate in reconstruction.
            var conflictedSections = new HashSet<int>();
            foreach (KoploperLogicalSectionConflict conflict in shadow.Conflicts ?? Array.Empty<KoploperLogicalSectionConflict>())
            {
                conflictedSections.Add(conflict.LogicalSectionId);
            }

            foreach (LogicalLocomotiveShadow loco in shadow.Locomotives)
            {
                if (!identity.TryGetValue(loco.InternalLocomotiveId, out int dcc))
                {
                    continue;
                }

                if (loco.OccupiedLogicalSection is not int source)
                {
                    continue;
                }

                // Anchor: the locomotive's occupied section must be mapped, fresh and physically occupied.
                if (!sectionToAmp.ContainsKey(source) ||
                    conflictedSections.Contains(source) ||
                    !IsPhysicallyOccupiedFresh(physicalBySection, source))
                {
                    continue;
                }

                // The footprint is a SET, not a hard-coded {S,T}: it is the connected component of
                // physically-Occupied sections reachable from the anchor through switch-free topology
                // adjacency, restricted to sections that are safely attributable to this locomotive
                // (its own occupied/reserved sections or Free sections with no competing owner).
                // Expansion is transitive so a three-or-more-section spanning locomotive is fully
                // reconstructed, and stops at any section with a competing owner (fail closed).
                footprint[source] = dcc;

                var usedAmps = new HashSet<int> { sectionToAmp[source] };
                var visited = new HashSet<int> { source };
                var queue = new Queue<int>();
                queue.Enqueue(source);

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();

                    foreach (int candidate in topology.Blocks)
                    {
                        if (candidate == current || visited.Contains(candidate))
                        {
                            continue;
                        }

                        if (!AreAdjacent(topology, current, candidate))
                        {
                            continue;
                        }

                        if (!sectionToAmp.ContainsKey(candidate) ||
                            conflictedSections.Contains(candidate) ||
                            !IsPhysicallyOccupiedFresh(physicalBySection, candidate))
                        {
                            continue;
                        }

                        // The candidate must relate to the SAME locomotive (its reservation) or be free.
                        // Any other owner is a conflict and fails closed.
                        if (sectionOwner.TryGetValue(candidate, out int candidateOwner) &&
                            candidateOwner != loco.InternalLocomotiveId)
                        {
                            continue;
                        }

                        // No other locomotive may reserve the candidate.
                        if (reservedBy.TryGetValue(candidate, out List<int>? reservers) &&
                            reservers.Exists(r => r != loco.InternalLocomotiveId))
                        {
                            continue;
                        }

                        // The candidate must map to an amplifier not already used by this footprint
                        // (no two footprint sections may collide on one amplifier).
                        if (!usedAmps.Add(sectionToAmp[candidate]))
                        {
                            continue;
                        }

                        visited.Add(candidate);
                        footprint[candidate] = dcc;
                        queue.Enqueue(candidate);
                    }
                }
            }

            return footprint;
        }

        private static bool AreAdjacent(BlockTopology topology, int a, int b)
        {
            var noSwitches = new Dictionary<int, SwitchPosition>();

            if (topology.TryGetNextBlocks(a, noSwitches, lookAheadOnly: false, out List<int>? next) &&
                next.Contains(b))
            {
                return true;
            }

            if (topology.TryGetNextBlocks(b, noSwitches, lookAheadOnly: false, out List<int>? prev) &&
                prev.Contains(a))
            {
                return true;
            }

            return false;
        }

        private static bool IsPhysicallyOccupiedFresh(
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            int section)
            => physicalBySection.TryGetValue(section, out PhysicalSectionState? state) &&
               state.IsFresh &&
               state.Occupancy == PhysicalOccupancy.Occupied;
    }
}
