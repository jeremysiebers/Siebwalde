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
    /// Unit tests for <see cref="KoploperLogicalSectionShadowObserver"/>: no stale inheritance,
    /// monotonic shadow sequence, lifecycle, and read-failure recovery. Uses a scripted snapshot
    /// reader, a settable reservation observer, and a fixed clock.
    /// </summary>
    public class KoploperLogicalSectionShadowObserverTests
    {
        private static readonly DateTimeOffset Now = KoploperReservationTestFixtures.Now;
        private static readonly Dictionary<int, int> Map = new() { { 30, 2 } };

        private static KoploperSnapshotReadResult AuthoritativeReadResult()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[] { KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied) },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            return KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot);
        }

        private static KoploperReservationObservation AuthoritativeObservation()
        {
            return new KoploperReservationObservation(
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
        }

        private static KoploperLogicalSectionShadowObserver CreateObserver(
            FakeSnapshotReader reader,
            FakeReservationObserver reservationObserver,
            TimeSpan? maxSnapshotAge = null)
        {
            var options = new KoploperLogicalSectionShadowObserverOptions(maxSnapshotAge);
            return new KoploperLogicalSectionShadowObserver(reader, reservationObserver, "profile", Map, options, () => Now);
        }

        [Fact]
        public void CurrentShadow_NullBeforeFirstRefresh()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            var reservation = new FakeReservationObserver(AuthoritativeObservation());
            using var observer = CreateObserver(reader, reservation);

            Assert.Null(observer.CurrentShadow);
        }

        [Fact]
        public void Refresh_NoCurrentReservationObservation_PublishesUnavailableShadow()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            var reservation = new FakeReservationObserver(null);
            using var observer = CreateObserver(reader, reservation);

            KoploperLogicalSectionShadowObservation shadow = observer.Refresh();

            Assert.False(shadow.ShadowValid);
            Assert.False(shadow.SourceAuthoritative);
            Assert.Equal(KoploperSourceHealth.Unavailable, shadow.SourceHealth);
            Assert.Empty(shadow.Locomotives);
        }

        [Fact]
        public void Refresh_AuthoritativeObservation_PublishesValidShadow()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            var reservation = new FakeReservationObserver(AuthoritativeObservation());
            using var observer = CreateObserver(reader, reservation);

            KoploperLogicalSectionShadowObservation shadow = observer.Refresh();

            Assert.True(shadow.ShadowValid);
            Assert.True(shadow.SourceAuthoritative);
            var loco = Assert.Single(shadow.Locomotives);
            Assert.Equal(2, loco.OccupiedLogicalSection);
        }

        [Fact]
        public void ShadowSequence_MonotonicAcrossRefreshes()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            var reservation = new FakeReservationObserver(AuthoritativeObservation());
            using var observer = CreateObserver(reader, reservation);

            Assert.Equal(1, observer.Refresh().ShadowSequence);
            Assert.Equal(2, observer.Refresh().ShadowSequence);
            Assert.Equal(3, observer.Refresh().ShadowSequence);
        }

        [Fact]
        public void ReadFailure_NextCycleRecoversValidity()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            var reservation = new FakeReservationObserver(AuthoritativeObservation());
            using var observer = CreateObserver(reader, reservation);

            reader.EnqueueThrow();

            KoploperLogicalSectionShadowObservation failed = observer.Refresh();
            Assert.False(failed.ShadowValid);
            Assert.False(failed.SourceAuthoritative);

            KoploperLogicalSectionShadowObservation recovered = observer.Refresh();
            Assert.True(recovered.ShadowValid);
        }

        [Fact]
        public async Task StartStop_Lifecycle_PublishesAndStops()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            var reservation = new FakeReservationObserver(AuthoritativeObservation());
            using var observer = CreateObserver(reader, reservation);
            using var cts = new CancellationTokenSource();

            observer.Start(cts.Token);

            WaitUntil(() => observer.CurrentShadow is not null, TimeSpan.FromSeconds(5));
            Assert.NotNull(observer.CurrentShadow);

            await observer.StopAsync();

            int callsAfterStop = reader.CallCount;
            await Task.Delay(50);
            Assert.Equal(callsAfterStop, reader.CallCount);
        }

        [Fact]
        public async Task StopAsync_NotStarted_Idempotent()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            var reservation = new FakeReservationObserver(AuthoritativeObservation());
            using var observer = CreateObserver(reader, reservation);

            await observer.StopAsync(); // must not throw
            Assert.Null(observer.CurrentShadow);
        }

        [Fact]
        public void Start_Idempotent_SecondStartDoesNotThrow()
        {
            var reader = new FakeSnapshotReader(AuthoritativeReadResult);
            var reservation = new FakeReservationObserver(AuthoritativeObservation());
            using var observer = CreateObserver(reader, reservation);
            using var cts = new CancellationTokenSource();

            observer.Start(cts.Token);
            observer.Start(cts.Token); // idempotent

            WaitUntil(() => observer.CurrentShadow is not null, TimeSpan.FromSeconds(5));
            Assert.NotNull(observer.CurrentShadow);

            observer.Dispose();
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
            private readonly Queue<Func<KoploperSnapshotReadResult>> _script = new();
            private readonly Func<KoploperSnapshotReadResult> _fallback;

            public FakeSnapshotReader(Func<KoploperSnapshotReadResult> fallback)
            {
                _fallback = fallback;
            }

            public int CallCount { get; private set; }

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

        private sealed class FakeReservationObserver : IKoploperReservationObserver
        {
            private KoploperReservationObservation? _current;

            public FakeReservationObserver(KoploperReservationObservation? current)
            {
                _current = current;
            }

            public KoploperReservationObservation? CurrentObservation => _current;

            public KoploperReservationObservation Refresh() => _current!;

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
