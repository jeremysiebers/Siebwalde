namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The kind of logical-section shadow conflict. The numeric ordering is stable and not a
    /// severity ranking.
    /// </summary>
    public enum KoploperLogicalSectionConflictKind
    {
        /// <summary>A block is both manually blocked and automatically reserved/occupied.</summary>
        ManualVsAutomatic = 0,

        /// <summary>A locomotive occupies more than one mapped logical section in one snapshot.</summary>
        MultipleOccupiedSections = 1
    }

    /// <summary>
    /// A single logical-section shadow conflict. <see cref="InternalBlockId"/> is the offending
    /// Koploper block, <see cref="LogicalSectionId"/> the bound section, and
    /// <see cref="OwnerLocomotiveId"/> the owning locomotive (when any).
    /// </summary>
    public sealed record KoploperLogicalSectionConflict(
        KoploperLogicalSectionConflictKind Kind,
        int InternalBlockId,
        int LogicalSectionId,
        int? OwnerLocomotiveId);
}
