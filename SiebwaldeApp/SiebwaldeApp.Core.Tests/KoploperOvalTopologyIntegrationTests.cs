using System;
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
    /// Integration tests for the Koploper Oval profile (5-block passing loop with switches 1 and 2)
    /// driven through the REAL FullSimulation chain (<see cref="TrackControlHost"/> +
    /// <see cref="TrackApplicationRuntimeHost"/> + <see cref="DeterministicTrackTransport"/>).
    /// The switch-conditional branch 3→4 (switch 1 straight) vs 3→5 (switch 1 diverging) is selected
    /// through the normal ECoS switch command path and observed through the movement simulator +
    /// amplifier occupancy feedback.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class KoploperOvalTopologyIntegrationTests
    {
        private const string KoploperOvalJson = @"
{
  ""name"": ""Koploper Oval"",
  ""detectedSlaves"": [1,2,3,4,5],
  ""sections"": [
    { ""id"": 1, ""amplifierSlave"": 1, ""bezetmelders"": [""1.01"",""1.02""], ""lengthMm"": 1000.0 },
    { ""id"": 2, ""amplifierSlave"": 2, ""bezetmelders"": [""1.03"",""1.04""], ""lengthMm"": 1000.0 },
    { ""id"": 3, ""amplifierSlave"": 3, ""bezetmelders"": [""1.05"",""1.06""], ""lengthMm"": 1000.0 },
    { ""id"": 4, ""amplifierSlave"": 4, ""bezetmelders"": [""1.07"",""1.08""], ""lengthMm"": 1000.0 },
    { ""id"": 5, ""amplifierSlave"": 5, ""bezetmelders"": [""1.09"",""1.10""], ""lengthMm"": 1000.0 }
  ],
  ""blocks"": [
    { ""id"": 1, ""sectionIds"": [1] },
    { ""id"": 2, ""sectionIds"": [2] },
    { ""id"": 3, ""sectionIds"": [3] },
    { ""id"": 4, ""sectionIds"": [4] },
    { ""id"": 5, ""sectionIds"": [5] }
  ],
  ""switches"": [
    { ""ecosAddress"": 1, ""physicalAddress"": 1 },
    { ""ecosAddress"": 2, ""physicalAddress"": 2 }
  ],
  ""routes"": [
    { ""fromBlock"": 1, ""toBlock"": 2 },
    { ""fromBlock"": 2, ""toBlock"": 3 },
    { ""fromBlock"": 3, ""toBlock"": 4, ""switchId"": 1, ""requiredSwitchPosition"": ""straight"" },
    { ""fromBlock"": 3, ""toBlock"": 5, ""switchId"": 1, ""requiredSwitchPosition"": ""diverging"" },
    { ""fromBlock"": 4, ""toBlock"": 1 },
    { ""fromBlock"": 5, ""toBlock"": 1 }
  ],
  ""locomotives"": [
    { ""address"": 1, ""initialBlock"": 1 },
    { ""address"": 2, ""initialBlock"": 3 }
  ]
}";

        [Fact]
        public async Task KoploperOval_SwitchStraight_Loco1_Goes3To4_Then4To1()
        {
            await RunBranchAsync(
                switchCommand: "set(11, switch[1g])",
                test: async (runtime, ecosPort, ct) =>
                {
                    // Drive loco 1 (decoder address 1) forward; with switch 1 straight the
                    // branch from block 3 resolves to block 4, then 4→1.
                    Assert.Equal("<END 0 (OK)>", await SendEcosCommandAsync(ecosPort, "set(1000, dir[0])", TimeSpan.FromSeconds(10)));
                    Assert.Equal("<END 0 (OK)>", await SendEcosCommandAsync(ecosPort, "set(1000, speedstep[10])", TimeSpan.FromSeconds(10)));

                    await WaitUntilAsync(() => LocoBlock(runtime, 1) == 4, TimeSpan.FromSeconds(30));
                    await WaitUntilAsync(
                        () => TrackAmplifierRegisters.IsOccupied(runtime.TrackAmplifiers[4].HoldingReg),
                        TimeSpan.FromSeconds(10));

                    await WaitUntilAsync(() => LocoBlock(runtime, 1) == 1, TimeSpan.FromSeconds(30));
                    await WaitUntilAsync(
                        () => TrackAmplifierRegisters.IsOccupied(runtime.TrackAmplifiers[1].HoldingReg),
                        TimeSpan.FromSeconds(10));
                });
        }

        [Fact]
        public async Task KoploperOval_SwitchDiverging_Loco1_Goes3To5_Then5To1()
        {
            await RunBranchAsync(
                switchCommand: "set(11, switch[1r])",
                test: async (runtime, ecosPort, ct) =>
                {
                    Assert.Equal("<END 0 (OK)>", await SendEcosCommandAsync(ecosPort, "set(1000, dir[0])", TimeSpan.FromSeconds(10)));
                    Assert.Equal("<END 0 (OK)>", await SendEcosCommandAsync(ecosPort, "set(1000, speedstep[10])", TimeSpan.FromSeconds(10)));

                    await WaitUntilAsync(() => LocoBlock(runtime, 1) == 5, TimeSpan.FromSeconds(30));
                    await WaitUntilAsync(
                        () => TrackAmplifierRegisters.IsOccupied(runtime.TrackAmplifiers[5].HoldingReg),
                        TimeSpan.FromSeconds(10));

                    await WaitUntilAsync(() => LocoBlock(runtime, 1) == 1, TimeSpan.FromSeconds(30));
                    await WaitUntilAsync(
                        () => TrackAmplifierRegisters.IsOccupied(runtime.TrackAmplifiers[1].HoldingReg),
                        TimeSpan.FromSeconds(10));
                });
        }

        [Fact]
        public async Task KoploperOval_UnknownSwitch_Loco1StopsAtBlock3()
        {
            await RunBranchAsync(
                switchCommand: null,
                test: async (runtime, ecosPort, ct) =>
                {
                    // With switch 1 left unknown, both switch-conditional routes from block 3 are
                    // skipped and the locomotive must halt at block 3 rather than pass through.
                    Assert.Equal("<END 0 (OK)>", await SendEcosCommandAsync(ecosPort, "set(1000, dir[0])", TimeSpan.FromSeconds(10)));
                    Assert.Equal("<END 0 (OK)>", await SendEcosCommandAsync(ecosPort, "set(1000, speedstep[10])", TimeSpan.FromSeconds(10)));

                    await WaitUntilAsync(() => LocoBlock(runtime, 1) == 3, TimeSpan.FromSeconds(30));

                    await Task.Delay(700, ct);
                    Assert.Equal(3, LocoBlock(runtime, 1));
                });
        }

        // ---------------------------------------------------------------------------------
        // Harness
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Composes the REAL chain from the Koploper Oval profile, starts FullSimulation, maps
        /// ECoS object id 1000 to decoder address 1, sets the switch (when requested) and asserts
        /// the baseline profile/placement before running <paramref name="test"/>.
        /// </summary>
        private static async Task RunBranchAsync(
            string? switchCommand,
            Func<TrackApplicationRuntimeHost, int, CancellationToken, Task> test)
        {
            Assert.True(LayoutProfileLoader.TryLoad(KoploperOvalJson, out var profile, out var loadErrors), string.Join("; ", loadErrors));
            var composition = FullSimulationProfile.Compose(profile!);

            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-koploper-fw-{Guid.NewGuid():N}.hex");
            var locoPath = Path.Combine(Path.GetTempPath(), $"siebwalde-koploper-locos-{Guid.NewGuid():N}.json");
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

                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.Equal("Koploper Oval", runtime.ActiveProfileName);
                Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, runtime.SimulatedTrackIo!.DetectedSlaves.ToArray());

                // Both locomotives are placed from the profile: decoder address 1 in block 1,
                // decoder address 2 in block 3.
                var positions = runtime.MovementSimulation!.GetLocoPositions();
                Assert.Contains(positions, p => p.Address == 1 && p.BlockId == 1);
                Assert.Contains(positions, p => p.Address == 2 && p.BlockId == 3);

                // Bind ECoS object id 1000 to decoder address 1, then (optionally) set the switch.
                Assert.Equal("<END 0 (OK)>", await SendEcosCommandAsync(ecosPort, "set(1000, addr[1])", TimeSpan.FromSeconds(10)));
                if (switchCommand is not null)
                {
                    Assert.Equal("<END 0 (OK)>", await SendEcosCommandAsync(ecosPort, switchCommand, TimeSpan.FromSeconds(10)));
                }

                await test(runtime, ecosPort, timeout.Token);
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

        private static int? LocoBlock(TrackApplicationRuntimeHost runtime, int address)
            => runtime.MovementSimulation?.GetLocoPositions().FirstOrDefault(p => p.Address == address)?.BlockId;

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
