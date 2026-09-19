using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Describes one Koploper block: its occupancy detectors (bezetmelders) and the
    /// physical amplifier sections it covers.
    /// </summary>
    public sealed class KoploperBlock
    {
        public int Number { get; init; }
        public IReadOnlyList<string> Bezetmelders { get; init; } = Array.Empty<string>();
        public IReadOnlyList<ushort> AmplifierSections { get; init; } = Array.Empty<ushort>();
    }

    /// <summary>
    /// Maps Koploper blocks to bezetmelders and amplifier sections, and back.
    ///
    /// Configuration text uses comma-separated entries of the form
    /// "block:bezetmelder[+bezetmelder]:section[+section]", for example:
    ///   "1:1.01+1.02:1, 2:1.03+1.04:2, 3:1.05+1.06:3, 4:1.07+1.08:4, 5:1.09+1.10:5"
    /// A line break also separates entries, because the settings field is multi-line.
    /// </summary>
    public sealed class KoploperBlockMap
    {
        private static readonly char[] EntrySeparators = { ',', '\r', '\n' };

        private readonly List<KoploperBlock> _blocks = new();

        private KoploperBlockMap()
        {
        }

        public IReadOnlyList<KoploperBlock> Blocks => _blocks;

        public static KoploperBlockMap Parse(string? configuration)
        {
            var map = new KoploperBlockMap();

            if (string.IsNullOrWhiteSpace(configuration))
            {
                return map;
            }

            foreach (var entry in configuration.Split(EntrySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = entry.Split(':');
                if (parts.Length != 3 || !int.TryParse(parts[0].Trim(), out var blockNumber))
                {
                    continue;
                }

                var bezetmelders = parts[1]
                    .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                var sections = new List<ushort>();
                foreach (var sectionText in parts[2].Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (ushort.TryParse(sectionText, out var section) && section > 0)
                    {
                        sections.Add(section);
                    }
                }

                map._blocks.Add(new KoploperBlock
                {
                    Number = blockNumber,
                    Bezetmelders = bezetmelders,
                    AmplifierSections = sections
                });
            }

            return map;
        }

        public bool TryGetByBlock(int koploperBlock, out KoploperBlock block)
        {
            block = _blocks.FirstOrDefault(b => b.Number == koploperBlock)!;
            return block is not null;
        }

        /// <summary>Finds the Koploper block a bezetmelder belongs to (for example "1.03").</summary>
        public bool TryGetByBezetmelder(string bezetmelder, out KoploperBlock block)
        {
            block = _blocks.FirstOrDefault(b =>
                b.Bezetmelders.Any(x => string.Equals(x, bezetmelder, StringComparison.OrdinalIgnoreCase)))!;
            return block is not null;
        }

        /// <summary>Finds the Koploper block that covers a physical amplifier section.</summary>
        public bool TryGetByAmplifierSection(ushort amplifierSection, out KoploperBlock block)
        {
            block = _blocks.FirstOrDefault(b => b.AmplifierSections.Contains(amplifierSection))!;
            return block is not null;
        }
    }
}
