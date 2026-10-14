using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The block-level projection for one locomotive: its currently occupied Koploper internal block
    /// (if exactly one mapped occupied block) and the set of Koploper internal blocks reserved for it.
    /// A block resolves to 1..N logical sections (KIS10-02), but the shadow stays block-level and never
    /// exposes a per-section occupancy. <see cref="ReservedLogicalBlocks"/> is a set, sorted ascending
    /// for determinism.
    /// </summary>
    public sealed record LogicalLocomotiveShadow(
        int InternalLocomotiveId,
        int? OccupiedLogicalBlock,
        IReadOnlyCollection<int> ReservedLogicalBlocks);
}
