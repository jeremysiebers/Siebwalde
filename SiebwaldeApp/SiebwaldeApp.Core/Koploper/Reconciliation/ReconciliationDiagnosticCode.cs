namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Diagnostic codes for the logical/physical reconciliation. Each code names one observed
    /// condition. The numeric ordering is stable and not a severity ranking.
    /// </summary>
    public enum ReconciliationDiagnosticCode
    {
        /// <summary>The logical-section shadow was null or not shadow-valid.</summary>
        ReconLogicalSourceInvalid = 0,

        /// <summary>The physical occupancy source was null, invalid or unavailable.</summary>
        ReconPhysicalSourceInvalid = 1,

        /// <summary>The logical and physical observations carry different profile identities.</summary>
        ReconProfileMismatch = 2,

        /// <summary>A logical section has no physical section state in the physical observation.</summary>
        ReconPhysicalSectionMissing = 3,

        /// <summary>The physical occupancy for a section is unknown.</summary>
        ReconPhysicalUnknown = 4,

        /// <summary>The physical occupancy for a section is not fresh.</summary>
        ReconPhysicalStale = 5,

        /// <summary>A section is physically occupied but no locomotive owns it logically.</summary>
        ReconFreePhysicallyOccupied = 6,

        /// <summary>A locomotive owns a section but the physical section is clear.</summary>
        ReconOccupiedPhysicallyClear = 7,

        /// <summary>A section is reserved but is already physically occupied.</summary>
        ReconReservedPhysicallyOccupied = 8,

        /// <summary>A section is manually blocked and therefore has no automatic owner.</summary>
        ReconManualBlocked = 9,

        /// <summary>A section has no physical binding (not in the logically bound section set).</summary>
        ReconSectionBindingInvalid = 10
    }
}
