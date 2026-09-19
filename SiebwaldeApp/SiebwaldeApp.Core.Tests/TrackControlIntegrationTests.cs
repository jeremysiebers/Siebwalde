using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class TrackControlIntegrationTests
    {
        private const string OvalMapping =
            "1:1.01+1.02:1, 2:1.03+1.04:2, 3:1.05+1.06:3, 4:1.07+1.08:4, 5:1.09+1.10:5";

        private const string OvalTopology =
            "amps: 1:1,2:2,3:3,4:4,5:5 ; routes: 1>2,2>3,3>4,3>5,4>1,5>1";

        private sealed class FakeCommClient : ITrackCommClient
        {
            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
#pragma warning disable CS0067
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;
#pragma warning restore CS0067

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public void RaiseAmplifierData(int index, TrackAmplifierItem amplifier)
                => AmplifierDataReceived?.Invoke(this, new AmplifierDataEventArgs(index, amplifier));
        }

        private sealed class FakeBlockPositionProvider : IBlockPositionProvider
        {
            public event Action<int, int>? BlockEntered;

            public int? TryGetBlockForLoc(int loc) => null;
        }

        private sealed class RecordingFeedbackSink : IHardwareFeedbackSink
        {
            public List<(int SensorId, bool Occupied)> SensorEvents { get; } = new();

            public Task OnSwitchChangedAsync(int ecosId, int decoderAddress, int outputIndex) => Task.CompletedTask;

            public Task OnSensorChangedAsync(int sensorId, bool occupied)
            {
                SensorEvents.Add((sensorId, occupied));
                return Task.CompletedTask;
            }
        }

        private static TrackControlIntegration CreateIntegration(
            FakeCommClient commClient,
            TrackApplicationVariables variables,
            RecordingFeedbackSink sink)
            => new(
                commClient,
                variables,
                new FakeBlockPositionProvider(),
                BlockTopology.Parse(OvalTopology),
                KoploperBlockMap.Parse(OvalMapping),
                feedbackSink: sink);

        [Fact]
        public void Attach_EstablishesInitialOccupancy()
        {
            var variables = new TrackApplicationVariables();
            var sink = new RecordingFeedbackSink();
            var integration = CreateIntegration(new FakeCommClient(), variables, sink);

            integration.Attach();

            // 5 blocks x 2 bezetmelders, all free
            Assert.Equal(10, sink.SensorEvents.Count);
        }

        [Fact]
        public void AmplifierDataEvent_ForwardsOccupancyChange()
        {
            var variables = new TrackApplicationVariables();
            var sink = new RecordingFeedbackSink();
            var commClient = new FakeCommClient();
            var integration = CreateIntegration(commClient, variables, sink);

            integration.Attach();
            sink.SensorEvents.Clear();

            // Amplifier section 3 reports occupied.
            var amplifier = variables.trackAmpItems[3];
            amplifier.HoldingReg = new ushort[12];
            amplifier.HoldingReg[TrackAmplifierRegisters.Status] = TrackAmplifierRegisters.OccupiedBit;

            commClient.RaiseAmplifierData(3, amplifier);

            // Block 3 -> bezetmelders 1.05/1.06 -> sensors 5/6
            Assert.Equal(2, sink.SensorEvents.Count);
            Assert.Contains((5, true), sink.SensorEvents);
            Assert.Contains((6, true), sink.SensorEvents);
        }

        [Fact]
        public void Detach_StopsForwarding()
        {
            var variables = new TrackApplicationVariables();
            var sink = new RecordingFeedbackSink();
            var commClient = new FakeCommClient();
            var integration = CreateIntegration(commClient, variables, sink);

            integration.Attach();
            integration.Detach();
            sink.SensorEvents.Clear();

            var amplifier = variables.trackAmpItems[3];
            amplifier.HoldingReg = new ushort[12];
            amplifier.HoldingReg[TrackAmplifierRegisters.Status] = TrackAmplifierRegisters.OccupiedBit;

            commClient.RaiseAmplifierData(3, amplifier);

            Assert.Empty(sink.SensorEvents);
        }
    }
}
