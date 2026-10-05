namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A versioned Koploper memory-layout profile: the executable identity a layout is valid
    /// for, plus every offset/RVA the decoder needs. Offsets are the single home for raw layout
    /// knowledge; business code must not scatter offsets.
    /// </summary>
    public interface IKoploperMemoryLayout
    {
        /// <summary>Expected SHA-256 of the Koploper executable this layout supports.</summary>
        string SupportedSha256Hex { get; }

        /// <summary>Koploper version this layout targets.</summary>
        string Version { get; }

        /// <summary>Preferred image base of the PE (used for identity checks, not runtime math).</summary>
        uint PreferredImageBase { get; }

        /// <summary>RVA of the central root pointer location.</summary>
        uint RootPointerRva { get; }

        /// <summary>Offset of the block TList within the root object.</summary>
        uint RootBlockListOffset { get; }

        /// <summary>Offset of the locomotive TList within the root object.</summary>
        uint RootLocoListOffset { get; }

        /// <summary>Offset of the item-pointer array within a TList.</summary>
        uint TListItemsOffset { get; }

        /// <summary>Offset of the count field within a TList.</summary>
        uint TListCountOffset { get; }

        /// <summary>Offset of the capacity field within a TList.</summary>
        uint TListCapacityOffset { get; }

        /// <summary>Offset of the internal block id within a TBlok object.</summary>
        uint BlockInternalIdOffset { get; }

        /// <summary>Offset of the display block number candidate within a TBlok object.</summary>
        uint BlockDisplayIdOffset { get; }

        /// <summary>Offset of the owner locomotive pointer within a TBlok object.</summary>
        uint BlockOwnerOffset { get; }

        /// <summary>Offset of the raw state byte within a TBlok object.</summary>
        uint BlockStateOffset { get; }

        /// <summary>Offset of the status-change flag within a TBlok object.</summary>
        uint BlockChangedFlagOffset { get; }

        /// <summary>Offset of the update tick within a TBlok object.</summary>
        uint BlockUpdateTickOffset { get; }

        /// <summary>Offset of the internal locomotive id within a locomotive object.</summary>
        uint LocoInternalIdOffset { get; }

        /// <summary>Offset of the block reference at +0x54 (unverified semantic) within a locomotive object.</summary>
        uint LocoBlockRef54Offset { get; }

        /// <summary>Offset of the block reference at +0x58 (strong current-block candidate) within a locomotive object.</summary>
        uint LocoBlockRef58Offset { get; }
    }
}
