using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Deterministic, offline tests for the pure <see cref="LocomotiveCarrierReconciliation"/>
    /// projector (Phase 1). No hardware, no writer: only the projection from the KIS-07 shadow,
    /// the KIS-08 physical occupancy, the topology and the physical mapping into per-amplifier
    /// carrier decisions.
    /// </summary>
    public class LocomotiveCarrierReconciliationTests
    {
        private const int Neutral = AmplifierSpeedMapper.NeutralPwm;
        private static readonly TimeSpan Window = TimeSpan.FromSeconds(5);

        private static BlockTopology Topology(string text) => BlockTopology.Parse(text);

        private static IReadOnlyList<LayoutPhysicalAmplifierBinding> Mapping(params (int Section, int Amp)[] pairs)
            => pairs.Select(p => new LayoutPhysicalAmplifierBinding { SectionId = p.Section, PhysicalAmplifier = p.Amp }).ToArray();

        private static PhysicalSectionOccupancyObservation Physical(params (int Section, PhysicalOccupancy Occ)[] states)
            => ReconciliationTestFixtures.Physical(
                states.Select(s => ReconciliationTestFixtures.PhysicalState(s.Section, s.Occ)).ToArray());

        private static PhysicalSectionOccupancyObservation PhysicalStale(int section)
            => ReconciliationTestFixtures.Physical(
                new[] { ReconciliationTestFixtures.PhysicalState(section, PhysicalOccupancy.Unknown, isFresh: false) });

        private static LogicalLocomotiveShadow Loco(int id, int? occupied, params int[] reserved)
            => ReconciliationTestFixtures.Loco(id, occupied, reserved);

        private static KoploperLogicalSectionShadowObservation Shadow(params LogicalLocomotiveShadow[] locos)
            => ReconciliationTestFixtures.Shadow(locomotives: locos, profileId: "Simple Loop");

        private static CarrierDecision Reconcile(
            Dictionary<int, (int Speed, int Direction)> desired,
            Dictionary<int, int> identity,
            KoploperLogicalSectionShadowObservation? shadow,
            PhysicalSectionOccupancyObservation? physical,
            BlockTopology topology,
            IReadOnlyList<LayoutPhysicalAmplifierBinding> mapping,
            IReadOnlyDictionary<int, IReadOnlyList<int>>? blockToSections = null,
            Dictionary<int, (int OwnerDcc, int Hr0)>? previousTargets = null,
            Dictionary<int, int>? physicalFootprint = null)
            => LocomotiveCarrierReconciliation.Reconcile(
                desired, identity, shadow, physical, topology, mapping,
                blockToSections ?? ReconciliationTestFixtures.BlockMap(mapping.Select(b => b.SectionId).ToArray()),
                previousTargets, physicalFootprint, Window);

        private static CarrierAmplifierDecision Decision(CarrierDecision decision, int amp)
            => Assert.Single(decision.Amplifiers, d => d.Amplifier == amp);

        // -----------------------------------------------------------------
        // 1) Simple Loop
        // -----------------------------------------------------------------

        [Fact]
        public void SimpleLoop_OccupiedAndReserved_DriveCurrentAndLookAhead()
        {
            var mapping = Mapping((1, 1), (2, 3), (3, 4), (4, 6));
            var topology = Topology("amps: 1:1,2:3,3:4,4:6 ; routes: 1>2,2>3,3>4,4>1");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear), (3, PhysicalOccupancy.Clear), (4, PhysicalOccupancy.Clear)),
                topology,
                mapping);

            Assert.False(decision.LayoutStopRequired);
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 1).Verdict);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), Decision(decision, 1).Hr0);
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 3).Verdict); // pre-energize
            Assert.Equal(Neutral, Decision(decision, 4).Hr0);
            Assert.Equal(Neutral, Decision(decision, 6).Hr0);
        }

        // -----------------------------------------------------------------
        // 2) Boundary lead
        // -----------------------------------------------------------------

        [Fact]
        public void BoundaryLead_ReservedBecomesOccupied_ExpectedTransition_DriveBoth()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping);

            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 1).Verdict);
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 3).Verdict);
            Assert.Equal(CarrierReason.ExpectedBoundaryLead, Decision(decision, 3).Reason);
        }

        // -----------------------------------------------------------------
        // 3) Departure release
        // -----------------------------------------------------------------

        [Fact]
        public void DepartureRelease_PreviouslyDrivenAmp_FilteredClearHeldForTail()
        {
            var mapping = Mapping((1, 1), (2, 3), (3, 4));
            var topology = Topology("amps: 1:1,2:3,3:4 ; routes: 1>2,2>3");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 2, 3)),
                Physical((1, PhysicalOccupancy.Clear), (2, PhysicalOccupancy.Occupied), (3, PhysicalOccupancy.Clear)),
                topology,
                mapping,
                previousTargets: new Dictionary<int, (int, int)> { [1] = (1, 650), [3] = (1, 650) });

            // Departed amp1 is filtered Clear but still held driven (tail hold); the pure projector
            // does NOT neutralize it (the tail timing is a stateful controller concern).
            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, Decision(decision, 1).Verdict);
            Assert.Equal(CarrierReason.DepartedTailHold, Decision(decision, 1).Reason);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), Decision(decision, 1).Hr0);
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 3).Verdict); // occupied section 2
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 4).Verdict); // reserved section 3
        }

        // -----------------------------------------------------------------
        // 4) Generic non-sequential ids
        // -----------------------------------------------------------------

        [Fact]
        public void GenericNonSequentialIds_AreIdIndependent()
        {
            var mapping = Mapping((10, 7), (20, 11), (30, 13));
            var topology = Topology("amps: 10:7,20:11,30:13 ; routes: 10>20,20>30");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 10, 20)),
                Physical((10, PhysicalOccupancy.Occupied), (20, PhysicalOccupancy.Clear), (30, PhysicalOccupancy.Clear)),
                topology,
                mapping);

            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 7).Verdict);
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 11).Verdict);
            Assert.Equal(CarrierVerdict.Deny, Decision(decision, 13).Verdict);
        }

        // -----------------------------------------------------------------
        // 5) Branch
        // -----------------------------------------------------------------

        [Fact]
        public void Branch_ReservedBranchDrives_UnreservedBranchDoesNot()
        {
            var mapping = Mapping((1, 1), (2, 3), (3, 4));
            var topology = Topology("amps: 1:1,2:3,3:4 ; routes: 1>2,1>3");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1, 3)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear), (3, PhysicalOccupancy.Clear)),
                topology,
                mapping);

            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 1).Verdict);
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 4).Verdict); // reserved branch C
            Assert.Equal(CarrierVerdict.Deny, Decision(decision, 3).Verdict);  // reachable-but-unreserved B
        }

        // -----------------------------------------------------------------
        // 6) Cross-loco exclusivity
        // -----------------------------------------------------------------

        [Fact]
        public void CrossLoco_OtherLocosReservedSection_IsNotDrivenByA()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0), [2] = (0, 0) },
                new Dictionary<int, int> { [10] = 1, [20] = 2 },
                Shadow(Loco(10, 1), Loco(20, null, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)),
                topology,
                mapping);

            Assert.Equal(1, Decision(decision, 1).OwnerDcc); // A drives section 1
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 1).Verdict);
            Assert.Equal(2, Decision(decision, 3).OwnerDcc); // section 2 belongs to B, not A
            Assert.NotEqual(CarrierVerdict.Drive, Decision(decision, 3).Verdict); // B has speed 0
        }

        // -----------------------------------------------------------------
        // 7) Multi-reserved + speed 0
        // -----------------------------------------------------------------

        [Fact]
        public void MultiReserved_AllDriven_ThenSpeedZero_AllNeutral()
        {
            var mapping = Mapping((1, 1), (2, 3), (3, 4), (4, 6));
            var topology = Topology("amps: 1:1,2:3,3:4,4:6 ; routes: 1>2,2>3,3>4,4>1");
            var physical = Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear), (3, PhysicalOccupancy.Clear), (4, PhysicalOccupancy.Clear));
            var identity = new Dictionary<int, int> { [10] = 1 };
            var shadow = Shadow(Loco(10, 1, 2, 3, 4));

            var drive = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) }, identity, shadow, physical, topology, mapping);
            Assert.Equal(new[] { 1, 3, 4, 6 }, drive.Amplifiers.Where(d => d.Verdict == CarrierVerdict.Drive).Select(d => d.Amplifier).ToArray());

            var stop = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (0, 0) }, identity, shadow, physical, topology, mapping);
            Assert.All(stop.Amplifiers, d => Assert.Equal(Neutral, d.Hr0));
            Assert.DoesNotContain(stop.Amplifiers, d => d.Verdict == CarrierVerdict.Drive);
        }

        // -----------------------------------------------------------------
        // 8) Free + Occupied (contradiction, no layout stop)
        // -----------------------------------------------------------------

        [Fact]
        public void FreeOccupied_LocalRevoke_NoLayoutStop()
        {
            var mapping = Mapping((1, 1));
            var topology = Topology("amps: 1:1");

            var decision = Reconcile(
                new Dictionary<int, (int, int)>(),
                new Dictionary<int, int>(),
                Shadow(), // no locomotives
                Physical((1, PhysicalOccupancy.Occupied)),
                topology,
                mapping);

            Assert.Equal(CarrierVerdict.Revoke, Decision(decision, 1).Verdict);
            Assert.Equal(Neutral, Decision(decision, 1).Hr0);
            Assert.False(decision.LayoutStopRequired);
            Assert.Contains(decision.Diagnostics, d => d.Contains("ContradictionPhysicalWithoutLogicalOwner"));
        }

        // -----------------------------------------------------------------
        // 9) Reserved + Occupied, NOT expected (loco-stop + latch, no layout stop)
        // -----------------------------------------------------------------

        [Fact]
        public void ReservedOccupied_NotExpected_NeutralAndLocoStopAndLatch()
        {
            var mapping = Mapping((1, 1), (2, 3), (3, 4));
            var topology = Topology("amps: 1:1,2:3,3:4 ; routes: 1>2"); // no route 1>3

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1, 3)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear), (3, PhysicalOccupancy.Occupied)),
                topology,
                mapping);

            Assert.Equal(CarrierVerdict.Revoke, Decision(decision, 4).Verdict); // section 3 -> amp 4
            Assert.Equal(Neutral, Decision(decision, 4).Hr0);
            Assert.True(decision.AnyLocoStopRequired);
            Assert.Contains(1, decision.LatchedLocoIds);
            Assert.False(decision.LayoutStopRequired);
        }

        // -----------------------------------------------------------------
        // 10) Stale KIS (shadow invalid) -> layout stop
        // -----------------------------------------------------------------

        [Fact]
        public void ShadowInvalid_LayoutStop_AllNeutral()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                ReconciliationTestFixtures.Shadow(shadowValid: false, profileId: "Simple Loop"),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)),
                topology,
                mapping);

            Assert.True(decision.LayoutStopRequired);
            Assert.All(decision.Amplifiers, d => Assert.Equal(Neutral, d.Hr0));
        }

        // -----------------------------------------------------------------
        // 11) Stale physical (unknown) -> fail closed
        // -----------------------------------------------------------------

        [Fact]
        public void PhysicalUnknown_FailClosed()
        {
            var mapping = Mapping((1, 1));
            var topology = Topology("amps: 1:1");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)),
                PhysicalStale(1),
                topology,
                mapping);

            Assert.Equal(CarrierVerdict.Neutral, Decision(decision, 1).Verdict);
            Assert.Equal(Neutral, Decision(decision, 1).Hr0);
        }

        // -----------------------------------------------------------------
        // 12) Mapping collision
        // -----------------------------------------------------------------

        [Fact]
        public void MappingCollision_TwoOwnersOneAmp_Conflict()
        {
            var mapping = Mapping((1, 1), (2, 1)); // two sections -> one amp
            var topology = Topology("amps: 1:1");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0), [2] = (100, 0) },
                new Dictionary<int, int> { [10] = 1, [20] = 2 },
                Shadow(Loco(10, 1), Loco(20, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping);

            Assert.Equal(CarrierVerdict.Conflict, Decision(decision, 1).Verdict);
            Assert.Equal(Neutral, Decision(decision, 1).Hr0);
        }

        // -----------------------------------------------------------------
        // 13) Missing LocomotiveBinding
        // -----------------------------------------------------------------

        [Fact]
        public void MissingBinding_LocoContributesZeroTargets()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            // internal id 99 has no binding.
            var decision = Reconcile(
                new Dictionary<int, (int, int)>(),
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1), Loco(99, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping);

            Assert.DoesNotContain(decision.Amplifiers, d => d.OwnerDcc is not null && d.Verdict == CarrierVerdict.Drive && d.Amplifier == 3);
            Assert.Contains(decision.Diagnostics, d => d.Contains("internal id 99"));
        }

        // -----------------------------------------------------------------
        // (c) Cross-loco OCCUPIED exclusivity
        // -----------------------------------------------------------------

        [Fact]
        public void CrossLoco_OccupiedByOtherLoco_IsNotDrivenByA()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            // B (dcc 2) OCCUPIES section 2; A (dcc 1) occupies section 1 and RESERVES section 2.
            // B is listed FIRST so A's reservation would otherwise overwrite B's occupied ownership.
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0), [2] = (0, 0) },
                new Dictionary<int, int> { [10] = 1, [20] = 2 },
                Shadow(Loco(20, 2), Loco(10, 1, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping);

            // amp3 (section 2) is owned by B, never driven by A.
            Assert.Equal(2, Decision(decision, 3).OwnerDcc);
            Assert.NotEqual(CarrierVerdict.Drive, Decision(decision, 3).Verdict); // B speed 0 -> neutral

            // amp1 (A's occupied section) is still driven by A.
            Assert.Equal(1, Decision(decision, 1).OwnerDcc);
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 1).Verdict);
        }

        // -----------------------------------------------------------------
        // (k) Numeric-adjacency shortcut
        // -----------------------------------------------------------------

        [Fact]
        public void BoundaryLead_NonAdjacentDeclaredRoute_IsRecognized()
        {
            // Non-adjacent ids {10,20} with a declared route 10>20: |10-20| = 10, not 1, yet the
            // declared route (not numeric adjacency) makes this an expected boundary lead.
            var mapping = Mapping((10, 7), (20, 11));
            var topology = Topology("amps: 10:7,20:11 ; routes: 10>20");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 10, 20)), // A occupied 10, reserved 20
                Physical((10, PhysicalOccupancy.Occupied), (20, PhysicalOccupancy.Occupied)),
                topology,
                mapping);

            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 11).Verdict);
            Assert.Equal(CarrierReason.ExpectedBoundaryLead, Decision(decision, 11).Reason);
        }

        [Fact]
        public void BoundaryLead_NumericAdjacentButUndeclared_IsNotExpected()
        {
            // Numerically adjacent ids {1,2} with NO declared route: |1-2| = 1, yet without a
            // declared route this must NOT be an expected boundary lead (fails closed).
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3"); // no routes

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1, 2)), // A occupied 1, reserved 2
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping);

            var amp3 = Decision(decision, 3);
            Assert.NotEqual(CarrierVerdict.Drive, amp3.Verdict);
            Assert.NotEqual(CarrierReason.ExpectedBoundaryLead, amp3.Reason);
            Assert.Equal(Neutral, amp3.Hr0); // revoke (loco-stop + latch) -> neutral
        }

        // -----------------------------------------------------------------
        // (Issue 1) Departed-section release is PHYSICAL-gated
        // -----------------------------------------------------------------

        [Fact]
        public void DepartedSection_StillOccupied_IsHeldDriven()
        {
            var mapping = Mapping((1, 1), (2, 3), (3, 4));
            var topology = Topology("amps: 1:1,2:3,3:4 ; routes: 1>2,2>3");

            // A moved from section 1 to section 2 (Occupied(2)+Reserved(3)); section 1 is now Free
            // but physically still Occupied (the rear is crossing). It was previously driven (amp1).
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 2, 3)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied), (3, PhysicalOccupancy.Clear)),
                topology,
                mapping,
                previousTargets: new Dictionary<int, (int, int)> { [1] = (1, 650) });

            // Departed amp1 is held driven (non-neutral) with A's retained speed, NOT neutralized.
            var amp1 = Decision(decision, 1);
            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, amp1.Verdict);
            Assert.Equal(CarrierReason.DepartedPhysicalHold, amp1.Reason);
            Assert.Equal(1, amp1.OwnerDcc);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), amp1.Hr0);

            // Current targets still driven.
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 3).Verdict); // occupied section 2
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 4).Verdict); // reserved section 3
        }

        [Fact]
        public void DepartedSection_Clear_IsHeldForTail()
        {
            var mapping = Mapping((1, 1), (2, 3), (3, 4));
            var topology = Topology("amps: 1:1,2:3,3:4 ; routes: 1>2,2>3");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 2, 3)),
                Physical((1, PhysicalOccupancy.Clear), (2, PhysicalOccupancy.Occupied), (3, PhysicalOccupancy.Clear)),
                topology,
                mapping,
                previousTargets: new Dictionary<int, (int, int)> { [1] = (1, 650) });

            // Departed amp1 is now filtered Clear -> held driven for the post-clear tail hold, not released.
            var amp1 = Decision(decision, 1);
            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, amp1.Verdict);
            Assert.Equal(CarrierReason.DepartedTailHold, amp1.Reason);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), amp1.Hr0);
        }

        // -----------------------------------------------------------------
        // (Two-stage) Immediate target preparation
        // -----------------------------------------------------------------

        [Fact]
        public void ReservedTarget_IsPoweredImmediately_NoPhysicalOrKisOccupiedWait()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            // T (section 2) is Reserved(A) and physically Clear; A occupies section 1. T must be
            // powered with A's retained PWM immediately (no wait for physical Occupied(T) or KIS Occupied(T)).
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)),
                topology,
                mapping);

            var amp3 = Decision(decision, 3); // section 2 -> amp 3
            Assert.Equal(CarrierVerdict.Drive, amp3.Verdict);
            Assert.Equal(CarrierReason.ReservedPreEnergize, amp3.Reason);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), amp3.Hr0);
        }

        [Fact]
        public void ReservedTarget_ClearPhysical_StillPoweredImmediately()
        {
            // No artificial delay: a Reserved target is powered even though its physical occupancy is
            // still Clear and KIS has not yet reported Occupied(T). (No two-second movement pause.)
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)),
                topology,
                mapping);

            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 3).Verdict);
            Assert.NotEqual(Neutral, Decision(decision, 3).Hr0);
        }

        // -----------------------------------------------------------------
        // (Two-stage) Physical source hold + stale
        // -----------------------------------------------------------------

        [Fact]
        public void DepartedSection_StaleTelemetry_IsHeldNotReleased()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 2)),
                PhysicalStale(1),
                topology,
                mapping,
                previousTargets: new Dictionary<int, (int, int)> { [1] = (1, 650) });

            // Stale/Unknown is NOT interpreted as Clear: the departed source stays held, not released.
            var amp1 = Decision(decision, 1);
            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, amp1.Verdict);
            Assert.Equal(CarrierReason.DepartedSourceStale, amp1.Reason);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), amp1.Hr0);
        }

        [Fact]
        public void KisOccupiedTarget_DoesNotAffectSourceHold()
        {
            // Regardless of KIS reporting Occupied(T), the departed source S stays driven while S is
            // still physically occupied (KIS state never neutralizes S by itself).
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 2)), // A now occupies T, S is departed
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                previousTargets: new Dictionary<int, (int, int)> { [1] = (1, 650) });

            var amp1 = Decision(decision, 1);
            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, amp1.Verdict);
            Assert.Equal(CarrierReason.DepartedPhysicalHold, amp1.Reason);
            Assert.NotEqual(Neutral, amp1.Hr0);
        }

        [Fact]
        public void DepartedSource_Handover_NewOwnerTerminatesHold()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            // A departs section 1 (occupies 2); B now reserves section 1. A's previous hold on amp1
            // must terminate and B's decision must apply (explicit safe handover, no latest-writer race).
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0), [2] = (0, 0) },
                new Dictionary<int, int> { [10] = 1, [20] = 2 },
                Shadow(Loco(10, 2), Loco(20, null, 1)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                previousTargets: new Dictionary<int, (int, int)> { [1] = (1, 650) });

            // Section 1 now belongs to B: A's hold is terminated, B's (speed-zero) neutral applies.
            var amp1 = Decision(decision, 1);
            Assert.NotEqual(CarrierVerdict.DepartingPhysicalHold, amp1.Verdict);
            Assert.Equal(2, amp1.OwnerDcc);
            Assert.Equal(Neutral, amp1.Hr0);
        }

        // -----------------------------------------------------------------
        // (Speed-zero repair) A stopped spanning locomotive keeps its physical attribution
        // -----------------------------------------------------------------

        [Fact]
        public void DepartedSource_SpeedZero_StopsButKeepsAttribution()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (0, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 2)), // A occupies 2; S=1 departed
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                previousTargets: new Dictionary<int, (int, int)> { [1] = (1, 650) },
                physicalFootprint: new Dictionary<int, int> { [1] = 1 });

            var amp1 = Decision(decision, 1);
            Assert.Equal(CarrierVerdict.DepartingPhysicalStop, amp1.Verdict);
            Assert.Equal(CarrierReason.DepartingSourceStopped, amp1.Reason);
            Assert.Equal(1, amp1.OwnerDcc);
            Assert.Equal(Neutral, amp1.Hr0);

            // Occupied target still attributed to A, neutral (stop semantics).
            var amp3 = Decision(decision, 3);
            Assert.Equal(1, amp3.OwnerDcc);
            Assert.Equal(Neutral, amp3.Hr0);

            // The footprint retains the departed source S -> A (residue); the occupied target is
            // logically owned and therefore not part of the residue.
            Assert.Equal(1, decision.PhysicalFootprint[1]);
            Assert.False(decision.PhysicalFootprint.ContainsKey(2));
        }

        [Fact]
        public void DepartedSource_SpeedZero_NextCycleNoFreeOccupiedRevoke()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            // No previousTargets: S was neutralized last cycle, so only the footprint retains the
            // attribution. This must NOT fall into Free+Occupied -> Revoke.
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (0, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [1] = 1 });

            var amp1 = Decision(decision, 1);
            Assert.Equal(CarrierVerdict.DepartingPhysicalStop, amp1.Verdict);
            Assert.NotEqual(CarrierVerdict.Revoke, amp1.Verdict);
            Assert.Equal(1, amp1.OwnerDcc);
            Assert.Equal(Neutral, amp1.Hr0);
            Assert.DoesNotContain(decision.Diagnostics, d => d.Contains("ContradictionPhysicalWithoutLogicalOwner"));
        }

        [Fact]
        public void ReconstructedExtension_SpeedZero_NeutralAttributed_NoRevoke()
        {
            // User-placed stopped locomotive: S=Occupied(A), T=Free, both physically occupied.
            // T is attributed to A (neutral) and must not be an ownerless intrusion.
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (0, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)), // A occupies 1 only; 2 is Free
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [1] = 1, [2] = 1 });

            var amp3 = Decision(decision, 3); // section 2 -> amp 3
            Assert.Equal(CarrierVerdict.DepartingPhysicalStop, amp3.Verdict);
            Assert.Equal(1, amp3.OwnerDcc);
            Assert.Equal(Neutral, amp3.Hr0);
            Assert.NotEqual(CarrierVerdict.Revoke, amp3.Verdict);
        }

        [Fact]
        public void FootprintExtension_AtSpeed_DrivesWithCurrentPwm()
        {
            // A reconstructed footprint extension (T=Free) is DRIVEN with the footprint owner's
            // current speed/direction on the first valid speed command, so a cold-start spanning
            // locomotive does not leave its extension section stuck at neutral (399).
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)), // T=Free
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [1] = 1, [2] = 1 });

            // S (occupied A) is driven.
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 1).Verdict);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), Decision(decision, 1).Hr0);

            // T (reconstructed extension) is now driven with the same current A PWM.
            var amp3 = Decision(decision, 3);
            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, amp3.Verdict);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), amp3.Hr0);
            Assert.Equal(1, amp3.OwnerDcc);
        }

        // -----------------------------------------------------------------
        // (A) Cold startup: A spans S+T, speed=0 -> both HR0=399, attribution survives
        // -----------------------------------------------------------------

        [Fact]
        public void StartupFootprint_SpeedZero_BothNeutralAttributed_RequirementA()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (0, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)), // A occupies S only; T is Free
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [1] = 1, [2] = 1 });

            // Both S and T are neutral (399) and still attributed to A.
            Assert.Equal(Neutral, Decision(decision, 1).Hr0);
            Assert.Equal(1, Decision(decision, 1).OwnerDcc);
            Assert.Equal(Neutral, Decision(decision, 3).Hr0);
            Assert.Equal(1, Decision(decision, 3).OwnerDcc);

            // No ownerless-intrusion escalation; the Free extension stays in the residue footprint.
            Assert.DoesNotContain(decision.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);
            Assert.Equal(1, decision.PhysicalFootprint[2]);
            Assert.False(decision.PhysicalFootprint.ContainsKey(1)); // logically owned -> not residue
        }

        // -----------------------------------------------------------------
        // (B) First valid speed>0: BOTH S and T receive current A drive state in the SAME decision
        // -----------------------------------------------------------------

        [Fact]
        public void FirstSpeed_DrivesFullFootprint_SameReconciliation_RequirementB()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [1] = 1, [2] = 1 });

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);

            // S is A's occupied section: driven.
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 1).Verdict);
            Assert.Equal(pwm, Decision(decision, 1).Hr0);

            // T is A's reconstructed extension: driven with the same current A PWM in the SAME decision.
            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, Decision(decision, 3).Verdict);
            Assert.Equal(pwm, Decision(decision, 3).Hr0);
            Assert.Equal(1, Decision(decision, 3).OwnerDcc);
        }

        // -----------------------------------------------------------------
        // (C) No previousTargets: same result
        // -----------------------------------------------------------------

        [Fact]
        public void FirstSpeed_NoPreviousTargets_SameResult_RequirementC()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                previousTargets: new Dictionary<int, (int, int)>(), // empty drive history
                physicalFootprint: new Dictionary<int, int> { [1] = 1, [2] = 1 });

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 1).Verdict);
            Assert.Equal(pwm, Decision(decision, 1).Hr0);
            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, Decision(decision, 3).Verdict);
            Assert.Equal(pwm, Decision(decision, 3).Hr0);
        }

        // -----------------------------------------------------------------
        // (D) No pre-existing ExpectedTransition: same result when reconstruction otherwise valid
        // -----------------------------------------------------------------

        [Fact]
        public void FirstSpeed_NoExpectedTransition_StillDrivesFootprint_RequirementD()
        {
            // T is Free (not Reserved(A)) and physically Occupied: there is no reservation/boundary
            // lead ("ExpectedTransition"). The reconstructed footprint alone must still drive T.
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)), // no reservation of T
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [1] = 1, [2] = 1 });

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            var amp3 = Decision(decision, 3);
            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, amp3.Verdict);
            Assert.Equal(pwm, amp3.Hr0);
            Assert.NotEqual(CarrierReason.ExpectedBoundaryLead, amp3.Reason);
            Assert.False(decision.AnyLocoStopRequired);
        }

        // -----------------------------------------------------------------
        // (H) A required footprint section reserved by Loc B is a boundary, not a partial departure
        // -----------------------------------------------------------------

        [Fact]
        public void AdjacentSections_OtherLocoReserved_IsBoundaryNotContainment()
        {
            // A occupies S (speed>0); B RESERVES adjacent T (speed 0). T is other-loco territory,
            // so it is a BOUNDARY for A, not a partial-footprint trigger: A drives S, T stays B's
            // (neutral). Two adjacent trains must not neutralize each other.
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0), [2] = (0, 0) },
                new Dictionary<int, int> { [10] = 1, [20] = 2 },
                Shadow(Loco(10, 1), Loco(20, null, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [1] = 1 });

            // A is NOT contained: its occupied anchor still drives.
            var amp1 = Decision(decision, 1);
            Assert.Equal(CarrierVerdict.Drive, amp1.Verdict);
            Assert.Equal(1, amp1.OwnerDcc);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), amp1.Hr0);

            // T remains B's (B speed 0 -> neutral), never driven by A.
            var amp3 = Decision(decision, 3);
            Assert.Equal(2, amp3.OwnerDcc);
            Assert.NotEqual(CarrierVerdict.Drive, amp3.Verdict);
            Assert.Equal(Neutral, amp3.Hr0);

            // No partial-footprint containment, no loco-stop escalation.
            Assert.DoesNotContain(decision.Amplifiers, d => d.Reason == CarrierReason.PartialFootprintContain);
            Assert.DoesNotContain(decision.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);
            Assert.False(decision.AnyLocoStopRequired);
            Assert.False(decision.LayoutStopRequired);
        }

        // -----------------------------------------------------------------
        // (DEFECT 1) Authority gate: a logically-Free loco with stale speed must not drive
        // -----------------------------------------------------------------

        [Fact]
        public void FreeLoco_StaleSpeed_DoesNotDriveFootprint()
        {
            // A became logically Free (no occupied, no reserved) but retains a stale Speed > 0 and a
            // physical footprint residue on section 2. It must NOT drive its still-occupied footprint.
            var mapping = Mapping((2, 3));
            var topology = Topology("amps: 2:3");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, null)), // A logically Free
                Physical((2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [2] = 1 });

            var amp3 = Decision(decision, 3);
            Assert.Equal(Neutral, amp3.Hr0);
            Assert.Equal(1, amp3.OwnerDcc);
            Assert.NotEqual(CarrierVerdict.DepartingPhysicalHold, amp3.Verdict);
            Assert.NotEqual(CarrierVerdict.Drive, amp3.Verdict);

            // Normal case: A OCCUPIES section 1 (valid logical authority) -> its footprint extension on
            // section 2 still drives, proving the authority gate is not over-broad.
            var driveMapping = Mapping((1, 1), (2, 3));
            var driveTopology = Topology("amps: 1:1,2:3 ; routes: 1>2");
            var drive = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                driveTopology,
                driveMapping,
                physicalFootprint: new Dictionary<int, int> { [1] = 1, [2] = 1 });

            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, Decision(drive, 3).Verdict);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), Decision(drive, 3).Hr0);
        }

        // -----------------------------------------------------------------
        // (DEFECT 2) Adjacent trains with no cross-reservation must not contain each other
        // -----------------------------------------------------------------

        [Fact]
        public void AdjacentTrains_NoCrossReservation_NeitherContained()
        {
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0), [2] = (100, 0) },
                new Dictionary<int, int> { [10] = 1, [20] = 2 },
                Shadow(Loco(10, 1), Loco(20, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping);

            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 1).Verdict);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), Decision(decision, 1).Hr0);
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 3).Verdict);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), Decision(decision, 3).Hr0);

            Assert.DoesNotContain(decision.Amplifiers, d => d.Reason == CarrierReason.PartialFootprintContain);
            Assert.DoesNotContain(decision.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);
            Assert.False(decision.AnyLocoStopRequired);
        }

        // -----------------------------------------------------------------
        // (ACCEPTANCE ISSUE 3) Containment is neutral-only: no dead loco-stop flag
        // -----------------------------------------------------------------

        [Fact]
        public void Containment_NeutralOnly_NoLocoStopFlag()
        {
            // Genuine partial-footprint case: A occupies S (speed>0), adjacent T is physically
            // Occupied but FREE (no logical owner, no reservation) and NOT attributable to A. A's
            // footprint is partial, so A is contained NEUTRALLY with no loco-stop flag raised.
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [1] = 1 });

            var amp1 = Decision(decision, 1);
            Assert.Equal(Neutral, amp1.Hr0);
            Assert.Equal(1, amp1.OwnerDcc);
            Assert.Equal(CarrierReason.PartialFootprintContain, amp1.Reason);

            Assert.False(decision.AnyLocoStopRequired);
            Assert.Empty(decision.LatchedLocoIds);
            Assert.False(decision.LayoutStopRequired);
        }

        // -----------------------------------------------------------------
        // (Decision B2) A stale/Unknown/unmapped REQUIRED section contains the locomotive
        // -----------------------------------------------------------------

        [Fact]
        public void ReservedTarget_StaleTelemetry_ContainsDeparture()
        {
            // §34: A occupies S (fresh Occupied) and reserves T; T's physical telemetry is stale/Unknown.
            // A must be CONTAINED: S is neutral (no Drive on S), so there is NO partial first departure.
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1, 2)), // A occupies S=1, reserves T=2
                ReconciliationTestFixtures.Physical(new[]
                {
                    ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied),
                    ReconciliationTestFixtures.PhysicalState(2, PhysicalOccupancy.Unknown, isFresh: false),
                }),
                topology,
                mapping);

            // A's occupied anchor S is contained: neutral, not driven.
            var amp1 = Decision(decision, 1);
            Assert.Equal(Neutral, amp1.Hr0);
            Assert.Equal(1, amp1.OwnerDcc);
            Assert.NotEqual(CarrierVerdict.Drive, amp1.Verdict);
            Assert.Equal(CarrierReason.PartialFootprintContain, amp1.Reason);

            // T (section 2) is stale -> neutral (fail closed), not driven.
            var amp3 = Decision(decision, 3);
            Assert.Equal(Neutral, amp3.Hr0);
            Assert.NotEqual(CarrierVerdict.Drive, amp3.Verdict);

            // No A target is driven, and no partial first departure.
            Assert.DoesNotContain(decision.Amplifiers, d => d.OwnerDcc == 1 && d.Verdict == CarrierVerdict.Drive);

            // Containment is neutral-only: no loco-stop, no latch, no layout stop.
            Assert.False(decision.AnyLocoStopRequired);
            Assert.Empty(decision.LatchedLocoIds);
            Assert.False(decision.LayoutStopRequired);
        }

        [Fact]
        public void UnrelatedStaleBranch_DoesNotContainA()
        {
            // §33: branch topology 1>2 and 1>3. A occupies 1 and reserves 2 (valid target); section 3 is
            // stale/Unknown. Section 3 is an UNRELATED branch: local fail-closed, A NOT contained, and A
            // drives section 1 normally (Decision B1).
            var mapping = Mapping((1, 1), (2, 3), (3, 4));
            var topology = Topology("amps: 1:1,2:3,3:4 ; routes: 1>2,1>3");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1, 2)), // A occupies 1, reserves 2 (valid target)
                ReconciliationTestFixtures.Physical(new[]
                {
                    ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied),
                    ReconciliationTestFixtures.PhysicalState(2, PhysicalOccupancy.Clear),
                    ReconciliationTestFixtures.PhysicalState(3, PhysicalOccupancy.Unknown, isFresh: false),
                }),
                topology,
                mapping);

            // Section 3 (unrelated stale branch) is local fail-closed neutral.
            var amp4 = Decision(decision, 4);
            Assert.Equal(Neutral, amp4.Hr0);
            Assert.NotEqual(CarrierVerdict.Drive, amp4.Verdict);

            // A is NOT contained: it drives its occupied anchor and its reserved target normally.
            var amp1 = Decision(decision, 1);
            Assert.Equal(CarrierVerdict.Drive, amp1.Verdict);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), amp1.Hr0);
            Assert.Equal(CarrierVerdict.Drive, Decision(decision, 3).Verdict);

            Assert.DoesNotContain(decision.Amplifiers, d => d.Reason == CarrierReason.PartialFootprintContain);
            Assert.False(decision.AnyLocoStopRequired);
        }

        [Fact]
        public void MissingMapping_RequiredFootprint_ContainsDeparture()
        {
            // §35: A's required (retained) footprint section lacks a physical mapping. A must be
            // CONTAINED: its occupied anchor S is neutral (no Drive on S).
            var mapping = Mapping((1, 1)); // section 2 has NO physical mapping
            var topology = Topology("amps: 1:1 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)), // A occupies S=1
                ReconciliationTestFixtures.Physical(new[]
                {
                    ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied),
                }),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [2] = 1 }); // A's retained footprint section 2 (unmapped)

            // A's occupied anchor S is contained: neutral, not driven.
            var amp1 = Decision(decision, 1);
            Assert.Equal(Neutral, amp1.Hr0);
            Assert.Equal(1, amp1.OwnerDcc);
            Assert.NotEqual(CarrierVerdict.Drive, amp1.Verdict);
            Assert.Equal(CarrierReason.PartialFootprintContain, amp1.Reason);

            // Neutral-only containment: no loco-stop, no latch, no layout stop.
            Assert.False(decision.AnyLocoStopRequired);
            Assert.Empty(decision.LatchedLocoIds);
            Assert.False(decision.LayoutStopRequired);
        }

        [Fact]
        public void UnmappedUnrelatedBranch_DoesNotContainA()
        {
            // §35 (B1 counterpart): an unmapped branch that is NOT Reserved(A) and NOT in A's retained
            // footprint must NOT stop A. A drives its occupied anchor normally.
            var mapping = Mapping((1, 1)); // section 2 has NO physical mapping
            var topology = Topology("amps: 1:1 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)), // A occupies 1; no reservation, no footprint on 2
                ReconciliationTestFixtures.Physical(new[]
                {
                    ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied),
                }),
                topology,
                mapping);

            var amp1 = Decision(decision, 1);
            Assert.Equal(CarrierVerdict.Drive, amp1.Verdict);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), amp1.Hr0);

            Assert.DoesNotContain(decision.Amplifiers, d => d.Reason == CarrierReason.PartialFootprintContain);
            Assert.False(decision.AnyLocoStopRequired);
        }

        [Fact]
        public void RetainedFootprint_StaleTelemetry_ContainsDeparture()
        {
            // §16 case 1: a section already in A's retained _footprint goes stale/Unknown. A must be
            // CONTAINED: its occupied anchor S is neutral (no Drive on S).
            var mapping = Mapping((1, 1), (2, 3));
            var topology = Topology("amps: 1:1,2:3 ; routes: 1>2");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1)), // A occupies S=1
                ReconciliationTestFixtures.Physical(new[]
                {
                    ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied),
                    ReconciliationTestFixtures.PhysicalState(2, PhysicalOccupancy.Unknown, isFresh: false),
                }),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [1] = 1, [2] = 1 }); // A retains section 2

            // A's occupied anchor S is contained: neutral, not driven.
            var amp1 = Decision(decision, 1);
            Assert.Equal(Neutral, amp1.Hr0);
            Assert.Equal(1, amp1.OwnerDcc);
            Assert.NotEqual(CarrierVerdict.Drive, amp1.Verdict);
            Assert.Equal(CarrierReason.PartialFootprintContain, amp1.Reason);

            // The stale footprint section 2 stays attributed to A and neutral (fail closed).
            var amp3 = Decision(decision, 3);
            Assert.Equal(Neutral, amp3.Hr0);
            Assert.Equal(1, amp3.OwnerDcc);

            Assert.DoesNotContain(decision.Amplifiers, d => d.OwnerDcc == 1 && d.Verdict == CarrierVerdict.Drive);
            Assert.False(decision.AnyLocoStopRequired);
            Assert.Empty(decision.LatchedLocoIds);
            Assert.False(decision.LayoutStopRequired);
        }

        // -----------------------------------------------------------------
        // (Containment completeness) A contained locomotive never drives its reserved target
        // -----------------------------------------------------------------

        [Fact]
        public void ContainedLoco_ReservedTarget_NotDriven()
        {
            // A occupies S=1 (fresh Occupied) and reserves T=2 (fresh Occupied, expected boundary lead
            // via route 1>2). A is CONTAINED via a stale retained-footprint section 3 (Decision B2).
            // The fresh expected-boundary-lead reserved target T must NOT be driven: neutral with
            // PartialFootprintContain (neutral-only, no loco-stop escalation).
            var mapping = Mapping((1, 1), (2, 3), (3, 4));
            var topology = Topology("amps: 1:1,2:3,3:4 ; routes: 1>2,2>3");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1, 2)), // A occupies 1, reserves 2
                ReconciliationTestFixtures.Physical(new[]
                {
                    ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied),
                    ReconciliationTestFixtures.PhysicalState(2, PhysicalOccupancy.Occupied),
                    ReconciliationTestFixtures.PhysicalState(3, PhysicalOccupancy.Unknown, isFresh: false),
                }),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [3] = 1 }); // A retains stale section 3

            // A's occupied anchor S is contained: neutral, not driven.
            var amp1 = Decision(decision, 1);
            Assert.Equal(Neutral, amp1.Hr0);
            Assert.Equal(1, amp1.OwnerDcc);
            Assert.NotEqual(CarrierVerdict.Drive, amp1.Verdict);
            Assert.Equal(CarrierReason.PartialFootprintContain, amp1.Reason);

            // The fresh expected-boundary-lead reserved target T must NOT be driven.
            var amp3 = Decision(decision, 3); // section 2 -> amp 3
            Assert.Equal(Neutral, amp3.Hr0);
            Assert.Equal(CarrierVerdict.Neutral, amp3.Verdict);
            Assert.Equal(CarrierReason.PartialFootprintContain, amp3.Reason);
            Assert.NotEqual(CarrierVerdict.Drive, amp3.Verdict);

            // Neutral-only containment: no loco-stop, no latch, no layout stop.
            Assert.False(decision.AnyLocoStopRequired);
            Assert.Empty(decision.LatchedLocoIds);
            Assert.False(decision.LayoutStopRequired);
        }

        [Fact]
        public void ContainedLoco_ReservedClearPreEnergize_NotDriven()
        {
            // Same containment trigger, but the reserved target T=2 is physically Clear: the Reserved(A)
            // pre-energize DRIVE must also be neutralized for a contained locomotive.
            var mapping = Mapping((1, 1), (2, 3), (3, 4));
            var topology = Topology("amps: 1:1,2:3,3:4 ; routes: 1>2,2>3");

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [10] = 1 },
                Shadow(Loco(10, 1, 2)), // A occupies 1, reserves 2
                ReconciliationTestFixtures.Physical(new[]
                {
                    ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied),
                    ReconciliationTestFixtures.PhysicalState(2, PhysicalOccupancy.Clear),
                    ReconciliationTestFixtures.PhysicalState(3, PhysicalOccupancy.Unknown, isFresh: false),
                }),
                topology,
                mapping,
                physicalFootprint: new Dictionary<int, int> { [3] = 1 }); // A retains stale section 3

            // The Clear reserved target T must NOT be pre-energized.
            var amp3 = Decision(decision, 3); // section 2 -> amp 3
            Assert.Equal(Neutral, amp3.Hr0);
            Assert.Equal(CarrierVerdict.Neutral, amp3.Verdict);
            Assert.Equal(CarrierReason.PartialFootprintContain, amp3.Reason);
            Assert.NotEqual(CarrierReason.ReservedPreEnergize, amp3.Reason);
            Assert.NotEqual(CarrierVerdict.Drive, amp3.Verdict);

            // Neutral-only containment: no loco-stop, no latch, no layout stop.
            Assert.False(decision.AnyLocoStopRequired);
            Assert.Empty(decision.LatchedLocoIds);
            Assert.False(decision.LayoutStopRequired);
        }
    }
}
