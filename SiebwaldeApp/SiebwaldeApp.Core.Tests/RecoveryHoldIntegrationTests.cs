using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Properties;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// End-to-end recovery-hold tests over the REAL production pipeline
    /// (<see cref="TrackApplicationRuntimeHost"/> → <see cref="TrackInitStateMachine"/> → 9 steps →
    /// <see cref="TrackControlMain"/>) on the deterministic simulator transport. The fault is driven
    /// through the production <see cref="GlobalStopMonitor"/> latch path: after a healthy non-energized
    /// start the write loop is idle and the simulator has no periodic SLAVEINFO heartbeat, so the
    /// monitor latches <see cref="GlobalStopReason.Pic32Lost"/> once status and readback go stale
    /// (2 s) — the same production path a real silent PIC32 takes. No synthetic disconnect flag is set.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class RecoveryHoldIntegrationTests
    {
        private sealed class FakeEcosHostService : IEcosHostService
        {
            public event EventHandler<RuntimeFaultEventArgs>? Faulted;

            public bool IsRunning { get; private set; }
            public TrackControlMode? Mode { get; private set; }
            public ControlDiagnostics? Diagnostics => null;
            public bool IsUnsafe => false;

            public bool ResetSafety() => false;

            public void SetProfile(LayoutProfile profile) { }

            public IReadOnlyDictionary<int, SwitchPosition> GetSwitchPositions()
                => new Dictionary<int, SwitchPosition>();

            public Task<EcosHostStartResult> StartAsync(
                TrackControlMode mode,
                ITrackCommClient? commClient,
                TrackApplicationVariables? variables,
                CancellationToken cancellationToken = default,
                IMovementSimulation? movementSimulation = null)
            {
                Mode = mode;
                IsRunning = true;
                return Task.FromResult(EcosHostStartResult.Started);
            }

            public Task<EcosHostStopResult> StopAsync(CancellationToken ct = default)
            {
                IsRunning = false;
                Mode = null;
                return Task.FromResult(EcosHostStopResult.Stopped);
            }

            public void Stop()
            {
                IsRunning = false;
                Mode = null;
            }
        }

        private sealed class SimulatorFactory
        {
            private readonly TrackSimulatorConfig _config;

            public SimulatorFactory(TrackSimulatorConfig config) => _config = config;

            public List<DeterministicTrackTransport> Created { get; } = new();

            public ITrackTransport Create()
            {
                var simulator = new DeterministicTrackTransport(_config);
                Created.Add(simulator);
                return simulator;
            }
        }

        private static TrackAmplifierGroups Groups(params int[] mainRailway)
            => TrackAmplifierGroups.Create(mainRailway, null, null);

        private static TrackInitStageState Stage(TrackApplicationRuntimeHost runtime, TrackInitStage stage)
            => runtime.InitStages.First(s => s.Stage == stage).State;

        private static async Task RunWithRuntimeAsync(
            SimulatorFactory factory,
            TrackAmplifierGroups groups,
            Func<TrackApplicationRuntimeHost, CancellationToken, Task> testBody)
        {
            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-test-firmware-{Guid.NewGuid():N}.hex");

            try
            {
                WriteTestFirmwareHex(tempHexPath);
                CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var runtime = new TrackApplicationRuntimeHost(
                    new FakeEcosHostService(),
                    controlTrace: null,
                    transportFactory: factory.Create,
                    amplifierGroups: groups,
                    neutralObserveWindow: TimeSpan.FromSeconds(2));

                try
                {
                    await testBody(runtime, timeout.Token);
                }
                finally
                {
                    await runtime.DisposeAsync();
                }
            }
            finally
            {
                try { CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                try { File.Delete(tempHexPath); } catch { /* best-effort */ }
            }
        }

        /// <summary>Writes a minimal Intel HEX whose file checksum equals the simulator's 0x0E39.</summary>
        private static void WriteTestFirmwareHex(string path)
        {
            const int dataRows = (0x8000 - 0x800) / 16; // 1920

            var sb = new StringBuilder();
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

        [Fact]
        public Task CommLoss_EntersRecoveryHold_FailClosed_Invalidates4567_Retains23()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig()); // detected {1,2,3}, no heartbeat

            return RunWithRuntimeAsync(factory, Groups(1, 2, 3), async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(InitializationStatus.StoppedAtGlobalReady, runtime.RunStatus);

                // The production monitor latches Pic32Lost once the (idle) status/readback go stale.
                await WaitUntilAsync(
                    () => runtime.RunStatus == InitializationStatus.RecoveryHold,
                    TimeSpan.FromSeconds(15));

                Assert.True(runtime.GlobalStop.IsLatched);
                Assert.Equal(GlobalStopReason.Pic32Lost, runtime.GlobalStop.Reason);

                // Stages 4..7 invalidated (their evidence is stale); stages 2/3 retained.
                Assert.Equal(TrackInitStageState.Pending, Stage(runtime, TrackInitStage.ConfigureValidate));
                Assert.Equal(TrackInitStageState.Pending, Stage(runtime, TrackInitStage.GlobalReady));
                Assert.Equal(TrackInitStageState.Pending, Stage(runtime, TrackInitStage.AmpPowerEnable));
                Assert.Equal(TrackInitStageState.Pending, Stage(runtime, TrackInitStage.NormalOperation));
                Assert.Equal(TrackInitStageState.Passed, Stage(runtime, TrackInitStage.FirmwareAddress));
                Assert.Equal(TrackInitStageState.Passed, Stage(runtime, TrackInitStage.AmplifierStandby));

                // Fail-closed: movement, energize and re-arm are all withheld.
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);
                Assert.False(runtime.TrackPowerOn);
                Assert.False(runtime.TrackPowerAuthorized);
            });
        }

        [Fact]
        public Task Revalidate_Healthy_ReturnsToStoppedAtGlobalReady_NoAutoRearm_NoGrant()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig());

            return RunWithRuntimeAsync(factory, Groups(1, 2, 3), async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);
                Assert.Equal(InitializationStatus.StoppedAtGlobalReady, runtime.RunStatus);

                await WaitUntilAsync(
                    () => runtime.RunStatus == InitializationStatus.RecoveryHold,
                    TimeSpan.FromSeconds(15));
                Assert.True(runtime.GlobalStop.IsLatched);

                var simulator = Assert.Single(factory.Created);
                var rearmBefore = simulator.SentCommands.Count(c => c == TrackCommand.REARM_GLOBAL_STOP);

                // Reconnect (250) + re-run stages 1..5. The reconnect does NOT clear the latch.
                var result = await runtime.RevalidateAsync(ct);

                Assert.Equal(InitializationStatus.StoppedAtGlobalReady, result);
                Assert.Equal(InitializationStatus.StoppedAtGlobalReady, runtime.RunStatus);

                // Stages 1..5 Passed; Stage 6 gated; Stage 7 not Passed.
                for (var stage = TrackInitStage.ControllerConnection; stage <= TrackInitStage.GlobalReady; stage++)
                {
                    Assert.Equal(TrackInitStageState.Passed, Stage(runtime, stage));
                }
                Assert.NotEqual(TrackInitStageState.Passed, Stage(runtime, TrackInitStage.AmpPowerEnable));
                Assert.NotEqual(TrackInitStageState.Passed, Stage(runtime, TrackInitStage.NormalOperation));

                // The reconnect did NOT auto-clear the latch, did NOT re-arm (no 112), did NOT grant
                // movement, did NOT energize Stage 6.
                Assert.True(runtime.GlobalStop.IsLatched);
                Assert.Equal(GlobalStopReason.Pic32Lost, runtime.GlobalStop.Reason);
                Assert.Equal(rearmBefore, simulator.SentCommands.Count(c => c == TrackCommand.REARM_GLOBAL_STOP));
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.TrackPowerOn);
            });
        }

        [Fact]
        public Task Revalidate_MissingAmplifier_DoesNotReachStoppedAtGlobalReady()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig()); // detected {1,2,3}; amp 4 absent

            return RunWithRuntimeAsync(factory, Groups(1, 2, 4), async (runtime, ct) =>
            {
                // Amp 4 is configured but never detected: config fails closed (never StoppedAtGlobalReady).
                await runtime.StartAsync(TrackControlMode.Real, ct);
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.NotEqual(InitializationStatus.StoppedAtGlobalReady, runtime.RunStatus);

                // Revalidation re-runs the config gate and fails closed again.
                var result = await runtime.RevalidateAsync(ct);

                Assert.NotEqual(InitializationStatus.StoppedAtGlobalReady, result);
                Assert.Equal(TrackInitStageState.Failed, Stage(runtime, TrackInitStage.ConfigureValidate));
                Assert.Equal(TrackInitStageState.Failed, Stage(runtime, TrackInitStage.GlobalReady));
                Assert.NotEqual(TrackInitStageState.Passed, Stage(runtime, TrackInitStage.AmpPowerEnable));
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
            });
        }
    }
}
