using System;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for <see cref="KoploperSnapshotReader"/>. Uses the scripted process locator and
    /// memory reader so torn reads, restarts and lifecycle outcomes are fully deterministic.
    /// </summary>
    public class KoploperSnapshotReaderTests
    {
        private static readonly DateTimeOffset FixedNow = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
        private static readonly nuint ModuleBase = (nuint)KoploperSnapshotTestFixtures.ModuleBase;

        [Fact]
        public void ReadSnapshot_CoherentFirstTry_Healthy()
        {
            var locator = new ScriptedKoploperProcessLocator();
            locator.SetFallback(KoploperProcessStatus.Found, KoploperSnapshotTestFixtures.Info(100, 1000));
            var scripted = new ScriptedKoploperMemoryReader(KoploperSnapshotTestFixtures.WatchAddress);
            KoploperSnapshotTestFixtures.BuildValidMap(scripted.Inner);
            var reader = KoploperSnapshotTestFixtures.CreateReader(locator, () => scripted, clock: () => FixedNow);

            KoploperSnapshotReadResult result = reader.ReadSnapshot();

            Assert.Equal(KoploperSourceHealth.Healthy, result.Health);
            Assert.NotNull(result.Snapshot);
            Assert.Equal(1, result.Snapshot.Sequence);
            Assert.Equal(0, result.Snapshot.RetryCount);
            Assert.True(result.Snapshot.IsConsistent);
            Assert.Empty(result.Snapshot.BlockDiagnostics);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(2, result.Snapshot.Blocks.Count);
            Assert.Single(result.Snapshot.Locomotives);
            Assert.Equal(FixedNow, result.Snapshot.CapturedAtUtc);
        }

        [Fact]
        public void ReadSnapshot_TornThenCoherent_DegradedWithRetry()
        {
            var locator = new ScriptedKoploperProcessLocator();
            locator.SetFallback(KoploperProcessStatus.Found, KoploperSnapshotTestFixtures.Info(100, 1000));
            var scripted = new ScriptedKoploperMemoryReader(KoploperSnapshotTestFixtures.WatchAddress);
            KoploperSnapshotTestFixtures.BuildValidMap(scripted.Inner);

            // One one-shot mutation at the first PASS-B boundary: flips block 0's changed flag.
            // The changed flag is not used by the state decoder, so the final decode stays clean.
            scripted.EnqueueMutation(inner =>
                inner.SetBytes(
                    (nuint)(KoploperSnapshotTestFixtures.BlockObjectsBase + KoploperSnapshotTestFixtures.Layout.BlockChangedFlagOffset),
                    new byte[] { 1 }));

            var reader = KoploperSnapshotTestFixtures.CreateReader(locator, () => scripted);

            KoploperSnapshotReadResult result = reader.ReadSnapshot();

            Assert.Equal(KoploperSourceHealth.Degraded, result.Health);
            Assert.NotNull(result.Snapshot);
            Assert.Equal(1, result.Snapshot.RetryCount);
            Assert.Empty(result.Snapshot.BlockDiagnostics);
        }

        [Fact]
        public void ReadSnapshot_PersistentTorn_Inconsistent()
        {
            var locator = new ScriptedKoploperProcessLocator();
            locator.SetFallback(KoploperProcessStatus.Found, KoploperSnapshotTestFixtures.Info(100, 1000));
            var scripted = new ScriptedKoploperMemoryReader(KoploperSnapshotTestFixtures.WatchAddress);
            KoploperSnapshotTestFixtures.BuildValidMap(scripted.Inner);

            byte changed = 0;
            scripted.SetPersistentMutation(inner =>
            {
                changed = changed == 0 ? (byte)1 : (byte)0;
                inner.SetBytes(
                    (nuint)(KoploperSnapshotTestFixtures.BlockObjectsBase + KoploperSnapshotTestFixtures.Layout.BlockChangedFlagOffset),
                    new byte[] { changed });
            });

            var reader = KoploperSnapshotTestFixtures.CreateReader(
                locator,
                () => scripted,
                options: new KoploperSnapshotReadOptions(maxAttempts: 4));

            KoploperSnapshotReadResult result = reader.ReadSnapshot();

            Assert.Equal(KoploperSourceHealth.Inconsistent, result.Health);
            Assert.Null(result.Snapshot);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_SNAPSHOT_INCONSISTENT, result.Diagnostics);
        }

        [Fact]
        public void ReadSnapshot_ProcessNotFound_ProcessNotFound()
        {
            var locator = new ScriptedKoploperProcessLocator();
            locator.SetFallback(KoploperProcessStatus.NotFound, null);
            var reader = KoploperSnapshotTestFixtures.CreateReader(locator, () => new FakeKoploperMemoryReader());

            KoploperSnapshotReadResult result = reader.ReadSnapshot();

            Assert.Equal(KoploperSourceHealth.ProcessNotFound, result.Health);
            Assert.Null(result.Snapshot);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_PROCESS_NOT_FOUND, result.Diagnostics);
        }

        [Fact]
        public void ReadSnapshot_UnsupportedBinary_UnsupportedVersion()
        {
            var locator = new ScriptedKoploperProcessLocator();
            locator.SetFallback(KoploperProcessStatus.Found, KoploperSnapshotTestFixtures.Info(100, 1000));
            var reader = KoploperSnapshotTestFixtures.CreateReader(
                locator,
                () => new FakeKoploperMemoryReader(),
                gate: KoploperVersionGateResult.UnsupportedVersion);

            KoploperSnapshotReadResult result = reader.ReadSnapshot();

            Assert.Equal(KoploperSourceHealth.UnsupportedVersion, result.Health);
            Assert.Null(result.Snapshot);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_UNSUPPORTED_BINARY, result.Diagnostics);
        }

        [Fact]
        public void ReadSnapshot_Restart_InvalidatesAndEmitsRestarted()
        {
            var locator = new ScriptedKoploperProcessLocator();
            locator.SetFallback(KoploperProcessStatus.Found, KoploperSnapshotTestFixtures.Info(100, 1000));
            var scripted = new ScriptedKoploperMemoryReader(KoploperSnapshotTestFixtures.WatchAddress);
            KoploperSnapshotTestFixtures.BuildValidMap(scripted.Inner);
            var reader = KoploperSnapshotTestFixtures.CreateReader(locator, () => scripted);

            KoploperSnapshotReadResult first = reader.ReadSnapshot();
            Assert.Equal(KoploperSourceHealth.Healthy, first.Health);
            Assert.NotNull(first.Snapshot);
            Assert.Equal(1, first.Snapshot.Sequence);
            Assert.Equal(100, first.Snapshot.Generation.ProcessId);
            Assert.DoesNotContain(KoploperDiagnosticCode.KOPLOPER_RESTARTED, first.Diagnostics);

            // Process restarts: new PID and start time.
            locator.SetFallback(KoploperProcessStatus.Found, KoploperSnapshotTestFixtures.Info(200, 2000));

            KoploperSnapshotReadResult second = reader.ReadSnapshot();

            Assert.Equal(KoploperSourceHealth.Healthy, second.Health);
            Assert.NotNull(second.Snapshot);
            Assert.Equal(200, second.Snapshot.Generation.ProcessId);
            Assert.Equal(1, second.Snapshot.Sequence); // reset on generation change
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_RESTARTED, second.Diagnostics);
        }

        [Fact]
        public void ReadSnapshot_SemanticInvalidity_DegradedNotInconsistent()
        {
            // An unknown raw state maps to a per-block Unknown + diagnostic, degrading the
            // snapshot — but the snapshot itself must still be published (not whole-snapshot
            // non-authoritative). The coherent reads agree, so no retry is involved.
            var locator = new ScriptedKoploperProcessLocator();
            locator.SetFallback(KoploperProcessStatus.Found, KoploperSnapshotTestFixtures.Info(100, 1000));
            var scripted = new ScriptedKoploperMemoryReader(KoploperSnapshotTestFixtures.WatchAddress);
            KoploperSnapshotTestFixtures.BuildValidMap(scripted.Inner);

            // Set block 0's raw state to an unknown value (7) with no owner.
            scripted.Inner.SetBytes(
                (nuint)(KoploperSnapshotTestFixtures.BlockObjectsBase + KoploperSnapshotTestFixtures.Layout.BlockStateOffset),
                new byte[] { 7 });

            var reader = KoploperSnapshotTestFixtures.CreateReader(locator, () => scripted);

            KoploperSnapshotReadResult result = reader.ReadSnapshot();

            Assert.Equal(KoploperSourceHealth.Degraded, result.Health);
            Assert.NotNull(result.Snapshot);
            Assert.Equal(0, result.Snapshot.RetryCount);
            Assert.NotEmpty(result.Snapshot.BlockDiagnostics);
            Assert.Contains(result.Snapshot.BlockDiagnostics, d => d.Code == KoploperDiagnosticCode.KOPLOPER_UNKNOWN_BLOCK_STATE);
            Assert.Equal(KoploperBlockState.Unknown, result.Snapshot.Blocks[0].State);
        }
    }
}
