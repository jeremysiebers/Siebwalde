using System;
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
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Integration tests for the config-driven FullSimulation: a real
    /// <see cref="TrackControlHost"/> + <see cref="TrackApplicationRuntimeHost"/> composed from a
    /// <see cref="LayoutProfile"/> (the example oval), started in
    /// <see cref="TrackControlMode.FullSimulation"/>. A locomotive is driven through the real ECoS
    /// command path; the movement simulator shifts occupancy through the real transport -> comm ->
    /// occupancy-bridge -> ECoS sensor-event path.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class FullSimulationTopologyIntegrationTests
    {
        private const string OvalJson = @"
{
  ""name"": ""Example Oval"",
  ""detectedSlaves"": [1,2,3,4],
  ""sections"": [
    { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [""1.01"",""1.02""], ""lengthMm"": 1000.0 },
    { ""id"": 2, ""amplifierSlave"": 2, ""bezetmelders"": [""1.03"",""1.04""], ""lengthMm"": 1000.0 },
    { ""id"": 3, ""amplifierSlave"": 3, ""bezetmelders"": [""1.05"",""1.06""], ""lengthMm"": 1000.0 },
    { ""id"": 4, ""amplifierSlave"": 4, ""bezetmelders"": [""1.07"",""1.08""], ""lengthMm"": 1000.0 }
  ],
  ""blocks"": [
    { ""id"": 1, ""sectionIds"": [1] },
    { ""id"": 2, ""sectionIds"": [2] },
    { ""id"": 3, ""sectionIds"": [3] },
    { ""id"": 4, ""sectionIds"": [4] }
  ],
  ""switches"": [],
  ""routes"": [
    { ""fromBlock"": 1, ""toBlock"": 2 },
    { ""fromBlock"": 2, ""toBlock"": 3 },
    { ""fromBlock"": 3, ""toBlock"": 4 },
    { ""fromBlock"": 4, ""toBlock"": 1 }
  ],
  ""locomotives"": [
    { ""address"": 1000, ""initialBlock"": 1 },
    { ""address"": 1001, ""initialBlock"": 3 }
  ]
}";

        [Fact]
        public async Task FullSimulation_OvalProfile_DrivesLoco_AndShiftsOccupancy()
        {
            Assert.True(LayoutProfileLoader.TryLoad(OvalJson, out var profile, out var loadErrors), string.Join("; ", loadErrors));
            var composition = FullSimulationProfile.Compose(profile!);

            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-topology-fw-{Guid.NewGuid():N}.hex");
            var locoPath = Path.Combine(Path.GetTempPath(), $"siebwalde-topology-locos-{Guid.NewGuid():N}.json");
            var ecosPort = GetFreeTcpPort();
            var externalPort = GetFreeTcpPort();

            FakeKoploperExternalInfoServer? externalServer = null;
            TrackApplicationRuntimeHost? runtime = null;

            try
            {
                WriteTestFirmwareHex(tempHexPath);
                CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

                externalServer = new FakeKoploperExternalInfoServer(externalPort, locoAddress: 1000, blockNumber: 1);

                var ecosHost = new TrackControlHost(
                    locoRepositoryPath: locoPath,
                    topology: composition.BlockTopology,
                    blockMap: composition.KoploperBlockMap,
                    switchMapping: composition.SwitchMapping,
                    ecosListenPort: ecosPort,
                    koploperExternalInfoHost: "127.0.0.1",
                    koploperExternalInfoPort: externalPort,
                    trackAmplifierGroups: composition.TrackAmplifierGroups);

                runtime = new TrackApplicationRuntimeHost(
                    ecosHost,
                    controlTrace: null,
                    amplifierGroups: composition.TrackAmplifierGroups,
                    simulatorConfig: composition.SimulatorConfig,
                    fullSimulationProfile: composition.Profile);

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
                await runtime.StartAsync(TrackControlMode.FullSimulation, timeout.Token);

                // AC1: the profile drives the FullSimulation runtime (detected slaves + movement sim).
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.Equal("Example Oval", runtime.ActiveProfileName);
                Assert.NotNull(runtime.MovementSimulation);
                Assert.Equal(new byte[] { 1, 2, 3, 4 }, runtime.SimulatedTrackIo!.DetectedSlaves.ToArray());

                var simulator = Assert.IsType<DeterministicTrackTransport>(runtime.SimulatedTrackIo);

                // AC2: drive a locomotive through the real ECoS path.
                Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", 10, out var normalizedSpeed));
                var expectedPwm = AmplifierSpeedMapper.ToPwm(normalizedSpeed, direction: 0);
                var expectedHr0 = TrackApplicationVariables.BuildHr0Value(expectedPwm, emoStop: false);

                // Forward direction first, then the speedstep command.
                var dirLine = await SendEcosCommandAsync(ecosPort, "set(1000,dir[0])", TimeSpan.FromSeconds(10));
                Assert.Equal("<END 0 (OK)>", dirLine);

                var speedLine = await SendEcosCommandAsync(ecosPort, "set(1000,speedstep[10])", TimeSpan.FromSeconds(10));
                Assert.Equal("<END 0 (OK)>", speedLine);

                // The 108 write reaches the transport with the correct HR0 on amplifier 1.
                await WaitUntilAsync(
                    () => simulator.GetRegisters(1)[0] == expectedHr0
                          && runtime.TrackAmplifiers[1].HoldingReg[0] == expectedHr0,
                    TimeSpan.FromSeconds(10));
                Assert.Contains((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, simulator.SentCommands);

                // AC3: occupancy shifts to the next section (block 1 -> block 2) as ECoS sensor events.
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, ecosPort, timeout.Token);
                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
                using var reader = new StreamReader(stream, Encoding.ASCII);

                await writer.WriteAsync("request(100,view)");
                await writer.FlushAsync();
                await ReadUntilAsync(reader, line => line.StartsWith("<REPLY request(100,view)>", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
                await ReadUntilAsync(reader, line => line.StartsWith("<END ", StringComparison.Ordinal), TimeSpan.FromSeconds(10));

                // Read until sensor 3 (block 2's first bezetmelder, bit 2) becomes occupied.
                var stateLine = await ReadUntilStateAsync(
                    reader,
                    mask => (mask & (1 << 2)) != 0,
                    TimeSpan.FromSeconds(20));

                var finalMask = ParseStateValue(stateLine);
                Assert.True((finalMask & (1 << 2)) != 0, "Block 2 (sensor 3) should be occupied.");
                Assert.True((finalMask & (1 << 0)) == 0, "Block 1 (sensor 1) should have cleared.");

                // AC4: stop + restart yields no stale occupancy.
                await runtime.StopAsync(timeout.Token);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
                Assert.Null(runtime.SimulatedTrackIo);
                Assert.Null(runtime.MovementSimulation);

                await runtime.StartAsync(TrackControlMode.FullSimulation, timeout.Token);
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);

                // Fresh start re-places the profile locomotives and does not carry stale occupancy:
                // section 1 (block 1) is occupied again from the profile's initial placement, and
                // section 2 (block 2) is free until a locomotive is driven again.
                var freshSimulator = Assert.IsType<DeterministicTrackTransport>(runtime.SimulatedTrackIo);
                await WaitUntilAsync(
                    () => TrackAmplifierRegisters.IsOccupied(runtime.TrackAmplifiers[1].HoldingReg),
                    TimeSpan.FromSeconds(10));
                Assert.False(TrackAmplifierRegisters.IsOccupied(runtime.TrackAmplifiers[2].HoldingReg));
                Assert.Equal(AmplifierSpeedMapper.NeutralPwm, freshSimulator.GetRegisters(1)[0] & 0x03FF); // neutral, not stale movement
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

        // ---------------------------------------------------------------------------------
        // Harness helpers
        // ---------------------------------------------------------------------------------

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

        /// <summary>Reads ECoS lines until a "100 state[...]" line satisfies the mask predicate.</summary>
        private static async Task<string> ReadUntilStateAsync(StreamReader reader, Func<int, bool> predicate, TimeSpan timeout)
        {
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
                    throw new TimeoutException($"Timed out waiting for an ECoS sensor state after {timeout}.");
                }

                if (line is null)
                {
                    throw new IOException("ECoS connection closed while waiting for a sensor state.");
                }

                if (line.StartsWith("100 state[", StringComparison.Ordinal))
                {
                    var mask = ParseStateValue(line);
                    if (predicate(mask))
                    {
                        return line;
                    }
                }
            }

            throw new TimeoutException($"Timed out waiting for an ECoS sensor state after {timeout}.");
        }

        private static int ParseStateValue(string stateLine)
        {
            var open = stateLine.IndexOf('[');
            var close = stateLine.IndexOf(']', open);
            var valueText = stateLine.Substring(open + 1, close - open - 1).Trim();

            return valueText.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt32(valueText.Substring(2), 16)
                : int.Parse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture);
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

        private static void WriteTestFirmwareHex(string path)
        {
            const int dataRows = (0x8000 - 0x800) / 16;

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

        /// <summary>Minimal stub for Koploper's external-information server (loco -> block).</summary>
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
                            {
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Peer closed (or cancelled during shutdown).
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
