using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>Position of a switch as seen by the routing model.</summary>
    public enum SwitchPosition
    {
        Straight = 0,
        Diverging = 1
    }

    /// <summary>
    /// A possible transition from one block to the next.
    /// A transition can be unconditional or require a specific switch position.
    /// <see cref="AllowLookAhead"/> is false for transitions that must never be
    /// pre-commanded, for example a station departure block that another train
    /// may be leaving at the same time.
    /// </summary>
    public sealed class BlockTransition
    {
        public int FromBlock { get; init; }
        public int ToBlock { get; init; }
        public int? SwitchId { get; init; }
        public SwitchPosition? RequiredSwitchPosition { get; init; }
        public bool AllowLookAhead { get; init; } = true;
    }

    /// <summary>
    /// Maps Koploper block numbers to track amplifier slave numbers and describes how
    /// blocks are chained.
    ///
    /// Configuration text uses sections separated by ';':
    ///   "amps: 1:1,2:2,3:3 ; routes: 1>2,2>3,3>4,4>1"
    ///   - "amps:"   block:amplifier, multiple amplifiers with '+' (for example "10:1+2")
    ///   - "routes:" from>to, optionally "@switchId:position" and a trailing '!' to forbid look-ahead
    ///               for example "10>11@5:0,10>12@5:1!"
    /// A section without a prefix is treated as amplifiers (backwards compatible).
    /// </summary>
    public sealed class BlockTopology
    {
        private readonly Dictionary<int, ushort[]> _blockToAmplifiers = new();
        private readonly Dictionary<int, List<BlockTransition>> _transitionsFrom = new();

        private BlockTopology()
        {
        }

        /// <summary>All configured block numbers that have amplifier mappings.</summary>
        public IReadOnlyCollection<int> Blocks => _blockToAmplifiers.Keys;

        /// <summary>All configured transitions.</summary>
        public IEnumerable<BlockTransition> Transitions => _transitionsFrom.Values.SelectMany(t => t);

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

            foreach (var section in configuration.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (section.StartsWith("routes:", StringComparison.OrdinalIgnoreCase))
                {
                    topology.ParseRoutes(section["routes:".Length..]);
                }
                else if (section.StartsWith("amps:", StringComparison.OrdinalIgnoreCase))
                {
                    topology.ParseAmplifiers(section["amps:".Length..]);
                }
                else
                {
                    topology.ParseAmplifiers(section);
                }
            }

            return topology;
        }

        private void ParseAmplifiers(string text)
        {
            foreach (var entry in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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
                    _blockToAmplifiers[block] = amplifiers.ToArray();
                }
            }
        }

        private void ParseRoutes(string text)
        {
            foreach (var entry in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var transition = ParseTransition(entry);
                if (transition is null)
                {
                    continue;
                }

                if (!_transitionsFrom.TryGetValue(transition.FromBlock, out var list))
                {
                    list = new List<BlockTransition>();
                    _transitionsFrom[transition.FromBlock] = list;
                }

                list.Add(transition);
            }
        }

        private static BlockTransition? ParseTransition(string entry)
        {
            var allowLookAhead = true;
            if (entry.EndsWith("!", StringComparison.Ordinal))
            {
                allowLookAhead = false;
                entry = entry[..^1].Trim();
            }

            int? switchId = null;
            SwitchPosition? requiredPosition = null;

            var atIndex = entry.IndexOf('@');
            if (atIndex > 0)
            {
                var switchText = entry[(atIndex + 1)..];
                entry = entry[..atIndex].Trim();

                var colonIndex = switchText.IndexOf(':');
                if (colonIndex <= 0)
                {
                    return null;
                }

                if (!int.TryParse(switchText[..colonIndex].Trim(), out var parsedSwitch))
                {
                    return null;
                }

                if (!int.TryParse(switchText[(colonIndex + 1)..].Trim(), out var position))
                {
                    return null;
                }

                switchId = parsedSwitch;
                requiredPosition = position == 0 ? SwitchPosition.Straight : SwitchPosition.Diverging;
            }

            var gtIndex = entry.IndexOf('>');
            if (gtIndex <= 0 || gtIndex == entry.Length - 1)
            {
                return null;
            }

            if (!int.TryParse(entry[..gtIndex].Trim(), out var fromBlock) ||
                !int.TryParse(entry[(gtIndex + 1)..].Trim(), out var toBlock))
            {
                return null;
            }

            return new BlockTransition
            {
                FromBlock = fromBlock,
                ToBlock = toBlock,
                SwitchId = switchId,
                RequiredSwitchPosition = requiredPosition,
                AllowLookAhead = allowLookAhead
            };
        }

        /// <summary>
        /// Gets the amplifier slave numbers assigned to a block.
        /// </summary>
        public bool TryGetAmplifiers(int block, out ushort[] amplifiers)
            => _blockToAmplifiers.TryGetValue(block, out amplifiers!);

        /// <summary>Gets all configured transitions starting at a block.</summary>
        public IReadOnlyList<BlockTransition> GetTransitionsFrom(int block)
            => _transitionsFrom.TryGetValue(block, out var list) ? list : Array.Empty<BlockTransition>();

        /// <summary>
        /// Resolves the possible next blocks for a block, given the current switch positions.
        /// When <paramref name="lookAheadOnly"/> is true, transitions that forbid look-ahead
        /// (for example station departures) are excluded.
        /// </summary>
        public bool TryGetNextBlocks(
            int block,
            IReadOnlyDictionary<int, SwitchPosition> switchPositions,
            bool lookAheadOnly,
            out List<int> nextBlocks)
        {
            nextBlocks = new List<int>();

            foreach (var transition in GetTransitionsFrom(block))
            {
                if (lookAheadOnly && !transition.AllowLookAhead)
                {
                    continue;
                }

                if (transition.SwitchId is int switchId)
                {
                    if (switchPositions is null ||
                        !switchPositions.TryGetValue(switchId, out var actual) ||
                        transition.RequiredSwitchPosition != actual)
                    {
                        continue;
                    }
                }

                nextBlocks.Add(transition.ToBlock);
            }

            return nextBlocks.Count > 0;
        }
    }
}
