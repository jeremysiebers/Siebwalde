using System;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for <see cref="KoploperSnapshotFreshness"/> with a deterministic clock.
    /// </summary>
    public class KoploperSnapshotFreshnessTests
    {
        private static readonly DateTimeOffset Captured = new(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Age_ReturnsElapsed()
        {
            Assert.Equal(
                TimeSpan.FromSeconds(3),
                KoploperSnapshotFreshness.Age(Snapshot(Captured), Captured.AddSeconds(3)));
        }

        [Fact]
        public void IsCurrent_AgeEqualsMaxAge_True()
        {
            Assert.True(KoploperSnapshotFreshness.IsCurrent(
                Snapshot(Captured),
                Captured.AddSeconds(5),
                TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void IsCurrent_AgeExceedsMaxAge_False()
        {
            Assert.False(KoploperSnapshotFreshness.IsCurrent(
                Snapshot(Captured),
                Captured.AddSeconds(6),
                TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void ResolveHealth_AgeExceedsMaxAge_Stale()
        {
            Assert.Equal(
                KoploperSourceHealth.Stale,
                KoploperSnapshotFreshness.ResolveHealth(
                    Snapshot(Captured, KoploperSourceHealth.Healthy),
                    Captured.AddSeconds(6),
                    TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void ResolveHealth_Fresh_ReturnsSnapshotHealth()
        {
            Assert.Equal(
                KoploperSourceHealth.Degraded,
                KoploperSnapshotFreshness.ResolveHealth(
                    Snapshot(Captured, KoploperSourceHealth.Degraded),
                    Captured.AddSeconds(1),
                    TimeSpan.FromSeconds(5)));
        }

        private static KoploperStateSnapshot Snapshot(DateTimeOffset capturedAtUtc, KoploperSourceHealth health = KoploperSourceHealth.Healthy)
        {
            return new KoploperStateSnapshot(
                new KoploperProcessGeneration(1, capturedAtUtc),
                new KoploperExecutableIdentity("Koploper", "9.4.0.9", "HASH", 0x00400000),
                Sequence: 1,
                CapturedAtUtc: capturedAtUtc,
                ReadDuration: TimeSpan.Zero,
                RetryCount: 0,
                IsConsistent: true,
                SourceHealth: health,
                Blocks: Array.Empty<KoploperBlockSnapshot>(),
                BlockDiagnostics: Array.Empty<KoploperBlockDiagnostic>(),
                Locomotives: Array.Empty<KoploperLocomotiveSnapshot>());
        }
    }
}
