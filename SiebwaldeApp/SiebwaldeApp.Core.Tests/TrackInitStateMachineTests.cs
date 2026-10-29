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
    /// Tests for the Ovaaltje2 <see cref="TrackInitStateMachine"/>: seven-stage grouping, automatic
    /// fail-closed progression, manual step/retry/backtrack/abort, and the operator-gated Stage 6
    /// (track power). Fake steps and a fake comm client keep the tests deterministic; the SAME step
    /// contract and stage actions are exercised (no second initialization stack).
    /// </summary>
    public class TrackInitStateMachineTests
    {
#pragma warning disable CS0067
        private sealed class FakeTrackCommClient : ITrackCommClient
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

        private sealed class FakeStep : IInitializationStep
        {
            private readonly Func<InitStepResult> _result;

            public FakeStep(string name, Func<InitStepResult> result)
            {
                Name = name;
                _result = result;
            }

            public string Name { get; }

            public Task<InitStepResult> ExecuteAsync(ReceivedMessage? lastMessage, CancellationToken cancellationToken)
                => Task.FromResult(_result());
        }

        /// <summary>Builds the production-shaped chain: connect/query/precondition -> reset/upload/detect/flash -> init -> setpoints -> enable.</summary>
        private static IInitializationStep[] HappyChain()
        {
            return new IInitializationStep[]
            {
                new FakeStep("ConnectToEthernetTarget", () => InitStepResult.Next("QueryControllerVersion")),
                new FakeStep("QueryControllerVersion", () => InitStepResult.Next("GlobalStopPrecondition")),
                new FakeStep("GlobalStopPrecondition", () => InitStepResult.Next("ResetAllSlaves")),
                new FakeStep("ResetAllSlaves", () => InitStepResult.Next("DataUpload")),
                new FakeStep("DataUpload", () => InitStepResult.Next("DetectSlaves")),
                new FakeStep("DetectSlaves", () => InitStepResult.Next("FlashFwTrackamplifiers")),
                new FakeStep("FlashFwTrackamplifiers", () => InitStepResult.Next("InitTrackamplifiers")),
                new FakeStep("InitTrackamplifiers", () => InitStepResult.Next("SetDefaultPwmSetpoints")),
                new FakeStep("SetDefaultPwmSetpoints", () => InitStepResult.Next("EnableTrackamplifiers")),
                new FakeStep("EnableTrackamplifiers", () => InitStepResult.Completed()),
            };
        }

        private static (TrackInitStateMachine machine, FakeTrackCommClient comm) Build(
            params IInitializationStep[] steps)
        {
            var comm = new FakeTrackCommClient();
            var variables = new TrackApplicationVariables();
            var machine = new TrackInitStateMachine(
                comm,
                variables,
                steps,
                requiredAmps: () => new byte[] { 1, 3, 4, 6 });
            return (machine, comm);
        }

        private static TrackInitStageStatus StageOf(TrackInitStateMachine machine, TrackInitStage stage)
            => machine.Stages.First(s => s.Stage == stage);

        [Fact]
        public async Task Automatic_RunsStepStages_AndCompletes()
        {
            var (machine, _) = Build(HappyChain());

            await machine.RunAutomaticAsync(CancellationToken.None);

            Assert.True(machine.IsCompleted, $"RunStatus={machine.RunStatus}, CurrentStage={machine.CurrentStage}");
            Assert.Equal(InitializationStatus.Completed, machine.RunStatus);
            Assert.Equal(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.ControllerConnection).State);
            Assert.Equal(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.FirmwareAddress).State);
            Assert.Equal(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.AmplifierStandby).State);
            // Stage 5 (global ready) and Stage 4 (configure + validate) are host-finalized: the
            // step runner never auto-passes them (they require the whole required amp set).
            Assert.Equal(TrackInitStageState.Running, StageOf(machine, TrackInitStage.GlobalReady).State);
            Assert.Equal(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.NormalOperation).State);
            Assert.Equal(TrackInitStageState.Pending, StageOf(machine, TrackInitStage.ConfigureValidate).State);
            Assert.Equal(TrackInitStageState.Pending, StageOf(machine, TrackInitStage.AmpPowerEnable).State);
        }

        [Fact]
        public async Task Automatic_StepError_FailsClosed_AtThatStage()
        {
            var steps = new IInitializationStep[]
            {
                new FakeStep("ConnectToEthernetTarget", () => InitStepResult.Next("QueryControllerVersion")),
                new FakeStep("QueryControllerVersion", () => InitStepResult.Next("GlobalStopPrecondition")),
                new FakeStep("GlobalStopPrecondition", () => InitStepResult.Next("ResetAllSlaves")),
                new FakeStep("ResetAllSlaves", () => InitStepResult.Next("DataUpload")),
                new FakeStep("DataUpload", () => InitStepResult.Error("detect failed")),
                new FakeStep("DetectSlaves", () => InitStepResult.Next("FlashFwTrackamplifiers")),
            };
            var (machine, _) = Build(steps);

            await machine.RunAutomaticAsync(CancellationToken.None);

            Assert.True(machine.IsFailed);
            Assert.Equal(TrackInitStageState.Failed, StageOf(machine, TrackInitStage.FirmwareAddress).State);
            // Nothing after the failed stage advanced.
            Assert.Equal(TrackInitStageState.Pending, StageOf(machine, TrackInitStage.AmplifierStandby).State);
        }

        [Fact]
        public async Task Manual_StepAsync_AdvancesOneStageAtATime()
        {
            var (machine, _) = Build(HappyChain());

            var first = await machine.StepAsync(CancellationToken.None);
            Assert.Equal(TrackInitStageState.Passed, first);
            Assert.Equal(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.ControllerConnection).State);
            Assert.Equal(TrackInitStage.FirmwareAddress, machine.CurrentStage);

            var second = await machine.StepAsync(CancellationToken.None);
            Assert.Equal(TrackInitStageState.Passed, second);
            Assert.Equal(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.FirmwareAddress).State);
            Assert.Equal(TrackInitStage.AmplifierStandby, machine.CurrentStage);
        }

        [Fact]
        public async Task Retry_AfterFailure_ReRunsCurrentStage()
        {
            var calls = 0;
            var steps = new IInitializationStep[]
            {
                new FakeStep("ConnectToEthernetTarget", () => InitStepResult.Next("QueryControllerVersion")),
                new FakeStep("QueryControllerVersion", () => InitStepResult.Next("GlobalStopPrecondition")),
                new FakeStep("GlobalStopPrecondition", () => InitStepResult.Next("ResetAllSlaves")),
                new FakeStep("ResetAllSlaves", () =>
                {
                    calls++;
                    return calls < 2 ? InitStepResult.Error("transient") : InitStepResult.Next("DataUpload");
                }),
                new FakeStep("DataUpload", () => InitStepResult.Next("InitTrackamplifiers")),
            };
            var (machine, _) = Build(steps);

            var outcome = await machine.StepAsync(CancellationToken.None); // stage 1 passes
            Assert.Equal(TrackInitStageState.Passed, outcome);

            var failed = await machine.StepAsync(CancellationToken.None); // stage 2 fails (ResetAllSlaves errors)
            Assert.Equal(TrackInitStageState.Failed, failed);
            Assert.True(machine.IsFailed);

            var retried = await machine.RetryAsync(CancellationToken.None); // re-run stage 2 -> ResetAllSlaves Nexts
            Assert.Equal(TrackInitStageState.Passed, retried);
            Assert.Equal(2, calls);
        }

        [Fact]
        public void Backtrack_MovesPointerBack_AndMarksPending()
        {
            var (machine, _) = Build(HappyChain());

            // Advance two stages manually.
            _ = machine.StepAsync(CancellationToken.None).GetAwaiter().GetResult();
            _ = machine.StepAsync(CancellationToken.None).GetAwaiter().GetResult();
            Assert.Equal(TrackInitStage.AmplifierStandby, machine.CurrentStage);

            machine.Backtrack();
            Assert.Equal(TrackInitStage.FirmwareAddress, machine.CurrentStage);
            Assert.Equal(TrackInitStageState.Pending, StageOf(machine, TrackInitStage.FirmwareAddress).State);
        }

        [Fact]
        public async Task TrackPower_NotAuthorized_IsBlocked()
        {
            var (machine, _) = Build(HappyChain());

            var enabled = await machine.EnableTrackPowerAsync(CancellationToken.None);

            Assert.False(enabled);
            Assert.False(machine.TrackPowerOn);
            Assert.Equal(TrackInitStageState.Blocked, StageOf(machine, TrackInitStage.AmpPowerEnable).State);
        }

        [Fact]
        public async Task TrackPower_Authorized_WritesAndVerifies_BecomesOn()
        {
            var comm = new FakeTrackCommClient();

            // Simulate the amplifiers reporting TRACK_POWER_ON via HR_STATE
            // (inhibited==0 + enable_active + config_ok) and advertising track-power support
            // (HR22 protocol 0x02) before the step polls.
            var variables = new TrackApplicationVariables();
            foreach (var amp in new byte[] { 1, 3, 4, 6 })
            {
                variables.trackAmpItems[amp].HoldingReg[TrackAmplifierRegisters.State] =
                    (ushort)(TrackAmplifierRegisters.EnableActiveBit | TrackAmplifierRegisters.ConfigOkBit);
                variables.trackAmpItems[amp].HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol] =
                    TrackAmplifierRegisters.TrackPowerProtocolVersion;
            }

            var machine = new TrackInitStateMachine(
                comm,
                variables,
                HappyChain(),
                requiredAmps: () => new byte[] { 1, 3, 4, 6 });

            // Stage 5 (GlobalReady) must be Passed before track power is authorized/energized.
            machine.MarkStagePassed(TrackInitStage.GlobalReady);
            machine.AuthorizeTrackPowerEnable();
            var enabled = await machine.EnableTrackPowerAsync(CancellationToken.None);

            Assert.True(enabled);
            Assert.True(machine.TrackPowerOn);
            Assert.Equal(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.AmpPowerEnable).State);

            // The Stage 6 action wrote HR_TRACK_POWER (register 23) to each required amplifier.
            var trackPowerWrites = comm.Sent
                .Where(m => m.Command == TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH
                            && m.Data.Length >= 4 && m.Data[3] == TrackAmplifierRegisters.TrackPower)
                .ToArray();
            Assert.Equal(4, trackPowerWrites.Length);
            Assert.All(trackPowerWrites, m => Assert.Equal((byte)0xAA, m.Data[1]));
        }

        [Fact]
        public async Task GlobalReady_NotAutoPassed_AndHostFailedPerAmpReadiness_BlocksTrackPower()
        {
            // Amp 3 is not ready (Phase1Failure); amps 1/4/6 are ready.
            var variables = new TrackApplicationVariables();
            foreach (var amp in new byte[] { 1, 4, 6 })
            {
                variables.trackAmpItems[amp].ConfigurationVerified = true;
            }

            variables.trackAmpItems[3].Phase1Failure = "incompatible firmware";

            var machine = new TrackInitStateMachine(
                new FakeTrackCommClient(),
                variables,
                HappyChain(),
                requiredAmps: () => new byte[] { 1, 3, 4, 6 });

            await machine.RunAutomaticAsync(CancellationToken.None);

            // Stage 5 is host-finalized: the step runner must NOT auto-pass it.
            Assert.Equal(TrackInitStageState.Running, StageOf(machine, TrackInitStage.GlobalReady).State);

            // The host marks Stage 5 failed because amp 3 is not ready, with a per-amp reason.
            machine.MarkStageFailed(TrackInitStage.GlobalReady, "Not all required amplifiers are ready.");
            var stage5 = StageOf(machine, TrackInitStage.GlobalReady);
            Assert.Equal(TrackInitStageState.Failed, stage5.State);
            Assert.Contains(stage5.Amps, a => a.SlaveNumber == 3 && !a.Ok);
            Assert.Contains(stage5.Amps, a => a.SlaveNumber == 3 && a.Detail.Contains("incompatible"));

            // Stage 6 remains blocked (track power is gated and never auto-energized).
            var enabled = await machine.EnableTrackPowerAsync(CancellationToken.None);
            Assert.False(enabled);
            Assert.Equal(TrackInitStageState.Blocked, StageOf(machine, TrackInitStage.AmpPowerEnable).State);
        }

        [Fact]
        public async Task GlobalReady_HostPassed_WhenAllRequiredAmpsReady()
        {
            var variables = new TrackApplicationVariables();
            foreach (var amp in new byte[] { 1, 3, 4, 6 })
            {
                variables.trackAmpItems[amp].ConfigurationVerified = true;
                // HR_STATE reflects READY (group-ready accepted).
                variables.trackAmpItems[amp].HoldingReg[TrackAmplifierRegisters.State] =
                    (ushort)(TrackAmplifierRegisters.ReadyState | TrackAmplifierRegisters.EnableActiveBit
                             | TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit);
            }

            var machine = new TrackInitStateMachine(
                new FakeTrackCommClient(),
                variables,
                HappyChain(),
                requiredAmps: () => new byte[] { 1, 3, 4, 6 });

            await machine.RunAutomaticAsync(CancellationToken.None);
            Assert.Equal(TrackInitStageState.Running, StageOf(machine, TrackInitStage.GlobalReady).State);

            machine.MarkStagePassed(TrackInitStage.GlobalReady);
            var stage5 = StageOf(machine, TrackInitStage.GlobalReady);
            Assert.Equal(TrackInitStageState.Passed, stage5.State);
            Assert.Equal(4, stage5.Amps.Count);
            Assert.All(stage5.Amps, a => Assert.True(a.Ok));
        }

        [Fact]
        public async Task TrackPower_RequiresGlobalReadyPassed_EvenWhenAuthorized()
        {
            var comm = new FakeTrackCommClient();
            var variables = new TrackApplicationVariables();
            foreach (var amp in new byte[] { 1, 3, 4, 6 })
            {
                variables.trackAmpItems[amp].HoldingReg[TrackAmplifierRegisters.State] =
                    (ushort)(TrackAmplifierRegisters.EnableActiveBit | TrackAmplifierRegisters.ConfigOkBit);
                variables.trackAmpItems[amp].HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol] =
                    TrackAmplifierRegisters.TrackPowerProtocolVersion;
            }

            var machine = new TrackInitStateMachine(
                comm,
                variables,
                HappyChain(),
                requiredAmps: () => new byte[] { 1, 3, 4, 6 });

            await machine.RunAutomaticAsync(CancellationToken.None);

            // Stage 5 failed (host-finalized): even with operator authorization, track power is
            // refused (defense-in-depth R2 precondition on GlobalReady Passed).
            machine.MarkStageFailed(TrackInitStage.GlobalReady, "GROUP_READY rejected by amplifier 3.");
            machine.AuthorizeTrackPowerEnable();

            var enabled = await machine.EnableTrackPowerAsync(CancellationToken.None);
            Assert.False(enabled);
            Assert.False(machine.TrackPowerOn);
            Assert.Equal(TrackInitStageState.Blocked, StageOf(machine, TrackInitStage.AmpPowerEnable).State);
            Assert.Empty(comm.Sent); // no HR_TRACK_POWER bit-0 write issued

            // After Stage 5 Passed, track power proceeds.
            machine.MarkStagePassed(TrackInitStage.GlobalReady);
            enabled = await machine.EnableTrackPowerAsync(CancellationToken.None);
            Assert.True(enabled);
            Assert.True(machine.TrackPowerOn);
            Assert.NotEmpty(comm.Sent);
        }
    }
}
