using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The reconciled state of one logical section: the automatic (logical) role, the physical
    /// occupancy, and the safety stance derived by comparing them. The physical occupancy never
    /// carries a locomotive identity here — <see cref="LogicalOwnerLocomotiveId"/> comes only from
    /// the logical shadow, never from the physical reading.
    /// </summary>
    public sealed record LogicalSectionReconciliation(
        int LogicalSectionId,
        AutomaticSectionRole AutomaticRole,
        int? LogicalOwnerLocomotiveId,
        bool ManualBlocked,
        PhysicalOccupancy PhysicalOccupancy,
        bool PhysicalFresh,
        LogicalSectionReconciliationState ReconciliationState,
        SectionSafetyEligibility SectionSafetyEligibility,
        IReadOnlyList<ReconciliationDiagnosticCode> Diagnostics)
    {
        /// <summary>
        /// True only when this section is eligible to grant safety authority to its logical owner.
        /// </summary>
        public bool SectionSafetyEligibleForOwner => SectionSafetyEligibility == SectionSafetyEligibility.Eligible;
    }
}
