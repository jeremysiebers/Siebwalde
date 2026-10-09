using System;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Shared deterministic fixtures for the KIS-04 snapshot-reader tests: the proven pointer
    /// chain constants, a minimal valid memory map (two free blocks + one locomotive), and the
    /// wiring helpers for a <see cref="KoploperSnapshotReader"/>.
    /// </summary>
    internal static class KoploperSnapshotTestFixtures
    {
        public static readonly IKoploperMemoryLayout Layout = Koploper94MemoryLayout.Instance;

        // Proven pointer chain (from the live PID 1576 investigation), reused as the layout for
        // the in-memory reader. Values are arbitrary but internally consistent.
        public const uint ModuleBase = 0x00400000;
        public const uint RootCell = 0x007287E4;
        public const uint RootObject = 0x027E981C;
        public const uint BlockListPointer = 0x03001000;
        public const uint LocoListPointer = 0x03002000;
        public const uint BlockItemsArray = 0x00600000;
        public const uint LocoItemsArray = 0x00610000;
        public const uint BlockObjectsBase = 0x00700000;
        public const uint LocoObjectsBase = 0x00800000;
        public const uint BlockStride = 0x200;
        public const uint LocoStride = 0x200;

        /// <summary>The root-pointer cell, read exactly once per graph walk.</summary>
        public static nuint WatchAddress => (nuint)(ModuleBase + Layout.RootPointerRva);

        public static KoploperProcessInfo Info(int processId, long utcTicks)
            => new(
                processId,
                new DateTimeOffset(utcTicks, TimeSpan.Zero),
                (nuint)ModuleBase,
                @"C:\Koploper\koploper.exe",
                "koploper.exe");

        public static KoploperExecutableIdentity Identity()
            => new("Koploper", Layout.Version, Layout.SupportedSha256Hex, Layout.PreferredImageBase);

        /// <summary>Writes a minimal valid map: two free blocks and one locomotive.</summary>
        public static void BuildValidMap(FakeKoploperMemoryReader reader)
        {
            // Root chain (double dereference).
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootCell);
            reader.SetU32((nuint)RootCell, RootObject);

            // List-pointer fields.
            reader.SetU32((nuint)(RootObject + Layout.RootBlockListOffset), BlockListPointer);
            reader.SetU32((nuint)(RootObject + Layout.RootLocoListOffset), LocoListPointer);

            // Block list: two free blocks.
            reader.SetU32((nuint)(BlockListPointer + Layout.TListItemsOffset), BlockItemsArray);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCountOffset), 2);
            reader.SetU32((nuint)(BlockListPointer + Layout.TListCapacityOffset), 2);
            for (uint i = 0; i < 2; i++)
            {
                uint blockAddress = BlockObjectsBase + (i * BlockStride);
                reader.SetU32((nuint)(BlockItemsArray + (i * 4)), blockAddress);
                WriteBlock(reader, blockAddress, i + 1, 100 + i + 1, owner: 0, state: 0, changed: 0, tick: 0);
            }

            // Locomotive list: one locomotive (internal id 24).
            reader.SetU32((nuint)(LocoListPointer + Layout.TListItemsOffset), LocoItemsArray);
            reader.SetU32((nuint)(LocoListPointer + Layout.TListCountOffset), 1);
            reader.SetU32((nuint)(LocoListPointer + Layout.TListCapacityOffset), 1);
            uint locoAddress = LocoObjectsBase;
            reader.SetU32((nuint)LocoItemsArray, locoAddress);
            WriteLoco(reader, locoAddress, internalId: 24, ref54: 0x54, ref58: 0x58);
        }

        public static void WriteBlock(
            FakeKoploperMemoryReader reader,
            uint address,
            uint internalId,
            uint displayId,
            uint owner,
            byte state,
            byte changed,
            uint tick,
            byte manualBlocked = 0)
        {
            reader.SetU32((nuint)(address + Layout.BlockInternalIdOffset), internalId);
            reader.SetU32((nuint)(address + Layout.BlockDisplayIdOffset), displayId);
            reader.SetU32((nuint)(address + Layout.BlockOwnerOffset), owner);
            reader.SetBytes((nuint)(address + Layout.BlockStateOffset), new[] { state });
            reader.SetBytes((nuint)(address + Layout.BlockChangedFlagOffset), new[] { changed });
            reader.SetU32((nuint)(address + Layout.BlockUpdateTickOffset), tick);
            reader.SetBytes((nuint)(address + Layout.BlockManualBlockedOffset), new[] { manualBlocked });
        }

        public static void WriteLoco(FakeKoploperMemoryReader reader, uint address, uint internalId, uint ref54, uint ref58)
        {
            reader.SetU32((nuint)(address + Layout.LocoInternalIdOffset), internalId);
            reader.SetU32((nuint)(address + Layout.LocoBlockRef54Offset), ref54);
            reader.SetU32((nuint)(address + Layout.LocoBlockRef58Offset), ref58);
        }

        public static KoploperSnapshotReader CreateReader(
            IKoploperProcessLocator locator,
            Func<IKoploperMemoryReader> memoryFactory,
            KoploperSnapshotReadOptions? options = null,
            Func<DateTimeOffset>? clock = null,
            KoploperVersionGateResult gate = KoploperVersionGateResult.Supported)
        {
            KoploperExecutableIdentity? identity = gate == KoploperVersionGateResult.Supported ? Identity() : null;
            var verifier = new FakeKoploperExecutableVerifier(gate, identity);
            var rawGraphReader = new KoploperRawObjectGraphReader(Layout, new KoploperDecodePlausibility());
            var blockStateDecoder = new KoploperBlockStateDecoder();
            return new KoploperSnapshotReader(locator, verifier, memoryFactory, rawGraphReader, blockStateDecoder, options, clock);
        }
    }
}
