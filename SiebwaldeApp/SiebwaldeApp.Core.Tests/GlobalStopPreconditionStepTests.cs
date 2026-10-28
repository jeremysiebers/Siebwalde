using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Offline (software/protocol only) tests for the startup global-stop precondition step and the
    /// pure re-arm path of <see cref="TrackAmplifierReleaseAuthority"/>. No hardware, no network.
    ///
    /// These verify the three required behaviors:
    ///   1. A latched 111 state (1/2) gates the init (no advance to ResetAllSlaves/107) until the
    ///      operator recovery runs; a disarmed/armed state (0/3) proceeds immediately.
    ///   2. The recovery emits REARM_GLOBAL_STOP (112) only after physical-stop-clear is true;
    ///      with physicalStopInputsClear=false it fails closed (no 112).
    ///   3. No HR0 write (EXEC_MBUS_SLAVE_DATA_EXCH/108) and no ENABLE (HR1=0x8000) is emitted in
    ///      the query/recovery path.
    /// </summary>
    public class GlobalStopPreconditionStepTests
    {
        private sealed class RecordingCommClient : ITrackCommClient
        {
            public readonly List<SendMessage> Sent = new();

            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
            {
                Sent.Add(message);
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync() => default;
        }

        private static ReceivedMessage Status(ushort state)
            => new(TaskId.CONTROLLER, TrackCommand.GLOBAL_STOP_STATE, TaskStates.DONE, state);

        // ------------------------------------------------------------------
        // Required behavior 1: gating on the observed 111 state
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(GlobalStopStatusMessage.Disarmed)] // 0
        [InlineData(GlobalStopStatusMessage.Armed)]    // 3
        public async Task NotLatched_ProceedsImmediately_WithoutRearm(ushort state)
        {
            var comm = new RecordingCommClient();
            var step = new GlobalStopPreconditionStep(comm); // fail-closed gates

            // subState 0: send the consume-only 111 query.
            var first = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Continue, first.Kind);
            Assert.Equal(new[] { TrackCommand.GLOBAL_STOP_STATE }, comm.Sent.Select(m => m.Command).ToArray());

            // subState 1: the observed state is not latched -> advance immediately.
            var advanced = await step.ExecuteAsync(Status(state), CancellationToken.None);
            Assert.Equal(InitStepResultKind.NextStep, advanced.Kind);
            Assert.Equal("ResetAllSlaves", advanced.NextStepName);

            // No re-arm and no reset was issued by this step.
            Assert.Equal(new[] { TrackCommand.GLOBAL_STOP_STATE }, comm.Sent.Select(m => m.Command).ToArray());
        }

        [Theory]
        [InlineData(GlobalStopStatusMessage.LatchedHostLoss)]       // 1
        [InlineData(GlobalStopStatusMessage.LatchedHostInitiated)]  // 2
        public async Task Latched_DoesNotProceed_UntilOperatorRecovery(ushort state)
        {
            var comm = new RecordingCommClient();
            var step = new GlobalStopPreconditionStep(comm); // operator + physical gates both fail-closed

            await step.ExecuteAsync(null, CancellationToken.None); // send 111 query
            var gated = await step.ExecuteAsync(Status(state), CancellationToken.None);

            // Latched: the step must NOT advance to ResetAllSlaves.
            Assert.Equal(InitStepResultKind.Continue, gated.Kind);

            // Without the operator action, recovery never emits 112 (or 107).
            var waiting = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Continue, waiting.Kind);

            Assert.DoesNotContain(TrackCommand.REARM_GLOBAL_STOP, comm.Sent.Select(m => m.Command));
            Assert.DoesNotContain(TrackCommand.EXEC_MBUS_STATE_RESET, comm.Sent.Select(m => m.Command));
        }

        // ------------------------------------------------------------------
        // Required behavior 2: 112 only after physical-stop-clear
        // ------------------------------------------------------------------

        [Fact]
        public async Task OperatorPermitted_ButPhysicalStopNotClear_FailsClosed_No112()
        {
            var comm = new RecordingCommClient();
            var step = new GlobalStopPreconditionStep(
                comm,
                operatorPermitRecovery: () => true,
                physicalStopInputsClear: () => false);

            await step.ExecuteAsync(null, CancellationToken.None); // 111 query
            await step.ExecuteAsync(Status(GlobalStopStatusMessage.LatchedHostLoss), CancellationToken.None); // latched

            var blocked = await step.ExecuteAsync(null, CancellationToken.None);

            Assert.Equal(InitStepResultKind.Continue, blocked.Kind);
            Assert.DoesNotContain(TrackCommand.REARM_GLOBAL_STOP, comm.Sent.Select(m => m.Command));
        }

        [Fact]
        public async Task OperatorPermitted_AndPhysicalStopClear_Emits112Only_NoHr0NoEnable()
        {
            var comm = new RecordingCommClient();
            var step = new GlobalStopPreconditionStep(
                comm,
                operatorPermitRecovery: () => true,
                physicalStopInputsClear: () => true);

            await step.ExecuteAsync(null, CancellationToken.None); // 111 query
            await step.ExecuteAsync(Status(GlobalStopStatusMessage.LatchedHostLoss), CancellationToken.None); // latched

            var recovered = await step.ExecuteAsync(null, CancellationToken.None); // emit 112
            Assert.Equal(InitStepResultKind.Continue, recovered.Kind);

            // The recovery emitted REARM_GLOBAL_STOP (112) with an empty payload (no HR0/HR1 write).
            var rearm = Assert.Single(comm.Sent, m => m.Command == TrackCommand.REARM_GLOBAL_STOP);
            Assert.Empty(rearm.Data);

            // Re-query then observe armed -> advance.
            var requery = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Continue, requery.Kind);
            var advanced = await step.ExecuteAsync(Status(GlobalStopStatusMessage.Armed), CancellationToken.None);
            Assert.Equal(InitStepResultKind.NextStep, advanced.Kind);
            Assert.Equal("ResetAllSlaves", advanced.NextStepName);

            // Required behavior 3: the whole path emitted only 111 and 112 — no HR0 write (108) and
            // no ENABLE (105 / HR1=0x8000 register write).
            var commands = comm.Sent.Select(m => m.Command).ToArray();
            Assert.DoesNotContain(TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, commands); // HR0/HR1 register write
            Assert.DoesNotContain(TrackCommand.EXEC_MBUS_STATE_SLAVE_ENABLE, commands); // MBUS enable
            Assert.Equal(
                new[] { TrackCommand.GLOBAL_STOP_STATE, TrackCommand.REARM_GLOBAL_STOP, TrackCommand.GLOBAL_STOP_STATE },
                commands);
        }

        // ------------------------------------------------------------------
        // Release authority: pure re-arm primitive (112-only, no HR0)
        // ------------------------------------------------------------------

        [Fact]
        public async Task RearmGlobalStopAsync_PhysicalStopNotClear_FailsClosed_No112()
        {
            var comm = new RecordingCommClient();
            var authority = new TrackAmplifierReleaseAuthority(comm, physicalStopInputsClear: () => false);

            var result = await authority.RearmGlobalStopAsync();

            Assert.Equal(AmplifierReleaseResultKind.BlockedStopInputsNotClear, result.Kind);
            Assert.Empty(comm.Sent);
        }

        [Fact]
        public async Task RearmGlobalStopAsync_PhysicalStopClear_Emits112Only_NoHr0()
        {
            var comm = new RecordingCommClient();
            var authority = new TrackAmplifierReleaseAuthority(comm, physicalStopInputsClear: () => true);

            var result = await authority.RearmGlobalStopAsync();

            Assert.Equal(AmplifierReleaseResultKind.Released, result.Kind);
            var sent = Assert.Single(comm.Sent);
            Assert.Equal(TrackCommand.REARM_GLOBAL_STOP, sent.Command);
            Assert.Empty(sent.Data); // no HR0 movement command, no HR1 enable
        }
    }
}
