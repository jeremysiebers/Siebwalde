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

            /// <summary>Set to false to model "no valid amplifier data yet".</summary>
            public bool Known { get; set; } = true;

            public bool IsBlockOccupied(int block) => Occupied.Contains(block);

            public bool IsBlockOccupancyKnown(int block) => Known;
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

        [Theory]
        [InlineData("1.01", 1)]
        [InlineData("1.03", 3)]
        [InlineData("1.10", 10)]
        [InlineData("2.01", 17)]
        public void TryGetSensorId_ConvertsBezetmelderNames(string name, int expected)
        {
            Assert.True(TrackAmplifierOccupancyBridge.TryGetSensorId(name, out var sensorId));
            Assert.Equal(expected, sensorId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("garbage")]
        [InlineData("1")]
        [InlineData("1.17")]
        [InlineData("0.01")]
        public void TryGetSensorId_RejectsInvalidNames(string name)
        {
            Assert.False(TrackAmplifierOccupancyBridge.TryGetSensorId(name, out _));
        }

        [Fact]
        public async Task FirstPoll_ReportsEveryBezetmelder()
        {
            var occupancy = new FakeOccupancy();
            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(KoploperBlockMap.Parse(OvalMapping), occupancy, sink);

            await bridge.EvaluateAsync();

            // 5 blocks x 2 bezetmelders
            Assert.Equal(10, sink.SensorEvents.Count);
            Assert.All(sink.SensorEvents, e => Assert.False(e.Occupied));
        }

        [Fact]
        public async Task SecondPoll_WithoutChange_SendsNothing()
        {
            var occupancy = new FakeOccupancy();
            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(KoploperBlockMap.Parse(OvalMapping), occupancy, sink);

            await bridge.EvaluateAsync();
            sink.SensorEvents.Clear();
            await bridge.EvaluateAsync();

            Assert.Empty(sink.SensorEvents);
        }

        [Fact]
        public async Task OccupancyChange_ReportsTheBezetmeldersOfThatBlock()
        {
            var occupancy = new FakeOccupancy();
            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(KoploperBlockMap.Parse(OvalMapping), occupancy, sink);

            await bridge.EvaluateAsync();
            sink.SensorEvents.Clear();

            occupancy.Occupied.Add(3);
            await bridge.EvaluateAsync();

            Assert.Equal(2, sink.SensorEvents.Count);
            Assert.Contains((5, true), sink.SensorEvents);
            Assert.Contains((6, true), sink.SensorEvents);
        }

        [Fact]
        public async Task BlockWithSingleBezetmelder_ReportsOneEvent()
        {
            var occupancy = new FakeOccupancy();
            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(
                KoploperBlockMap.Parse("7:1.11:7"), occupancy, sink);

            await bridge.EvaluateAsync();

            Assert.Single(sink.SensorEvents);
            Assert.Equal((11, false), sink.SensorEvents[0]);
        }
    }
}

