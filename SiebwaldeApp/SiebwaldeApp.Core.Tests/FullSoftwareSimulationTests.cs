using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Properties;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Integration tests for <see cref="TrackControlMode.FullSimulation"/>: the REAL track-control
    /// chain (<see cref="TrackControlHost"/> + <see cref="TrackApplicationRuntimeHost"/> +
    /// <see cref="DeterministicTrackTransport"/>) started with a controllable simulated-amplifier
    /// I/O surface. No second control implementation and no hardware are involved; the transport is
    /// the deterministic PIC32 emulation and the HR0 readback is the PIC18 holding-register echo.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class FullSoftwareSimulationTests
    {
        private static BlockTopology Topology => BlockTopology.Parse("amps: 1:1,2:2 ; routes: 1>2,2>1");

        private static KoploperBlockMap BlockMap => KoploperBlockMap.Parse("1:1.01:1, 2:1.02:2");

        private static readonly byte[] DetectedSlaves = { 1, 2, 3 };

        private static readonly TrackSimulatorConfig FullSimConfig =
            new(detectedSlaves: DetectedSlaves, periodicSlaveInfoInterval: TimeSpan.FromMilliseconds(500));

        // ---------------------------------------------------------------------------------
        // AC1/AC2: default-domain path + fail-closed regression
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task FullSimulation_EmptyGroups_UsesSimulationDefaultDomain_ReachesRunningAndGrants()
        {
            await RunAsync(TrackControlMode.FullSimulation, TrackAmplifierGroups.Empty, (runtime, _, ecosPort, ct) =>
            {
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.True(runtime.IsMovementSafe);

                // The FullSimulation I/O surface is available and drives the default detected
                // slaves {1,2,3} as the effective domain when no group is configured.
                Assert.NotNull(runtime.SimulatedTrackIo);
                Assert.Equal(new byte[] { 1, 2, 3 }, runtime.SimulatedTrackIo!.DetectedSlaves.ToArray());

                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task RealMode_EmptyGroups_NoSimulator_FailsClosed_AndBlocksKoploperCommand()
        {
            await RunAsync(TrackControlMode.Real, TrackAmplifierGroups.Empty, async (runtime, simulator, ecosPort, ct) =>
            {
                // Real mode has no simulator to fall back on, so an empty domain must stay fail-closed.
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);
                Assert.Null(runtime.SimulatedTrackIo);

                // AC4 gate: the same Koploper locomotive-speed request is refused as a safety interlock.
                var endLine = await SendEcosCommandAsync(ecosPort, "set(1000,speedstep[10])", TimeSpan.FromSeconds(10));
                Assert.Equal("<END 8 (SAFETY_INTERLOCK)>", endLine);

                // Nothing was queued, so no non-neutral write reaches the transport.
                await Task.Delay(300, ct);
                Assert.Equal(0, simulator.GetRegisters(1)[0] & 0x03FF);
            });
        }

        // ---------------------------------------------------------------------------------
        // Movement-safety gate: forced non-neutral readback keeps permission NotGranted
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task FullSimulation_ForcedNonNeutralReadback_NotGranted_BlocksKoploperCommand_WithoutNewWrite()
        {
            var groups = TrackAmplifierGroups.Create(new[] { 1, 2, 3 }, null, null);

            // Slave 1 reads back HR0 = 500 (non-neutral) even though neutral was commanded, so
            // EstablishObservedNeutralAsync cannot grant movement permission.
            var config = new TrackSimulatorConfig(
                hr0ReadbackOverrides: new Dictionary<byte, ushort> { { 1, 500 } });

            await RunAsync(TrackControlMode.FullSimulation, groups, async (runtime, simulator, ecosPort, ct) =>
            {
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);

                // The neutral-establishment phase commands neutral to the whole configured domain,
                // so one 108 write per amplifier already reached the transport before the command
                // under test. Snapshot here so we can prove the refused Koploper command itself
                // produces no new 108 write.
                var commandsBefore = simulator.SentCommands.Count;

                var endLine = await SendEcosCommandAsync(ecosPort, "set(1000,speedstep[10])", TimeSpan.FromSeconds(10));
                Assert.Equal("<END 8 (SAFETY_INTERLOCK)>", endLine);

                await Task.Delay(300, ct);
                var newCommands = simulator.SentCommands.Skip(commandsBefore).ToList();
                Assert.DoesNotContain((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, newCommands);
            }, config);
        }

        // ---------------------------------------------------------------------------------
        // AC3: manual command path
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task FullSimulation_ManualSetAmplifierControl_Writes108_AndEchoes()
        {
            await RunAsync(TrackControlMode.FullSimulation, TrackAmplifierGroups.Empty, async (runtime, simulator, ecosPort, ct) =>
            {
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);

                var expectedHr0 = TrackApplicationVariables.BuildHr0Value(500, emoStop: false);
                Assert.Equal(0x01F4, expectedHr0);

                runtime.SetAmplifierControl(1, 500, false);

                await WaitUntilAsync(
                    () => simulator.GetRegisters(1)[0] == expectedHr0
                          && runtime.TrackAmplifiers[1].HoldingReg[0] == expectedHr0,
                    TimeSpan.FromSeconds(5));

                // The 10 Hz write loop carried the non-neutral HR0 to the transport via a 108 write,
                // and the C# HoldingReg[0] mirrors the echoed SLAVEINFO readback (command echo).
                Assert.Contains((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, simulator.SentCommands);
                Assert.Equal(expectedHr0, simulator.GetRegisters(1)[0]);
                Assert.Equal(expectedHr0, runtime.TrackAmplifiers[1].HoldingReg[0]);
            });
        }

        // ---------------------------------------------------------------------------------
        // AC4: Koploper command path
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task FullSimulation_KoploperCommand_AcceptsAndWrites108_AndEchoes()
        {
            await RunAsync(TrackControlMode.FullSimulation, TrackAmplifierGroups.Empty, async (runtime, simulator, ecosPort, ct) =>
            {
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);

                Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", 10, out var normalizedSpeed));
                var expectedPwm = AmplifierSpeedMapper.ToPwm(normalizedSpeed, direction: 1);
                var expectedHr0 = TrackApplicationVariables.BuildHr0Value(expectedPwm, emoStop: false);
                Assert.NotEqual(AmplifierSpeedMapper.NeutralPwm, (int)expectedHr0);

                var commandsBefore = simulator.SentCommands.Count;

                // Koploper's normal locomotive-speed request, through the real ECoS host. The fake
                // external-info server has placed loco 1000 in block 1 (-> amplifier 1).
                var endLine = await SendEcosCommandAsync(ecosPort, "set(1000,speedstep[10])", TimeSpan.FromSeconds(10));
                Assert.Equal("<END 0 (OK)>", endLine);

                await WaitUntilAsync(
                    () => simulator.GetRegisters(1)[0] == expectedHr0
                          && runtime.TrackAmplifiers[1].HoldingReg[0] == expectedHr0,
                    TimeSpan.FromSeconds(5));

                Assert.True(simulator.SentCommands.Count > commandsBefore);
                Assert.Contains((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, simulator.SentCommands);
                Assert.Equal(expectedHr0, simulator.GetRegisters(1)[0]);
                Assert.Equal(expectedHr0, runtime.TrackAmplifiers[1].HoldingReg[0]);
            });
        }

        // ---------------------------------------------------------------------------------
        // AC5: occupancy feedback + heartbeat freshness
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task FullSimulation_Occupancy_EmitsEcosSensorEvents_AndStaysFresh()
        {
            await RunAsync(TrackControlMode.FullSimulation, TrackAmplifierGroups.Empty, async (runtime, _, ecosPort, ct) =>
            {
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.NotNull(runtime.SimulatedTrackIo);

                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, ecosPort, ct);
                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
                using var reader = new StreamReader(stream, Encoding.ASCII);

                // Establish the ECoS writer and consume the initial feedback + reply.
                await writer.WriteAsync("request(100,view)");
                await writer.FlushAsync();
                await ReadUntilAsync(
                    reader,
                    line => line.StartsWith("<REPLY request(100,view)>", StringComparison.Ordinal),
                    TimeSpan.FromSeconds(10));
                await ReadUntilAsync(
                    reader,
                    line => line.StartsWith("<END ", StringComparison.Ordinal),
                    TimeSpan.FromSeconds(10));

                // Occupy block 1 -> bezetmelder "1.01" -> sensor 1 -> bit 0 of feedback module 100.
                runtime.SimulatedTrackIo!.SetSlaveOccupancy(1, true);
                var occupiedLines = await ReadUntilAsync(
                    reader,
                    line => line.StartsWith("100 state[", StringComparison.Ordinal),
                    TimeSpan.FromSeconds(10));
                Assert.Contains(occupiedLines, l => l.StartsWith("<EVENT 100>", StringComparison.Ordinal));
                AssertOccupancyBit(occupiedLines, occupied: true);

                // Clear it again -> live clear event.
                runtime.SimulatedTrackIo!.SetSlaveOccupancy(1, false);
                var clearLines = await ReadUntilAsync(
                    reader,
                    line => line.StartsWith("100 state[", StringComparison.Ordinal),
                    TimeSpan.FromSeconds(10));
                Assert.Contains(clearLines, l => l.StartsWith("<EVENT 100>", StringComparison.Ordinal));
                AssertOccupancyBit(clearLines, occupied: false);

                // Occupancy is sustained past the 2 s stale threshold because the 500 ms heartbeat
                // keeps the holding-register view fresh.
                runtime.SimulatedTrackIo!.SetSlaveOccupancy(1, true);
                await Task.Delay(2500, ct);
                Assert.True(TrackAmplifierDataFreshness.IsCurrentData(runtime.TrackAmplifiers[1]));
                Assert.True(TrackAmplifierRegisters.IsOccupied(runtime.TrackAmplifiers[1].HoldingReg));
            });
        }

        // ---------------------------------------------------------------------------------
        // AC2 restart: start/stop cycles release and re-establish the I/O surface
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task FullSimulation_Restart_ThreeCycles_EachGrants_AndReleasesSimulatedIo()
        {
            await RunAsync(TrackControlMode.FullSimulation, TrackAmplifierGroups.Empty, async (runtime, _, ecosPort, ct) =>
            {
                // First start (done by the harness) is Running + Granted.
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.NotNull(runtime.SimulatedTrackIo);

                for (int i = 0; i < 3; i++)
                {
                    await runtime.StopAsync(ct);
                    Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
                    Assert.Null(runtime.SimulatedTrackIo);

                    await runtime.StartAsync(TrackControlMode.FullSimulation, ct);
                    Assert.Equal(TrackRuntimeState.Running, runtime.State);
                    Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                    Assert.NotNull(runtime.SimulatedTrackIo);
                }

                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
                Assert.Null(runtime.SimulatedTrackIo);
            });
        }

        // ---------------------------------------------------------------------------------
        // Harness
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Composes the REAL <see cref="TrackControlHost"/> and starts it in <paramref name="mode"/>
        /// against the deterministic transport. For FullSimulation the transport is built internally
        /// from the simulator config; for Real mode it is supplied via the transport factory.
        /// </summary>
        private static async Task RunAsync(
            TrackControlMode mode,
            TrackAmplifierGroups groups,
            Func<TrackApplicationRuntimeHost, DeterministicTrackTransport, int, CancellationToken, Task> testBody,
            TrackSimulatorConfig? simulatorConfig = null)
        {
            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-fullsim-fw-{Guid.NewGuid():N}.hex");
            var locoPath = Path.Combine(Path.GetTempPath(), $"siebwalde-fullsim-locos-{Guid.NewGuid():N}.json");
            var ecosPort = GetFreeTcpPort();
            var externalPort = GetFreeTcpPort();

            FakeKoploperExternalInfoServer? externalServer = null;
            DeterministicTrackTransport? simulator = null;
            TrackApplicationRuntimeHost? runtime = null;

            try
            {
                WriteTestFirmwareHex(tempHexPath);
                CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

                externalServer = new FakeKoploperExternalInfoServer(externalPort, locoAddress: 1000, blockNumber: 1);

                var ecosHost = new TrackControlHost(
                    locoRepositoryPath: locoPath,
                    topology: Topology,
                    blockMap: BlockMap,
                    ecosListenPort: ecosPort,
                    koploperExternalInfoHost: "127.0.0.1",
                    koploperExternalInfoPort: externalPort,
                    trackAmplifierGroups: groups);

                if (mode == TrackControlMode.FullSimulation)
                {
                    runtime = new TrackApplicationRuntimeHost(
                        ecosHost,
                        controlTrace: null,
                        amplifierGroups: groups,
                        simulatorConfig: simulatorConfig ?? FullSimConfig);
                }
                else
                {
                    runtime = new TrackApplicationRuntimeHost(
                        ecosHost,
                        controlTrace: null,
                        transportFactory: () =>
                        {
                            simulator = new DeterministicTrackTransport(
                                new TrackSimulatorConfig(detectedSlaves: DetectedSlaves));
                            return simulator;
                        },
                        amplifierGroups: groups);
                }

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
                await runtime.StartAsync(mode, timeout.Token);

                if (mode == TrackControlMode.FullSimulation)
                {
                    simulator = Assert.IsType<DeterministicTrackTransport>(runtime.SimulatedTrackIo);
                }

                await testBody(runtime, simulator!, ecosPort, timeout.Token);
            }
            finally
            {
                if (runtime is not null)
                {
                    try { await runtime.DisposeAsync(); } catch { /* best-effort */ }
                }

                if (externalServer is not null)
                {
                    try { await externalServer.DisposeAsync(); } catch { /* best-effort */ }
                }

                try { CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                try { File.Delete(tempHexPath); } catch { /* best-effort */ }
                try { File.Delete(locoPath); } catch { /* best-effort */ }
            }
        }

        private static int GetFreeTcpPort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private static async Task<string> SendEcosCommandAsync(int port, string command, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port, cts.Token);

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
            using var reader = new StreamReader(stream, Encoding.ASCII);

            await writer.WriteAsync(command);
            await writer.FlushAsync();

            var replyHeaderSeen = false;
            while (true)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync().WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException($"Timed out waiting for the reply to '{command}' after {timeout}.");
                }

                if (line is null)
                {
                    throw new IOException($"The ECoS connection closed before replying to '{command}'.");
                }

                if (!replyHeaderSeen && line.StartsWith("<REPLY ", StringComparison.Ordinal))
                {
                    replyHeaderSeen = true;
                    continue;
                }

                if (replyHeaderSeen && line.StartsWith("<END ", StringComparison.Ordinal))
                {
                    return line;
                }
            }
        }

        /// <summary>Reads ECoS lines until <paramref name="match"/> is satisfied, returning all lines read.</summary>
        private static async Task<List<string>> ReadUntilAsync(StreamReader reader, Func<string, bool> match, TimeSpan timeout)
        {
            var lines = new List<string>();
            var deadline = DateTimeOffset.UtcNow + timeout;

            while (DateTimeOffset.UtcNow < deadline)
            {
                string? line;
                try
                {
                    using var cts = new CancellationTokenSource(deadline - DateTimeOffset.UtcNow);
                    line = await reader.ReadLineAsync().WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException($"Timed out waiting for ECoS feedback after {timeout}.");
                }

                if (line is null)
                {
                    throw new IOException("ECoS connection closed while waiting for feedback.");
                }

                lines.Add(line);
                if (match(line))
                {
                    return lines;
                }
            }

            throw new TimeoutException($"Timed out waiting for ECoS feedback after {timeout}.");
        }

        /// <summary>Asserts the bit 0 (sensor 1) occupancy in a collected "100 state[...]" event line.</summary>
        private static void AssertOccupancyBit(List<string> lines, bool occupied)
        {
            var stateLine = lines.First(l => l.StartsWith("100 state[", StringComparison.Ordinal));

            var open = stateLine.IndexOf('[');
            var close = stateLine.IndexOf(']', open);
            var valueText = stateLine.Substring(open + 1, close - open - 1).Trim();

            var value = valueText.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt32(valueText.Substring(2), 16)
                : int.Parse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture);

            var bit0 = (value & 0x1) != 0;
            Assert.Equal(occupied, bit0);
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

        private static void WriteTestFirmwareHex(string path)
        {
            const int dataRows = (0x8000 - 0x800) / 16; // 1920, matching TrackAmplifierBootloaderHelpers.Execute

            var sb = new StringBuilder();

            for (int row = 0; row < dataRows; row++)
            {
                string address = (row * 16).ToString("X4");
                string data = row == dataRows - 1
                    ? "1F25" + new string('0', 28)
                    : new string('0', 32);

                sb.Append(':').Append("10").Append(address).Append("00").Append(data).Append("00").AppendLine();
            }

            sb.Append(':').Append("0C").Append("0000").Append("00").Append(new string('F', 24)).Append("00").AppendLine();

            File.WriteAllText(path, sb.ToString());
        }

        /// <summary>
        /// Minimal stub for Koploper's external-information server (loco -> block).
        /// </summary>
        private sealed class FakeKoploperExternalInfoServer : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task _acceptLoop;

            public FakeKoploperExternalInfoServer(int port, int locoAddress, int blockNumber)
            {
                _listener = new TcpListener(IPAddress.Loopback, port);
                _listener.Start();
                _acceptLoop = AcceptLoopAsync(locoAddress, blockNumber, _cts.Token);
            }

            private async Task AcceptLoopAsync(int locoAddress, int blockNumber, CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = await _listener.AcceptTcpClientAsync(ct);
                    }
                    catch
                    {
                        break;
                    }

                    _ = ServeAsync(client, locoAddress, blockNumber, ct);
                }
            }

            private static async Task ServeAsync(TcpClient client, int locoAddress, int blockNumber, CancellationToken ct)
            {
                using (client)
                {
                    var stream = client.GetStream();

                    const char sep = (char)0x1B;
                    var record = Encoding.ASCII.GetBytes(
                        $"&{locoAddress}{sep}{blockNumber}{sep}00:00:00{sep}00:00:00{sep}test{sep}");

                    try
                    {
                        await stream.WriteAsync(record, 0, record.Length, ct);
                        await stream.FlushAsync(ct);

                        var buffer = new byte[64];
                        while (!ct.IsCancellationRequested)
                        {
                            int n = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                            if (n <= 0)
                                break;
                        }
                    }
                    catch
                    {
                        // Peer closed (or cancelled during shutdown): nothing to do.
                    }
                }
            }

            public async ValueTask DisposeAsync()
            {
                _cts.Cancel();
                _listener.Stop();
                try { await _acceptLoop; } catch { /* best-effort */ }
                _cts.Dispose();
            }
        }
    }
}
