namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>Outcome of decoding the Koploper raw object graph.</summary>
    public enum KoploperObjectGraphDecodeResult
    {
        /// <summary>The block and locomotive registries were decoded.</summary>
        Success = 0,

        /// <summary>The resolved root pointer was zero.</summary>
        RootInvalid = 1,

        /// <summary>The block list header is inconsistent (for example count exceeds capacity).</summary>
        BlockListInvalid = 2,

        /// <summary>The locomotive list header is inconsistent (for example count exceeds capacity).</summary>
        LocoListInvalid = 3,

        /// <summary>A required memory read failed or a pointer was null/invalid.</summary>
        PointerInvalid = 4,

        /// <summary>Two block objects share the same internal block id.</summary>
        DuplicateBlockId = 5,

        /// <summary>Two locomotive objects share the same internal locomotive id.</summary>
        DuplicateLocoId = 6,

        /// <summary>A list size exceeded its configured plausibility bound.</summary>
        PlausibilityExceeded = 7
    }
}
