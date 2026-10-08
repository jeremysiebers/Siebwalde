using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for the raw object-graph reader's handling of the manual-blocked byte at +0x198:
    /// it must be read in the per-block chain, and a missing read must fail closed as PointerInvalid.
    /// </summary>
    public class KoploperManualBlockRawReadTests
    {
        private static readonly IKoploperMemoryLayout Layout = Koploper94MemoryLayout.Instance;
        private static readonly nuint ModuleBase = (nuint)KoploperSnapshotTestFixtures.ModuleBase;

        [Fact]
        public void Read_ValidMap_ReadsManualBlockedByte()
        {
            var reader = new FakeKoploperMemoryReader();
            KoploperSnapshotTestFixtures.BuildValidMap(reader);
            reader.SetBytes(
                (nuint)(KoploperSnapshotTestFixtures.BlockObjectsBase + Layout.BlockManualBlockedOffset),
                new byte[] { 1 });

            var graphReader = new KoploperRawObjectGraphReader(Layout, new KoploperDecodePlausibility());
            KoploperObjectGraphDecodeResult result = graphReader.Read(reader, ModuleBase, out KoploperRawObjectGraphObservation? observation);

            Assert.Equal(KoploperObjectGraphDecodeResult.Success, result);
            Assert.NotNull(observation);
            Assert.Equal((byte)1, observation.Registry.Blocks[0].ManualBlockedRaw);
        }

        [Fact]
        public void Read_MissingManualBlockedByte_IsPointerInvalid()
        {
            var reader = BuildMapWithoutManualBlockedByte();
            var graphReader = new KoploperRawObjectGraphReader(Layout, new KoploperDecodePlausibility());

            KoploperObjectGraphDecodeResult result = graphReader.Read(reader, ModuleBase, out KoploperRawObjectGraphObservation? observation);

            Assert.Equal(KoploperObjectGraphDecodeResult.PointerInvalid, result);
            Assert.Null(observation);
        }

        private static FakeKoploperMemoryReader BuildMapWithoutManualBlockedByte()
        {
            var reader = new FakeKoploperMemoryReader();

            // Root chain + list pointers (from the shared fixtures).
            reader.SetU32((nuint)(KoploperSnapshotTestFixtures.ModuleBase + Layout.RootPointerRva), KoploperSnapshotTestFixtures.RootCell);
            reader.SetU32((nuint)KoploperSnapshotTestFixtures.RootCell, KoploperSnapshotTestFixtures.RootObject);
            reader.SetU32((nuint)(KoploperSnapshotTestFixtures.RootObject + Layout.RootBlockListOffset), KoploperSnapshotTestFixtures.BlockListPointer);
            reader.SetU32((nuint)(KoploperSnapshotTestFixtures.RootObject + Layout.RootLocoListOffset), KoploperSnapshotTestFixtures.LocoListPointer);

            // Block list: one block, all fields written EXCEPT the manual-blocked byte at +0x198.
            reader.SetU32((nuint)(KoploperSnapshotTestFixtures.BlockListPointer + Layout.TListItemsOffset), KoploperSnapshotTestFixtures.BlockItemsArray);
            reader.SetU32((nuint)(KoploperSnapshotTestFixtures.BlockListPointer + Layout.TListCountOffset), 1);
            reader.SetU32((nuint)(KoploperSnapshotTestFixtures.BlockListPointer + Layout.TListCapacityOffset), 1);
            uint blockAddress = KoploperSnapshotTestFixtures.BlockObjectsBase;
            reader.SetU32((nuint)KoploperSnapshotTestFixtures.BlockItemsArray, blockAddress);
            reader.SetU32((nuint)(blockAddress + Layout.BlockInternalIdOffset), 1);
            reader.SetU32((nuint)(blockAddress + Layout.BlockDisplayIdOffset), 101);
            reader.SetU32((nuint)(blockAddress + Layout.BlockOwnerOffset), 0);
            reader.SetBytes((nuint)(blockAddress + Layout.BlockStateOffset), new byte[] { 0 });
            reader.SetBytes((nuint)(blockAddress + Layout.BlockChangedFlagOffset), new byte[] { 0 });
            reader.SetU32((nuint)(blockAddress + Layout.BlockUpdateTickOffset), 0);

            // Loco list: empty.
            reader.SetU32((nuint)(KoploperSnapshotTestFixtures.LocoListPointer + Layout.TListItemsOffset), KoploperSnapshotTestFixtures.LocoItemsArray);
            reader.SetU32((nuint)(KoploperSnapshotTestFixtures.LocoListPointer + Layout.TListCountOffset), 0);
            reader.SetU32((nuint)(KoploperSnapshotTestFixtures.LocoListPointer + Layout.TListCapacityOffset), 0);

            return reader;
        }
    }
}
