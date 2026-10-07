using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// One reservation observation published by the reservation observer: the authoritative
    /// per-locomotive trajectories derived from a single coherent snapshot, plus the authority
    /// and health provenance used to decide whether those trajectories may be trusted. A
    /// non-authoritative observation always publishes empty <see cref="Locomotives"/> and
    /// <see cref="Conflicts"/> — stale ownership is never inherited across cycles.
    /// </summary>
    public sealed record KoploperReservationObservation(
        KoploperProcessGeneration? Generation,
        long SourceSequence,
        long ObserverSequence,
        DateTimeOffset CapturedAtUtc,
        DateTimeOffset ObservedAtUtc,
        bool IsFresh,
        bool IsConsistent,
        KoploperSourceHealth SourceHealth,
        bool IsAuthoritative,
        KoploperReservationAuthorityReason AuthorityReason,
        IReadOnlyList<KoploperLocomotiveTrajectory> Locomotives,
        IReadOnlyList<KoploperReservationConflict> Conflicts,
        IReadOnlyList<KoploperDiagnosticCode> Diagnostics)
    {
        /// <summary>Elapsed time between snapshot capture and observation.</summary>
        public TimeSpan Age => ObservedAtUtc - CapturedAtUtc;
    }
}
