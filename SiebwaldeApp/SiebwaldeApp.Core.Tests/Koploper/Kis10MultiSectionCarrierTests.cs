using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// KIS10-04 block-level carrier tests: a Koploper block maps to 1..N sections and its COMPLETE
    /// drive group follows one owner's speed/direction (PO §21). Layout under test:
    /// block 10 → sections [1,2] → amps 1,2 ; block 20 → sections [3,4] → amps 4,6 ; route 10&gt;20.
    /// </summary>
    public class Kis10MultiSectionCarrierTests
    {
        private const int Neutral = AmplifierSpeedMapper.NeutralPwm;

        private static readonly IReadOnlyDictionary<int, IReadOnlyList<int>> TwoBlocks =
            ReconciliationTestFixtures.BlockMap((10, new[] { 1, 2 }), (20, new[] { 3, 4 }));

        private static IReadOnlyList<LayoutPhysicalAmplifierBinding> Mapping(params (int Section, int Amp)[] pairs)
            => pairs.Select(p => new LayoutPhysicalAmplifierBinding { SectionId = p.Section, PhysicalAmplifier = p.Amp }).ToArray();

        private static BlockTopology Topology(string text) => BlockTopology.Parse(text);

        private static LogicalLocomotiveShadow Loco(int id, int? occupied, params int[] reserved)
            => ReconciliationTestFixtures.Loco(id, occupied, reserved);

        private static KoploperLogicalSectionShadowObservation Shadow(params LogicalLocomotiveShadow[] locos)
            => ReconciliationTestFixtures.Shadow(locomotives: locos, profileId: "Simple Loop");

        private static KoploperLogicalSectionShadowObservation InvalidShadow()
            => ReconciliationTestFixtures.Shadow(locomotives: Array.Empty<LogicalLocomotiveShadow>(), shadowValid: false);

        private static PhysicalSectionOccupancyObservation Physical(params (int Section, PhysicalOccupancy Occ)[] states)
            => ReconciliationTestFixtures.Physical(
                states.Select(s => ReconciliationTestFixtures.PhysicalState(s.Section, s.Occ)).ToArray());

        private static CarrierDecision Reconcile(
            Dictionary<int, (int, int)> desired,
            Dictionary<int, int> identity,
            KoploperLogicalSectionShadowObservation? shadow,
            PhysicalSectionOccupancyObservation? physical,
            string topology,
            IReadOnlyList<LayoutPhysicalAmplifierBinding> mapping,
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockToSections,
            Dictionary<int, (int, int)>? previousTargets = null,
            Dictionary<int, int>? footprint = null)
            => LocomotiveCarrierReconciliation.Reconcile(
                desired, identity, shadow, physical, Topology(topology), mapping, blockToSections,
                previousTargets, footprint, TimeSpan.FromSeconds(5));

        private static int Pwm(TrackApplicationVariables variables, int amp)
            => variables.PendingWrites.TryGetValue((ushort)amp, out var write) ? write.Hr0Value & 0x03FF : Neutral;

        private static LocomotiveCarrierController CreateController(
            TrackApplicationVariables variables,
            AmplifierCommandTracker tracker,
            Dictionary<int, int> identity,
            string topology,
            IReadOnlyList<LayoutPhysicalAmplifierBinding> mapping,
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockToSections,
            Func<DateTimeOffset>? clock = null)
            => new(
                variables, tracker, trace: null, identity, Topology(topology), mapping, blockToSections,
                clock: clock);

        // -----------------------------------------------------------------
        // §42 Drive group
        // -----------------------------------------------------------------

        [Fact]
        public void DriveGroup_ReservedBlock_SpeedPositive_BothAmpsDriven()
        {
            var mapping = Mapping((1, 1), (2, 2));
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [100] = 1 },
                Shadow(Loco(100, null, 10)),
                Physical((1, PhysicalOccupancy.Clear), (2, PhysicalOccupancy.Clear)),
                "routes: 10>20",
                mapping,
                TwoBlocks);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(2, decision.Amplifiers.Count);
            Assert.Equal(pwm, Assert.Single(decision.Amplifiers, d => d.Amplifier == 1).Hr0);
            Assert.Equal(pwm, Assert.Single(decision.Amplifiers, d => d.Amplifier == 2).Hr0);
            Assert.All(decision.Amplifiers, d => Assert.Equal(CarrierVerdict.Drive, d.Verdict));
        }

        [Fact]
        public void DriveGroup_SpeedChange_BothAmpsTrackTogether()
        {
            var mapping = Mapping((1, 1), (2, 2));
            var identity = new Dictionary<int, int> { [100] = 1 };
            var shadow = Shadow(Loco(100, 10));
            var physical = Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied));

            var fast = Reconcile(new Dictionary<int, (int, int)> { [1] = (100, 0) }, identity, shadow, physical, "routes: 10>20", mapping, TwoBlocks);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), Assert.Single(fast.Amplifiers, d => d.Amplifier == 1).Hr0);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), Assert.Single(fast.Amplifiers, d => d.Amplifier == 2).Hr0);

            var slow = Reconcile(new Dictionary<int, (int, int)> { [1] = (40, 1) }, identity, shadow, physical, "routes: 10>20", mapping, TwoBlocks);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(40, 1), Assert.Single(slow.Amplifiers, d => d.Amplifier == 1).Hr0);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(40, 1), Assert.Single(slow.Amplifiers, d => d.Amplifier == 2).Hr0);
        }

        [Fact]
        public void DriveGroup_SpeedZero_BothNeutral_AttributionRetained()
        {
            var mapping = Mapping((1, 1), (2, 2));
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (0, 0) },
                new Dictionary<int, int> { [100] = 1 },
                Shadow(Loco(100, 10)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                "routes: 10>20",
                mapping,
                TwoBlocks);

            Assert.Equal(Neutral, Assert.Single(decision.Amplifiers, d => d.Amplifier == 1).Hr0);
            Assert.Equal(Neutral, Assert.Single(decision.Amplifiers, d => d.Amplifier == 2).Hr0);
            Assert.All(decision.Amplifiers, d => Assert.NotEqual(CarrierVerdict.Drive, d.Verdict));
            Assert.All(decision.Amplifiers, d => Assert.Equal(1, d.OwnerDcc));
        }

        [Fact]
        public void DriveGroup_CrossLoco_OccupiedWinsReserved_CannotClaimEitherAmp()
        {
            var mapping = Mapping((1, 1), (2, 2));
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0), [2] = (0, 0) },
                new Dictionary<int, int> { [100] = 1, [200] = 2 },
                Shadow(Loco(200, null, 10), Loco(100, 10)), // B reserves, A occupies (A listed second)
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                "routes: 10>20",
                mapping,
                TwoBlocks);

            // Occupied(A) precedence: both amps of block 10 belong to A; B cannot claim either.
            Assert.All(decision.Amplifiers, d => Assert.Equal(1, d.OwnerDcc));
            Assert.All(decision.Amplifiers, d => Assert.Equal(CarrierVerdict.Drive, d.Verdict));
        }

        [Fact]
        public void DriveGroup_PartialBinding_FailsClosedNoPartialEnergization()
        {
            // F1: block 10 -> sections [1, 2], but section 2 has NO physical amplifier binding.
            // The carrier must NOT partially energize amp 1 (section 1): fail closed + diagnostic.
            var mapping = Mapping((1, 1)); // section 2 unbound
            var blockMap = ReconciliationTestFixtures.BlockMap((10, new[] { 1, 2 }));

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [100] = 1 },
                Shadow(Loco(100, 10)), // A occupies block 10
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                "routes: 10>20",
                mapping,
                blockMap);

            var amp1 = Assert.Single(decision.Amplifiers, d => d.Amplifier == 1);
            Assert.Equal(Neutral, amp1.Hr0);
            Assert.Equal(CarrierVerdict.Neutral, amp1.Verdict);
            Assert.Equal(CarrierReason.MissingBinding, amp1.Reason);
            Assert.Contains(decision.Diagnostics, d => d.Contains("incomplete drive group"));
        }

        [Fact]
        public void DriveGroup_FullyBoundBlock_StillDrivesCompleteGroup()
        {
            // (c) Regression guard for the F1 wiring: a fully-bound multi-section block still drives
            // its COMPLETE drive group through the projector path (occupied block, both sections bound).
            var mapping = Mapping((1, 1), (2, 2));

            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [100] = 1 },
                Shadow(Loco(100, 10)), // A occupies block 10 (both sections bound)
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                "routes: 10>20",
                mapping,
                TwoBlocks);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, Assert.Single(decision.Amplifiers, d => d.Amplifier == 1).Hr0);
            Assert.Equal(pwm, Assert.Single(decision.Amplifiers, d => d.Amplifier == 2).Hr0);
            Assert.All(decision.Amplifiers, d => Assert.Equal(CarrierVerdict.Drive, d.Verdict));
            Assert.DoesNotContain(decision.Diagnostics, d => d.Contains("incomplete drive group"));
        }

        // -----------------------------------------------------------------
        // §43 Intra-block occupancy progression (PO §21 — no per-section neutralize)
        // -----------------------------------------------------------------

        [Fact]
        public void IntraBlock_OccupancyProgression_NoFalseTransitionOrFreeOccupied()
        {
            var mapping = Mapping((1, 1), (2, 2));
            var identity = new Dictionary<int, int> { [100] = 1 };
            var desired = new Dictionary<int, (int, int)> { [1] = (100, 0) };
            var shadow = Shadow(Loco(100, 10)); // block 10 authorized (Occupied) throughout

            (int, PhysicalOccupancy)[] clearClear = { (1, PhysicalOccupancy.Clear), (2, PhysicalOccupancy.Clear) };
            (int, PhysicalOccupancy)[] occClear = { (1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear) };
            (int, PhysicalOccupancy)[] occOcc = { (1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied) };
            (int, PhysicalOccupancy)[] clearOcc = { (1, PhysicalOccupancy.Clear), (2, PhysicalOccupancy.Occupied) };

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            foreach (var states in new[] { clearClear, occClear, occOcc, clearOcc, clearClear })
            {
                var decision = Reconcile(desired, identity, shadow, Physical(states), "routes: 10>20", mapping, TwoBlocks);

                Assert.Equal(pwm, Assert.Single(decision.Amplifiers, d => d.Amplifier == 1).Hr0);
                Assert.Equal(pwm, Assert.Single(decision.Amplifiers, d => d.Amplifier == 2).Hr0);
                Assert.DoesNotContain(decision.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);
                Assert.DoesNotContain(decision.Diagnostics, d => d.Contains("ContradictionPhysicalWithoutLogicalOwner"));
            }
        }

        [Fact]
        public void IntraBlock_SectionClear_DoesNotNeutralizeItsAmp()
        {
            var mapping = Mapping((1, 1), (2, 2));
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [100] = 1 },
                Shadow(Loco(100, 10)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)), // section 2 clear
                "routes: 10>20",
                mapping,
                TwoBlocks);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, Assert.Single(decision.Amplifiers, d => d.Amplifier == 2).Hr0); // NOT neutralized
            Assert.Equal(CarrierVerdict.Drive, Assert.Single(decision.Amplifiers, d => d.Amplifier == 2).Verdict);
            Assert.DoesNotContain(decision.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);
        }

        // -----------------------------------------------------------------
        // §44 Inter-block transition
        // -----------------------------------------------------------------

        [Fact]
        public void InterBlock_ReservedNextBlock_PreparesFullDriveGroup()
        {
            var mapping = Mapping((1, 1), (2, 2), (3, 4), (4, 6));
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [100] = 1 },
                Shadow(Loco(100, 10, 20)), // A occupies block 10, reserves block 20
                Physical(
                    (1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied),
                    (3, PhysicalOccupancy.Clear), (4, PhysicalOccupancy.Clear)),
                "routes: 10>20",
                mapping,
                TwoBlocks);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(4, decision.Amplifiers.Count);
            Assert.All(decision.Amplifiers, d => Assert.Equal(pwm, d.Hr0));
            Assert.All(decision.Amplifiers, d => Assert.Equal(CarrierVerdict.Drive, d.Verdict));
            Assert.Equal(CarrierReason.ReservedPreEnergize, Assert.Single(decision.Amplifiers, d => d.Amplifier == 4).Reason);
            Assert.Equal(CarrierReason.ReservedPreEnergize, Assert.Single(decision.Amplifiers, d => d.Amplifier == 6).Reason);
        }

        [Fact]
        public void InterBlock_PreviousBlockDeparted_StillPhysicallyOccupied_HeldDriven()
        {
            var mapping = Mapping((1, 1), (2, 2), (3, 4), (4, 6));
            // A moved to block 20 (occupies 20); block 10 departed but physically still occupied.
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0) },
                new Dictionary<int, int> { [100] = 1 },
                Shadow(Loco(100, 20)),
                Physical(
                    (1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied),
                    (3, PhysicalOccupancy.Occupied), (4, PhysicalOccupancy.Occupied)),
                "routes: 10>20",
                mapping,
                TwoBlocks,
                previousTargets: new Dictionary<int, (int, int)> { [1] = (1, 650), [2] = (1, 650) });

            // Departed block 10 (amps 1,2) still physically occupied -> held driven (not released).
            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, Assert.Single(decision.Amplifiers, d => d.Amplifier == 1).Verdict);
            Assert.Equal(pwm, Assert.Single(decision.Amplifiers, d => d.Amplifier == 1).Hr0);
            Assert.Equal(CarrierVerdict.DepartingPhysicalHold, Assert.Single(decision.Amplifiers, d => d.Amplifier == 2).Verdict);

            // Current block 20 (amps 4,6) driven.
            Assert.Equal(CarrierVerdict.Drive, Assert.Single(decision.Amplifiers, d => d.Amplifier == 4).Verdict);
            Assert.Equal(CarrierVerdict.Drive, Assert.Single(decision.Amplifiers, d => d.Amplifier == 6).Verdict);
        }

        // -----------------------------------------------------------------
        // §46 Two locomotives, no contamination
        // -----------------------------------------------------------------

        [Fact]
        public void TwoLocos_IndependentBlocks_NoContamination()
        {
            var mapping = Mapping((1, 1), (2, 2), (3, 4), (4, 6));
            var decision = Reconcile(
                new Dictionary<int, (int, int)> { [1] = (100, 0), [2] = (50, 1) },
                new Dictionary<int, int> { [100] = 1, [200] = 2 },
                Shadow(Loco(100, 10), Loco(200, 20)),
                Physical(
                    (1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied),
                    (3, PhysicalOccupancy.Occupied), (4, PhysicalOccupancy.Occupied)),
                "routes: 10>20",
                mapping,
                TwoBlocks);

            Assert.Equal(1, Assert.Single(decision.Amplifiers, d => d.Amplifier == 1).OwnerDcc);
            Assert.Equal(1, Assert.Single(decision.Amplifiers, d => d.Amplifier == 2).OwnerDcc);
            Assert.Equal(2, Assert.Single(decision.Amplifiers, d => d.Amplifier == 4).OwnerDcc);
            Assert.Equal(2, Assert.Single(decision.Amplifiers, d => d.Amplifier == 6).OwnerDcc);

            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), Assert.Single(decision.Amplifiers, d => d.Amplifier == 1).Hr0);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(50, 1), Assert.Single(decision.Amplifiers, d => d.Amplifier == 4).Hr0);
            Assert.DoesNotContain(decision.Amplifiers, d => d.Verdict == CarrierVerdict.Conflict);
        }

        // -----------------------------------------------------------------
        // §45 Startup / reconstruction
        // -----------------------------------------------------------------

        [Fact]
        public void Startup_TrainInOneSectionOfMultiSectionBlock_FootprintWholeOccupiedSections()
        {
            var mapping = Mapping((1, 1), (2, 2));
            var footprint = PhysicalFootprintReconstructor.Reconstruct(
                Shadow(Loco(100, 10)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)),
                Topology("routes: 10>20"),
                mapping,
                TwoBlocks,
                new Dictionary<int, int> { [100] = 1 });

            Assert.Equal(1, footprint[1]);
            Assert.False(footprint.ContainsKey(2)); // clear, not in footprint
        }

        [Fact]
        public void Startup_TrainSpanningTwoSectionsOfOneBlock_FootprintBothSections()
        {
            var mapping = Mapping((1, 1), (2, 2));
            var footprint = PhysicalFootprintReconstructor.Reconstruct(
                Shadow(Loco(100, 10)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                Topology("routes: 10>20"),
                mapping,
                TwoBlocks,
                new Dictionary<int, int> { [100] = 1 });

            Assert.Equal(1, footprint[1]);
            Assert.Equal(1, footprint[2]);
        }

        [Fact]
        public void Startup_TrainSpanningBlockBoundary_FootprintCrossesBlocks()
        {
            var mapping = Mapping((1, 1), (2, 2), (3, 4), (4, 6));
            var footprint = PhysicalFootprintReconstructor.Reconstruct(
                Shadow(Loco(100, 10)), // A occupies block 10 (section 2 physically occupied)
                Physical(
                    (1, PhysicalOccupancy.Clear), (2, PhysicalOccupancy.Occupied),
                    (3, PhysicalOccupancy.Occupied), (4, PhysicalOccupancy.Clear)),
                Topology("routes: 10>20"),
                mapping,
                TwoBlocks,
                new Dictionary<int, int> { [100] = 1 });

            Assert.Equal(1, footprint[2]); // occupied section of block 10
            Assert.Equal(1, footprint[3]); // adjacent Free section of block 20 (cross-block)
            Assert.False(footprint.ContainsKey(1));
            Assert.False(footprint.ContainsKey(4));
        }

        [Fact]
        public void Startup_SpeedZeroThenFirstMovement_UsesCompleteEligibleDriveGroup()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker, new Dictionary<int, int> { [100] = 1 },
                "routes: 10>20", Mapping((1, 1), (2, 2)), TwoBlocks);

            // Cold start: A occupies block 10 spanning both sections, speed 0.
            controller.OnShadowChanged(Shadow(Loco(100, 10)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));
            Assert.Equal(Neutral, Pwm(variables, 1));
            Assert.Equal(Neutral, Pwm(variables, 2));
            Assert.DoesNotContain(controller.LastDecision!.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);

            // First movement: the COMPLETE drive group (both amps) is powered.
            controller.SetLocoSpeed(1, 100, 0);
            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, Pwm(variables, 1));
            Assert.Equal(pwm, Pwm(variables, 2));
        }

        // -----------------------------------------------------------------
        // §48 Authority suspect coherence
        // -----------------------------------------------------------------

        [Fact]
        public void AuthoritySuspect_DriveGroupStaysCoherent_RecoverNoLatch()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var controller = CreateController(
                variables, tracker, new Dictionary<int, int> { [100] = 1 },
                "routes: 10>20", Mapping((1, 1), (2, 2)), TwoBlocks,
                clock: () => clock.UtcNow);

            controller.OnShadowChanged(Shadow(Loco(100, 10)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));
            controller.SetLocoSpeed(1, 100, 0);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, Pwm(variables, 1));
            Assert.Equal(pwm, Pwm(variables, 2));

            // Single invalid shadow -> Suspect: no drive-group member disappears.
            controller.OnShadowChanged(InvalidShadow());
            Assert.Equal(AuthorityLifecycleState.AuthoritySuspect, controller.AuthorityLifecycle);
            Assert.Equal(pwm, Pwm(variables, 1));
            Assert.Equal(pwm, Pwm(variables, 2));

            // Recover -> Valid, no latch.
            controller.OnShadowChanged(Shadow(Loco(100, 10)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);
            Assert.Equal(pwm, Pwm(variables, 1));
            Assert.Equal(pwm, Pwm(variables, 2));
        }

        [Fact]
        public void AuthoritySuspect_PersistentLoss_Latches_AllMembersNeutral()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var controller = CreateController(
                variables, tracker, new Dictionary<int, int> { [100] = 1 },
                "routes: 10>20", Mapping((1, 1), (2, 2)), TwoBlocks,
                clock: () => clock.UtcNow);

            controller.OnShadowChanged(Shadow(Loco(100, 10)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));
            controller.SetLocoSpeed(1, 100, 0);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, Pwm(variables, 1));
            Assert.Equal(pwm, Pwm(variables, 2));

            controller.OnShadowChanged(InvalidShadow());
            clock.UtcNow = clock.UtcNow.AddMilliseconds(1000);
            controller.OnShadowChanged(InvalidShadow());
            controller.OnShadowChanged(InvalidShadow());
            Assert.Equal(AuthorityLifecycleState.AuthorityLostAfterValid, controller.AuthorityLifecycle);

            Assert.Equal(Neutral, Pwm(variables, 1));
            Assert.Equal(Neutral, Pwm(variables, 2));
        }
    }
}
