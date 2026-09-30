using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Supplies the last known Koploper block position of a locomotive. Moved from the ECoS
    /// emulator into Core so the movement simulator (Core) and the Integration layer can both
    /// depend on it without referencing the ECoS emulator.
    /// </summary>
    public interface IBlockPositionProvider
    {
        /// <summary>
        /// Returns the last known block position of a specific locomotive, or null when unknown.
        /// </summary>
        int? TryGetBlockForLoc(int loc);

        /// <summary>
        /// Raised when Koploper reports that a locomotive entered a new block.
        /// </summary>
        event Action<int, int>? BlockEntered;
    }
}
