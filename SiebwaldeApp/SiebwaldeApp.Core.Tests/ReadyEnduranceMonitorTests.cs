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
    /// Offline tests for the single-session READY-endurance hold monitor
    /// (<see cref="ReadyEnduranceMonitor"/>), the authorized FALLBACK to the blocked repeated-init
    /// test — explicitly NOT a repeated-init test. The monitor is driven against the real
    /// <see cref="TrackApplicationRuntimeHost"/> over the deterministic simulator transport (no
    /// track controller, no network, no hardware). Covers: a healthy hold emitting the expected
    /// state each poll, fail-closed on a latched global stop / amp fault / HR8 leaving READY, the
    /// 8 h duration bound, the operator-STOP cancellation, and the read-only monitor never issuing
    /// a duplicate GROUP_READY or any write during the hold.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class ReadyEnduranceMonitorTests
    {
        private static readonly byte[] DetectedSlaves = { 1, 3, 4, 6 };
        private static readonly byte[] DomainBytes = { 1, 3, 4, 6 };

        private static TrackAmplifierGroups Domain
            => TrackAmplifierGroups.Create(new[] { 1, 3, 4, 6 }, null, null);

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

        private static TrackApplicationRuntimeHost CreateRuntime(DeterministicTrackTransport transport)
            => new(new EcosHost(), transportFactory: () => transport, amplifierGroups: Domain);

        private static DeterministicTrackTransport CreateTransport()
            => new(new TrackSimulatorConfig(
                detectedSlaves: DetectedSlaves,
                periodicSlaveInfoInterval: TimeSpan.FromMilliseconds(50)));

        private sealed class RecordingSink : IReadyEnduranceSink
        {
            public List<ReadyEnduranceSample> Samples { get; } = new();
            public List<ReadyEnduranceSummary> Summaries { get; } = new();
            public void WriteSample(ReadyEnduranceSample sample) => Samples.Add(sample);
            public void WriteSummary(ReadyEnduranceSummary summary) => Summaries.Add(summary);
        }

        private sealed class FakeClock
        {
            public DateTimeOffset Now { get; private set; } =
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            public DateTimeOffset UtcNow() => Now;
            public void Advance(TimeSpan delta) => Now = Now.Add(delta);
        }

        private static int HeartbeatCount(DeterministicTrackTransport transport)
            => transport.SentFrames.Count(f => f.Length >= 2 && f[1] == TrackCommand.HOST_HEARTBEAT);

        private static bool IsGroupReadyWrite(byte[] frame)
            => frame.Length >= 8 &&
               frame[1] == TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH &&
               frame[3] == 0xAA && // write direction
               frame[4] == 1 &&    // register count
               frame[5] == TrackAmplifierRegisters.TrackPower &&
               frame[6] == (TrackAmplifierRegisters.GroupReadyBit & 0xFF) &&
               frame[7] == (TrackAmplifierRegisters.GroupReadyBit >> 8);

        private static int GroupReadyWriteCount(DeterministicTrackTransport transport)
            => transport.SentFrames.Count(IsGroupReadyWrite);

        // ---------------------------------------------------------------------------------
        // Firmware-path fixture (mirrors the PV05 heartbeat test: FlashFwTrackamplifiersStep
        // validates the configured TrackAmplifierFwPath checksum).
        // ---------------------------------------------------------------------------------

        private sealed class FirmwarePathScope : IDisposable
        {
            private readonly string _original;
            private readonly string _temp;
            public FirmwarePathScope()
            {
                _original = Properties.CoreSettings.Default.TrackAmplifierFwPath;
                _temp = Path.Combine(Path.GetTempPath(), $"endurance-{Guid.NewGuid():N}.hex");
                WriteTestFirmwareHex(_temp);
                Properties.CoreSettings.Default.TrackAmplifierFwPath = _temp;
            }
            public void Dispose()
            {
                try { Properties.CoreSettings.Default.TrackAmplifierFwPath = _original; } catch { /* best-effort */ }
                try { File.Delete(_temp); } catch { /* best-effort */ }
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

        // ---------------------------------------------------------------------------------
        // Tests
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task HealthyHold_EmitsExpectedFields_EveryPoll_AndIsReadOnly()
        {
            using var fw = new FirmwarePathScope();
            var transport = CreateTransport();
            await using var runtime = CreateRuntime(transport);

            using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await runtime.StartAsync(TrackControlMode.Real, startTimeout.Token);
            Assert.Equal(TrackRuntimeState.Running, runtime.State);
            Assert.Equal(InitializationStatus.StoppedAtGlobalReady, runtime.RunStatus);

            int groupReadyBefore = GroupReadyWriteCount(transport);
            int heartbeatBefore = HeartbeatCount(transport);

            var sink = new RecordingSink();
            var monitor = new ReadyEnduranceMonitor(
                runtime, DomainBytes, sink,
                pollInterval: TimeSpan.FromMilliseconds(25),
                maxDuration: TimeSpan.FromHours(8),
                heartbeatActive: () => runtime.HeartbeatActive);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(700));
            var summary = await monitor.RunAsync(cts.Token);

            Assert.True(summary.Healthy);
            Assert.Equal(ReadyEnduranceDisposition.OperatorStop, summary.Disposition);
            Assert.NotEmpty(sink.Samples);

            foreach (var s in sink.Samples)
            {
                Assert.True(s.Healthy, s.Failure);
                Assert.Equal("Running", s.State);
                Assert.Equal("StoppedAtGlobalReady", s.RunStatus);
                Assert.Equal("Disarmed", s.GlobalStopState);
                Assert.True(s.HeartbeatActive);
                Assert.False(s.TrackPowerOn);
                Assert.Equal(DomainBytes.Length, s.Amps.Count);
                Assert.All(s.Amps, a =>
                {
                    Assert.True(a.Detected);
                    Assert.True(a.Ready, $"amp{a.Slave} HR8=0x{a.Hr8:X4} low-nibble={a.Hr8LowNibble}");
                    Assert.False(a.Fault, $"amp{a.Slave} HR9=0x{a.Hr9:X4}");
                    Assert.True(a.LeaseFresh);
                    Assert.Null(a.Phase1Failure);
                    Assert.True(a.ConfigurationVerified);
                });
            }

            // Heartbeat (110) keeps being sent during the hold (independent task stays alive).
            int heartbeatAfter = HeartbeatCount(transport);
            Assert.True(heartbeatAfter > heartbeatBefore,
                $"heartbeat count must keep rising during the hold (before={heartbeatBefore}, after={heartbeatAfter}).");

            // The monitor is read-only: it never issues a duplicate GROUP_READY or any other write.
            int groupReadyAfter = GroupReadyWriteCount(transport);
            Assert.Equal(groupReadyBefore, groupReadyAfter);
        }

        [Fact]
        public async Task LatchedGlobalStop_FailsHoldClosed()
        {
            using var fw = new FirmwarePathScope();
            var transport = CreateTransport();
            await using var runtime = CreateRuntime(transport);

            using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await runtime.StartAsync(TrackControlMode.Real, startTimeout.Token);
            Assert.Equal(TrackRuntimeState.Running, runtime.State);

            var sink = new RecordingSink();
            var monitor = new ReadyEnduranceMonitor(
                runtime, DomainBytes, sink,
                pollInterval: TimeSpan.FromMilliseconds(25),
                heartbeatActive: () => runtime.HeartbeatActive);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var runTask = monitor.RunAsync(cts.Token);

            await Task.Delay(100); // allow at least one healthy poll
            runtime.EmergencyStop(); // latch the global stop (operator stop)

            var summary = await runTask;

            Assert.False(summary.Healthy);
            Assert.Equal(ReadyEnduranceDisposition.FirstFailure, summary.Disposition);
            Assert.StartsWith("global stop latched", summary.FirstFailureReason);
        }

        [Fact]
        public async Task AmpFault_FailsHoldClosed()
        {
            using var fw = new FirmwarePathScope();
            var transport = CreateTransport();
            await using var runtime = CreateRuntime(transport);

            using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await runtime.StartAsync(TrackControlMode.Real, startTimeout.Token);
            Assert.Equal(TrackRuntimeState.Running, runtime.State);

            var sink = new RecordingSink();
            var monitor = new ReadyEnduranceMonitor(
                runtime, DomainBytes, sink,
                pollInterval: TimeSpan.FromMilliseconds(25),
                heartbeatActive: () => runtime.HeartbeatActive);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var runTask = monitor.RunAsync(cts.Token);

            await Task.Delay(100);
            transport.SetSlaveFault(1, 0x0020); // comms fault on amp 1

            var summary = await runTask;

            Assert.False(summary.Healthy);
            Assert.Equal(ReadyEnduranceDisposition.FirstFailure, summary.Disposition);
            Assert.Contains("amp1", summary.FirstFailureReason);
            Assert.Contains("HR9", summary.FirstFailureReason);
        }

        [Fact]
        public async Task Hr8LeavingReady_FailsHoldClosed()
        {
            using var fw = new FirmwarePathScope();
            var transport = CreateTransport();
            await using var runtime = CreateRuntime(transport);

            using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await runtime.StartAsync(TrackControlMode.Real, startTimeout.Token);
            Assert.Equal(TrackRuntimeState.Running, runtime.State);

            var sink = new RecordingSink();
            var monitor = new ReadyEnduranceMonitor(
                runtime, DomainBytes, sink,
                pollInterval: TimeSpan.FromMilliseconds(25),
                heartbeatActive: () => runtime.HeartbeatActive);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var runTask = monitor.RunAsync(cts.Token);

            await Task.Delay(100);
            transport.SetSlaveState(4, 8); // amp 4 leaves READY -> STANDBY (low nibble 8)

            var summary = await runTask;

            Assert.False(summary.Healthy);
            Assert.Equal(ReadyEnduranceDisposition.FirstFailure, summary.Disposition);
            Assert.Contains("amp4", summary.FirstFailureReason);
            Assert.Contains("HR8", summary.FirstFailureReason);
        }

        [Fact]
        public async Task DurationBound_TerminatesCleanly()
        {
            using var fw = new FirmwarePathScope();
            var transport = CreateTransport();
            await using var runtime = CreateRuntime(transport);

            using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await runtime.StartAsync(TrackControlMode.Real, startTimeout.Token);
            Assert.Equal(TrackRuntimeState.Running, runtime.State);

            var clock = new FakeClock();
            var sink = new RecordingSink();
            var monitor = new ReadyEnduranceMonitor(
                runtime, DomainBytes, sink,
                clock: clock.UtcNow,
                pollInterval: TimeSpan.FromMilliseconds(10),
                maxDuration: TimeSpan.FromHours(8),
                heartbeatActive: () => runtime.HeartbeatActive);

            var runTask = monitor.RunAsync(CancellationToken.None);
            await Task.Delay(60); // allow at least one poll
            clock.Advance(TimeSpan.FromHours(8) + TimeSpan.FromSeconds(1));

            var summary = await runTask;

            Assert.Equal(ReadyEnduranceDisposition.DurationBoundReached, summary.Disposition);
            Assert.True(summary.Healthy);
            Assert.True(summary.PollCount >= 1);
        }

        [Fact]
        public async Task OperatorStop_TerminatesCleanly()
        {
            using var fw = new FirmwarePathScope();
            var transport = CreateTransport();
            await using var runtime = CreateRuntime(transport);

            using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await runtime.StartAsync(TrackControlMode.Real, startTimeout.Token);
            Assert.Equal(TrackRuntimeState.Running, runtime.State);

            var sink = new RecordingSink();
            var monitor = new ReadyEnduranceMonitor(
                runtime, DomainBytes, sink,
                pollInterval: TimeSpan.FromMilliseconds(25),
                maxDuration: TimeSpan.FromHours(8),
                heartbeatActive: () => runtime.HeartbeatActive);

            using var cts = new CancellationTokenSource();
            var runTask = monitor.RunAsync(cts.Token);

            await Task.Delay(100); // allow at least one healthy poll
            cts.Cancel();          // operator STOP signal

            var summary = await runTask;

            Assert.Equal(ReadyEnduranceDisposition.OperatorStop, summary.Disposition);
            Assert.True(summary.Healthy);
        }
    }
}
