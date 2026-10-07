using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Evaluates one snapshot read into a reservation observation. Pure and stateless: it never
    /// retains state between calls, never merges with a previous observation, and rebuilds the
    /// per-locomotive trajectories from scratch each cycle (the full snapshot is the truth).
    /// A non-authoritative result always publishes empty trajectories and conflicts — no stale
    /// ownership is ever inherited.
    /// </summary>
    public static class KoploperReservationAggregator
    {
        /// <summary>
        /// Aggregates <paramref name="readResult"/> into an observation. The
        /// <paramref name="observerSequence"/> is the observer's own monotonic counter,
        /// <paramref name="observedAtUtc"/> is the observation instant, and
        /// <paramref name="maxSnapshotAge"/> (when non-null) bounds freshness.
        /// </summary>
        public static KoploperReservationObservation Evaluate(
            KoploperSnapshotReadResult readResult,
            long observerSequence,
            DateTimeOffset observedAtUtc,
            TimeSpan? maxSnapshotAge)
        {
            ArgumentNullException.ThrowIfNull(readResult);

            KoploperStateSnapshot? snapshot = readResult.Snapshot;

            if (snapshot is null)
            {
                KoploperReservationAuthorityReason reason = readResult.Health switch
                {
                    KoploperSourceHealth.ProcessNotFound => KoploperReservationAuthorityReason.ProcessNotFound,
                    KoploperSourceHealth.UnsupportedVersion => KoploperReservationAuthorityReason.UnsupportedVersion,
                    KoploperSourceHealth.Inconsistent => KoploperReservationAuthorityReason.Inconsistent,
                    _ => KoploperReservationAuthorityReason.Unavailable
                };

                return new KoploperReservationObservation(
                    Generation: null,
                    SourceSequence: 0,
                    ObserverSequence: observerSequence,
                    CapturedAtUtc: observedAtUtc,
                    ObservedAtUtc: observedAtUtc,
                    IsFresh: false,
                    IsConsistent: false,
                    SourceHealth: readResult.Health,
                    IsAuthoritative: false,
                    AuthorityReason: reason,
                    Locomotives: Array.Empty<KoploperLocomotiveTrajectory>(),
                    Conflicts: Array.Empty<KoploperReservationConflict>(),
                    Diagnostics: readResult.Diagnostics.ToArray());
            }

            bool isFresh = maxSnapshotAge is null
                || KoploperSnapshotFreshness.IsCurrent(snapshot, observedAtUtc, maxSnapshotAge.Value);

            if (!isFresh)
            {
                return NonAuthoritative(
                    snapshot,
                    observerSequence,
                    observedAtUtc,
                    isFresh,
                    KoploperSourceHealth.Stale,
                    KoploperReservationAuthorityReason.Stale,
                    readResult.Diagnostics);
            }

            // Defensive: a snapshot produced by the reader is always consistent, but fail closed
            // if one is not.
            if (!snapshot.IsConsistent)
            {
                return NonAuthoritative(
                    snapshot,
                    observerSequence,
                    observedAtUtc,
                    isFresh,
                    KoploperSourceHealth.Inconsistent,
                    KoploperReservationAuthorityReason.Inconsistent,
                    readResult.Diagnostics);
            }

            KoploperReservationAuthorityReason authorityReason;
            switch (snapshot.SourceHealth)
            {
                case KoploperSourceHealth.Healthy:
                    authorityReason = KoploperReservationAuthorityReason.Authoritative;
                    break;

                case KoploperSourceHealth.Degraded:
                    if (snapshot.BlockDiagnostics.Count > 0)
                    {
                        authorityReason = KoploperReservationAuthorityReason.DegradedSemanticUnknown;
                    }
                    else if (snapshot.RetryCount > 0)
                    {
                        authorityReason = KoploperReservationAuthorityReason.DegradedRetryRecovered;
                    }
                    else
                    {
                        // Degraded always implies retries or block diagnostics; fail closed on a
                        // contradiction rather than publishing unverifiable authority.
                        return NonAuthoritative(
                            snapshot,
                            observerSequence,
                            observedAtUtc,
                            isFresh,
                            KoploperSourceHealth.Inconsistent,
                            KoploperReservationAuthorityReason.Inconsistent,
                            readResult.Diagnostics);
                    }
                    break;

                default:
                    // A non-null snapshot is only ever Healthy or Degraded; fail closed otherwise.
                    return NonAuthoritative(
                        snapshot,
                        observerSequence,
                        observedAtUtc,
                        isFresh,
                        KoploperSourceHealth.Inconsistent,
                        KoploperReservationAuthorityReason.Inconsistent,
                        readResult.Diagnostics);
            }

            (IReadOnlyList<KoploperLocomotiveTrajectory> locomotives,
                IReadOnlyList<KoploperReservationConflict> conflicts) = Aggregate(snapshot);

            var diagnostics = new List<KoploperDiagnosticCode>(readResult.Diagnostics.Count + snapshot.BlockDiagnostics.Count + 1);
            diagnostics.AddRange(readResult.Diagnostics);
            foreach (KoploperBlockDiagnostic blockDiagnostic in snapshot.BlockDiagnostics)
            {
                diagnostics.Add(blockDiagnostic.Code);
            }

            if (conflicts.Count > 0)
            {
                diagnostics.Add(KoploperDiagnosticCode.KOPLOPER_MULTIPLE_OCCUPIED_BLOCKS);
            }

            return new KoploperReservationObservation(
                Generation: snapshot.Generation,
                SourceSequence: snapshot.Sequence,
                ObserverSequence: observerSequence,
                CapturedAtUtc: snapshot.CapturedAtUtc,
                ObservedAtUtc: observedAtUtc,
                IsFresh: true,
                IsConsistent: snapshot.IsConsistent,
                SourceHealth: snapshot.SourceHealth,
                IsAuthoritative: true,
                AuthorityReason: authorityReason,
                Locomotives: locomotives,
                Conflicts: conflicts,
                Diagnostics: diagnostics);
        }

        private static KoploperReservationObservation NonAuthoritative(
            KoploperStateSnapshot snapshot,
            long observerSequence,
            DateTimeOffset observedAtUtc,
            bool isFresh,
            KoploperSourceHealth sourceHealth,
            KoploperReservationAuthorityReason reason,
            IReadOnlyList<KoploperDiagnosticCode> readDiagnostics)
        {
            return new KoploperReservationObservation(
                Generation: snapshot.Generation,
                SourceSequence: snapshot.Sequence,
                ObserverSequence: observerSequence,
                CapturedAtUtc: snapshot.CapturedAtUtc,
                ObservedAtUtc: observedAtUtc,
                IsFresh: isFresh,
                IsConsistent: snapshot.IsConsistent,
                SourceHealth: sourceHealth,
                IsAuthoritative: false,
                AuthorityReason: reason,
                Locomotives: Array.Empty<KoploperLocomotiveTrajectory>(),
                Conflicts: Array.Empty<KoploperReservationConflict>(),
                Diagnostics: readDiagnostics.ToArray());
        }

        private static (IReadOnlyList<KoploperLocomotiveTrajectory>, IReadOnlyList<KoploperReservationConflict>) Aggregate(
            KoploperStateSnapshot snapshot)
        {
            var occupiedByOwner = new Dictionary<int, List<int>>();
            var reservedByOwner = new Dictionary<int, HashSet<int>>();

            foreach (KoploperBlockSnapshot block in snapshot.Blocks)
            {
                if (block.OwnerLocomotiveId is not int ownerId)
                {
                    continue;
                }

                switch (block.State)
                {
                    case KoploperBlockState.Occupied:
                        if (!occupiedByOwner.TryGetValue(ownerId, out List<int>? occupied))
                        {
                            occupied = new List<int>();
                            occupiedByOwner[ownerId] = occupied;
                        }

                        occupied.Add(block.InternalBlockId);
                        break;

                    case KoploperBlockState.Reserved:
                        if (!reservedByOwner.TryGetValue(ownerId, out HashSet<int>? reserved))
                        {
                            reserved = new HashSet<int>();
                            reservedByOwner[ownerId] = reserved;
                        }

                        reserved.Add(block.InternalBlockId);
                        break;

                    default:
                        // Free, Transition and Unknown blocks do not contribute ownership.
                        break;
                }
            }

            var locomotives = new List<KoploperLocomotiveTrajectory>(snapshot.Locomotives.Count);
            var conflicts = new List<KoploperReservationConflict>();

            foreach (KoploperLocomotiveSnapshot locomotive in snapshot.Locomotives.OrderBy(l => l.InternalLocomotiveId))
            {
                int id = locomotive.InternalLocomotiveId;
                occupiedByOwner.TryGetValue(id, out List<int>? occupied);
                reservedByOwner.TryGetValue(id, out HashSet<int>? reserved);

                if (occupied is { Count: >= 2 })
                {
                    int[] occupiedBlocks = occupied.OrderBy(b => b).ToArray();
                    conflicts.Add(new KoploperReservationConflict(id, occupiedBlocks));
                    locomotives.Add(new KoploperLocomotiveTrajectory(
                        id,
                        null,
                        Array.Empty<int>(),
                        IsAuthoritative: false,
                        KoploperLocomotiveTrajectoryReason.MultipleOccupiedBlocks));
                }
                else
                {
                    int? occupiedBlock = occupied is { Count: 1 } ? occupied[0] : null;
                    int[] reservedBlocks = reserved is null
                        ? Array.Empty<int>()
                        : reserved.OrderBy(b => b).ToArray();

                    locomotives.Add(new KoploperLocomotiveTrajectory(
                        id,
                        occupiedBlock,
                        reservedBlocks,
                        IsAuthoritative: true,
                        KoploperLocomotiveTrajectoryReason.Authoritative));
                }
            }

            return (locomotives, conflicts);
        }
    }
}
