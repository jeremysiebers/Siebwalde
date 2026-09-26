using System;
using System.Collections.Generic;
using System.IO;
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
    /// V3 software integration tests for the observed-neutral movement gate.
    ///
    /// These tests drive a normal locomotive-speed request end-to-end through the REAL production
    /// composition (no second implementation, no fake <see cref="IEcosHostService"/>):
    ///
    ///   <c>set(1000,speedstep[10])</c>
    ///     -> <see cref="SimpleEcosBackend"/>
    ///     -> <see cref="ControlSafetyInterlockBackend"/> (movement gate / safety interlock)
    ///     -> <see cref="TrackAmplifierHardwareBackend.SetLocoSpeed"/>
    ///     -> <see cref="TrackApplicationVariables.SetDesiredAmplifierControl"/> (shared gate)
    ///     -> <see cref="TrackControlMain"/> write loop
    ///     -> <c>EXEC_MBUS_SLAVE_DATA_EXCH</c> (108)
    ///     -> <see cref="DeterministicTrackTransport"/> (software PIC32 emulation)
    ///     -> SLAVEINFO echo
    ///     -> C# <c>HoldingReg[0]</c> readback.
    ///
    /// Software-only: the simulated HR0 readback is the PIC18 holding-register echo, NOT applied
    /// physical PWM. No claim about physical neutral or physical movement is made here.
    /// </summary>
    public class ObservedNeutralV3IntegrationTests
    {
        private static BlockTopology Topology => BlockTopology.Parse("amps: 1:1,2:2 ; routes: 1>2,2>1");

        private static KoploperBlockMap BlockMap => KoploperBlockMap.Parse("1:1.01:1, 2:1.02:2");

        private static readonly byte[] DetectedSlaves = { 1, 2, 3 };

        // ---------------------------------------------------------------------------------
        // Scenarios
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task AcceptedPath_GrantedPermission_NonNeutralRequestReachesTheTransportAndEchoes()
        {
            var groups = TrackAmplifierGroups.Create(new[] { 1, 2, 3 }, null, null);

            await RunAsync(groups, async (runtime, simulator, ecosPort, ct) =>
            {
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.True(runtime.IsMovementSafe);

                // The expected non-neutral HR0 is computed through the production mappers, so the
                // test tracks them without hardcoding the value. speedstep[10] on DCC28 normalizes
                // to 45, and the default direction (1) maps into the reverse PWM band.
                Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", 10, out var normalizedSpeed));
                var expectedPwm = AmplifierSpeedMapper.ToPwm(normalizedSpeed, direction: 1);
                var expectedHr0 = TrackApplicationVariables.BuildHr0Value(expectedPwm, emoStop: false);
                Assert.NotEqual(AmplifierSpeedMapper.NeutralPwm, (int)expectedHr0);

                var commandsBefore = simulator.SentCommands.Count;

                // The PO's normal locomotive-speed request, through the real ECoS host.
                var endLine = await SendEcosCommandAsync(ecosPort, "set(1000,speedstep[10])", TimeSpan.FromSeconds(10));

                Assert.Equal("<END 0 (OK)>", endLine);

                // The 10 Hz write loop pushes the pending non-neutral HR0 through a 108 write; the
                // simulator stores it and echoes it back as SLAVEINFO, which the C# readback mirrors.
                await WaitUntilAsync(
                    () => simulator.GetRegisters(1)[0] == expectedHr0
                          && runtime.TrackAmplifiers[1].HoldingReg[0] == expectedHr0,
                    TimeSpan.FromSeconds(5));

                // A new 108 write carried the non-neutral HR0 to the transport.
                Assert.True(simulator.SentCommands.Count > commandsBefore);
                Assert.Contains((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, simulator.SentCommands);

                // Commanded vs readback: the simulator register is the written (command) echo, and
                // the C# HoldingReg[0] is the parsed SLAVEINFO readback. Both equal the commanded
                // HR0. This is the PIC18 holding-register echo only -- NOT applied physical PWM.
                Assert.Equal(expectedHr0, simulator.GetRegisters(1)[0]);
                Assert.Equal(expectedHr0, runtime.TrackAmplifiers[1].HoldingReg[0]);
            });
        }

        [Fact]
        public async Task BlockedPath_EmptySafetyDomain_BlocksEveryCallerAndCannotBeBypassed()
        {
            // An empty safety domain is fail-closed: observed neutral can never be established, so
            // movement permission stays NotGranted.
            var groups = TrackAmplifierGroups.Empty;

            await RunAsync(groups, async (runtime, simulator, ecosPort, ct) =>
            {
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);

                // (a) The normal ECoS locomotive-speed request is refused as a safety interlock.
                var endLine = await SendEcosCommandAsync(ecosPort, "set(1000,speedstep[10])", TimeSpan.FromSeconds(10));
                Assert.Equal("<END 8 (SAFETY_INTERLOCK)>", endLine);

                // Nothing was queued, so no 108 write reaches the transport and amplifier 1 stays
                // at its initial (never-written) value.
                await Task.Delay(300, ct);
                Assert.DoesNotContain((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, simulator.SentCommands);
                Assert.Equal(0, simulator.GetRegisters(1)[0] & 0x03FF);

                // (b) A different caller (the manual/operator path) cannot bypass the gate either:
                // SetDesiredAmplifierControl refuses the non-neutral setpoint regardless of caller.
                runtime.SetAmplifierControl(1, 500, false);
                await Task.Delay(300, ct);
                Assert.DoesNotContain((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, simulator.SentCommands);
                Assert.Equal(0, simulator.GetRegisters(1)[0] & 0x03FF);
            });
        }

        // ---------------------------------------------------------------------------------
        // Harness
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Composes the REAL <see cref="TrackControlHost"/> with the deterministic simulator
        /// transport, starts the runtime in Real mode, and hands the started runtime + simulator to
        /// <paramref name="testBody"/>. A stub Koploper external-info server reports loco 1000 in
        /// block 1 so the real hardware backend can resolve the locomotive to block/amplifier 1.
        /// </summary>
        private static async Task RunAsync(
            TrackAmplifierGroups groups,
            Func<TrackApplicationRuntimeHost, DeterministicTrackTransport, int, CancellationToken, Task> testBody)
        {
            // The real-mode init pipeline reads the firmware hex in FlashFwTrackamplifiersStep even
            // when flashing is skipped, so the test must not depend on the (untracked) repo dist/
            // hex being present. Generate a minimal self-contained hex whose checksum equals the
            // simulator's FirmwareChecksum (0x251F).
            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-fw-{Guid.NewGuid():N}.hex");
            var locoPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-locos-{Guid.NewGuid():N}.json");
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

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await runtime.StartAsync(TrackControlMode.Real, timeout.Token);

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

        /// <summary>Reserves and releases a port so the test does not collide with 15471/5700.</summary>
        private static int GetFreeTcpPort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        /// <summary>
        /// Connects to the ECoS listener, sends one ASCII command (which ends with ')'), and reads
        /// the reply's <c>&lt;END ...&gt;</c> line. The initial feedback <c>&lt;EVENT ...&gt;</c> the
        /// backend emits on first attach is skipped.
        /// </summary>
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
                    return;

                await Task.Delay(25);
            }

            throw new TimeoutException($"Condition was not met within {timeout}.");
        }

        /// <summary>
        /// Writes a minimal Intel HEX that <see cref="TrackAmplifierBootloaderHelpers.Execute"/>
        /// parses to a file checksum of 0x251F (the simulator's <see cref="TrackSimulatorConfig.DefaultFirmwareChecksum"/>).
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

            // One config row: record length 0x0C (12 bytes -> 24 hex chars). Its content is only
            // used when flashing, which is skipped, so any 12 bytes are acceptable.
            sb.Append(':').Append("0C").Append("0000").Append("00").Append(new string('F', 24)).Append("00").AppendLine();

            File.WriteAllText(path, sb.ToString());
        }

        /// <summary>
        /// Minimal stub for Koploper's external-information server. The real
        /// <see cref="KoploperExternalInfoClient"/> connects OUT to this port and reads records,
        /// so this stub reports "loco <c>locoAddress</c> -> block <c>blockNumber</c>", which is how
        /// a locomotive obtains a known block in a software-only test.
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

                    // Koploper external-info record: 5 ASCII fields separated by 0x1B.
                    // fields[0] = "&<loco>", fields[1] = "<block>", fields[2..4] = times/description.
                    // Use an explicit (char)0x1B separator: a "\x1B" escape is greedy over following
                    // hex digits (the "00:00:00" time fields), so it must not be used here.
                    const char sep = (char)0x1B;
                    var record = Encoding.ASCII.GetBytes(
                        $"&{locoAddress}{sep}{blockNumber}{sep}00:00:00{sep}00:00:00{sep}test{sep}");

                    try
                    {
                        await stream.WriteAsync(record, 0, record.Length, ct);
                        await stream.FlushAsync(ct);

                        // Hold the connection open (the client sends nothing) so it does not enter
                        // a reconnect loop. Exit when the peer closes or we are cancelled.
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
