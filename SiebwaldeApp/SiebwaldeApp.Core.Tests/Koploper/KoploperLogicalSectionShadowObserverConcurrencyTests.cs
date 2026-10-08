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
    /// Concurrency tests for <see cref="KoploperLogicalSectionShadowObserver"/>: the volatile
    /// publication of <see cref="KoploperLogicalSectionShadowObserver.CurrentShadow"/> must be
    /// visible to other threads without an explicit memory barrier on the read side.
    /// </summary>
    public class KoploperLogicalSectionShadowObserverConcurrencyTests
    {
        private static readonly DateTimeOffset Now = KoploperReservationTestFixtures.Now;
        private static readonly Dictionary<int, int> Map = new() { { 30, 2 } };

        [Fact]
        public void CurrentShadow_VolatilePublication_VisibleAcrossThreads()
        {
            var reader = new CountingSnapshotReader();
            var reservation = new FixedReservationObserver();
            var options = new KoploperLogicalSectionShadowObserverOptions(MaxSnapshotAge: null);

            using var observer = new KoploperLogicalSectionShadowObserver(reader, reservation, "profile", Map, options, () => Now);
            using var cts = new CancellationTokenSource();

            observer.Start(cts.Token);

            KoploperLogicalSectionShadowObservation? seen = null;
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
            {
                seen = observer.CurrentShadow;
                if (seen is not null)
                {
                    break;
                }

                Thread.Yield();
            }

            Assert.NotNull(seen);
            Assert.True(seen!.ShadowValid);

            observer.Dispose();
        }

        private sealed class CountingSnapshotReader : IKoploperSnapshotReader
        {
            public KoploperSnapshotReadResult ReadSnapshot()
            {
                var snapshot = KoploperReservationTestFixtures.Snapshot(
                    blocks: new[] { KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied) },
                    locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

                return KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot);
            }
        }

        private sealed class FixedReservationObserver : IKoploperReservationObserver
        {
            private static readonly KoploperReservationObservation Observation = new(
                Generation: KoploperReservationTestFixtures.Generation(),
                SourceSequence: 1,
                ObserverSequence: 1,
                CapturedAtUtc: Now,
                ObservedAtUtc: Now,
                IsFresh: true,
                IsConsistent: true,
                SourceHealth: KoploperSourceHealth.Healthy,
                IsAuthoritative: true,
                AuthorityReason: KoploperReservationAuthorityReason.Authoritative,
                Locomotives: Array.Empty<KoploperLocomotiveTrajectory>(),
                Conflicts: Array.Empty<KoploperReservationConflict>(),
                Diagnostics: Array.Empty<KoploperDiagnosticCode>());

            public KoploperReservationObservation? CurrentObservation => Observation;

            public KoploperReservationObservation Refresh() => Observation;

            public void Start(CancellationToken cancellationToken)
            {
            }

            public Task StopAsync() => Task.CompletedTask;

            public void Dispose()
            {
            }
        }
    }
}
