using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiebwaldeApp.Core.TrackApplication.Topology
{
    /// <summary>
    /// One logical track section. <see cref="Id"/> is the LOGICAL section identifier (the identity
    /// used by blocks, routes and occupancy); <see cref="AmplifierSlave"/> is the amplifier
    /// (ModBus slave) that reports/drives this section. In FullSimulation this is the simulated
    /// slave mapping; the REAL amplifier addresses are a separate physical binding and are NOT
    /// part of this topology.
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

    /// <summary>
    /// Binds one logical section to its REAL physical amplifier (ModBus slave) address. This is the
    /// separate physical binding; <see cref="LayoutSection.AmplifierSlave"/> remains the logical /
    /// simulated mapping and is not reinterpreted by this type.
    /// </summary>
    public sealed class LayoutPhysicalAmplifierBinding
    {
        public int SectionId { get; init; }
        public int PhysicalAmplifier { get; init; }
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

        /// <summary>
        /// The slaves the deterministic transport should detect: a simulation binding. The physical
        /// binding (the REAL amplifier addresses) is a future, separately-configured input and is
        /// NOT part of this topology.
        /// </summary>
        public IReadOnlyList<byte> DetectedSlaves { get; init; } = Array.Empty<byte>();

        /// <summary>
        /// The explicit logical-section -&gt; REAL-physical-amplifier binding. Empty means "no
        /// physical binding declared" (Real mode stays fail-closed). It is independent of
        /// <see cref="LayoutSection.AmplifierSlave"/>, which keeps its logical/simulated meaning.
        /// </summary>
        public IReadOnlyList<LayoutPhysicalAmplifierBinding> PhysicalAmplifierMapping { get; init; }
            = Array.Empty<LayoutPhysicalAmplifierBinding>();

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
        /// The REAL physical amplifier addresses bound by <see cref="PhysicalAmplifierMapping"/>,
        /// deduplicated and sorted ascending. Empty when no physical binding is declared.
        /// </summary>
        public IReadOnlyList<byte> ToPhysicalDomain()
            => PhysicalAmplifierMapping
                .Select(binding => (byte)binding.PhysicalAmplifier)
                .Distinct()
                .OrderBy(address => address)
                .ToArray();

        /// <summary>
        /// Projects this profile into the production <see cref="SiebwaldeApp.Core.BlockTopology"/>.
        /// Block -&gt; amplifier(s) comes from the block's sections; routes come from
        /// <see cref="Routes"/>.
        /// </summary>
        public SiebwaldeApp.Core.BlockTopology ToBlockTopology()
            => SiebwaldeApp.Core.BlockTopology.Parse(BuildBlockTopologyConfig(usePhysical: false));

        /// <summary>
        /// Projects this profile into the production <see cref="SiebwaldeApp.Core.KoploperBlockMap"/>.
        /// </summary>
        public SiebwaldeApp.Core.KoploperBlockMap ToKoploperBlockMap()
            => SiebwaldeApp.Core.KoploperBlockMap.Parse(BuildKoploperBlockMapConfig(usePhysical: false));

        /// <summary>
        /// Projects this profile into a REAL-mode <see cref="SiebwaldeApp.Core.BlockTopology"/> whose
        /// block -&gt; amplifier entries use the physical amplifier addresses from
        /// <see cref="PhysicalAmplifierMapping"/>. When no physical binding is declared, the result
        /// is empty (fail-closed).
        /// </summary>
        public SiebwaldeApp.Core.BlockTopology ToRealBlockTopology()
            => SiebwaldeApp.Core.BlockTopology.Parse(BuildBlockTopologyConfig(usePhysical: true));

        /// <summary>
        /// Projects this profile into a REAL-mode <see cref="SiebwaldeApp.Core.KoploperBlockMap"/>
        /// whose amplifier sections are the physical amplifier addresses from
        /// <see cref="PhysicalAmplifierMapping"/>. When no physical binding is declared, the result
        /// is empty (fail-closed).
        /// </summary>
        public SiebwaldeApp.Core.KoploperBlockMap ToRealKoploperBlockMap()
            => SiebwaldeApp.Core.KoploperBlockMap.Parse(BuildKoploperBlockMapConfig(usePhysical: true));

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
            => GetBlockAmplifierSlaves(blockId, usePhysical: false);

        /// <summary>
        /// The amplifier slaves of a block's sections, in section order and deduplicated. When
        /// <paramref name="usePhysical"/> is true the physical amplifier address from
        /// <see cref="PhysicalAmplifierMapping"/> is used and a section without a physical binding
        /// contributes no amplifier (mirroring the unknown-section skip).
        /// </summary>
        private IReadOnlyList<int> GetBlockAmplifierSlaves(int blockId, bool usePhysical)
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
                if (section is null)
                {
                    continue;
                }

                int amplifier;
                if (usePhysical)
                {
                    var binding = PhysicalAmplifierMapping.FirstOrDefault(b => b.SectionId == sectionId);
                    if (binding is null)
                    {
                        continue;
                    }

                    amplifier = binding.PhysicalAmplifier;
                }
                else
                {
                    amplifier = section.AmplifierSlave;
                }

                if (seen.Add(amplifier))
                {
                    result.Add(amplifier);
                }
            }

            return result;
        }

        private string BuildBlockTopologyConfig(bool usePhysical)
        {
            var sb = new StringBuilder();

            var ampEntries = Blocks
                .Select(block => $"{block.Id}:{string.Join("+", GetBlockAmplifierSlaves(block.Id, usePhysical))}")
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

        private string BuildKoploperBlockMapConfig(bool usePhysical)
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

                    int amplifier;
                    if (usePhysical)
                    {
                        var binding = PhysicalAmplifierMapping.FirstOrDefault(b => b.SectionId == sectionId);
                        if (binding is null)
                        {
                            continue; // mirror the unknown-section skip: no amplifier, no bezetmelders
                        }

                        amplifier = binding.PhysicalAmplifier;
                    }
                    else
                    {
                        amplifier = section.AmplifierSlave;
                    }

                    bezetmelders.AddRange(section.Bezetmelders);
                    if (!sections.Contains(amplifier))
                    {
                        sections.Add(amplifier);
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
