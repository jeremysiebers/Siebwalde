using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// P0 stop-reliability tests: a blocked initialization (a controller that never acknowledges
    /// command 106) must not prevent the operator from stopping the application. StartAsync holds the
    /// lifecycle gate while the init runs, so StopAsync must cancel the pending init BEFORE acquiring
    /// the gate — otherwise Stop blocks forever. These tests drive the real
    /// <see cref="TrackApplicationRuntimeHost"/> over a fault-injected transport.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class TrackApplicationRuntimeHostStopReliabilityTests
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

        /// <summary>Wraps a transport and drops command 106 (START_DATA_UPLOAD), so the real
        /// <see cref="DataUploadStep"/> never receives its DONE acknowledgement.</summary>
        private sealed class DataUploadDropTransport : ITrackTransport
        {
            private readonly ITrackTransport _inner;
            public DataUploadDropTransport(ITrackTransport inner) => _inner = inner;
            public bool SawCommand106 { get; private set; }

            public Task OpenAsync(CancellationToken cancellationToken = default) => _inner.OpenAsync(cancellationToken);
            public Task CloseAsync(CancellationToken cancellationToken = default) => _inner.CloseAsync(cancellationToken);

            public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
            {
                // [0]=HEADER, [1]=command. Drop the data-upload command so no DONE is ever produced.
                if (count >= 2 && buffer[offset + 1] == TrackCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD)
                {
                    SawCommand106 = true;
                    return Task.CompletedTask;
                }

                return _inner.SendAsync(buffer, offset, count, cancellationToken);
            }

            public IAsyncEnumerable<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
                => _inner.ReceiveAsync(cancellationToken);

            public ValueTask DisposeAsync() => _inner.DisposeAsync();
        }

        [Fact]
        public async Task StopDuringBlockedDataUpload_CompletesWithoutDeadlock()
        {
            // The init reaches the DataUpload step (106 dropped -> no DONE) and blocks there. Stop must
            // cancel the pending init and complete, instead of deadlocking on the lifecycle gate.
            var transport = new DataUploadDropTransport(new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1, 2, 3 },
                periodicSlaveInfoInterval: TimeSpan.FromMilliseconds(50))));

            var originalFwPath = SiebwaldeApp.Core.Properties.CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"p0-stop-{Guid.NewGuid():N}.hex");
            try
            {
                WriteTestFirmwareHex(tempHexPath);
                SiebwaldeApp.Core.Properties.CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

                await using var runtime = new TrackApplicationRuntimeHost(
                    new EcosHost(),
                    transportFactory: () => transport,
                    amplifierGroups: TrackAmplifierGroups.Create(new[] { 1 }, null, null));

                using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var startTask = runtime.StartAsync(TrackControlMode.Real, startTimeout.Token);

                // Let the init reach and block on the data-upload step (dropped 106).
                await WaitUntilAsync(() => transport.SawCommand106, TimeSpan.FromSeconds(5));

                // Stop must complete without deadlocking on the gate held by the blocked init.
                var stopWatch = Stopwatch.StartNew();
                using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await runtime.StopAsync(stopTimeout.Token);
                stopWatch.Stop();

                // The blocked start must have been cancelled/failed-closed and the gate released.
                await startTask.ConfigureAwait(false);

                // No deadlock: StopAsync completed and the runtime reached a terminal state. The
                // duration is dominated by the safety neutral-observe window (5 s) in the failed-init
                // cleanup, well within the 15 s StopBudget.
                Assert.NotEqual(TrackRuntimeState.Running, runtime.State);
                Assert.True(stopWatch.Elapsed < TimeSpan.FromSeconds(15),
                    $"StopAsync took {stopWatch.Elapsed}; expected bounded shutdown (<= StopBudget).");
            }
            finally
            {
                try { SiebwaldeApp.Core.Properties.CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                try { System.IO.File.Delete(tempHexPath); } catch { /* best-effort */ }
            }
        }

        private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (condition()) return;
                await Task.Delay(10);
            }

            throw new TimeoutException($"Condition not met within {timeout}.");
        }

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
