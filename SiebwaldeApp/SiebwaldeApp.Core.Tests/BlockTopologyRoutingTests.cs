using System.Collections.Generic;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class BlockTopologyRoutingTests
    {
        [Fact]
        public void ParsesUnconditionalTransition()
        {
            var topology = BlockTopology.Parse("amps: 1:1 ; routes: 1>2,2>3");

            var transitions = topology.GetTransitionsFrom(1);

            Assert.Single(transitions);
            Assert.Equal(2, transitions[0].ToBlock);
            Assert.Null(transitions[0].SwitchId);
            Assert.True(transitions[0].AllowLookAhead);
        }

        [Fact]
        public void ParsesSwitchConditionedTransition()
        {
            var topology = BlockTopology.Parse("routes: 10>11@5:0,10>12@5:1");

            var transitions = topology.GetTransitionsFrom(10);

            Assert.Equal(2, transitions.Count);
            Assert.Equal(11, transitions[0].ToBlock);
            Assert.Equal(5, transitions[0].SwitchId);
            Assert.Equal(SwitchPosition.Straight, transitions[0].RequiredSwitchPosition);
            Assert.Equal(12, transitions[1].ToBlock);
            Assert.Equal(SwitchPosition.Diverging, transitions[1].RequiredSwitchPosition);
        }

        [Fact]
        public void ParsesNoLookAheadMarker()
        {
            var topology = BlockTopology.Parse("routes: 20>21!");

            var transitions = topology.GetTransitionsFrom(20);

            Assert.Single(transitions);
            Assert.False(transitions[0].AllowLookAhead);
        }

        [Fact]
        public void TryGetNextBlocks_FiltersBySwitchPosition()
        {
            var topology = BlockTopology.Parse("routes: 10>11@5:0,10>12@5:1");
            var switches = new Dictionary<int, SwitchPosition> { [5] = SwitchPosition.Diverging };

            Assert.True(topology.TryGetNextBlocks(10, switches, lookAheadOnly: false, out var next));
            Assert.Equal(new List<int> { 12 }, next);
        }

        [Fact]
        public void TryGetNextBlocks_ExcludesNoLookAheadTransitions()
        {
            var topology = BlockTopology.Parse("routes: 20>21!");
            var switches = new Dictionary<int, SwitchPosition>();

            Assert.False(topology.TryGetNextBlocks(20, switches, lookAheadOnly: true, out var next));
            Assert.Empty(next);
        }

        [Fact]
        public void PlainConfiguration_StillParsesAmplifiers()
        {
            var topology = BlockTopology.Parse("1:1,2:2");

            Assert.True(topology.TryGetAmplifiers(1, out var amplifiers));
            Assert.Equal(new ushort[] { 1 }, amplifiers);
        }
    }
}
