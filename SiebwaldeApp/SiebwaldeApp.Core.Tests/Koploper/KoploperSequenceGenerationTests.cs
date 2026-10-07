using System.Reflection;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for the per-generation sequence in <see cref="KoploperSnapshotReader"/>:
    /// monotone within a generation, reset on a generation change, and fail-closed on overflow.
    /// </summary>
    public class KoploperSequenceGenerationTests
    {
        [Fact]
        public void Sequence_MonotoneAcrossReads()
        {
            var locator = new ScriptedKoploperProcessLocator();
            locator.SetFallback(KoploperProcessStatus.Found, KoploperSnapshotTestFixtures.Info(100, 1000));
            var scripted = new ScriptedKoploperMemoryReader(KoploperSnapshotTestFixtures.WatchAddress);
            KoploperSnapshotTestFixtures.BuildValidMap(scripted.Inner);
            var reader = KoploperSnapshotTestFixtures.CreateReader(locator, () => scripted);

            long first = reader.ReadSnapshot().Snapshot!.Sequence;
            long second = reader.ReadSnapshot().Snapshot!.Sequence;
            long third = reader.ReadSnapshot().Snapshot!.Sequence;

            Assert.Equal(1, first);
            Assert.Equal(2, second);
            Assert.Equal(3, third);
        }

        [Fact]
        public void Sequence_ResetsOnGenerationChange()
        {
            var locator = new ScriptedKoploperProcessLocator();
            locator.SetFallback(KoploperProcessStatus.Found, KoploperSnapshotTestFixtures.Info(100, 1000));
            var scripted = new ScriptedKoploperMemoryReader(KoploperSnapshotTestFixtures.WatchAddress);
            KoploperSnapshotTestFixtures.BuildValidMap(scripted.Inner);
            var reader = KoploperSnapshotTestFixtures.CreateReader(locator, () => scripted);

            long beforeRestart = reader.ReadSnapshot().Snapshot!.Sequence;
            Assert.Equal(1, beforeRestart);

            // Restart into a new generation.
            locator.SetFallback(KoploperProcessStatus.Found, KoploperSnapshotTestFixtures.Info(200, 2000));
            KoploperSnapshotReadResult afterRestart = reader.ReadSnapshot();
            Assert.Equal(1, afterRestart.Snapshot!.Sequence);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_RESTARTED, afterRestart.Diagnostics);

            // Sequence then continues within the new generation.
            long next = reader.ReadSnapshot().Snapshot!.Sequence;
            Assert.Equal(2, next);
        }

        [Fact]
        public void Sequence_Overflow_FailsClosedUnavailable()
        {
            var locator = new ScriptedKoploperProcessLocator();
            locator.SetFallback(KoploperProcessStatus.Found, KoploperSnapshotTestFixtures.Info(100, 1000));
            var scripted = new ScriptedKoploperMemoryReader(KoploperSnapshotTestFixtures.WatchAddress);
            KoploperSnapshotTestFixtures.BuildValidMap(scripted.Inner);
            var reader = KoploperSnapshotTestFixtures.CreateReader(locator, () => scripted);

            // Drive the retained sequence to long.MaxValue via reflection, then verify the next
            // increment fails closed instead of silently wrapping.
            FieldInfo? field = typeof(KoploperSnapshotReader).GetField(
                "_sequence",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field!.SetValue(reader, long.MaxValue);

            KoploperSnapshotReadResult result = reader.ReadSnapshot();

            Assert.Equal(KoploperSourceHealth.Unavailable, result.Health);
            Assert.Null(result.Snapshot);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_SNAPSHOT_INCONSISTENT, result.Diagnostics);
        }
    }
}
