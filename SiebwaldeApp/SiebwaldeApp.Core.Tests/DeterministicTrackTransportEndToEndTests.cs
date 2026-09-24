using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Deterministic end-to-end tests for the track runtime. They run the REAL
    /// <see cref="TrackCommClientAsync"/> + 9-step initialization pipeline + <see cref="TrackControlMain"/>
    /// write loop on top of the software-only <see cref="DeterministicTrackTransport"/>, with a fake
    /// <see cref="IEcosHostService"/> so no port (15471) or Koploper dial (5700) is involved.
    /// </summary>
    public class DeterministicTrackTransportEndToEndTests
    {
        private sealed class FakeEcosHostService : IEcosHostService
        {
            public event EventHandler<RuntimeFaultEventArgs>? Faulted;

            public bool IsRunning { get; private set; }
            public TrackControlMode? Mode { get; private set; }
            public ControlDiagnostics? Diagnostics => null;
            public bool IsUnsafe => false;

            public bool ResetSafety() => false;

            public Task<EcosHostStartResult> StartAsync(
                TrackControlMode mode,
                ITrackCommClient? commClient,
                TrackApplicationVariables? variables,
                CancellationToken cancellationToken = default)
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

        private sealed class SimulatorFactory
        {
            public List<DeterministicTrackTransport> Created { get; } = new();

            private readonly TrackSimulatorConfig _config;

            public SimulatorFactory(TrackSimulatorConfig config) => _config = config;

            public ITrackTransport Create()
            {
                var simulator = new DeterministicTrackTransport(_config);
                Created.Add(simulator);
                return simulator;
            }
        }

        private static readonly byte[] ExpectedInitCommands = { 250, 107, 100, 106, 101, 104, 105 };

        private static async Task RunWithRuntimeAsync(
            SimulatorFactory factory,
            Func<TrackApplicationRuntimeHost, CancellationToken, Task> testBody)
        {
            // The real-mode init pipeline reads the firmware hex in FlashFwTrackamplifiersStep; without
            // a present, parseable hex the init never reaches Completed and every test below fails.
            Assert.True(
                File.Exists(CoreConfiguration.TrackAmplifierFirmwarePath),
                $"Track-amplifier firmware hex not found at '{CoreConfiguration.TrackAmplifierFirmwarePath}'. " +
                "The real-mode init pipeline requires a present, parseable hex to reach Completed.");

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var runtime = new TrackApplicationRuntimeHost(
                new FakeEcosHostService(),
                controlTrace: null,
                transportFactory: factory.Create);

            try
            {
                await testBody(runtime, timeout.Token);
            }
            finally
            {
                await runtime.DisposeAsync();
            }
        }

        private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                if (condition())
                    return;

                await Task.Delay(25);
            }

            throw new TimeoutException($"Condition was not met within {timeout}.");
        }

        [Fact]
        public Task RealMode_EndToEnd_InitializesDetectsAndWritesAmplifier()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig());

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);

                // Running only happens once the init pipeline reached Completed.
                Assert.Equal(TrackRuntimeState.Running, runtime.State);

                var simulator = Assert.Single(factory.Created);

                Assert.Equal(ExpectedInitCommands, simulator.SentCommands.ToArray());

                for (int slave = 1; slave <= 3; slave++)
                {
                    var amplifier = runtime.TrackAmplifiers[slave];
                    Assert.Equal((ushort)1, amplifier.SlaveDetected);
                    Assert.Equal(
                        (ushort)TrackSimulatorConfig.DefaultFirmwareChecksum,
                        amplifier.HoldingReg[TrackAmplifierRegisters.SwChecksum]);
                }

                // Queue a manual write and wait for the 10 Hz TrackControlMain loop to push it
                // through the transport and echo it back as a SLAVEINFO frame.
                runtime.SetAmplifierControl(1, 500, false);

                await WaitUntilAsync(
                    () => simulator.GetRegisters(1)[0] == 0x01F4
                          && runtime.TrackAmplifiers[1].HoldingReg[0] == 0x01F4,
                    TimeSpan.FromSeconds(5));

                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            });
        }

        [Fact]
        public Task Restart_UsesFreshTransportAndLeavesNoStalePendingWrites()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig());

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);
                Assert.Equal(TrackRuntimeState.Running, runtime.State);

                var first = Assert.Single(factory.Created);
                var firstCommands = first.SentCommands.ToArray();

                await runtime.RestartAsync(ct);
                Assert.Equal(TrackRuntimeState.Running, runtime.State);

                Assert.Equal(2, factory.Created.Count);
                var second = factory.Created[1];
                Assert.NotSame(first, second);

                // Fresh simulator and a deterministic re-run of the same init sequence.
                Assert.Equal(firstCommands, second.SentCommands.ToArray());

                // Give the 10 Hz write loop a few ticks: a stale PendingWrites entry from the first
                // run must not produce an EXEC_MBUS_SLAVE_DATA_EXCH (108) in the fresh runtime.
                await Task.Delay(300, ct);
                Assert.DoesNotContain((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, second.SentCommands.ToArray());

                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            });
        }

        [Fact]
        public Task ThreeStartStopCycles_EachReturnsToStoppedAndReleasesTransport()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig());

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                for (int i = 0; i < 3; i++)
                {
                    await runtime.StartAsync(TrackControlMode.Real, ct);
                    Assert.Equal(TrackRuntimeState.Running, runtime.State);

                    await runtime.StopAsync(ct);
                    Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
                }

                Assert.Equal(3, factory.Created.Count);

                foreach (var simulator in factory.Created)
                {
                    Assert.Equal(1, simulator.OpenCallCount);
                    Assert.True(simulator.CloseCallCount >= 1);
                    Assert.True(simulator.IsDisposed);
                }
            });
        }

        [Fact]
        public Task TwoInits_AreDeterministic()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig());

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);
                var first = Assert.Single(factory.Created);
                var firstCommands = first.SentCommands.ToArray();
                var firstRegisters = new Dictionary<byte, ushort[]>();
                for (byte slave = 1; slave <= 3; slave++)
                {
                    firstRegisters[slave] = first.GetRegisters(slave);
                }

                await runtime.StopAsync(ct);

                await runtime.StartAsync(TrackControlMode.Real, ct);
                Assert.Equal(TrackRuntimeState.Running, runtime.State);

                var second = factory.Created[1];
                Assert.Equal(firstCommands, second.SentCommands.ToArray());
                for (byte slave = 1; slave <= 3; slave++)
                {
                    Assert.Equal(firstRegisters[slave], second.GetRegisters(slave));
                }

                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            });
        }
    }
}
