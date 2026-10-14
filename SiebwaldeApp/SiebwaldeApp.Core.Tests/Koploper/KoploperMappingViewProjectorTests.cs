using System.Linq;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// KIS10-08 read-only mapping view: the pure <see cref="KoploperMappingViewProjector"/> renders
    /// block -&gt; sections -&gt; amplifier -&gt; bezetmelder and surfaces validation conflicts, without
    /// touching any runtime authority (it only reads the <see cref="LayoutProfile"/>).
    /// </summary>
    public class KoploperMappingViewProjectorTests
    {
        private static string Section(int id, string bezetmelder)
            => $"{{ \"id\": {id}, \"amplifierSlave\": {id}, \"bezetmelders\": [\"{bezetmelder}\"], \"lengthMm\": 100 }}";

        private static string ProfileJson(
            string name,
            string physicalAmplifierMapping,
            string koploperBlockBindings,
            string sections)
            => @"
{
  ""name"": """ + name + @""",
  ""detectedSlaves"": [1, 2, 3, 4],
  ""physicalAmplifierMapping"": " + physicalAmplifierMapping + @",
  ""koploperBlockBindings"": " + koploperBlockBindings + @",
  ""sections"": [ " + sections + @" ],
  ""blocks"": [],
  ""switches"": [],
  ""routes"": [],
  ""locomotives"": []
}";

        [Fact]
        public void Build_MultiSectionAndOneSectionBlocks_RendersBlockSectionsAmpBezetmelder()
        {
            var json = ProfileJson(
                "KIS10 Mapping",
                "[ { \"sectionId\": 1, \"physicalAmplifier\": 1 }, { \"sectionId\": 2, \"physicalAmplifier\": 2 }, { \"sectionId\": 3, \"physicalAmplifier\": 4 } ]",
                "[ { \"koploperInternalBlockId\": 10, \"sectionIds\": [1, 2] }, { \"koploperInternalBlockId\": 20, \"sectionIds\": [3] } ]",
                Section(1, "1.01") + ", " + Section(2, "1.02") + ", " + Section(3, "1.03"));

            Assert.True(LayoutProfileLoader.TryLoad(json, out var profile, out var errors), string.Join("; ", errors));
            var view = KoploperMappingViewProjector.Build(profile!);

            Assert.Equal("KIS10 Mapping", view.ProfileName);
            Assert.False(view.HasValidationIssues);
            Assert.Equal(2, view.Blocks.Count);

            // Multi-section block: two sections with their own amp + bezetmelder.
            var block10 = view.Blocks.Single(b => b.BlockNumber == 10);
            Assert.Equal(2, block10.Sections.Count);
            Assert.Equal(1, block10.Sections[0].SectionId);
            Assert.Equal(1, block10.Sections[0].Amplifier);
            Assert.Equal(new[] { "1.01" }, block10.Sections[0].Bezetmelders);
            Assert.Equal(2, block10.Sections[1].SectionId);
            Assert.Equal(2, block10.Sections[1].Amplifier);
            Assert.Equal(new[] { "1.02" }, block10.Sections[1].Bezetmelders);

            // One-section block.
            var block20 = view.Blocks.Single(b => b.BlockNumber == 20);
            var section = Assert.Single(block20.Sections);
            Assert.Equal(3, section.SectionId);
            Assert.Equal(4, section.Amplifier);
            Assert.Equal(new[] { "1.03" }, section.Bezetmelders);
        }

        [Fact]
        public void Build_ConflictProfile_SurfacesValidationIssue()
        {
            // One physical amplifier bound to two sections is a conflict.
            var profile = new LayoutProfile
            {
                Name = "Conflict Profile",
                PhysicalAmplifierMapping = new[]
                {
                    new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 },
                    new LayoutPhysicalAmplifierBinding { SectionId = 2, PhysicalAmplifier = 1 }
                },
                KoploperBlockBindings = new[]
                {
                    new KoploperBlockBinding(10, new[] { 1, 2 })
                },
                Sections = new[]
                {
                    new LayoutSection { Id = 1, AmplifierSlave = 1, Bezetmelders = new[] { "1.01" } },
                    new LayoutSection { Id = 2, AmplifierSlave = 2, Bezetmelders = new[] { "1.02" } }
                }
            };

            var view = KoploperMappingViewProjector.Build(profile);

            Assert.True(view.HasValidationIssues);
            Assert.Contains(view.ValidationIssues, i => i.Contains("physical amplifier 1") && i.Contains("multiple sections"));
        }

        [Fact]
        public void Build_NoBlockBindings_SurfacesMissingMapping()
        {
            var profile = new LayoutProfile
            {
                Name = "Empty",
                PhysicalAmplifierMapping = new[]
                {
                    new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 }
                },
                Sections = new[]
                {
                    new LayoutSection { Id = 1, AmplifierSlave = 1, Bezetmelders = new[] { "1.01" } }
                }
            };

            var view = KoploperMappingViewProjector.Build(profile);

            Assert.True(view.HasValidationIssues);
            Assert.Contains(view.ValidationIssues, i => i.Contains("no Koploper block bindings"));
        }

        [Fact]
        public void ToDisplayText_RendersTheReadOnlyMapping()
        {
            var json = ProfileJson(
                "KIS10 Mapping",
                "[ { \"sectionId\": 1, \"physicalAmplifier\": 1 }, { \"sectionId\": 2, \"physicalAmplifier\": 2 } ]",
                "[ { \"koploperInternalBlockId\": 10, \"sectionIds\": [1, 2] } ]",
                Section(1, "1.01") + ", " + Section(2, "1.02"));

            Assert.True(LayoutProfileLoader.TryLoad(json, out var profile, out var errors), string.Join("; ", errors));
            var text = KoploperMappingViewProjector.Build(profile!).ToDisplayText();

            Assert.Contains("KIS10 Mapping", text);
            Assert.Contains("block 10", text);
            Assert.Contains("section 1 -> amp 1 -> bezetmelders [1.01]", text);
            Assert.Contains("section 2 -> amp 2 -> bezetmelders [1.02]", text);
            Assert.Contains("validation: ok", text);
        }
    }
}
