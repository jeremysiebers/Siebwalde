using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    public class Koploper94MemoryLayoutTests
    {
        [Fact]
        public void OffsetsMatchReverseEngineeringHandoff()
        {
            IKoploperMemoryLayout layout = Koploper94MemoryLayout.Instance;

            Assert.Equal(0x3259B0u, layout.RootPointerRva);
            Assert.Equal(0x5ACu, layout.RootBlockListOffset);
            Assert.Equal(0x5C8u, layout.RootLocoListOffset);
            Assert.Equal(0x04u, layout.TListItemsOffset);
            Assert.Equal(0x08u, layout.TListCountOffset);
            Assert.Equal(0x0Cu, layout.TListCapacityOffset);
            Assert.Equal(0x14Cu, layout.BlockInternalIdOffset);
            Assert.Equal(0x15Cu, layout.BlockDisplayIdOffset);
            Assert.Equal(0x1ACu, layout.BlockOwnerOffset);
            Assert.Equal(0x1EDu, layout.BlockStateOffset);
            Assert.Equal(0x1EEu, layout.BlockChangedFlagOffset);
            Assert.Equal(0x1F0u, layout.BlockUpdateTickOffset);
            Assert.Equal(0x1A8u, layout.LocoInternalIdOffset);
            Assert.Equal(0x54u, layout.LocoBlockRef54Offset);
            Assert.Equal(0x58u, layout.LocoBlockRef58Offset);
        }

        [Fact]
        public void SupportedSha256HexMatchesHandoff()
        {
            Assert.Equal(
                "645B4681C14975F3619EB44968C74F3B7925CB914C5A23302932918D73079D2E",
                Koploper94MemoryLayout.Instance.SupportedSha256Hex);
        }

        [Fact]
        public void VersionAndImageBaseMatchHandoff()
        {
            IKoploperMemoryLayout layout = Koploper94MemoryLayout.Instance;

            Assert.Equal("9.4.0.9", layout.Version);
            Assert.Equal(0x00400000u, layout.PreferredImageBase);
        }
    }
}
