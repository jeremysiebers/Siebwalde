using System;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Bounded-lifetime tests for the Stage-2 <see cref="DetectSlavesStep"/> (command 101,
    /// EXEC_MBUS_STATE_SLAVE_DETECT). A controller whose slave detection never completes (the F2
    /// post-REARM lease-expired stall) must not leave the initialization blocked indefinitely: the
    /// step now fails closed when <c>MBUS_STATE_SLAVE_DETECT / DONE</c> is not acknowledged within
    /// its window, and a late DONE is never treated as authorization to resume.
    /// </summary>
    public class DetectSlavesStepTests
    {
#pragma warning disable CS0067
        private sealed class RecordingCommClient : ITrackCommClient
        {
            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;
            public int SendCount { get; private set; }

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
            {
                SendCount++;
                return Task.CompletedTask;
            }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
#pragma warning restore CS0067

        private static ReceivedMessage DetectDone =>
            new(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_DETECT, TaskStates.DONE, 0);

        private static TrackApplicationVariables DetectedVariables(params ushort[] slaves)
        {
            var variables = new TrackApplicationVariables();
            foreach (var slave in slaves)
            {
                variables.trackAmpItems[slave].SlaveDetected = 1;
            }
            return variables;
        }

        [Fact]
        public async Task Timeout_WithoutAck_FailsClosed()
        {
            var comm = new RecordingCommClient();
            var variables = new TrackApplicationVariables();
            var step = new DetectSlavesStep(comm, variables, timeout: TimeSpan.FromMilliseconds(40));

            var first = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Continue, first.Kind);
            Assert.Equal(1, comm.SendCount); // command 101 was transmitted once

            await Task.Delay(80); // exceed the window

            var result = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Error, result.Kind);
            Assert.Contains("timed out", result.ErrorMessage);
        }

        [Fact]
        public async Task LateDone_DoesNotResume_FailsClosed()
        {
            var comm = new RecordingCommClient();
            var variables = DetectedVariables(1, 2, 3);
            var step = new DetectSlavesStep(comm, variables, timeout: TimeSpan.FromMilliseconds(40));

            await step.ExecuteAsync(null, CancellationToken.None); // substate 0: send
            await Task.Delay(80); // exceed the window

            // A late DONE must NOT resume the step: the timeout is authoritative, even when enough
            // slaves were previously detected.
            var result = await step.ExecuteAsync(DetectDone, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Error, result.Kind);
        }

        [Fact]
        public async Task DoneWithinWindow_WithEnoughSlaves_AdvancesToFlashFwTrackamplifiers()
        {
            var comm = new RecordingCommClient();
            var variables = DetectedVariables(1, 2, 3);
            var step = new DetectSlavesStep(comm, variables, timeout: TimeSpan.FromSeconds(5));

            await step.ExecuteAsync(null, CancellationToken.None); // substate 0: send

            // The step waits 2 s for the SLAVEINFO readbacks to populate before counting.
            var result = await step.ExecuteAsync(DetectDone, CancellationToken.None);
            Assert.Equal(InitStepResultKind.NextStep, result.Kind);
            Assert.Equal("FlashFwTrackamplifiers", result.NextStepName);
        }

        [Fact]
        public async Task DoneWithinWindow_TooFewSlaves_RetriesDetection()
        {
            var comm = new RecordingCommClient();
            var variables = DetectedVariables(1); // only one slave: below the 3-slave threshold
            var step = new DetectSlavesStep(comm, variables, timeout: TimeSpan.FromSeconds(5));

            await step.ExecuteAsync(null, CancellationToken.None); // substate 0: send

            var result = await step.ExecuteAsync(DetectDone, CancellationToken.None);
            Assert.Equal(InitStepResultKind.NextStep, result.Kind);
            Assert.Equal("RecoverSlaves", result.NextStepName);
        }
    }
}
