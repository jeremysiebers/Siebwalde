using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Reports the fresh occupancy of a single amplifier address (the section it drives) as a
    /// tri-state <see cref="PhysicalOccupancy"/>: <see cref="PhysicalOccupancy.Occupied"/>,
    /// <see cref="PhysicalOccupancy.Clear"/>, or <see cref="PhysicalOccupancy.Unknown"/> when the
    /// address is unmapped, stale, or not a track amplifier.
    ///
    /// This is the per-section source the occupancy bridge needs so each Koploper bezetmelder
    /// reflects the occupancy of ITS OWN section (KIS10-06). It is distinct from the block-level
    /// <see cref="IOccupancyProvider"/> that look-ahead/safety consumers still use.
    /// </summary>
    public interface IAmplifierOccupancyProvider
    {
        /// <summary>
        /// The fresh occupancy of the amplifier. <see cref="PhysicalOccupancy.Unknown"/> is
        /// deliberately not <see cref="PhysicalOccupancy.Clear"/>: a caller must never translate an
        /// unknown reading into "free".
        /// </summary>
        PhysicalOccupancy GetAmplifierOccupancy(int amplifierAddress);
    }
}
