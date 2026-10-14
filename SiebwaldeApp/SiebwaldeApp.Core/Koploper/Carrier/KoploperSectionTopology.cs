using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Section-level adjacency for the block-level shadow (KIS10-02): within a Koploper block the
    /// bound sections are adjacent in declaration order, and across blocks the LAST section of block B
    /// is adjacent to the FIRST section of block C for every switch-free route B → C in
    /// <see cref="SiebwaldeApp.Core.BlockTopology"/>. Pure and stateless; used by the footprint
    /// reconstructor and the carrier's partial-footprint containment flood.
    /// </summary>
    internal static class KoploperSectionTopology
    {
        /// <summary>
        /// Builds an UNDIRECTED section adjacency map (section → adjacent sections). Switch-gated
        /// routes are excluded (an empty switch-position set makes <c>TryGetNextBlocks</c> skip them).
        /// </summary>
        public static Dictionary<int, List<int>> BuildSectionAdjacency(
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockToSections,
            BlockTopology topology)
        {
            var adjacency = new Dictionary<int, List<int>>();

            void Add(int a, int b)
            {
                if (a == b)
                {
                    return;
                }

                if (!adjacency.TryGetValue(a, out List<int>? fromA))
                {
                    fromA = new List<int>();
                    adjacency[a] = fromA;
                }

                if (!fromA.Contains(b))
                {
                    fromA.Add(b);
                }

                if (!adjacency.TryGetValue(b, out List<int>? fromB))
                {
                    fromB = new List<int>();
                    adjacency[b] = fromB;
                }

                if (!fromB.Contains(a))
                {
                    fromB.Add(a);
                }
            }

            var firstSection = new Dictionary<int, int>();
            var lastSection = new Dictionary<int, int>();
            foreach (KeyValuePair<int, IReadOnlyList<int>> entry in blockToSections)
            {
                IReadOnlyList<int>? sections = entry.Value;
                if (sections is null || sections.Count == 0)
                {
                    continue;
                }

                firstSection[entry.Key] = sections[0];
                lastSection[entry.Key] = sections[sections.Count - 1];

                for (int i = 0; i + 1 < sections.Count; i++)
                {
                    Add(sections[i], sections[i + 1]);
                }
            }

            var noSwitches = new Dictionary<int, SwitchPosition>();
            foreach (int block in blockToSections.Keys)
            {
                if (topology.TryGetNextBlocks(block, noSwitches, lookAheadOnly: false, out List<int>? nextBlocks))
                {
                    foreach (int next in nextBlocks)
                    {
                        if (lastSection.TryGetValue(block, out int last) && firstSection.TryGetValue(next, out int first))
                        {
                            Add(last, first);
                        }
                    }
                }
            }

            return adjacency;
        }
    }
}
