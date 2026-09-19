using System;
using System.Collections.Generic;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class TrackAmplifierHardwareBackendLookAheadTests
    {
        private sealed class FakeBlockPositionProvider : IBlockPositionProvider
        {
            private readonly int? _block;

            public FakeBlockPositionProvider(int? block) => _block = block;

            public event Action<int, int>? BlockEntered;

            public int? TryGetBlockForLoc(int loc) => _block;
        }

        private sealed class FakeOccupancy : IOccupancyProvider
        {
            private readonly HashSet<int> _occupied;

            public FakeOccupancy(params int[] occupied) => _occupied = new HashSet<int>(occupied);

            public bool IsBlockOccupied(int block) => _occupied.Contains(block);

            public bool IsBlockOccupancyKnown(int block) => true;
        }

        [Fact]
        public void SetLocoSpeed_AlsoCommandsTheNextBlock()
        {
            var variables = new TrackApplicationVariables();
            var topology = BlockTopology.Parse("amps: 1:1,2:2 ; routes: 1>2");
            var backend = new TrackAmplifierHardwareBackend(
                new FakeBlockPositionProvider(1),
                topology,
                variables,
                lookAheadPlanner: new LookAheadPlanner(topology),
                occupancyProvider: new FakeOccupancy());

            backend.SetLocoSpeed(address: 42, ecosSpeed: 127, direction: 0);

            Assert.True(variables.PendingWrites[1].TryConsumeHr0(out var current));
            Assert.True(variables.PendingWrites[2].TryConsumeHr0(out var next));
            Assert.Equal(AmplifierSpeedMapper.MaxPwm, current & 0x03FF);
            Assert.Equal(current, next);
        }

        [Fact]
        public void SetLocoSpeed_StationDeparture_DoesNotCommandNextBlock()
        {
            var variables = new TrackApplicationVariables();
            var topology = BlockTopology.Parse("amps: 20:1,21:2 ; routes: 20>21!");
            var backend = new TrackAmplifierHardwareBackend(
                new FakeBlockPositionProvider(20),
                topology,
                variables,
                lookAheadPlanner: new LookAheadPlanner(topology),
                occupancyProvider: new FakeOccupancy());

            backend.SetLocoSpeed(address: 42, ecosSpeed: 100, direction: 0);

            Assert.True(variables.PendingWrites.ContainsKey(1));
            Assert.False(variables.PendingWrites.ContainsKey(2));
        }

        [Fact]
        public void SetLocoSpeed_OccupiedNextBlock_DoesNotCommandIt()
        {
            var variables = new TrackApplicationVariables();
            var topology = BlockTopology.Parse("amps: 1:1,2:2 ; routes: 1>2");
            var backend = new TrackAmplifierHardwareBackend(
                new FakeBlockPositionProvider(1),
                topology,
                variables,
                lookAheadPlanner: new LookAheadPlanner(topology),
                occupancyProvider: new FakeOccupancy(2));

            backend.SetLocoSpeed(address: 42, ecosSpeed: 100, direction: 0);

            Assert.True(variables.PendingWrites.ContainsKey(1));
            Assert.False(variables.PendingWrites.ContainsKey(2));
        }

        [Fact]
        public void SetLocoSpeed_SwitchPositionSelectsLookAheadTarget()
        {
            var variables = new TrackApplicationVariables();
            var topology = BlockTopology.Parse("amps: 10:1,11:2,12:3 ; routes: 10>11@5:0,10>12@5:1");
            var backend = new TrackAmplifierHardwareBackend(
                new FakeBlockPositionProvider(10),
                topology,
                variables,
                lookAheadPlanner: new LookAheadPlanner(topology),
                occupancyProvider: new FakeOccupancy(),
                switchPositionProvider: () => new Dictionary<int, SwitchPosition> { [5] = SwitchPosition.Diverging });

            backend.SetLocoSpeed(address: 42, ecosSpeed: 100, direction: 0);

            Assert.True(variables.PendingWrites.ContainsKey(1));
            Assert.False(variables.PendingWrites.ContainsKey(2));
            Assert.True(variables.PendingWrites.ContainsKey(3));
        }

        [Fact]
        public void SetLocoSpeed_WithoutPlanner_OnlyCommandsCurrentBlock()
        {
            var variables = new TrackApplicationVariables();
            var topology = BlockTopology.Parse("amps: 1:1,2:2 ; routes: 1>2");
            var backend = new TrackAmplifierHardwareBackend(
                new FakeBlockPositionProvider(1),
                topology,
                variables);

            backend.SetLocoSpeed(address: 42, ecosSpeed: 100, direction: 0);

            Assert.True(variables.PendingWrites.ContainsKey(1));
            Assert.False(variables.PendingWrites.ContainsKey(2));
        }
    }
}
