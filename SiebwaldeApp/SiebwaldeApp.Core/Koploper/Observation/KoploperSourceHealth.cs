namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Overall health of the Koploper read-only internal-state source, as observed for a single
    /// snapshot read. The numeric ordering is stable and not a severity ranking.
    /// </summary>
    public enum KoploperSourceHealth
    {
        /// <summary>A coherent snapshot was produced with no retries and no block diagnostics.</summary>
        Healthy = 0,

        /// <summary>A coherent snapshot was produced, but with retries or per-block diagnostics.</summary>
        Degraded = 1,

        /// <summary>The source could not be read (access denied, ambiguous process, unverifiable binary).</summary>
        Unavailable = 2,

        /// <summary>The executable did not match the supported binary hash/version.</summary>
        UnsupportedVersion = 3,

        /// <summary>The Koploper process is not running.</summary>
        ProcessNotFound = 4,

        /// <summary>A previously captured snapshot has exceeded the configured maximum age.</summary>
        Stale = 5,

        /// <summary>Repeated reads could not produce a coherent snapshot.</summary>
        Inconsistent = 6
    }
}
