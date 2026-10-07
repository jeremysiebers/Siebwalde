using System;
using System.Collections.Generic;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Shared deterministic fixtures for the KIS-05 reservation tests. These build
    /// <see cref="KoploperStateSnapshot"/> and <see cref="KoploperSnapshotReadResult"/> directly,
    /// bypassing the memory reader, so aggregation and authority rules can be exercised without
    /// any process/memory dependency.
    /// </summary>
    internal static class KoploperReservationTestFixtures
    {
        public static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

        public static KoploperProcessGeneration Generation(int processId = 100, long utcTicks = 1000)
            => new(processId, new DateTimeOffset(utcTicks, TimeSpan.Zero));

        public static KoploperExecutableIdentity Identity()
            => new("Koploper", "9.4.0.9", "HASH", 0x00400000);

        public static KoploperBlockSnapshot Block(int internalBlockId, int? ownerLocomotiveId, KoploperBlockState state)
            => new(internalBlockId, internalBlockId, ownerLocomotiveId, state, (uint)state, null);

        public static KoploperLocomotiveSnapshot Locomotive(int internalLocomotiveId)
            => new(internalLocomotiveId, (uint)(0x00800000 + (internalLocomotiveId * 0x200)));

        public static KoploperBlockDiagnostic BlockDiagnostic(int internalBlockId, KoploperDiagnosticCode code)
            => new(internalBlockId, code, 7, 0);

        public static KoploperStateSnapshot Snapshot(
            long sequence = 1,
            DateTimeOffset? capturedAtUtc = null,
            KoploperSourceHealth health = KoploperSourceHealth.Healthy,
            int retryCount = 0,
            bool isConsistent = true,
            IReadOnlyList<KoploperBlockSnapshot>? blocks = null,
            IReadOnlyList<KoploperBlockDiagnostic>? blockDiagnostics = null,
            IReadOnlyList<KoploperLocomotiveSnapshot>? locomotives = null,
            KoploperProcessGeneration? generation = null)
        {
            return new KoploperStateSnapshot(
                generation ?? Generation(),
                Identity(),
                sequence,
                capturedAtUtc ?? Now,
                TimeSpan.Zero,
                retryCount,
                isConsistent,
                health,
                blocks ?? Array.Empty<KoploperBlockSnapshot>(),
                blockDiagnostics ?? Array.Empty<KoploperBlockDiagnostic>(),
                locomotives ?? Array.Empty<KoploperLocomotiveSnapshot>());
        }

        public static KoploperSnapshotReadResult ReadResult(
            KoploperSourceHealth health,
            KoploperStateSnapshot? snapshot = null,
            IReadOnlyList<KoploperDiagnosticCode>? diagnostics = null)
        {
            return new KoploperSnapshotReadResult(health, snapshot, diagnostics ?? Array.Empty<KoploperDiagnosticCode>());
        }
    }
}
