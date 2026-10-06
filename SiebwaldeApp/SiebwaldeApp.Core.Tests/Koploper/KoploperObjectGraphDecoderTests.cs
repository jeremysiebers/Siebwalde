using System.Linq;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    public class KoploperObjectGraphDecoderTests
    {
        private static readonly IKoploperMemoryLayout Layout = Koploper94MemoryLayout.Instance;
        private static readonly KoploperDecodePlausibility Plausibility = new();

        // Proven pointer chain (from the live PID 1576 investigation):
        //   moduleBase + 0x3259B0 -> rootCell (0x007287E4) -> root (0x027E981C, heap)
        //   root + 0x5AC -> block TList pointer (0x03001000) -> TList
        //   root + 0x5C8 -> loco  TList pointer (0x03002000) -> TList
        private const uint ModuleBase = 0x00400000;
        private const uint RootCell = 0x007287E4;         // global pointer-cell (first dereference target)
        private const uint RootObject = 0x027E981C;       // active heap root object (second dereference target)
        private const uint BlockListPointer = 0x03001000; // block TList object address (reached via root+0x5AC)
        private const uint LocoListPointer = 0x03002000;  // loco TList object address (reached via root+0x5C8)
        private const uint BlockItemsArray = 0x00600000;
        private const uint LocoItemsArray = 0x00610000;
        private const uint BlockObjectsBase = 0x00700000;
        private const uint LocoObjectsBase = 0x00800000;
        private const uint BlockStride = 0x200;
        private const uint LocoStride = 0x200;

        [Fact]
        public void Decode_ValidMap_SuccessWithAllRawFields()
        {
            // Regression for both indirection layers: the decoder must dereference the root
            // twice (moduleBase+RVA -> rootCell -> root) and then dereference the list-pointer
            // fields (root+offset -> TList pointer -> TList). Any single-layer revert makes this
            // test return PointerInvalid instead of Success.
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

            // ObjectAddress preserves the loco's own TList item pointer (its object address).
            Assert.Equal(LocoObjectsBase, loco2.ObjectAddress);

            KoploperRawLocomotive loco24 = registry.Locomotives[2];
            Assert.Equal(24u, loco24.InternalLocomotiveId);
        }

        [Fact]
        public void Decode_NullRootCell_IsPointerInvalid()
        {
            // [moduleBase + RootPointerRva] holds 0 -> the resolver fails the first dereference,
            // which the decoder surfaces as PointerInvalid (RootInvalid is no longer reachable
            // because the resolver rejects null intermediate pointers).
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), 0u);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out KoploperRawRegistry? registry);

            Assert.Equal(KoploperObjectGraphDecodeResult.PointerInvalid, result);
            Assert.Null(registry);
        }

        [Fact]
        public void Decode_BlockListPointerReadFailure_IsPointerInvalid()
        {
            // Root resolves but [root + RootBlockListOffset] is unmapped -> the block list
            // pointer read fails.
            var reader = new FakeKoploperMemoryReader();
            SetRootOnly(reader);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out KoploperRawRegistry? registry);

            Assert.Equal(KoploperObjectGraphDecodeResult.PointerInvalid, result);
            Assert.Null(registry);
        }

        [Fact]
        public void Decode_NullBlockListPointer_IsPointerInvalid()
        {
            var reader = new FakeKoploperMemoryReader();
            SetRootOnly(reader);
            reader.SetU32((nuint)(RootObject + Layout.RootBlockListOffset), 0u);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out KoploperRawRegistry? registry);

            Assert.Equal(KoploperObjectGraphDecodeResult.PointerInvalid, result);
            Assert.Null(registry);
        }

        [Fact]
        public void Decode_LocoListPointerReadFailure_IsPointerInvalid()
        {
            // Root and block list decode fine, but [root + RootLocoListOffset] is unmapped.
            var reader = new FakeKoploperMemoryReader();
            SetRootOnly(reader);
            reader.SetU32((nuint)(RootObject + Layout.RootBlockListOffset), BlockListPointer);
            WriteBlockListHeader(reader, count: 0, capacity: 0);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out KoploperRawRegistry? registry);

            Assert.Equal(KoploperObjectGraphDecodeResult.PointerInvalid, result);
            Assert.Null(registry);
        }

        [Fact]
        public void Decode_NullLocoListPointer_IsPointerInvalid()
        {
            var reader = new FakeKoploperMemoryReader();
            SetRootOnly(reader);
            reader.SetU32((nuint)(RootObject + Layout.RootBlockListOffset), BlockListPointer);
            WriteBlockListHeader(reader, count: 0, capacity: 0);
            reader.SetU32((nuint)(RootObject + Layout.RootLocoListOffset), 0u);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out KoploperRawRegistry? registry);

            Assert.Equal(KoploperObjectGraphDecodeResult.PointerInvalid, result);
            Assert.Null(registry);
        }

        [Fact]
        public void Decode_UnreadableBlockTList_IsPointerInvalid()
        {
            // The block list pointer field is valid but the TList object it points to is unmapped.
            var reader = new FakeKoploperMemoryReader();
            SetRootAndListPointers(reader);
            // No TList header written at BlockListPointer.
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out KoploperRawRegistry? registry);

            Assert.Equal(KoploperObjectGraphDecodeResult.PointerInvalid, result);
            Assert.Null(registry);
        }

        [Fact]
        public void Decode_BlockCountExceedsCapacity_IsBlockListInvalid()
        {
            var reader = new FakeKoploperMemoryReader();
            SetRootAndListPointers(reader);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCountOffset), 5);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCapacityOffset), 3);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out _);

            Assert.Equal(KoploperObjectGraphDecodeResult.BlockListInvalid, result);
        }

        [Fact]
        public void Decode_BlockCountExceedsPlausibility_IsPlausibilityExceeded()
        {
            var reader = new FakeKoploperMemoryReader();
            SetRootAndListPointers(reader);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCountOffset), Plausibility.MaxBlockCount + 1);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCapacityOffset), Plausibility.MaxBlockCount + 1);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out _);

            Assert.Equal(KoploperObjectGraphDecodeResult.PlausibilityExceeded, result);
        }

        [Fact]
        public void Decode_NullItemArrayWithCount_IsPointerInvalid()
        {
            // TList items pointer is null while count > 0 -> reading the first item slot fails.
            var reader = new FakeKoploperMemoryReader();
            SetRootAndListPointers(reader);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListItemsOffset), 0u);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCountOffset), 1);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCapacityOffset), 1);
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out _);

            Assert.Equal(KoploperObjectGraphDecodeResult.PointerInvalid, result);
        }

        [Fact]
        public void Decode_NullBlockItemPointer_IsPointerInvalid()
        {
            var reader = new FakeKoploperMemoryReader();
            SetRootAndListPointers(reader);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCountOffset), 1);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCapacityOffset), 1);
            reader.SetU32((nuint)BlockItemsArray, 0u); // null item pointer
            var decoder = new KoploperObjectGraphDecoder(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = decoder.Decode(reader, (nuint)ModuleBase, out _);

            Assert.Equal(KoploperObjectGraphDecodeResult.PointerInvalid, result);
        }

        [Fact]
        public void Decode_UnreadableBlockField_IsPointerInvalid()
        {
            var reader = new FakeKoploperMemoryReader();
            SetRootAndListPointers(reader);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCountOffset), 1);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCapacityOffset), 1);

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
            SetRootAndListPointers(reader);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCountOffset), 2);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCapacityOffset), 2);

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
            SetRootAndListPointers(reader);

            // A valid single-block list (must decode before the loco list is walked).
            reader.SetU32((nuint)(BlockListPointer + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCountOffset), 1);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCapacityOffset), 1);
            uint blockAddress = BlockObjectsBase;
            reader.SetU32((nuint)BlockItemsArray, blockAddress);
            WriteBlock(reader, blockAddress, internalId: 1, displayId: 101, owner: 0, state: 0, changed: 0, tick: 0);

            // Two locomotives sharing the same internal ID.
            reader.SetU32((nuint)(LocoListPointer + Layout.TListItemsOffset), LocoItemsArray);
            reader.SetU32((nuint)(LocoListPointer + Layout.TListCountOffset), 2);
            reader.SetU32((nuint)(LocoListPointer + Layout.TListCapacityOffset), 2);
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

        private static void SetRootOnly(FakeKoploperMemoryReader reader)
        {
            // Two-level root chain only (no list-pointer fields): moduleBase + RVA -> rootCell -> root.
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootCell);
            reader.SetU32((nuint)RootCell, RootObject);
        }

        private static void SetRootAndListPointers(FakeKoploperMemoryReader reader)
        {
            SetRootOnly(reader);

            // List-pointer fields: root + offset -> block/loco TList pointer.
            reader.SetU32((nuint)(RootObject + Layout.RootBlockListOffset), BlockListPointer);
            reader.SetU32((nuint)(RootObject + Layout.RootLocoListOffset), LocoListPointer);
        }

        private static void WriteBlockListHeader(FakeKoploperMemoryReader reader, uint count, uint capacity)
        {
            reader.SetU32((nuint)(BlockListPointer + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCountOffset), count);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCapacityOffset), capacity);
        }

        private static FakeKoploperMemoryReader BuildValidMap()
        {
            var reader = new FakeKoploperMemoryReader();

            SetRootAndListPointers(reader);

            // Block TList header: items pointer, count, capacity (offsets +0x04/+0x08/+0x0C).
            reader.SetU32((nuint)(BlockListPointer + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCountOffset), 30);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCapacityOffset), 30);

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
            reader.SetU32((nuint)(LocoListPointer + Layout.TListItemsOffset), LocoItemsArray);
            reader.SetU32((nuint)(LocoListPointer + Layout.TListCountOffset), 3);
            reader.SetU32((nuint)(LocoListPointer + Layout.TListCapacityOffset), 3);

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
