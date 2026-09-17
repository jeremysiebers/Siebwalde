using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Maps Koploper block numbers to one or more track amplifier slave numbers.
    ///
    /// Configuration is text, for example "1:1,2:2,3:3,4:4" where each entry is
    /// "block:amplifier". Multiple amplifiers can be assigned to one block with '+',
    /// for example "10:1+2".
    /// </summary>
    public sealed class BlockTopology
    {
        private readonly Dictionary<int, ushort[]> _blockToAmplifiers = new();

        private BlockTopology()
        {
        }

        /// <summary>All configured block numbers.</summary>
        public IReadOnlyCollection<int> Blocks => _blockToAmplifiers.Keys;

        /// <summary>
        /// Parses a topology configuration. Invalid entries are ignored.
        /// </summary>
        public static BlockTopology Parse(string? configuration)
        {
            var topology = new BlockTopology();

            if (string.IsNullOrWhiteSpace(configuration))
            {
                return topology;
            }

            foreach (var entry in configuration.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separatorIndex = entry.IndexOf(':');
                if (separatorIndex <= 0 || separatorIndex == entry.Length - 1)
                {
                    continue;
                }

                if (!int.TryParse(entry[..separatorIndex].Trim(), out var block))
                {
                    continue;
                }

                var amplifiers = new List<ushort>();
                foreach (var ampText in entry[(separatorIndex + 1)..].Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (ushort.TryParse(ampText, out var amplifier) && amplifier > 0)
                    {
                        amplifiers.Add(amplifier);
                    }
                }

                if (amplifiers.Count > 0)
                {
                    topology._blockToAmplifiers[block] = amplifiers.ToArray();
                }
            }

            return topology;
        }

        /// <summary>
        /// Gets the amplifier slave numbers assigned to a block.
        /// </summary>
        public bool TryGetAmplifiers(int block, out ushort[] amplifiers)
            => _blockToAmplifiers.TryGetValue(block, out amplifiers!);
    }
}
