using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Properties;
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
            // The real-mode init pipeline reads the firmware hex in FlashFwTrackamplifiersStep even when
            // flashing is skipped, so the test must not depend on the (untracked) repo dist/ hex being
            // present. Generate a minimal self-contained hex whose checksum equals the simulator's
            // FirmwareChecksum (0x251F), point the setting at it, then restore + delete on the way out.
            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-test-firmware-{Guid.NewGuid():N}.hex");

            try
            {
                WriteTestFirmwareHex(tempHexPath);
                CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

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
            finally
            {
                try { CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                try { File.Delete(tempHexPath); } catch { /* best-effort */ }
            }
        }

        /// <summary>
        /// Writes a minimal Intel HEX that <see cref="TrackAmplifierBootloaderHelpers.Execute"/> parses
        /// to a file checksum of 0x251F: 1920 zero data rows, with the LAST row's first word set to
        /// 0x251F little-endian (0x1F, 0x25) and its checksum word (bytes 14..15) left zero (that word is
        /// excluded from the sum), followed by one 12-byte config row so the config word is "found".
        /// </summary>
        private static void WriteTestFirmwareHex(string path)
        {
            const int dataRows = (0x8000 - 0x800) / 16; // 1920, matching TrackAmplifierBootloaderHelpers.Execute

            var sb = new StringBuilder();

            for (int row = 0; row < dataRows; row++)
            {
                string address = (row * 16).ToString("X4");

                // 32 hex chars = 16 data bytes. The checksum byte (CC) is not read by Execute().
                string data = row == dataRows - 1
                    ? "1F25" + new string('0', 28)
                    : new string('0', 32);

                sb.Append(':').Append("10").Append(address).Append("00").Append(data).Append("00").AppendLine();
            }

            // One config row: record length 0x0C (12 bytes -> 24 hex chars). Its content is only used
            // when flashing, which is skipped, so any 12 bytes are acceptable.
            sb.Append(':').Append("0C").Append("0000").Append("00").Append(new string('F', 24)).Append("00").AppendLine();

            File.WriteAllText(path, sb.ToString());
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
