namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Typed block state derived from the raw <c>TBlok</c> state byte. Only the values in the
    /// validated runtime-state model (<see href="docs/koploper-internal-state-integration.md"/>
    /// §6.4 and §10) are represented; <see cref="Unknown"/> is the fail-closed value for any raw
    /// byte that could not be published as Free/Reserved/Occupied/Transition.
    /// </summary>
    public enum KoploperBlockState
    {
        /// <summary>No validated state could be derived for this raw value.</summary>
        Unknown = -1,

        /// <summary>The block is free (raw state 0 with no owner).</summary>
        Free = 0,

        /// <summary>The block is reserved for an owner locomotive (raw state 1).</summary>
        Reserved = 1,

        /// <summary>The block is administratively occupied by an owner locomotive (raw state 2).</summary>
        Occupied = 2,

        /// <summary>The block is in a transition/release/reset state (raw state 9); never Free.</summary>
        Transition = 9
    }
}
