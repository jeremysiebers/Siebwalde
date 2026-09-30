using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class DeterministicMovementSimulatorTests
    {
        private const string OvalJson = @"
{
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
  ""locomotives"": []
}";

        private static DeterministicMovementSimulator CreateOval()
        {
            Assert.True(LayoutProfileLoader.TryLoad(OvalJson, out var profile, out var errors), string.Join("; ", errors));
            return new DeterministicMovementSimulator(profile!);
        }

        [Fact]
        public void PlaceLoco_SetSpeed_AdvanceMovesOffset()
        {
            var sim = CreateOval();
            sim.PlaceLoco(1000, 1);
            sim.SetLocoSpeed(1000, 10, direction: 0); // 100 mm/s forward

            sim.Advance(TimeSpan.FromSeconds(1));

            var position = sim.GetLocoPositions().Single(p => p.Address == 1000);
            Assert.Equal(1, position.BlockId);
            Assert.Equal(1, position.SectionId);
            Assert.Equal(100.0, position.OffsetMm, 3);
        }

        [Fact]
        public void CrossingBoundary_EmitsClearThenOccupy_InOrder()
        {
            var sim = CreateOval();
            sim.PlaceLoco(1000, 1);

            var events = new List<(int Section, bool Occupied)>();
            sim.SectionOccupancyChanged += (_, e) => events.Add((e.SectionId, e.Occupied));

            sim.SetLocoSpeed(1000, 127, direction: 0); // 1270 mm/s > 1000 mm section
            sim.Advance(TimeSpan.FromSeconds(1));

            Assert.Equal(2, events.Count);
            Assert.Equal((1, false), events[0]); // leaving section 1
            Assert.Equal((2, true), events[1]);  // entering section 2

            var position = sim.GetLocoPositions().Single(p => p.Address == 1000);
            Assert.Equal(2, position.BlockId);
            Assert.Equal(2, position.SectionId);
            Assert.Equal(0.0, position.OffsetMm, 3); // placed at the start of the next section
        }

        [Fact]
        public void SpeedZero_HaltsMovement()
        {
            var sim = CreateOval();
            sim.PlaceLoco(1000, 1);
            sim.SetLocoSpeed(1000, 10, direction: 0);
            sim.Advance(TimeSpan.FromSeconds(1));

            var moved = sim.GetLocoPositions().Single().OffsetMm;
            Assert.True(moved > 0);

            sim.SetLocoSpeed(1000, 0, direction: 0);
            sim.Advance(TimeSpan.FromSeconds(1));

            Assert.Equal(moved, sim.GetLocoPositions().Single().OffsetMm, 3);
        }

        [Fact]
        public void DirectionReverses()
        {
            var sim = CreateOval();
            sim.PlaceLoco(1000, 1);

            sim.SetLocoSpeed(1000, 10, direction: 0);
            sim.Advance(TimeSpan.FromMilliseconds(500));
            Assert.Equal(50.0, sim.GetLocoPositions().Single().OffsetMm, 3);

            sim.SetLocoSpeed(1000, 10, direction: 1);
            sim.Advance(TimeSpan.FromMilliseconds(500));
            Assert.Equal(0.0, sim.GetLocoPositions().Single().OffsetMm, 3);
        }

        [Fact]
        public void NonZeroDirection_IsReverse_DecreasesOffset()
        {
            var sim = CreateOval();
            sim.PlaceLoco(1000, 1); // block 1, section 1, offset 0

            // Forward (ECoS direction 0) increases the offset.
            sim.SetLocoSpeed(1000, 10, direction: 0); // 100 mm/s forward
            sim.Advance(TimeSpan.FromMilliseconds(500));
            var forwardOffset = sim.GetLocoPositions().Single().OffsetMm;
            Assert.Equal(50.0, forwardOffset, 3);

            // Reverse (ECoS non-zero direction) must decrease the offset. Regression: the old
            // `direction >= 0` mapping treated 1 as forward, so a reverse command kept moving forward.
            sim.SetLocoSpeed(1000, 10, direction: 1); // 100 mm/s reverse
            sim.Advance(TimeSpan.FromMilliseconds(500));
            var reverseOffset = sim.GetLocoPositions().Single().OffsetMm;
            Assert.Equal(0.0, reverseOffset, 3);
            Assert.True(reverseOffset < forwardOffset, "Reverse must decrease the offset.");
        }

        [Fact]
        public void ReverseFromFirstBlock_WrapsToPreviousBlock()
        {
            var sim = CreateOval();
            sim.PlaceLoco(1000, 1); // block 1, offset 0

            sim.SetLocoSpeed(1000, 10, direction: 1);
            sim.Advance(TimeSpan.FromMilliseconds(100)); // 10 mm reverse -> crosses to block 4

            var position = sim.GetLocoPositions().Single();
            Assert.Equal(4, position.BlockId); // reversed 4>1 route
            Assert.Equal(4, position.SectionId);
        }

        [Fact]
        public void Stop_HaltsTimer_AndEmitsNoGhostOccupancy()
        {
            var sim = CreateOval();
            sim.PlaceLoco(1000, 1);

            var events = new ConcurrentQueue<SectionOccupancyChangedEventArgs>();
            sim.SectionOccupancyChanged += (_, e) => events.Enqueue(e);

            sim.SetLocoSpeed(1000, 127, direction: 0);

            using var cts = new CancellationTokenSource();
            sim.Start(cts.Token);

            // Wait for the first boundary crossing (leave section 1 + enter section 2).
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (events.Count < 2 && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(20);
            }

            Assert.True(events.Count >= 2, "Expected a boundary crossing to emit occupancy events.");

            sim.Stop();
            var countAfterStop = events.Count;
            Thread.Sleep(400);

            Assert.Equal(countAfterStop, events.Count); // no ghost occupancy after Stop
        }

        [Fact]
        public void TryGetBlockForLoc_ReturnsSimulatedBlock()
        {
            var sim = CreateOval();
            sim.PlaceLoco(1000, 3);

            // Interface (IBlockPositionProvider) overload.
            Assert.Equal(3, ((IBlockPositionProvider)sim).TryGetBlockForLoc(1000));
            Assert.Null(((IBlockPositionProvider)sim).TryGetBlockForLoc(9999));

            // Public bool overload.
            Assert.True(sim.TryGetBlockForLoc(1000, out var blockId));
            Assert.Equal(3, blockId);
            Assert.False(sim.TryGetBlockForLoc(9999, out _));
        }
    }
}
