using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The reservation trajectory derived for one locomotive from a single coherent snapshot:
    /// its currently occupied block (if exactly one), the set of blocks reserved for it, and
    /// whether that trajectory can be treated as authoritative. When a locomotive owns more
    /// than one occupied block the trajectory is not authoritative and both
    /// <see cref="OccupiedBlock"/> and <see cref="ReservedBlocks"/> are empty.
    /// </summary>
    public sealed record KoploperLocomotiveTrajectory(
        int InternalLocomotiveId,
        int? OccupiedBlock,
        IReadOnlyCollection<int> ReservedBlocks,
        bool IsAuthoritative,
        KoploperLocomotiveTrajectoryReason Reason);
}
