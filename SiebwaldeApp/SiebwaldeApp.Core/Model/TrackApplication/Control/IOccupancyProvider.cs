namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Reports whether a block is currently occupied, and whether that answer is actually known.
    ///
    /// "Unknown" and "clear" are deliberately different: an occupancy source that has not
    /// received valid data yet, or has lost communication, must never be read as "the block is
    /// free". Callers that need a safe answer must check <see cref="IsBlockOccupancyKnown"/>
    /// before treating a false <see cref="IsBlockOccupied"/> as safe.
    ///
    /// Implementations: the real one reads amplifier occupancy; tests use a fake.
    /// </summary>
    public interface IOccupancyProvider
    {
        /// <summary>
        /// True when the block is reported occupied. A false value is only meaningful together
        /// with <see cref="IsBlockOccupancyKnown"/>.
        /// </summary>
        bool IsBlockOccupied(int block);

        /// <summary>
        /// True when the occupancy of the block can actually be confirmed from valid data.
        /// False means unknown: either the block is not mapped, or the underlying source has not
        /// delivered valid data (yet).
        /// </summary>
        bool IsBlockOccupancyKnown(int block);
    }
}
