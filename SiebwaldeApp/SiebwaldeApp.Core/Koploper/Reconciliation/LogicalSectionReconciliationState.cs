namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The reconciled safety state of one logical section: how the logical (Koploper) role and the
    /// physical occupancy compare. The numeric ordering is stable and not a severity ranking; the
    /// <see cref="SectionSafetyEligibility"/> on the section record carries the eligibility stance.
    /// </summary>
    public enum LogicalSectionReconciliationState
    {
        /// <summary>The logical role is free and the physical section is clear.</summary>
        ConsistFreeClear = 0,

        /// <summary>The logical role is reserved and the physical section is clear.</summary>
        ConsistReservedClear = 1,

        /// <summary>The logical role is occupied and the physical section is occupied.</summary>
        ConsistOccupied = 2,

        /// <summary>The section is physically occupied but no locomotive owns it logically.</summary>
        ContradictionPhysicalWithoutLogicalOwner = 3,

        /// <summary>A locomotive owns the section but the physical section is clear.</summary>
        ContradictionLogicalOccupiedPhysicalClear = 4,

        /// <summary>The section is reserved but is already physically occupied.</summary>
        ContradictionReservedPhysicallyOccupied = 5,

        /// <summary>The section is manually blocked (operator reservation, no automatic owner).</summary>
        ManualBlocked = 6,

        /// <summary>The section could not be assessed (binding, missing, unknown or stale physical data).</summary>
        NotAssessable = 7
    }
}
