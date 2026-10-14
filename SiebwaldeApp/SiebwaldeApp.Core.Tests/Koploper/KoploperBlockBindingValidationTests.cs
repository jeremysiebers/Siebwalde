using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for the Koploper internal block -&gt; logical section binding validation in
    /// <see cref="LayoutProfileLoader"/>: invalid/duplicate internal ids, unknown/duplicate
    /// sections, and that an absent/empty binding is legal.
    /// </summary>
    public class KoploperBlockBindingValidationTests
    {
        private const string BaseProfileJson = @"
{
  ""name"": ""Binding Test"",
  ""detectedSlaves"": [1],
  ""sections"": [
    { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [], ""lengthMm"": 100 },
    { ""id"": 2, ""amplifierSlave"": 2, ""bezetmelders"": [], ""lengthMm"": 100 }
  ],
  ""blocks"": [],
  ""switches"": [],
  ""routes"": [],
  ""locomotives"": []
}";

        private static string WithBindings(string bindings)
            => @"
{
  ""name"": ""Binding Test"",
  ""detectedSlaves"": [1],
  ""physicalAmplifierMapping"": [
    { ""sectionId"": 1, ""physicalAmplifier"": 1 },
    { ""sectionId"": 2, ""physicalAmplifier"": 2 }
  ],
  ""sections"": [
    { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [], ""lengthMm"": 100 },
    { ""id"": 2, ""amplifierSlave"": 2, ""bezetmelders"": [], ""lengthMm"": 100 }
  ],
  ""blocks"": [],
  ""switches"": [],
  ""routes"": [],
  ""locomotives"": [],
  ""koploperBlockBindings"": " + bindings + @"
}";

        [Fact]
        public void AbsentBindings_IsLegal()
        {
            Assert.True(LayoutProfileLoader.TryLoad(BaseProfileJson, out var profile, out var errors), string.Join("; ", errors));
            Assert.NotNull(profile);
            Assert.Empty(profile!.KoploperBlockBindings);
        }

        [Fact]
        public void EmptyBindings_IsLegal()
        {
            var json = WithBindings("[]");
            Assert.True(LayoutProfileLoader.TryLoad(json, out var profile, out var errors), string.Join("; ", errors));
            Assert.NotNull(profile);
            Assert.Empty(profile!.KoploperBlockBindings);
        }

        [Fact]
        public void ValidBindings_LoadAndPreserve()
        {
            var json = WithBindings(@"[ { ""koploperInternalBlockId"": 30, ""sectionIds"": [1] }, { ""koploperInternalBlockId"": 22, ""sectionIds"": [2] } ]");
            Assert.True(LayoutProfileLoader.TryLoad(json, out var profile, out var errors), string.Join("; ", errors));
            Assert.NotNull(profile);

            Assert.Equal(2, profile!.KoploperBlockBindings.Count);
            Assert.Equal(30, profile.KoploperBlockBindings[0].KoploperInternalBlockId);
            Assert.Equal(new[] { 1 }, profile.KoploperBlockBindings[0].SectionIds);
            Assert.Equal(22, profile.KoploperBlockBindings[1].KoploperInternalBlockId);
            Assert.Equal(new[] { 2 }, profile.KoploperBlockBindings[1].SectionIds);
        }

        [Fact]
        public void LegacyLogicalSectionIdForm_LoadsAndNormalizes()
        {
            var json = WithBindings(@"[ { ""koploperInternalBlockId"": 30, ""logicalSectionId"": 1 } ]");
            Assert.True(LayoutProfileLoader.TryLoad(json, out var profile, out var errors), string.Join("; ", errors));
            Assert.NotNull(profile);

            Assert.Single(profile!.KoploperBlockBindings);
            Assert.Equal(30, profile.KoploperBlockBindings[0].KoploperInternalBlockId);
            Assert.Equal(new[] { 1 }, profile.KoploperBlockBindings[0].SectionIds);
        }

        [Fact]
        public void MultiSectionBinding_Loads()
        {
            var json = WithBindings(@"[ { ""koploperInternalBlockId"": 30, ""sectionIds"": [1, 2] } ]");
            Assert.True(LayoutProfileLoader.TryLoad(json, out var profile, out var errors), string.Join("; ", errors));
            Assert.NotNull(profile);

            Assert.Single(profile!.KoploperBlockBindings);
            Assert.Equal(30, profile.KoploperBlockBindings[0].KoploperInternalBlockId);
            Assert.Equal(new[] { 1, 2 }, profile.KoploperBlockBindings[0].SectionIds);
        }

        [Fact]
        public void BothLegacyAndNewForms_AreRejected()
        {
            var json = WithBindings(@"[ { ""koploperInternalBlockId"": 30, ""logicalSectionId"": 1, ""sectionIds"": [1] } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("both 'logicalSectionId' and 'sectionIds'"));
        }

        [Fact]
        public void EmptySectionIds_IsRejectedAsEmptyDriveGroup()
        {
            var json = WithBindings(@"[ { ""koploperInternalBlockId"": 30, ""sectionIds"": [] } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("empty drive group"));
        }

        [Fact]
        public void SectionsWithoutPhysicalBinding_IsRejectedAsEmptyDriveGroup()
        {
            // No physicalAmplifierMapping declared -> every section lacks a physical binding.
            var json = @"
{
  ""name"": ""Binding Test"",
  ""detectedSlaves"": [1],
  ""sections"": [
    { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [], ""lengthMm"": 100 }
  ],
  ""blocks"": [],
  ""switches"": [],
  ""routes"": [],
  ""locomotives"": [],
  ""koploperBlockBindings"": [ { ""koploperInternalBlockId"": 30, ""sectionIds"": [1] } ]
}";
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("empty drive group"));
        }

        [Fact]
        public void InvalidInternalBlockId_IsRejected()
        {
            var json = WithBindings(@"[ { ""koploperInternalBlockId"": 0, ""logicalSectionId"": 1 } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("invalid internal block id 0"));
        }

        [Fact]
        public void NegativeInternalBlockId_IsRejected()
        {
            var json = WithBindings(@"[ { ""koploperInternalBlockId"": -5, ""logicalSectionId"": 1 } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("invalid internal block id -5"));
        }

        [Fact]
        public void DuplicateInternalBlockId_IsRejected()
        {
            var json = WithBindings(@"[ { ""koploperInternalBlockId"": 30, ""logicalSectionId"": 1 }, { ""koploperInternalBlockId"": 30, ""logicalSectionId"": 2 } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("Duplicate Koploper block binding for internal block id 30"));
        }

        [Fact]
        public void UnknownSection_IsRejected()
        {
            var json = WithBindings(@"[ { ""koploperInternalBlockId"": 30, ""logicalSectionId"": 99 } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("references unknown section 99"));
        }

        [Fact]
        public void DuplicateSection_IsRejected()
        {
            var json = WithBindings(@"[ { ""koploperInternalBlockId"": 30, ""logicalSectionId"": 1 }, { ""koploperInternalBlockId"": 31, ""logicalSectionId"": 1 } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("Logical section 1 is bound to more than one Koploper internal block"));
        }

        [Fact]
        public void DuplicateSection_AcrossLists_IsRejected()
        {
            var json = WithBindings(@"[ { ""koploperInternalBlockId"": 30, ""sectionIds"": [1, 2] }, { ""koploperInternalBlockId"": 31, ""sectionIds"": [2] } ]");
            Assert.False(LayoutProfileLoader.TryLoad(json, out _, out var errors));
            Assert.Contains(errors, e => e.Contains("Logical section 2 is bound to more than one Koploper internal block"));
        }
    }
}
