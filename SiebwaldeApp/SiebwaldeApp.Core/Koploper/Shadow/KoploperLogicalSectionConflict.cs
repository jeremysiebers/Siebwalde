namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The kind of block-level shadow conflict. The numeric ordering is stable and not a severity
    /// ranking.
    /// </summary>
    public enum KoploperLogicalSectionConflictKind
    {
        /// <summary>A block is both manually blocked and automatically reserved/occupied.</summary>
        ManualVsAutomatic = 0,

        /// <summary>A locomotive occupies more than one mapped Koploper block in one snapshot.</summary>
        MultipleOccupiedBlocks = 1
    }

    /// <summary>
    /// A single block-level shadow conflict. <see cref="InternalBlockId"/> is the offending Koploper
    /// block and <see cref="OwnerLocomotiveId"/> the owning locomotive (when any). In the block-level
    /// shadow (KIS10-02) the offending block fully identifies the conflict, so the former bound
    /// logical-section id is no longer carried (a block now resolves to 1..N sections).
    /// </summary>
    public sealed record KoploperLogicalSectionConflict(
        KoploperLogicalSectionConflictKind Kind,
        int InternalBlockId,
        int? OwnerLocomotiveId);
}
