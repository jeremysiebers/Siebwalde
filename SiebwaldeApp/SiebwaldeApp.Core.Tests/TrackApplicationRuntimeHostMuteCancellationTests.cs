using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Locks in the deterministic teardown of <see cref="TrackApplicationRuntimeHost"/> when the
    /// controller is silent (comm failure). A bounded cancellation token cancels the in-flight start
    /// (which would otherwise block forever on the never-acknowledged 250 connection request), the
    /// host transitions fail-closed to <see cref="TrackRuntimeState.Failed"/>, and
    /// <see cref="TrackApplicationRuntimeHost.DisposeAsync"/> tears it down to
    /// <see cref="TrackRuntimeState.Stopped"/>. No controller, no network, no hardware.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class TrackApplicationRuntimeHostMuteCancellationTests
    {
        private sealed class StubEcosHostService : IEcosHostService
        {
            public event EventHandler<RuntimeFaultEventArgs>? Faulted;

            public bool IsRunning { get; private set; }
            public TrackControlMode? Mode { get; private set; }
            public ControlDiagnostics? Diagnostics => null;
            public bool IsUnsafe => false;

            public bool ResetSafety() => false;

            public void SetProfile(LayoutProfile profile)
            {
                // No-op: the harness supplies no layout profile.
            }

            public IReadOnlyDictionary<int, SwitchPosition> GetSwitchPositions()
                => new Dictionary<int, SwitchPosition>();

            public Task<EcosHostStartResult> StartAsync(
                TrackControlMode mode,
                ITrackCommClient? commClient,
                TrackApplicationVariables? variables,
                CancellationToken cancellationToken = default,
                IMovementSimulation? movementSimulation = null)
            {
                Mode = mode;
                IsRunning = true;
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

        [Fact]
        public async Task MutedController_StartCancelsToFailed_AndDisposesToStopped()
        {
            var created = new List<DeterministicTrackTransport>();
            var runtime = new TrackApplicationRuntimeHost(
                new StubEcosHostService(),
                controlTrace: null,
                transportFactory: () =>
                {
                    var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(controllerMute: true));
                    created.Add(transport);
                    return transport;
                },
                amplifierGroups: TrackAmplifierGroups.Create(new[] { 1, 2, 3 }, null, null));

            try
            {
                using var cts = new CancellationTokenSource();
                var startTask = runtime.StartAsync(TrackControlMode.FullSimulation, cts.Token);
                try
                {
                    // The 250 connection request is transmitted but never acknowledged by the muted
                    // controller, so the init machine is stuck and the host stays Starting.
                    await WaitUntilAsync(
                        () => created.Count == 1 && created[0].SentCommands.Contains(EnumClientCommands.CLIENT_CONNECTION_REQUEST),
                        TimeSpan.FromSeconds(5));

                    Assert.Equal(TrackRuntimeState.Starting, runtime.State);
                    Assert.DoesNotContain(TrackCommand.GET_CONTROLLER_VERSION, created[0].SentCommands);
                }
                finally
                {
                    // Always cancel so the stuck start aborts deterministically before teardown.
                    cts.Cancel();
                    try
                    {
                        await startTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // The start was cancelled; the host ends fail-closed (Failed).
                    }
                }

                Assert.Equal(TrackRuntimeState.Failed, runtime.State);
            }
            finally
            {
                await runtime.DisposeAsync();
            }

            Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
        }

        private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                if (condition())
                {
                    return;
                }

                await Task.Delay(25);
            }

            throw new TimeoutException($"Condition was not met within {timeout}.");
        }
    }
}
