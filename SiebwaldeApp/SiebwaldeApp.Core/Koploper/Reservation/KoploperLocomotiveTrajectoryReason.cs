namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Why a <see cref="KoploperLocomotiveTrajectory"/> carries the authority flag it does.
    /// The numeric ordering is stable and not a severity ranking.
    /// </summary>
    public enum KoploperLocomotiveTrajectoryReason
    {
        /// <summary>The trajectory is derived from a single authoritative occupied block.</summary>
        Authoritative = 0,

        /// <summary>The locomotive owns more than one occupied block; the trajectory is not authoritative.</summary>
        MultipleOccupiedBlocks = 1
    }
}
