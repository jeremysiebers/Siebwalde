namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Memory-layout profile for Koploper 9.4 build 9 (<c>9.4.0.9</c>). This is the only place
    /// the offsets discovered in the reverse-engineering handoff are declared. The singleton is
    /// immutable and shared.
    /// </summary>
    public sealed class Koploper94MemoryLayout : IKoploperMemoryLayout
    {
        /// <summary>Shared immutable instance of the Koploper 9.4 layout profile.</summary>
        public static Koploper94MemoryLayout Instance { get; } = new();

        private Koploper94MemoryLayout()
        {
        }

        /// <inheritdoc />
        public string SupportedSha256Hex => "645B4681C14975F3619EB44968C74F3B7925CB914C5A23302932918D73079D2E";

        /// <inheritdoc />
        public string Version => "9.4.0.9";

        /// <inheritdoc />
        public uint PreferredImageBase => 0x00400000u;

        /// <inheritdoc />
        public uint RootPointerRva => 0x3259B0u;

        /// <inheritdoc />
        public uint RootBlockListOffset => 0x5ACu;

        /// <inheritdoc />
        public uint RootLocoListOffset => 0x5C8u;

        /// <inheritdoc />
        public uint TListItemsOffset => 0x04u;

        /// <inheritdoc />
        public uint TListCountOffset => 0x08u;

        /// <inheritdoc />
        public uint TListCapacityOffset => 0x0Cu;

        /// <inheritdoc />
        public uint BlockInternalIdOffset => 0x14Cu;

        /// <inheritdoc />
        public uint BlockDisplayIdOffset => 0x15Cu;

        /// <inheritdoc />
        public uint BlockOwnerOffset => 0x1ACu;

        /// <inheritdoc />
        public uint BlockStateOffset => 0x1EDu;

        /// <inheritdoc />
        public uint BlockChangedFlagOffset => 0x1EEu;

        /// <inheritdoc />
        public uint BlockUpdateTickOffset => 0x1F0u;

        /// <inheritdoc />
        public uint LocoInternalIdOffset => 0x1A8u;

        /// <inheritdoc />
        public uint LocoBlockRef54Offset => 0x54u;

        /// <inheritdoc />
        public uint LocoBlockRef58Offset => 0x58u;
    }
}
