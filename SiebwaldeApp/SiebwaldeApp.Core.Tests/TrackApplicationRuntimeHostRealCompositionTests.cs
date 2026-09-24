using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Real-mode composition tests for <see cref="TrackApplicationRuntimeHost"/>, using a fake
    /// <see cref="ITrackTransport"/> and a fake <see cref="IEcosHostService"/>. No track controller,
    /// no network and no hardware are involved: the fake transport records the transport/comm wiring
    /// and forces a fast, deterministic init failure, while the fake host records the fresh
    /// <see cref="TrackApplicationVariables"/> handed to it each start.
    /// </summary>
    public class TrackApplicationRuntimeHostRealCompositionTests
    {
        private sealed class RecordingTrackTransport : ITrackTransport
        {
            public int OpenCallCount { get; private set; }
            public int CloseCallCount { get; private set; }
            public int DisposeCallCount { get; private set; }
            public List<byte> SentCommands { get; } = new();

            /// <summary>When true, SendAsync throws to force a fast deterministic init failure.</summary>
            public bool ThrowOnSend { get; set; }

            /// <summary>When true, DisposeAsync throws to simulate a partial-cleanup failure.</summary>
            public bool ThrowOnDispose { get; set; }

            public Task OpenAsync(CancellationToken cancellationToken = default)
            {
                OpenCallCount++;
                return Task.CompletedTask;
            }

            public Task CloseAsync(CancellationToken cancellationToken = default)
            {
                CloseCallCount++;
                return Task.CompletedTask;
            }

            public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
            {
                if (buffer is { Length: >= 2 })
                {
                    SentCommands.Add(buffer[1]);
                }

                if (ThrowOnSend)
                {
                    throw new InvalidOperationException("No controller in test.");
                }

                return Task.CompletedTask;
            }

            public async IAsyncEnumerable<byte[]> ReceiveAsync(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;
                yield break;
            }

            public ValueTask DisposeAsync()
            {
                DisposeCallCount++;
                if (ThrowOnDispose)
                {
                    throw new InvalidOperationException("Dispose failed in test.");
                }

                return ValueTask.CompletedTask;
            }
        }

        private sealed class RecordingEcosHostService : IEcosHostService
        {
            public event EventHandler<RuntimeFaultEventArgs>? Faulted;

            public bool IsRunning { get; private set; }
            public TrackControlMode? Mode { get; private set; }
            public ControlDiagnostics? Diagnostics => null;
            public bool IsUnsafe => false;

            public TrackApplicationVariables? LastVariables { get; private set; }
            public List<TrackApplicationVariables> SeenVariables { get; } = new();

            public bool ResetSafety() => false;

            public Task<EcosHostStartResult> StartAsync(
                TrackControlMode mode,
                ITrackCommClient? commClient,
                TrackApplicationVariables? variables,
                CancellationToken cancellationToken = default)
            {
                Mode = mode;
                IsRunning = true;
                LastVariables = variables;
                if (variables is not null)
                {
                    SeenVariables.Add(variables);
                }

                return Task.FromResult(EcosHostStartResult.Started);
            }

            public Task<EcosHostStopResult> StopAsync(CancellationToken ct = default)
            {
                IsRunning = false;
                Mode = null;
                return Task.FromResult(EcosHostStopResult.Stopped);
            }

            public void Stop()
            {
                IsRunning = false;
                Mode = null;
            }
        }

        private static TrackApplicationRuntimeHost CreateRuntime(
            RecordingTrackTransport transport,
            RecordingEcosHostService host)
            => new(host, controlTrace: null, transportFactory: () => transport);

        [Fact]
        public async Task RealModeStart_WhenInitFails_TransitionsToFailed()
        {
            var transport = new RecordingTrackTransport { ThrowOnSend = true };
            var host = new RecordingEcosHostService();
            var runtime = CreateRuntime(transport, host);

            string? failure = null;
            runtime.StateChanged += (_, e) =>
            {
                if (e.FailureReason is not null)
                {
                    failure = e.FailureReason;
                }
            };

            await runtime.StartAsync(TrackControlMode.Real);

            // A genuine init failure must surface as Failed, never as Running.
            Assert.Equal(TrackRuntimeState.Failed, runtime.State);
            Assert.NotNull(failure);

            // Composition ran before failing: the transport was opened and the first init
            // command (the verbatim first step, ConnectToEthernetTarget) was issued.
            Assert.True(transport.OpenCallCount >= 1);
            Assert.NotEmpty(transport.SentCommands);
            Assert.Equal(EnumClientCommands.CLIENT_CONNECTION_REQUEST, transport.SentCommands[0]);
        }

        [Fact]
        public async Task RealMode_AfterFailedStartAndCleanStop_CanAttemptRestart()
        {
            var transport = new RecordingTrackTransport { ThrowOnSend = true };
            var host = new RecordingEcosHostService();
            var runtime = CreateRuntime(transport, host);

            await runtime.StartAsync(TrackControlMode.Real);
            Assert.Equal(TrackRuntimeState.Failed, runtime.State);

            await runtime.StopAsync();
            Assert.Equal(TrackRuntimeState.Stopped, runtime.State);

            // A clean stop leaves the coordinator restartable: the next start composes a fresh
            // transport (re-opened) and fails again at init.
            await runtime.StartAsync(TrackControlMode.Real);
            Assert.Equal(TrackRuntimeState.Failed, runtime.State);
            Assert.True(transport.OpenCallCount >= 2);
        }

        [Fact]
        public async Task RealMode_DisposeFaultDuringFailedStart_StillAllowsCleanStop()
        {
            var transport = new RecordingTrackTransport { ThrowOnSend = true, ThrowOnDispose = true };
            var host = new RecordingEcosHostService();
            var runtime = CreateRuntime(transport, host);

            await runtime.StartAsync(TrackControlMode.Real);
            Assert.Equal(TrackRuntimeState.Failed, runtime.State);

            // Even though the comm-client dispose threw during failed-start cleanup, the fields
            // were still released (StopTrackPartAsync finally), so a subsequent stop is clean.
            await runtime.StopAsync();
            Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
        }
    }
}
