namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Overall health of the physical section occupancy source, as observed for a single read.
    /// The numeric ordering is stable and not a severity ranking.
    /// </summary>
    public enum PhysicalSourceHealth
    {
        /// <summary>Every mapped section produced a fresh physical observation.</summary>
        Healthy = 0,

        /// <summary>At least one mapped section was stale or missing (occupancy unknown).</summary>
        Degraded = 1,

        /// <summary>The source could not produce any section state (no sections or an invalid mapping).</summary>
        Unavailable = 2
    }
}
