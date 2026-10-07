using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// One reservation observation published by the reservation observer: the authoritative
    /// per-locomotive trajectories derived from a single coherent snapshot, plus the authority
    /// and health provenance used to decide whether those trajectories may be trusted.
    /// <para>
    /// Stale ownership is never inherited across cycles: <see cref="Locomotives"/> and
    /// <see cref="Conflicts"/> are rebuilt from scratch each cycle and are empty for source-level
    /// failures (Stale / Inconsistent / Unavailable / UnsupportedVersion / ProcessNotFound /
    /// SemanticUnknown). The <c>OwnershipConflict</c> case keeps the global
    /// <see cref="IsAuthoritative"/> = <c>false</c> as the single authority gate but may still
    /// carry per-loco trajectory detail for diagnostics; consumers must gate on the global
    /// <see cref="IsAuthoritative"/>, never on per-loco flags alone.
    /// </para>
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
