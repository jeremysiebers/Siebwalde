using System;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Bounded-lifetime tests for the Stage-2 <see cref="DataUploadStep"/>. A non-responsive
    /// controller must not leave the initialization blocked indefinitely: the step now fails closed
    /// when command 106 (EXEC_MBUS_STATE_START_DATA_UPLOAD) is not acknowledged within its window,
    /// and a late DONE is never treated as authorization to resume.
    /// </summary>
    public class DataUploadStepTests
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

        private static ReceivedMessage DataUploadDone =>
            new(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD, TaskStates.DONE, 0);

        [Fact]
        public async Task Timeout_WithoutAck_FailsClosed()
        {
            var comm = new RecordingCommClient();
            var variables = new TrackApplicationVariables();
            var step = new DataUploadStep(comm, variables, timeout: TimeSpan.FromMilliseconds(40));

            var first = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Continue, first.Kind);
            Assert.Equal(1, comm.SendCount); // command 106 was transmitted once

            await Task.Delay(80); // exceed the window

            var result = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Error, result.Kind);
            Assert.Contains("timed out", result.ErrorMessage);
        }

        [Fact]
        public async Task LateDone_DoesNotResume_FailsClosed()
        {
            var comm = new RecordingCommClient();
            var variables = new TrackApplicationVariables();
            var step = new DataUploadStep(comm, variables, timeout: TimeSpan.FromMilliseconds(40));

            await step.ExecuteAsync(null, CancellationToken.None); // substate 0: send
            await Task.Delay(80); // exceed the window

            // A late DONE must NOT resume the step: the timeout is authoritative.
            var result = await step.ExecuteAsync(DataUploadDone, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Error, result.Kind);
        }

        [Fact]
        public async Task DoneWithinWindow_AdvancesToDetectSlaves()
        {
            var comm = new RecordingCommClient();
            var variables = new TrackApplicationVariables();
            var step = new DataUploadStep(comm, variables, timeout: TimeSpan.FromSeconds(5));

            await step.ExecuteAsync(null, CancellationToken.None); // substate 0: send

            var result = await step.ExecuteAsync(DataUploadDone, CancellationToken.None);
            Assert.Equal(InitStepResultKind.NextStep, result.Kind);
            Assert.Equal("DetectSlaves", result.NextStepName);
        }
    }
}
