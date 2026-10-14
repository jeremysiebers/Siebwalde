using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The reconciled view of one locomotive: its logical occupied/reserved Koploper internal BLOCKS
    /// (KIS10-02 block-level shadow) plus two fail-closed roll-ups over the sections those blocks
    /// resolve to. Both roll-ups are <c>false</c> whenever the global reconciliation gates fail,
    /// regardless of what the locomotive owns.
    /// </summary>
    public sealed record LogicalLocomotiveReconciliation(
        int LocomotiveId,
        int? OccupiedBlock,
        IReadOnlyCollection<int> ReservedBlocks,
        bool AllOwnedSectionsAssessable,
        bool AllOwnedSectionsConsistent);
}
