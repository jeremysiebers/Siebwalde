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
    /// Integration-style offline tests for the stateful <see cref="LocomotiveCarrierController"/>
    /// (Phase 2): speed / shadow / occupancy changes all funnel into the pure projector, and the
    /// thin writer applies only the projector-authorized physical targets. No hardware, no comm.
    /// </summary>
    public class LocomotiveCarrierControllerTests
    {
        private const int Neutral = AmplifierSpeedMapper.NeutralPwm;

        private static LogicalLocomotiveShadow Loco(int id, int? occupied, params int[] reserved)
            => ReconciliationTestFixtures.Loco(id, occupied, reserved);

        private static KoploperLogicalSectionShadowObservation Shadow(params LogicalLocomotiveShadow[] locos)
            => ReconciliationTestFixtures.Shadow(locomotives: locos, profileId: "Simple Loop");

        private static PhysicalSectionOccupancyObservation Physical(params (int Section, PhysicalOccupancy Occ)[] states)
            => ReconciliationTestFixtures.Physical(
                states.Select(s => ReconciliationTestFixtures.PhysicalState(s.Section, s.Occ)).ToArray());

        private static LocomotiveCarrierController CreateController(
            TrackApplicationVariables variables,
            AmplifierCommandTracker tracker,
            Dictionary<int, int> identity,
            string topology,
            (int Section, int Amp)[] mapping,
            TimeSpan? tailHoldWindow = null,
            Func<DateTimeOffset>? clock = null,
            IReadOnlyDictionary<int, IReadOnlyList<int>>? blockToSections = null)
        {
            var parsedTopology = BlockTopology.Parse(topology);
            var parsedMapping = mapping
                .Select(p => new LayoutPhysicalAmplifierBinding { SectionId = p.Section, PhysicalAmplifier = p.Amp })
                .ToArray();
            return new LocomotiveCarrierController(
                variables, tracker, trace: null, identity, parsedTopology, parsedMapping,
                blockToSections ?? ReconciliationTestFixtures.BlockMap(mapping.Select(p => p.Section).ToArray()),
                tailHoldWindow: tailHoldWindow, clock: clock);
        }

        [Fact]
        public void SetLocoSpeed_DrivesOnlyProjectorAuthorizedAmps_NotTopologyNext()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            controller.OnShadowChanged(Shadow(Loco(10, 1))); // A occupies section 1, no reserved
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));

            bool accepted = controller.SetLocoSpeed(1, 100, 0);

            Assert.True(accepted);
            Assert.Equal(100, controller.DesiredLocoStates[1].Speed);

            // amp1 (occupied section 1) is driven; amp3 (topology-next block 2) is NOT driven.
            Assert.NotEqual(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.Equal(Neutral, variables.PendingWrites[3].Hr0Value & 0x03FF);
        }

        [Fact]
        public void ShadowChange_ReservedByOtherLoco_BlocksA()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1, [20] = 2 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            controller.OnShadowChanged(Shadow(Loco(10, 1), Loco(20, null, 2))); // A occupied 1, B reserved 2
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));

            controller.SetLocoSpeed(1, 100, 0); // A speed 100 (B has no speed -> neutral)

            // amp1 (A's occupied) driven; amp3 (B's reserved) NOT driven by A.
            Assert.NotEqual(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.Equal(Neutral, variables.PendingWrites[3].Hr0Value & 0x03FF);
        }

        [Fact]
        public void OccupancyChange_FreeOccupied_RevokesLocally_NoLayoutStop()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int>(),
                "amps: 1:1",
                new[] { (1, 1) });

            controller.OnShadowChanged(Shadow()); // no locomotives
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied))); // free section occupied

            var decision = controller.LastDecision;
            Assert.NotNull(decision);
            Assert.False(decision!.LayoutStopRequired);
            Assert.Equal(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.Contains(decision.Diagnostics, d => d.Contains("ContradictionPhysicalWithoutLogicalOwner"));
        }

        [Fact]
        public void GetObservedHr0_CapturesFreshProtocolReadback()
        {
            var variables = new TrackApplicationVariables();
            var amp = variables.trackAmpItems.First(a => a.SlaveNumber == 3);
            amp.ProtocolReadbackObserved = true;
            amp.LastDataReceivedUtc = DateTimeOffset.UtcNow;
            var regs = new ushort[12];
            regs[TrackAmplifierRegisters.PwmCommand] = 650;
            amp.HoldingReg = regs;

            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker, new Dictionary<int, int>(), "amps: 1:1", new[] { (1, 1) });

            var observed = controller.GetObservedHr0();

            var entry = Assert.Single(observed, o => o.Amplifier == 3);
            Assert.Equal(650, entry.ObservedHr0);
            Assert.True(entry.Fresh);
        }

        [Fact]
        public void DepartingSource_FilteredClear_HeldThenReleasedAfterTail()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                tailHoldWindow: TimeSpan.FromMilliseconds(500),
                clock: () => clock.UtcNow);

            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));
            controller.SetLocoSpeed(1, 100, 0);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, variables.PendingWrites[1].Hr0Value & 0x03FF);

            // A departs section 1 (KIS occupies section 2); section 1 still physically Occupied -> held.
            controller.OnShadowChanged(Shadow(Loco(10, 2)));
            Assert.Equal(pwm, variables.PendingWrites[1].Hr0Value & 0x03FF); // not 399

            // Section 1 becomes filtered Clear -> tail hold starts (clock stays at Now).
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Clear), (2, PhysicalOccupancy.Occupied)));
            Assert.Equal(pwm, variables.PendingWrites[1].Hr0Value & 0x03FF); // tail hold

            // Still within the 500 ms tail window.
            clock.UtcNow = clock.UtcNow.AddMilliseconds(400);
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Clear), (2, PhysicalOccupancy.Occupied)));
            Assert.Equal(pwm, variables.PendingWrites[1].Hr0Value & 0x03FF); // still held

            // Tail window elapsed -> released to neutral (399).
            clock.UtcNow = clock.UtcNow.AddMilliseconds(200); // 600 ms since tail start
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Clear), (2, PhysicalOccupancy.Occupied)));
            Assert.Equal(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);
        }

        [Fact]
        public void DepartingSource_SpeedUpdate_BothHeldSourceAndTargetGetNewSpeed()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));
            controller.SetLocoSpeed(1, 100, 0);

            int pwm100 = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm100, variables.PendingWrites[1].Hr0Value & 0x03FF); // amp1 (occupied)
            Assert.Equal(pwm100, variables.PendingWrites[3].Hr0Value & 0x03FF); // amp3 (reserved pre-energize)

            // A's train enters section 2 (T): A now occupies 2; section 1 (S) is departed but still
            // physically occupied (rear crossing). Both S and T stay driven.
            controller.OnShadowChanged(Shadow(Loco(10, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));
            Assert.Equal(pwm100, variables.PendingWrites[1].Hr0Value & 0x03FF); // S held at 100
            Assert.Equal(pwm100, variables.PendingWrites[3].Hr0Value & 0x03FF); // T driven at 100

            // Speed change: both the held source (S) and the occupied target (T) get the new speed.
            controller.SetLocoSpeed(1, 80, 0);
            int pwm80 = AmplifierSpeedMapper.ToPwm(80, 0);
            Assert.Equal(pwm80, variables.PendingWrites[1].Hr0Value & 0x03FF); // S held at 80
            Assert.Equal(pwm80, variables.PendingWrites[3].Hr0Value & 0x03FF); // T driven at 80
        }

        [Fact]
        public void DepartingSource_SpeedZero_NeutralizesImmediately()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));
            controller.SetLocoSpeed(1, 100, 0);
            Assert.NotEqual(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);

            // A departs section 1; section 1 still physically Occupied -> held.
            controller.OnShadowChanged(Shadow(Loco(10, 2)));
            Assert.NotEqual(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);

            // Speed zero overrides the hold immediately (normal stop path).
            controller.SetLocoSpeed(1, 0, 0);
            Assert.Equal(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);
        }

        [Fact]
        public void BackplaneAmplifier_NeverWritten()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:51",
                new[] { (1, 51) });

            controller.SetLocoSpeed(1, 100, 0);
            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied)));

            // Backplane address 51 must never receive a carrier write.
            Assert.False(variables.PendingWrites.ContainsKey(51));
        }

        // -----------------------------------------------------------------
        // Speed-zero repair (§23): a stopped spanning locomotive keeps its physical attribution
        // -----------------------------------------------------------------

        [Fact]
        public void SpeedZero_SpanningLoco_KeepsAttribution_NoFalseIntrusion()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));
            controller.SetLocoSpeed(1, 100, 0);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, variables.PendingWrites[1].Hr0Value & 0x03FF); // S driven
            Assert.Equal(pwm, variables.PendingWrites[3].Hr0Value & 0x03FF); // T driven (boundary lead)

            // KIS advances to T: S departed, still physically occupied -> held driven.
            controller.OnShadowChanged(Shadow(Loco(10, 2)));
            Assert.Equal(pwm, variables.PendingWrites[1].Hr0Value & 0x03FF);

            // Speed -> 0 while S+T still physically occupied: both neutralize, attribution retained.
            controller.SetLocoSpeed(1, 0, 0);
            Assert.Equal(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.Equal(Neutral, variables.PendingWrites[3].Hr0Value & 0x03FF);

            // Next reconciliation: NO Free+Occupied Revoke for S.
            var decision = controller.LastDecision!;
            Assert.DoesNotContain(decision.Amplifiers, d => d.Amplifier == 1 && d.Verdict == CarrierVerdict.Revoke);
            Assert.DoesNotContain(decision.Diagnostics, d => d.Contains("ContradictionPhysicalWithoutLogicalOwner"));

            // Stopped locomotive may remain spanning S+T indefinitely without false intrusion.
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));
            Assert.DoesNotContain(controller.LastDecision!.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);
        }

        [Fact]
        public void SpeedZero_SpanningLoco_SourceClear_ReleasesAttributionAfterTail()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                tailHoldWindow: TimeSpan.FromMilliseconds(250),
                clock: () => clock.UtcNow);

            controller.SetLocoSpeed(1, 100, 0);
            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));
            controller.OnShadowChanged(Shadow(Loco(10, 2))); // S departed
            controller.SetLocoSpeed(1, 0, 0); // speed zero

            // S becomes filtered Clear -> tail hold begins (attribution still held, neutral).
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Clear), (2, PhysicalOccupancy.Occupied)));
            Assert.DoesNotContain(
                controller.LastDecision!.Amplifiers,
                d => d.Amplifier == 1 && d.Verdict == CarrierVerdict.Revoke);

            // After the tail window the physical attribution is removed safely (Free+Clear, no intrusion).
            clock.UtcNow = clock.UtcNow.AddMilliseconds(300);
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Clear), (2, PhysicalOccupancy.Occupied)));
            Assert.Equal(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.DoesNotContain(
                controller.LastDecision!.Amplifiers,
                d => d.Amplifier == 1 && d.Verdict == CarrierVerdict.Revoke);
        }

        // -----------------------------------------------------------------
        // Startup / recovery reconstruction (§24)
        // -----------------------------------------------------------------

        [Fact]
        public void Startup_ColdStart_ReconstructsFootprint_NeutralThenFollowsCarrier()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            // Cold carrier start: S Occupied(A), T Reserved(A), physical S+T occupied, speed 0.
            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));

            // Footprint alone with speed 0: both outputs remain neutral, no intrusion.
            Assert.Equal(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.Equal(Neutral, variables.PendingWrites[3].Hr0Value & 0x03FF);
            Assert.DoesNotContain(controller.LastDecision!.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);

            // Valid speed later: outputs follow normal carrier semantics (S driven, T boundary lead).
            controller.SetLocoSpeed(1, 100, 0);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(100, 0), variables.PendingWrites[3].Hr0Value & 0x03FF);
        }

        // -----------------------------------------------------------------
        // User-placed locomotive (§26)
        // -----------------------------------------------------------------

        [Fact]
        public void UserPlacedLoco_TwoAdjacentAmps_NeutralFootprint_NoRevoke()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            // User places a stopped locomotive over two adjacent amps BEFORE carrier startup:
            // KIS only reports S=Occupied(A); T is Free but physically occupied.
            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));

            // Reconstructable -> neutral footprint, no local unexpected-occupancy intrusion.
            Assert.Equal(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.Equal(Neutral, variables.PendingWrites[3].Hr0Value & 0x03FF);
            Assert.DoesNotContain(controller.LastDecision!.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);
        }

        // -----------------------------------------------------------------
        // Multi-locomotive (§25)
        // -----------------------------------------------------------------

        [Fact]
        public void MultiLoco_IndependentFootprints_NoCrossLocoAttribution()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1, [20] = 2 },
                "amps: 1:1,2:3,3:4 ; routes: 1>2,2>3",
                new[] { (1, 1), (2, 3), (3, 4) });

            // Loc A stopped spanning sections 1+2; Loc B independently owns section 3.
            controller.SetLocoSpeed(1, 0, 0);
            controller.SetLocoSpeed(2, 0, 0);
            controller.OnShadowChanged(Shadow(Loco(10, 1, 2), Loco(20, 3)));
            controller.OnOccupancyChanged(Physical(
                (1, PhysicalOccupancy.Occupied),
                (2, PhysicalOccupancy.Occupied),
                (3, PhysicalOccupancy.Occupied)));

            var decision = controller.LastDecision!;

            // Section 2 is attributed to A (its reserved extension), not to B.
            var amp3 = Assert.Single(decision.Amplifiers, d => d.Amplifier == 3);
            Assert.Equal(1, amp3.OwnerDcc);
            Assert.Equal(Neutral, amp3.Hr0);

            // Section 3 (amp 4) is attributed to B.
            var amp4 = Assert.Single(decision.Amplifiers, d => d.Amplifier == 4);
            Assert.Equal(2, amp4.OwnerDcc);

            // No false global conflict.
            Assert.False(decision.LayoutStopRequired);
            Assert.DoesNotContain(decision.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);
        }

        [Fact]
        public void MultiLoco_TwoLocosEachSpanningPair_NoFalseGlobalConflict()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1, [20] = 2 },
                "amps: 1:1,2:3,3:4,4:6 ; routes: 1>2,2>3,3>4,4>1",
                new[] { (1, 1), (2, 3), (3, 4), (4, 6) });

            controller.SetLocoSpeed(1, 0, 0);
            controller.SetLocoSpeed(2, 0, 0);
            controller.OnShadowChanged(Shadow(Loco(10, 1, 2), Loco(20, 3, 4)));
            controller.OnOccupancyChanged(Physical(
                (1, PhysicalOccupancy.Occupied),
                (2, PhysicalOccupancy.Occupied),
                (3, PhysicalOccupancy.Occupied),
                (4, PhysicalOccupancy.Occupied)));

            var decision = controller.LastDecision!;

            Assert.False(decision.LayoutStopRequired);
            Assert.DoesNotContain(decision.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);
            Assert.DoesNotContain(decision.Amplifiers, d => d.Verdict == CarrierVerdict.Conflict);

            // Four sections, all neutral, each attributed to the right locomotive.
            Assert.Equal(1, Assert.Single(decision.Amplifiers, d => d.Amplifier == 1).OwnerDcc);
            Assert.Equal(1, Assert.Single(decision.Amplifiers, d => d.Amplifier == 3).OwnerDcc);
            Assert.Equal(2, Assert.Single(decision.Amplifiers, d => d.Amplifier == 4).OwnerDcc);
            Assert.Equal(2, Assert.Single(decision.Amplifiers, d => d.Amplifier == 6).OwnerDcc);
        }

        // -----------------------------------------------------------------
        // (E) User places A across S+T before startup: first departure powers the full footprint
        // -----------------------------------------------------------------

        [Fact]
        public void UserPlacedLoco_FirstDeparture_PowersCompleteFootprint_RequirementE()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            // User placed A across S (Occupied) and T (Free) before startup.
            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));

            // Cold start, speed 0: both neutral, no intrusion.
            Assert.Equal(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.Equal(Neutral, variables.PendingWrites[3].Hr0Value & 0x03FF);
            Assert.DoesNotContain(controller.LastDecision!.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);

            // First departure: the complete eligible reconstructed footprint is powered.
            controller.SetLocoSpeed(1, 100, 0);
            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, variables.PendingWrites[1].Hr0Value & 0x03FF); // S
            Assert.Equal(pwm, variables.PendingWrites[3].Hr0Value & 0x03FF); // T
        }

        // -----------------------------------------------------------------
        // (I) After movement, old footprint sections shrink through the 250ms tail release lifecycle
        // -----------------------------------------------------------------

        [Fact]
        public void FootprintSection_Clear_TailReleaseLifecycle_RequirementI()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                tailHoldWindow: TimeSpan.FromMilliseconds(250),
                clock: () => clock.UtcNow);

            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));
            controller.SetLocoSpeed(1, 100, 0);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, variables.PendingWrites[1].Hr0Value & 0x03FF); // S driven
            Assert.Equal(pwm, variables.PendingWrites[3].Hr0Value & 0x03FF); // T driven

            // T departs (filtered Clear): held driven during the tail hold, not released yet.
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));
            Assert.Equal(pwm, variables.PendingWrites[3].Hr0Value & 0x03FF); // still held

            // After the tail window: T is released to neutral and its attribution is dropped.
            clock.UtcNow = clock.UtcNow.AddMilliseconds(300);
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));
            Assert.Equal(Neutral, variables.PendingWrites[3].Hr0Value & 0x03FF);
            Assert.DoesNotContain(
                controller.LastDecision!.Amplifiers,
                d => d.Amplifier == 3 && d.Verdict == CarrierVerdict.Revoke);
        }

        // -----------------------------------------------------------------
        // (J) First-movement speed changes: all still-active footprint amps track current A speed
        // -----------------------------------------------------------------

        [Fact]
        public void Footprint_SpeedChange_AllActiveAmpsTrackCurrentPwm_RequirementJ()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));

            controller.SetLocoSpeed(1, 100, 0);
            int pwm100 = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm100, variables.PendingWrites[1].Hr0Value & 0x03FF); // S
            Assert.Equal(pwm100, variables.PendingWrites[3].Hr0Value & 0x03FF); // T

            // Speed change: every still-active footprint amp tracks the new PWM (no stale startup PWM).
            controller.SetLocoSpeed(1, 80, 0);
            int pwm80 = AmplifierSpeedMapper.ToPwm(80, 0);
            Assert.Equal(pwm80, variables.PendingWrites[1].Hr0Value & 0x03FF); // S
            Assert.Equal(pwm80, variables.PendingWrites[3].Hr0Value & 0x03FF); // T
        }

        // -----------------------------------------------------------------
        // (K) Speed returns to zero while footprint still spans: all drive neutral, attribution remains
        // -----------------------------------------------------------------

        [Fact]
        public void Footprint_SpeedZeroWhileSpanning_AllNeutralAttributionRemains_RequirementK()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = CreateController(
                variables, tracker,
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));
            controller.SetLocoSpeed(1, 100, 0);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.Equal(pwm, variables.PendingWrites[3].Hr0Value & 0x03FF);

            // Speed -> 0 while the train still spans S+T: both neutralize, attribution remains.
            controller.SetLocoSpeed(1, 0, 0);
            Assert.Equal(Neutral, variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.Equal(Neutral, variables.PendingWrites[3].Hr0Value & 0x03FF);

            var decision = controller.LastDecision!;
            Assert.DoesNotContain(decision.Amplifiers, d => d.Verdict == CarrierVerdict.Revoke);
            Assert.Equal(1, Assert.Single(decision.Amplifiers, d => d.Amplifier == 1).OwnerDcc);
            Assert.Equal(1, Assert.Single(decision.Amplifiers, d => d.Amplifier == 3).OwnerDcc);
        }
    }
}
