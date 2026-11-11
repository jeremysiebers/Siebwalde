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
    /// Focused unit tests for the recovery-hold transition (<see cref="TrackInitStateMachine.EnterRecoveryHold"/>)
    /// and the revalidation semantics it enables, plus the desired-setpoint neutralization on a fault
    /// latch. These drive the REAL production latch path (<see cref="GlobalStopController.Latch"/> raises
    /// the same <c>Latched</c> event the <see cref="GlobalStopMonitor"/> fires on comm/host-authority loss)
    /// and the REAL stage-state transitions — no synthetic disconnect flag is flipped.
    /// </summary>
    public class RecoveryHoldTests
    {
#pragma warning disable CS0067
        private sealed class FakeTrackCommClient : ITrackCommClient
        {
            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

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

        private static readonly byte[] RequiredAmps = { 1, 3, 4, 6 };

        /// <summary>Production-shaped chain (same stage grouping the runtime host composes).</summary>
        private static IInitializationStep[] HappyChain() => new IInitializationStep[]
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

        private static (TrackInitStateMachine machine, TrackApplicationVariables variables) Build()
        {
            var comm = new FakeTrackCommClient();
            var variables = new TrackApplicationVariables();
            var machine = new TrackInitStateMachine(comm, variables, HappyChain(), () => RequiredAmps);
            return (machine, variables);
        }

        private static TrackInitStageStatus StageOf(TrackInitStateMachine machine, TrackInitStage stage)
            => machine.Stages.First(s => s.Stage == stage);

        /// <summary>Runs a machine to the non-energized <see cref="InitializationStatus.StoppedAtGlobalReady"/>.</summary>
        private static async Task RunToStoppedAtGlobalReadyAsync(TrackInitStateMachine machine)
        {
            await machine.RunAutomaticAsync(CancellationToken.None);
            machine.MarkStagePassed(TrackInitStage.ConfigureValidate);
            machine.MarkStagePassed(TrackInitStage.GlobalReady);
        }

        // ------------------------------------------------------------------
        // EnterRecoveryHold
        // ------------------------------------------------------------------

        [Fact]
        public async Task EnterRecoveryHold_CommFault_InvalidatesStages4567_Retains23_ResetsPointer()
        {
            var (machine, variables) = Build();
            await RunToStoppedAtGlobalReadyAsync(machine);
            Assert.Equal(InitializationStatus.StoppedAtGlobalReady, machine.RunStatus);

            // The same production latch the monitor fires on Pic32Loss (comm loss).
            variables.GlobalStop.Latch(GlobalStopReason.Pic32Lost);
            machine.EnterRecoveryHold();

            Assert.True(machine.IsRecoveryHold);
            Assert.Equal(InitializationStatus.RecoveryHold, machine.RunStatus);
            Assert.False(machine.IsCompleted);
            Assert.False(machine.IsFailed);

            // Stages 4/5/6/7 invalidated (their evidence is stale).
            Assert.Equal(TrackInitStageState.Pending, StageOf(machine, TrackInitStage.ConfigureValidate).State);
            Assert.Equal(TrackInitStageState.Pending, StageOf(machine, TrackInitStage.GlobalReady).State);
            Assert.Equal(TrackInitStageState.Pending, StageOf(machine, TrackInitStage.AmpPowerEnable).State);
            Assert.Equal(TrackInitStageState.Pending, StageOf(machine, TrackInitStage.NormalOperation).State);

            // Stages 2/3 RETAINED (still Passed, but treated as unconfirmed until revalidated).
            Assert.Equal(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.FirmwareAddress).State);
            Assert.Equal(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.AmplifierStandby).State);

            // Pointer reset to Stage 1; energize/re-arm withheld.
            Assert.Equal(TrackInitStage.ControllerConnection, machine.CurrentStage);
            Assert.False(machine.TrackPowerAuthorized);
            Assert.False(machine.TrackPowerOn);
        }

        [Theory]
        [InlineData(GlobalStopReason.Operator)]
        [InlineData(GlobalStopReason.Koploper)]
        public async Task EnterRecoveryHold_OperatorOrKoploperStop_IsNoOp(GlobalStopReason reason)
        {
            var (machine, variables) = Build();
            await RunToStoppedAtGlobalReadyAsync(machine);

            variables.GlobalStop.Latch(reason);
            machine.EnterRecoveryHold();

            // Only comm/host-authority faults invalidate stage evidence.
            Assert.Equal(InitializationStatus.StoppedAtGlobalReady, machine.RunStatus);
            Assert.False(machine.IsRecoveryHold);
            Assert.Equal(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.ConfigureValidate).State);
            Assert.Equal(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.GlobalReady).State);
        }

        [Fact]
        public void EnterRecoveryHold_WhenIdle_IsNoOp()
        {
            var (machine, variables) = Build();

            variables.GlobalStop.Latch(GlobalStopReason.Pic32HostLoss);
            machine.EnterRecoveryHold();

            // The run never started (Idle), so there is no Running/Completed evidence to invalidate.
            Assert.Equal(InitializationStatus.Idle, machine.RunStatus);
            Assert.False(machine.IsRecoveryHold);
        }

        [Fact]
        public async Task EnterRecoveryHold_ViaLatchedEvent_TransitionsToRecoveryHold()
        {
            var (machine, variables) = Build();
            // Mirror the host wiring: the latch event drives EnterRecoveryHold.
            variables.GlobalStop.Latched += machine.EnterRecoveryHold;
            await RunToStoppedAtGlobalReadyAsync(machine);

            variables.GlobalStop.Latch(GlobalStopReason.Pic32HostInitiated);

            Assert.Equal(InitializationStatus.RecoveryHold, machine.RunStatus);
        }

        // ------------------------------------------------------------------
        // Revalidation semantics (RunAutomaticAsync is the primitive RevalidateAsync reuses)
        // ------------------------------------------------------------------

        [Fact]
        public async Task Revalidate_AfterRecoveryHold_ReturnsToStoppedAtGlobalReady()
        {
            var (machine, variables) = Build();
            await RunToStoppedAtGlobalReadyAsync(machine);

            variables.GlobalStop.Latch(GlobalStopReason.Pic32Lost);
            machine.EnterRecoveryHold();
            Assert.Equal(InitializationStatus.RecoveryHold, machine.RunStatus);

            // The controller comes back and the operator re-arms (physical stop clear confirmed).
            Assert.True(variables.GlobalStop.ReArm());
            Assert.Equal(GlobalStopState.Armed, variables.GlobalStop.State);

            // Re-run the automatic path (re-issues Stage 1, re-confirms Stages 2/3, reaches
            // NormalOperation) and finalize Stages 4/5 as the host's RevalidateAsync does.
            await machine.RunAutomaticAsync(CancellationToken.None);
            machine.MarkStagePassed(TrackInitStage.ConfigureValidate);
            machine.MarkStagePassed(TrackInitStage.GlobalReady);

            Assert.Equal(InitializationStatus.StoppedAtGlobalReady, machine.RunStatus);
            for (var stage = TrackInitStage.ControllerConnection; stage <= TrackInitStage.GlobalReady; stage++)
            {
                Assert.Equal(TrackInitStageState.Passed, StageOf(machine, stage).State);
            }

            // Stage 6 is gated; NormalOperation is not Passed.
            Assert.NotEqual(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.AmpPowerEnable).State);
            Assert.NotEqual(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.NormalOperation).State);
        }

        [Fact]
        public async Task Revalidate_MissingAmplifier_DoesNotReachStoppedAtGlobalReady()
        {
            var (machine, variables) = Build();
            await RunToStoppedAtGlobalReadyAsync(machine);

            variables.GlobalStop.Latch(GlobalStopReason.Pic32Lost);
            machine.EnterRecoveryHold();

            // Model the revalidation's fresh readback finding amplifier 6 missing/incompatible.
            variables.trackAmpItems[6].ConfigurationVerified = false;
            variables.trackAmpItems[6].Phase1Failure = "not detected";

            await machine.RunAutomaticAsync(CancellationToken.None);
            // The host's RevalidateAsync finalizes Stages 4/5 fail-closed when any amp is not ready.
            machine.MarkStageFailed(TrackInitStage.ConfigureValidate, "One or more required amplifiers did not complete configuration.");
            machine.MarkStageFailed(TrackInitStage.GlobalReady, "Not all required amplifiers are ready.");

            Assert.NotEqual(InitializationStatus.StoppedAtGlobalReady, machine.RunStatus);
            Assert.Equal(TrackInitStageState.Failed, StageOf(machine, TrackInitStage.ConfigureValidate).State);
            Assert.Equal(TrackInitStageState.Failed, StageOf(machine, TrackInitStage.GlobalReady).State);
            Assert.NotEqual(TrackInitStageState.Passed, StageOf(machine, TrackInitStage.AmpPowerEnable).State);
        }

        // ------------------------------------------------------------------
        // Desired-setpoint neutralization on fault (TrackApplicationVariables)
        // ------------------------------------------------------------------

        [Fact]
        public void FaultLatch_NonSuppressed_NeutralizesDesiredSetpoint()
        {
            var variables = new TrackApplicationVariables();
            // No movement-permission set: the gate is disabled, so the non-neutral value is queued.
            Assert.True(variables.SetDesiredAmplifierControl(1, 500, false));
            Assert.Equal(500, variables.PendingWrites[1].Hr0Value & 0x03FF);

            // A host-authority-loss latch (non-suppressed) clears + re-queues neutral.
            variables.GlobalStop.Latch(GlobalStopReason.Pic32HostLoss);

            Assert.True(variables.PendingWrites.ContainsKey(1));
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, variables.PendingWrites[1].Hr0Value & 0x03FF);
        }

        [Fact]
        public void FaultLatch_Pic32Lost_SuppressesAndClearsDesiredSetpoint()
        {
            var variables = new TrackApplicationVariables();
            Assert.True(variables.SetDesiredAmplifierControl(1, 500, false));

            // Pic32Lost suppresses slave writes: the stale desired value is dropped, not re-applied.
            variables.GlobalStop.Latch(GlobalStopReason.Pic32Lost);

            Assert.Empty(variables.PendingWrites);
            Assert.True(variables.GlobalStop.SuppressSlaveWrites);
        }
    }
}
