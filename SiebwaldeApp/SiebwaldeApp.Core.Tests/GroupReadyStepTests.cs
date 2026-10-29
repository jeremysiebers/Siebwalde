using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Focused tests for the Stage 5 <see cref="GroupReadyStep"/>: it writes GROUP_READY
    /// (HR_TRACK_POWER bit 1) to every required amplifier and verifies each accepted it (HR_STATE
    /// reflects READY, i.e. left STANDBY). GROUP_READY never energizes. If any amplifier stays
    /// STANDBY (rejects), the step fails closed with the rejecting amplifier list.
    /// </summary>
    public class GroupReadyStepTests
    {
#pragma warning disable CS0067
        private sealed class RecordingCommClient : ITrackCommClient
        {
            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;
            public List<SendMessage> Sent { get; } = new();

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
            {
                Sent.Add(message);
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
#pragma warning restore CS0067

        private static readonly byte[] RequiredAmps = { 1, 3, 4, 6 };

        private static ushort ReadyState =>
            (ushort)(TrackAmplifierRegisters.ReadyState | TrackAmplifierRegisters.EnableActiveBit
                     | TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit);

        private static ushort StandbyState =>
            (ushort)(TrackAmplifierRegisters.StandbyState | TrackAmplifierRegisters.EnableActiveBit
                     | TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit);

        private static TrackApplicationVariables VariablesWith(byte[] amps, ushort state)
        {
            var variables = new TrackApplicationVariables();
            foreach (var amp in amps)
            {
                variables.trackAmpItems[amp].HoldingReg[TrackAmplifierRegisters.State] = state;
            }

            return variables;
        }

        [Fact]
        public async Task Execute_WritesGroupReady_AndVerifies_Completes()
        {
            var comm = new RecordingCommClient();
            var variables = VariablesWith(RequiredAmps, ReadyState);
            var step = new GroupReadyStep(comm, variables, RequiredAmps);

            // substate 0 writes GROUP_READY, substate 1 verifies + completes.
            var result = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Continue, result.Kind);

            result = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Completed, result.Kind);

            // Every required amplifier received a GROUP_READY (bit 1) write to HR_TRACK_POWER.
            var writes = comm.Sent
                .Where(m => m.Data.Length >= 4 && m.Data[3] == TrackAmplifierRegisters.TrackPower)
                .ToArray();
            Assert.Equal(RequiredAmps.Length, writes.Length);
            Assert.All(writes, m => Assert.Equal(TrackAmplifierRegisters.GroupReadyBit & 0xFF, m.Data[4]));
        }

        [Fact]
        public async Task Execute_OneAmpStaysStandby_FailsClosed()
        {
            var comm = new RecordingCommClient();
            var variables = VariablesWith(RequiredAmps, ReadyState);
            // Amp 6 rejects GROUP_READY -> its HR_STATE stays STANDBY.
            variables.trackAmpItems[6].HoldingReg[TrackAmplifierRegisters.State] = StandbyState;

            var step = new GroupReadyStep(comm, variables, RequiredAmps, verifyWindow: TimeSpan.FromMilliseconds(50));
            await step.ExecuteAsync(null, CancellationToken.None);

            InitStepResult result = default;
            for (int i = 0; i < 20; i++)
            {
                result = await step.ExecuteAsync(null, CancellationToken.None);
                if (result.Kind == InitStepResultKind.Error)
                {
                    break;
                }

                await Task.Delay(10);
            }

            Assert.Equal(InitStepResultKind.Error, result.Kind);
            Assert.Contains("6", result.ErrorMessage);
        }

        [Fact]
        public void IsGroupReady_RequiresReadyState()
        {
            var item = new TrackAmplifierItem { HoldingReg = new ushort[TrackAmplifierRegisters.Count] };

            item.HoldingReg[TrackAmplifierRegisters.State] = (ushort)TrackAmplifierRegisters.ReadyState;
            Assert.True(GroupReadyStep.IsGroupReady(item));

            item.HoldingReg[TrackAmplifierRegisters.State] = (ushort)TrackAmplifierRegisters.StandbyState;
            Assert.False(GroupReadyStep.IsGroupReady(item));

            item.HoldingReg[TrackAmplifierRegisters.State] = 0; // NOT_INITIALIZED
            Assert.False(GroupReadyStep.IsGroupReady(item));
        }
    }
}
