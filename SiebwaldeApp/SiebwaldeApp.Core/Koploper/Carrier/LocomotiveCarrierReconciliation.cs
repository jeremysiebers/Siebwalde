using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The PURE, STATELESS ownership-aware locomotive carrier projector. It reconciles the KIS-07
    /// BLOCK-level shadow (authority, only when <c>ShadowValid</c>) against the KIS-08 physical section
    /// occupancy and the physical amplifier mapping, and emits one <see cref="CarrierAmplifierDecision"/>
    /// per amplifier. It performs no side effects, holds no static state, and does no I/O.
    /// <para>
    /// KIS10-04: authority is BLOCK-level. <c>blockOwner</c> (internal block → DCC) is built from
    /// <c>OccupiedLogicalBlock</c> first then <c>ReservedLogicalBlocks</c> (fill), same precedence as
    /// before. Every physical amplifier resolves to its block via section → block, and ALL amplifiers
    /// of one owned block form that locomotive's DRIVE GROUP and follow the SAME speed/direction
    /// (PO §21 intra-block release semantics: an individual section becoming physically Clear does NOT
    /// neutralize its amp while the block remains authorized).
    /// </para>
    /// </summary>
    public static class LocomotiveCarrierReconciliation
    {
        private const int Neutral = AmplifierSpeedMapper.NeutralPwm;

        public static CarrierDecision Reconcile(
            IReadOnlyDictionary<int, (int Speed, int Direction)> desiredLocoStates,
            IReadOnlyDictionary<int, int> identity,
            KoploperLogicalSectionShadowObservation? shadow,
            PhysicalSectionOccupancyObservation? physical,
            BlockTopology topology,
            IReadOnlyList<LayoutPhysicalAmplifierBinding> mapping,
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockToSections,
            IReadOnlyDictionary<int, (int OwnerDcc, int Hr0)>? previousTargets,
            IReadOnlyDictionary<int, int>? physicalFootprint,
            TimeSpan confirmationWindow)
        {
            ArgumentNullException.ThrowIfNull(desiredLocoStates);
            ArgumentNullException.ThrowIfNull(identity);
            ArgumentNullException.ThrowIfNull(topology);
            ArgumentNullException.ThrowIfNull(mapping);
            ArgumentNullException.ThrowIfNull(blockToSections);

            var diagnostics = new List<string>();
            var decisions = new Dictionary<int, CarrierAmplifierDecision>();

            // Amplifier domain: mapped amplifiers plus any previously-targeted amplifier.
            var domain = new SortedSet<int>();
            foreach (var binding in mapping)
            {
                domain.Add(binding.PhysicalAmplifier);
            }

            if (previousTargets is not null)
            {
                foreach (var amp in previousTargets.Keys)
                {
                    domain.Add(amp);
                }
            }

            // 1) Shadow invalid -> layout stop, all amplifiers neutral (strongest fail closed).
            if (shadow is null || !shadow.ShadowValid)
            {
                diagnostics.Add("KIS shadow is invalid/unavailable; layout stop, all amplifiers neutral.");
                foreach (var amp in domain)
                {
                    decisions[amp] = new CarrierAmplifierDecision(
                        amp, null, Neutral, CarrierVerdict.Neutral,
                        CarrierReason.ShadowInvalidLayoutStop, "shadow-invalid-layout-stop");
                }

                return new CarrierDecision(
                    OrderDecisions(decisions), true, false, Array.Empty<int>(), diagnostics,
                    new Dictionary<int, int>());
            }

            // 2) Per-loco logical ownership at BLOCK level, then expanded to sections.
            var locoOccupiedBlock = new Dictionary<int, int?>();
            var locoReservedBlocks = new Dictionary<int, HashSet<int>>();
            var blockOwner = new Dictionary<int, int>();

            foreach (var loco in shadow.Locomotives)
            {
                if (!identity.TryGetValue(loco.InternalLocomotiveId, out int dcc))
                {
                    diagnostics.Add(
                        $"KIS locomotive internal id {loco.InternalLocomotiveId} has no LocomotiveBinding; " +
                        "it contributes zero physical targets (fail closed).");
                    continue;
                }

                locoOccupiedBlock[dcc] = loco.OccupiedLogicalBlock;
                locoReservedBlocks[dcc] = new HashSet<int>(loco.ReservedLogicalBlocks ?? Array.Empty<int>());

                // Occupied ownership is resolved first: an Occupied(B) block is never taken over by a
                // Reserved(A) block, regardless of shadow iteration order.
                if (loco.OccupiedLogicalBlock is int occBlock)
                {
                    blockOwner[occBlock] = dcc;
                }
            }

            // Reserved ownership fills only blocks that no locomotive already occupies.
            foreach (var loco in shadow.Locomotives)
            {
                if (!identity.TryGetValue(loco.InternalLocomotiveId, out int dcc))
                {
                    continue;
                }

                foreach (var r in loco.ReservedLogicalBlocks ?? Array.Empty<int>())
                {
                    if (!blockOwner.ContainsKey(r))
                    {
                        blockOwner[r] = dcc;
                    }
                }
            }

            // Section -> block (reverse of blockToSections), and section -> owner (block owner expanded).
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

            var sectionOwner = new Dictionary<int, int>();
            foreach (var (block, dcc) in blockOwner)
            {
                if (!blockToSections.TryGetValue(block, out IReadOnlyList<int>? sections))
                {
                    continue;
                }

                foreach (int section in sections)
                {
                    sectionOwner[section] = dcc;
                }
            }

            // Reserved sections per locomotive (block reserved -> sections), for section-level checks.
            var locoReservedSections = new Dictionary<int, HashSet<int>>();
            foreach (var (dcc, reservedBlocks) in locoReservedBlocks)
            {
                var sections = new HashSet<int>();
                foreach (int block in reservedBlocks)
                {
                    if (blockToSections.TryGetValue(block, out IReadOnlyList<int>? blockSections))
                    {
                        sections.UnionWith(blockSections);
                    }
                }

                locoReservedSections[dcc] = sections;
            }

            // 3) Physical state per logical section.
            var physicalBySection = new Dictionary<int, PhysicalSectionState>();
            if (physical is not null)
            {
                foreach (var state in physical.Sections)
                {
                    physicalBySection[state.LogicalSectionId] = state;
                }
            }

            // 4) Amp -> sections and section -> amp.
            var sectionsByAmp = new Dictionary<int, List<int>>();
            foreach (var binding in mapping)
            {
                if (!sectionsByAmp.TryGetValue(binding.PhysicalAmplifier, out var list))
                {
                    list = new List<int>();
                    sectionsByAmp[binding.PhysicalAmplifier] = list;
                }

                list.Add(binding.SectionId);
            }

            var sectionToAmp = new Dictionary<int, int>();
            foreach (var binding in mapping)
            {
                sectionToAmp[binding.SectionId] = binding.PhysicalAmplifier;
            }

            Dictionary<int, List<int>> sectionAdjacency = KoploperSectionTopology.BuildSectionAdjacency(blockToSections, topology);

            var manualBlockedSections = new HashSet<int>(shadow.ManualBlockedSections ?? Array.Empty<int>());

            // 5) Partial-footprint containment (section-level flood).
            HashSet<int> containedDccs = DetectContainedLocos(
                sectionOwner,
                locoReservedSections,
                locoOccupiedBlock,
                blockToSections,
                physicalBySection,
                sectionToAmp,
                sectionAdjacency,
                physicalFootprint,
                previousTargets);

            var locoStop = new HashSet<int>();
            var latched = new HashSet<int>();

            // 6) Block-level drive decisions for every owned block (one verdict per block's drive group).
            var blockVerdicts = new Dictionary<int, (CarrierVerdict Verdict, CarrierReason Reason, int Hr0, int? Owner)>();
            foreach (var (block, dcc) in blockOwner)
            {
                if (!blockToSections.TryGetValue(block, out IReadOnlyList<int>? sections) || sections.Count == 0)
                {
                    continue;
                }

                // Drive-group completeness (PO §21 / F1): resolve the block's COMPLETE drive group
                // through the projector. An EMPTY projection means a bound section has no physical
                // amplifier binding, so the block must NOT be partially energized — fail closed.
                IReadOnlyList<int> driveGroup = KoploperDriveGroupProjector.Project(block, blockToSections, mapping);
                if (driveGroup.Count == 0)
                {
                    int[] unboundSections = sections.Where(s => !sectionToAmp.ContainsKey(s)).ToArray();
                    diagnostics.Add(
                        $"Block {block} has an incomplete drive group: section(s) " +
                        $"{string.Join(", ", unboundSections)} have no physical amplifier binding; " +
                        "fail closed (no partial block energization).");
                    blockVerdicts[block] = (CarrierVerdict.Neutral, CarrierReason.MissingBinding, Neutral, dcc);
                    continue;
                }

                bool manualBlocked = sections.Any(s => manualBlockedSections.Contains(s));
                bool isOccupied = locoOccupiedBlock.TryGetValue(dcc, out int? occ) && occ == block;
                bool contained = containedDccs.Contains(dcc);
                PhysicalOccupancy blockOcc = BlockOccupancy(sections, physicalBySection);
                (int Speed, int Direction) desired = desiredLocoStates.TryGetValue(dcc, out var d)
                    ? d
                    : (0, 1);

                if (manualBlocked)
                {
                    blockVerdicts[block] = (CarrierVerdict.Deny, CarrierReason.ManualBlocked, Neutral, dcc);
                    continue;
                }

                if (blockOcc == PhysicalOccupancy.Unknown)
                {
                    // Fail closed: unknown/stale telemetry over the whole block.
                    blockVerdicts[block] = (CarrierVerdict.Neutral, CarrierReason.PhysicalUnknownStale, Neutral, dcc);
                    continue;
                }

                if (desired.Speed <= 0)
                {
                    blockVerdicts[block] = (CarrierVerdict.Neutral, CarrierReason.SpeedZeroNeutral, Neutral, dcc);
                    continue;
                }

                if (contained)
                {
                    // Neutral-only containment: no loco-stop, no latch, no layout stop.
                    blockVerdicts[block] = (CarrierVerdict.Neutral, CarrierReason.PartialFootprintContain, Neutral, dcc);
                    continue;
                }

                int hr0 = AmplifierSpeedMapper.ToPwm(desired.Speed, desired.Direction);

                if (isOccupied)
                {
                    // PO §21: an authorized (Occupied) block drives its COMPLETE drive group at the
                    // owner's speed, regardless of per-section physical occupancy.
                    blockVerdicts[block] = (CarrierVerdict.Drive, CarrierReason.OccupiedConsistent, hr0, dcc);
                    continue;
                }

                // Reserved block.
                if (blockOcc == PhysicalOccupancy.Clear)
                {
                    blockVerdicts[block] = (CarrierVerdict.Drive, CarrierReason.ReservedPreEnergize, hr0, dcc);
                    continue;
                }

                // Reserved + physically occupied: expected boundary lead, or revoke + loco-stop + latch.
                if (IsExpectedBoundaryLead(locoOccupiedBlock.GetValueOrDefault(dcc), block, blockToSections, topology, physicalBySection, confirmationWindow))
                {
                    blockVerdicts[block] = (CarrierVerdict.Drive, CarrierReason.ExpectedBoundaryLead, hr0, dcc);
                }
                else
                {
                    locoStop.Add(dcc);
                    latched.Add(dcc);
                    blockVerdicts[block] = (CarrierVerdict.Revoke, CarrierReason.ReservedNotExpectedRevoke, Neutral, dcc);
                }
            }

            // 7) Per-amplifier decisions.
            foreach (var amp in domain)
            {
                CarrierAmplifierDecision? decision = DecideAmplifier(
                    amp,
                    sectionsByAmp,
                    sectionToBlock,
                    blockOwner,
                    sectionOwner,
                    blockVerdicts,
                    manualBlockedSections,
                    physicalBySection,
                    physicalFootprint,
                    containedDccs,
                    desiredLocoStates,
                    locoOccupiedBlock,
                    locoReservedBlocks,
                    diagnostics);

                if (decision is not null)
                {
                    decisions[amp] = decision;
                }
            }

            // 8) Physical source hold (two-stage release, Stage 1 deassert already applied upstream):
            //    a previously-driven target whose section is no longer in any locomotive's logical set
            //    stays DRIVEN at the previous owner's retained speed while it is still physically
            //    occupied, or is filtered Clear but within the post-clear tail hold, or has gone stale.
            if (previousTargets is not null)
            {
                foreach (var (amp, (prevOwner, _)) in previousTargets)
                {
                    if (HasCurrentOwner(amp, sectionsByAmp, sectionOwner))
                    {
                        continue;
                    }

                    if (decisions.TryGetValue(amp, out var existing))
                    {
                        if (existing.Verdict == CarrierVerdict.Conflict)
                        {
                            continue;
                        }

                        if (existing.Verdict == CarrierVerdict.Revoke &&
                            existing.Reason == CarrierReason.ReservedNotExpectedRevoke)
                        {
                            continue;
                        }
                    }

                    if (!desiredLocoStates.TryGetValue(prevOwner, out var prevDesired) || prevDesired.Speed <= 0)
                    {
                        decisions[amp] = new CarrierAmplifierDecision(
                            amp, prevOwner, Neutral, CarrierVerdict.DepartingPhysicalStop,
                            CarrierReason.DepartingSourceStopped, "departed-stop");
                        continue;
                    }

                    if (containedDccs.Contains(prevOwner))
                    {
                        decisions[amp] = new CarrierAmplifierDecision(
                            amp, prevOwner, Neutral, CarrierVerdict.DepartingPhysicalStop,
                            CarrierReason.PartialFootprintContain, "departed-contained");
                        continue;
                    }

                    if (!HoldsLogicalAuthority(prevOwner, locoOccupiedBlock, locoReservedBlocks))
                    {
                        decisions[amp] = new CarrierAmplifierDecision(
                            amp, prevOwner, Neutral, CarrierVerdict.DepartingPhysicalStop,
                            CarrierReason.DepartingSourceStopped, "departed-no-authority");
                        continue;
                    }

                    int holdHr0 = AmplifierSpeedMapper.ToPwm(prevDesired.Speed, prevDesired.Direction);
                    PhysicalOccupancy departedOcc = DepartedSourceOccupancy(amp, sectionsByAmp, physicalBySection);

                    decisions[amp] = departedOcc switch
                    {
                        PhysicalOccupancy.Occupied => new CarrierAmplifierDecision(
                            amp, prevOwner, holdHr0, CarrierVerdict.DepartingPhysicalHold,
                            CarrierReason.DepartedPhysicalHold, "departed-physical-hold"),

                        PhysicalOccupancy.Clear => new CarrierAmplifierDecision(
                            amp, prevOwner, holdHr0, CarrierVerdict.DepartingPhysicalHold,
                            CarrierReason.DepartedTailHold, "departed-tail-hold"),

                        _ => new CarrierAmplifierDecision(
                            amp, prevOwner, holdHr0, CarrierVerdict.DepartingPhysicalHold,
                            CarrierReason.DepartedSourceStale, "departed-source-stale"),
                    };
                }
            }

            return new CarrierDecision(
                OrderDecisions(decisions),
                LayoutStopRequired: false,
                AnyLocoStopRequired: locoStop.Count > 0,
                LatchedLocoIds: latched.OrderBy(x => x).ToArray(),
                Diagnostics: diagnostics,
                PhysicalFootprint: BuildFootprint(mapping, sectionOwner, physicalFootprint, previousTargets, physicalBySection));
        }

        /// <summary>Aggregated physical occupancy of a block's sections (Unknown dominates).</summary>
        private static PhysicalOccupancy BlockOccupancy(
            IReadOnlyList<int> sections,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection)
        {
            bool anyUnknown = false;
            bool anyOccupied = false;
            foreach (int section in sections)
            {
                if (!physicalBySection.TryGetValue(section, out PhysicalSectionState? state) ||
                    !state.IsFresh ||
                    state.Occupancy == PhysicalOccupancy.Unknown)
                {
                    anyUnknown = true;
                    continue;
                }

                if (state.Occupancy == PhysicalOccupancy.Occupied)
                {
                    anyOccupied = true;
                }
            }

            if (anyUnknown)
            {
                return PhysicalOccupancy.Unknown;
            }

            return anyOccupied ? PhysicalOccupancy.Occupied : PhysicalOccupancy.Clear;
        }

        private static CarrierAmplifierDecision? DecideAmplifier(
            int amp,
            IReadOnlyDictionary<int, List<int>> sectionsByAmp,
            IReadOnlyDictionary<int, int> sectionToBlock,
            IReadOnlyDictionary<int, int> blockOwner,
            IReadOnlyDictionary<int, int> sectionOwner,
            IReadOnlyDictionary<int, (CarrierVerdict Verdict, CarrierReason Reason, int Hr0, int? Owner)> blockVerdicts,
            HashSet<int> manualBlockedSections,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            IReadOnlyDictionary<int, int>? physicalFootprint,
            HashSet<int> containedDccs,
            IReadOnlyDictionary<int, (int Speed, int Direction)> desiredLocoStates,
            IReadOnlyDictionary<int, int?> locoOccupiedBlock,
            IReadOnlyDictionary<int, HashSet<int>> locoReservedBlocks,
            List<string> diagnostics)
        {
            if (!sectionsByAmp.TryGetValue(amp, out var sections) || sections.Count == 0)
            {
                return null; // amplifier exists only via previousTargets (handled later)
            }

            // Resolve the amplifier to its block (all of its sections must be in one block).
            var blocks = new SortedSet<int>();
            foreach (int section in sections)
            {
                if (sectionToBlock.TryGetValue(section, out int block))
                {
                    blocks.Add(block);
                }
            }

            // Mapping collision: one amplifier resolves to sections in more than one block.
            if (blocks.Count > 1)
            {
                return new CarrierAmplifierDecision(
                    amp, null, Neutral, CarrierVerdict.Conflict,
                    CarrierReason.MappingCollisionConflict, "mapping-collision");
            }

            // (b) Manual blocked -> Deny + Neutral (block-level: any bound section manual-blocked).
            if (manualBlockedSections.Overlaps(sections))
            {
                int? manualOwner = blocks.Count == 1 && blockOwner.TryGetValue(blocks.First(), out int mbOwner)
                    ? mbOwner
                    : (sections.Count == 1 && sectionOwner.TryGetValue(sections[0], out int so) ? so : null);
                return new CarrierAmplifierDecision(
                    amp, manualOwner, Neutral, CarrierVerdict.Deny, CarrierReason.ManualBlocked, "manual-blocked");
            }

            // Owned block -> use the cached block drive-group verdict.
            if (blocks.Count == 1 && blockOwner.TryGetValue(blocks.First(), out int dcc))
            {
                (CarrierVerdict Verdict, CarrierReason Reason, int Hr0, int? Owner) blockVerdict = blockVerdicts[blocks.First()];
                return new CarrierAmplifierDecision(
                    amp, dcc, blockVerdict.Hr0, blockVerdict.Verdict, blockVerdict.Reason,
                    Evidence(blockVerdict.Verdict, blockVerdict.Reason));
            }

            // Free / unbound section: per-section decision over the representative section.
            int sec = sections[0];

            // (c) Physical unknown/stale -> fail closed.
            PhysicalSectionState? phys = physicalBySection.TryGetValue(sec, out var p) ? p : null;
            PhysicalOccupancy occupancy = phys?.Occupancy ?? PhysicalOccupancy.Unknown;
            bool physicalFresh = phys is { IsFresh: true };

            if (!physicalFresh || occupancy == PhysicalOccupancy.Unknown)
            {
                if (physicalFootprint is not null && physicalFootprint.TryGetValue(sec, out int footprintStaleOwner))
                {
                    return new CarrierAmplifierDecision(
                        amp, footprintStaleOwner, Neutral, CarrierVerdict.DepartingPhysicalStop,
                        CarrierReason.DepartedSourceStale, "footprint-stale");
                }

                return new CarrierAmplifierDecision(
                    amp, null, Neutral, CarrierVerdict.Neutral, CarrierReason.PhysicalUnknownStale, "physical-unknown-stale");
            }

            // (d) No logical owner.
            if (!sectionOwner.TryGetValue(sec, out int freeOwner) &&
                (blocks.Count == 0 || !blockOwner.ContainsKey(blocks.First())))
            {
                if (physicalFootprint is not null && physicalFootprint.TryGetValue(sec, out int footprintOwner))
                {
                    if (occupancy == PhysicalOccupancy.Occupied)
                    {
                        if (containedDccs.Contains(footprintOwner))
                        {
                            diagnostics.Add(
                                $"Partial footprint containment: loco {footprintOwner} held neutral on " +
                                $"footprint section {sec} (neutral-only, no loco-stop escalation).");
                            return new CarrierAmplifierDecision(
                                amp, footprintOwner, Neutral, CarrierVerdict.DepartingPhysicalStop,
                                CarrierReason.PartialFootprintContain, "footprint-partial-contain");
                        }

                        if (desiredLocoStates.TryGetValue(footprintOwner, out var footprintDesired) &&
                            footprintDesired.Speed > 0 &&
                            HoldsLogicalAuthority(footprintOwner, locoOccupiedBlock, locoReservedBlocks))
                        {
                            int footprintHr0 = AmplifierSpeedMapper.ToPwm(footprintDesired.Speed, footprintDesired.Direction);
                            return new CarrierAmplifierDecision(
                                amp, footprintOwner, footprintHr0, CarrierVerdict.DepartingPhysicalHold,
                                CarrierReason.DepartedPhysicalHold, "footprint-occupied-driven");
                        }

                        return new CarrierAmplifierDecision(
                            amp, footprintOwner, Neutral, CarrierVerdict.DepartingPhysicalStop,
                            CarrierReason.DepartingSourceStopped, "footprint-occupied");
                    }

                    return new CarrierAmplifierDecision(
                        amp, footprintOwner, Neutral, CarrierVerdict.DepartingPhysicalStop,
                        CarrierReason.DepartedTailHold, "footprint-clear");
                }

                if (occupancy == PhysicalOccupancy.Occupied)
                {
                    diagnostics.Add(
                        $"ContradictionPhysicalWithoutLogicalOwner: section {sec} (amp {amp}) is physically " +
                        "occupied with no logical owner; neutralizing locally, no layout stop.");
                    return new CarrierAmplifierDecision(
                        amp, null, Neutral, CarrierVerdict.Revoke,
                        CarrierReason.FreeOccupiedRevoke, "free-occupied");
                }

                return new CarrierAmplifierDecision(
                    amp, null, Neutral, CarrierVerdict.Deny, CarrierReason.FreeClearDeny, "free-clear");
            }

            // Defensive: a section with a logical owner but no block verdict (unmapped edge) is denied.
            return new CarrierAmplifierDecision(
                amp, null, Neutral, CarrierVerdict.Deny, CarrierReason.OtherLocoOwns, "other-loco-owns");
        }

        private static string Evidence(CarrierVerdict verdict, CarrierReason reason)
            => reason switch
            {
                CarrierReason.OccupiedConsistent => "occupied-consistent",
                CarrierReason.ReservedPreEnergize => "reserved-pre-energize",
                CarrierReason.ExpectedBoundaryLead => "reserved-expected-lead",
                CarrierReason.ReservedNotExpectedRevoke => "reserved-not-expected",
                CarrierReason.SpeedZeroNeutral => "speed-zero",
                CarrierReason.PartialFootprintContain => "partial-contain",
                CarrierReason.ManualBlocked => "manual-blocked",
                CarrierReason.PhysicalUnknownStale => "physical-unknown-stale",
                _ => "block-level",
            };

        private static bool IsExpectedBoundaryLead(
            int? occupiedBlock,
            int reservedBlock,
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockToSections,
            BlockTopology topology,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            TimeSpan confirmationWindow)
        {
            if (occupiedBlock is not int occ)
            {
                return false;
            }

            // A switch-free route occ -> reservedBlock must exist.
            if (!topology.TryGetNextBlocks(occ, new Dictionary<int, SwitchPosition>(), lookAheadOnly: false, out var nextBlocks) ||
                !nextBlocks.Contains(reservedBlock))
            {
                return false;
            }

            if (!blockToSections.TryGetValue(occ, out IReadOnlyList<int>? occSections) ||
                !IsBlockPhysicallyOccupiedFresh(occSections, physicalBySection))
            {
                return false;
            }

            if (!blockToSections.TryGetValue(reservedBlock, out IReadOnlyList<int>? resSections) ||
                !IsBlockPhysicallyOccupiedFresh(resSections, physicalBySection))
            {
                return false;
            }

            // Expiry: a boundary lead older than the confirmation window is no longer expected.
            return MaxBlockAge(resSections, physicalBySection) <= confirmationWindow;
        }

        private static bool IsBlockPhysicallyOccupiedFresh(
            IReadOnlyList<int> sections,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection)
        {
            bool anyOccupied = false;
            foreach (int section in sections)
            {
                if (!physicalBySection.TryGetValue(section, out PhysicalSectionState? state) ||
                    !state.IsFresh ||
                    state.Occupancy == PhysicalOccupancy.Unknown)
                {
                    return false;
                }

                if (state.Occupancy == PhysicalOccupancy.Occupied)
                {
                    anyOccupied = true;
                }
            }

            return anyOccupied;
        }

        private static TimeSpan MaxBlockAge(
            IReadOnlyList<int> sections,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection)
        {
            TimeSpan max = TimeSpan.Zero;
            foreach (int section in sections)
            {
                if (physicalBySection.TryGetValue(section, out PhysicalSectionState? state) &&
                    state.Occupancy == PhysicalOccupancy.Occupied &&
                    state.Age > max)
                {
                    max = state.Age;
                }
            }

            return max;
        }

        private static IReadOnlyDictionary<int, int> BuildFootprint(
            IReadOnlyList<LayoutPhysicalAmplifierBinding> mapping,
            IReadOnlyDictionary<int, int> sectionOwner,
            IReadOnlyDictionary<int, int>? physicalFootprint,
            IReadOnlyDictionary<int, (int OwnerDcc, int Hr0)>? previousTargets,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection)
        {
            var footprint = new Dictionary<int, int>();

            foreach (LayoutPhysicalAmplifierBinding binding in mapping)
            {
                int section = binding.SectionId;

                // Logical ownership is recomputed fresh every cycle, not remembered.
                if (sectionOwner.ContainsKey(section))
                {
                    continue;
                }

                if (physicalFootprint is not null &&
                    physicalFootprint.TryGetValue(section, out int retainedOwner))
                {
                    footprint[section] = retainedOwner;
                    continue;
                }

                if (previousTargets is not null &&
                    previousTargets.TryGetValue(binding.PhysicalAmplifier, out var prev) &&
                    IsPhysicallyPresent(physicalBySection, section))
                {
                    footprint[section] = prev.OwnerDcc;
                }
            }

            return footprint;
        }

        private static bool IsPhysicallyPresent(
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            int section)
        {
            if (!physicalBySection.TryGetValue(section, out PhysicalSectionState? state))
            {
                return true;
            }

            if (!state.IsFresh || state.Occupancy == PhysicalOccupancy.Unknown)
            {
                return true;
            }

            return state.Occupancy == PhysicalOccupancy.Occupied;
        }

        private static bool HoldsLogicalAuthority(
            int dcc,
            IReadOnlyDictionary<int, int?> locoOccupiedBlock,
            IReadOnlyDictionary<int, HashSet<int>> locoReservedBlocks)
        {
            if (locoOccupiedBlock.TryGetValue(dcc, out var occ) && occ is not null) return true;
            return locoReservedBlocks.TryGetValue(dcc, out var reserved) && reserved.Count > 0;
        }

        private static IReadOnlyList<CarrierAmplifierDecision> OrderDecisions(
            IReadOnlyDictionary<int, CarrierAmplifierDecision> decisions)
            => decisions.Values.OrderBy(d => d.Amplifier).ToArray();

        private static bool HasCurrentOwner(
            int amp,
            IReadOnlyDictionary<int, List<int>> sectionsByAmp,
            IReadOnlyDictionary<int, int> sectionOwner)
        {
            if (!sectionsByAmp.TryGetValue(amp, out var sections))
            {
                return false;
            }

            return sections.Any(s => sectionOwner.ContainsKey(s));
        }

        private static PhysicalOccupancy DepartedSourceOccupancy(
            int amp,
            IReadOnlyDictionary<int, List<int>> sectionsByAmp,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection)
        {
            if (!sectionsByAmp.TryGetValue(amp, out var sections) || sections.Count == 0)
            {
                return PhysicalOccupancy.Unknown;
            }

            bool anyUnknown = false;
            bool anyOccupied = false;

            foreach (var section in sections)
            {
                if (!physicalBySection.TryGetValue(section, out var state) ||
                    !state.IsFresh ||
                    state.Occupancy == PhysicalOccupancy.Unknown)
                {
                    anyUnknown = true;
                    continue;
                }

                if (state.Occupancy == PhysicalOccupancy.Occupied)
                {
                    anyOccupied = true;
                }
            }

            if (anyUnknown)
            {
                return PhysicalOccupancy.Unknown;
            }

            return anyOccupied ? PhysicalOccupancy.Occupied : PhysicalOccupancy.Clear;
        }

        private static HashSet<int> DetectContainedLocos(
            IReadOnlyDictionary<int, int> sectionOwner,
            IReadOnlyDictionary<int, HashSet<int>> locoReservedSections,
            IReadOnlyDictionary<int, int?> locoOccupiedBlock,
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockToSections,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            IReadOnlyDictionary<int, int> sectionToAmp,
            IReadOnlyDictionary<int, List<int>> sectionAdjacency,
            IReadOnlyDictionary<int, int>? physicalFootprint,
            IReadOnlyDictionary<int, (int OwnerDcc, int Hr0)>? previousTargets)
        {
            var contained = new HashSet<int>();

            DetectRequiredFootprintContain(
                locoReservedSections, locoOccupiedBlock, blockToSections, physicalBySection,
                sectionToAmp, physicalFootprint, contained);

            foreach (var (dcc, occupiedBlock) in locoOccupiedBlock)
            {
                if (occupiedBlock is not int sourceBlock ||
                    !blockToSections.TryGetValue(sourceBlock, out IReadOnlyList<int>? sourceSections))
                {
                    continue;
                }

                // Anchor: the occupied block's fresh, physically occupied, mapped sections.
                var anchor = new List<int>();
                bool anchorInvalid = false;
                foreach (int section in sourceSections)
                {
                    if (!IsPhysicallyOccupiedFresh(physicalBySection, section))
                    {
                        continue;
                    }

                    if (!sectionToAmp.ContainsKey(section))
                    {
                        anchorInvalid = true;
                        break;
                    }

                    anchor.Add(section);
                }

                if (anchorInvalid || anchor.Count == 0)
                {
                    continue;
                }

                var visited = new HashSet<int>(anchor);
                var queue = new Queue<int>(anchor);

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
                            !IsPhysicallyOccupiedFresh(physicalBySection, candidate))
                        {
                            continue;
                        }

                        if (sectionOwner.TryGetValue(candidate, out int candidateOwner) &&
                            candidateOwner != dcc)
                        {
                            continue;
                        }

                        if (ReservedByOther(dcc, candidate, locoReservedSections))
                        {
                            continue;
                        }

                        visited.Add(candidate);

                        if (IsEntitledTo(dcc, candidate, sectionOwner, locoReservedSections, physicalFootprint, previousTargets, sectionToAmp))
                        {
                            queue.Enqueue(candidate);
                        }
                        else
                        {
                            contained.Add(dcc);
                        }
                    }
                }
            }

            return contained;
        }

        private static void DetectRequiredFootprintContain(
            IReadOnlyDictionary<int, HashSet<int>> locoReservedSections,
            IReadOnlyDictionary<int, int?> locoOccupiedBlock,
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockToSections,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            IReadOnlyDictionary<int, int> sectionToAmp,
            IReadOnlyDictionary<int, int>? physicalFootprint,
            HashSet<int> contained)
        {
            foreach (var (dcc, reserved) in locoReservedSections)
            {
                if (!HoldsLogicalAuthority(dcc, locoOccupiedBlock, locoReservedSections))
                {
                    continue;
                }

                foreach (int section in reserved)
                {
                    if (!IsResolvableFresh(sectionToAmp, physicalBySection, section))
                    {
                        contained.Add(dcc);
                        break;
                    }
                }

                if (physicalFootprint is not null)
                {
                    foreach (var (section, owner) in physicalFootprint)
                    {
                        if (owner == dcc && !IsResolvableFresh(sectionToAmp, physicalBySection, section))
                        {
                            contained.Add(dcc);
                            break;
                        }
                    }
                }
            }
        }

        private static bool IsResolvableFresh(
            IReadOnlyDictionary<int, int> sectionToAmp,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            int section)
            => sectionToAmp.ContainsKey(section) &&
               physicalBySection.TryGetValue(section, out PhysicalSectionState? state) &&
               state.IsFresh &&
               state.Occupancy != PhysicalOccupancy.Unknown;

        private static bool IsEntitledTo(
            int dcc,
            int section,
            IReadOnlyDictionary<int, int> sectionOwner,
            IReadOnlyDictionary<int, HashSet<int>> locoReservedSections,
            IReadOnlyDictionary<int, int>? physicalFootprint,
            IReadOnlyDictionary<int, (int OwnerDcc, int Hr0)>? previousTargets,
            IReadOnlyDictionary<int, int> sectionToAmp)
        {
            if (sectionOwner.TryGetValue(section, out int owner) && owner == dcc)
            {
                return true;
            }

            if (locoReservedSections.TryGetValue(dcc, out var reserved) && reserved.Contains(section))
            {
                return true;
            }

            if (physicalFootprint is not null &&
                physicalFootprint.TryGetValue(section, out int footprintOwner) &&
                footprintOwner == dcc)
            {
                return true;
            }

            if (sectionToAmp.TryGetValue(section, out int amp) &&
                previousTargets is not null &&
                previousTargets.TryGetValue(amp, out var prev) &&
                prev.OwnerDcc == dcc)
            {
                return true;
            }

            return false;
        }

        private static bool ReservedByOther(
            int dcc,
            int section,
            IReadOnlyDictionary<int, HashSet<int>> locoReservedSections)
        {
            foreach (var (owner, reserved) in locoReservedSections)
            {
                if (owner != dcc && reserved.Contains(section))
                {
                    return true;
                }
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
