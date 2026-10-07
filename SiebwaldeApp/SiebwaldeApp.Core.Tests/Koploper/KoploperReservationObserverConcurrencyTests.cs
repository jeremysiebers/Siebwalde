using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Concurrency tests for <see cref="KoploperReservationObserver"/>: the volatile publication
    /// of <see cref="KoploperReservationObserver.CurrentObservation"/> must be visible to other
    /// threads without an explicit memory barrier on the read side.
    /// </summary>
    public class KoploperReservationObserverConcurrencyTests
    {
        private static readonly DateTimeOffset Now = KoploperReservationTestFixtures.Now;

        [Fact]
        public void CurrentObservation_VolatilePublication_VisibleAcrossThreads()
        {
            var reader = new CountingSnapshotReader();
            var options = new KoploperReservationObserverOptions(
                pollingInterval: TimeSpan.FromMilliseconds(10),
                maxSnapshotAge: null);

            using var observer = new KoploperReservationObserver(reader, options, () => Now);
            using var cts = new CancellationTokenSource();

            observer.Start(cts.Token);

            // Spin on a separate thread reading the volatile property until a non-null observation
            // becomes visible (no lock or other memory barrier on the reading side).
            KoploperReservationObservation? seen = null;
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
            Assert.True(seen!.IsAuthoritative);

            observer.Dispose();
        }

        private sealed class CountingSnapshotReader : IKoploperSnapshotReader
        {
            public KoploperSnapshotReadResult ReadSnapshot()
            {
                var snapshot = KoploperReservationTestFixtures.Snapshot(
                    blocks: new[]
                    {
                        KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied)
                    },
                    locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

                return KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot);
            }
        }
    }
}
