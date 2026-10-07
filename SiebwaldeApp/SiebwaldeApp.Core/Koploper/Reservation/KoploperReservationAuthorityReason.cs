namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Why a <see cref="KoploperReservationObservation"/> carries the authority flag it does.
    /// The numeric ordering is stable and not a severity ranking.
    /// </summary>
    public enum KoploperReservationAuthorityReason
    {
        /// <summary>The observation is derived from a fresh, coherent, healthy snapshot.</summary>
        Authoritative = 0,

        /// <summary>Fresh and coherent, degraded only by retries with no per-block diagnostics.</summary>
        DegradedRetryRecovered = 1,

        /// <summary>Fresh and coherent, degraded with per-block diagnostics; Unknown blocks are excluded.</summary>
        DegradedSemanticUnknown = 2,

        /// <summary>The snapshot exceeded the configured maximum age.</summary>
        Stale = 3,

        /// <summary>The snapshot could not be produced coherently.</summary>
        Inconsistent = 4,

        /// <summary>The source could not be read.</summary>
        Unavailable = 5,

        /// <summary>The executable did not match the supported binary hash/version.</summary>
        UnsupportedVersion = 6,

        /// <summary>The Koploper process is not running.</summary>
        ProcessNotFound = 7
    }
}
