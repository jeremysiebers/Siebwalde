using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for <see cref="KoploperReservationAggregator"/> covering the full aggregation
    /// and authority matrix. Snapshots and read results are built directly via
    /// <see cref="KoploperReservationTestFixtures"/>.
    /// </summary>
    public class KoploperReservationAggregatorTests
    {
        private static readonly DateTimeOffset Now = KoploperReservationTestFixtures.Now;
        private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(5);

        private static KoploperReservationObservation Evaluate(
            KoploperSnapshotReadResult result,
            long observerSequence = 1,
            DateTimeOffset? observedAtUtc = null,
            TimeSpan? maxSnapshotAge = null)
        {
            return KoploperReservationAggregator.Evaluate(
                result,
                observerSequence,
                observedAtUtc ?? Now,
                maxSnapshotAge ?? MaxAge);
        }

        // ---------------------------------------------------------------- basic aggregation

        [Fact]
        public void Occupied_SingleOwner_SetsOccupiedBlock()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            KoploperLocomotiveTrajectory trajectory = Assert.Single(observation.Locomotives);
            Assert.Equal(24, trajectory.InternalLocomotiveId);
            Assert.Equal(1, trajectory.OccupiedBlock);
            Assert.Empty(trajectory.ReservedBlocks);
            Assert.True(trajectory.IsAuthoritative);
            Assert.Equal(KoploperLocomotiveTrajectoryReason.Authoritative, trajectory.Reason);
        }

        [Fact]
        public void Reserved_SingleOwner_AddsToReservedBlocks()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(7, 24, KoploperBlockState.Reserved)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            KoploperLocomotiveTrajectory trajectory = Assert.Single(observation.Locomotives);
            Assert.Null(trajectory.OccupiedBlock);
            Assert.Equal(new[] { 7 }, trajectory.ReservedBlocks);
            Assert.True(trajectory.IsAuthoritative);
        }

        [Fact]
        public void Reserved_MultipleBlocks_SortedAndUnique()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(3, 24, KoploperBlockState.Reserved),
                    KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Reserved),
                    KoploperReservationTestFixtures.Block(2, 24, KoploperBlockState.Reserved)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            KoploperLocomotiveTrajectory trajectory = Assert.Single(observation.Locomotives);
            Assert.Equal(new[] { 1, 2, 3 }, trajectory.ReservedBlocks);
        }

        [Fact]
        public void OccupiedAndReserved_SameOwner_BothReflected()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(5, 24, KoploperBlockState.Reserved),
                    KoploperReservationTestFixtures.Block(2, 24, KoploperBlockState.Occupied)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            KoploperLocomotiveTrajectory trajectory = Assert.Single(observation.Locomotives);
            Assert.Equal(2, trajectory.OccupiedBlock);
            Assert.Equal(new[] { 5 }, trajectory.ReservedBlocks);
        }

        [Fact]
        public void MultipleLocomotives_SortedByInternalId()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(1, 42, KoploperBlockState.Occupied),
                    KoploperReservationTestFixtures.Block(2, 7, KoploperBlockState.Occupied),
                    KoploperReservationTestFixtures.Block(3, 24, KoploperBlockState.Reserved)
                },
                locomotives: new[]
                {
                    KoploperReservationTestFixtures.Locomotive(24),
                    KoploperReservationTestFixtures.Locomotive(42),
                    KoploperReservationTestFixtures.Locomotive(7)
                });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            Assert.Equal(new[] { 7, 24, 42 }, observation.Locomotives.Select(l => l.InternalLocomotiveId).ToArray());
            Assert.Equal(2, observation.Locomotives[0].OccupiedBlock);
            Assert.Equal(new[] { 3 }, observation.Locomotives[1].ReservedBlocks);
            Assert.Equal(1, observation.Locomotives[2].OccupiedBlock);
        }

        [Fact]
        public void FreeBlock_Ignored()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(1, null, KoploperBlockState.Free)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            KoploperLocomotiveTrajectory trajectory = Assert.Single(observation.Locomotives);
            Assert.Null(trajectory.OccupiedBlock);
            Assert.Empty(trajectory.ReservedBlocks);
            Assert.True(trajectory.IsAuthoritative);
        }

        [Fact]
        public void TransitionBlock_Ignored()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(1, null, KoploperBlockState.Transition)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            KoploperLocomotiveTrajectory trajectory = Assert.Single(observation.Locomotives);
            Assert.Null(trajectory.OccupiedBlock);
            Assert.Empty(trajectory.ReservedBlocks);
        }

        [Fact]
        public void UnknownBlock_Excluded_NoOwnership()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(9, null, KoploperBlockState.Unknown)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            KoploperLocomotiveTrajectory trajectory = Assert.Single(observation.Locomotives);
            Assert.Null(trajectory.OccupiedBlock);
            Assert.Empty(trajectory.ReservedBlocks);
        }

        [Fact]
        public void Reservation_Disappears_NoStaleOwnership()
        {
            var occupiedSnapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation first = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, occupiedSnapshot));
            Assert.Equal(1, Assert.Single(first.Locomotives).OccupiedBlock);

            // The block is now Free: the full snapshot is truth, so no ownership may be inherited.
            var freeSnapshot = KoploperReservationTestFixtures.Snapshot(
                sequence: 2,
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(1, null, KoploperBlockState.Free)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation second = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, freeSnapshot));

            KoploperLocomotiveTrajectory trajectory = Assert.Single(second.Locomotives);
            Assert.Null(trajectory.OccupiedBlock);
            Assert.Empty(trajectory.ReservedBlocks);
        }

        [Fact]
        public void FullSnapshotTruth_AtomicReplacement()
        {
            var reservedSnapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Reserved),
                    KoploperReservationTestFixtures.Block(2, 24, KoploperBlockState.Reserved)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation first = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, reservedSnapshot));
            Assert.Equal(new[] { 1, 2 }, Assert.Single(first.Locomotives).ReservedBlocks);

            // The second snapshot replaces the reserved set entirely (block 1 no longer reserved).
            var replacedSnapshot = KoploperReservationTestFixtures.Snapshot(
                sequence: 2,
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(3, 24, KoploperBlockState.Reserved)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation second = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, replacedSnapshot));
            Assert.Equal(new[] { 3 }, Assert.Single(second.Locomotives).ReservedBlocks);
        }

        [Fact]
        public void DuplicateReservedBlocks_Deduplicated()
        {
            // Pathological duplicate reserved entries for the same owner must collapse to one.
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(4, 24, KoploperBlockState.Reserved),
                    KoploperReservationTestFixtures.Block(4, 24, KoploperBlockState.Reserved)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            KoploperLocomotiveTrajectory trajectory = Assert.Single(observation.Locomotives);
            Assert.Equal(new[] { 4 }, trajectory.ReservedBlocks);
        }

        // ---------------------------------------------------------------- conflict

        [Fact]
        public void MultipleOccupied_SameOwner_ConflictAndNonAuthoritativeTrajectory()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(2, 24, KoploperBlockState.Occupied),
                    KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            Assert.False(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.OwnershipConflict, observation.AuthorityReason);

            KoploperReservationConflict conflict = Assert.Single(observation.Conflicts);
            Assert.Equal(24, conflict.InternalLocomotiveId);
            Assert.Equal(new[] { 1, 2 }, conflict.OccupiedBlocks);

            KoploperLocomotiveTrajectory trajectory = Assert.Single(observation.Locomotives);
            Assert.False(trajectory.IsAuthoritative);
            Assert.Equal(KoploperLocomotiveTrajectoryReason.MultipleOccupiedBlocks, trajectory.Reason);
            Assert.Null(trajectory.OccupiedBlock);
            Assert.Empty(trajectory.ReservedBlocks);

            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_MULTIPLE_OCCUPIED_BLOCKS, observation.Diagnostics);
        }

        [Fact]
        public void DegradedRetryRecovered_WithOwnershipConflict_GlobalNonAuthoritative()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                health: KoploperSourceHealth.Degraded,
                retryCount: 1,
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(2, 24, KoploperBlockState.Occupied),
                    KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Degraded, snapshot));

            Assert.False(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.OwnershipConflict, observation.AuthorityReason);
            Assert.Single(observation.Conflicts);
        }

        [Fact]
        public void OwnershipConflict_OtherLocosRemainPerLocoAuthoritative()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                health: KoploperSourceHealth.Healthy,
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied),
                    KoploperReservationTestFixtures.Block(2, 24, KoploperBlockState.Occupied),
                    KoploperReservationTestFixtures.Block(3, 7, KoploperBlockState.Occupied)
                },
                locomotives: new[]
                {
                    KoploperReservationTestFixtures.Locomotive(24),
                    KoploperReservationTestFixtures.Locomotive(7)
                });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            Assert.False(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.OwnershipConflict, observation.AuthorityReason);

            KoploperLocomotiveTrajectory conflicted = Assert.Single(observation.Locomotives.Where(l => l.InternalLocomotiveId == 24));
            Assert.False(conflicted.IsAuthoritative);
            Assert.Equal(KoploperLocomotiveTrajectoryReason.MultipleOccupiedBlocks, conflicted.Reason);

            KoploperLocomotiveTrajectory other = Assert.Single(observation.Locomotives.Where(l => l.InternalLocomotiveId == 7));
            Assert.True(other.IsAuthoritative);
            Assert.Equal(KoploperLocomotiveTrajectoryReason.Authoritative, other.Reason);
            Assert.Equal(3, other.OccupiedBlock);
        }

        // ---------------------------------------------------------------- authority matrix

        [Fact]
        public void Healthy_Authoritative()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                health: KoploperSourceHealth.Healthy,
                blocks: new[] { KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied) },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            Assert.True(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.Authoritative, observation.AuthorityReason);
            Assert.True(observation.IsFresh);
            Assert.True(observation.IsConsistent);
            Assert.Equal(KoploperSourceHealth.Healthy, observation.SourceHealth);
            Assert.Single(observation.Locomotives);
        }

        [Fact]
        public void Degraded_RetryOnly_AuthoritativeDegradedRetryRecovered()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                health: KoploperSourceHealth.Degraded,
                retryCount: 1,
                blocks: new[] { KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied) },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Degraded, snapshot));

            Assert.True(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.DegradedRetryRecovered, observation.AuthorityReason);
            Assert.Single(observation.Locomotives);
        }

        [Fact]
        public void Degraded_SemanticUnknown_NonAuthoritative()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                health: KoploperSourceHealth.Degraded,
                retryCount: 0,
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied),
                    KoploperReservationTestFixtures.Block(9, null, KoploperBlockState.Unknown)
                },
                blockDiagnostics: new[]
                {
                    KoploperReservationTestFixtures.BlockDiagnostic(9, KoploperDiagnosticCode.KOPLOPER_UNKNOWN_BLOCK_STATE)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Degraded, snapshot));

            Assert.False(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.SemanticUnknown, observation.AuthorityReason);
            Assert.Empty(observation.Locomotives);
            Assert.Empty(observation.Conflicts);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_UNKNOWN_BLOCK_STATE, observation.Diagnostics);
            Assert.Equal(KoploperSourceHealth.Degraded, observation.SourceHealth);
            Assert.True(observation.IsFresh);
            Assert.True(observation.IsConsistent);
        }

        [Fact]
        public void Degraded_RetryAndSemantic_SemanticTakesPrecedence()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                health: KoploperSourceHealth.Degraded,
                retryCount: 2,
                blocks: new[]
                {
                    KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied),
                    KoploperReservationTestFixtures.Block(9, null, KoploperBlockState.Unknown)
                },
                blockDiagnostics: new[]
                {
                    KoploperReservationTestFixtures.BlockDiagnostic(9, KoploperDiagnosticCode.KOPLOPER_OWNER_NOT_FOUND)
                },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Degraded, snapshot));

            Assert.False(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.SemanticUnknown, observation.AuthorityReason);
            Assert.Empty(observation.Locomotives);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_OWNER_NOT_FOUND, observation.Diagnostics);
        }

        [Fact]
        public void Stale_NonAuthoritativeEmptyObservation()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                capturedAtUtc: Now.AddSeconds(-10),
                blocks: new[] { KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied) },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            Assert.False(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.Stale, observation.AuthorityReason);
            Assert.False(observation.IsFresh);
            Assert.Equal(KoploperSourceHealth.Stale, observation.SourceHealth);
            Assert.Empty(observation.Locomotives);
            Assert.Empty(observation.Conflicts);
        }

        [Fact]
        public void InconsistentNullSnapshot_NonAuthoritative()
        {
            KoploperReservationObservation observation = Evaluate(
                KoploperReservationTestFixtures.ReadResult(
                    KoploperSourceHealth.Inconsistent,
                    diagnostics: new[] { KoploperDiagnosticCode.KOPLOPER_SNAPSHOT_INCONSISTENT }));

            Assert.False(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.Inconsistent, observation.AuthorityReason);
            Assert.Null(observation.Generation);
            Assert.Equal(0, observation.SourceSequence);
            Assert.Empty(observation.Locomotives);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_SNAPSHOT_INCONSISTENT, observation.Diagnostics);
        }

        [Fact]
        public void InconsistentSnapshotFlag_NonAuthoritative()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                isConsistent: false,
                blocks: new[] { KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied) },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            Assert.False(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.Inconsistent, observation.AuthorityReason);
            Assert.Empty(observation.Locomotives);
        }

        [Fact]
        public void UnsupportedVersion_NonAuthoritative()
        {
            KoploperReservationObservation observation = Evaluate(
                KoploperReservationTestFixtures.ReadResult(
                    KoploperSourceHealth.UnsupportedVersion,
                    diagnostics: new[] { KoploperDiagnosticCode.KOPLOPER_UNSUPPORTED_BINARY }));

            Assert.False(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.UnsupportedVersion, observation.AuthorityReason);
            Assert.Empty(observation.Locomotives);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_UNSUPPORTED_BINARY, observation.Diagnostics);
        }

        [Fact]
        public void ProcessNotFound_NonAuthoritative()
        {
            KoploperReservationObservation observation = Evaluate(
                KoploperReservationTestFixtures.ReadResult(
                    KoploperSourceHealth.ProcessNotFound,
                    diagnostics: new[] { KoploperDiagnosticCode.KOPLOPER_PROCESS_NOT_FOUND }));

            Assert.False(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.ProcessNotFound, observation.AuthorityReason);
            Assert.Empty(observation.Locomotives);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_PROCESS_NOT_FOUND, observation.Diagnostics);
        }

        [Fact]
        public void Unavailable_NonAuthoritative()
        {
            KoploperReservationObservation observation = Evaluate(
                KoploperReservationTestFixtures.ReadResult(
                    KoploperSourceHealth.Unavailable,
                    diagnostics: new[] { KoploperDiagnosticCode.KOPLOPER_ACCESS_DENIED }));

            Assert.False(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.Unavailable, observation.AuthorityReason);
            Assert.Empty(observation.Locomotives);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_ACCESS_DENIED, observation.Diagnostics);
        }

        // ---------------------------------------------------------------- provenance / sequence

        [Fact]
        public void GenerationAndSourceSequence_Propagated()
        {
            KoploperProcessGeneration generation = KoploperReservationTestFixtures.Generation(200, 2000);
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                sequence: 1,
                generation: generation,
                blocks: new[] { KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied) },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            Assert.Equal(generation, observation.Generation);
            Assert.Equal(1, observation.SourceSequence);
        }

        [Fact]
        public void ObserverSequence_PropagatedToObservation()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                blocks: new[] { KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied) },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(
                KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot),
                observerSequence: 42);

            Assert.Equal(42, observation.ObserverSequence);
        }

        [Fact]
        public void NullSnapshot_SourceSequenceZero()
        {
            KoploperReservationObservation observation = Evaluate(
                KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.ProcessNotFound, diagnostics: new[] { KoploperDiagnosticCode.KOPLOPER_PROCESS_NOT_FOUND }));

            Assert.Equal(0, observation.SourceSequence);
        }

        [Fact]
        public void Age_IsObservedMinusCaptured()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                capturedAtUtc: Now.AddSeconds(-3),
                blocks: new[] { KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied) },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = Evaluate(KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot));

            Assert.Equal(TimeSpan.FromSeconds(3), observation.Age);
        }

        [Fact]
        public void MaxSnapshotAgeNull_AlwaysFresh()
        {
            var snapshot = KoploperReservationTestFixtures.Snapshot(
                capturedAtUtc: Now.AddSeconds(-1000),
                blocks: new[] { KoploperReservationTestFixtures.Block(1, 24, KoploperBlockState.Occupied) },
                locomotives: new[] { KoploperReservationTestFixtures.Locomotive(24) });

            KoploperReservationObservation observation = KoploperReservationAggregator.Evaluate(
                KoploperReservationTestFixtures.ReadResult(KoploperSourceHealth.Healthy, snapshot),
                1,
                Now,
                maxSnapshotAge: null);

            Assert.True(observation.IsFresh);
            Assert.True(observation.IsAuthoritative);
            Assert.Equal(KoploperReservationAuthorityReason.Authoritative, observation.AuthorityReason);
        }
    }
}
