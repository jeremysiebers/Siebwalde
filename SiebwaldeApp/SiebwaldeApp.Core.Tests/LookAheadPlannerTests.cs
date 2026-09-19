using System.Collections.Generic;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class LookAheadPlannerTests
    {
        private sealed class FakeOccupancy : IOccupancyProvider
        {
            private readonly HashSet<int> _occupied;

            public FakeOccupancy(params int[] occupied) => _occupied = new HashSet<int>(occupied);

            public bool IsBlockOccupied(int block) => _occupied.Contains(block);

            public bool IsBlockOccupancyKnown(int block) => true;
        }

        [Fact]
        public void PlansNextFreeBlock()
        {
            var planner = new LookAheadPlanner(BlockTopology.Parse("routes: 1>2"));
            var switches = new Dictionary<int, SwitchPosition>();

            Assert.True(planner.TryPlanNext(1, switches, new FakeOccupancy(), out var next));
            Assert.Equal(2, next);
        }

        [Fact]
        public void DoesNotPlanOccupiedBlock()
        {
            var planner = new LookAheadPlanner(BlockTopology.Parse("routes: 1>2"));
            var switches = new Dictionary<int, SwitchPosition>();

            Assert.False(planner.TryPlanNext(1, switches, new FakeOccupancy(2), out _));
        }

        [Fact]
        public void FollowsSwitchPosition()
        {
            var planner = new LookAheadPlanner(BlockTopology.Parse("routes: 10>11@5:0,10>12@5:1"));
            var switches = new Dictionary<int, SwitchPosition> { [5] = SwitchPosition.Diverging };

            Assert.True(planner.TryPlanNext(10, switches, new FakeOccupancy(), out var next));
            Assert.Equal(12, next);
        }

        [Fact]
        public void StationDeparture_IsNotPreCommanded()
        {
            // A station departure block must never be look-ahead commanded.
            var planner = new LookAheadPlanner(BlockTopology.Parse("routes: 20>21!"));
            var switches = new Dictionary<int, SwitchPosition>();

            Assert.False(planner.TryPlanNext(20, switches, new FakeOccupancy(), out _));
        }

        [Fact]
        public void SwitchTargetOccupied_DoesNotPlan()
        {
            var planner = new LookAheadPlanner(BlockTopology.Parse("routes: 10>12@5:1"));
            var switches = new Dictionary<int, SwitchPosition> { [5] = SwitchPosition.Diverging };

            Assert.False(planner.TryPlanNext(10, switches, new FakeOccupancy(12), out _));
        }

        [Fact]
        public void NoTransition_DoesNotPlan()
        {
            var planner = new LookAheadPlanner(BlockTopology.Parse("routes: 1>2"));
            var switches = new Dictionary<int, SwitchPosition>();

            Assert.False(planner.TryPlanNext(99, switches, new FakeOccupancy(), out _));
        }
    }
}
