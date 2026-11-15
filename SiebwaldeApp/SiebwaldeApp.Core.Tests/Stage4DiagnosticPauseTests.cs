using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Offline integration tests for Work Package B (PV07-C): the C# controlled Stage-4 diagnostic
    /// pause (Variant A) and single-amplifier read-only diagnostics. These drive the real
    /// <see cref="TrackApplicationRuntimeHost"/> over the deterministic simulator transport — no
    /// track controller, no network, no hardware. They cover: normal startup unchanged, pause after
    /// Stage 4, no Stage 5 while paused, heartbeat during pause, global-stop / StopAsync / token
    /// cancellation during pause, resume, read-only amp 6 vs amp 4, cache-freshness classification,
    /// the one-shot single GROUP_READY (accepted / rejected / refused), no Stage 6 via diagnostic
    /// mode, and regression of the PV05/PV06 heartbeat correction.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class Stage4DiagnosticPauseTests
    {
        private static readonly byte[] DetectedSlaves = { 1, 3, 4, 6 };

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

        private static TrackApplicationRuntimeHost CreateRuntime(
            DeterministicTrackTransport transport,
            TrackInitDiagnosticMode diagnosticMode)
            => new(new EcosHost(), transportFactory: () => transport, amplifierGroups: Domain)
            {
                DiagnosticMode = diagnosticMode
            };

        // Periodic SLAVEINFO is kept enabled (matching the real PIC32's cyclic upload) so the
        // amplifier cache stays fresh during the init and the Stage-4 config readback has a
        // continuous data source. The torn-read race in LastDataReceivedUtc is fixed at the source
        // (TrackAmplifierItem uses an atomic Volatile.Read/Write backing field), so the periodic
        // timer no longer causes spurious "compatibility changed" failures.
        private static DeterministicTrackTransport CreateTransport(
            IReadOnlyCollection<byte>? groupReadyRejectSlaves = null)
            => new(new TrackSimulatorConfig(
                detectedSlaves: DetectedSlaves,
                periodicSlaveInfoInterval: TimeSpan.FromMilliseconds(50),
                groupReadyRejectSlaves: groupReadyRejectSlaves));

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
                _temp = Path.Combine(Path.GetTempPath(), $"p4diag-{Guid.NewGuid():N}.hex");
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

        private static bool IsGroupReadyWrite(byte[] frame, byte slave)
            => frame.Length >= 8 &&
               frame[1] == TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH &&
               frame[2] == slave &&
               frame[3] == 0xAA && // write direction
               frame[4] == 1 &&    // register count
               frame[5] == TrackAmplifierRegisters.TrackPower &&
               frame[6] == (TrackAmplifierRegisters.GroupReadyBit & 0xFF) &&
               frame[7] == (TrackAmplifierRegisters.GroupReadyBit >> 8);

        private static bool AnyGroupReadyWrite(DeterministicTrackTransport transport)
            => transport.SentFrames.Any(f => f.Length >= 8 && f[1] == TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH &&
                f[3] == 0xAA && f[4] == 1 && f[5] == TrackAmplifierRegisters.TrackPower &&
                f[6] == (TrackAmplifierRegisters.GroupReadyBit & 0xFF) && f[7] == (TrackAmplifierRegisters.GroupReadyBit >> 8));

        private static int HeartbeatCount(DeterministicTrackTransport transport)
            => transport.SentFrames.Count(f => f.Length >= 2 && f[1] == TrackCommand.HOST_HEARTBEAT);

        private static bool StagePassed(TrackApplicationRuntimeHost runtime, TrackInitStage stage)
            => runtime.InitStages.Any(s => s.Stage == stage && s.State == TrackInitStageState.Passed);

        /// <summary>
        /// Runs <paramref name="body"/> with a runtime started in diagnostic-pause mode, waited to the
        /// Stage-4 pause, and always torn down (StopAsync + await the start task) even when the body
        /// throws, so a failed assertion never leaks a paused runtime into the next test.
        /// </summary>
        private static async Task WithPausedRuntimeAsync(
            DeterministicTrackTransport transport,
            Func<TrackApplicationRuntimeHost, Task> body)
        {
            using var fw = new FirmwarePathScope();
            await using var runtime = CreateRuntime(transport, TrackInitDiagnosticMode.PauseAfterStage4);
            using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var startTask = runtime.StartAsync(TrackControlMode.Real, startTimeout.Token);
            try
            {
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed < TimeSpan.FromSeconds(30))
                {
                    if (runtime.IsPausedAtStage4)
                    {
                        break;
                    }

                    if (runtime.State == TrackRuntimeState.Failed)
                    {
                        var failedStages = string.Join(", ", runtime.InitStages.Select(s => $"{s.Stage}={s.State}"));
                        throw new Xunit.Sdk.XunitException(
                            $"Runtime failed before reaching the Stage-4 pause. Stages: {failedStages}");
                    }

                    await Task.Delay(10);
                }

                if (!runtime.IsPausedAtStage4)
                {
                    var stuckStages = string.Join(", ", runtime.InitStages.Select(s => $"{s.Stage}={s.State}"));
                    throw new Xunit.Sdk.XunitException(
                        $"Runtime did not reach the Stage-4 pause within 30 s (state={runtime.State}, runStatus={runtime.RunStatus}, globalStop={runtime.GlobalStopState.State}). Stages: {stuckStages}");
                }

                await body(runtime);
            }
            finally
            {
                try { await runtime.StopAsync(); } catch { /* best-effort */ }
                try { await startTask; } catch { /* best-effort */ }
            }
        }

        // ---------------------------------------------------------------------------------
        // Freshness classifier unit tests (no runtime).
        // ---------------------------------------------------------------------------------

        [Fact]
        public void Classify_Unknown_WhenNotDetected()
        {
            var item = new TrackAmplifierItem { SlaveNumber = 6, SlaveDetected = 0 };
            Assert.Equal(AmplifierRegisterFreshness.Unknown,
                AmplifierRegisterFreshnessClassifier.Classify(item, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void Classify_Unknown_WhenNeverReceived()
        {
            var item = new TrackAmplifierItem { SlaveNumber = 6, SlaveDetected = 1, LastDataReceivedUtc = null };
            Assert.Equal(AmplifierRegisterFreshness.Unknown,
                AmplifierRegisterFreshnessClassifier.Classify(item, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void Classify_Stale_WhenNotProtocolObserved()
        {
            var item = new TrackAmplifierItem
            {
                SlaveNumber = 6,
                SlaveDetected = 1,
                LastDataReceivedUtc = DateTimeOffset.UtcNow,
                ProtocolReadbackObserved = false
            };
            Assert.Equal(AmplifierRegisterFreshness.Stale,
                AmplifierRegisterFreshnessClassifier.Classify(item, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void Classify_Fresh_WhenCurrentProtocolReadback()
        {
            var item = new TrackAmplifierItem
            {
                SlaveNumber = 6,
                SlaveDetected = 1,
                LastDataReceivedUtc = DateTimeOffset.UtcNow,
                ProtocolReadbackObserved = true
            };
            Assert.Equal(AmplifierRegisterFreshness.Fresh,
                AmplifierRegisterFreshnessClassifier.Classify(item, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void Classify_Cached_WhenReadbackExceededWindow()
        {
            var item = new TrackAmplifierItem
            {
                SlaveNumber = 6,
                SlaveDetected = 1,
                LastDataReceivedUtc = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(30),
                ProtocolReadbackObserved = true
            };
            Assert.Equal(AmplifierRegisterFreshness.Cached,
                AmplifierRegisterFreshnessClassifier.Classify(item, DateTimeOffset.UtcNow));
        }

        // ---------------------------------------------------------------------------------
        // Pause + diagnostics integration tests.
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task NormalStartup_WithoutDiagnosticMode_IsUnchanged()
        {
            using var fw = new FirmwarePathScope();
            var transport = CreateTransport();
            await using var runtime = CreateRuntime(transport, TrackInitDiagnosticMode.None);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await runtime.StartAsync(TrackControlMode.Real, timeout.Token);

            Assert.Equal(TrackRuntimeState.Running, runtime.State);
            Assert.False(runtime.IsPausedAtStage4);
        }

        [Fact]
        public Task DiagnosticMode_PausesAfterStage4_BeforeGroupReady()
        {
            var transport = CreateTransport();
            return WithPausedRuntimeAsync(transport, async runtime =>
            {
                Assert.True(StagePassed(runtime, TrackInitStage.ConfigureValidate));
                Assert.False(StagePassed(runtime, TrackInitStage.GlobalReady));
                Assert.False(AnyGroupReadyWrite(transport));
            });
        }

        [Fact]
        public Task DiagnosticMode_DoesNotAutoAdvanceToStage5_WhilePaused()
        {
            var transport = CreateTransport();
            return WithPausedRuntimeAsync(transport, async runtime =>
            {
                await Task.Delay(700);
                Assert.True(runtime.IsPausedAtStage4);
                Assert.False(StagePassed(runtime, TrackInitStage.GlobalReady));
                Assert.False(AnyGroupReadyWrite(transport));
            });
        }

        [Fact]
        public Task Heartbeat_Continues_DuringPause()
        {
            var transport = CreateTransport();
            return WithPausedRuntimeAsync(transport, async runtime =>
            {
                int before = HeartbeatCount(transport);
                await Task.Delay(1000);
                int after = HeartbeatCount(transport);
                Assert.True(after > before,
                    $"heartbeat count should keep rising during the pause (before={before}, after={after}).");
            });
        }

        [Fact]
        public Task ResumeStage4Pause_ProceedsToStage5AndRunning()
        {
            var transport = CreateTransport();
            return WithPausedRuntimeAsync(transport, async runtime =>
            {
                runtime.ResumeStage4Pause();
                // The pause is released; wait for the runtime to reach Running.
                await WaitUntilAsync(() => runtime.State == TrackRuntimeState.Running, TimeSpan.FromSeconds(30));
                Assert.True(StagePassed(runtime, TrackInitStage.GlobalReady));
                Assert.True(AnyGroupReadyWrite(transport));
            });
        }

        [Fact]
        public Task StopAsync_DuringPause_IsFailClosed()
            => WithPausedRuntimeAsync(CreateTransport(), async runtime =>
            {
                using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await runtime.StopAsync(stopTimeout.Token);
                // A mid-pause operator stop is fail-closed: neutral could not be observed, so the
                // runtime must not claim a clean Stopped.
                Assert.Equal(TrackRuntimeState.Failed, runtime.State);
            });

        [Fact]
        public Task GlobalStop_DuringPause_AbortsFailClosed()
            => WithPausedRuntimeAsync(CreateTransport(), async runtime =>
            {
                runtime.EmergencyStop();
                await WaitUntilAsync(() => runtime.State == TrackRuntimeState.Failed, TimeSpan.FromSeconds(20));
            });

        [Fact]
        public async Task StartCancellationToken_DuringPause_AbortsFailClosed()
        {
            using var fw = new FirmwarePathScope();
            var transport = CreateTransport();
            await using var runtime = CreateRuntime(transport, TrackInitDiagnosticMode.PauseAfterStage4);

            using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var startTask = runtime.StartAsync(TrackControlMode.Real, startTimeout.Token);
            try
            {
                await WaitUntilAsync(() => runtime.IsPausedAtStage4, TimeSpan.FromSeconds(30));

                // A caller-supplied start timeout cancels the linked CTS, so the pause await throws
                // OperationCanceledException and the start fails closed (the same OCE path StopAsync
                // uses).
                startTimeout.Cancel();
                await WaitUntilAsync(() => runtime.State != TrackRuntimeState.Starting, TimeSpan.FromSeconds(20));
                await startTask;
                Assert.Equal(TrackRuntimeState.Failed, runtime.State);
            }
            finally
            {
                try { await runtime.StopAsync(); } catch { /* best-effort */ }
                try { await startTask; } catch { /* best-effort */ }
            }
        }

        [Fact]
        public Task ReadAmplifierDiagnostics_Fresh_Amp6VsAmp4()
            => WithPausedRuntimeAsync(CreateTransport(), async runtime =>
            {
                var amp6 = await runtime.ReadAmplifierDiagnosticsAsync(6);
                var amp4 = await runtime.ReadAmplifierDiagnosticsAsync(4);

                Assert.Equal(AmplifierRegisterFreshness.Fresh, amp6.Freshness);
                Assert.Equal(AmplifierRegisterFreshness.Fresh, amp4.Freshness);

                Assert.True(amp6.IsStandby, $"amp 6 HR8=0x{amp6.Hr8State:X4}");
                Assert.True(amp4.IsStandby, $"amp 4 HR8=0x{amp4.Hr8State:X4}");
                Assert.False(amp6.IsReady);
                Assert.False(amp4.IsReady);
                Assert.True(TrackAmplifierRegisters.HasCompatibleProtocolVersion(amp6.Hr22Protocol));
                Assert.True(TrackAmplifierRegisters.HasCompatibleProtocolVersion(amp4.Hr22Protocol));
                Assert.True(amp6.ConfigurationVerified);
            });

        [Fact]
        public Task ReadAmplifierDiagnostics_Unknown_ForUndetectedAmp()
            => WithPausedRuntimeAsync(CreateTransport(), async runtime =>
            {
                var reading = await runtime.ReadAmplifierDiagnosticsAsync(9); // not detected
                Assert.Equal(AmplifierRegisterFreshness.Unknown, reading.Freshness);
                Assert.False(reading.IsReady);
                Assert.False(reading.IsStandby);
            });

        [Fact]
        public Task IssueSingleGroupReady_Amp6_Accepted()
        {
            var transport = CreateTransport();
            return WithPausedRuntimeAsync(transport, async runtime =>
            {
                var result = await runtime.IssueSingleGroupReadyAsync(6);

                Assert.Equal(SingleAmplifierGroupReadyDisposition.Accepted, result.Disposition);
                Assert.Equal(AmplifierRegisterFreshness.Fresh, result.Hr8Freshness);
                Assert.True((result.Hr8Value & 0xF) == TrackAmplifierRegisters.ReadyState,
                    $"amp 6 HR8=0x{result.Hr8Value:X4} should be READY after the single GROUP_READY.");

                int count = transport.SentFrames.Count(f => IsGroupReadyWrite(f, 6));
                Assert.Equal(1, count);
            });
        }

        [Fact]
        public Task IssueSingleGroupReady_Amp6_Rejected_WhenLocalConditionsNotMet()
            => WithPausedRuntimeAsync(CreateTransport(groupReadyRejectSlaves: new byte[] { 6 }), async runtime =>
            {
                var result = await runtime.IssueSingleGroupReadyAsync(6);

                Assert.Equal(SingleAmplifierGroupReadyDisposition.Rejected, result.Disposition);
                Assert.False((result.Hr8Value & 0xF) == TrackAmplifierRegisters.ReadyState,
                    "amp 6 must NOT be READY when it rejects GROUP_READY.");
            });

        [Fact]
        public Task IssueSingleGroupReady_OneShot_SecondCallRefused()
            => WithPausedRuntimeAsync(CreateTransport(), async runtime =>
            {
                var first = await runtime.IssueSingleGroupReadyAsync(6);
                Assert.Equal(SingleAmplifierGroupReadyDisposition.Accepted, first.Disposition);

                var second = await runtime.IssueSingleGroupReadyAsync(4);
                Assert.Equal(SingleAmplifierGroupReadyDisposition.NotAuthorized, second.Disposition);
            });

        [Fact]
        public async Task IssueSingleGroupReady_Refused_WhenNotPaused()
        {
            using var fw = new FirmwarePathScope();
            var transport = CreateTransport();
            await using var runtime = CreateRuntime(transport, TrackInitDiagnosticMode.None);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await runtime.StartAsync(TrackControlMode.Real, timeout.Token);

            var result = await runtime.IssueSingleGroupReadyAsync(6);
            Assert.Equal(SingleAmplifierGroupReadyDisposition.NotPaused, result.Disposition);
        }

        [Fact]
        public Task IssueSingleGroupReady_InvalidTarget_Refused()
            => WithPausedRuntimeAsync(CreateTransport(), async runtime =>
            {
                // 51 is a backplane/configuration slave, not a track amplifier and not in the domain.
                var result = await runtime.IssueSingleGroupReadyAsync(51);
                Assert.Equal(SingleAmplifierGroupReadyDisposition.InvalidTarget, result.Disposition);
            });

        [Fact]
        public Task EnableTrackPower_DuringPause_IsRefused_NoStage6ViaDiagnosticMode()
            => WithPausedRuntimeAsync(CreateTransport(), async runtime =>
            {
                // Stage 6 (track power) is gated on Stage 5 (GlobalReady) Passed + observed neutral.
                // During the pause neither holds, so enabling is refused (fail-closed).
                runtime.AuthorizeTrackPowerEnable();
                bool enabled = await runtime.EnableTrackPowerAsync();

                Assert.False(enabled);
                Assert.False(runtime.TrackPowerOn);
            });
    }
}
