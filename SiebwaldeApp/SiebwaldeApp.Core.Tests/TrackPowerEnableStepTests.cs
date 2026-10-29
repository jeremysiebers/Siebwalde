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
    /// Focused tests for the Stage 6 <see cref="TrackPowerEnableStep"/>: it writes HR_TRACK_POWER
    /// (register 23, bit 0) to every required amplifier and verifies HR_STATE per amplifier before
    /// declaring TRACK_POWER_ON, emitting NO non-neutral PWM and failing closed if any amplifier does
    /// not reach the verified state.
    /// </summary>
    public class TrackPowerEnableStepTests
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

        private static TrackApplicationVariables VariablesWith(byte[] amps, ushort state,
            ushort fwVersionPatchProtocol = TrackAmplifierRegisters.TrackPowerProtocolVersion)
        {
            var variables = new TrackApplicationVariables();
            foreach (var amp in amps)
            {
                variables.trackAmpItems[amp].HoldingReg[TrackAmplifierRegisters.State] = state;
                // HR22 = patch<<8 | protocol. patch 0 + protocol 0x02 advertises track power.
                variables.trackAmpItems[amp].HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol] =
                    fwVersionPatchProtocol;
            }

            return variables;
        }

        private static ushort TrackPowerOnState =>
            (ushort)(TrackAmplifierRegisters.EnableActiveBit | TrackAmplifierRegisters.ConfigOkBit);

        [Fact]
        public async Task Execute_WritesTrackPower_AndVerifies_Completes()
        {
            var comm = new RecordingCommClient();
            var variables = VariablesWith(RequiredAmps, TrackPowerOnState);
            var step = new TrackPowerEnableStep(comm, variables, RequiredAmps);

            // substate 0 writes HR_TRACK_POWER, then substate 1 reads + verifies + completes.
            var result = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Continue, result.Kind);

            result = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Completed, result.Kind);

            // Every required amplifier received a HR_TRACK_POWER write (register 23, bit 0).
            var writes = comm.Sent
                .Where(m => m.Data.Length >= 4 && m.Data[3] == TrackAmplifierRegisters.TrackPower)
                .ToArray();
            Assert.Equal(RequiredAmps.Length, writes.Length);
            Assert.All(writes, m => Assert.Equal((byte)0xAA, m.Data[1]));
            Assert.All(writes, m => Assert.Equal(TrackAmplifierRegisters.TrackPowerEnableBit & 0xFF, m.Data[4]));

            // No non-neutral PWM: the step never writes HR0 (PwmCommand).
            Assert.DoesNotContain(comm.Sent, m => m.Data.Length >= 4 && m.Data[3] == TrackAmplifierRegisters.PwmCommand);
        }

        [Fact]
        public async Task Execute_OneAmplifierNotReady_FailsClosed()
        {
            var comm = new RecordingCommClient();
            var variables = VariablesWith(RequiredAmps, TrackPowerOnState);
            // Amplifier 6 stays inhibited (bit 15 set), so it never reaches TRACK_POWER_ON.
            variables.trackAmpItems[6].HoldingReg[TrackAmplifierRegisters.State] |= TrackAmplifierRegisters.InhibitedBit;

            var step = new TrackPowerEnableStep(comm, variables, RequiredAmps, verifyWindow: TimeSpan.FromMilliseconds(50));

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
        public async Task Execute_NoRequiredAmplifiers_FailsClosed()
        {
            var comm = new RecordingCommClient();
            var variables = new TrackApplicationVariables();
            var step = new TrackPowerEnableStep(comm, variables, Array.Empty<byte>());

            var result = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Error, result.Kind);
        }

        [Fact]
        public async Task Execute_Protocol01_NoTrackPowerSupport_FailsClosedWithoutWrite()
        {
            var comm = new RecordingCommClient();
            // Protocol 0x01 is Phase-1 compatible but does NOT support HR_TRACK_POWER.
            var variables = VariablesWith(RequiredAmps, TrackPowerOnState,
                fwVersionPatchProtocol: TrackAmplifierRegisters.Phase1ProtocolVersion);
            var step = new TrackPowerEnableStep(comm, variables, RequiredAmps);

            var result = await step.ExecuteAsync(null, CancellationToken.None);

            Assert.Equal(InitStepResultKind.Error, result.Kind);
            Assert.Contains("HR_TRACK_POWER", result.ErrorMessage);
            // No HR_TRACK_POWER write was issued for the unsupported firmware.
            Assert.DoesNotContain(comm.Sent,
                m => m.Data.Length >= 4 && m.Data[3] == TrackAmplifierRegisters.TrackPower);
        }

        [Fact]
        public void SupportsTrackPower_TrueOnlyForProtocol02OrHigher()
        {
            Assert.False(TrackAmplifierRegisters.SupportsTrackPower(0x0000)); // absent/legacy
            Assert.False(TrackAmplifierRegisters.SupportsTrackPower(0x0001)); // Phase-1 base
            Assert.True(TrackAmplifierRegisters.SupportsTrackPower(0x0002));  // track-power capable
            Assert.True(TrackAmplifierRegisters.SupportsTrackPower(0x0003));  // future
        }

        [Fact]
        public void IsTrackPowerOn_RequiresInhibitedClearEnableActiveAndConfigOk()
        {
            var item = new TrackAmplifierItem
            {
                HoldingReg = new ushort[TrackAmplifierRegisters.Count]
            };

            item.HoldingReg[TrackAmplifierRegisters.State] = 0;
            Assert.False(TrackPowerEnableStep.IsTrackPowerOn(item));

            item.HoldingReg[TrackAmplifierRegisters.State] =
                (ushort)(TrackAmplifierRegisters.EnableActiveBit | TrackAmplifierRegisters.ConfigOkBit);
            Assert.True(TrackPowerEnableStep.IsTrackPowerOn(item));

            item.HoldingReg[TrackAmplifierRegisters.State] |= TrackAmplifierRegisters.InhibitedBit;
            Assert.False(TrackPowerEnableStep.IsTrackPowerOn(item));
        }
    }
}
