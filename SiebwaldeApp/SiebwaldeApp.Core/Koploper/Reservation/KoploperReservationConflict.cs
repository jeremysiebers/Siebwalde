using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A reservation conflict for one locomotive: the locomotive owns more than one occupied
    /// block in the same snapshot. <see cref="OccupiedBlocks"/> is sorted for determinism.
    /// </summary>
    public sealed record KoploperReservationConflict(
        int InternalLocomotiveId,
        IReadOnlyList<int> OccupiedBlocks);
}
