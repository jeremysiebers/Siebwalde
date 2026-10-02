using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Core.TrackApplication.Topology
{
    /// <summary>
    /// Loads and validates a <see cref="LayoutProfile"/> from JSON text or a file.
    ///
    /// Validation is exhaustive: every problem is collected and returned in
    /// <see cref="TryLoad"/>'s <c>errors</c> list, never silently dropped. A profile is only
    /// returned when the JSON is syntactically valid AND passes every rule below.
    /// </summary>
    public static class LayoutProfileLoader
    {
        /// <summary>Maximum valid section / amplifier slave address (track amplifier range).</summary>
        private const int MaxSlave = TrackAmplifierAddress.MaxTrackAmplifier; // 50

        /// <summary>Number of points per bezetmelder module (ECoS feedback module size).</summary>
        private const int PointsPerModule = 16;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// The directory of repository-managed profiles, resolved against
        /// <see cref="AppContext.BaseDirectory"/> (the files are copied to the output directory by
        /// the Core project).
        /// </summary>
        public static string ProfilesDirectory
            => Path.Combine(AppContext.BaseDirectory, "Topology", "profiles");

        /// <summary>
        /// The repository-managed "Simple Loop" profile file, resolved against
        /// <see cref="AppContext.BaseDirectory"/>.
        /// </summary>
        public static string SimpleLoopPath
            => Path.Combine(ProfilesDirectory, "simple-loop.json");

        /// <summary>
        /// The repository-managed "Koploper Oval" profile file, resolved against
        /// <see cref="AppContext.BaseDirectory"/>.
        /// </summary>
        public static string KoploperOvalPath
            => Path.Combine(ProfilesDirectory, "koploper-oval.json");

        /// <summary>
        /// Enumerates the display names (<see cref="LayoutProfile.Name"/>) of the loadable
        /// repository profiles by scanning <c>Topology/profiles/*.json</c> under
        /// <see cref="AppContext.BaseDirectory"/>. Files that fail to load or validate are skipped.
        /// </summary>
        public static IReadOnlyList<string> GetAvailableProfileNames()
        {
            var names = new List<string>();

            foreach (var path in EnumerateProfileFiles())
            {
                if (TryLoadFromFile(path, out var profile, out _) &&
                    profile is not null &&
                    !string.IsNullOrWhiteSpace(profile.Name))
                {
                    names.Add(profile.Name);
                }
            }

            return names;
        }

        /// <summary>
        /// Loads the repository profile whose <see cref="LayoutProfile.Name"/> equals
        /// <paramref name="name"/> (case-insensitive). Returns false (with an error) when no such
        /// profile exists or it cannot be loaded/validated.
        /// </summary>
        public static bool TryLoadByName(
            string? name,
            out LayoutProfile? profile,
            out IReadOnlyList<string> errors)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                profile = null;
                errors = new List<string> { "No profile name was supplied." };
                return false;
            }

            foreach (var path in EnumerateProfileFiles())
            {
                if (TryLoadFromFile(path, out var candidate, out _) && candidate is not null)
                {
                    if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        profile = candidate;
                        errors = Array.Empty<string>();
                        return true;
                    }
                }
            }

            profile = null;
            errors = new List<string> { $"No repository profile named '{name}' was found in '{ProfilesDirectory}'." };
            return false;
        }

        private static IEnumerable<string> EnumerateProfileFiles()
        {
            if (!Directory.Exists(ProfilesDirectory))
            {
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(ProfilesDirectory, "*.json")
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }

        /// <summary>Loads a profile from JSON text. Returns false (with every error) on any problem.</summary>
        public static bool TryLoad(string? json, out LayoutProfile? profile, out IReadOnlyList<string> errors)
        {
            var errorList = new List<string>();

            if (string.IsNullOrWhiteSpace(json))
            {
                profile = null;
                errors = errorList;
                errorList.Add("No JSON content was supplied.");
                return false;
            }

            ProfileDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<ProfileDto>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                profile = null;
                errorList.Add($"The profile JSON is not valid: {ex.Message}");
                errors = errorList;
                return false;
            }

            if (dto is null)
            {
                profile = null;
                errorList.Add("The profile JSON deserialized to null.");
                errors = errorList;
                return false;
            }

            profile = BuildProfile(dto, errorList);

            if (errorList.Count > 0)
            {
                profile = null;
                errors = errorList;
                return false;
            }

            errors = errorList;
            return true;
        }

        /// <summary>Loads a profile from a file. Returns false (with every error) on any problem.</summary>
        public static bool TryLoadFromFile(string path, out LayoutProfile? profile, out IReadOnlyList<string> errors)
        {
            try
            {
                var text = File.ReadAllText(path);
                return TryLoad(text, out profile, out errors);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                profile = null;
                errors = new List<string> { $"Could not read the profile file '{path}': {ex.Message}" };
                return false;
            }
        }

        private static LayoutProfile BuildProfile(ProfileDto dto, List<string> errors)
        {
            var sections = BuildSections(dto.Sections, errors);
            var switches = BuildSwitches(dto.Switches, errors);

            var sectionIds = new HashSet<int>(sections.Select(s => s.Id));
            var switchEcos = new HashSet<int>(switches.Select(s => s.EcosAddress));

            var blocks = BuildBlocks(dto.Blocks, sectionIds, errors);
            var blockIds = new HashSet<int>(blocks.Select(b => b.Id));

            var routes = BuildRoutes(dto.Routes, blockIds, switchEcos, errors);

            var locomotives = BuildLocomotives(dto.Locomotives, blockIds, errors);

            var detectedSlaves = BuildDetectedSlaves(dto.DetectedSlaves, errors);

            var physicalMapping = BuildPhysicalAmplifierMapping(dto.PhysicalAmplifierMapping, sectionIds, errors);

            return new LayoutProfile
            {
                Name = dto.Name ?? string.Empty,
                Description = dto.Description ?? string.Empty,
                DetectedSlaves = detectedSlaves,
                PhysicalAmplifierMapping = physicalMapping,
                Sections = sections,
                Blocks = blocks,
                Switches = switches,
                Routes = routes,
                Locomotives = locomotives
            };
        }

        private static List<LayoutSection> BuildSections(List<SectionDto>? sectionDtos, List<string> errors)
        {
            var result = new List<LayoutSection>();
            if (sectionDtos is null)
            {
                return result;
            }

            var seen = new HashSet<int>();
            foreach (var dto in sectionDtos)
            {
                var id = dto.Id;
                if (id < TrackAmplifierAddress.MinTrackAmplifier || id > MaxSlave)
                {
                    errors.Add($"Section id {id} is outside the valid range 1..{MaxSlave}.");
                    continue;
                }

                if (!seen.Add(id))
                {
                    errors.Add($"Duplicate section id {id}.");
                    continue;
                }

                if (dto.AmplifierSlave < TrackAmplifierAddress.MinTrackAmplifier || dto.AmplifierSlave > MaxSlave)
                {
                    errors.Add($"Section {id} has an invalid amplifier slave {dto.AmplifierSlave} (valid 1..{MaxSlave}).");
                    continue;
                }

                if (!(dto.LengthMm > 0))
                {
                    errors.Add($"Section {id} has a non-positive length ({dto.LengthMm} mm).");
                    continue;
                }

                var bezetmelders = dto.Bezetmelders ?? new List<string>();
                var hasBadBezetmelder = false;
                foreach (var name in bezetmelders)
                {
                    if (!TryParseBezetmelder(name, out _, out _))
                    {
                        errors.Add($"Section {id} has an invalid bezetmelder name '{name}' (expected 'module.point' with point 1..{PointsPerModule}).");
                        hasBadBezetmelder = true;
                    }
                }

                if (hasBadBezetmelder)
                {
                    continue;
                }

                result.Add(new LayoutSection
                {
                    Id = id,
                    AmplifierSlave = dto.AmplifierSlave,
                    Bezetmelders = bezetmelders.ToArray(),
                    LengthMm = dto.LengthMm
                });
            }

            return result;
        }

        private static List<LayoutSwitch> BuildSwitches(List<SwitchDto>? switchDtos, List<string> errors)
        {
            var result = new List<LayoutSwitch>();
            if (switchDtos is null)
            {
                return result;
            }

            var seen = new HashSet<int>();
            foreach (var dto in switchDtos)
            {
                if (dto.EcosAddress <= 0)
                {
                    errors.Add($"Switch ECoS address {dto.EcosAddress} is not valid.");
                    continue;
                }

                if (dto.PhysicalAddress <= 0)
                {
                    errors.Add($"Switch physical address {dto.PhysicalAddress} is not valid.");
                    continue;
                }

                if (!seen.Add(dto.EcosAddress))
                {
                    errors.Add($"Duplicate switch ECoS address {dto.EcosAddress}.");
                    continue;
                }

                SwitchPosition? defaultPosition = null;
                if (!string.IsNullOrWhiteSpace(dto.DefaultPosition))
                {
                    if (!TryParsePosition(dto.DefaultPosition, out var parsed))
                    {
                        errors.Add($"Switch {dto.EcosAddress} has an invalid default position '{dto.DefaultPosition}' (expected 'straight', 'diverging' or 'keep').");
                        continue;
                    }

                    defaultPosition = parsed;
                }

                result.Add(new LayoutSwitch
                {
                    EcosAddress = dto.EcosAddress,
                    PhysicalAddress = dto.PhysicalAddress,
                    Inverted = dto.Inverted,
                    DefaultPosition = defaultPosition
                });
            }

            return result;
        }

        private static List<LayoutBlock> BuildBlocks(List<BlockDto>? blockDtos, HashSet<int> sectionIds, List<string> errors)
        {
            var result = new List<LayoutBlock>();
            if (blockDtos is null)
            {
                return result;
            }

            var seen = new HashSet<int>();
            foreach (var dto in blockDtos)
            {
                if (!seen.Add(dto.Id))
                {
                    errors.Add($"Duplicate block id {dto.Id}.");
                    continue;
                }

                var sectionIdsInBlock = dto.SectionIds ?? new List<int>();
                var ok = true;
                foreach (var sectionId in sectionIdsInBlock)
                {
                    if (!sectionIds.Contains(sectionId))
                    {
                        errors.Add($"Block {dto.Id} references unknown section {sectionId}.");
                        ok = false;
                    }
                }

                if (!ok)
                {
                    continue;
                }

                result.Add(new LayoutBlock
                {
                    Id = dto.Id,
                    SectionIds = sectionIdsInBlock.ToArray()
                });
            }

            return result;
        }

        private static List<LayoutRoute> BuildRoutes(
            List<RouteDto>? routeDtos,
            HashSet<int> blockIds,
            HashSet<int> switchEcos,
            List<string> errors)
        {
            var result = new List<LayoutRoute>();
            if (routeDtos is null)
            {
                return result;
            }

            foreach (var dto in routeDtos)
            {
                if (!blockIds.Contains(dto.FromBlock))
                {
                    errors.Add($"Route {dto.FromBlock}>{dto.ToBlock} references unknown from-block {dto.FromBlock}.");
                    continue;
                }

                if (!blockIds.Contains(dto.ToBlock))
                {
                    errors.Add($"Route {dto.FromBlock}>{dto.ToBlock} references unknown to-block {dto.ToBlock}.");
                    continue;
                }

                SwitchPosition? required = null;
                if (dto.SwitchId is int switchId)
                {
                    if (!switchEcos.Contains(switchId))
                    {
                        errors.Add($"Route {dto.FromBlock}>{dto.ToBlock} references unknown switch {switchId}.");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(dto.RequiredSwitchPosition) &&
                        !TryParsePosition(dto.RequiredSwitchPosition, out required))
                    {
                        errors.Add($"Route {dto.FromBlock}>{dto.ToBlock} has an invalid required switch position '{dto.RequiredSwitchPosition}'.");
                        continue;
                    }
                }

                result.Add(new LayoutRoute
                {
                    FromBlock = dto.FromBlock,
                    ToBlock = dto.ToBlock,
                    SwitchId = dto.SwitchId,
                    RequiredSwitchPosition = required,
                    AllowLookAhead = dto.AllowLookAhead
                });
            }

            return result;
        }

        private static List<LayoutLocomotive> BuildLocomotives(List<LocoDto>? locoDtos, HashSet<int> blockIds, List<string> errors)
        {
            var result = new List<LayoutLocomotive>();
            if (locoDtos is null)
            {
                return result;
            }

            var seen = new HashSet<int>();
            foreach (var dto in locoDtos)
            {
                if (dto.Address <= 0)
                {
                    errors.Add($"Locomotive address {dto.Address} is not valid.");
                    continue;
                }

                if (!seen.Add(dto.Address))
                {
                    errors.Add($"Duplicate locomotive address {dto.Address}.");
                    continue;
                }

                if (!blockIds.Contains(dto.InitialBlock))
                {
                    errors.Add($"Locomotive {dto.Address} references unknown initial block {dto.InitialBlock}.");
                    continue;
                }

                result.Add(new LayoutLocomotive
                {
                    Address = dto.Address,
                    InitialBlock = dto.InitialBlock
                });
            }

            return result;
        }

        private static List<byte> BuildDetectedSlaves(List<byte>? detectedSlaves, List<string> errors)
        {
            var result = new List<byte>();
            if (detectedSlaves is null || detectedSlaves.Count == 0)
            {
                errors.Add("Detected slaves must be non-empty.");
                return result;
            }

            var seen = new HashSet<byte>();
            foreach (var slave in detectedSlaves)
            {
                if (slave < TrackAmplifierAddress.MinTrackAmplifier || slave > MaxSlave)
                {
                    errors.Add($"Detected slave {slave} is outside the valid range 1..{MaxSlave}.");
                    continue;
                }

                if (!seen.Add(slave))
                {
                    errors.Add($"Duplicate detected slave {slave}.");
                    continue;
                }

                result.Add(slave);
            }

            return result;
        }

        /// <summary>
        /// Validates the logical-section -&gt; REAL-physical-amplifier binding. The WHOLE mapping being
        /// absent/empty is LEGAL (meaning "no physical binding declared"; Real mode stays
        /// fail-closed). When present, every problem is collected (never silent):
        /// (1) unknown section, (2) duplicate section, (3) physical amplifier outside the track
        /// amplifier range 1..50, (4) two sections sharing one physical amplifier, and
        /// (5) a declared mapping that does not cover every known section exactly once.
        /// </summary>
        private static List<LayoutPhysicalAmplifierBinding> BuildPhysicalAmplifierMapping(
            List<PhysicalAmplifierDto>? mappingDtos,
            HashSet<int> sectionIds,
            List<string> errors)
        {
            var result = new List<LayoutPhysicalAmplifierBinding>();
            if (mappingDtos is null || mappingDtos.Count == 0)
            {
                return result;
            }

            var seenSections = new HashSet<int>();
            var seenAmplifiers = new HashSet<int>();

            foreach (var dto in mappingDtos)
            {
                if (!sectionIds.Contains(dto.SectionId))
                {
                    errors.Add($"Physical amplifier mapping references unknown section {dto.SectionId}.");
                    continue;
                }

                if (!seenSections.Add(dto.SectionId))
                {
                    errors.Add($"Duplicate physical amplifier mapping for section {dto.SectionId}.");
                    continue;
                }

                if (dto.PhysicalAmplifier < TrackAmplifierAddress.MinTrackAmplifier ||
                    dto.PhysicalAmplifier > TrackAmplifierAddress.MaxTrackAmplifier)
                {
                    errors.Add(
                        $"Physical amplifier mapping for section {dto.SectionId} has an invalid amplifier " +
                        $"{dto.PhysicalAmplifier} (valid {TrackAmplifierAddress.MinTrackAmplifier}..{TrackAmplifierAddress.MaxTrackAmplifier}).");
                    continue;
                }

                if (!seenAmplifiers.Add(dto.PhysicalAmplifier))
                {
                    errors.Add($"Physical amplifier {dto.PhysicalAmplifier} is mapped to more than one section.");
                    continue;
                }

                result.Add(new LayoutPhysicalAmplifierBinding
                {
                    SectionId = dto.SectionId,
                    PhysicalAmplifier = dto.PhysicalAmplifier
                });
            }

            // A declared mapping must cover every known section exactly once.
            foreach (var sectionId in sectionIds)
            {
                if (!seenSections.Contains(sectionId))
                {
                    errors.Add($"Physical amplifier mapping is missing section {sectionId}.");
                }
            }

            return result;
        }

        /// <summary>
        /// Parses a bezetmelder name such as "1.03" (module 1, point 3) into its module and point.
        /// Module is 1..N; point is 1..<see cref="PointsPerModule"/>.
        /// </summary>
        public static bool TryParseBezetmelder(string? name, out int module, out int point)
        {
            module = 0;
            point = 0;

            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var parts = name.Split('.');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out module) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out point))
            {
                return false;
            }

            return module >= 1 && point >= 1 && point <= PointsPerModule;
        }

        private static bool TryParsePosition(string? text, out SwitchPosition? position)
        {
            position = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (text.Equals("straight", StringComparison.OrdinalIgnoreCase) || text.Equals("g", StringComparison.OrdinalIgnoreCase))
            {
                position = SwitchPosition.Straight;
                return true;
            }

            if (text.Equals("diverging", StringComparison.OrdinalIgnoreCase) || text.Equals("r", StringComparison.OrdinalIgnoreCase))
            {
                position = SwitchPosition.Diverging;
                return true;
            }

            if (text.Equals("keep", StringComparison.OrdinalIgnoreCase))
            {
                position = null;
                return true;
            }

            return false;
        }

        // ---------------------------------------------------------------------------------
        // JSON DTOs (camelCase on disk)
        // ---------------------------------------------------------------------------------

        private sealed class ProfileDto
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public List<byte>? DetectedSlaves { get; set; }
            public List<PhysicalAmplifierDto>? PhysicalAmplifierMapping { get; set; }
            public List<SectionDto>? Sections { get; set; }
            public List<BlockDto>? Blocks { get; set; }
            public List<SwitchDto>? Switches { get; set; }
            public List<RouteDto>? Routes { get; set; }
            public List<LocoDto>? Locomotives { get; set; }
        }

        private sealed class SectionDto
        {
            public int Id { get; set; }
            public int AmplifierSlave { get; set; }
            public List<string>? Bezetmelders { get; set; }
            public double LengthMm { get; set; }
        }

        private sealed class BlockDto
        {
            public int Id { get; set; }
            public List<int>? SectionIds { get; set; }
        }

        private sealed class PhysicalAmplifierDto
        {
            public int SectionId { get; set; }
            public int PhysicalAmplifier { get; set; }
        }

        private sealed class SwitchDto
        {
            public int EcosAddress { get; set; }
            public int PhysicalAddress { get; set; }
            public bool Inverted { get; set; }
            public string? DefaultPosition { get; set; }
        }

        private sealed class RouteDto
        {
            public int FromBlock { get; set; }
            public int ToBlock { get; set; }
            public int? SwitchId { get; set; }
            public string? RequiredSwitchPosition { get; set; }
            public bool AllowLookAhead { get; set; } = true;
        }

        private sealed class LocoDto
        {
            public int Address { get; set; }
            public int InitialBlock { get; set; }
        }
    }
}
