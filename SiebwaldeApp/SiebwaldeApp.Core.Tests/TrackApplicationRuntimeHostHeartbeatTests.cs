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
    /// Integration verification that the host-authority heartbeat (110) is sent DURING initialization
    /// (after the validated controller session), not merely after Stage 5 / "Running". Regression for
    /// the PV05 failure: without this, a controller re-armed into ARMED re-latches its 2000 ms watchdog
    /// because no heartbeat renews authority while the init runs 107/100/106.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class TrackApplicationRuntimeHostHeartbeatTests
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

        [Fact]
        public async Task Heartbeat_IsSent_DuringInit_BeforeDataUpload()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1, 2, 3 },
                periodicSlaveInfoInterval: TimeSpan.FromMilliseconds(50)));

            var originalFwPath = SiebwaldeApp.Core.Properties.CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"hb-init-{Guid.NewGuid():N}.hex");
            try
            {
                WriteTestFirmwareHex(tempHexPath);
                SiebwaldeApp.Core.Properties.CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

                await using var runtime = new TrackApplicationRuntimeHost(
                    new EcosHost(),
                    transportFactory: () => transport,
                    amplifierGroups: TrackAmplifierGroups.Create(new[] { 1 }, null, null));

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await runtime.StartAsync(TrackControlMode.Real, timeout.Token);

                // The init must complete (non-energized -> StoppedAtGlobalReady / Running).
                Assert.NotEqual(TrackRuntimeState.Failed, runtime.State);

                var frames = transport.SentFrames.ToArray();
                int firstHeartbeat = Array.FindIndex(frames, f => f.Length >= 2 && f[1] == 110);
                int firstDataUpload = Array.FindIndex(frames, f => f.Length >= 2 && f[1] == 106);

                Assert.True(firstHeartbeat >= 0, "HOST_HEARTBEAT (110) frames must be transmitted");
                Assert.True(firstDataUpload >= 0, "data-upload (106) frame must be transmitted");
                Assert.True(firstHeartbeat < firstDataUpload,
                    $"first heartbeat (index {firstHeartbeat}) must precede data-upload (index {firstDataUpload}) " +
                    "— i.e. the heartbeat runs during the init, not only after Stage 5.");
            }
            finally
            {
                try { SiebwaldeApp.Core.Properties.CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                try { File.Delete(tempHexPath); } catch { /* best-effort */ }
            }
        }

        private static void WriteTestFirmwareHex(string path)
        {
            const int dataRows = (0x8000 - 0x800) / 16;
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
