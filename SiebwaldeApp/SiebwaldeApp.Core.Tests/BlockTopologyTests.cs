using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class BlockTopologyTests
    {
        [Fact]
        public void Parse_MapsBlockToSingleAmplifier()
        {
            var topology = BlockTopology.Parse("1:1,2:2,3:3,4:4");

            Assert.True(topology.TryGetAmplifiers(1, out var amp1));
            Assert.Equal(new ushort[] { 1 }, amp1);
            Assert.True(topology.TryGetAmplifiers(4, out var amp4));
            Assert.Equal(new ushort[] { 4 }, amp4);
        }

        [Fact]
        public void Parse_SupportsMultipleAmplifiersPerBlock()
        {
            var topology = BlockTopology.Parse("10:1+2");

            Assert.True(topology.TryGetAmplifiers(10, out var amps));
            Assert.Equal(new ushort[] { 1, 2 }, amps);
        }

        [Fact]
        public void Parse_IgnoresInvalidEntries()
        {
            var topology = BlockTopology.Parse("garbage,1:1,,x:y,2:");

            Assert.True(topology.TryGetAmplifiers(1, out _));
            Assert.False(topology.TryGetAmplifiers(2, out _));
            Assert.Single(topology.Blocks);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_EmptyConfiguration_HasNoBlocks(string? configuration)
        {
            var topology = BlockTopology.Parse(configuration);

            Assert.Empty(topology.Blocks);
        }

        [Fact]
        public void TryGetAmplifiers_UnknownBlock_ReturnsFalse()
        {
            var topology = BlockTopology.Parse("1:1");

            Assert.False(topology.TryGetAmplifiers(99, out _));
        }

        [Fact]
        public void Parse_TrimsWhitespace()
        {
            var topology = BlockTopology.Parse(" 1 : 2 , 3 : 4 ");

            Assert.True(topology.TryGetAmplifiers(1, out var amps));
            Assert.Equal(new ushort[] { 2 }, amps);
            Assert.True(topology.TryGetAmplifiers(3, out var amps3));
            Assert.Equal(new ushort[] { 4 }, amps3);
        }
    }
}
