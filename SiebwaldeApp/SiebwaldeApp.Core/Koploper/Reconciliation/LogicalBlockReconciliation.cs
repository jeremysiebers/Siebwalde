using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The block-level roll-up of a logical/physical reconciliation: for one Koploper internal block,
    /// whether ALL of its bound sections are safety-eligible. This does NOT replace the per-section
    /// <see cref="LogicalSectionReconciliation"/> enum — it is an additive roll-up (KIS10-02) for
    /// block-level consumers. <see cref="AllSectionsEligible"/> is false whenever the global
    /// reconciliation gates fail or any bound section is not <see cref="SectionSafetyEligibility.Eligible"/>.
    /// </summary>
    public sealed record LogicalBlockReconciliation(
        int InternalBlockId,
        int? LogicalOwnerLocomotiveId,
        bool AllSectionsEligible);
}
