using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Decodes the Koploper raw object graph against an injected memory layout, using the
    /// configured plausibility bounds. Offsets come exclusively from the layout profile.
    /// </summary>
    public sealed class KoploperObjectGraphDecoder : IKoploperObjectGraphDecoder
    {
        private readonly IKoploperMemoryLayout _layout;
        private readonly KoploperDecodePlausibility _plausibility;

        public KoploperObjectGraphDecoder(IKoploperMemoryLayout layout, KoploperDecodePlausibility plausibility)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _plausibility = plausibility ?? throw new ArgumentNullException(nameof(plausibility));
        }

        /// <inheritdoc />
        public KoploperObjectGraphDecodeResult Decode(
            IKoploperMemoryReader reader,
            nuint moduleBase,
            out KoploperRawRegistry? registry)
        {
            registry = null;
            ArgumentNullException.ThrowIfNull(reader);

            // Resolve the central root pointer via the resolver component.
            var rootResolver = new Koploper94RootResolver(reader, _layout);
            if (!rootResolver.TryResolveRoot(moduleBase, out nuint root))
            {
                return KoploperObjectGraphDecodeResult.PointerInvalid;
            }
            if (root == 0)
            {
                return KoploperObjectGraphDecodeResult.RootInvalid;
            }

            // Block registry: the root field holds a pointer to the block TList (not the TList
            // inline); dereference it before reading the list.
            nuint blockListPointerAddress = checked(root + _layout.RootBlockListOffset);
            if (!reader.TryReadPointer32(blockListPointerAddress, out uint blockListPointer))
            {
                return KoploperObjectGraphDecodeResult.PointerInvalid;
            }
            if (blockListPointer == 0)
            {
                return KoploperObjectGraphDecodeResult.PointerInvalid;
            }
            if (!TryReadList(reader, blockListPointer, out KoploperTList blockList))
            {
                return KoploperObjectGraphDecodeResult.PointerInvalid;
            }
            if (blockList.Count > blockList.Capacity)
            {
                return KoploperObjectGraphDecodeResult.BlockListInvalid;
            }
            if (blockList.Count > _plausibility.MaxBlockCount)
            {
                return KoploperObjectGraphDecodeResult.PlausibilityExceeded;
            }

            var blocks = new List<KoploperRawBlock>(checked((int)blockList.Count));
            var blockIds = new HashSet<uint>();
            for (uint i = 0; i < blockList.Count; i++)
            {
                if (!TryReadItemPointer(reader, blockList.ItemsArrayAddress, i, out nuint blockAddress))
                {
                    return KoploperObjectGraphDecodeResult.PointerInvalid;
                }

                if (!reader.TryReadUInt32(checked(blockAddress + _layout.BlockInternalIdOffset), out uint internalId) ||
                    !reader.TryReadUInt32(checked(blockAddress + _layout.BlockDisplayIdOffset), out uint displayId) ||
                    !reader.TryReadPointer32(checked(blockAddress + _layout.BlockOwnerOffset), out uint ownerPointer) ||
                    !reader.TryReadByte(checked(blockAddress + _layout.BlockStateOffset), out byte rawState) ||
                    !reader.TryReadByte(checked(blockAddress + _layout.BlockChangedFlagOffset), out byte changedFlag) ||
                    !reader.TryReadUInt32(checked(blockAddress + _layout.BlockUpdateTickOffset), out uint updateTick))
                {
                    return KoploperObjectGraphDecodeResult.PointerInvalid;
                }

                if (!blockIds.Add(internalId))
                {
                    return KoploperObjectGraphDecodeResult.DuplicateBlockId;
                }

                blocks.Add(new KoploperRawBlock(internalId, displayId, ownerPointer, rawState, changedFlag, updateTick));
            }

            // Locomotive registry: the root field holds a pointer to the loco TList (not the
            // TList inline); dereference it before reading the list.
            nuint locoListPointerAddress = checked(root + _layout.RootLocoListOffset);
            if (!reader.TryReadPointer32(locoListPointerAddress, out uint locoListPointer))
            {
                return KoploperObjectGraphDecodeResult.PointerInvalid;
            }
            if (locoListPointer == 0)
            {
                return KoploperObjectGraphDecodeResult.PointerInvalid;
            }
            if (!TryReadList(reader, locoListPointer, out KoploperTList locoList))
            {
                return KoploperObjectGraphDecodeResult.PointerInvalid;
            }
            if (locoList.Count > locoList.Capacity)
            {
                return KoploperObjectGraphDecodeResult.LocoListInvalid;
            }
            if (locoList.Count > _plausibility.MaxLocoCount)
            {
                return KoploperObjectGraphDecodeResult.PlausibilityExceeded;
            }

            var locomotives = new List<KoploperRawLocomotive>(checked((int)locoList.Count));
            var locoIds = new HashSet<uint>();
            for (uint i = 0; i < locoList.Count; i++)
            {
                if (!TryReadItemPointer(reader, locoList.ItemsArrayAddress, i, out nuint locoAddress))
                {
                    return KoploperObjectGraphDecodeResult.PointerInvalid;
                }

                if (!reader.TryReadUInt32(checked(locoAddress + _layout.LocoInternalIdOffset), out uint internalId) ||
                    !reader.TryReadUInt32(checked(locoAddress + _layout.LocoBlockRef54Offset), out uint blockRef54) ||
                    !reader.TryReadUInt32(checked(locoAddress + _layout.LocoBlockRef58Offset), out uint blockRef58))
                {
                    return KoploperObjectGraphDecodeResult.PointerInvalid;
                }

                if (!locoIds.Add(internalId))
                {
                    return KoploperObjectGraphDecodeResult.DuplicateLocoId;
                }

                locomotives.Add(new KoploperRawLocomotive(internalId, blockRef54, blockRef58));
            }

            registry = new KoploperRawRegistry(blocks, locomotives);
            return KoploperObjectGraphDecodeResult.Success;
        }

        private bool TryReadList(IKoploperMemoryReader reader, nuint listAddress, out KoploperTList list)
        {
            list = default;

            if (!reader.TryReadPointer32(checked(listAddress + _layout.TListItemsOffset), out uint itemsArrayAddress))
            {
                return false;
            }
            if (!reader.TryReadUInt32(checked(listAddress + _layout.TListCountOffset), out uint count))
            {
                return false;
            }
            if (!reader.TryReadUInt32(checked(listAddress + _layout.TListCapacityOffset), out uint capacity))
            {
                return false;
            }

            list = new KoploperTList(itemsArrayAddress, count, capacity);
            return true;
        }

        private bool TryReadItemPointer(IKoploperMemoryReader reader, nuint itemsArrayAddress, uint index, out nuint itemAddress)
        {
            itemAddress = 0;

            nuint slotAddress = checked(itemsArrayAddress + (uint)(index * 4));
            if (!reader.TryReadPointer32(slotAddress, out uint itemPointer))
            {
                return false;
            }
            if (itemPointer == 0)
            {
                return false;
            }

            itemAddress = itemPointer;
            return true;
        }
    }
}
