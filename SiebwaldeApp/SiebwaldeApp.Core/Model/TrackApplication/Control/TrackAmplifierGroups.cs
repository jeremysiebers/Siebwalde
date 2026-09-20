using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The operational role of a physical track amplifier. This is a different concept from the
    /// physical device type (<see cref="TrackAmplifierAddress"/>) and from the logical block
    /// mapping (<see cref="BlockTopology"/>):
    ///
    /// - physical device type decides whether track-amplifier PWM/HR0 semantics are legal at all;
    /// - operational group decides which safety domain an amplifier belongs to (main railway,
    ///   mountain railway, spare);
    /// - block mapping decides which blocks a locomotive may be routed through.
    ///
    /// An amplifier may be a legitimate track amplifier yet belong to no configured group; it
    /// must not be inferred as main railway from its address alone.
    /// </summary>
    public enum TrackAmplifierOperationalGroup
    {
        /// <summary>Not named by any configured group.</summary>
        Unassigned = 0,

        /// <summary>Main railway amplifier.</summary>
        MainRailway = 1,

        /// <summary>Mountain-railway amplifier (separate operational domain, same hardware).</summary>
        MountainRailway = 2,

        /// <summary>Installed spare amplifier without its own operational role.</summary>
        Spare = 3
    }

    /// <summary>
    /// Authoritative operational grouping of physical track amplifiers.
    ///
    /// It answers the maintainer questions that the physical address range cannot:
    /// which addresses are legitimate track amplifiers (<see cref="TrackAmplifierAddress"/>),
    /// which are main railway, which are mountain railway, which are installed spares, and which
    /// addresses are backplane/configuration devices that must never receive PWM commands.
    ///
    /// Configuration text uses sections separated by ';' (a line break also separates sections,
    /// because the settings field is multi-line):
    ///   "main: 1,3,4,5 ; mountain: 6,7 ; spare: 8"
    /// An address may appear in at most one group. Non-track-amplifier and duplicate entries are
    /// recorded in <see cref="Errors"/> and ignored, so a malformed configuration never silently
    /// becomes a plausible but wrong safety domain.
    ///
    /// This type deliberately does not decide cross-domain safety behaviour (for example whether
    /// a main-railway failure must also neutralize the mountain railway). That remains an explicit
    /// safety-policy decision.
    /// </summary>
    public sealed class TrackAmplifierGroups
    {
        private static readonly char[] SectionSeparators = { ';', '\r', '\n' };

        private readonly Dictionary<ushort, TrackAmplifierOperationalGroup> _byAddress;

        private TrackAmplifierGroups(
            IReadOnlyList<ushort> mainRailway,
            IReadOnlyList<ushort> mountainRailway,
            IReadOnlyList<ushort> spare,
            IReadOnlyList<string> errors)
        {
            MainRailway = mainRailway;
            MountainRailway = mountainRailway;
            Spare = spare;
            Errors = errors;

            _byAddress = new Dictionary<ushort, TrackAmplifierOperationalGroup>();
            foreach (var address in mainRailway)
            {
                _byAddress[address] = TrackAmplifierOperationalGroup.MainRailway;
            }

            foreach (var address in mountainRailway)
            {
                _byAddress[address] = TrackAmplifierOperationalGroup.MountainRailway;
            }

            foreach (var address in spare)
            {
                _byAddress[address] = TrackAmplifierOperationalGroup.Spare;
            }

            AllConfigured = _byAddress.Keys.OrderBy(a => a).ToArray();
        }

        /// <summary>No amplifier is assigned to any operational group.</summary>
        public static TrackAmplifierGroups Empty { get; } =
            new(Array.Empty<ushort>(), Array.Empty<ushort>(), Array.Empty<ushort>(), Array.Empty<string>());

        /// <summary>Configured main-railway amplifier addresses, ordered.</summary>
        public IReadOnlyList<ushort> MainRailway { get; }

        /// <summary>Configured mountain-railway amplifier addresses, ordered.</summary>
        public IReadOnlyList<ushort> MountainRailway { get; }

        /// <summary>Configured installed-spare amplifier addresses, ordered.</summary>
        public IReadOnlyList<ushort> Spare { get; }

        /// <summary>Every configured amplifier address across all groups, ordered.</summary>
        public IReadOnlyList<ushort> AllConfigured { get; }

        /// <summary>Human-readable configuration problems found while parsing.</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>The operational group of an address, or <see cref="TrackAmplifierOperationalGroup.Unassigned"/>.</summary>
        public TrackAmplifierOperationalGroup Classify(ushort address)
            => _byAddress.TryGetValue(address, out var group)
                ? group
                : TrackAmplifierOperationalGroup.Unassigned;

        /// <summary>True when the address is named by any configured group.</summary>
        public bool IsConfigured(ushort address) => _byAddress.ContainsKey(address);

        /// <summary>The addresses configured in one operational group, ordered.</summary>
        public IReadOnlyList<ushort> GetGroup(TrackAmplifierOperationalGroup group) => group switch
        {
            TrackAmplifierOperationalGroup.MainRailway => MainRailway,
            TrackAmplifierOperationalGroup.MountainRailway => MountainRailway,
            TrackAmplifierOperationalGroup.Spare => Spare,
            _ => Array.Empty<ushort>()
        };

        /// <summary>
        /// Builds a grouping from explicit address collections. Non-track-amplifier and duplicate
        /// addresses are reported in <see cref="Errors"/> and ignored.
        /// </summary>
        public static TrackAmplifierGroups Create(
            IEnumerable<int>? mainRailway,
            IEnumerable<int>? mountainRailway,
            IEnumerable<int>? spare)
        {
            var errors = new List<string>();
            var assigned = new HashSet<ushort>();

            var main = Sanitize(mainRailway, "main", assigned, errors);
            var mountain = Sanitize(mountainRailway, "mountain", assigned, errors);
            var spareAmps = Sanitize(spare, "spare", assigned, errors);

            return new TrackAmplifierGroups(main, mountain, spareAmps, errors);
        }

        /// <summary>
        /// Parses a grouping configuration. Invalid entries are recorded in <see cref="Errors"/>
        /// instead of being silently dropped.
        /// </summary>
        public static TrackAmplifierGroups Parse(string? configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration))
            {
                return Empty;
            }

            var main = new List<int>();
            var mountain = new List<int>();
            var spare = new List<int>();

            foreach (var section in configuration.Split(
                SectionSeparators,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separatorIndex = section.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var name = section[..separatorIndex].Trim();
                var value = section[(separatorIndex + 1)..];

                var target = name.ToLowerInvariant() switch
                {
                    "main" or "mainrailway" or "main-railway" => main,
                    "mountain" or "mountainrailway" or "mountain-railway" => mountain,
                    "spare" or "spares" => spare,
                    _ => null
                };

                if (target is null)
                {
                    continue;
                }

                foreach (var token in value.Split(
                    new[] { ',', ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (int.TryParse(token, out var address))
                    {
                        target.Add(address);
                    }
                }
            }

            return Create(main, mountain, spare);
        }

        private static IReadOnlyList<ushort> Sanitize(
            IEnumerable<int>? addresses,
            string groupName,
            HashSet<ushort> assigned,
            List<string> errors)
        {
            var result = new List<ushort>();
            if (addresses is null)
            {
                return result;
            }

            foreach (var address in addresses.Distinct())
            {
                if (!TrackAmplifierAddress.IsTrackAmplifierAddress(address))
                {
                    errors.Add(
                        $"Group '{groupName}' address {address} is not a track amplifier " +
                        $"({TrackAmplifierAddress.MinTrackAmplifier}..{TrackAmplifierAddress.MaxTrackAmplifier}); ignored.");
                    continue;
                }

                var value = (ushort)address;
                if (!assigned.Add(value))
                {
                    errors.Add($"Address {address} is assigned to more than one operational group; ignored in '{groupName}'.");
                    continue;
                }

                result.Add(value);
            }

            result.Sort();
            return result;
        }
    }
}
