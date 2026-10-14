using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class KoploperBlockMapTests
    {
        // The authoritative mapping for the test oval, from the Koploper export.
        private const string OvalMapping =
            "1:1.01+1.02:1, 2:1.03+1.04:2, 3:1.05+1.06:3, 4:1.07+1.08:4, 5:1.09+1.10:5";

        [Fact]
        public void ParsesTheOvalMapping()
        {
            var map = KoploperBlockMap.Parse(OvalMapping);

            Assert.Equal(5, map.Blocks.Count);
            Assert.True(map.TryGetByBlock(3, out var block3));
            Assert.Equal(new[] { "1.05", "1.06" }, block3.Bezetmelders);
            Assert.Equal(new ushort[] { 3 }, block3.AmplifierSections);
        }

        [Fact]
        public void LineBreaksSeparateEntries()
        {
            var map = KoploperBlockMap.Parse("1:1.01:1\n2:1.02:2");

            Assert.Equal(2, map.Blocks.Count);
            Assert.True(map.TryGetByBlock(2, out var block2));
            Assert.Equal(new[] { "1.02" }, block2.Bezetmelders);
            Assert.Equal(new ushort[] { 2 }, block2.AmplifierSections);
        }

        [Fact]
        public void FindsBlockByBezetmelder()
        {
            var map = KoploperBlockMap.Parse(OvalMapping);

            Assert.True(map.TryGetByBezetmelder("1.03", out var block));
            Assert.Equal(2, block.Number);
            Assert.False(map.TryGetByBezetmelder("9.99", out _));
        }

        [Fact]
        public void FindsBlockByAmplifierSection()
        {
            var map = KoploperBlockMap.Parse(OvalMapping);

            Assert.True(map.TryGetByAmplifierSection(5, out var block));
            Assert.Equal(5, block.Number);
            Assert.False(map.TryGetByAmplifierSection(99, out _));
        }

        [Fact]
        public void SupportsMultipleAmplifierSectionsPerBlock()
        {
            var map = KoploperBlockMap.Parse("10:1.11+1.12:6+7");

            Assert.True(map.TryGetByBlock(10, out var block));
            Assert.Equal(new ushort[] { 6, 7 }, block.AmplifierSections);
        }

        [Fact]
        public void EmptyConfiguration_HasNoBlocks()
        {
            Assert.Empty(KoploperBlockMap.Parse(null).Blocks);
            Assert.Empty(KoploperBlockMap.Parse("").Blocks);
        }

        [Fact]
        public void IgnoresMalformedEntries()
        {
            var map = KoploperBlockMap.Parse("garbage,1:1.01:1,broken");

            Assert.Single(map.Blocks);
        }

        [Fact]
        public void ToKoploperFeedbackMap_SingleAmplifier_AllBezetmeldersMapToIt()
        {
            var map = KoploperBlockMap.Parse("1:1.01+1.02:1");

            var feedback = map.ToKoploperFeedbackMap();

            var block = Assert.Single(feedback);
            Assert.Equal(1, block.BlockNumber);
            var section = Assert.Single(block.Sections);
            Assert.Equal(1, section.Amplifier);
            Assert.Equal(new[] { "1.01", "1.02" }, section.Bezetmelders);
        }

        [Fact]
        public void ToKoploperFeedbackMap_MultipleAmplifiers_PairsPositionally()
        {
            var map = KoploperBlockMap.Parse("10:1.11+1.12:6+7");

            var feedback = map.ToKoploperFeedbackMap();

            var block = Assert.Single(feedback);
            Assert.Equal(10, block.BlockNumber);
            Assert.Equal(2, block.Sections.Count);
            Assert.Equal(6, block.Sections[0].Amplifier);
            Assert.Equal(new[] { "1.11" }, block.Sections[0].Bezetmelders);
            Assert.Equal(7, block.Sections[1].Amplifier);
            Assert.Equal(new[] { "1.12" }, block.Sections[1].Bezetmelders);
        }
    }
}
