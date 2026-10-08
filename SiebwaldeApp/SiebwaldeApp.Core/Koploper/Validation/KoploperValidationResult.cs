namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// Outcome of one cross-check between an authoritative reservation observation and an
    /// independent sample. The numeric ordering is stable and not a severity ranking.
    /// </summary>
    public enum KoploperValidationResult
    {
        /// <summary>The observation and the independent sample agree.</summary>
        Match = 0,

        /// <summary>A transient difference consistent with an in-flight block transition.</summary>
        ExpectedTransitionSkew = 1,

        /// <summary>The independent source could not produce a comparable sample.</summary>
        SourceUnavailable = 2,

        /// <summary>The locomotive's current/occupied block differs between sources.</summary>
        CurrentPositionMismatch = 3,

        /// <summary>The set of owning locomotives differs between sources.</summary>
        OwnerMismatch = 4,

        /// <summary>The set of reserved blocks for a locomotive differs between sources.</summary>
        ReservationMismatch = 5,

        /// <summary>The sources were captured against different process generations.</summary>
        GenerationMismatch = 6,

        /// <summary>The reservation observation is not authoritative, so it cannot be cross-checked.</summary>
        NonAuthoritativeSource = 7,

        /// <summary>An unexpected result not covered by the other values.</summary>
        Unknown = 8
    }
}
