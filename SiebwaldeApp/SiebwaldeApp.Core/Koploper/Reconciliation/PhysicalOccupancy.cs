namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Tri-state physical occupancy of a logical track section, derived from a physical track
    /// amplifier (ModBus slave) readback. It carries only the occupancy; it never carries a
    /// locomotive identity, because a physical occupancy detector cannot know which locomotive
    /// (if any) is present.
    /// </summary>
    public enum PhysicalOccupancy
    {
        /// <summary>The physical state is not known (no fresh observation is available).</summary>
        Unknown = -1,

        /// <summary>The section is observed clear (no train detected).</summary>
        Clear = 0,

        /// <summary>The section is observed occupied (a train is detected).</summary>
        Occupied = 1
    }
}
