using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Result of one snapshot read attempt. <see cref="Snapshot"/> is non-null exactly when
    /// <see cref="Health"/> is <see cref="KoploperSourceHealth.Healthy"/> or
    /// <see cref="KoploperSourceHealth.Degraded"/>; otherwise the read failed and
    /// <see cref="Diagnostics"/> carries the observed condition(s).
    /// </summary>
    public sealed record KoploperSnapshotReadResult(
        KoploperSourceHealth Health,
        KoploperStateSnapshot? Snapshot,
        IReadOnlyList<KoploperDiagnosticCode> Diagnostics);
}
