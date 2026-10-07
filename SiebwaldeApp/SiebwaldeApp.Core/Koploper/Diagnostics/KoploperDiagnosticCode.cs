namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Diagnostic codes for the Koploper read-only internal-state source. Each code names one
    /// observed condition; the list matches the diagnostics recommended in the technical
    /// handoff and is deliberately free of UI wording.
    /// </summary>
    public enum KoploperDiagnosticCode
    {
        /// <summary>The Koploper process could not be found.</summary>
        KOPLOPER_PROCESS_NOT_FOUND = 0,

        /// <summary>The executable did not match the supported binary hash/version.</summary>
        KOPLOPER_UNSUPPORTED_BINARY = 1,

        /// <summary>The target process exists but could not be accessed.</summary>
        KOPLOPER_ACCESS_DENIED = 2,

        /// <summary>The resolved root pointer was invalid (for example zero).</summary>
        KOPLOPER_ROOT_INVALID = 3,

        /// <summary>A list header was inconsistent (for example count exceeds capacity).</summary>
        KOPLOPER_LIST_INVALID = 4,

        /// <summary>A required memory read failed or a pointer was invalid.</summary>
        KOPLOPER_POINTER_INVALID = 5,

        /// <summary>A snapshot changed while it was being read and is not coherent.</summary>
        KOPLOPER_SNAPSHOT_INCONSISTENT = 6,

        /// <summary>A block raw state byte was not a known state value.</summary>
        KOPLOPER_UNKNOWN_BLOCK_STATE = 7,

        /// <summary>A block owner pointer could not be resolved to a known locomotive.</summary>
        KOPLOPER_OWNER_NOT_FOUND = 8,

        /// <summary>A snapshot exceeded the configured maximum age.</summary>
        KOPLOPER_STALE = 9,

        /// <summary>The Koploper process restarted; all previous state was invalidated.</summary>
        KOPLOPER_RESTARTED = 10,

        /// <summary>A Koploper block has no mapping to a Siebwalde section/amplifier.</summary>
        KOPLOPER_BLOCK_MAPPING_MISSING = 11,

        /// <summary>A Koploper block maps to more than one conflicting Siebwalde section/amplifier.</summary>
        KOPLOPER_BLOCK_MAPPING_CONFLICT = 12,

        /// <summary>
        /// A block has an owner pointer in a state (0 or 9) where the validated profile expects
        /// none; the block is not published as a valid Free/Reserved/Occupied state.
        /// </summary>
        KOPLOPER_STATE_OWNER_INCONSISTENT = 13,

        /// <summary>A locomotive owns more than one occupied block in the same snapshot.</summary>
        KOPLOPER_MULTIPLE_OCCUPIED_BLOCKS = 14
    }
}
