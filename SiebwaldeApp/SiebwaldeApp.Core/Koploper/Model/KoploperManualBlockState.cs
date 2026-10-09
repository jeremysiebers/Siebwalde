namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Typed manual-blocked state derived from the raw <c>TBlok</c> manual-blocked byte at +0x198.
    /// This dimension is independent of the automatic block state
    /// (<see cref="KoploperBlockState"/>): a block can be manually blocked regardless of whether it
    /// is free, reserved, occupied or in transition. <see cref="Invalid"/> is the fail-closed value
    /// for any raw byte that could not be published as NotBlocked/Blocked.
    /// </summary>
    public enum KoploperManualBlockState
    {
        /// <summary>No validated manual-blocked state could be derived for this raw value.</summary>
        Invalid = -1,

        /// <summary>The block is not manually blocked (raw byte 0).</summary>
        NotBlocked = 0,

        /// <summary>The block is manually blocked (raw byte 1).</summary>
        Blocked = 1
    }
}
