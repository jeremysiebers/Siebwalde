using System.Collections.Generic;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class TrackAmplifierOccupancyBridgeTests
    {
        private sealed class FakeOccupancy : IOccupancyProvider
        {
            public HashSet<int> Occupied { get; } = new();

            public bool IsBlockOccupied(int block) => Occupied.Contains(block);
        }

        private sealed class RecordingFeedbackSink : IHardwareFeedbackSink
        {
            public List<(int SensorId, bool Occupied)> SensorEvents { get; } = new();

            public Task OnSwitchChangedAsync(int ecosId, int decoderAddress, int outputIndex)
                => Task.CompletedTask;

            public Task OnSensorChangedAsync(int sensorId, bool occupied)
            {
                SensorEvents.Add((sensorId, occupied));
                return Task.CompletedTask;
            }
        }

        private const string OvalMapping =
            "1:1.01+1.02:1, 2:1.03+1.04:2, 3:1.05+1.06:3, 4:1.07+1.08:4, 5:1.09+1.10:5";

        [Fact]
        public void SensorIdsFollowTheBlockNumbering()
        {
            Assert.Equal(1, TrackAmplifierOccupancyBridge.EnterSensor(1));
            Assert.Equal(2, TrackAmplifierOccupancyBridge.ExitSensor(1));
            Assert.Equal(9, TrackAmplifierOccupancyBridge.EnterSensor(5));
            Assert.Equal(10, TrackAmplifierOccupancyBridge.ExitSensor(5));
        }

        [Fact]
        public async Task FirstPoll_ReportsEveryBlock()
        {
            var occupancy = new FakeOccupancy();
            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(KoploperBlockMap.Parse(OvalMapping), occupancy, sink);

            await bridge.PollAsync();

            // 5 blocks x (enter + exit)
            Assert.Equal(10, sink.SensorEvents.Count);
            Assert.All(sink.SensorEvents, e => Assert.False(e.Occupied));
        }

        [Fact]
        public async Task SecondPoll_WithoutChange_SendsNothing()
        {
            var occupancy = new FakeOccupancy();
            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(KoploperBlockMap.Parse(OvalMapping), occupancy, sink);

            await bridge.PollAsync();
            sink.SensorEvents.Clear();
            await bridge.PollAsync();

            Assert.Empty(sink.SensorEvents);
        }

        [Fact]
        public async Task OccupancyChange_ReportsBothBezetmeldersOfThatBlock()
        {
            var occupancy = new FakeOccupancy();
            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(KoploperBlockMap.Parse(OvalMapping), occupancy, sink);

            await bridge.PollAsync();
            sink.SensorEvents.Clear();

            occupancy.Occupied.Add(3);
            await bridge.PollAsync();

            Assert.Equal(2, sink.SensorEvents.Count);
            Assert.Contains((5, true), sink.SensorEvents);
            Assert.Contains((6, true), sink.SensorEvents);
        }

        [Fact]
        public async Task OccupancyCleared_ReportsBothBezetmeldersAsFree()
        {
            var occupancy = new FakeOccupancy();
            occupancy.Occupied.Add(2);

            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(KoploperBlockMap.Parse(OvalMapping), occupancy, sink);

            await bridge.PollAsync();
            sink.SensorEvents.Clear();

            occupancy.Occupied.Remove(2);
            await bridge.PollAsync();

            Assert.Equal(2, sink.SensorEvents.Count);
            Assert.Contains((3, false), sink.SensorEvents);
            Assert.Contains((4, false), sink.SensorEvents);
        }
    }
}
