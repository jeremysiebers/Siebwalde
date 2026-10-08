using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// One flattened validation sample: the authoritative observation context plus the
    /// independent cross-check context and the resulting <see cref="KoploperValidationResult"/>.
    /// This is the unit written to a validation trace.
    /// </summary>
    public sealed record KoploperValidationSample(
        DateTimeOffset ValidatedAtUtc,
        int ProcessId,
        KoploperProcessGeneration? Generation,
        long SourceSequence,
        long ObserverSequence,
        KoploperSourceHealth SourceHealth,
        bool IsAuthoritative,
        KoploperReservationAuthorityReason AuthorityReason,
        int? LocId,
        int? OccupiedBlock,
        IReadOnlyList<int> ReservedBlocks,
        int? Port5700CurrentBlock,
        int? Port5700Loc,
        KoploperCrossCheckSource? CrossCheckSource,
        string? GuiMarker,
        string? ScenarioId,
        KoploperValidationResult Result,
        string? Note);
}
