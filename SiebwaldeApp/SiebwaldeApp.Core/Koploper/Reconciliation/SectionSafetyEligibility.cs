namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Whether a logical section's reconciled state is eligible to grant safety authority to its
    /// logical owner. The numeric ordering is stable and not a severity ranking. This is a
    /// fail-closed stance: only an explicit, consistent reservation/occupancy match is eligible.
    /// </summary>
    public enum SectionSafetyEligibility
    {
        /// <summary>The reconciled state is consistent with its owner and may be considered eligible.</summary>
        Eligible = 0,

        /// <summary>The reconciled state must not be considered eligible (contradiction, manual block, or a failed global gate).</summary>
        Denied = 1,

        /// <summary>The section could not be assessed, so eligibility cannot be established.</summary>
        NotAssessable = 2
    }
}
