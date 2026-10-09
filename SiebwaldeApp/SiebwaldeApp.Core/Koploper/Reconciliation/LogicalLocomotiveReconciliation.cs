using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The reconciled view of one locomotive: its logical occupied/reserved sections plus two
    /// fail-closed roll-ups over those owned sections. Both roll-ups are <c>false</c> whenever the
    /// global reconciliation gates fail, regardless of what the locomotive owns.
    /// </summary>
    public sealed record LogicalLocomotiveReconciliation(
        int LocomotiveId,
        int? OccupiedSection,
        IReadOnlyCollection<int> ReservedSections,
        bool AllOwnedSectionsAssessable,
        bool AllOwnedSectionsConsistent);
}
