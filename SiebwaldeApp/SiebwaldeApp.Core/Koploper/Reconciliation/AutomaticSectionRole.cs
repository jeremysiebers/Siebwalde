namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The automatic (locomotive-derived) role a Koploper logical section plays in the logical
    /// shadow. It is inverted from the shadow's per-locomotive occupied/reserved projection; it
    /// carries no physical meaning and never assigns a locomotive identity to a physical reading.
    /// The numeric ordering is stable and not a severity ranking.
    /// </summary>
    public enum AutomaticSectionRole
    {
        /// <summary>The section has no automatic ownership (free from the logical view).</summary>
        None = 0,

        /// <summary>The section is reserved for a locomotive.</summary>
        Reserved = 1,

        /// <summary>The section is occupied by a locomotive.</summary>
        Occupied = 2
    }
}
