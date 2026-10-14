using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Offline tests for Phase 3: the carrier feeder loop (feeds shadow/physical/observed-HR0 and
    /// routes escalation) and the escalation routing into the production safety latch. No hardware,
    /// no comm.
    /// </summary>
    public class LocomotiveCarrierFeederTests
    {
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

        [Fact]
        public void Route_LayoutStopRequired_RoutesToLayoutStop()
        {
            var sink = new RecordingStopSink();
            var guard = new ControlSafetyGuard(sink, new ControlDiagnostics());

            var decision = new CarrierDecision(
                Amplifiers: Array.Empty<CarrierAmplifierDecision>(),
                LayoutStopRequired: true,
                AnyLocoStopRequired: false,
                LatchedLocoIds: Array.Empty<int>(),
                Diagnostics: Array.Empty<string>(),
                PhysicalFootprint: new Dictionary<int, int>());

            CarrierEscalation.Route(decision, guard, AuthorityLifecycleState.AuthorityLostAfterValid);

            Assert.Equal(1, sink.LayoutStops);
            Assert.Equal(0, sink.LocoStops);
            Assert.True(guard.IsLayoutLatched);
        }

        [Fact]
        public void Route_LatchedLocoIds_RoutesToLocoStop()
        {
            var sink = new RecordingStopSink();
            var guard = new ControlSafetyGuard(sink, new ControlDiagnostics());

            var decision = new CarrierDecision(
                Amplifiers: Array.Empty<CarrierAmplifierDecision>(),
                LayoutStopRequired: false,
                AnyLocoStopRequired: true,
                LatchedLocoIds: new[] { 1, 2 },
                Diagnostics: Array.Empty<string>(),
                PhysicalFootprint: new Dictionary<int, int>());

            CarrierEscalation.Route(decision, guard, AuthorityLifecycleState.AuthorityValid);

            Assert.Equal(0, sink.LayoutStops);
            Assert.Equal(2, sink.LocoStops);
            Assert.Equal(2, guard.LatchedLocos.Count);
        }

        [Fact]
        public async Task Feeder_FeedsCarrier_AndInvokesEscalation()
        {
            var variables = new TrackApplicationVariables();
            var observer = BuildObserver(variables);
            var carrier = new LocomotiveCarrierController(
                variables,
                new AmplifierCommandTracker(),
                trace: null,
                new Dictionary<int, int> { [10] = 1 },
                BlockTopology.Parse("amps: 1:1,2:3 ; routes: 1>2"),
                new[]
                {
                    new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 },
                    new LayoutPhysicalAmplifierBinding { SectionId = 2, PhysicalAmplifier = 3 }
                },
                ReconciliationTestFixtures.BlockMap(1, 2));

            var escalations = new List<CarrierDecision>();
            var feeder = new LocomotiveCarrierFeeder(
                observer, carrier, decision => escalations.Add(decision), TimeSpan.FromMilliseconds(10));

            using var cts = new CancellationTokenSource();
            feeder.Start(cts.Token);

            try
            {
                await Task.Delay(150);
                await feeder.StopAsync();
            }
            finally
            {
                await feeder.StopAsync();
            }

            Assert.True(escalations.Count > 0);
            Assert.NotNull(carrier.Shadow);
            Assert.NotNull(carrier.Physical);
            Assert.NotNull(carrier.LastDecision);
        }

        [Fact]
        public async Task Feeder_WritesTimingCycleLine()
        {
            var variables = new TrackApplicationVariables();
            var observer = BuildObserver(variables);
            var carrier = new LocomotiveCarrierController(
                variables,
                new AmplifierCommandTracker(),
                trace: null,
                new Dictionary<int, int> { [10] = 1 },
                BlockTopology.Parse("amps: 1:1,2:3 ; routes: 1>2"),
                new[]
                {
                    new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 },
                    new LayoutPhysicalAmplifierBinding { SectionId = 2, PhysicalAmplifier = 3 }
                },
                ReconciliationTestFixtures.BlockMap(1, 2));

            string path = Path.Combine(Path.GetTempPath(), $"carrier-timing-{Guid.NewGuid():N}.jsonl");
            var timing = new CarrierTimingTrace(path);
            var feeder = new LocomotiveCarrierFeeder(
                observer, carrier,
                timingTrace: timing,
                interval: TimeSpan.FromMilliseconds(10));

            using var cts = new CancellationTokenSource();
            feeder.Start(cts.Token);

            try
            {
                await Task.Delay(150);
                await feeder.StopAsync();
            }
            finally
            {
                await feeder.StopAsync();
            }

            try
            {
                Assert.True(File.Exists(path));
                Assert.Contains("\"event\":\"carrier_cycle\"", File.ReadAllLines(path)[0]);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static Kis09PassiveObserver BuildObserver(TrackApplicationVariables variables)
        {
            var profile = new LayoutProfile
            {
                Name = "Simple Loop",
                PhysicalAmplifierMapping = new[]
                {
                    new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 },
                    new LayoutPhysicalAmplifierBinding { SectionId = 2, PhysicalAmplifier = 3 }
                },
                KoploperBlockBindings = new[]
                {
                    new KoploperBlockBinding(1, new[] { 1 }),
                    new KoploperBlockBinding(2, new[] { 2 })
                }
            };

            var fakeReservation = new FakeReservationObserver();
            var fakeShadow = new FakeKoploperLogicalSectionShadowObserver(
                ReconciliationTestFixtures.Shadow(profileId: "Simple Loop"));

            return new Kis09PassiveObserver(
                variables, profile, fakeReservation, fakeShadow,
                clock: () => ReconciliationTestFixtures.Now);
        }

        private sealed class FakeReservationObserver : IKoploperReservationObserver
        {
            public KoploperReservationObservation? CurrentObservation => null;

            public KoploperReservationObservation Refresh()
                => new KoploperReservationObservation(
                    Generation: null,
                    SourceSequence: 0,
                    ObserverSequence: 0,
                    CapturedAtUtc: ReconciliationTestFixtures.Now,
                    ObservedAtUtc: ReconciliationTestFixtures.Now,
                    IsFresh: false,
                    IsConsistent: false,
                    SourceHealth: KoploperSourceHealth.Unavailable,
                    IsAuthoritative: false,
                    AuthorityReason: KoploperReservationAuthorityReason.Unavailable,
                    Locomotives: Array.Empty<KoploperLocomotiveTrajectory>(),
                    Conflicts: Array.Empty<KoploperReservationConflict>(),
                    Diagnostics: Array.Empty<KoploperDiagnosticCode>());

            public void Start(CancellationToken cancellationToken) { }
            public Task StopAsync() => Task.CompletedTask;
            public void Dispose() { }
        }
    }
}
