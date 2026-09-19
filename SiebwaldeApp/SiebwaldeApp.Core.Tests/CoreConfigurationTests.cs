using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Covers the fallback that protects against legacy user.config files holding an empty
    /// mapping value. A blank persisted value must resolve to the declared default, while a
    /// non-empty (even malformed) value must be used as-is so errors stay diagnosable.
    /// </summary>
    public class CoreConfigurationTests
    {
        private const string TopologySetting = "BlockTopologyConfig";
        private const string BlockMapSetting = "KoploperBlockMapConfig";

        private static string DeclaredDefault(string settingName)
            => Properties.CoreSettings.Default.Properties[settingName].DefaultValue as string ?? "";

        [Fact]
        public void DeclaredDefaults_ArePresentInTheSettingsMetadata()
        {
            // The Designer must stay the single source of truth for the default mapping.
            Assert.False(string.IsNullOrWhiteSpace(DeclaredDefault(TopologySetting)));
            Assert.False(string.IsNullOrWhiteSpace(DeclaredDefault(BlockMapSetting)));
        }

        [Fact]
        public void DeclaredDefaults_ParseIntoAMapping()
        {
            Assert.NotEmpty(BlockTopology.Parse(DeclaredDefault(TopologySetting)).Blocks);
            Assert.NotEmpty(KoploperBlockMap.Parse(DeclaredDefault(BlockMapSetting)).Blocks);
        }

        [Fact]
        public void ResolveSettingOrDefault_WithValidValue_UsesItAsIs()
        {
            const string value = "amps: 7:7 ; routes: 7>8";

            Assert.Equal(value, CoreConfiguration.ResolveSettingOrDefault(value, TopologySetting));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\r\n\t")]
        public void ResolveSettingOrDefault_WithBlankValue_FallsBackToTheDeclaredDefault(string? blank)
        {
            var resolved = CoreConfiguration.ResolveSettingOrDefault(blank, TopologySetting);

            Assert.Equal(DeclaredDefault(TopologySetting), resolved);
            Assert.NotEmpty(BlockTopology.Parse(resolved).Blocks);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void ResolveSettingOrDefault_WithBlankBlockMap_FallsBackToTheDeclaredDefault(string? blank)
        {
            var resolved = CoreConfiguration.ResolveSettingOrDefault(blank, BlockMapSetting);

            Assert.Equal(DeclaredDefault(BlockMapSetting), resolved);
            Assert.NotEmpty(KoploperBlockMap.Parse(resolved).Blocks);
        }

        [Fact]
        public void ResolveSettingOrDefault_WithMalformedValue_DoesNotMaskItWithTheDefault()
        {
            const string malformed = "this is not a topology";

            var resolved = CoreConfiguration.ResolveSettingOrDefault(malformed, TopologySetting);

            // The malformed value must survive so the operator can see the mistake.
            Assert.Equal(malformed, resolved);
            Assert.NotEqual(DeclaredDefault(TopologySetting), resolved);

            // Existing validation behaviour: invalid entries are ignored rather than fixed up.
            Assert.Empty(BlockTopology.Parse(resolved).Blocks);
        }

        [Fact]
        public void BuildBlockTopology_WithTheDeclaredDefault_ProducesBlocks()
        {
            var topology = BlockTopology.Parse(DeclaredDefault(TopologySetting));

            Assert.NotEmpty(topology.Blocks);
        }
    }
}
