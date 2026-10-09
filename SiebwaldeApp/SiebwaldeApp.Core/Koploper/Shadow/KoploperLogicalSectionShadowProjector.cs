using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Projects a KIS-05 reservation observation plus a KIS-04 state snapshot onto Siebwalde
    /// logical sections, producing a <see cref="KoploperLogicalSectionShadowObservation"/>. Pure
    /// and stateless: it never retains state between calls and rebuilds the projection from scratch
    /// each cycle (no stale ownership is inherited).
    /// <para>
    /// The authority gate is the reservation observation's <see cref="KoploperReservationObservation.IsAuthoritative"/>
    /// (KIS-05). The per-block manual-blocked state, the per-block automatic state and the owner all
    /// come from the <see cref="KoploperStateSnapshot"/> (KIS-04), because the reservation
    /// observation does not carry manual state.
    /// </para>
    /// </summary>
    public static class KoploperLogicalSectionShadowProjector
    {
        /// <summary>
        /// Projects <paramref name="observation"/> and <paramref name="snapshot"/> into a shadow
        /// observation. <paramref name="internalBlockToSection"/> maps a Koploper internal block id
        /// (machine identity) to a Siebwalde logical section id. <paramref name="shadowSequence"/> is
        /// the shadow observer's own monotonic counter, <paramref name="observedAtUtc"/> the
        /// observation instant, and <paramref name="maxSnapshotAge"/> (when non-null) bounds the
        /// freshness of the manual-state snapshot.
        /// </summary>
        public static KoploperLogicalSectionShadowObservation Project(
            KoploperReservationObservation observation,
            KoploperStateSnapshot? snapshot,
            string profileId,
            IReadOnlyDictionary<int, int> internalBlockToSection,
            long shadowSequence,
            DateTimeOffset observedAtUtc,
            TimeSpan? maxSnapshotAge)
        {
            ArgumentNullException.ThrowIfNull(observation);
            ArgumentNullException.ThrowIfNull(internalBlockToSection);
            ArgumentNullException.ThrowIfNull(profileId);

            bool mappingValid = IsOneToOne(internalBlockToSection);
            bool sourceAuthoritative = observation.IsAuthoritative;

            if (!sourceAuthoritative)
            {
                return NonAuthoritative(observation, profileId, mappingValid, shadowSequence, observedAtUtc,
                    observation.SourceHealth, observation.IsFresh);
            }

            if (snapshot is null)
            {
                return NonAuthoritative(observation, profileId, mappingValid, shadowSequence, observedAtUtc,
                    KoploperSourceHealth.Unavailable, isFresh: false);
            }

            bool isFresh = maxSnapshotAge is null
                || KoploperSnapshotFreshness.IsCurrent(snapshot, observedAtUtc, maxSnapshotAge.Value);

            if (!isFresh)
            {
                return NonAuthoritative(observation, profileId, mappingValid, shadowSequence, observedAtUtc,
                    KoploperSourceHealth.Stale, isFresh: false);
            }

            bool manualStateValid = snapshot.Blocks.All(block => block.ManualBlocked != KoploperManualBlockState.Invalid);

            // Per-loco projection buckets, lazily created for any owner id encountered.
            var occupied = new Dictionary<int, int?>();
            var reserved = new Dictionary<int, HashSet<int>>();

            var manualBlockedSections = new HashSet<int>();
            var unmappedBlocks = new List<KoploperUnmappedBlock>();
            var conflicts = new List<KoploperLogicalSectionConflict>();
            var diagnostics = new List<KoploperDiagnosticCode>();
            bool unmappedOwned = false;
            bool unmappedManual = false;

            foreach (KoploperBlockSnapshot block in snapshot.Blocks)
            {
                bool mapped = internalBlockToSection.TryGetValue(block.InternalBlockId, out int sectionId);

                if (!mapped)
                {
                    bool owned = (block.State == KoploperBlockState.Reserved || block.State == KoploperBlockState.Occupied)
                        && block.OwnerLocomotiveId.HasValue;
                    bool manualBlocked = block.ManualBlocked == KoploperManualBlockState.Blocked;

                    if (owned || manualBlocked)
                    {
                        unmappedBlocks.Add(new KoploperUnmappedBlock(
                            block.InternalBlockId,
                            block.State,
                            block.OwnerLocomotiveId,
                            block.ManualBlocked));

                        if (owned)
                        {
                            unmappedOwned = true;
                            diagnostics.Add(KoploperDiagnosticCode.KOPLOPER_UNMAPPED_OWNED_BLOCK);
                        }

                        if (manualBlocked)
                        {
                            unmappedManual = true;
                            diagnostics.Add(KoploperDiagnosticCode.KOPLOPER_UNMAPPED_MANUAL_BLOCK);
                        }
                    }

                    continue;
                }

                bool automaticOwnership = block.State == KoploperBlockState.Reserved || block.State == KoploperBlockState.Occupied;
                bool manualBlockedMapped = block.ManualBlocked == KoploperManualBlockState.Blocked;

                // Manual-blocked + automatic reserved/occupied is a conflict; the block contributes nothing.
                if (manualBlockedMapped && automaticOwnership)
                {
                    conflicts.Add(new KoploperLogicalSectionConflict(
                        KoploperLogicalSectionConflictKind.ManualVsAutomatic,
                        block.InternalBlockId,
                        sectionId,
                        block.OwnerLocomotiveId));
                    diagnostics.Add(KoploperDiagnosticCode.KOPLOPER_MANUAL_AUTOMATIC_CONFLICT);
                    continue;
                }

                if (block.OwnerLocomotiveId is int ownerId)
                {
                    switch (block.State)
                    {
                        case KoploperBlockState.Occupied:
                            if (occupied.TryGetValue(ownerId, out int? existingOccupied) && existingOccupied.HasValue)
                            {
                                if (existingOccupied.Value != sectionId)
                                {
                                    conflicts.Add(new KoploperLogicalSectionConflict(
                                        KoploperLogicalSectionConflictKind.MultipleOccupiedSections,
                                        block.InternalBlockId,
                                        sectionId,
                                        ownerId));
                                }
                            }
                            else
                            {
                                occupied[ownerId] = sectionId;
                            }

                            break;

                        case KoploperBlockState.Reserved:
                            if (!reserved.TryGetValue(ownerId, out HashSet<int>? set))
                            {
                                set = new HashSet<int>();
                                reserved[ownerId] = set;
                            }

                            set.Add(sectionId);
                            break;

                        default:
                            // Free, Transition and Unknown blocks do not contribute automatic ownership.
                            break;
                    }
                }

                if (manualBlockedMapped)
                {
                    manualBlockedSections.Add(sectionId);
                }
            }

            // Build the per-loco shadow list from the union of declared locomotives and any owner
            // ids encountered (sorted for determinism). The decoder guarantees owner ids are a
            // subset of the declared locomotives, so this is normally identical to the loco list.
            var allLocoIds = new SortedSet<int>(snapshot.Locomotives.Select(l => l.InternalLocomotiveId));
            allLocoIds.UnionWith(occupied.Keys);
            allLocoIds.UnionWith(reserved.Keys);

            var locomotives = new List<LogicalLocomotiveShadow>(allLocoIds.Count);
            foreach (int locoId in allLocoIds)
            {
                int? occ = occupied.TryGetValue(locoId, out int? occValue) ? occValue : null;
                int[] res = reserved.TryGetValue(locoId, out HashSet<int>? resSet)
                    ? resSet.OrderBy(s => s).ToArray()
                    : Array.Empty<int>();

                locomotives.Add(new LogicalLocomotiveShadow(locoId, occ, res));
            }

            conflicts.Sort((a, b) =>
            {
                int byBlock = a.InternalBlockId.CompareTo(b.InternalBlockId);
                if (byBlock != 0)
                {
                    return byBlock;
                }

                return a.Kind.CompareTo(b.Kind);
            });

            bool shadowValid = mappingValid
                && manualStateValid
                && conflicts.Count == 0
                && !unmappedOwned
                && !unmappedManual;

            return new KoploperLogicalSectionShadowObservation(
                Generation: observation.Generation,
                SourceSequence: observation.SourceSequence,
                ShadowSequence: shadowSequence,
                CapturedAtUtc: observation.CapturedAtUtc,
                ObservedAtUtc: observedAtUtc,
                IsFresh: true,
                SourceHealth: observation.SourceHealth,
                SourceAuthoritative: true,
                ProfileId: profileId,
                MappingValid: mappingValid,
                ManualStateValid: manualStateValid,
                ShadowValid: shadowValid,
                Locomotives: locomotives,
                ManualBlockedSections: manualBlockedSections.OrderBy(s => s).ToArray(),
                UnmappedBlocks: unmappedBlocks,
                Conflicts: conflicts,
                Diagnostics: diagnostics.Distinct().OrderBy(code => code).ToArray());
        }

        /// <summary>
        /// The binding map is valid when it is non-empty AND one-to-one (no two internal block ids
        /// map to the same logical section).
        /// </summary>
        private static bool IsOneToOne(IReadOnlyDictionary<int, int> internalBlockToSection)
        {
            if (internalBlockToSection.Count == 0)
            {
                return false;
            }

            var seenSections = new HashSet<int>();
            foreach (int section in internalBlockToSection.Values)
            {
                if (!seenSections.Add(section))
                {
                    return false;
                }
            }

            return true;
        }

        private static KoploperLogicalSectionShadowObservation NonAuthoritative(
            KoploperReservationObservation observation,
            string profileId,
            bool mappingValid,
            long shadowSequence,
            DateTimeOffset observedAtUtc,
            KoploperSourceHealth sourceHealth,
            bool isFresh)
        {
            return new KoploperLogicalSectionShadowObservation(
                Generation: observation.Generation,
                SourceSequence: observation.SourceSequence,
                ShadowSequence: shadowSequence,
                CapturedAtUtc: observation.CapturedAtUtc,
                ObservedAtUtc: observedAtUtc,
                IsFresh: isFresh,
                SourceHealth: sourceHealth,
                SourceAuthoritative: false,
                ProfileId: profileId,
                MappingValid: mappingValid,
                ManualStateValid: false,
                ShadowValid: false,
                Locomotives: Array.Empty<LogicalLocomotiveShadow>(),
                ManualBlockedSections: Array.Empty<int>(),
                UnmappedBlocks: Array.Empty<KoploperUnmappedBlock>(),
                Conflicts: Array.Empty<KoploperLogicalSectionConflict>(),
                Diagnostics: Array.Empty<KoploperDiagnosticCode>());
        }
    }
}
