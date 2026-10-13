using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The PURE, STATELESS ownership-aware locomotive carrier projector (Phase 1). It reconciles the
    /// KIS-07 logical-section shadow (authority, only when <c>ShadowValid</c>) against the KIS-08
    /// physical section occupancy and the physical amplifier mapping, and emits one
    /// <see cref="CarrierAmplifierDecision"/> per amplifier. It performs no side effects, holds no
    /// static state, and does no I/O. It is not yet wired into the live control path (Phase 2).
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
            IReadOnlyDictionary<int, (int OwnerDcc, int Hr0)>? previousTargets,
            IReadOnlyDictionary<int, int>? physicalFootprint,
            TimeSpan confirmationWindow)
        {
            ArgumentNullException.ThrowIfNull(desiredLocoStates);
            ArgumentNullException.ThrowIfNull(identity);
            ArgumentNullException.ThrowIfNull(topology);
            ArgumentNullException.ThrowIfNull(mapping);

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

            // 2) Per-loco logical ownership: section -> owner dcc.
            var sectionOwner = new Dictionary<int, int>();
            var locoOccupied = new Dictionary<int, int?>();
            var locoReserved = new Dictionary<int, HashSet<int>>();

            foreach (var loco in shadow.Locomotives)
            {
                if (!identity.TryGetValue(loco.InternalLocomotiveId, out int dcc))
                {
                    diagnostics.Add(
                        $"KIS locomotive internal id {loco.InternalLocomotiveId} has no LocomotiveBinding; " +
                        "it contributes zero physical targets (fail closed).");
                    continue;
                }

                locoOccupied[dcc] = loco.OccupiedLogicalSection;
                locoReserved[dcc] = new HashSet<int>(loco.ReservedLogicalSections ?? Array.Empty<int>());

                // Occupied ownership is resolved first: an Occupied(B) section is never taken over
                // by a Reserved(A) section, regardless of shadow iteration order.
                if (loco.OccupiedLogicalSection is int occ)
                {
                    sectionOwner[occ] = dcc;
                }
            }

            // Reserved ownership fills only sections that no locomotive already occupies.
            foreach (var loco in shadow.Locomotives)
            {
                if (!identity.TryGetValue(loco.InternalLocomotiveId, out int dcc))
                {
                    continue;
                }

                foreach (var r in loco.ReservedLogicalSections ?? Array.Empty<int>())
                {
                    if (!sectionOwner.ContainsKey(r))
                    {
                        sectionOwner[r] = dcc;
                    }
                }
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

            // 4) Amp -> sections (generic: one section maps to one amp, several sections may map to one amp).
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

            // Section -> amplifier (one-to-one in the carrier model), used by the partial-footprint
            // containment detector to resolve a section to its amplifier for drive-history lookup.
            var sectionToAmp = new Dictionary<int, int>();
            foreach (var binding in mapping)
            {
                sectionToAmp[binding.SectionId] = binding.PhysicalAmplifier;
            }

            // Partial-footprint containment: a locomotive whose occupied anchor has an adjacent,
            // physically-Occupied section that is NOT safely attributable to it must not drive its
            // partial footprint forward (see requirement 7). Detected fresh every cycle from the
            // current shadow, physical occupancy, footprint residue and drive history.
            HashSet<int> containedDccs = DetectContainedLocos(
                sectionOwner,
                locoReserved,
                locoOccupied,
                physicalBySection,
                sectionToAmp,
                topology,
                physicalFootprint,
                previousTargets);

            var manualBlocked = new HashSet<int>(shadow.ManualBlockedSections ?? Array.Empty<int>());

            var locoStop = new HashSet<int>();
            var latched = new HashSet<int>();

            // 5) Per-amplifier decisions.
            foreach (var amp in domain)
            {
                CarrierAmplifierDecision? decision = DecideAmplifier(
                    amp,
                    sectionOwner,
                    locoOccupied,
                    locoReserved,
                    physicalBySection,
                    sectionsByAmp,
                    manualBlocked,
                    topology,
                    desiredLocoStates,
                    confirmationWindow,
                    physicalFootprint,
                    diagnostics,
                    locoStop,
                    latched,
                    containedDccs);

                if (decision is not null)
                {
                    decisions[amp] = decision;
                }
            }

            // 6) Physical source hold (two-stage release, Stage 1 deassert already applied upstream):
            //    a previously-driven target whose section is no longer in any locomotive's logical
            //    set stays DRIVEN at the previous owner's retained speed while it is still physically
            //    occupied, or is filtered Clear but within the post-clear tail hold, or has gone
            //    stale. KIS removing the section from the owner's logical set does NOT by itself
            //    neutralize it; only a speed-zero / safety stop releases it immediately here (the
            //    tail-hold timing is applied by the stateful controller, not this pure projector).
            if (previousTargets is not null)
            {
                foreach (var (amp, (prevOwner, _)) in previousTargets)
                {
                    // A previously-driven amp whose section now has a (possibly different) logical
                    // owner is a handover, not a departed source: the normal decision already applies
                    // and must not be overwritten by the previous owner's hold (explicit safe handover).
                    if (HasCurrentOwner(amp, sectionsByAmp, sectionOwner))
                    {
                        continue;
                    }

                    if (decisions.TryGetValue(amp, out var existing))
                    {
                        // A mapping collision is a hard contradiction and must not be overridden.
                        if (existing.Verdict == CarrierVerdict.Conflict)
                        {
                            continue;
                        }

                        // A reserved section that is unexpectedly occupied is an escalation (loco
                        // stop + latch), not a departed overlap; leave it as Revoke.
                        if (existing.Verdict == CarrierVerdict.Revoke &&
                            existing.Reason == CarrierReason.ReservedNotExpectedRevoke)
                        {
                            continue;
                        }
                    }

                    // Speed zero / safety stop means NON-NEUTRAL DRIVE MUST END; it does NOT mean the
                    // locomotive physically disappeared. The departed source stays attributed to its
                    // owner (neutral), so the next cycle does not reinterpret the same physical
                    // occupancy as an ownerless Free+Occupied intrusion.
                    if (!desiredLocoStates.TryGetValue(prevOwner, out var prevDesired) || prevDesired.Speed <= 0)
                    {
                        decisions[amp] = new CarrierAmplifierDecision(
                            amp, prevOwner, Neutral, CarrierVerdict.DepartingPhysicalStop,
                            CarrierReason.DepartingSourceStopped, "departed-stop");
                        continue;
                    }

                    // A contained locomotive (partial footprint) must not have its departed source
                    // re-driven by the hold; keep it neutral and attributed (no release, no drive).
                    if (containedDccs.Contains(prevOwner))
                    {
                        decisions[amp] = new CarrierAmplifierDecision(
                            amp, prevOwner, Neutral, CarrierVerdict.DepartingPhysicalStop,
                            CarrierReason.PartialFootprintContain, "departed-contained");
                        continue;
                    }

                    // Authority gate: a departed source must not be re-driven once its owner became
                    // logically Free (no occupied, no reserved), even with a stale retained Speed > 0.
                    if (!HoldsLogicalAuthority(prevOwner, locoOccupied, locoReserved))
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

        /// <summary>
        /// Derives the section -&gt; owner physical footprint RESIDUE to persist into the next cycle.
        /// Logical ownership (Occupied + Reserved) is recomputed fresh from the shadow every cycle and
        /// is therefore NOT stored here. The residue is only the stateful part: sections that are no
        /// longer logically owned but are still physically attributed to a locomotive — a departed
        /// source whose tail is still on the section, or a reconstructed extension at startup. This is
        /// what prevents a stopped spanning locomotive from being reclassified as an ownerless
        /// intrusion.
        /// </summary>
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

                // An existing residue section keeps its attribution (released later by the tail hold).
                if (physicalFootprint is not null &&
                    physicalFootprint.TryGetValue(section, out int retainedOwner))
                {
                    footprint[section] = retainedOwner;
                    continue;
                }

                // A previously-driven target whose section has just left its owner's logical set is a
                // newly-departed source: remember it as residue while it is still physically present
                // (occupied or stale/unknown), so a later speed-zero stop does not de-attribute it.
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
                return true; // unknown -> fail closed (keep attribution)
            }

            if (!state.IsFresh || state.Occupancy == PhysicalOccupancy.Unknown)
            {
                return true; // stale/unknown -> fail closed (keep attribution)
            }

            return state.Occupancy == PhysicalOccupancy.Occupied;
        }

        /// <summary>
        /// True when a locomotive still holds valid LOGICAL authority: it either occupies a logical
        /// section or reserves at least one. A locomotive that became logically Free (no occupied, no
        /// reserved) has no authority to drive, even if a stale retained <c>Speed &gt; 0</c> survives
        /// in the desired-loco states.
        /// </summary>
        private static bool HoldsLogicalAuthority(
            int dcc,
            IReadOnlyDictionary<int, int?> locoOccupied,
            IReadOnlyDictionary<int, HashSet<int>> locoReserved)
        {
            if (locoOccupied.TryGetValue(dcc, out var occ) && occ is not null) return true;
            return locoReserved.TryGetValue(dcc, out var reserved) && reserved.Count > 0;
        }

        private static CarrierAmplifierDecision? DecideAmplifier(
            int amp,
            IReadOnlyDictionary<int, int> sectionOwner,
            IReadOnlyDictionary<int, int?> locoOccupied,
            IReadOnlyDictionary<int, HashSet<int>> locoReserved,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            IReadOnlyDictionary<int, List<int>> sectionsByAmp,
            HashSet<int> manualBlocked,
            BlockTopology topology,
            IReadOnlyDictionary<int, (int Speed, int Direction)> desiredLocoStates,
            TimeSpan confirmationWindow,
            IReadOnlyDictionary<int, int>? physicalFootprint,
            List<string> diagnostics,
            HashSet<int> locoStop,
            HashSet<int> latched,
            HashSet<int> containedDccs)
        {
            // (a) Mapping collision: multiple sections with different owners -> Conflict.
            if (sectionsByAmp.TryGetValue(amp, out var sections) && sections.Count > 1)
            {
                var owners = sections
                    .Select(s => sectionOwner.TryGetValue(s, out var o) ? (int?)o : null)
                    .Distinct()
                    .ToList();

                if (owners.Count > 1)
                {
                    return new CarrierAmplifierDecision(
                        amp, null, Neutral, CarrierVerdict.Conflict,
                        CarrierReason.MappingCollisionConflict, "mapping-collision");
                }
            }

            // An amplifier with no mapped section exists only via previousTargets (handled later).
            int? section = sections is { Count: > 0 } ? sections[0] : null;
            if (section is not int sec)
            {
                return null;
            }

            // (b) Manual blocked -> Deny + Neutral.
            if (manualBlocked.Contains(sec))
            {
                return new CarrierAmplifierDecision(
                    amp, sectionOwner.TryGetValue(sec, out var mbOwner) ? mbOwner : null, Neutral,
                    CarrierVerdict.Deny, CarrierReason.ManualBlocked, "manual-blocked");
            }

            // (c) Physical unknown/stale -> fail closed. A footprint-attributed section keeps its
            // attribution (neutral) even when its telemetry is stale, so a spanning locomotive is not
            // silently de-attributed.
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
                    amp, sectionOwner.TryGetValue(sec, out var staleOwner) ? staleOwner : null, Neutral,
                    CarrierVerdict.Neutral, CarrierReason.PhysicalUnknownStale, "physical-unknown-stale");
            }

            // (d) No logical owner.
            if (!sectionOwner.TryGetValue(sec, out int dcc))
            {
                // A section still physically attributed to a locomotive (departed source or a
                // reconstructed extension) is NOT an ownerless intrusion. Its drive state now depends
                // on the footprint owner's CURRENT retained speed (so a reconstructed startup
                // extension is driven on the first valid speed command, and a speed-zero stop
                // neutralizes it while keeping the attribution). It is released through the normal
                // two-stage physical release (deassert filter + tail hold) once it actually clears.
                if (physicalFootprint is not null && physicalFootprint.TryGetValue(sec, out int footprintOwner))
                {
                    if (occupancy == PhysicalOccupancy.Occupied)
                    {
                        // A contained locomotive (partial footprint) must not drive its footprint
                        // extension forward: neutral, attributed, escalated locally.
                        if (containedDccs.Contains(footprintOwner))
                        {
                            diagnostics.Add(
                                $"Partial footprint containment: loco {footprintOwner} held neutral on " +
                                $"footprint section {sec} (neutral-only, no loco-stop escalation).");
                            return new CarrierAmplifierDecision(
                                amp, footprintOwner, Neutral, CarrierVerdict.DepartingPhysicalStop,
                                CarrierReason.PartialFootprintContain, "footprint-partial-contain");
                        }

                        // The footprint owner's current retained speed drives the extension, but only
                        // while the owner still holds valid logical authority. A logically-Free loco
                        // with a stale retained speed must not re-drive its footprint residue.
                        if (desiredLocoStates.TryGetValue(footprintOwner, out var footprintDesired) &&
                            footprintDesired.Speed > 0 &&
                            HoldsLogicalAuthority(footprintOwner, locoOccupied, locoReserved))
                        {
                            int footprintHr0 = AmplifierSpeedMapper.ToPwm(
                                footprintDesired.Speed, footprintDesired.Direction);
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

            // (e) Owned by dcc.
            int? occupiedSection = locoOccupied.TryGetValue(dcc, out var occ) ? occ : null;
            bool isOccupied = occupiedSection == sec;
            bool isReserved = locoReserved.TryGetValue(dcc, out var reserved) && reserved.Contains(sec);

            (int Speed, int Direction) desired = desiredLocoStates.TryGetValue(dcc, out var d)
                ? d
                : (0, 1);

            // Speed 0 -> all targets neutral.
            if (desired.Speed <= 0)
            {
                return new CarrierAmplifierDecision(
                    amp, dcc, Neutral, CarrierVerdict.Neutral, CarrierReason.SpeedZeroNeutral, "speed-zero");
            }

            int hr0 = AmplifierSpeedMapper.ToPwm(desired.Speed, desired.Direction);

            if (isOccupied)
            {
                if (occupancy == PhysicalOccupancy.Occupied)
                {
                    // A contained locomotive (partial footprint) must not drive its occupied anchor
                    // forward: neutral, attributed, escalated locally (no layout stop, no latch).
                    if (containedDccs.Contains(dcc))
                    {
                        diagnostics.Add(
                            $"Partial footprint containment: loco {dcc} held neutral on occupied " +
                            $"section {sec} (neutral-only, no loco-stop escalation).");
                        return new CarrierAmplifierDecision(
                            amp, dcc, Neutral, CarrierVerdict.Revoke,
                            CarrierReason.PartialFootprintContain, "occupied-partial-contain");
                    }

                    return new CarrierAmplifierDecision(
                        amp, dcc, hr0, CarrierVerdict.Drive, CarrierReason.OccupiedConsistent,
                        "occupied-consistent");
                }

                // Occupied(A) + Clear -> Release (the loco left its occupied section).
                return new CarrierAmplifierDecision(
                    amp, dcc, Neutral, CarrierVerdict.Release, CarrierReason.OccupiedClearRelease,
                    "occupied-clear-release");
            }

            if (isReserved)
            {
                if (occupancy == PhysicalOccupancy.Clear)
                {
                    // A contained locomotive (partial footprint) must not pre-energize its reserved
                    // target forward: neutral, attributed (neutral-only, no loco-stop escalation).
                    if (containedDccs.Contains(dcc))
                    {
                        return new CarrierAmplifierDecision(
                            amp, dcc, Neutral, CarrierVerdict.Neutral,
                            CarrierReason.PartialFootprintContain, "reserved-contained");
                    }

                    return new CarrierAmplifierDecision(
                        amp, dcc, hr0, CarrierVerdict.Drive, CarrierReason.ReservedPreEnergize,
                        "reserved-pre-energize");
                }

                // Reserved(A) + Occupied: expected boundary lead, or revoke + loco-stop + latch.
                if (IsExpectedBoundaryLead(occupiedSection, sec, topology, physicalBySection, confirmationWindow))
                {
                    // A contained locomotive (partial footprint) must not drive its expected boundary
                    // lead forward: neutral, attributed (neutral-only, no loco-stop escalation).
                    if (containedDccs.Contains(dcc))
                    {
                        return new CarrierAmplifierDecision(
                            amp, dcc, Neutral, CarrierVerdict.Neutral,
                            CarrierReason.PartialFootprintContain, "boundary-lead-contained");
                    }

                    return new CarrierAmplifierDecision(
                        amp, dcc, hr0, CarrierVerdict.Drive, CarrierReason.ExpectedBoundaryLead,
                        "reserved-expected-lead");
                }

                locoStop.Add(dcc);
                latched.Add(dcc);
                return new CarrierAmplifierDecision(
                    amp, dcc, Neutral, CarrierVerdict.Revoke, CarrierReason.ReservedNotExpectedRevoke,
                    "reserved-not-expected");
            }

            // Defensive: owned but neither occupied nor reserved for dcc.
            return new CarrierAmplifierDecision(
                amp, dcc, Neutral, CarrierVerdict.Deny, CarrierReason.OtherLocoOwns, "other-loco-owns");
        }

        private static bool IsExpectedBoundaryLead(
            int? occupiedSection,
            int reservedSection,
            BlockTopology topology,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            TimeSpan confirmationWindow)
        {
            if (occupiedSection is not int occ)
            {
                return false;
            }

            // A switch-free route S -> T must exist. Switch-gated transitions are conservatively
            // treated as NOT expected because no switch position is supplied to this pure projector.
            if (!topology.TryGetNextBlocks(
                    occ,
                    new Dictionary<int, SwitchPosition>(),
                    lookAheadOnly: false,
                    out var nextBlocks) ||
                !nextBlocks.Contains(reservedSection))
            {
                return false;
            }

            if (!physicalBySection.TryGetValue(occ, out var occState) ||
                occState.Occupancy != PhysicalOccupancy.Occupied ||
                !occState.IsFresh)
            {
                return false;
            }

            if (!physicalBySection.TryGetValue(reservedSection, out var resState) ||
                resState.Occupancy != PhysicalOccupancy.Occupied ||
                !resState.IsFresh)
            {
                return false;
            }

            // Expiry: a boundary lead older than the confirmation window is no longer expected.
            return resState.Age <= confirmationWindow;
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

        /// <summary>
        /// The filtered physical occupancy of a departed source's mapped section(s): Occupied when any
        /// mapped section is fresh + Occupied, Clear when every mapped section is fresh + Clear, and
        /// Unknown (fail closed) when any mapped section is missing, not fresh, or Unknown.
        /// </summary>
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

        /// <summary>
        /// Detects locomotives that must NOT drive a PARTIAL footprint forward, so the caller commands
        /// them neutral. Two independent fail-closed triggers add a locomotive to the contained set:
        /// <list type="number">
        /// <item>
        /// (Product Owner Decision B2) A REQUIRED section — one the locomotive actively reserves
        /// (<c>Reserved(A)</c>) or one retained in its physical footprint residue — is physically
        /// stale/Unknown or its physical amplifier mapping is missing/invalid. This blocks a PARTIAL
        /// UNSAFE FIRST DEPARTURE: the locomotive may not drive its known subset while one of its own
        /// required sections is unresolved.
        /// </item>
        /// <item>
        /// (requirement 7) The occupied anchor's connected physically-Occupied component extends into a
        /// fresh, mapped, physically-Occupied section that no rule safely attributes to the locomotive
        /// (competing ownership / ambiguity).
        /// </item>
        /// </list>
        /// A section owned or reserved by a DIFFERENT locomotive remains a HARD BOUNDARY (Decision A):
        /// it stops the flood but is never itself a containment trigger. An unrelated stale/Unknown or
        /// unmapped branch that is neither <c>Reserved(A)</c> nor in A's retained footprint is local
        /// fail-closed neutrality (Decision B1) and does NOT contain A.
        /// </summary>
        private static HashSet<int> DetectContainedLocos(
            IReadOnlyDictionary<int, int> sectionOwner,
            IReadOnlyDictionary<int, HashSet<int>> locoReserved,
            IReadOnlyDictionary<int, int?> locoOccupied,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            IReadOnlyDictionary<int, int> sectionToAmp,
            BlockTopology topology,
            IReadOnlyDictionary<int, int>? physicalFootprint,
            IReadOnlyDictionary<int, (int OwnerDcc, int Hr0)>? previousTargets)
        {
            var contained = new HashSet<int>();

            // (Decision B2) A locomotive whose OWN required section (Reserved(A) or a retained-footprint
            // section) is stale/Unknown or unmapped must not depart with a partial footprint.
            DetectRequiredFootprintContain(
                locoReserved, locoOccupied, physicalBySection, sectionToAmp, physicalFootprint, contained);

            var noSwitches = new Dictionary<int, SwitchPosition>();

            foreach (var (dcc, occupiedSection) in locoOccupied)
            {
                if (occupiedSection is not int source)
                {
                    continue;
                }

                // A valid anchor must be mapped, fresh and physically occupied.
                if (!sectionToAmp.ContainsKey(source) ||
                    !IsPhysicallyOccupiedFresh(physicalBySection, source))
                {
                    continue;
                }

                // Flood-fill the physically-Occupied connected component around the anchor. Any
                // reached section that is not entitled to this locomotive marks the footprint partial.
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

                        if (!AreAdjacent(topology, current, candidate, noSwitches))
                        {
                            continue;
                        }

                        if (!sectionToAmp.ContainsKey(candidate) ||
                            !IsPhysicallyOccupiedFresh(physicalBySection, candidate))
                        {
                            continue;
                        }

                        // A candidate cleanly owned or reserved by a DIFFERENT locomotive is that other
                        // train's territory, not a partial-footprint trigger for this locomotive: stop
                        // the flood here (mirror PhysicalFootprintReconstructor's stop-at-other-owner).
                        if (sectionOwner.TryGetValue(candidate, out int candidateOwner) &&
                            candidateOwner != dcc)
                        {
                            continue;
                        }

                        if (ReservedByOther(dcc, candidate, locoReserved))
                        {
                            continue;
                        }

                        visited.Add(candidate);

                        if (IsEntitledTo(
                                dcc, candidate, sectionOwner, locoReserved, physicalFootprint,
                                previousTargets, sectionToAmp))
                        {
                            queue.Enqueue(candidate);
                        }
                        else
                        {
                            // Occupied but not attributable: partial footprint -> contain this loco.
                            contained.Add(dcc);
                        }
                    }
                }
            }

            return contained;
        }

        /// <summary>
        /// (Product Owner Decision B2) A locomotive holding logical authority must not depart with a
        /// PARTIAL footprint: if any section it REQUIRES — one it actively reserves (<c>Reserved(A)</c>)
        /// or one retained in its physical footprint residue — is physically stale/Unknown OR its
        /// physical amplifier mapping is missing/invalid, the locomotive is contained. Its drive-eligible
        /// targets are then commanded neutral with reason <see cref="CarrierReason.PartialFootprintContain"/>
        /// (neutral-only: no layout stop, no latch, no loco-stop). A stale/Unknown/unmapped section that
        /// is NOT required by the locomotive (an unrelated topology-adjacent branch) is NOT a trigger
        /// (Decision B1) and stays local fail-closed.
        /// </summary>
        private static void DetectRequiredFootprintContain(
            IReadOnlyDictionary<int, HashSet<int>> locoReserved,
            IReadOnlyDictionary<int, int?> locoOccupied,
            IReadOnlyDictionary<int, PhysicalSectionState> physicalBySection,
            IReadOnlyDictionary<int, int> sectionToAmp,
            IReadOnlyDictionary<int, int>? physicalFootprint,
            HashSet<int> contained)
        {
            foreach (var (dcc, reserved) in locoReserved)
            {
                if (!HoldsLogicalAuthority(dcc, locoOccupied, locoReserved))
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

        /// <summary>
        /// True when a section is mapped to a physical amplifier AND has a fresh, non-Unknown physical
        /// occupancy observation, i.e. it is resolved enough to be safely attributable and driven. A
        /// missing mapping or a missing/stale/Unknown physical observation leaves it unresolved (fail
        /// closed).
        /// </summary>
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
            IReadOnlyDictionary<int, HashSet<int>> locoReserved,
            IReadOnlyDictionary<int, int>? physicalFootprint,
            IReadOnlyDictionary<int, (int OwnerDcc, int Hr0)>? previousTargets,
            IReadOnlyDictionary<int, int> sectionToAmp)
        {
            // Logically occupied by the locomotive.
            if (sectionOwner.TryGetValue(section, out int owner) && owner == dcc)
            {
                return true;
            }

            // Actively reserved by the locomotive (even when another locomotive occupies it — the
            // reservation boundary is handled by the normal boundary-lead / exclusivity policy, not
            // by partial-footprint containment).
            if (locoReserved.TryGetValue(dcc, out var reserved) && reserved.Contains(section))
            {
                return true;
            }

            // Attributed by the physical footprint residue.
            if (physicalFootprint is not null &&
                physicalFootprint.TryGetValue(section, out int footprintOwner) &&
                footprintOwner == dcc)
            {
                return true;
            }

            // A departed source still held in the drive history.
            if (sectionToAmp.TryGetValue(section, out int amp) &&
                previousTargets is not null &&
                previousTargets.TryGetValue(amp, out var prev) &&
                prev.OwnerDcc == dcc)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// True when a section is reserved by a locomotive OTHER than <paramref name="dcc"/>. Such a
        /// section is a reservation boundary, not a partial-footprint trigger for <paramref name="dcc"/>.
        /// </summary>
        private static bool ReservedByOther(
            int dcc,
            int section,
            IReadOnlyDictionary<int, HashSet<int>> locoReserved)
        {
            foreach (var (owner, reserved) in locoReserved)
            {
                if (owner != dcc && reserved.Contains(section))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AreAdjacent(
            BlockTopology topology,
            int a,
            int b,
            IReadOnlyDictionary<int, SwitchPosition> switchPositions)
        {
            if (topology.TryGetNextBlocks(a, switchPositions, lookAheadOnly: false, out var next) &&
                next.Contains(b))
            {
                return true;
            }

            if (topology.TryGetNextBlocks(b, switchPositions, lookAheadOnly: false, out var prev) &&
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
