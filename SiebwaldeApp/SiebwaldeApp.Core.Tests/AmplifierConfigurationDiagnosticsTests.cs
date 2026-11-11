using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Focused diagnostics tests for the Phase-1 config path. They exercise the PRODUCTION
    /// <see cref="AmplifierConfigurationDownloader"/> (and, for the "never invoked" scenario, the
    /// production <see cref="TrackApplicationRuntimeHost"/>) over the existing in-process
    /// <see cref="DeterministicTrackTransport"/>. Each test captures the diagnostic <c>log</c> sink
    /// and asserts it distinguishes the scenario by the emitted state vocabulary (ENTRY/REQUESTED/
    /// TRANSMITTED/OBSERVED/VERIFIED/FAILED/RETURN). Software/protocol only: no hardware, no live
    /// application.
    /// </summary>
    public class AmplifierConfigurationDiagnosticsTests
    {
        // ---------------------------------------------------------------------
        // Success
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Success_LogsEntryRequestedTransmittedVerifiedAndReturnTrue()
        {
            var (client, _, item) = await StartDetectedAsync(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 })).ConfigureAwait(false);
            try
            {
                var lines = new List<string>();
                var downloader = new AmplifierConfigurationDownloader(client, lines.Add);

                var ok = await downloader.DownloadAsync(item, new AmplifierConfiguration(), CancellationToken.None);

                Assert.True(ok);
                Assert.True(item.ConfigurationVerified);
                Assert.Null(item.Phase1Failure);

                Assert.Contains(lines, l => l.StartsWith("slave=1 ENTRY"));
                Assert.Contains(lines, l => l.Contains("REQUESTED HR3="));
                Assert.Contains(lines, l => l.Contains("REQUESTED HR4="));
                Assert.Contains(lines, l => l.Contains("REQUESTED HR5="));
                Assert.Contains(lines, l => l.Contains("REQUESTED HR6="));
                Assert.Contains(lines, l => l.Contains("TRANSMITTED write HR3="));
                Assert.Contains(lines, l => l.Contains("VERIFIED HR3="));
                Assert.Contains(lines, l => l.Contains("REQUESTED APPLY HR1=0x2000"));
                Assert.Contains(lines, l => l.Contains("TRANSMITTED APPLY HR1=0x2000"));
                Assert.Contains(lines, l => l.Contains("VERIFIED APPLY"));
                Assert.Contains(lines, l => l.Contains("RETURN true (configured+verified)"));
            }
            finally
            {
                await client.DisposeAsync();
            }
        }

        // ---------------------------------------------------------------------
        // Early precondition failure (incompatible) — no write body reached
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Incompatible_LogsPreconditionDecomposition_NoWrites()
        {
            var (client, _, item) = await StartDetectedAsync(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 },
                protocolVersionOverrides: new Dictionary<byte, ushort> { { 1, 0x00 } })).ConfigureAwait(false);
            try
            {
                var lines = new List<string>();
                var downloader = new AmplifierConfigurationDownloader(client, lines.Add);

                var ok = await downloader.DownloadAsync(item, new AmplifierConfiguration(), CancellationToken.None);

                Assert.False(ok);
                Assert.False(item.ConfigurationVerified);
                Assert.NotNull(item.Phase1Failure);

                // Fresh + identity OK, protocol rejected -> the decomposition must name protocol.
                Assert.Contains(lines, l => l.Contains("FAILED precondition incompatible"));
                Assert.Contains(lines, l => l.Contains("fresh=True"));
                Assert.Contains(lines, l => l.Contains("identityOk=True"));
                Assert.Contains(lines, l => l.Contains("protocolOk=False"));
                Assert.Contains(lines, l => l.Contains("RETURN false (incompatible)"));

                // The write body is never reached.
                Assert.DoesNotContain(lines, l => l.Contains("REQUESTED HR"));
                Assert.DoesNotContain(lines, l => l.Contains("TRANSMITTED"));
            }
            finally
            {
                await client.DisposeAsync();
            }
        }

        // ---------------------------------------------------------------------
        // Config APPLY rejected (out-of-range) -> writes verify, APPLY never confirms
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ConfigApplyRejected_LogsWritesVerifiedThenApplyTimeout()
        {
            var (client, _, item) = await StartDetectedAsync(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 })).ConfigureAwait(false);
            try
            {
                var lines = new List<string>();
                var downloader = new AmplifierConfigurationDownloader(client, lines.Add)
                {
                    Timeout = TimeSpan.FromMilliseconds(300)
                };

                // Watchdog 30 (< 50) is accepted into the pending config (echoed) but rejected at
                // APPLY, so INIT_DONE|CONFIG_OK is never set and the APPLY verify loop times out.
                var ok = await downloader.DownloadAsync(
                    item, new AmplifierConfiguration(WatchdogMs: 30), CancellationToken.None);

                Assert.False(ok);
                Assert.False(item.ConfigurationVerified);
                Assert.NotNull(item.Phase1Failure);

                // The writes all echo-verify (the simulator stages them in "pending"), then the
                // APPLY is transmitted but never confirms.
                Assert.Contains(lines, l => l.Contains("VERIFIED HR3="));
                Assert.Contains(lines, l => l.Contains("VERIFIED HR5=0x001E"));
                Assert.Contains(lines, l => l.Contains("VERIFIED HR6="));
                Assert.Contains(lines, l => l.Contains("TRANSMITTED APPLY HR1=0x2000"));
                Assert.DoesNotContain(lines, l => l.Contains("VERIFIED APPLY"));
                Assert.Contains(lines, l => l.Contains("RETURN false (timeout)"));
            }
            finally
            {
                await client.DisposeAsync();
            }
        }

        // ---------------------------------------------------------------------
        // Config write timeout (write received, no echo) -> stale readback
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ConfigWriteTimeout_DroppedEcho_LogsTimeout()
        {
            var (client, _, item) = await StartDetectedAsync(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 },
                droppedEchoSlaves: new byte[] { 1 })).ConfigureAwait(false);
            try
            {
                var lines = new List<string>();
                var downloader = new AmplifierConfigurationDownloader(client, lines.Add)
                {
                    Timeout = TimeSpan.FromMilliseconds(300)
                };

                var ok = await downloader.DownloadAsync(item, new AmplifierConfiguration(), CancellationToken.None);

                Assert.False(ok);
                Assert.False(item.ConfigurationVerified);
                Assert.NotNull(item.Phase1Failure);

                // The write is transmitted but no echo arrives, so HR3 is never VERIFIED.
                Assert.Contains(lines, l => l.Contains("TRANSMITTED write HR3="));
                Assert.DoesNotContain(lines, l => l.Contains("VERIFIED HR3="));
                Assert.Contains(lines, l => l.Contains("RETURN false (timeout)"));
            }
            finally
            {
                await client.DisposeAsync();
            }
        }

        // ---------------------------------------------------------------------
        // APPLY not confirmed (config writes succeed, APPLY edge lost)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ApplyNotConfirmed_LogsTransmittedApplyThenTimeout()
        {
            var inner = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 }));
            var transport = new ApplyDropTransport(inner);
            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(transport, variables);
            var item = await DetectAsync(client, variables).ConfigureAwait(false);
            try
            {
                var lines = new List<string>();
                var downloader = new AmplifierConfigurationDownloader(client, lines.Add)
                {
                    Timeout = TimeSpan.FromMilliseconds(300)
                };

                var ok = await downloader.DownloadAsync(item, new AmplifierConfiguration(), CancellationToken.None);

                Assert.False(ok);
                Assert.False(item.ConfigurationVerified);
                Assert.NotNull(item.Phase1Failure);

                // Every config write is VERIFIED, the APPLY is transmitted, but INIT_DONE|CONFIG_OK
                // never confirms -> the APPLY verify loop times out.
                Assert.Contains(lines, l => l.Contains("VERIFIED HR3="));
                Assert.Contains(lines, l => l.Contains("VERIFIED HR6="));
                Assert.Contains(lines, l => l.Contains("TRANSMITTED APPLY HR1=0x2000"));
                Assert.DoesNotContain(lines, l => l.Contains("VERIFIED APPLY"));
                Assert.Contains(lines, l => l.Contains("RETURN false (timeout)"));
            }
            finally
            {
                await client.DisposeAsync();
            }
        }

        // ---------------------------------------------------------------------
        // Stale or missing readback -> freshness precondition fail (fresh=False)
        // ---------------------------------------------------------------------

        [Theory]
        [InlineData(false)] // missing: never observed
        [InlineData(true)]  // stale: observed once, now > 2 s old
        public async Task StaleOrMissingReadback_LogsFreshFalsePrecondition(bool stale)
        {
            var regs = new ushort[TrackAmplifierRegisters.Count];
            regs[TrackAmplifierRegisters.Identity] = SimulatedTrackAmplifier.Phase1Identity;
            regs[TrackAmplifierRegisters.FwVersionPatchProtocol] = TrackAmplifierRegisters.Phase1ProtocolVersion;
            var item = new TrackAmplifierItem
            {
                SlaveNumber = 1,
                SlaveDetected = stale ? (ushort)1 : (ushort)0,
                HoldingReg = regs,
                ProtocolReadbackObserved = stale,
                LastDataReceivedUtc = stale ? DateTimeOffset.UtcNow - TimeSpan.FromSeconds(10) : null
            };

            var comm = new RecordingCommClient();
            var lines = new List<string>();
            var downloader = new AmplifierConfigurationDownloader(comm, lines.Add);

            var ok = await downloader.DownloadAsync(item, new AmplifierConfiguration(), CancellationToken.None);

            Assert.False(ok);
            Assert.False(item.ConfigurationVerified);
            // Identity/protocol are fine, so the reason falls back to "incompatible firmware", but
            // the decomposition must expose fresh=False as the actual failing gate.
            Assert.Contains(lines, l => l.Contains("FAILED precondition incompatible"));
            Assert.Contains(lines, l => l.Contains("fresh=False"));
            Assert.Contains(lines, l => l.Contains("identityOk=True"));
            Assert.Contains(lines, l => l.Contains("protocolOk=True"));
            Assert.DoesNotContain(lines, l => l.Contains("REQUESTED HR"));
        }

        // ---------------------------------------------------------------------
        // Cancellation (caller token) -> escapes as OperationCanceledException
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Cancellation_EscapesAsOperationCanceledException_AndLogs()
        {
            var (client, _, item) = await StartDetectedAsync(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 })).ConfigureAwait(false);
            try
            {
                var lines = new List<string>();
                var downloader = new AmplifierConfigurationDownloader(client, lines.Add);
                using var cts = new CancellationTokenSource();
                cts.Cancel();

                // A caller cancellation is NOT a timeout: it must propagate (existing behavior).
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => downloader.DownloadAsync(item, new AmplifierConfiguration(), cts.Token));

                Assert.Contains(lines, l => l.Contains("cancellation"));
            }
            finally
            {
                await client.DisposeAsync();
            }
        }

        // ---------------------------------------------------------------------
        // Exception (transport throws) -> logged and returned false
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Exception_LogsExceptionAndReturnFalse()
        {
            var regs = new ushort[TrackAmplifierRegisters.Count];
            regs[TrackAmplifierRegisters.Identity] = SimulatedTrackAmplifier.Phase1Identity;
            regs[TrackAmplifierRegisters.FwVersionPatchProtocol] = TrackAmplifierRegisters.Phase1ProtocolVersion;
            var item = new TrackAmplifierItem
            {
                SlaveNumber = 1,
                SlaveDetected = 1,
                HoldingReg = regs,
                ProtocolReadbackObserved = true,
                LastDataReceivedUtc = DateTimeOffset.UtcNow
            };

            var comm = new ThrowingCommClient();
            var lines = new List<string>();
            var downloader = new AmplifierConfigurationDownloader(comm, lines.Add);

            var ok = await downloader.DownloadAsync(item, new AmplifierConfiguration(), CancellationToken.None);

            Assert.False(ok);
            Assert.False(item.ConfigurationVerified);
            Assert.StartsWith("configuration download failed:", item.Phase1Failure);
            Assert.Contains(lines, l => l.Contains("FAILED exception"));
            Assert.Contains(lines, l => l.Contains("RETURN false (exception)"));
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        private static async Task<(TrackCommClientAsync client, DeterministicTrackTransport transport, TrackAmplifierItem item)>
            StartDetectedAsync(TrackSimulatorConfig config, byte slave = 1)
        {
            var variables = new TrackApplicationVariables();
            var transport = new DeterministicTrackTransport(config);
            var client = new TrackCommClientAsync(transport, variables);
            var item = await DetectAsync(client, variables, slave).ConfigureAwait(false);
            return (client, transport, item);
        }

        private static async Task<TrackAmplifierItem> DetectAsync(
            TrackCommClientAsync client, TrackApplicationVariables variables, byte slave = 1)
        {
            await client.StartAsync(false);
            await client.SendAsync(new SendMessage(TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT, Array.Empty<byte>()));
            await WaitUntilAsync(() => variables.trackAmpItems[slave].ProtocolReadbackObserved, TimeSpan.FromSeconds(2));
            return variables.trackAmpItems[slave];
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

                await Task.Delay(10);
            }

            throw new TimeoutException($"Condition was not met within {timeout}.");
        }

        /// <summary>Wraps a transport and drops the APPLY write (108 write to HR1 = 0x2000) so
        /// INIT_DONE|CONFIG_OK is never set. Delegates everything else (a controlled in-process
        /// transport, not a controller emulator).</summary>
        private sealed class ApplyDropTransport : ITrackTransport
        {
            private const byte WriteDirection = 0xAA;
            private readonly ITrackTransport _inner;
            public ApplyDropTransport(ITrackTransport inner) => _inner = inner;

            public Task OpenAsync(CancellationToken cancellationToken = default) => _inner.OpenAsync(cancellationToken);
            public Task CloseAsync(CancellationToken cancellationToken = default) => _inner.CloseAsync(cancellationToken);

            public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
            {
                // [0]=0xAA [1]=108 [2]=slave [3]=dir [4]=regCount [5]=startReg [6..7]=value LE
                if (count >= 8 &&
                    buffer[offset + 1] == TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH &&
                    buffer[offset + 3] == WriteDirection &&
                    buffer[offset + 5] == TrackAmplifierRegisters.Control &&
                    (buffer[offset + 6] | (buffer[offset + 7] << 8)) == TrackAmplifierRegisters.ApplyConfigBit)
                {
                    return Task.CompletedTask; // drop the APPLY write
                }

                return _inner.SendAsync(buffer, offset, count, cancellationToken);
            }

            public IAsyncEnumerable<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
                => _inner.ReceiveAsync(cancellationToken);

            public ValueTask DisposeAsync() => _inner.DisposeAsync();
        }

        private sealed class RecordingCommClient : ITrackCommClient
        {
            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;
            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public ValueTask DisposeAsync() => default;
        }

        private sealed class ThrowingCommClient : ITrackCommClient
        {
            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;
            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("boom");
            public ValueTask DisposeAsync() => default;
        }
    }

    /// <summary>
    /// Host-level diagnostic test: a full <see cref="TrackApplicationRuntimeHost"/> Real-mode start
    /// over the in-process transport, capturing the production Phase-1 config log sink. Proves the
    /// host logs "SKIPPED incompatible" (and never "INVOKE DownloadAsync") when an amplifier is
    /// detected but fails the compatibility gate — the exact distinction the Stage-4 investigation
    /// needs to tell "never invoked" from "invoked and failed".
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class TrackApplicationRuntimeHostConfigDiagnosticsTests
    {
        private sealed class EcosHost : IEcosHostService
        {
            public event EventHandler<RuntimeFaultEventArgs>? Faulted { add { } remove { } }
            public bool IsRunning { get; private set; }
            public TrackControlMode? Mode { get; private set; }
            public ControlDiagnostics? Diagnostics => null;
            public bool IsUnsafe => false;
            public bool ResetSafety() => false;
            public void SetProfile(LayoutProfile profile) { }
            public IReadOnlyDictionary<int, SwitchPosition> GetSwitchPositions() => new Dictionary<int, SwitchPosition>();
            public Task<EcosHostStartResult> StartAsync(TrackControlMode mode, ITrackCommClient? commClient,
                TrackApplicationVariables? variables, CancellationToken cancellationToken = default,
                IMovementSimulation? movementSimulation = null)
            { IsRunning = true; Mode = mode; return Task.FromResult(EcosHostStartResult.Started); }
            public void Stop() { IsRunning = false; Mode = null; }
            public Task<EcosHostStopResult> StopAsync(CancellationToken ct = default)
            { Stop(); return Task.FromResult(EcosHostStopResult.Stopped); }
        }

        private sealed class CaptureLogger : ILogger
        {
            private readonly object _sync = new();
            private readonly List<string> _lines = new();
            public IReadOnlyList<string> Lines { get { lock (_sync) return _lines.ToList(); } }

            public void Log(string message, LogLevel level, string loggerinstance)
            {
                if (loggerinstance == "TrackAppLog" && message.Contains("P1CFG-"))
                {
                    lock (_sync) _lines.Add(message);
                }
            }
        }

        [Fact]
        public async Task Host_NeverInvokesDownloadAsync_WhenAmplifierIncompatible()
        {
            // Detect THREE slaves so DetectSlavesStep's hardcoded _minRequiredSlaves==3 gate is met
            // (otherwise the in-process transport cannot emulate the recover/flash handshake and the
            // init run stalls). Only amplifier 1 is configured into the safety domain, and only
            // amplifier 1 is protocol-0x00 (incompatible for write/control authority).
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1, 2, 3 },
                protocolVersionOverrides: new Dictionary<byte, ushort> { { 1, 0x00 } },
                periodicSlaveInfoInterval: TimeSpan.FromMilliseconds(50)));

            var capture = new CaptureLogger();
            SiebwaldeApp.Core.IoC.Logger.AddLogger(capture);

            // The real-mode init pipeline reads the firmware hex in FlashFwTrackamplifiersStep even
            // when flashing is skipped, so the test points the setting at a minimal self-contained
            // hex (0x0E39 checksum) and restores it afterwards — same as the existing end-to-end tests.
            var originalFwPath = SiebwaldeApp.Core.Properties.CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"p1cfg-{Guid.NewGuid():N}.hex");
            try
            {
                WriteTestFirmwareHex(tempHexPath);
                SiebwaldeApp.Core.Properties.CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

                await using var runtime = new TrackApplicationRuntimeHost(
                    new EcosHost(),
                    transportFactory: () => transport,
                    amplifierGroups: TrackAmplifierGroups.Create(new[] { 1 }, null, null));

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await runtime.StartAsync(TrackControlMode.Real, timeout.Token);
            }
            finally
            {
                SiebwaldeApp.Core.IoC.Logger.RemoveLogger(capture);
                try { SiebwaldeApp.Core.Properties.CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                try { System.IO.File.Delete(tempHexPath); } catch { /* best-effort */ }
            }

            var lines = capture.Lines;
            Assert.NotEmpty(lines);
            Assert.Contains(lines, l => l.Contains("ENTRY domain=[1]"));
            Assert.Contains(lines, l => l.Contains("SKIPPED incompatible"));
            Assert.DoesNotContain(lines, l => l.Contains("INVOKE DownloadAsync"));
        }

        /// <summary>Writes a minimal Intel HEX parsed to a file checksum of 0x0E39 by
        /// <see cref="TrackAmplifierBootloaderHelpers.Execute"/> (flashing is skipped).</summary>
        private static void WriteTestFirmwareHex(string path)
        {
            const int dataRows = (0x8000 - 0x800) / 16; // 1920
            var sb = new System.Text.StringBuilder();
            for (int row = 0; row < dataRows; row++)
            {
                string address = (row * 16).ToString("X4");
                string data = row == dataRows - 1 ? "390E" + new string('0', 28) : new string('0', 32);
                sb.Append(':').Append("10").Append(address).Append("00").Append(data).Append("00").AppendLine();
            }
            sb.Append(':').Append("0C").Append("0000").Append("00").Append(new string('F', 24)).Append("00").AppendLine();
            System.IO.File.WriteAllText(path, sb.ToString());
        }
    }
}
