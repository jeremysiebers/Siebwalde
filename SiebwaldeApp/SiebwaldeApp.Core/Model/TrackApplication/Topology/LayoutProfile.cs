using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiebwaldeApp.Core.TrackApplication.Topology
{
    /// <summary>
    /// One physical track section. <see cref="Id"/> is the section identifier and also the ModBus
    /// slave address the section is associated with (1..50); <see cref="AmplifierSlave"/> is the
    /// amplifier that reports this section's occupancy. In the simple oval these coincide.
    /// </summary>
    public sealed class LayoutSection
    {
        public int Id { get; init; }
        public int AmplifierSlave { get; init; }
        public IReadOnlyList<string> Bezetmelders { get; init; } = Array.Empty<string>();
        public double LengthMm { get; init; }
    }

    /// <summary>A group of ordered sections that form one Koploper block.</summary>
    public sealed class LayoutBlock
    {
        public int Id { get; init; }
        public IReadOnlyList<int> SectionIds { get; init; } = Array.Empty<int>();
    }

    /// <summary>Maps an ECoS/Koploper switch address to a physical switch output.</summary>
    public sealed class LayoutSwitch
    {
        public int EcosAddress { get; init; }
        public int PhysicalAddress { get; init; }
        public bool Inverted { get; init; }

        /// <summary>Position to drive to at initialization, or null for "keep".</summary>
        public SwitchPosition? DefaultPosition { get; init; }
    }

    /// <summary>A directional transition from one block to another.</summary>
    public sealed class LayoutRoute
    {
        public int FromBlock { get; init; }
        public int ToBlock { get; init; }
        public int? SwitchId { get; init; }
        public SwitchPosition? RequiredSwitchPosition { get; init; }
        public bool AllowLookAhead { get; init; } = true;
    }

    /// <summary>A locomotive and the block it is initially placed in.</summary>
    public sealed class LayoutLocomotive
    {
        public int Address { get; init; }
        public int InitialBlock { get; init; }
    }

    /// <summary>
    /// Immutable, config-driven description of a simulated model railway layout. It is the durable
    /// source from which the existing runtime types (<see cref="SiebwaldeApp.Core.BlockTopology"/>,
    /// <see cref="SiebwaldeApp.Core.KoploperBlockMap"/>, <see cref="SiebwaldeApp.Core.SwitchMapping"/>,
    /// <see cref="SiebwaldeApp.Core.TrackAmplifierGroups"/>) are projected, so the real control chain
    /// is composed from one profile instead of from several hand-maintained configuration strings.
    /// </summary>
    public sealed class LayoutProfile
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public IReadOnlyList<byte> DetectedSlaves { get; init; } = Array.Empty<byte>();
        public IReadOnlyList<LayoutSection> Sections { get; init; } = Array.Empty<LayoutSection>();
        public IReadOnlyList<LayoutBlock> Blocks { get; init; } = Array.Empty<LayoutBlock>();
        public IReadOnlyList<LayoutSwitch> Switches { get; init; } = Array.Empty<LayoutSwitch>();
        public IReadOnlyList<LayoutRoute> Routes { get; init; } = Array.Empty<LayoutRoute>();
        public IReadOnlyList<LayoutLocomotive> Locomotives { get; init; } = Array.Empty<LayoutLocomotive>();

        /// <summary>Maps a section identifier to its defining section, or null when unknown.</summary>
        public LayoutSection? TryGetSection(int sectionId)
            => Sections.FirstOrDefault(s => s.Id == sectionId);

        /// <summary>Maps a block identifier to its defining block, or null when unknown.</summary>
        public LayoutBlock? TryGetBlock(int blockId)
            => Blocks.FirstOrDefault(b => b.Id == blockId);

        /// <summary>
        /// Projects this profile into the production <see cref="SiebwaldeApp.Core.BlockTopology"/>.
        /// Block -&gt; amplifier(s) comes from the block's sections; routes come from
        /// <see cref="Routes"/>.
        /// </summary>
        public SiebwaldeApp.Core.BlockTopology ToBlockTopology()
            => SiebwaldeApp.Core.BlockTopology.Parse(BuildBlockTopologyConfig());

        /// <summary>
        /// Projects this profile into the production <see cref="SiebwaldeApp.Core.KoploperBlockMap"/>.
        /// </summary>
        public SiebwaldeApp.Core.KoploperBlockMap ToKoploperBlockMap()
            => SiebwaldeApp.Core.KoploperBlockMap.Parse(BuildKoploperBlockMapConfig());

        /// <summary>
        /// Projects this profile into the production <see cref="SiebwaldeApp.Core.SwitchMapping"/>.
        /// </summary>
        public SiebwaldeApp.Core.SwitchMapping ToSwitchMapping()
            => SiebwaldeApp.Core.SwitchMapping.Parse(BuildSwitchMapConfig());

        /// <summary>
        /// The operational grouping (main / mountain / spare) is deliberately NOT part of the
        /// profile: it remains an open Product Owner decision and is not inferred from detected
        /// slaves. This returns the empty grouping; the FullSimulation runtime then falls back to
        /// the profile's detected slaves as the observed-neutral domain, which is the established
        /// behaviour for an unconfigured domain.
        /// </summary>
        public SiebwaldeApp.Core.TrackAmplifierGroups ToTrackAmplifierGroups()
            => SiebwaldeApp.Core.TrackAmplifierGroups.Empty;

        /// <summary>The amplifier slaves of a block's sections, in section order and deduplicated.</summary>
        public IReadOnlyList<int> GetBlockAmplifierSlaves(int blockId)
        {
            var block = TryGetBlock(blockId);
            if (block is null)
            {
                return Array.Empty<int>();
            }

            var seen = new HashSet<int>();
            var result = new List<int>();
            foreach (var sectionId in block.SectionIds)
            {
                var section = TryGetSection(sectionId);
                if (section is not null && seen.Add(section.AmplifierSlave))
                {
                    result.Add(section.AmplifierSlave);
                }
            }

            return result;
        }

        private string BuildBlockTopologyConfig()
        {
            var sb = new StringBuilder();

            var ampEntries = Blocks
                .Select(block => $"{block.Id}:{string.Join("+", GetBlockAmplifierSlaves(block.Id))}")
                .Where(entry => !entry.EndsWith(":", StringComparison.Ordinal));
            sb.Append("amps: ").Append(string.Join(",", ampEntries));

            if (Routes.Count > 0)
            {
                sb.Append(" ; routes: ").Append(string.Join(",", Routes.Select(FormatRoute)));
            }

            return sb.ToString();
        }

        private static string FormatRoute(LayoutRoute route)
        {
            var entry = $"{route.FromBlock}>{route.ToBlock}";
            if (route.SwitchId is int switchId)
            {
                entry += $"@{switchId}:{(route.RequiredSwitchPosition == SwitchPosition.Diverging ? 1 : 0)}";
            }

            if (!route.AllowLookAhead)
            {
                entry += "!";
            }

            return entry;
        }

        private string BuildKoploperBlockMapConfig()
        {
            var entries = new List<string>();
            foreach (var block in Blocks)
            {
                var bezetmelders = new List<string>();
                var sections = new List<int>();
                foreach (var sectionId in block.SectionIds)
                {
                    var section = TryGetSection(sectionId);
                    if (section is null)
                    {
                        continue;
                    }

                    bezetmelders.AddRange(section.Bezetmelders);
                    if (!sections.Contains(section.AmplifierSlave))
                    {
                        sections.Add(section.AmplifierSlave);
                    }
                }

                if (sections.Count == 0)
                {
                    continue;
                }

                entries.Add($"{block.Id}:{string.Join("+", bezetmelders)}:{string.Join("+", sections)}");
            }

            return string.Join(",", entries);
        }

        private string BuildSwitchMapConfig()
        {
            if (Switches.Count == 0)
            {
                return string.Empty;
            }

            var entries = Switches.Select(sw =>
            {
                var parts = new List<string> { sw.EcosAddress.ToString(), sw.PhysicalAddress.ToString() };
                if (sw.Inverted)
                {
                    parts.Add("inverted");
                }

                if (sw.DefaultPosition == SwitchPosition.Straight)
                {
                    parts.Add("g");
                }
                else if (sw.DefaultPosition == SwitchPosition.Diverging)
                {
                    parts.Add("r");
                }

                return string.Join(":", parts);
            });

            return "switches: " + string.Join(",", entries);
        }
    }
}
