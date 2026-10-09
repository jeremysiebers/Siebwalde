using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for <see cref="LogicalPhysicalReconciliationObserver"/>: no stale inheritance,
    /// monotonic reconciliation sequence, null-first non-assessable publication, read-failure
    /// recovery, and lifecycle idempotence. Uses a settable shadow observer, a scripted physical
    /// source and a fixed clock.
    /// </summary>
    public class LogicalPhysicalReconciliationObserverTests
    {
        private static readonly DateTimeOffset Now = ReconciliationTestFixtures.Now;
        private static readonly int[] BoundSections = { 1, 2, 3, 4 };

        private static LogicalPhysicalReconciliationObserver CreateObserver(
            FakeKoploperLogicalSectionShadowObserver shadowObserver,
            FakePhysicalSectionOccupancySource physicalSource,
            IReadOnlyCollection<int>? bound = null,
            LogicalPhysicalReconciliationObserverOptions? options = null,
            Func<DateTimeOffset>? clock = null)
            => new(
                shadowObserver,
                physicalSource,
                "simple-loop",
                bound ?? BoundSections,
                options,
                clock ?? (() => Now));

        private static KoploperLogicalSectionShadowObservation OccupiedShadow(int locoId = 10, int sectionId = 1)
            => ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(locoId, sectionId) });

        private static PhysicalSectionOccupancyObservation OccupiedPhysical(int sectionId = 1)
            => ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(sectionId, PhysicalOccupancy.Occupied) });

        [Fact]
        public void CurrentObservation_NullBeforeFirstRefresh()
        {
            var shadowObserver = new FakeKoploperLogicalSectionShadowObserver(OccupiedShadow());
            var physicalSource = new FakePhysicalSectionOccupancySource(() => OccupiedPhysical());
            using var observer = CreateObserver(shadowObserver, physicalSource);

            Assert.Null(observer.CurrentObservation);
        }

        [Fact]
        public void Refresh_ProducesAssessableReconciliation()
        {
            var shadowObserver = new FakeKoploperLogicalSectionShadowObserver(OccupiedShadow());
            var physicalSource = new FakePhysicalSectionOccupancySource(() => OccupiedPhysical());
            using var observer = CreateObserver(shadowObserver, physicalSource);

            LogicalPhysicalReconciliationObservation obs = observer.Refresh();

            Assert.True(obs.ReconciliationAssessable);
            Assert.True(obs.LogicalSourceValid);
            Assert.True(obs.PhysicalSourceValid);

            var section = Assert.Single(obs.Sections, s => s.LogicalSectionId == 1);
            Assert.Equal(LogicalSectionReconciliationState.ConsistOccupied, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.Eligible, section.SectionSafetyEligibility);
        }

        [Fact]
        public void Refresh_NullShadow_NonAssessable()
        {
            var shadowObserver = new FakeKoploperLogicalSectionShadowObserver(null);
            var physicalSource = new FakePhysicalSectionOccupancySource(() => OccupiedPhysical());
            using var observer = CreateObserver(shadowObserver, physicalSource);

            LogicalPhysicalReconciliationObservation obs = observer.Refresh();

            Assert.False(obs.ReconciliationAssessable);
            Assert.False(obs.LogicalSourceValid);
            Assert.Contains(ReconciliationDiagnosticCode.ReconLogicalSourceInvalid, obs.Diagnostics);
        }

        [Fact]
        public void ReconciliationSequence_MonotonicAcrossRefreshes()
        {
            var shadowObserver = new FakeKoploperLogicalSectionShadowObserver(OccupiedShadow());
            var physicalSource = new FakePhysicalSectionOccupancySource(() => OccupiedPhysical());
            using var observer = CreateObserver(shadowObserver, physicalSource);

            Assert.Equal(1, observer.Refresh().ReconciliationSequence);
            Assert.Equal(2, observer.Refresh().ReconciliationSequence);
            Assert.Equal(3, observer.Refresh().ReconciliationSequence);
        }

        [Fact]
        public void Refresh_PhysicalReadThrows_NonAssessableThenRecovers()
        {
            var shadowObserver = new FakeKoploperLogicalSectionShadowObserver(OccupiedShadow());
            var physicalSource = new FakePhysicalSectionOccupancySource(() => OccupiedPhysical());
            using var observer = CreateObserver(shadowObserver, physicalSource);

            physicalSource.EnqueueThrow();

            LogicalPhysicalReconciliationObservation failed = observer.Refresh();
            Assert.False(failed.ReconciliationAssessable);
            Assert.False(failed.LogicalSourceValid);
            Assert.False(failed.PhysicalSourceValid);

            LogicalPhysicalReconciliationObservation recovered = observer.Refresh();
            Assert.True(recovered.ReconciliationAssessable);
        }

        [Fact]
        public void Refresh_NoStaleInheritance()
        {
            var shadowObserver = new FakeKoploperLogicalSectionShadowObserver(OccupiedShadow());
            var physicalSource = new FakePhysicalSectionOccupancySource(() => OccupiedPhysical());
            using var observer = CreateObserver(shadowObserver, physicalSource);

            LogicalPhysicalReconciliationObservation first = observer.Refresh();
            Assert.Equal(LogicalSectionReconciliationState.ConsistOccupied, Assert.Single(first.Sections, s => s.LogicalSectionId == 1).ReconciliationState);

            // The physical section goes clear while the locomotive still owns it: a contradiction.
            physicalSource.Fallback = () => ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Clear) });

            LogicalPhysicalReconciliationObservation second = observer.Refresh();
            Assert.Equal(LogicalSectionReconciliationState.ContradictionLogicalOccupiedPhysicalClear, Assert.Single(second.Sections, s => s.LogicalSectionId == 1).ReconciliationState);

            // The previous observation must not have been mutated.
            Assert.Equal(LogicalSectionReconciliationState.ConsistOccupied, Assert.Single(first.Sections, s => s.LogicalSectionId == 1).ReconciliationState);
        }

        [Fact]
        public void Refresh_CarriesLogicalAndPhysicalGenerations_NoStaleAfterRestart()
        {
            var generationA = ReconciliationTestFixtures.Generation(processId: 100, utcTicks: 1000);
            var shadowObserver = new FakeKoploperLogicalSectionShadowObserver(
                ReconciliationTestFixtures.Shadow(
                    generation: generationA,
                    locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) }));
            var physicalSource = new FakePhysicalSectionOccupancySource(
                () => ReconciliationTestFixtures.Physical(
                    sourceGeneration: 7,
                    sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) }));
            using var observer = CreateObserver(shadowObserver, physicalSource);

            LogicalPhysicalReconciliationObservation first = observer.Refresh();
            Assert.Equal(generationA, first.LogicalGeneration);
            Assert.Equal(7, first.PhysicalGeneration);

            // Simulate a Koploper restart (new generation) and a physical source restart (new generation).
            var generationB = ReconciliationTestFixtures.Generation(processId: 200, utcTicks: 2000);
            shadowObserver.CurrentShadow = ReconciliationTestFixtures.Shadow(
                generation: generationB,
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            physicalSource.Fallback = () => ReconciliationTestFixtures.Physical(
                sourceGeneration: 8,
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation second = observer.Refresh();
            Assert.Equal(generationB, second.LogicalGeneration);
            Assert.Equal(8, second.PhysicalGeneration);

            // The first observation is an immutable snapshot and is unaffected.
            Assert.Equal(generationA, first.LogicalGeneration);
            Assert.Equal(7, first.PhysicalGeneration);
        }

        [Fact]
        public async Task StartStop_Lifecycle_PublishesAndStops()
        {
            var shadowObserver = new FakeKoploperLogicalSectionShadowObserver(OccupiedShadow());
            var physicalSource = new FakePhysicalSectionOccupancySource(() => OccupiedPhysical());
            using var observer = CreateObserver(shadowObserver, physicalSource);
            using var cts = new CancellationTokenSource();

            observer.Start(cts.Token);

            WaitUntil(() => observer.CurrentObservation is not null, TimeSpan.FromSeconds(5));
            Assert.NotNull(observer.CurrentObservation);

            await observer.StopAsync();

            int callsAfterStop = physicalSource.CallCount;
            await Task.Delay(50);
            Assert.Equal(callsAfterStop, physicalSource.CallCount);
        }

        [Fact]
        public async Task StopAsync_NotStarted_Idempotent()
        {
            var shadowObserver = new FakeKoploperLogicalSectionShadowObserver(OccupiedShadow());
            var physicalSource = new FakePhysicalSectionOccupancySource(() => OccupiedPhysical());
            using var observer = CreateObserver(shadowObserver, physicalSource);

            await observer.StopAsync(); // must not throw
            Assert.Null(observer.CurrentObservation);
        }

        [Fact]
        public void Start_Idempotent_SecondStartDoesNotThrow()
        {
            var shadowObserver = new FakeKoploperLogicalSectionShadowObserver(OccupiedShadow());
            var physicalSource = new FakePhysicalSectionOccupancySource(() => OccupiedPhysical());
            using var observer = CreateObserver(shadowObserver, physicalSource);
            using var cts = new CancellationTokenSource();

            observer.Start(cts.Token);
            observer.Start(cts.Token); // idempotent; must not spawn a second loop or throw

            WaitUntil(() => observer.CurrentObservation is not null, TimeSpan.FromSeconds(5));
            Assert.NotNull(observer.CurrentObservation);

            observer.Dispose();
        }

        [Fact]
        public void Dispose_CancelsAndBlocksUntilStopped()
        {
            var shadowObserver = new FakeKoploperLogicalSectionShadowObserver(OccupiedShadow());
            var physicalSource = new FakePhysicalSectionOccupancySource(() => OccupiedPhysical());
            var observer = CreateObserver(shadowObserver, physicalSource);
            using var cts = new CancellationTokenSource();

            observer.Start(cts.Token);
            WaitUntil(() => observer.CurrentObservation is not null, TimeSpan.FromSeconds(5));

            observer.Dispose();

            int callsAfterDispose = physicalSource.CallCount;
            Thread.Sleep(50);
            Assert.Equal(callsAfterDispose, physicalSource.CallCount);
        }

        private static void WaitUntil(Func<bool> condition, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new Xunit.Sdk.XunitException("Timed out waiting for condition.");
                }

                Thread.Sleep(5);
            }
        }
    }
}
