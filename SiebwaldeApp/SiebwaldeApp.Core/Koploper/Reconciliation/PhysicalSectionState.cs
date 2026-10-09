using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The physical occupancy of one logical track section, projected from its physical track
    /// amplifier (ModBus slave) readback. Occupancy is a bare tri-state with no locomotive
    /// identity: a physical detector cannot know which locomotive (if any) is present.
    /// </summary>
    public sealed record PhysicalSectionState(
        int LogicalSectionId,
        PhysicalOccupancy Occupancy,
        DateTimeOffset ObservedAtUtc,
        TimeSpan Age,
        bool IsFresh,
        PhysicalSourceHealth SourceHealth);
}
