using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Chooses the next block to pre-command (look-ahead) for a locomotive.
    ///
    /// Only transitions that allow look-ahead are considered (station departures are
    /// excluded), and only when the target block is free. When a switch offers several
    /// possible next blocks, the switch position selects the candidate.
    /// </summary>
    public sealed class LookAheadPlanner
    {
        private readonly BlockTopology _topology;

        public LookAheadPlanner(BlockTopology topology)
        {
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
        }

        /// <summary>
        /// Tries to find the next block that may be pre-commanded from the current block.
        /// </summary>
        /// <param name="currentBlock">Block the locomotive currently occupies.</param>
        /// <param name="switchPositions">Current switch positions (switch id -> position).</param>
        /// <param name="occupancy">Occupancy source used to check whether the target block is free.</param>
        /// <param name="nextBlock">The chosen next block when the method returns true.</param>
        public bool TryPlanNext(
            int currentBlock,
            IReadOnlyDictionary<int, SwitchPosition> switchPositions,
            IOccupancyProvider occupancy,
            out int nextBlock)
        {
            nextBlock = 0;

            if (occupancy is null ||
                !_topology.TryGetNextBlocks(currentBlock, switchPositions, lookAheadOnly: true, out var candidates))
            {
                return false;
            }

            foreach (var candidate in candidates)
            {
                if (!occupancy.IsBlockOccupied(candidate))
                {
                    nextBlock = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
