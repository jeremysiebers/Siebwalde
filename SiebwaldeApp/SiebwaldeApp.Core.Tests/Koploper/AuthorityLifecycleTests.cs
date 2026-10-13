using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Deterministic offline tests for the startup authority-lifecycle repair: the carrier's
    /// drive authority distinguishes <c>AUTHORITY_NOT_YET_OBSERVED</c> (startup, fail-closed but
    /// no latched fault) from <c>AUTHORITY_LOST_AFTER_VALID</c> (operational loss, latched layout
    /// stop, no auto-resume). No hardware, no comm, no timing.
    /// </summary>
    public class AuthorityLifecycleTests
    {
        private const int Neutral = AmplifierSpeedMapper.NeutralPwm;

        private sealed class RecordingStopSink : ISafetyStopSink
        {
            public int LocoStops { get; private set; }
            public int LayoutStops { get; private set; }

            public SafetyStopResult StopLoco(int address)
            {
                LocoStops++;
                return SafetyStopResult.Commanded(new ushort[] { (ushort)address });
            }

            public SafetyStopResult StopLayout()
            {
                LayoutStops++;
                return SafetyStopResult.Commanded(new ushort[] { 1 });
            }
        }

        private sealed class RecordingBackend : IHardwareBackend
        {
            public List<(int Address, int Speed, int Direction)> LocoCommands { get; } = new();

            public bool SetPower(bool on) => true;

            public bool SetLocoSpeed(int address, int ecosSpeed, int direction)
            {
                LocoCommands.Add((address, ecosSpeed, direction));
                return true;
            }

            public bool SetSwitch(int decoderAddress, int outputIndex, bool on) => true;
        }

        private static LogicalLocomotiveShadow Loco(int id, int? occupied, params int[] reserved)
            => ReconciliationTestFixtures.Loco(id, occupied, reserved);

        private static KoploperLogicalSectionShadowObservation Shadow(params LogicalLocomotiveShadow[] locos)
            => ReconciliationTestFixtures.Shadow(locomotives: locos, profileId: "Simple Loop");

        private static KoploperLogicalSectionShadowObservation InvalidShadow()
            => ReconciliationTestFixtures.Shadow(locomotives: Array.Empty<LogicalLocomotiveShadow>(), shadowValid: false);

        private static PhysicalSectionOccupancyObservation Physical(params (int Section, PhysicalOccupancy Occ)[] states)
            => ReconciliationTestFixtures.Physical(
                states.Select(s => ReconciliationTestFixtures.PhysicalState(s.Section, s.Occ)).ToArray());

        private static (LocomotiveCarrierController Controller, TrackApplicationVariables Variables, AmplifierCommandTracker Tracker)
            CreateCarrier(
                Dictionary<int, int> identity,
                string topology,
                (int Section, int Amp)[] mapping,
                IControlTrace? trace = null,
                Func<DateTimeOffset>? clock = null,
                int consecutiveInvalidRequired = 3,
                TimeSpan? authorityInvalidMinimumDuration = null)
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var controller = new LocomotiveCarrierController(
                variables,
                tracker,
                trace,
                identity,
                BlockTopology.Parse(topology),
                mapping
                    .Select(p => new LayoutPhysicalAmplifierBinding { SectionId = p.Section, PhysicalAmplifier = p.Amp })
                    .ToArray(),
                clock: clock,
                consecutiveInvalidRequired: consecutiveInvalidRequired,
                authorityInvalidMinimumDuration: authorityInvalidMinimumDuration);
            return (controller, variables, tracker);
        }

        /// <summary>
        /// Builds a VALID, DRIVING carrier: loco A (DCC 1) occupies section 1 and reserves section 2,
        /// speed 100, so both amplifier 1 (occupied) and amplifier 3 (reserved pre-energize) are driven.
        /// </summary>
        private static (LocomotiveCarrierController Controller, TrackApplicationVariables Variables) DrivingCarrier(
            Func<DateTimeOffset>? clock = null,
            int consecutiveInvalidRequired = 3,
            TimeSpan? authorityInvalidMinimumDuration = null)
        {
            var (controller, variables, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                clock: clock,
                consecutiveInvalidRequired: consecutiveInvalidRequired,
                authorityInvalidMinimumDuration: authorityInvalidMinimumDuration);

            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));
            controller.SetLocoSpeed(1, 100, 0);

            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);
            Assert.NotEqual(Neutral, Pwm(variables, 1));
            Assert.NotEqual(Neutral, Pwm(variables, 3));

            return (controller, variables);
        }

        private static (ControlSafetyGuard Guard, RecordingStopSink Sink) CreateGuard()
        {
            var sink = new RecordingStopSink();
            var guard = new ControlSafetyGuard(sink, new ControlDiagnostics());
            return (guard, sink);
        }

        private static int Pwm(TrackApplicationVariables variables, int amp)
            => variables.PendingWrites.TryGetValue((ushort)amp, out var write)
                ? write.Hr0Value & 0x03FF
                : Neutral;

        // -----------------------------------------------------------------
        // 1. Startup, no KIS snapshot: neutral, movement denied, NO latch.
        // -----------------------------------------------------------------

        [Fact]
        public void Startup_NoSnapshot_DeniesMovement_WithoutLatch()
        {
            var (controller, variables, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            Assert.Equal(AuthorityLifecycleState.AuthorityNotYetObserved, controller.AuthorityLifecycle);
            Assert.True(controller.IsAuthorityNotReady);

            // A startup non-zero speed command is refused and NOT retained.
            Assert.False(controller.SetLocoSpeed(1, 100, 0));
            Assert.Empty(controller.DesiredLocoStates);

            // A shadow-invalid decision routes as not-ready (no layout stop latch).
            controller.OnShadowChanged(null);
            var decision = controller.LastDecision!;
            Assert.True(decision.LayoutStopRequired);

            var (guard, sink) = CreateGuard();
            CarrierEscalation.Route(decision, guard, controller.AuthorityLifecycle);

            Assert.False(guard.IsLatched);
            Assert.False(guard.IsLayoutLatched);
            Assert.Equal(0, sink.LayoutStops);
            Assert.Equal(Neutral, Pwm(variables, 1));
            Assert.Equal(Neutral, Pwm(variables, 3));
        }

        // -----------------------------------------------------------------
        // 2. Multiple no-KIS cycles: stay neutral/not-ready, no repeated latch.
        // -----------------------------------------------------------------

        [Fact]
        public void Startup_MultipleNoSnapshotCycles_NeverLatches()
        {
            var (controller, variables, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            var (guard, sink) = CreateGuard();

            for (int i = 0; i < 5; i++)
            {
                controller.OnShadowChanged(null);
                controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied)));
                CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);
            }

            Assert.Equal(AuthorityLifecycleState.AuthorityNotYetObserved, controller.AuthorityLifecycle);
            Assert.False(guard.IsLatched);
            Assert.Equal(0, sink.LayoutStops);
            Assert.Equal(Neutral, Pwm(variables, 1));
        }

        // -----------------------------------------------------------------
        // 3. Initial KIS input invalid before any valid snapshot: no operational-loss latch.
        // -----------------------------------------------------------------

        [Fact]
        public void Startup_InitialInvalidShadow_NoOperationalLossLatch()
        {
            var (controller, _, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            controller.OnShadowChanged(InvalidShadow());

            Assert.Equal(AuthorityLifecycleState.AuthorityNotYetObserved, controller.AuthorityLifecycle);
            Assert.True(controller.IsAuthorityNotReady);
            Assert.False(controller.SetLocoSpeed(1, 100, 0));

            var (guard, sink) = CreateGuard();
            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);

            Assert.False(guard.IsLatched);
            Assert.Equal(0, sink.LayoutStops);
        }

        // -----------------------------------------------------------------
        // 4. First valid KIS: -> VALID, startup inhibit clears, movement allowed.
        // -----------------------------------------------------------------

        [Fact]
        public void FirstValidShadow_BecomesValid_MovementAllowed()
        {
            var (controller, variables, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));

            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);
            Assert.False(controller.IsAuthorityNotReady);

            Assert.True(controller.SetLocoSpeed(1, 100, 0));
            Assert.Equal(100, controller.DesiredLocoStates[1].Speed);
            Assert.NotEqual(Neutral, Pwm(variables, 1));
        }

        // -----------------------------------------------------------------
        // 5. Non-zero speed refused while not-ready, then first valid KIS: no auto-start.
        // -----------------------------------------------------------------

        [Fact]
        public void RefusedStartupSpeed_NotReplayed_WhenAuthorityBecomesValid()
        {
            var (controller, variables, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            // Denied at startup: not retained.
            Assert.False(controller.SetLocoSpeed(1, 100, 0));
            Assert.Empty(controller.DesiredLocoStates);

            // First valid KIS arrives: the train must NOT auto-start.
            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);
            Assert.Equal(Neutral, Pwm(variables, 1));

            // A fresh valid command is required (and accepted).
            Assert.True(controller.SetLocoSpeed(1, 100, 0));
            Assert.NotEqual(Neutral, Pwm(variables, 1));
        }

        // -----------------------------------------------------------------
        // 6. VALID -> INVALID: layout StopRequired/latch, movement revoked.
        // -----------------------------------------------------------------

        [Fact]
        public void ValidToInvalid_LatchesLayoutStop_AndRevokesMovement()
        {
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, variables, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                clock: () => clock.UtcNow);

            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));
            controller.SetLocoSpeed(1, 100, 0);
            Assert.NotEqual(Neutral, Pwm(variables, 1));

            // Operational loss requires N+X qualification (>=3 consecutive invalid + >=1000 ms).
            controller.OnShadowChanged(InvalidShadow());
            clock.UtcNow = clock.UtcNow.AddMilliseconds(1000);
            controller.OnShadowChanged(InvalidShadow());
            controller.OnShadowChanged(InvalidShadow());
            Assert.Equal(AuthorityLifecycleState.AuthorityLostAfterValid, controller.AuthorityLifecycle);

            var (guard, sink) = CreateGuard();
            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);

            Assert.True(guard.IsLayoutLatched);
            Assert.Equal(1, sink.LayoutStops);
            Assert.Equal(Neutral, Pwm(variables, 1));
        }

        // -----------------------------------------------------------------
        // 7. VALID -> INVALID -> VALID: latch remains, NO auto-restart.
        // -----------------------------------------------------------------

        [Fact]
        public void ValidToInvalidToValid_LatchRemains_NoAutoRestart()
        {
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, variables, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                clock: () => clock.UtcNow);

            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));
            controller.SetLocoSpeed(1, 100, 0);

            // Qualify the loss (>=3 consecutive invalid + >=1000 ms).
            controller.OnShadowChanged(InvalidShadow());
            clock.UtcNow = clock.UtcNow.AddMilliseconds(1000);
            controller.OnShadowChanged(InvalidShadow());
            controller.OnShadowChanged(InvalidShadow());
            Assert.Equal(AuthorityLifecycleState.AuthorityLostAfterValid, controller.AuthorityLifecycle);

            var (guard, sink) = CreateGuard();
            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);
            Assert.True(guard.IsLayoutLatched);

            // Recovered: authority becomes valid again, but the latch must remain (no auto-restart).
            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);
            Assert.False(controller.IsAuthorityNotReady);

            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);
            Assert.True(guard.IsLayoutLatched);
            Assert.Equal(1, sink.LayoutStops); // no second stop; the latch is idempotent

            // The layout latch still blocks movement, and the retained drive state was cleared on
            // loss, so there is no auto-restart.
            Assert.False(guard.AllowsMovement(1));
            Assert.Equal(Neutral, Pwm(variables, 1));
        }

        // -----------------------------------------------------------------
        // 8. VALID -> INVALID with active physical footprint: normal neutralization.
        // -----------------------------------------------------------------

        [Fact]
        public void ValidToInvalid_WithActiveFootprint_NeutralizesAndLatches()
        {
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, variables, tracker) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                clock: () => clock.UtcNow);

            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)));
            controller.SetLocoSpeed(1, 100, 0);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, Pwm(variables, 1));
            Assert.Equal(pwm, Pwm(variables, 3));
            Assert.True(tracker.HasOutstanding(1));

            // Qualify the authority loss while the locomotive still has a physical footprint.
            controller.OnShadowChanged(InvalidShadow());
            clock.UtcNow = clock.UtcNow.AddMilliseconds(1000);
            controller.OnShadowChanged(InvalidShadow());
            controller.OnShadowChanged(InvalidShadow());

            var (guard, _) = CreateGuard();
            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);
            Assert.True(guard.IsLayoutLatched);

            // Both footprint amplifiers are commanded neutral.
            Assert.Equal(Neutral, Pwm(variables, 1));
            Assert.Equal(Neutral, Pwm(variables, 3));
        }

        // -----------------------------------------------------------------
        // 9. App begins INVALID -> later VALID: startup acquisition, no latch.
        // -----------------------------------------------------------------

        [Fact]
        public void InvalidThenValid_IsStartupAcquisition_NotOperationalLoss()
        {
            var (controller, _, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            var (guard, sink) = CreateGuard();

            controller.OnShadowChanged(InvalidShadow());
            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);
            Assert.False(guard.IsLatched);

            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);
            Assert.False(controller.IsAuthorityNotReady);

            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);
            Assert.False(guard.IsLatched);
            Assert.Equal(0, sink.LayoutStops);
        }

        // -----------------------------------------------------------------
        // 10. Startup KIS becomes valid while another latched fault present.
        // -----------------------------------------------------------------

        [Fact]
        public void StartupBecomesValid_OtherLatchedFault_RemainsAndBlocksMovement()
        {
            var (guard, _) = CreateGuard();
            guard.Apply(new ControlDiagnostic
            {
                Code = DiagnosticCode.StateUnknown,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "carrier-other-fault",
                LocoAddress = null,
                Detail = "An independent latched fault."
            });
            Assert.True(guard.IsLatched);

            var (controller, _, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);

            // Routing the valid decision must not clear the independent fault.
            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);

            Assert.True(guard.IsLatched);
            Assert.False(guard.AllowsMovement(1));
            Assert.Contains("StateUnknown|carrier-other-fault", guard.LatchedKeys);
        }

        // -----------------------------------------------------------------
        // 11. Startup KIS becomes valid with no other fault: permission available.
        // -----------------------------------------------------------------

        [Fact]
        public void StartupBecomesValid_NoOtherFault_MovementPermissionAvailable()
        {
            var (controller, _, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            var (guard, _) = CreateGuard();

            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);

            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);

            Assert.False(guard.IsLatched);
            Assert.True(guard.AllowsMovement(1));
        }

        // -----------------------------------------------------------------
        // 12. KIS valid does not clear PIC32/system/other unrelated fault state.
        // -----------------------------------------------------------------

        [Fact]
        public void KisValid_DoesNotClearUnrelatedFaultState()
        {
            var (guard, _) = CreateGuard();
            guard.Apply(new ControlDiagnostic
            {
                Code = DiagnosticCode.BackendUnavailable,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "pic32-comm",
                LocoAddress = null,
                Detail = "PIC32 communication unavailable."
            });
            Assert.True(guard.IsLatched);

            var (controller, _, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);

            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);

            Assert.True(guard.IsLatched);
            Assert.Contains("BackendUnavailable|pic32-comm", guard.LatchedKeys);
            Assert.False(guard.AllowsMovement(1));
        }

        // -----------------------------------------------------------------
        // Movement gate: ControlSafetyInterlockBackend denies non-zero movement
        // during NOT_OBSERVED without a latch.
        // -----------------------------------------------------------------

        [Fact]
        public void Interlock_AuthorityNotReady_DeniesNonZeroMovement_WithoutLatch()
        {
            var guard = new ControlSafetyGuard(new RecordingStopSink(), new ControlDiagnostics());
            var inner = new RecordingBackend();
            var interlock = new ControlSafetyInterlockBackend(
                inner, guard, new ControlDiagnostics(),
                authorityNotReady: () => true);

            Assert.False(interlock.SetLocoSpeed(42, 40, 0));
            Assert.True(interlock.IsMovementBlocked(42));
            Assert.Empty(inner.LocoCommands);
            Assert.False(guard.IsLatched); // no latch

            // A stop command is always allowed.
            Assert.True(interlock.SetLocoSpeed(42, 0, 0));
            Assert.Equal(new[] { (42, 0, 0) }, inner.LocoCommands);
        }

        [Fact]
        public void Interlock_AuthorityReady_AllowsNonZeroMovement()
        {
            var guard = new ControlSafetyGuard(new RecordingStopSink(), new ControlDiagnostics());
            var inner = new RecordingBackend();
            var interlock = new ControlSafetyInterlockBackend(
                inner, guard, new ControlDiagnostics(),
                authorityNotReady: () => false);

            Assert.True(interlock.SetLocoSpeed(42, 40, 0));
            Assert.False(interlock.IsMovementBlocked(42));
            Assert.Equal(new[] { (42, 40, 0) }, inner.LocoCommands);
        }

        // -----------------------------------------------------------------
        // N+X authority-loss qualification (KIS authority-loss repair).
        // A single transient invalid shadow must NOT latch a layout stop.
        // -----------------------------------------------------------------

        [Fact]
        public void NxQualification_OneInvalidThenValid_RecoversWithoutLatch()
        {
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, _) = DrivingCarrier(() => clock.UtcNow);

            controller.OnShadowChanged(InvalidShadow());
            Assert.Equal(AuthorityLifecycleState.AuthoritySuspect, controller.AuthorityLifecycle);
            Assert.False(controller.IsAuthorityNotReady);

            var (guard, sink) = CreateGuard();
            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);
            Assert.False(guard.IsLatched);

            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);
            Assert.False(guard.IsLatched);
            Assert.Equal(0, sink.LayoutStops);
        }

        [Fact]
        public void NxQualification_TwoInvalidSamplesThenValid_NoConfirmedLoss()
        {
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, _) = DrivingCarrier(() => clock.UtcNow);

            controller.OnShadowChanged(InvalidShadow()); // count 1
            controller.OnShadowChanged(InvalidShadow()); // count 2
            Assert.Equal(AuthorityLifecycleState.AuthoritySuspect, controller.AuthorityLifecycle);

            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);
        }

        [Fact]
        public void NxQualification_InvalidFor560MsThenValid_NoLatch()
        {
            // Models the actual V4 transient: a single ~0.56 s shadow invalidity.
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, _) = DrivingCarrier(() => clock.UtcNow);

            controller.OnShadowChanged(InvalidShadow());
            Assert.Equal(AuthorityLifecycleState.AuthoritySuspect, controller.AuthorityLifecycle);

            clock.UtcNow = clock.UtcNow.AddMilliseconds(560);
            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);

            var (guard, sink) = CreateGuard();
            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);
            Assert.False(guard.IsLatched);
            Assert.Equal(0, sink.LayoutStops);
        }

        [Fact]
        public void NxQualification_ThreeInvalidButWithinDuration_StillSuspect_NoLatch()
        {
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, _) = DrivingCarrier(() => clock.UtcNow);

            controller.OnShadowChanged(InvalidShadow()); // count 1
            controller.OnShadowChanged(InvalidShadow()); // count 2
            controller.OnShadowChanged(InvalidShadow()); // count 3 but elapsed 0 < 1000 ms
            Assert.Equal(AuthorityLifecycleState.AuthoritySuspect, controller.AuthorityLifecycle);

            var (guard, _) = CreateGuard();
            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);
            Assert.False(guard.IsLatched);
        }

        [Fact]
        public void NxQualification_ThreeInvalidAndDurationMet_LatchesLayoutStop()
        {
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, variables) = DrivingCarrier(() => clock.UtcNow);

            controller.OnShadowChanged(InvalidShadow()); // count 1, firstInvalidAtUtc = Now
            clock.UtcNow = clock.UtcNow.AddMilliseconds(1000);
            controller.OnShadowChanged(InvalidShadow()); // count 2, elapsed 1000 ms
            controller.OnShadowChanged(InvalidShadow()); // count 3, elapsed 1000 ms -> Lost
            Assert.Equal(AuthorityLifecycleState.AuthorityLostAfterValid, controller.AuthorityLifecycle);

            var (guard, sink) = CreateGuard();
            CarrierEscalation.Route(controller.LastDecision!, guard, controller.AuthorityLifecycle);

            Assert.True(guard.IsLayoutLatched);
            Assert.Equal(1, sink.LayoutStops);
            Assert.Equal(Neutral, Pwm(variables, 1));
            Assert.Equal(Neutral, Pwm(variables, 3));
        }

        [Fact]
        public void NxQualification_ConfigurableThresholds_QualifyAtConfiguredValues()
        {
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, _) = DrivingCarrier(
                () => clock.UtcNow,
                consecutiveInvalidRequired: 2,
                authorityInvalidMinimumDuration: TimeSpan.Zero);

            controller.OnShadowChanged(InvalidShadow()); // count 1
            controller.OnShadowChanged(InvalidShadow()); // count 2 >= N and elapsed 0 >= X -> Lost

            Assert.Equal(AuthorityLifecycleState.AuthorityLostAfterValid, controller.AuthorityLifecycle);
        }

        // -----------------------------------------------------------------
        // Continuity during AuthoritySuspect (PO §6/§8).
        // -----------------------------------------------------------------

        [Fact]
        public void Suspect_EstablishedFootprint_KeepsDriving()
        {
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, variables) = DrivingCarrier(() => clock.UtcNow);

            int pwm = AmplifierSpeedMapper.ToPwm(100, 0);
            Assert.Equal(pwm, Pwm(variables, 1));
            Assert.Equal(pwm, Pwm(variables, 3));

            // A single invalid shadow must NOT collapse the established footprint.
            controller.OnShadowChanged(InvalidShadow());
            Assert.Equal(AuthorityLifecycleState.AuthoritySuspect, controller.AuthorityLifecycle);
            Assert.Equal(pwm, Pwm(variables, 1));
            Assert.Equal(pwm, Pwm(variables, 3));
        }

        [Fact]
        public void Suspect_SpeedIncreaseAndDirectionFrozen_StopImmediate()
        {
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, variables) = DrivingCarrier(() => clock.UtcNow);

            controller.OnShadowChanged(InvalidShadow());
            Assert.Equal(AuthorityLifecycleState.AuthoritySuspect, controller.AuthorityLifecycle);

            int pwm100 = AmplifierSpeedMapper.ToPwm(100, 0);

            // Higher speed refused (no authority-exposure increase).
            Assert.False(controller.SetLocoSpeed(1, 120, 0));
            Assert.Equal(100, controller.DesiredLocoStates[1].Speed);
            Assert.Equal(pwm100, Pwm(variables, 1));

            // Direction change refused.
            Assert.False(controller.SetLocoSpeed(1, 100, 1));
            Assert.Equal(0, controller.DesiredLocoStates[1].Direction);

            // Lower speed (decrease) allowed.
            Assert.True(controller.SetLocoSpeed(1, 80, 0));
            Assert.Equal(80, controller.DesiredLocoStates[1].Speed);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(80, 0), Pwm(variables, 1));

            // Stop (speed 0) is immediate.
            Assert.True(controller.SetLocoSpeed(1, 0, 0));
            Assert.Equal(Neutral, Pwm(variables, 1));
            Assert.Equal(Neutral, Pwm(variables, 3));
        }

        [Fact]
        public void Suspect_NewLocoMovement_Refused()
        {
            var clock = new MutableClock(ReconciliationTestFixtures.Now);
            var (controller, _) = DrivingCarrier(() => clock.UtcNow);

            controller.OnShadowChanged(InvalidShadow());
            Assert.Equal(AuthorityLifecycleState.AuthoritySuspect, controller.AuthorityLifecycle);

            // A locomotive with no retained drive state must not acquire new movement authority.
            Assert.False(controller.SetLocoSpeed(2, 80, 0));
            Assert.False(controller.DesiredLocoStates.ContainsKey(2));
        }

        // -----------------------------------------------------------------
        // Startup (AuthorityNotYetObserved) never routes through Suspect / Lost.
        // -----------------------------------------------------------------

        [Fact]
        public void Startup_MultipleInvalidThenValid_NeverSuspectNeverLatch()
        {
            var (controller, _, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) });

            var (guard, sink) = CreateGuard();

            controller.OnShadowChanged(InvalidShadow());
            controller.OnShadowChanged(InvalidShadow());
            controller.OnShadowChanged(InvalidShadow());
            Assert.Equal(AuthorityLifecycleState.AuthorityNotYetObserved, controller.AuthorityLifecycle);

            controller.OnShadowChanged(Shadow(Loco(10, 1)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);
            Assert.False(guard.IsLatched);
            Assert.Equal(0, sink.LayoutStops);
        }

        // -----------------------------------------------------------------
        // Trace: transition-level lifecycle events carry the N+X numbers.
        // -----------------------------------------------------------------

        [Fact]
        public void LifecycleTrace_EmitsTransitionSequence()
        {
            var factory = new ControlTraceFormattingTests.CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);
            var clock = new MutableClock(ReconciliationTestFixtures.Now);

            var (controller, _, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                trace: trace,
                clock: () => clock.UtcNow);

            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));   // AUTHORITY_VALID
            controller.OnShadowChanged(InvalidShadow());            // AUTHORITY_SUSPECT_ENTER (count 1)
            controller.OnShadowChanged(InvalidShadow());            // AUTHORITY_SUSPECT_CONTINUE (count 2)
            clock.UtcNow = clock.UtcNow.AddMilliseconds(1000);
            controller.OnShadowChanged(InvalidShadow());            // AUTHORITY_LOST_AFTER_VALID (count 3)
            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));    // AUTHORITY_RECOVERED_BUT_LATCH_REMAINS

            var messages = factory.Records
                .Where(r => r.Instance == ControlTraceLogger.LoggerInstance)
                .Select(r => r.Message)
                .Where(m => m.StartsWith("EVENT=CARRIER_AUTHORITY_LIFECYCLE"))
                .ToArray();

            Assert.Contains(messages, m => m.Contains(" transition=AUTHORITY_VALID"));
            Assert.Contains(messages, m => m.Contains(" transition=AUTHORITY_SUSPECT_ENTER") && m.Contains(" count=1"));
            Assert.Contains(messages, m => m.Contains(" transition=AUTHORITY_SUSPECT_CONTINUE") && m.Contains(" count=2"));
            Assert.Contains(messages, m => m.Contains(" transition=AUTHORITY_LOST_AFTER_VALID") && m.Contains(" count=3"));
            Assert.Contains(messages, m => m.Contains(" transition=AUTHORITY_RECOVERED_BUT_LATCH_REMAINS"));
        }

        // -----------------------------------------------------------------
        // N+X qualification: one shadow observation advances the lifecycle EXACTLY once.
        // The production feeder calls OnShadowChanged then OnOccupancyChanged every cycle; before
        // the repair, both funneled into Reconcile(), so the lifecycle advanced twice per shadow
        // observation (making N=3 effective ceil(3/2)=2). This is the feeder-equivalent sequence,
        // exercised deterministically without any background loop or timing.
        // -----------------------------------------------------------------

        [Fact]
        public void FeederSequence_OneShadowObservation_AdvancesLifecycleExactlyOnce()
        {
            var factory = new ControlTraceFormattingTests.CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);
            var clock = new MutableClock(ReconciliationTestFixtures.Now);

            var (controller, _, _) = CreateCarrier(
                new Dictionary<int, int> { [10] = 1 },
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                trace: trace,
                clock: () => clock.UtcNow);

            controller.OnShadowChanged(Shadow(Loco(10, 1, 2)));
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));
            Assert.Equal(AuthorityLifecycleState.AuthorityValid, controller.AuthorityLifecycle);
            Assert.Equal(0, controller.ConsecutiveInvalidCount);

            // One feeder cycle: a single invalid shadow observation followed by the occupancy feed.
            // OnOccupancyChanged must NOT advance the lifecycle a second time for the same shadow.
            controller.OnShadowChanged(InvalidShadow());
            controller.OnOccupancyChanged(Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Clear)));

            Assert.Equal(AuthorityLifecycleState.AuthoritySuspect, controller.AuthorityLifecycle);
            Assert.Equal(1, controller.ConsecutiveInvalidCount);

            var lifecycleMessages = factory.Records
                .Where(r => r.Instance == ControlTraceLogger.LoggerInstance)
                .Select(r => r.Message)
                .Where(m => m.StartsWith("EVENT=CARRIER_AUTHORITY_LIFECYCLE"))
                .ToArray();

            Assert.Single(lifecycleMessages, m => m.Contains(" transition=AUTHORITY_SUSPECT_ENTER"));
            Assert.Contains(lifecycleMessages, m => m.Contains(" transition=AUTHORITY_SUSPECT_ENTER") && m.Contains(" count=1"));
            Assert.DoesNotContain(lifecycleMessages, m => m.Contains(" transition=AUTHORITY_SUSPECT_CONTINUE"));
        }
    }
}
