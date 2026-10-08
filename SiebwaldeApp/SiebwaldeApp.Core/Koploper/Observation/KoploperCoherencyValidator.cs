using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Compares two raw object-graph observations and reports whether they observed the same
    /// in-memory graph. Pure and stateless; used by the snapshot reader to reject torn reads.
    /// </summary>
    public static class KoploperCoherencyValidator
    {
        /// <summary>
        /// True when the two observations agree on every coherency anchor: the resolved root,
        /// both TList headers (item-array address, count, capacity), both item-address arrays,
        /// every compared per-block raw field and every compared per-locomotive raw field.
        /// </summary>
        public static bool IsCoherent(KoploperRawObjectGraphObservation first, KoploperRawObjectGraphObservation second)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);

            if (first.ResolvedRoot != second.ResolvedRoot)
            {
                return false;
            }

            if (!TListEqual(first.BlockList, second.BlockList))
            {
                return false;
            }

            if (!TListEqual(first.LocoList, second.LocoList))
            {
                return false;
            }

            if (!first.BlockItemAddresses.SequenceEqual(second.BlockItemAddresses))
            {
                return false;
            }

            if (!first.LocoItemAddresses.SequenceEqual(second.LocoItemAddresses))
            {
                return false;
            }

            if (!BlocksEqual(first.Registry.Blocks, second.Registry.Blocks))
            {
                return false;
            }

            if (!LocomotivesEqual(first.Registry.Locomotives, second.Registry.Locomotives))
            {
                return false;
            }

            return true;
        }

        private static bool TListEqual(KoploperTList first, KoploperTList second)
        {
            return first.ItemsArrayAddress == second.ItemsArrayAddress
                && first.Count == second.Count
                && first.Capacity == second.Capacity;
        }

        private static bool BlocksEqual(IReadOnlyList<KoploperRawBlock> first, IReadOnlyList<KoploperRawBlock> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (int i = 0; i < first.Count; i++)
            {
                if (!BlockEqual(first[i], second[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool BlockEqual(KoploperRawBlock first, KoploperRawBlock second)
        {
            return first.InternalBlockId == second.InternalBlockId
                && first.DisplayBlockNumber == second.DisplayBlockNumber
                && first.OwnerPointer == second.OwnerPointer
                && first.RawState == second.RawState
                && first.ChangedFlag == second.ChangedFlag
                && first.UpdateTick == second.UpdateTick
                && first.ManualBlockedRaw == second.ManualBlockedRaw;
        }

        private static bool LocomotivesEqual(IReadOnlyList<KoploperRawLocomotive> first, IReadOnlyList<KoploperRawLocomotive> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (int i = 0; i < first.Count; i++)
            {
                if (!LocomotiveEqual(first[i], second[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool LocomotiveEqual(KoploperRawLocomotive first, KoploperRawLocomotive second)
        {
            return first.InternalLocomotiveId == second.InternalLocomotiveId
                && first.BlockRef54 == second.BlockRef54
                && first.BlockRef58 == second.BlockRef58;
        }
    }
}
