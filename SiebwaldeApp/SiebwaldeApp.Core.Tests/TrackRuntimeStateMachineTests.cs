using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// State-machine tests that drive <see cref="TrackApplicationRuntimeHost"/> in simulator mode
    /// with a fake <see cref="IEcosHostService"/>. No track controller, no network and no WPF are
    /// involved: the simulator path skips the track part, so only the lifecycle state machine and
    /// the host hand-off are exercised.
    /// </summary>
    public class TrackRuntimeStateMachineTests
    {
        private sealed class FakeEcosHostService : IEcosHostService
        {
            public event EventHandler<RuntimeFaultEventArgs>? Faulted;

            public bool IsRunning { get; private set; }
            public TrackControlMode? Mode { get; private set; }
            public ControlDiagnostics? Diagnostics => null;
            public bool IsUnsafe => false;

            public int StartCallCount { get; private set; }
            public int StopCallCount { get; private set; }

            public Func<TrackControlMode, ITrackCommClient?, TrackApplicationVariables?, CancellationToken, Task<EcosHostStartResult>>? StartHandler { get; set; }
            public Func<CancellationToken, Task<EcosHostStopResult>>? StopHandler { get; set; }

            public bool ResetSafety() => false;

            /// <summary>Raises the host's Faulted event so a test can simulate a background fault.</summary>
            public void RaiseFault(RuntimeFaultEventArgs args) => Faulted?.Invoke(this, args);

            public Task<EcosHostStartResult> StartAsync(
                TrackControlMode mode,
                ITrackCommClient? commClient,
                TrackApplicationVariables? variables,
                CancellationToken cancellationToken = default)
            {
                StartCallCount++;
                Mode = mode;
                IsRunning = true;
                return StartHandler?.Invoke(mode, commClient, variables, cancellationToken)
                       ?? Task.FromResult(EcosHostStartResult.Started);
            }

            public Task<EcosHostStopResult> StopAsync(CancellationToken ct = default)
            {
                StopCallCount++;
                IsRunning = false;
                Mode = null;
                return StopHandler?.Invoke(ct) ?? Task.FromResult(EcosHostStopResult.Stopped);
            }

            public void Stop()
            {
                IsRunning = false;
                Mode = null;
            }
        }

        private sealed class RecordingRuntime
        {
            public readonly FakeEcosHostService Host = new();
            public readonly TrackApplicationRuntimeHost Runtime;
            public readonly List<TrackRuntimeState> States = new();
            public readonly List<string?> Failures = new();

            public RecordingRuntime()
            {
                Runtime = new TrackApplicationRuntimeHost(Host);
                Runtime.StateChanged += (_, e) =>
                {
                    States.Add(e.State);
                    if (e.FailureReason is not null)
                    {
                        Failures.Add(e.FailureReason);
                    }
                };
            }
        }

        [Fact]
        public async Task Start_FromStopped_TransitionsToRunning()
        {
            var rec = new RecordingRuntime();

            await rec.Runtime.StartAsync(TrackControlMode.Simulator);

            Assert.Equal(TrackRuntimeState.Running, rec.Runtime.State);
            Assert.Equal(TrackControlMode.Simulator, rec.Runtime.ActiveEcosMode);
            Assert.Equal(new[] { TrackRuntimeState.Starting, TrackRuntimeState.Running }, rec.States);
        }

        [Fact]
        public async Task Start_WhenAlreadyRunning_Throws()
        {
            var rec = new RecordingRuntime();
            await rec.Runtime.StartAsync(TrackControlMode.Simulator);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => rec.Runtime.StartAsync(TrackControlMode.Simulator));

            Assert.Equal(TrackRuntimeState.Running, rec.Runtime.State);
        }

        [Fact]
        public async Task Start_WhenEcosRejects_TransitionsToFailed()
        {
            var rec = new RecordingRuntime();
            rec.Host.StartHandler = (_, _, _, _) => Task.FromResult(EcosHostStartResult.Rejected);

            await rec.Runtime.StartAsync(TrackControlMode.Simulator);

            Assert.Equal(TrackRuntimeState.Failed, rec.Runtime.State);
            Assert.NotEmpty(rec.Failures);
        }

        [Fact]
        public async Task Start_WhenEcosThrows_TransitionsToFailed()
        {
            var rec = new RecordingRuntime();
            rec.Host.StartHandler = (_, _, _, _) => throw new InvalidOperationException("start boom");

            await rec.Runtime.StartAsync(TrackControlMode.Simulator);

            Assert.Equal(TrackRuntimeState.Failed, rec.Runtime.State);
            Assert.NotEmpty(rec.Failures);
        }

        [Fact]
        public async Task Stop_FromRunning_TransitionsToStopped()
        {
            var rec = new RecordingRuntime();
            await rec.Runtime.StartAsync(TrackControlMode.Simulator);

            await rec.Runtime.StopAsync();

            Assert.Equal(TrackRuntimeState.Stopped, rec.Runtime.State);
            Assert.Equal(
                new[]
                {
                    TrackRuntimeState.Starting, TrackRuntimeState.Running,
                    TrackRuntimeState.Stopping, TrackRuntimeState.Stopped
                },
                rec.States);
        }

        [Fact]
        public async Task Stop_WhenStopped_IsIdempotent()
        {
            var rec = new RecordingRuntime();

            await rec.Runtime.StopAsync();
            await rec.Runtime.StopAsync();

            Assert.Equal(TrackRuntimeState.Stopped, rec.Runtime.State);
            Assert.Empty(rec.States);
            Assert.Equal(0, rec.Host.StopCallCount);
        }

        [Fact]
        public async Task Stop_WhenEcosReturnsTimeout_TransitionsToFailed()
        {
            var rec = new RecordingRuntime();
            await rec.Runtime.StartAsync(TrackControlMode.Simulator);
            rec.Host.StopHandler = _ => Task.FromResult(EcosHostStopResult.Timeout);

            await rec.Runtime.StopAsync();

            Assert.Equal(TrackRuntimeState.Failed, rec.Runtime.State);
            Assert.NotEmpty(rec.Failures);
        }

        [Fact]
        public async Task Stop_WhenEcosReturnsFaulted_TransitionsToFailed()
        {
            var rec = new RecordingRuntime();
            await rec.Runtime.StartAsync(TrackControlMode.Simulator);
            rec.Host.StopHandler = _ => Task.FromResult(EcosHostStopResult.Faulted);

            await rec.Runtime.StopAsync();

            Assert.Equal(TrackRuntimeState.Failed, rec.Runtime.State);
            Assert.NotEmpty(rec.Failures);
        }

        [Fact]
        public async Task Restart_FromRunning_ReturnsToRunning()
        {
            var rec = new RecordingRuntime();
            await rec.Runtime.StartAsync(TrackControlMode.Simulator);

            await rec.Runtime.RestartAsync();

            Assert.Equal(TrackRuntimeState.Running, rec.Runtime.State);
            Assert.Equal(2, rec.Host.StartCallCount);
            Assert.Equal(1, rec.Host.StopCallCount);
        }

        [Fact]
        public async Task Restart_WhenStopFails_StaysFailed()
        {
            var rec = new RecordingRuntime();
            await rec.Runtime.StartAsync(TrackControlMode.Simulator);
            rec.Host.StopHandler = _ => Task.FromResult(EcosHostStopResult.Faulted);

            await rec.Runtime.RestartAsync();

            Assert.Equal(TrackRuntimeState.Failed, rec.Runtime.State);
            Assert.Equal(1, rec.Host.StartCallCount);
        }

        [Fact]
        public async Task Stop_FromFailed_TransitionsToStopped()
        {
            var rec = new RecordingRuntime();
            rec.Host.StartHandler = (_, _, _, _) => throw new InvalidOperationException("start boom");
            await rec.Runtime.StartAsync(TrackControlMode.Simulator);
            Assert.Equal(TrackRuntimeState.Failed, rec.Runtime.State);

            // A clean idempotent stop from Failed is allowed and recovers to Stopped.
            rec.Host.StopHandler = _ => Task.FromResult(EcosHostStopResult.Stopped);
            await rec.Runtime.StopAsync();

            Assert.Equal(TrackRuntimeState.Stopped, rec.Runtime.State);
        }

        [Fact]
        public async Task FaultedWhileRunning_TransitionsToFailed()
        {
            var rec = new RecordingRuntime();
            await rec.Runtime.StartAsync(TrackControlMode.Simulator);
            Assert.Equal(TrackRuntimeState.Running, rec.Runtime.State);

            // A background task fault while running must surface as Failed, never stay Running.
            rec.Host.RaiseFault(new RuntimeFaultEventArgs(
                "TrackSimulatorBackend.SimLoop",
                new InvalidOperationException("background boom")));

            Assert.Equal(TrackRuntimeState.Failed, rec.Runtime.State);
            Assert.NotEmpty(rec.Failures);
            Assert.Contains("background boom", rec.Failures);
        }
    }
}
