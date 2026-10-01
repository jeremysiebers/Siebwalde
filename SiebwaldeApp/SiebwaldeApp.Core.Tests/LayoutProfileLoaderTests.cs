using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class LayoutProfileLoaderTests
    {
        private const string ValidOvalJson = @"
{
  ""name"": ""Simple Loop"",
  ""description"": ""4 blocks, 4 sections, 4 slaves."",
  ""detectedSlaves"": [1,2,3,4],
  ""sections"": [
    { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [""1.01""], ""lengthMm"": 1000.0 },
    { ""id"": 2, ""amplifierSlave"": 2, ""bezetmelders"": [""1.02""], ""lengthMm"": 1000.0 },
    { ""id"": 3, ""amplifierSlave"": 3, ""bezetmelders"": [""1.03""], ""lengthMm"": 1000.0 },
    { ""id"": 4, ""amplifierSlave"": 4, ""bezetmelders"": [""1.04""], ""lengthMm"": 1000.0 }
  ],
  ""blocks"": [
    { ""id"": 1, ""sectionIds"": [1] },
    { ""id"": 2, ""sectionIds"": [2] },
    { ""id"": 3, ""sectionIds"": [3] },
    { ""id"": 4, ""sectionIds"": [4] }
  ],
  ""switches"": [],
  ""routes"": [
    { ""fromBlock"": 1, ""toBlock"": 2 },
    { ""fromBlock"": 2, ""toBlock"": 3 },
    { ""fromBlock"": 3, ""toBlock"": 4 },
    { ""fromBlock"": 4, ""toBlock"": 1 }
  ],
  ""locomotives"": [
    { ""address"": 1, ""initialBlock"": 1 },
    { ""address"": 2, ""initialBlock"": 3 }
  ]
}";

        private static string Json(string body)
            => body;

        // ---------------------------------------------------------------------------------
        // Valid profile
        // ---------------------------------------------------------------------------------

        [Fact]
        public void ValidOval_LoadsAndProjects()
        {
            Assert.True(LayoutProfileLoader.TryLoad(ValidOvalJson, out var profile, out var errors), string.Join("; ", errors));
            Assert.NotNull(profile);
            Assert.Empty(errors);

            Assert.Equal("Simple Loop", profile!.Name);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, profile.DetectedSlaves);
            Assert.Equal(4, profile.Sections.Count);
            Assert.Equal(4, profile.Blocks.Count);
            Assert.Equal(4, profile.Routes.Count);
            Assert.Equal(2, profile.Locomotives.Count);

            // Block topology: block 1 -> amplifier 1, route 1 -> 2.
            var topology = profile.ToBlockTopology();
            Assert.True(topology.TryGetAmplifiers(1, out var amps));
            Assert.Equal(new ushort[] { 1 }, amps);
            Assert.Contains(topology.Transitions, t => t.FromBlock == 1 && t.ToBlock == 2);

            // Koploper block map: block 1 -> bezetmelder 1.01 -> section 1.
            var blockMap = profile.ToKoploperBlockMap();
            Assert.True(blockMap.TryGetByBlock(1, out var block));
            Assert.Equal(new[] { "1.01" }, block.Bezetmelders);
            Assert.Equal(new ushort[] { 1 }, block.AmplifierSections);

            // Grouping is empty (not inferred from the profile).
            var groups = profile.ToTrackAmplifierGroups();
            Assert.Empty(groups.AllConfigured);
        }

        [Fact]
        public void SimpleLoopFile_IsCopiedToOutput_AndLoads()
        {
            Assert.True(
                LayoutProfileLoader.TryLoadFromFile(LayoutProfileLoader.SimpleLoopPath, out var profile, out var errors),
                string.Join("; ", errors));
            Assert.NotNull(profile);
            Assert.Equal("Simple Loop", profile!.Name);
            Assert.Equal(4, profile.Blocks.Count);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, profile.DetectedSlaves);

            // Locomotives use DCC decoder addresses 1 and 2 (not ECoS object IDs).
            Assert.Equal(1, profile.Locomotives[0].Address);
            Assert.Equal(2, profile.Locomotives[1].Address);

            // One bezetmelder per section (4 total) -- the Simple Loop single-melder rule.
            Assert.Equal(4, profile.Sections.SelectMany(s => s.Bezetmelders).Count());

            // Physical domain resolves to the prototype amplifiers 1/3/4/6.
            Assert.Equal(new byte[] { 1, 3, 4, 6 }, profile.ToPhysicalDomain());
        }

        [Fact]
        public void SimpleLoopProfile_MatchesKoploperReference()
        {
            Assert.True(
                LayoutProfileLoader.TryLoadFromFile(LayoutProfileLoader.SimpleLoopPath, out var profile, out var errors),
                string.Join("; ", errors));
            Assert.NotNull(profile);

            // 4 sections, 4 blocks, no switches.
            Assert.Equal(4, profile!.Sections.Count);
            Assert.Equal(4, profile.Blocks.Count);
            Assert.Empty(profile.Switches);

            // Closed chain 1 -> 2 -> 3 -> 4 -> 1.
            Assert.Equal(
                new[] { (1, 2), (2, 3), (3, 4), (4, 1) },
                profile.Routes.Select(r => (r.FromBlock, r.ToBlock)).ToArray());

            // ONE bezetmelder per section, mapping to exactly 1.01/1.02/1.03/1.04.
            Assert.Equal(
                new[] { "1.01", "1.02", "1.03", "1.04" },
                profile.Sections.SelectMany(s => s.Bezetmelders).ToArray());

            // Locomotives use DCC decoder addresses [1, 2] with initial blocks [1, 3].
            Assert.Equal(new[] { 1, 2 }, profile.Locomotives.Select(l => l.Address).ToArray());
            Assert.Equal(new[] { 1, 3 }, profile.Locomotives.Select(l => l.InitialBlock).ToArray());

            // Physical domain resolves to the prototype amplifiers 1/3/4/6.
            Assert.Equal(new byte[] { 1, 3, 4, 6 }, profile.ToPhysicalDomain());
        }

        [Fact]
        public void GetAvailableProfileNames_ContainsBothRepositoryProfiles()
        {
            var names = LayoutProfileLoader.GetAvailableProfileNames();
            Assert.Contains("Simple Loop", names);
            Assert.Contains("Koploper Oval", names);
        }

        [Fact]
        public void TryLoadByName_SimpleLoop_Loads()
        {
            Assert.True(
                LayoutProfileLoader.TryLoadByName("Simple Loop", out var profile, out var errors),
                string.Join("; ", errors));
            Assert.NotNull(profile);
            Assert.Equal(4, profile!.Blocks.Count);
        }

        [Fact]
        public void TryLoadByName_KoploperOval_LoadsAndProjects()
        {
            Assert.True(
                LayoutProfileLoader.TryLoadByName("Koploper Oval", out var profile, out var errors),
                string.Join("; ", errors));
            Assert.NotNull(profile);
            Assert.Equal("Koploper Oval", profile!.Name);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, profile.DetectedSlaves);
            Assert.Equal(5, profile.Sections.Count);
            Assert.Equal(5, profile.Blocks.Count);
            Assert.Equal(2, profile.Switches.Count);
            Assert.Equal(6, profile.Routes.Count);
            Assert.Equal(2, profile.Locomotives.Count);
            Assert.Equal(1, profile.Locomotives[0].Address);
            Assert.Equal(2, profile.Locomotives[1].Address);

            // 10 bezetmelders, two per block (1.01 .. 1.10).
            Assert.Equal(10, profile.Sections.SelectMany(s => s.Bezetmelders).Count());
            Assert.Equal(new[] { "1.09", "1.10" }, profile.TryGetSection(5)!.Bezetmelders);

            // Block topology: block 3 has the switch-conditional branch 3>4@1:0 and 3>5@1:1.
            var topology = profile.ToBlockTopology();
            var from3 = topology.GetTransitionsFrom(3);
            Assert.Contains(from3, t => t.ToBlock == 4 && t.SwitchId == 1 && t.RequiredSwitchPosition == SwitchPosition.Straight);
            Assert.Contains(from3, t => t.ToBlock == 5 && t.SwitchId == 1 && t.RequiredSwitchPosition == SwitchPosition.Diverging);

            // Switch mapping projects both switches.
            var switchMapping = profile.ToSwitchMapping();
            Assert.True(switchMapping.IsMapped(1));
            Assert.True(switchMapping.IsMapped(2));
        }

        [Fact]
        public void TryLoadByName_UnknownName_Fails()
        {
            Assert.False(LayoutProfileLoader.TryLoadByName("Not A Layout", out var profile, out var errors));
            Assert.Null(profile);
            Assert.NotEmpty(errors);
        }

        // ---------------------------------------------------------------------------------
        // Validation errors (each one caught)
        // ---------------------------------------------------------------------------------

        [Fact]
        public void BadSectionId_IsRejected()
        {
            var json = Json(@"{ ""detectedSlaves"": [1], ""sections"": [ { ""id"": 51, ""amplifierSlave"": 51, ""bezetmelders"": [], ""lengthMm"": 100 } ], ""blocks"": [], ""routes"": [], ""locomotives"": [] }");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("Section id 51"));
        }

        [Fact]
        public void UnknownBlockSection_IsRejected()
        {
            var json = Json(@"{ ""detectedSlaves"": [1], ""sections"": [ { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [], ""lengthMm"": 100 } ], ""blocks"": [ { ""id"": 1, ""sectionIds"": [99] } ], ""routes"": [], ""locomotives"": [] }");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("references unknown section 99"));
        }

        [Fact]
        public void DanglingRoute_IsRejected()
        {
            var json = Json(@"{ ""detectedSlaves"": [1], ""sections"": [ { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [], ""lengthMm"": 100 } ], ""blocks"": [ { ""id"": 1, ""sectionIds"": [1] } ], ""routes"": [ { ""fromBlock"": 1, ""toBlock"": 99 } ], ""locomotives"": [] }");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("unknown to-block 99"));
        }

        [Fact]
        public void BadBezetmelder_IsRejected()
        {
            var json = Json(@"{ ""detectedSlaves"": [1], ""sections"": [ { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [""1.17""], ""lengthMm"": 100 } ], ""blocks"": [ { ""id"": 1, ""sectionIds"": [1] } ], ""routes"": [], ""locomotives"": [] }");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("invalid bezetmelder name '1.17'"));
        }

        [Fact]
        public void EmptyDetectedSlaves_IsRejected()
        {
            var json = Json(@"{ ""detectedSlaves"": [], ""sections"": [ { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [], ""lengthMm"": 100 } ], ""blocks"": [ { ""id"": 1, ""sectionIds"": [1] } ], ""routes"": [], ""locomotives"": [] }");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("Detected slaves must be non-empty"));
        }

        [Fact]
        public void UnknownInitialBlock_IsRejected()
        {
            var json = Json(@"{ ""detectedSlaves"": [1], ""sections"": [ { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [], ""lengthMm"": 100 } ], ""blocks"": [ { ""id"": 1, ""sectionIds"": [1] } ], ""routes"": [], ""locomotives"": [ { ""address"": 1000, ""initialBlock"": 99 } ] }");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("unknown initial block 99"));
        }

        [Fact]
        public void DuplicateSectionId_IsRejected()
        {
            var json = Json(@"{ ""detectedSlaves"": [1], ""sections"": [ { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [], ""lengthMm"": 100 }, { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [], ""lengthMm"": 100 } ], ""blocks"": [], ""routes"": [], ""locomotives"": [] }");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("Duplicate section id 1"));
        }

        [Fact]
        public void MalformedJson_IsRejected()
        {
            Assert.False(LayoutProfileLoader.TryLoad("{ not json", out _, out var errors));
            Assert.NotEmpty(errors);
        }

        // ---------------------------------------------------------------------------------
        // Physical amplifier mapping (logical section -> REAL physical amplifier)
        // ---------------------------------------------------------------------------------

        private const string PhysicalBindingOvalJson = @"
{
  ""name"": ""Simple Loop"",
  ""description"": ""4 blocks, 4 sections, physical 1/3/4/6."",
  ""detectedSlaves"": [1,2,3,4],
  ""physicalAmplifierMapping"": [
    { ""sectionId"": 1, ""physicalAmplifier"": 1 },
    { ""sectionId"": 2, ""physicalAmplifier"": 3 },
    { ""sectionId"": 3, ""physicalAmplifier"": 4 },
    { ""sectionId"": 4, ""physicalAmplifier"": 6 }
  ],
  ""sections"": [
    { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [""1.01""], ""lengthMm"": 1000.0 },
    { ""id"": 2, ""amplifierSlave"": 2, ""bezetmelders"": [""1.02""], ""lengthMm"": 1000.0 },
    { ""id"": 3, ""amplifierSlave"": 3, ""bezetmelders"": [""1.03""], ""lengthMm"": 1000.0 },
    { ""id"": 4, ""amplifierSlave"": 4, ""bezetmelders"": [""1.04""], ""lengthMm"": 1000.0 }
  ],
  ""blocks"": [
    { ""id"": 1, ""sectionIds"": [1] },
    { ""id"": 2, ""sectionIds"": [2] },
    { ""id"": 3, ""sectionIds"": [3] },
    { ""id"": 4, ""sectionIds"": [4] }
  ],
  ""switches"": [],
  ""routes"": [
    { ""fromBlock"": 1, ""toBlock"": 2 },
    { ""fromBlock"": 2, ""toBlock"": 3 },
    { ""fromBlock"": 3, ""toBlock"": 4 },
    { ""fromBlock"": 4, ""toBlock"": 1 }
  ],
  ""locomotives"": [
    { ""address"": 1, ""initialBlock"": 1 },
    { ""address"": 2, ""initialBlock"": 3 }
  ]
}";

        /// <summary>A minimal valid two-section profile with a caller-supplied physical mapping array.</summary>
        private static string TwoSectionProfileWithMapping(string physicalAmplifierMapping)
            => @"
{
  ""name"": ""Two Sections"",
  ""detectedSlaves"": [1,2],
  ""sections"": [
    { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [], ""lengthMm"": 100 },
    { ""id"": 2, ""amplifierSlave"": 2, ""bezetmelders"": [], ""lengthMm"": 100 }
  ],
  ""blocks"": [
    { ""id"": 1, ""sectionIds"": [1] },
    { ""id"": 2, ""sectionIds"": [2] }
  ],
  ""switches"": [],
  ""routes"": [],
  ""locomotives"": [],
  ""physicalAmplifierMapping"": " + physicalAmplifierMapping + @"
}";

        [Fact]
        public void PhysicalBindingMapping_ProjectsRealDomain_AndKeepsLogicalProjectionIntact()
        {
            Assert.True(LayoutProfileLoader.TryLoad(PhysicalBindingOvalJson, out var profile, out var errors), string.Join("; ", errors));
            Assert.NotNull(profile);

            // Physical domain is deduplicated and sorted.
            Assert.Equal(new byte[] { 1, 3, 4, 6 }, profile!.ToPhysicalDomain());

            // Real block topology: block -> physical amplifier 1/3/4/6.
            var realTopology = profile.ToRealBlockTopology();
            Assert.True(realTopology.TryGetAmplifiers(1, out var block1));
            Assert.Equal(new ushort[] { 1 }, block1);
            Assert.True(realTopology.TryGetAmplifiers(2, out var block2));
            Assert.Equal(new ushort[] { 3 }, block2);
            Assert.True(realTopology.TryGetAmplifiers(3, out var block3));
            Assert.Equal(new ushort[] { 4 }, block3);
            Assert.True(realTopology.TryGetAmplifiers(4, out var block4));
            Assert.Equal(new ushort[] { 6 }, block4);

            // Real Koploper block map: physical sections.
            var realBlockMap = profile.ToRealKoploperBlockMap();
            Assert.True(realBlockMap.TryGetByBlock(1, out var realBlock1));
            Assert.Equal(new ushort[] { 1 }, realBlock1.AmplifierSections);
            Assert.True(realBlockMap.TryGetByBlock(2, out var realBlock2));
            Assert.Equal(new ushort[] { 3 }, realBlock2.AmplifierSections);
            Assert.True(realBlockMap.TryGetByBlock(3, out var realBlock3));
            Assert.Equal(new ushort[] { 4 }, realBlock3.AmplifierSections);
            Assert.True(realBlockMap.TryGetByBlock(4, out var realBlock4));
            Assert.Equal(new ushort[] { 6 }, realBlock4.AmplifierSections);

            // The logical/simulated projection is unchanged: block -> amplifierSlave 1..4.
            var topology = profile.ToBlockTopology();
            Assert.True(topology.TryGetAmplifiers(1, out var logicalBlock1));
            Assert.Equal(new ushort[] { 1 }, logicalBlock1);
            Assert.True(topology.TryGetAmplifiers(2, out var logicalBlock2));
            Assert.Equal(new ushort[] { 2 }, logicalBlock2);
            Assert.True(topology.TryGetAmplifiers(3, out var logicalBlock3));
            Assert.Equal(new ushort[] { 3 }, logicalBlock3);
            Assert.True(topology.TryGetAmplifiers(4, out var logicalBlock4));
            Assert.Equal(new ushort[] { 4 }, logicalBlock4);
        }

        [Fact]
        public void AbsentPhysicalMapping_IsLegal_AndRealProjectionsAreEmpty()
        {
            // ValidOvalJson declares no physicalAmplifierMapping, which is legal.
            Assert.True(LayoutProfileLoader.TryLoad(ValidOvalJson, out var profile, out var errors), string.Join("; ", errors));
            Assert.NotNull(profile);

            Assert.Empty(profile!.PhysicalAmplifierMapping);
            Assert.Empty(profile.ToPhysicalDomain());

            var realTopology = profile.ToRealBlockTopology();
            Assert.Empty(realTopology.Blocks);

            var realBlockMap = profile.ToRealKoploperBlockMap();
            Assert.Empty(realBlockMap.Blocks);
        }

        [Fact]
        public void PhysicalMapping_UnknownSection_IsRejected()
        {
            var json = TwoSectionProfileWithMapping("[ { \"sectionId\": 99, \"physicalAmplifier\": 1 }, { \"sectionId\": 2, \"physicalAmplifier\": 2 } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("unknown section 99"));
        }

        [Fact]
        public void PhysicalMapping_DuplicateSection_IsRejected()
        {
            var json = TwoSectionProfileWithMapping("[ { \"sectionId\": 1, \"physicalAmplifier\": 1 }, { \"sectionId\": 1, \"physicalAmplifier\": 3 }, { \"sectionId\": 2, \"physicalAmplifier\": 2 } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("Duplicate physical amplifier mapping for section 1"));
        }

        [Fact]
        public void PhysicalMapping_DuplicatePhysicalAmplifier_IsRejected()
        {
            var json = TwoSectionProfileWithMapping("[ { \"sectionId\": 1, \"physicalAmplifier\": 1 }, { \"sectionId\": 2, \"physicalAmplifier\": 1 } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("Physical amplifier 1 is mapped to more than one section"));
        }

        [Fact]
        public void PhysicalMapping_OutOfRangeAmplifier_IsRejected()
        {
            var json = TwoSectionProfileWithMapping("[ { \"sectionId\": 1, \"physicalAmplifier\": 51 }, { \"sectionId\": 2, \"physicalAmplifier\": 2 } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("invalid amplifier 51"));
        }

        [Fact]
        public void PhysicalMapping_IncompleteCoverage_IsRejected()
        {
            var json = TwoSectionProfileWithMapping("[ { \"sectionId\": 1, \"physicalAmplifier\": 1 } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("missing section 2"));
        }
    }
}
