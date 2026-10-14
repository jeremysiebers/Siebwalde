using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// KIS10-01 schema/config layer tests: the Koploper block -&gt; N sections generalization, the
    /// legacy + new JSON normalization, the added fail-closed validations (empty drive group,
    /// one-amp-in-two-blocks defensive scan is a no-op under current rules), and the Simple Loop
    /// N=1 migration regression.
    /// </summary>
    public class Kis10SchemaGeneralizationTests
    {
        private static string Section(int id)
            => $"{{ \"id\": {id}, \"amplifierSlave\": {id}, \"bezetmelders\": [\"1.{id:00}\"], \"lengthMm\": 100 }}";

        private static string ProfileJson(
            string sections,
            string physicalAmplifierMapping,
            string koploperBlockBindings)
            => @"
{
  ""name"": ""KIS10 Test"",
  ""detectedSlaves"": [1, 2, 3, 4, 5, 6],
  ""physicalAmplifierMapping"": " + physicalAmplifierMapping + @",
  ""koploperBlockBindings"": " + koploperBlockBindings + @",
  ""sections"": [ " + sections + @" ],
  ""blocks"": [],
  ""switches"": [],
  ""routes"": [],
  ""locomotives"": []
}";

        [Fact]
        public void OneBlock_OneSection_Loads()
        {
            var json = ProfileJson(
                Section(1),
                "[ { \"sectionId\": 1, \"physicalAmplifier\": 1 } ]",
                "[ { \"koploperInternalBlockId\": 10, \"sectionIds\": [1] } ]");

            Assert.True(LayoutProfileLoader.TryLoad(json, out var profile, out var errors), string.Join("; ", errors));
            Assert.NotNull(profile);

            Assert.Single(profile!.KoploperBlockBindings);
            Assert.Equal(new[] { 1 }, profile.KoploperBlockBindings[0].SectionIds);
            Assert.Equal(1, profile.ToSingleSectionKoploperBlockBindingMap()[10]);
        }

        [Fact]
        public void OneBlock_TwoSections_Loads()
        {
            var json = ProfileJson(
                Section(1) + ", " + Section(2),
                "[ { \"sectionId\": 1, \"physicalAmplifier\": 1 }, { \"sectionId\": 2, \"physicalAmplifier\": 2 } ]",
                "[ { \"koploperInternalBlockId\": 10, \"sectionIds\": [1, 2] } ]");

            Assert.True(LayoutProfileLoader.TryLoad(json, out var profile, out var errors), string.Join("; ", errors));
            Assert.NotNull(profile);

            Assert.Single(profile!.KoploperBlockBindings);
            Assert.Equal(new[] { 1, 2 }, profile.KoploperBlockBindings[0].SectionIds);
            Assert.Equal(new[] { 1, 2 }, profile.ToKoploperBlockBindingMap()[10]);
        }

        [Fact]
        public void OneBlock_ThreeSections_Loads()
        {
            var json = ProfileJson(
                Section(1) + ", " + Section(2) + ", " + Section(3),
                "[ { \"sectionId\": 1, \"physicalAmplifier\": 1 }, { \"sectionId\": 2, \"physicalAmplifier\": 2 }, { \"sectionId\": 3, \"physicalAmplifier\": 3 } ]",
                "[ { \"koploperInternalBlockId\": 10, \"sectionIds\": [1, 2, 3] } ]");

            Assert.True(LayoutProfileLoader.TryLoad(json, out var profile, out var errors), string.Join("; ", errors));
            Assert.NotNull(profile);

            Assert.Single(profile!.KoploperBlockBindings);
            Assert.Equal(new[] { 1, 2, 3 }, profile.KoploperBlockBindings[0].SectionIds);
            Assert.Equal(new[] { 1, 2, 3 }, profile.ToKoploperBlockBindingMap()[10]);
        }

        [Fact]
        public void DuplicatePhysicalAmplifier_Fails()
        {
            var json = ProfileJson(
                Section(1) + ", " + Section(2),
                "[ { \"sectionId\": 1, \"physicalAmplifier\": 1 }, { \"sectionId\": 2, \"physicalAmplifier\": 1 } ]",
                "[ { \"koploperInternalBlockId\": 10, \"sectionIds\": [1, 2] } ]");

            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("Physical amplifier 1 is mapped to more than one section"));
        }

        [Fact]
        public void DuplicateSectionIdentity_Fails()
        {
            var json = ProfileJson(
                Section(1) + ", " + Section(2),
                "[ { \"sectionId\": 1, \"physicalAmplifier\": 1 }, { \"sectionId\": 2, \"physicalAmplifier\": 2 } ]",
                "[ { \"koploperInternalBlockId\": 10, \"sectionIds\": [1] }, { \"koploperInternalBlockId\": 11, \"sectionIds\": [1] } ]");

            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("Logical section 1 is bound to more than one Koploper internal block"));
        }

        [Fact]
        public void InvalidFeedbackMapping_Fails()
        {
            var json = @"
{
  ""name"": ""KIS10 Test"",
  ""detectedSlaves"": [1],
  ""physicalAmplifierMapping"": [ { ""sectionId"": 1, ""physicalAmplifier"": 1 } ],
  ""koploperBlockBindings"": [ { ""koploperInternalBlockId"": 10, ""sectionIds"": [1] } ],
  ""sections"": [ { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [""1.99""], ""lengthMm"": 100 } ],
  ""blocks"": [],
  ""switches"": [],
  ""routes"": [],
  ""locomotives"": []
}";

            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("invalid bezetmelder name '1.99'"));
        }

        [Fact]
        public void BackplaneAmplifier_FailsAsTrackAmp()
        {
            var json = ProfileJson(
                Section(1),
                "[ { \"sectionId\": 1, \"physicalAmplifier\": 51 } ]",
                "[ { \"koploperInternalBlockId\": 10, \"sectionIds\": [1] } ]");

            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("invalid amplifier 51"));
        }

        [Fact]
        public void EmptyDriveGroup_Fails()
        {
            // A binding whose section has no physical amplifier binding is an empty drive group.
            var json = ProfileJson(
                Section(1),
                "[]",
                "[ { \"koploperInternalBlockId\": 10, \"sectionIds\": [1] } ]");

            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("empty drive group"));
        }

        [Fact]
        public void PartialDriveGroup_Fails()
        {
            // A block where ONE bound section has a physical amplifier but ANOTHER does not is a
            // partial drive group and must be rejected (Architect F2).
            var json = ProfileJson(
                Section(1) + ", " + Section(2),
                "[ { \"sectionId\": 1, \"physicalAmplifier\": 1 } ]",   // section 2 has no amplifier
                "[ { \"koploperInternalBlockId\": 10, \"sectionIds\": [1, 2] } ]");

            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("partial drive group"));
            Assert.Contains(errors, e => e.Contains("section(s) 2"));
        }

        [Fact]
        public void InvalidLogicalBlockBinding_Fails()
        {
            var json = ProfileJson(
                Section(1),
                "[ { \"sectionId\": 1, \"physicalAmplifier\": 1 } ]",
                "[ { \"koploperInternalBlockId\": 0, \"sectionIds\": [1] } ]");

            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("invalid internal block id 0"));
        }

        [Fact]
        public void SimpleLoopProfile_Migrates_AndProducesSamePhysicalBehavior()
        {
            Assert.True(
                LayoutProfileLoader.TryLoadFromFile(LayoutProfileLoader.SimpleLoopPath, out var profile, out var errors),
                string.Join("; ", errors));
            Assert.NotNull(profile);

            // Canonical 1:N map: internal block -> [section] (unchanged from the old 1:1 map for N=1).
            var map = profile!.ToKoploperBlockBindingMap();
            Assert.Equal(4, map.Count);
            Assert.Equal(new[] { 1 }, map[1]);
            Assert.Equal(new[] { 2 }, map[2]);
            Assert.Equal(new[] { 3 }, map[3]);
            Assert.Equal(new[] { 4 }, map[4]);

            // N=1 bridge map is identical to the old 1:1 map (the shadow/carrier keep working).
            var single = profile.ToSingleSectionKoploperBlockBindingMap();
            Assert.Equal(4, single.Count);
            Assert.Equal(1, single[1]);
            Assert.Equal(2, single[2]);
            Assert.Equal(3, single[3]);
            Assert.Equal(4, single[4]);

            // Physical domain unchanged: prototype amplifiers 1/3/4/6.
            Assert.Equal(new byte[] { 1, 3, 4, 6 }, profile.ToPhysicalDomain());

            // Real block topology projection unchanged.
            var realTopology = profile.ToRealBlockTopology();
            Assert.True(realTopology.TryGetAmplifiers(1, out var block1));
            Assert.Equal(new ushort[] { 1 }, block1);
            Assert.True(realTopology.TryGetAmplifiers(2, out var block2));
            Assert.Equal(new ushort[] { 3 }, block2);
            Assert.True(realTopology.TryGetAmplifiers(3, out var block3));
            Assert.Equal(new ushort[] { 4 }, block3);
            Assert.True(realTopology.TryGetAmplifiers(4, out var block4));
            Assert.Equal(new ushort[] { 6 }, block4);
        }
    }
}
