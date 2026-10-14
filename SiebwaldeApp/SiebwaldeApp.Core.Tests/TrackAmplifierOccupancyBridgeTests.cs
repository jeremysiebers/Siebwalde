using System.Collections.Generic;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class TrackAmplifierOccupancyBridgeTests
    {
        /// <summary>Per-section occupancy fake: each amplifier carries its own tri-state.</summary>
        private sealed class FakeAmplifierOccupancy : IAmplifierOccupancyProvider
        {
            private readonly Dictionary<int, PhysicalOccupancy> _states = new();

            public void Set(int amplifier, PhysicalOccupancy state) => _states[amplifier] = state;

            public PhysicalOccupancy GetAmplifierOccupancy(int amplifierAddress)
                => _states.TryGetValue(amplifierAddress, out var state) ? state : PhysicalOccupancy.Unknown;
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
            var occupancy = new FakeAmplifierOccupancy();
            for (int amp = 1; amp <= 5; amp++)
            {
                occupancy.Set(amp, PhysicalOccupancy.Clear);
            }

            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(
                KoploperBlockMap.Parse(OvalMapping).ToKoploperFeedbackMap(), occupancy, sink);

            await bridge.EvaluateAsync();

            // 5 blocks x 2 bezetmelders
            Assert.Equal(10, sink.SensorEvents.Count);
            Assert.All(sink.SensorEvents, e => Assert.False(e.Occupied));
        }

        [Fact]
        public async Task SecondPoll_WithoutChange_SendsNothing()
        {
            var occupancy = new FakeAmplifierOccupancy();
            for (int amp = 1; amp <= 5; amp++)
            {
                occupancy.Set(amp, PhysicalOccupancy.Clear);
            }

            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(
                KoploperBlockMap.Parse(OvalMapping).ToKoploperFeedbackMap(), occupancy, sink);

            await bridge.EvaluateAsync();
            sink.SensorEvents.Clear();
            await bridge.EvaluateAsync();

            Assert.Empty(sink.SensorEvents);
        }

        [Fact]
        public async Task OccupancyChange_ReportsTheBezetmeldersOfThatBlock()
        {
            var occupancy = new FakeAmplifierOccupancy();
            for (int amp = 1; amp <= 5; amp++)
            {
                occupancy.Set(amp, PhysicalOccupancy.Clear);
            }

            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(
                KoploperBlockMap.Parse(OvalMapping).ToKoploperFeedbackMap(), occupancy, sink);

            await bridge.EvaluateAsync();
            sink.SensorEvents.Clear();

            occupancy.Set(3, PhysicalOccupancy.Occupied);
            await bridge.EvaluateAsync();

            Assert.Equal(2, sink.SensorEvents.Count);
            Assert.Contains((5, true), sink.SensorEvents);
            Assert.Contains((6, true), sink.SensorEvents);
        }

        [Fact]
        public async Task BlockWithSingleBezetmelder_ReportsOneEvent()
        {
            var occupancy = new FakeAmplifierOccupancy();
            occupancy.Set(7, PhysicalOccupancy.Clear);

            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(
                KoploperBlockMap.Parse("7:1.11:7").ToKoploperFeedbackMap(), occupancy, sink);

            await bridge.EvaluateAsync();

            Assert.Single(sink.SensorEvents);
            Assert.Equal((11, false), sink.SensorEvents[0]);
        }

        [Fact]
        public async Task MultiSectionBlock_EachBezetmelderReflectsItsOwnSection()
        {
            // A block with two sections/amps/bezetmelders: section1 occupied, section2 clear.
            var feedbackMap = new List<KoploperFeedbackBlock>
            {
                new()
                {
                    BlockNumber = 10,
                    Sections = new List<KoploperFeedbackSection>
                    {
                        new() { SectionId = 1, Amplifier = 1, Bezetmelders = new[] { "1.01" } },
                        new() { SectionId = 2, Amplifier = 2, Bezetmelders = new[] { "1.02" } }
                    }
                }
            };

            var occupancy = new FakeAmplifierOccupancy();
            occupancy.Set(1, PhysicalOccupancy.Occupied);
            occupancy.Set(2, PhysicalOccupancy.Clear);

            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(feedbackMap, occupancy, sink);

            await bridge.EvaluateAsync();

            // No aggregation: bezetmelder 1.01 (sensor 1) reflects ITS section (occupied) and
            // bezetmelder 1.02 (sensor 2) reflects ITS section (clear).
            Assert.Equal(2, sink.SensorEvents.Count);
            Assert.Contains((1, true), sink.SensorEvents);
            Assert.Contains((2, false), sink.SensorEvents);
        }

        [Fact]
        public async Task MultiSectionBlock_UnknownSection_IsNeverReportedAsClear()
        {
            // One section occupied, one section unknown: the unknown section must not be pushed as free.
            var feedbackMap = new List<KoploperFeedbackBlock>
            {
                new()
                {
                    BlockNumber = 10,
                    Sections = new List<KoploperFeedbackSection>
                    {
                        new() { SectionId = 1, Amplifier = 1, Bezetmelders = new[] { "1.01" } },
                        new() { SectionId = 2, Amplifier = 2, Bezetmelders = new[] { "1.02" } }
                    }
                }
            };

            var occupancy = new FakeAmplifierOccupancy();
            occupancy.Set(1, PhysicalOccupancy.Occupied);
            // amp 2 left Unknown

            var sink = new RecordingFeedbackSink();
            var bridge = new TrackAmplifierOccupancyBridge(feedbackMap, occupancy, sink);

            await bridge.EvaluateAsync();

            // Only the occupied section produces an event; the unknown section is skipped.
            Assert.Single(sink.SensorEvents);
            Assert.Equal((1, true), sink.SensorEvents[0]);
        }
    }
}
