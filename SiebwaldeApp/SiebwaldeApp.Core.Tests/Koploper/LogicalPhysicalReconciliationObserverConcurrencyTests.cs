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
    /// Concurrency tests for <see cref="LogicalPhysicalReconciliationObserver"/>: the volatile
    /// publication of <see cref="LogicalPhysicalReconciliationObserver.CurrentObservation"/> must be
    /// visible to other threads without an explicit memory barrier on the read side.
    /// </summary>
    public class LogicalPhysicalReconciliationObserverConcurrencyTests
    {
        private static readonly DateTimeOffset Now = ReconciliationTestFixtures.Now;
        private static readonly int[] BoundSections = { 1, 2, 3, 4 };

        [Fact]
        public void CurrentObservation_VolatilePublication_VisibleAcrossThreads()
        {
            var shadowObserver = new FixedShadowObserver();
            var physicalSource = new CountingPhysicalSource();
            var options = new LogicalPhysicalReconciliationObserverOptions(
                pollingInterval: TimeSpan.FromMilliseconds(10));

            using var observer = new LogicalPhysicalReconciliationObserver(
                shadowObserver,
                physicalSource,
                "simple-loop",
                BoundSections,
                options,
                () => Now);
            using var cts = new CancellationTokenSource();

            observer.Start(cts.Token);

            // Spin reading the volatile property until a non-null observation becomes visible.
            LogicalPhysicalReconciliationObservation? seen = null;
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
            {
                seen = observer.CurrentObservation;
                if (seen is not null)
                {
                    break;
                }

                Thread.Yield();
            }

            Assert.NotNull(seen);
            Assert.True(seen!.ReconciliationAssessable);

            observer.Dispose();
        }

        private sealed class FixedShadowObserver : IKoploperLogicalSectionShadowObserver
        {
            private static readonly KoploperLogicalSectionShadowObservation Shadow =
                ReconciliationTestFixtures.Shadow(
                    locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });

            public KoploperLogicalSectionShadowObservation? CurrentShadow => Shadow;

            public KoploperLogicalSectionShadowObservation Refresh() => Shadow;

            public void Start(CancellationToken cancellationToken)
            {
            }

            public Task StopAsync() => Task.CompletedTask;

            public void Dispose()
            {
            }
        }

        private sealed class CountingPhysicalSource : IPhysicalSectionOccupancySource
        {
            public PhysicalSectionOccupancyObservation Read()
                => ReconciliationTestFixtures.Physical(
                    sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });
        }
    }
}
