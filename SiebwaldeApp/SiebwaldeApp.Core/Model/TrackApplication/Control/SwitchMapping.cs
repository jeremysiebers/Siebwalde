using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// One switch mapping: which ECoS/Koploper switch address drives which physical switch
    /// output, how the requested state is translated, and what the switch should do at
    /// initialization.
    /// </summary>
    public sealed class SwitchMappingEntry
    {
        /// <summary>The switch address Koploper uses in <c>set(&lt;id&gt;,switch[&lt;addr&gt;g|r])</c>.</summary>
        public int EcosAddress { get; init; }

        /// <summary>The physical switch/output address on the accessory bus.</summary>
        public int PhysicalAddress { get; init; }

        /// <summary>
        /// When true the ECoS direction is swapped for the physical output
        /// (ECoS straight drives the physical diverging output and vice versa).
        /// </summary>
        public bool Inverted { get; init; }

        /// <summary>
        /// Position to drive to during initialization, or null for <c>keep</c>: leave the
        /// switch alone because the physical rest position is not known or already correct.
        /// </summary>
        public SwitchPosition? DefaultPosition { get; init; }

        /// <summary>Translates an ECoS position into the physical position for this switch.</summary>
        public SwitchPosition ToPhysical(SwitchPosition ecosPosition)
            => Inverted
                ? (ecosPosition == SwitchPosition.Straight ? SwitchPosition.Diverging : SwitchPosition.Straight)
                : ecosPosition;
    }

    /// <summary>
    /// Maps ECoS/Koploper switch addresses to physical switch outputs.
    ///
    /// Configuration text is a comma (or line-break) separated list of
    /// <c>ecosAddress:physicalAddress[:inverted][:default]</c> entries, optionally prefixed
    /// with <c>switches:</c>, for example:
    ///   "switches: 1:1:keep, 2:2:keep"
    ///   "1:5:inverted:g, 2:2:r"
    ///
    /// The default marker is <c>g</c> (straight), <c>r</c> (diverging) or <c>keep</c>
    /// (do not drive at initialization). Omitting it means <c>keep</c>.
    ///
    /// Invalid entries are never turned into a plausible-but-wrong mapping: they are skipped
    /// and recorded in <see cref="Errors"/> so the caller can report them. A duplicate ECoS
    /// address is treated as a conflict; the first entry wins and the conflict is recorded.
    /// </summary>
    public sealed class SwitchMapping
    {
        private static readonly char[] EntrySeparators = { ',', '\r', '\n' };

        private readonly Dictionary<int, SwitchMappingEntry> _byEcosAddress = new();
        private readonly List<string> _errors = new();

        private SwitchMapping()
        {
        }

        /// <summary>All valid entries, in configuration order.</summary>
        public IReadOnlyList<SwitchMappingEntry> Entries => _byEcosAddress.Values.ToList();

        /// <summary>Descriptions of entries that could not be used.</summary>
        public IReadOnlyList<string> Errors => _errors;

        /// <summary>True when the configuration was non-empty but produced no usable entry.</summary>
        public bool HasErrors => _errors.Count > 0;

        /// <summary>Parses a switch mapping. Invalid entries are skipped and recorded.</summary>
        public static SwitchMapping Parse(string? configuration)
        {
            var mapping = new SwitchMapping();

            if (string.IsNullOrWhiteSpace(configuration))
            {
                return mapping;
            }

            var text = configuration.Trim();
            if (text.StartsWith("switches:", StringComparison.OrdinalIgnoreCase))
            {
                text = text["switches:".Length..];
            }

            foreach (var entry in text.Split(EntrySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                mapping.ParseEntry(entry);
            }

            return mapping;
        }

        private void ParseEntry(string entry)
        {
            var parts = entry.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length is < 2 or > 4)
            {
                _errors.Add($"'{entry}': expected ecos:physical[:inverted][:g|r|keep]");
                return;
            }

            if (!int.TryParse(parts[0], out var ecosAddress) || ecosAddress <= 0)
            {
                _errors.Add($"'{entry}': '{parts[0]}' is not a valid ECoS switch address");
                return;
            }

            if (!int.TryParse(parts[1], out var physicalAddress) || physicalAddress <= 0)
            {
                _errors.Add($"'{entry}': '{parts[1]}' is not a valid physical switch address");
                return;
            }

            var inverted = false;
            SwitchPosition? defaultPosition = null;

            for (var i = 2; i < parts.Length; i++)
            {
                var token = parts[i];

                if (token.Equals("inverted", StringComparison.OrdinalIgnoreCase))
                {
                    inverted = true;
                }
                else if (token.Equals("normal", StringComparison.OrdinalIgnoreCase))
                {
                    inverted = false;
                }
                else if (token.Equals("g", StringComparison.OrdinalIgnoreCase) ||
                         token.Equals("straight", StringComparison.OrdinalIgnoreCase))
                {
                    defaultPosition = SwitchPosition.Straight;
                }
                else if (token.Equals("r", StringComparison.OrdinalIgnoreCase) ||
                         token.Equals("diverging", StringComparison.OrdinalIgnoreCase))
                {
                    defaultPosition = SwitchPosition.Diverging;
                }
                else if (token.Equals("keep", StringComparison.OrdinalIgnoreCase))
                {
                    defaultPosition = null;
                }
                else
                {
                    _errors.Add($"'{entry}': '{token}' is not a valid modifier (expected inverted, g, r or keep)");
                    return;
                }
            }

            if (_byEcosAddress.ContainsKey(ecosAddress))
            {
                _errors.Add($"'{entry}': ECoS switch address {ecosAddress} is already mapped; keeping the first entry");
                return;
            }

            _byEcosAddress[ecosAddress] = new SwitchMappingEntry
            {
                EcosAddress = ecosAddress,
                PhysicalAddress = physicalAddress,
                Inverted = inverted,
                DefaultPosition = defaultPosition
            };
        }

        /// <summary>Looks up the mapping for an ECoS/Koploper switch address.</summary>
        public bool TryGetEntry(int ecosAddress, out SwitchMappingEntry entry)
            => _byEcosAddress.TryGetValue(ecosAddress, out entry!);

        /// <summary>True when the ECoS address is mapped to a physical switch.</summary>
        public bool IsMapped(int ecosAddress) => _byEcosAddress.ContainsKey(ecosAddress);
    }
}
