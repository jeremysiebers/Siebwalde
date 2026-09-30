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
    { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [""1.01"",""1.02""], ""lengthMm"": 1000.0 },
    { ""id"": 2, ""amplifierSlave"": 2, ""bezetmelders"": [""1.03"",""1.04""], ""lengthMm"": 1000.0 },
    { ""id"": 3, ""amplifierSlave"": 3, ""bezetmelders"": [""1.05"",""1.06""], ""lengthMm"": 1000.0 },
    { ""id"": 4, ""amplifierSlave"": 4, ""bezetmelders"": [""1.07"",""1.08""], ""lengthMm"": 1000.0 }
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
    { ""address"": 1000, ""initialBlock"": 1 },
    { ""address"": 1001, ""initialBlock"": 3 }
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

            // Koploper block map: block 1 -> bezetmelders 1.01+1.02 -> section 1.
            var blockMap = profile.ToKoploperBlockMap();
            Assert.True(blockMap.TryGetByBlock(1, out var block));
            Assert.Equal(new[] { "1.01", "1.02" }, block.Bezetmelders);
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
    }
}
