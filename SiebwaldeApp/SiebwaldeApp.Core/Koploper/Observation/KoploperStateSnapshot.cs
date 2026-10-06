using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// One coherent, decoded snapshot of the Koploper internal state, together with the
    /// read metadata needed for freshness and health decisions. <see cref="IsConsistent"/> is
    /// always true for a snapshot produced by the coherent-read reader; the field is kept
    /// explicit so the snapshot carries its own provenance.
    /// </summary>
    public sealed record KoploperStateSnapshot(
        KoploperProcessGeneration Generation,
        KoploperExecutableIdentity ExecutableIdentity,
        long Sequence,
        DateTimeOffset CapturedAtUtc,
        TimeSpan ReadDuration,
        int RetryCount,
        bool IsConsistent,
        KoploperSourceHealth SourceHealth,
        IReadOnlyList<KoploperBlockSnapshot> Blocks,
        IReadOnlyList<KoploperBlockDiagnostic> BlockDiagnostics,
        IReadOnlyList<KoploperLocomotiveSnapshot> Locomotives);
}
