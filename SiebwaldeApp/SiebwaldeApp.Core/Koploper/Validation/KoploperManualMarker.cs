using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>The kind of manual action an operator/scenario recorded.</summary>
    public enum KoploperManualMarkerKind
    {
        /// <summary>A reservation was created (for example by dragging a locomotive onto a block).</summary>
        CreateReservation = 0,

        /// <summary>A reservation was cancelled (the attempt may or may not have taken effect).</summary>
        CancelReservationAttempt = 1,

        /// <summary>A GUI state was observed and noted by the operator.</summary>
        ObservedGui = 2
    }

    /// <summary>
    /// A manually recorded marker tied to a validation scenario. Markers are evidence of operator
    /// action or GUI observation, kept separate from automatically captured samples.
    /// </summary>
    public sealed record KoploperManualMarker(
        string ScenarioId,
        DateTimeOffset MarkedAtUtc,
        int? LocId,
        IReadOnlyList<int> BlockIds,
        KoploperManualMarkerKind Kind,
        string Note);
}
