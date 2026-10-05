using System.Linq;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    public class KoploperObjectGraphDecoderTests
    {
        private static readonly IKoploperMemoryLayout Layout = Koploper94MemoryLayout.Instance;
        private static readonly KoploperDecodePlausibility Plausibility = new();

        private const uint ModuleBase = 0x00400000;
        private const uint RootObject = 0x00500000;
        private const uint BlockListAddress = RootObject + 0x5AC;
        private const uint LocoListAddress = RootObject + 0x5C8;
        private const uint BlockItemsArray = 0x00600000;
        private const uint LocoItemsArray = 0x00610000;
        private const uint BlockObjectsBase = 0x00700000;
        private const uint LocoObjectsBase = 0x00800000;
        private const uint BlockStride = 0x200;
        private const uint LocoStride = 0x200;

        [Fact]
        public void Decode_ValidMap_SuccessWithAllRawFields()
        {
            using var reader = BuildValidMap();
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out KoploperRawRegistry? registry);

            Assert.Equal(KoploperObjectGraphDecodeResult.Success, result);
            Assert.NotNull(registry);
            Assert.Equal(30, registry.Blocks.Count);
            Assert.Equal(3, registry.Locomotives.Count);

            // Locomotive internal IDs are the demo config's 2 / 8 / 24.
            Assert.Equal(new uint[] { 2, 8, 24 }, registry.Locomotives.Select(l => l.InternalLocomotiveId).ToArray());

            // First block raw fields.
            KoploperRawBlock first = registry.Blocks[0];
            Assert.Equal(1u, first.InternalBlockId);
            Assert.Equal(101u, first.DisplayBlockNumber);
            Assert.Equal(0xA000u, first.OwnerPointer);
            Assert.Equal((byte)0x01, first.RawState);
            Assert.Equal((byte)0x00, first.ChangedFlag);
            Assert.Equal(0x1000u, first.UpdateTick);

            // Last block raw fields.
            KoploperRawBlock last = registry.Blocks[29];
            Assert.Equal(30u, last.InternalBlockId);
            Assert.Equal(130u, last.DisplayBlockNumber);
            Assert.Equal(0xA000u + 29u, last.OwnerPointer);
            Assert.Equal((byte)0x01, last.RawState);
            Assert.Equal(0x1000u + 29u, last.UpdateTick);

            // A couple of locomotive raw fields.
            KoploperRawLocomotive loco2 = registry.Locomotives[0];
            Assert.Equal(2u, loco2.InternalLocomotiveId);
            Assert.Equal(0x54u, loco2.BlockRef54);
            Assert.Equal(0x58u, loco2.BlockRef58);

            KoploperRawLocomotive loco24 = registry.Locomotives[2];
            Assert.Equal(24u, loco24.InternalLocomotiveId);
        }

        [Fact]
        public void Decode_ZeroRoot_IsRootInvalid()
        {
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), 0u);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out KoploperRawRegistry? registry);

            Assert.Equal(KoploperObjectGraphDecodeResult.RootInvalid, result);
            Assert.Null(registry);
        }

        [Fact]
        public void Decode_BlockCountExceedsCapacity_IsBlockListInvalid()
        {
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootObject);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCountOffset), 5);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCapacityOffset), 3);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out _);

            Assert.Equal(KoploperObjectGraphDecodeResult.BlockListInvalid, result);
        }

        [Fact]
        public void Decode_BlockCountExceedsPlausibility_IsPlausibilityExceeded()
        {
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootObject);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCountOffset), Plausibility.MaxBlockCount + 1);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCapacityOffset), Plausibility.MaxBlockCount + 1);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out _);

            Assert.Equal(KoploperObjectGraphDecodeResult.PlausibilityExceeded, result);
        }

        [Fact]
        public void Decode_NullBlockItemPointer_IsPointerInvalid()
        {
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootObject);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCountOffset), 1);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCapacityOffset), 1);
            reader.SetU32((nuint)BlockItemsArray, 0u); // null item pointer
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out _);

            Assert.Equal(KoploperObjectGraphDecodeResult.PointerInvalid, result);
        }

        [Fact]
        public void Decode_UnreadableBlockField_IsPointerInvalid()
        {
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootObject);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCountOffset), 1);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCapacityOffset), 1);

            uint blockAddress = BlockObjectsBase;
            reader.SetU32((nuint)BlockItemsArray, blockAddress);
            // Only the internal id is mapped; the display id field is left unmapped.
            reader.SetU32((nuint)(blockAddress + Layout.BlockInternalIdOffset), 1u);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out _);

            Assert.Equal(KoploperObjectGraphDecodeResult.PointerInvalid, result);
        }

        [Fact]
        public void Decode_DuplicateBlockId_IsDuplicateBlockId()
        {
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootObject);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCountOffset), 2);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCapacityOffset), 2);

            for (uint i = 0; i < 2; i++)
            {
                uint blockAddress = BlockObjectsBase + (i * BlockStride);
                reader.SetU32((nuint)(BlockItemsArray + (i * 4)), blockAddress);
                WriteBlock(reader, blockAddress, internalId: 7, displayId: 100 + i, owner: 0, state: 0, changed: 0, tick: 0);
            }

            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);
            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out _);

            Assert.Equal(KoploperObjectGraphDecodeResult.DuplicateBlockId, result);
        }

        [Fact]
        public void Decode_DuplicateLocoId_IsDuplicateLocoId()
        {
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootObject);

            // A valid single-block list (must decode before the loco list is walked).
            reader.SetU32((nuint)(BlockListAddress + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCountOffset), 1);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCapacityOffset), 1);
            uint blockAddress = BlockObjectsBase;
            reader.SetU32((nuint)BlockItemsArray, blockAddress);
            WriteBlock(reader, blockAddress, internalId: 1, displayId: 101, owner: 0, state: 0, changed: 0, tick: 0);

            // Two locomotives sharing the same internal ID.
            reader.SetU32((nuint)(LocoListAddress + Layout.TListItemsOffset), LocoItemsArray);
            reader.SetU32((nuint)(LocoListAddress + Layout.TListCountOffset), 2);
            reader.SetU32((nuint)(LocoListAddress + Layout.TListCapacityOffset), 2);
            for (uint i = 0; i < 2; i++)
            {
                uint locoAddress = LocoObjectsBase + (i * LocoStride);
                reader.SetU32((nuint)(LocoItemsArray + (i * 4)), locoAddress);
                WriteLoco(reader, locoAddress, internalId: 24, ref54: 0, ref58: 0);
            }

            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);
            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out _);

            Assert.Equal(KoploperObjectGraphDecodeResult.DuplicateLocoId, result);
        }

        private static FakeKoploperMemoryReader BuildValidMap()
        {
            var reader = new FakeKoploperMemoryReader();

            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootObject);

            // Block TList header: items pointer, count, capacity (offsets +0x04/+0x08/+0x0C).
            reader.SetU32((nuint)(BlockListAddress + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCountOffset), 30);
            reader.SetU32((nuint)(BlockListAddress + Layout.TListCapacityOffset), 30);

            for (uint i = 0; i < 30; i++)
            {
                uint blockAddress = BlockObjectsBase + (i * BlockStride);
                reader.SetU32((nuint)(BlockItemsArray + (i * 4)), blockAddress);
                WriteBlock(
                    reader,
                    blockAddress,
                    internalId: i + 1,
                    displayId: 100 + i + 1,
                    owner: 0xA000 + i,
                    state: 0x01,
                    changed: 0x00,
                    tick: 0x1000 + i);
            }

            // Locomotive TList header.
            reader.SetU32((nuint)(LocoListAddress + Layout.TListItemsOffset), LocoItemsArray);
            reader.SetU32((nuint)(LocoListAddress + Layout.TListCountOffset), 3);
            reader.SetU32((nuint)(LocoListAddress + Layout.TListCapacityOffset), 3);

            uint[] locoIds = { 2, 8, 24 };
            for (uint i = 0; i < locoIds.Length; i++)
            {
                uint locoAddress = LocoObjectsBase + (i * LocoStride);
                reader.SetU32((nuint)(LocoItemsArray + (i * 4)), locoAddress);
                WriteLoco(reader, locoAddress, internalId: locoIds[i], ref54: 0x54 + i, ref58: 0x58 + i);
            }

            return reader;
        }

        private static void WriteBlock(
            FakeKoploperMemoryReader reader,
            uint address,
            uint internalId,
            uint displayId,
            uint owner,
            byte state,
            byte changed,
            uint tick)
        {
            reader.SetU32((nuint)(address + Layout.BlockInternalIdOffset), internalId);
            reader.SetU32((nuint)(address + Layout.BlockDisplayIdOffset), displayId);
            reader.SetU32((nuint)(address + Layout.BlockOwnerOffset), owner);
            reader.SetBytes((nuint)(address + Layout.BlockStateOffset), new[] { state });
            reader.SetBytes((nuint)(address + Layout.BlockChangedFlagOffset), new[] { changed });
            reader.SetU32((nuint)(address + Layout.BlockUpdateTickOffset), tick);
        }

        private static void WriteLoco(
            FakeKoploperMemoryReader reader,
            uint address,
            uint internalId,
            uint ref54,
            uint ref58)
        {
            reader.SetU32((nuint)(address + Layout.LocoInternalIdOffset), internalId);
            reader.SetU32((nuint)(address + Layout.LocoBlockRef54Offset), ref54);
            reader.SetU32((nuint)(address + Layout.LocoBlockRef58Offset), ref58);
        }
    }
}
