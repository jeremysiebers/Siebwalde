using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Projects a block-level logical shadow and a physical section occupancy observation into a
    /// single <see cref="LogicalPhysicalReconciliationObservation"/>. Pure and stateless: it rebuilds
    /// the reconciliation from scratch each call and never retains state between calls, so there is no
    /// stale-ownership inheritance. It is a total function — null or invalid inputs yield a
    /// non-assessable observation and it never throws.
    /// <para>
    /// KIS10-02: the shadow is block-level, so the projector inverts block-level occupied/reserved
    /// into per-section roles using the 1:N <paramref name="blockToSections"/> map. The per-section
    /// reconciliation state and <see cref="SectionSafetyEligibility"/> stay section-level; a block-level
    /// roll-up (<see cref="LogicalBlockReconciliation"/>) is added WITHOUT removing the per-section enum.
    /// </para>
    /// </summary>
    public static class LogicalPhysicalReconciliationProjector
    {
        /// <summary>
        /// Reconciles <paramref name="shadow"/> (logical, block-level) against <paramref name="physical"/>
        /// (physical, section-level). <paramref name="blockToSections"/> maps each Koploper internal
        /// block to its 1..N bound logical sections; its flattened section set is the logical-binding
        /// domain used by the P1 binding check. Global gates G1–G3 run first; a failed gate yields a
        /// non-assessable observation with every section marked NotAssessable/Denied. On success each
        /// section is evaluated by the per-section precedence P1–P11, and each owned block is rolled up.
        /// </summary>
        public static LogicalPhysicalReconciliationObservation Reconcile(
            KoploperLogicalSectionShadowObservation? shadow,
            PhysicalSectionOccupancyObservation? physical,
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockToSections,
            long reconciliationSequence,
            DateTimeOffset evaluatedAtUtc)
        {
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockMap =
                blockToSections ?? new Dictionary<int, IReadOnlyList<int>>();
            var boundSet = new HashSet<int>(blockMap.Values.SelectMany(sections => sections ?? Array.Empty<int>()));

            // G1: logical source must be present and shadow-valid.
            bool logicalSourceValid = shadow is not null && shadow.ShadowValid;

            // G2: physical source must be present, source-valid and not unavailable.
            bool physicalSourceValid = physical is not null
                && physical.SourceValid
                && physical.SourceHealth != PhysicalSourceHealth.Unavailable;

            // G3: the two observations must describe the same profile.
            bool profileMismatch = shadow is not null && physical is not null
                && !string.Equals(shadow.ProfileId, physical.ProfileId, StringComparison.Ordinal);

            var globalDiagnostics = new List<ReconciliationDiagnosticCode>();
            if (!logicalSourceValid)
            {
                globalDiagnostics.Add(ReconciliationDiagnosticCode.ReconLogicalSourceInvalid);
            }

            if (!physicalSourceValid)
            {
                globalDiagnostics.Add(ReconciliationDiagnosticCode.ReconPhysicalSourceInvalid);
            }

            if (profileMismatch)
            {
                globalDiagnostics.Add(ReconciliationDiagnosticCode.ReconProfileMismatch);
            }

            bool assessable = logicalSourceValid && physicalSourceValid && !profileMismatch;

            string profileId = shadow?.ProfileId ?? physical?.ProfileId ?? string.Empty;

            // Invert the shadow's per-loco BLOCK projection into per-section roles AND per-block roles.
            // Occupied wins over reserved on conflict; a section/block listed by no locomotive is None.
            var roleBySection = new Dictionary<int, (AutomaticSectionRole Role, int? Owner)>();
            var roleByBlock = new Dictionary<int, (AutomaticSectionRole Role, int? Owner)>();
            if (shadow is not null)
            {
                foreach (LogicalLocomotiveShadow loco in shadow.Locomotives)
                {
                    if (loco.OccupiedLogicalBlock is int occupiedBlock)
                    {
                        roleByBlock[occupiedBlock] = (AutomaticSectionRole.Occupied, loco.InternalLocomotiveId);
                        if (blockMap.TryGetValue(occupiedBlock, out IReadOnlyList<int>? occupiedSections))
                        {
                            foreach (int section in occupiedSections)
                            {
                                roleBySection[section] = (AutomaticSectionRole.Occupied, loco.InternalLocomotiveId);
                            }
                        }
                    }
                }

                foreach (LogicalLocomotiveShadow loco in shadow.Locomotives)
                {
                    foreach (int reservedBlock in loco.ReservedLogicalBlocks)
                    {
                        if (!roleByBlock.ContainsKey(reservedBlock))
                        {
                            roleByBlock[reservedBlock] = (AutomaticSectionRole.Reserved, loco.InternalLocomotiveId);
                        }

                        if (blockMap.TryGetValue(reservedBlock, out IReadOnlyList<int>? reservedSections))
                        {
                            foreach (int section in reservedSections)
                            {
                                if (!roleBySection.ContainsKey(section))
                                {
                                    roleBySection[section] = (AutomaticSectionRole.Reserved, loco.InternalLocomotiveId);
                                }
                            }
                        }
                    }
                }
            }

            var manualBlockedSections = new HashSet<int>(shadow?.ManualBlockedSections ?? Array.Empty<int>());

            var physicalBySection = new Dictionary<int, PhysicalSectionState>();
            if (physical is not null)
            {
                foreach (PhysicalSectionState section in physical.Sections)
                {
                    physicalBySection[section.LogicalSectionId] = section;
                }
            }

            var sectionIds = new SortedSet<int>();
            foreach (int id in roleBySection.Keys)
            {
                sectionIds.Add(id);
            }

            foreach (int id in manualBlockedSections)
            {
                sectionIds.Add(id);
            }

            foreach (int id in physicalBySection.Keys)
            {
                sectionIds.Add(id);
            }

            var sections = new List<LogicalSectionReconciliation>(sectionIds.Count);
            foreach (int sectionId in sectionIds)
            {
                roleBySection.TryGetValue(sectionId, out (AutomaticSectionRole Role, int? Owner) role);
                bool manualBlocked = manualBlockedSections.Contains(sectionId);
                bool physicalPresent = physicalBySection.TryGetValue(sectionId, out PhysicalSectionState? physicalState);

                PhysicalOccupancy occupancy = physicalState?.Occupancy ?? PhysicalOccupancy.Unknown;
                bool fresh = physicalState?.IsFresh ?? false;

                sections.Add(BuildSection(
                    sectionId,
                    role.Role,
                    role.Owner,
                    manualBlocked,
                    occupancy,
                    fresh,
                    physicalPresent,
                    assessable,
                    boundSet));
            }

            var sectionMap = sections.ToDictionary(s => s.LogicalSectionId);

            var locomotives = new List<LogicalLocomotiveReconciliation>();
            if (shadow is not null)
            {
                foreach (LogicalLocomotiveShadow loco in shadow.Locomotives.OrderBy(l => l.InternalLocomotiveId))
                {
                    locomotives.Add(BuildLocomotive(loco, blockMap, sectionMap, assessable));
                }
            }

            // Block-level roll-up: a logical block is Eligible iff ALL its bound sections are Eligible.
            var blocks = new List<LogicalBlockReconciliation>(roleByBlock.Count);
            foreach (var (blockId, role) in roleByBlock.OrderBy(kv => kv.Key))
            {
                bool allEligible = assessable;
                if (assessable && blockMap.TryGetValue(blockId, out IReadOnlyList<int>? blockSections) && blockSections is { Count: > 0 })
                {
                    allEligible = blockSections.All(sectionId =>
                        sectionMap.TryGetValue(sectionId, out LogicalSectionReconciliation? section) &&
                        section.SectionSafetyEligibility == SectionSafetyEligibility.Eligible);
                }
                else if (assessable)
                {
                    allEligible = false;
                }

                blocks.Add(new LogicalBlockReconciliation(blockId, role.Owner, allEligible));
            }

            return new LogicalPhysicalReconciliationObservation(
                ProfileId: profileId,
                LogicalGeneration: shadow?.Generation,
                LogicalSourceSequence: shadow?.SourceSequence ?? 0,
                LogicalShadowSequence: shadow?.ShadowSequence ?? 0,
                PhysicalGeneration: physical?.SourceGeneration ?? 0,
                PhysicalSequence: physical?.Sequence ?? 0,
                ReconciliationSequence: reconciliationSequence,
                EvaluatedAtUtc: evaluatedAtUtc,
                LogicalSourceValid: logicalSourceValid,
                PhysicalSourceValid: physicalSourceValid,
                ReconciliationAssessable: assessable,
                Sections: sections,
                Locomotives: locomotives,
                Blocks: blocks,
                Diagnostics: globalDiagnostics.Distinct().OrderBy(d => d).ToArray());
        }

        private static LogicalSectionReconciliation BuildSection(
            int sectionId,
            AutomaticSectionRole role,
            int? owner,
            bool manualBlocked,
            PhysicalOccupancy occupancy,
            bool fresh,
            bool physicalPresent,
            bool assessable,
            HashSet<int> boundSet)
        {
            if (!assessable)
            {
                return new LogicalSectionReconciliation(
                    sectionId,
                    role,
                    owner,
                    manualBlocked,
                    occupancy,
                    fresh,
                    LogicalSectionReconciliationState.NotAssessable,
                    SectionSafetyEligibility.Denied,
                    Array.Empty<ReconciliationDiagnosticCode>());
            }

            // P1: no physical binding.
            if (!boundSet.Contains(sectionId))
            {
                return new LogicalSectionReconciliation(
                    sectionId,
                    role,
                    owner,
                    manualBlocked,
                    occupancy,
                    fresh,
                    LogicalSectionReconciliationState.NotAssessable,
                    SectionSafetyEligibility.NotAssessable,
                    new[] { ReconciliationDiagnosticCode.ReconSectionBindingInvalid });
            }

            // P2: manually blocked (operator reservation, no automatic owner).
            if (manualBlocked)
            {
                return new LogicalSectionReconciliation(
                    sectionId,
                    role,
                    owner,
                    ManualBlocked: true,
                    occupancy,
                    fresh,
                    LogicalSectionReconciliationState.ManualBlocked,
                    SectionSafetyEligibility.Denied,
                    new[] { ReconciliationDiagnosticCode.ReconManualBlocked });
            }

            // P3: physical section missing.
            if (!physicalPresent)
            {
                return new LogicalSectionReconciliation(
                    sectionId,
                    role,
                    owner,
                    ManualBlocked: false,
                    occupancy,
                    fresh,
                    LogicalSectionReconciliationState.NotAssessable,
                    SectionSafetyEligibility.NotAssessable,
                    new[] { ReconciliationDiagnosticCode.ReconPhysicalSectionMissing });
            }

            // P4: physical occupancy unknown.
            if (occupancy == PhysicalOccupancy.Unknown)
            {
                return new LogicalSectionReconciliation(
                    sectionId,
                    role,
                    owner,
                    ManualBlocked: false,
                    occupancy,
                    fresh,
                    LogicalSectionReconciliationState.NotAssessable,
                    SectionSafetyEligibility.NotAssessable,
                    new[] { ReconciliationDiagnosticCode.ReconPhysicalUnknown });
            }

            // P5: physical occupancy not fresh.
            if (!fresh)
            {
                return new LogicalSectionReconciliation(
                    sectionId,
                    role,
                    owner,
                    ManualBlocked: false,
                    occupancy,
                    fresh,
                    LogicalSectionReconciliationState.NotAssessable,
                    SectionSafetyEligibility.NotAssessable,
                    new[] { ReconciliationDiagnosticCode.ReconPhysicalStale });
            }

            // P6–P11: the six role/occupancy combinations.
            switch (role)
            {
                case AutomaticSectionRole.None when occupancy == PhysicalOccupancy.Clear:
                    // P6
                    return new LogicalSectionReconciliation(
                        sectionId, role, owner, false, occupancy, fresh,
                        LogicalSectionReconciliationState.ConsistFreeClear,
                        SectionSafetyEligibility.Denied,
                        Array.Empty<ReconciliationDiagnosticCode>());

                case AutomaticSectionRole.None:
                    // P9: physically occupied without a logical owner.
                    return new LogicalSectionReconciliation(
                        sectionId, role, owner, false, occupancy, fresh,
                        LogicalSectionReconciliationState.ContradictionPhysicalWithoutLogicalOwner,
                        SectionSafetyEligibility.Denied,
                        new[] { ReconciliationDiagnosticCode.ReconFreePhysicallyOccupied });

                case AutomaticSectionRole.Reserved when occupancy == PhysicalOccupancy.Clear:
                    // P7
                    return new LogicalSectionReconciliation(
                        sectionId, role, owner, false, occupancy, fresh,
                        LogicalSectionReconciliationState.ConsistReservedClear,
                        SectionSafetyEligibility.Eligible,
                        Array.Empty<ReconciliationDiagnosticCode>());

                case AutomaticSectionRole.Reserved:
                    // P11
                    return new LogicalSectionReconciliation(
                        sectionId, role, owner, false, occupancy, fresh,
                        LogicalSectionReconciliationState.ContradictionReservedPhysicallyOccupied,
                        SectionSafetyEligibility.Denied,
                        new[] { ReconciliationDiagnosticCode.ReconReservedPhysicallyOccupied });

                case AutomaticSectionRole.Occupied when occupancy == PhysicalOccupancy.Occupied:
                    // P8
                    return new LogicalSectionReconciliation(
                        sectionId, role, owner, false, occupancy, fresh,
                        LogicalSectionReconciliationState.ConsistOccupied,
                        SectionSafetyEligibility.Eligible,
                        Array.Empty<ReconciliationDiagnosticCode>());

                case AutomaticSectionRole.Occupied:
                    // P10
                    return new LogicalSectionReconciliation(
                        sectionId, role, owner, false, occupancy, fresh,
                        LogicalSectionReconciliationState.ContradictionLogicalOccupiedPhysicalClear,
                        SectionSafetyEligibility.Denied,
                        new[] { ReconciliationDiagnosticCode.ReconOccupiedPhysicallyClear });

                default:
                    // Unreachable: Unknown/fresh handled above, and Clear/Occupied are exhaustive.
                    return new LogicalSectionReconciliation(
                        sectionId, role, owner, manualBlocked, occupancy, fresh,
                        LogicalSectionReconciliationState.NotAssessable,
                        SectionSafetyEligibility.NotAssessable,
                        Array.Empty<ReconciliationDiagnosticCode>());
            }
        }

        private static LogicalLocomotiveReconciliation BuildLocomotive(
            LogicalLocomotiveShadow loco,
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockMap,
            IReadOnlyDictionary<int, LogicalSectionReconciliation> sectionMap,
            bool assessable)
        {
            if (!assessable)
            {
                return new LogicalLocomotiveReconciliation(
                    loco.InternalLocomotiveId,
                    loco.OccupiedLogicalBlock,
                    loco.ReservedLogicalBlocks,
                    AllOwnedSectionsAssessable: false,
                    AllOwnedSectionsConsistent: false);
            }

            var ownedSectionIds = new List<int>();
            if (loco.OccupiedLogicalBlock is int occupiedBlock &&
                blockMap.TryGetValue(occupiedBlock, out IReadOnlyList<int>? occupiedSections))
            {
                ownedSectionIds.AddRange(occupiedSections);
            }

            foreach (int reservedBlock in loco.ReservedLogicalBlocks)
            {
                if (blockMap.TryGetValue(reservedBlock, out IReadOnlyList<int>? reservedSections))
                {
                    ownedSectionIds.AddRange(reservedSections);
                }
            }

            bool allAssessable = ownedSectionIds.All(id =>
                sectionMap.TryGetValue(id, out LogicalSectionReconciliation? section)
                && section.ReconciliationState != LogicalSectionReconciliationState.NotAssessable);

            bool allConsistent = ownedSectionIds.All(id =>
                sectionMap.TryGetValue(id, out LogicalSectionReconciliation? section)
                && IsConsistent(section.ReconciliationState));

            return new LogicalLocomotiveReconciliation(
                loco.InternalLocomotiveId,
                loco.OccupiedLogicalBlock,
                loco.ReservedLogicalBlocks,
                allAssessable,
                allConsistent);
        }

        private static bool IsConsistent(LogicalSectionReconciliationState state)
            => state is LogicalSectionReconciliationState.ConsistFreeClear
                or LogicalSectionReconciliationState.ConsistReservedClear
                or LogicalSectionReconciliationState.ConsistOccupied;
    }
}
