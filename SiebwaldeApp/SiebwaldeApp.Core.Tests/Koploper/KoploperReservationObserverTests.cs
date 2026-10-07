using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for <see cref="KoploperReservationObserver"/>: no-stale-inheritance, monotonic
    /// observer sequence, lifecycle, and read-failure recovery. Uses a scripted
    /// <see cref="IKoploperSnapshotReader"/> and a fixed clock.
    /// </summary>
    public class KoploperReservationObserverTests
    {
        private static readonly DateTimeOffset Now = KoploperReservationTestFixtures.Now;

        private static KoploperSnapshotReadResult AuthoritativeReadResult()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            return KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot);
        }

        private static KoploperSnapshotReadResult ProcessNotFoundReadResult()
        {
            return KoploperReservationTestFixtures.ReadResult(
                KoploperSourceHealth.ProcessNotFound,
                diagnostics: new[] { KoploperDiagnosticCode.KOPLOPER_PROCESS_NOT_FOUND });
        }

        private static KoploperReservationObserver CreateObserver(
            FakeSnapshotReader reader,
            TimeSpan? pollingInterval = null)
        {
            var options = new KoploperReservationObserverOptions(
                pollingInterval: pollingInterval ?? TimeSpan.FromMilliseconds(10),
                maxSnapshotAge: null);

            return new KoploperReservationObserver(reader, options, () => Now);
        }

        [Fact]
        public void CurrentObservation_NullBeforeFirstRefresh()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            using var observer = CreateObserver(reader);

            Assert.Null(observer.CurrentObservation);
        }

        [Fact]
        public void Refresh_NonAuthoritativeCycle_PublishesEmptyNotStaleOwnership()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            using var observer = CreateObserver(reader);

            KoploperReservationObservation authoritative = observer.Refresh();
            Assert.True(authoritative.IsAuthoritative);
            Assert.Single(authoritative.Locomotives);

            reader.EnqueueResult(ProcessNotFoundReadResult());

            KoploperReservationObservation nonAuthoritative = observer.Refresh();

            Assert.False(nonAuthoritative.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.ProcessNotFound, nonAuthoritative.AuthorityReason);
            Assert.Empty(nonAuthoritative.Locomotives);
            Assert.Empty(nonAuthoritative.Conflicts);

            // The published current observation must not inherit the previous ownership.
            Assert.NotNull(observer.CurrentObservation);
            Assert.Empty(observer.CurrentObservation!.Locomotives);
        }

        [Fact]
        public void ObserverSequence_MonotonicAcrossRefreshes()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            using var observer = CreateObserver(reader);

            Assert.Equal(1, observer.Refresh().ObserverSequence);
            Assert.Equal(2, observer.Refresh().ObserverSequence);
            Assert.Equal(3, observer.Refresh().ObserverSequence);
        }

        [Fact]
        public void ReadFailure_NextCycleRecoversAuthority()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            using var observer = CreateObserver(reader);

            reader.EnqueueThrow();

            KoploperReservationObservation failed = observer.Refresh();
            Assert.False(failed.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.Unavailable, failed.AuthorityReason);
            Assert.Empty(failed.Locomotives);

            KoploperReservationObservation recovered = observer.Refresh();
            Assert.True(recovered.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.Authoritative, recovered.AuthorityReason);
            Assert.Single(recovered.Locomotives);
        }

        [Fact]
        public async Task StartStop_Lifecycle_PublishesAndStops()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            using var observer = CreateObserver(reader);
            using var cts = new CancellationTokenSource();

            observer.Start(cts.Token);

            WaitUntil(() => observer.CurrentObservation is not null, TimeSpan.FromSeconds(5));
            Assert.NotNull(observer.CurrentObservation);

            await observer.StopAsync();

            int callsAfterStop = reader.CallCount;
            await Task.Delay(50);
            Assert.Equal(callsAfterStop, reader.CallCount);
        }

        [Fact]
        public async Task StopAsync_NotStarted_Idempotent()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            using var observer = CreateObserver(reader);

            await observer.StopAsync(); // must not throw
            Assert.Null(observer.CurrentObservation);
        }

        [Fact]
        public void Start_Idempotent_SecondStartDoesNotThrow()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            using var observer = CreateObserver(reader);
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
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            var observer = CreateObserver(reader);
            using var cts = new CancellationTokenSource();

            observer.Start(cts.Token);
            WaitUntil(() => observer.CurrentObservation is not null, TimeSpan.FromSeconds(5));

            observer.Dispose();

            int callsAfterDispose = reader.CallCount;
            Thread.Sleep(50);
            Assert.Equal(callsAfterDispose, reader.CallCount);
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

        private sealed class FakeSnapshotReader : IKoploperSnapshotReader
        {
            private readonly System.Collections.Generic.Queue<Func<KoploperSnapshotReadResult>> _script = new();
            private readonly Func<KoploperSnapshotReadResult> _fallback;

            public FakeSnapshotReader(Func<KoploperSnapshotReadResult> fallback)
            {
                _fallback = fallback;
            }

            public int CallCount { get; private set; }

            public void EnqueueResult(KoploperSnapshotReadResult result)
                => _script.Enqueue(() => result);

            public void EnqueueThrow()
                => _script.Enqueue(() => throw new InvalidOperationException("boom"));

            public KoploperSnapshotReadResult ReadSnapshot()
            {
                CallCount++;
                if (_script.Count > 0)
                {
                    return _script.Dequeue()();
                }

                return _fallback();
            }
        }
    }
}
