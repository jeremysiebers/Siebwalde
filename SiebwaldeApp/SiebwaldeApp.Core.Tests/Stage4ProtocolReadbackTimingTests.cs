using System;
using System.Collections.Generic;
using System.IO;
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
    /// Offline tests for the Stage-4 premature compatibility rejection correction (PV07b). An
    /// amplifier whose HR7 identity is compatible but whose HR22 protocol byte is still 0x00 (the
    /// lagging SIP10 packet of the cyclic SLAVEINFO upload) is NOT YET AVAILABLE — not
    /// "incompatible protocol version" — and is bounded-re-read at the Stage-4 config decision point
    /// until HR22 populates. Protocol 0x00 is never accepted as compatible. Drives the production
    /// <see cref="TrackApplicationRuntimeHost"/> over the deterministic transport and asserts on the
    /// captured P1CFG log. Software/protocol only: no hardware, no live application.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class Stage4ProtocolReadbackTimingTests
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

        /// <summary>
        /// Composes a full Real-mode <see cref="TrackApplicationRuntimeHost"/> with a single-amplifier
        /// safety domain (slave 1) and three detected slaves (satisfying DetectSlavesStep's minimum),
        /// starts it, and returns every captured P1CFG log line. The real-mode pipeline reads the
        /// firmware hex even when flashing is skipped, so the test points the setting at a minimal
        /// self-contained hex (0x0E39 checksum) and restores it afterwards.
        /// </summary>
        private static async Task<IReadOnlyList<string>> RunConfigAsync(TrackSimulatorConfig config)
        {
            var transport = new DeterministicTrackTransport(config);
            var capture = new CaptureLogger();
            SiebwaldeApp.Core.IoC.Logger.AddLogger(capture);

            var originalFwPath = SiebwaldeApp.Core.Properties.CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"p1cfg-{Guid.NewGuid():N}.hex");
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
                try { File.Delete(tempHexPath); } catch { /* best-effort */ }
            }

            return capture.Lines;
        }

        // ---------------------------------------------------------------------
        // A. SIP10 delayed -> eventually PASS (no premature "incompatible")
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ProtocolVersion_Sip10Delayed_ResolvesAndConfigSucceeds()
        {
            // HR21/HR22 are reported 0x0000 for the first 10 SLAVEINFO frames of slave 1 (the
            // lagging SIP10), then 0x0002. The decision point must bounded-re-read until HR22
            // populates, then proceed to config (no "incompatible").
            var lines = await RunConfigAsync(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1, 2, 3 },
                protocolVersionDelayFrames: new Dictionary<byte, int> { { 1, 10 } }));

            Assert.Contains(lines, l => l.Contains("protocol-readback recheck"));
            Assert.Contains(lines, l => l.Contains("INVOKE DownloadAsync"));
            Assert.DoesNotContain(lines, l => l.Contains("SKIPPED incompatible"));
            Assert.Contains(lines, l => l.Contains("DownloadAsync result=True"));
        }

        // ---------------------------------------------------------------------
        // B. Genuine incompatible (protocol 0x03) -> immediate FAIL
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ProtocolVersion_GenuineIncompatible_FailsIncompatibleProtocolVersion()
        {
            // HR7 compatible + HR22=0x0003 (protocol byte 0x03): non-zero and not 0x01/0x02, so it
            // is genuinely incompatible (never re-read, never configured).
            var lines = await RunConfigAsync(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1, 2, 3 },
                protocolVersionOverrides: new Dictionary<byte, ushort> { { 1, 0x0003 } }));

            Assert.Contains(lines, l => l.Contains("SKIPPED incompatible: incompatible protocol version"));
            Assert.DoesNotContain(lines, l => l.Contains("protocol-readback recheck"));
            Assert.DoesNotContain(lines, l => l.Contains("INVOKE DownloadAsync"));
        }

        // ---------------------------------------------------------------------
        // C. SIP10 never received -> bounded FAIL with a distinct reason
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ProtocolVersion_NeverReceived_FailsBoundedProtocolVersionNotReceived()
        {
            // HR22 stays 0x0000 for the whole bounded window (the SIP10 never populates): a bounded
            // FAIL with the distinct "protocol version not received" reason, never an infinite loop
            // and never an accepted config.
            var lines = await RunConfigAsync(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1, 2, 3 },
                protocolVersionDelayFrames: new Dictionary<byte, int> { { 1, 1_000_000 } }));

            Assert.Contains(lines, l => l.Contains("protocol-readback recheck"));
            Assert.Contains(lines, l => l.Contains("SKIPPED incompatible: protocol version not received"));
            Assert.DoesNotContain(lines, l => l.Contains("INVOKE DownloadAsync"));
            Assert.DoesNotContain(lines, l => l.Contains("DownloadAsync result=True"));
        }

        // ---------------------------------------------------------------------
        // D. Partial/stale cache with HR22=0 -> no false PASS
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ProtocolVersion_StalePartialFrame_NeverYieldsPass()
        {
            // A partial frame delivers HR22=0 and then the slave's echo is dropped, so the cache goes
            // stale with HR22=0: the amplifier must never be configured (no false PASS), and protocol
            // 0x00 must never be accepted as compatible.
            var lines = await RunConfigAsync(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1, 2, 3 },
                protocolVersionOverrides: new Dictionary<byte, ushort> { { 1, 0x00 } },
                droppedEchoSlaves: new byte[] { 1 }));

            Assert.Contains(lines, l => l.Contains("SKIPPED incompatible"));
            Assert.DoesNotContain(lines, l => l.Contains("INVOKE DownloadAsync"));
            Assert.DoesNotContain(lines, l => l.Contains("DownloadAsync result=True"));
        }

        // ---------------------------------------------------------------------
        // E. Normal fully populated -> PASS unchanged (regression)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ProtocolVersion_FullyPopulated_ConfigSucceedsUnchanged()
        {
            // HR22=0x0002 from the very first frame: no re-read is needed and the config proceeds
            // exactly as before the correction.
            var lines = await RunConfigAsync(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1, 2, 3 }));

            Assert.DoesNotContain(lines, l => l.Contains("protocol-readback recheck"));
            Assert.Contains(lines, l => l.Contains("INVOKE DownloadAsync"));
            Assert.DoesNotContain(lines, l => l.Contains("SKIPPED incompatible"));
            Assert.Contains(lines, l => l.Contains("DownloadAsync result=True"));
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
            File.WriteAllText(path, sb.ToString());
        }
    }
}
