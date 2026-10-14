using System;
using System.Collections.Generic;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A PURE, STATELESS helper that reconstructs a plausible physical footprint for a locomotive at
    /// startup/recovery, when the carrier has no remembered footprint. It answers ONLY "which physical
    /// sections are plausibly occupied by locomotive A" — it NEVER produces drive authority or a PWM
    /// target (see the ownership-aware carrier). Movement still requires valid logical authority
    /// (KIS-07 shadow) plus retained drive state plus speed &gt; 0.
    ///
    /// KIS10-05: it floods over SECTIONS, not blocks. Section adjacency is (a) intra-block ordered
    /// <c>SectionIds</c> of one block, and (b) across blocks via switch-free block routes (last section
    /// of block B ↔ first section of block C). The anchor is the locomotive's
    /// <c>OccupiedLogicalBlock</c>'s fresh + physically occupied sections.
    ///
    /// Reconstruction is FAIL-CLOSED: it returns an empty footprint unless every listed condition is
    /// met. A partial or ambiguous match produces no attribution (no partial first departure).
    /// </summary>
    public static class PhysicalFootprintReconstructor
    {
        public static IReadOnlyDictionary<int, int> Reconstruct(
            KoploperLogicalSectionShadowObservation? shadow,
            PhysicalSectionOccupancyObservation? physical,
            BlockTopology topology,
            IReadOnlyList<LayoutPhysicalAmplifierBinding> mapping,
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockToSections,
            IReadOnlyDictionary<int, int> identity)
        {
            ArgumentNullException.ThrowIfNull(topology);
            ArgumentNullException.ThrowIfNull(mapping);
            ArgumentNullException.ThrowIfNull(blockToSections);
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

            // Section -> amp from the physical mapping.
            var sectionToAmp = new Dictionary<int, int>();
            foreach (LayoutPhysicalAmplifierBinding binding in mapping)
            {
                sectionToAmp[binding.SectionId] = binding.PhysicalAmplifier;
            }

            // Section -> block (reverse of blockToSections).
            var sectionToBlock = new Dictionary<int, int>();
            foreach (KeyValuePair<int, IReadOnlyList<int>> entry in blockToSections)
            {
                if (entry.Value is null)
                {
                    continue;
                }

                foreach (int section in entry.Value)
                {
                    sectionToBlock[section] = entry.Key;
                }
            }

            Dictionary<int, List<int>> sectionAdjacency = KoploperSectionTopology.BuildSectionAdjacency(blockToSections, topology);

            // Logical ownership per section (internal loco id): Occupied resolves first, then Reserved
            // fills only the still-unowned sections (the same precedence as the projector).
            var sectionOwner = new Dictionary<int, int>();
            var reservedBy = new Dictionary<int, List<int>>();

            foreach (LogicalLocomotiveShadow loco in shadow.Locomotives)
            {
                if (!identity.ContainsKey(loco.InternalLocomotiveId))
                {
                    continue;
                }

                if (loco.OccupiedLogicalBlock is int occBlock &&
                    blockToSections.TryGetValue(occBlock, out IReadOnlyList<int>? occSections))
                {
                    foreach (int section in occSections)
                    {
                        sectionOwner[section] = loco.InternalLocomotiveId;
                    }
                }

                foreach (int r in loco.ReservedLogicalBlocks)
                {
                    if (!blockToSections.TryGetValue(r, out IReadOnlyList<int>? resSections))
                    {
                        continue;
                    }

                    foreach (int section in resSections)
                    {
                        if (!reservedBy.TryGetValue(section, out List<int>? owners))
                        {
                            owners = new List<int>();
                            reservedBy[section] = owners;
                        }

                        owners.Add(loco.InternalLocomotiveId);
                    }
                }
            }

            foreach (LogicalLocomotiveShadow loco in shadow.Locomotives)
            {
                if (!identity.ContainsKey(loco.InternalLocomotiveId))
                {
                    continue;
                }

                foreach (int r in loco.ReservedLogicalBlocks)
                {
                    if (!blockToSections.TryGetValue(r, out IReadOnlyList<int>? resSections))
                    {
                        continue;
                    }

                    foreach (int section in resSections)
                    {
                        if (!sectionOwner.ContainsKey(section))
                        {
                            sectionOwner[section] = loco.InternalLocomotiveId;
                        }
                    }
                }
            }

            // A BLOCK named in a shadow conflict must never participate in reconstruction.
            var conflictedSections = new HashSet<int>();
            foreach (KoploperLogicalSectionConflict conflict in shadow.Conflicts ?? Array.Empty<KoploperLogicalSectionConflict>())
            {
                if (blockToSections.TryGetValue(conflict.InternalBlockId, out IReadOnlyList<int>? conflictedBlockSections))
                {
                    conflictedSections.UnionWith(conflictedBlockSections);
                }
            }

            foreach (LogicalLocomotiveShadow loco in shadow.Locomotives)
            {
                if (!identity.TryGetValue(loco.InternalLocomotiveId, out int dcc))
                {
                    continue;
                }

                if (loco.OccupiedLogicalBlock is not int occupiedBlock ||
                    !blockToSections.TryGetValue(occupiedBlock, out IReadOnlyList<int>? occupiedSections))
                {
                    continue;
                }

                // Anchor: the occupied block's fresh + physically occupied sections. If any occupied
                // anchor section is unmapped or conflicted, reconstruction fails closed.
                var anchorSections = new List<int>();
                bool anchorInvalid = false;
                foreach (int section in occupiedSections)
                {
                    if (!IsPhysicallyOccupiedFresh(physicalBySection, section))
                    {
                        continue;
                    }

                    if (!sectionToAmp.ContainsKey(section) || conflictedSections.Contains(section))
                    {
                        anchorInvalid = true;
                        break;
                    }

                    anchorSections.Add(section);
                }

                if (anchorInvalid || anchorSections.Count == 0)
                {
                    continue;
                }

                // The footprint is a SET: the connected component of physically-Occupied sections
                // reachable from the anchor through section adjacency, restricted to sections that are
                // safely attributable to this locomotive (its own occupied/reserved sections or Free
                // sections with no competing owner). Expansion is transitive and stops at any section
                // with a competing owner or an already-used amplifier (fail closed).
                var usedAmps = new HashSet<int>();
                var visited = new HashSet<int>();
                var queue = new Queue<int>();

                foreach (int section in anchorSections)
                {
                    footprint[section] = dcc;
                    usedAmps.Add(sectionToAmp[section]);
                    visited.Add(section);
                    queue.Enqueue(section);
                }

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();

                    if (!sectionAdjacency.TryGetValue(current, out List<int>? neighbors))
                    {
                        continue;
                    }

                    foreach (int candidate in neighbors)
                    {
                        if (visited.Contains(candidate))
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

        private static bool IsPhysicallyOccupiedFresh(
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            int section)
            => physicalBySection.TryGetValue(section, out PhysicalSectionState? state) &&
               state.IsFresh &&
               state.Occupancy == PhysicalOccupancy.Occupied;
    }
}
