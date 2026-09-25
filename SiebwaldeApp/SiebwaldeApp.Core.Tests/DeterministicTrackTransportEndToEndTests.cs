using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Properties;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Deterministic end-to-end tests for the track runtime. They run the REAL
    /// <see cref="TrackCommClientAsync"/> + 9-step initialization pipeline + <see cref="TrackControlMain"/>
    /// write loop on top of the software-only <see cref="DeterministicTrackTransport"/>, with a fake
    /// <see cref="IEcosHostService"/> so no port (15471) or Koploper dial (5700) is involved.
    /// </summary>
    public class DeterministicTrackTransportEndToEndTests
    {
        private sealed class FakeEcosHostService : IEcosHostService
        {
            public event EventHandler<RuntimeFaultEventArgs>? Faulted;

            public bool IsRunning { get; private set; }
            public TrackControlMode? Mode { get; private set; }
            public ControlDiagnostics? Diagnostics => null;
            public bool IsUnsafe => false;

            public bool ResetSafety() => false;

            public Task<EcosHostStartResult> StartAsync(
                TrackControlMode mode,
                ITrackCommClient? commClient,
                TrackApplicationVariables? variables,
                CancellationToken cancellationToken = default)
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
            public List<DeterministicTrackTransport> Created { get; } = new();

            private readonly TrackSimulatorConfig _config;

            public SimulatorFactory(TrackSimulatorConfig config) => _config = config;

            public ITrackTransport Create()
            {
                var simulator = new DeterministicTrackTransport(_config);
                Created.Add(simulator);
                return simulator;
            }
        }

        private static readonly byte[] ExpectedInitCommands = { 250, 107, 100, 106, 101, 104, 105 };

        private static async Task RunWithRuntimeAsync(
            SimulatorFactory factory,
            Func<TrackApplicationRuntimeHost, CancellationToken, Task> testBody,
            TrackAmplifierGroups? amplifierGroups = null,
            TimeSpan? neutralObserveWindow = null)
        {
            // The real-mode init pipeline reads the firmware hex in FlashFwTrackamplifiersStep even when
            // flashing is skipped, so the test must not depend on the (untracked) repo dist/ hex being
            // present. Generate a minimal self-contained hex whose checksum equals the simulator's
            // FirmwareChecksum (0x251F), point the setting at it, then restore + delete on the way out.
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
                    amplifierGroups: amplifierGroups,
                    neutralObserveWindow: neutralObserveWindow);

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

        /// <summary>
        /// Writes a minimal Intel HEX that <see cref="TrackAmplifierBootloaderHelpers.Execute"/> parses
        /// to a file checksum of 0x251F: 1920 zero data rows, with the LAST row's first word set to
        /// 0x251F little-endian (0x1F, 0x25) and its checksum word (bytes 14..15) left zero (that word is
        /// excluded from the sum), followed by one 12-byte config row so the config word is "found".
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

            // One config row: record length 0x0C (12 bytes -> 24 hex chars). Its content is only used
            // when flashing, which is skipped, so any 12 bytes are acceptable.
            sb.Append(':').Append("0C").Append("0000").Append("00").Append(new string('F', 24)).Append("00").AppendLine();

            File.WriteAllText(path, sb.ToString());
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

        [Fact]
        public Task RealMode_EndToEnd_InitializesDetectsAndWritesAmplifier()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig());

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);

                // Running only happens once the init pipeline reached Completed.
                Assert.Equal(TrackRuntimeState.Running, runtime.State);

                var simulator = Assert.Single(factory.Created);

                // The init commands are a prefix; observed-neutral establishment appends the
                // neutral (108) writes for the configured domain.
                Assert.Equal(
                    ExpectedInitCommands,
                    simulator.SentCommands.Take(ExpectedInitCommands.Length).ToArray());

                for (int slave = 1; slave <= 3; slave++)
                {
                    var amplifier = runtime.TrackAmplifiers[slave];
                    Assert.Equal((ushort)1, amplifier.SlaveDetected);
                    Assert.Equal(
                        (ushort)TrackSimulatorConfig.DefaultFirmwareChecksum,
                        amplifier.HoldingReg[TrackAmplifierRegisters.SwChecksum]);
                }

                // Queue a manual write and wait for the 10 Hz TrackControlMain loop to push it
                // through the transport and echo it back as a SLAVEINFO frame.
                runtime.SetAmplifierControl(1, 500, false);

                await WaitUntilAsync(
                    () => simulator.GetRegisters(1)[0] == 0x01F4
                          && runtime.TrackAmplifiers[1].HoldingReg[0] == 0x01F4,
                    TimeSpan.FromSeconds(5));

                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            }, Groups(1, 2, 3));
        }

        [Fact]
        public Task Restart_UsesFreshTransportAndLeavesNoStalePendingWrites()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig());

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);
                Assert.Equal(TrackRuntimeState.Running, runtime.State);

                var first = Assert.Single(factory.Created);
                var firstCommands = first.SentCommands.ToArray();

                await runtime.RestartAsync(ct);
                Assert.Equal(TrackRuntimeState.Running, runtime.State);

                Assert.Equal(2, factory.Created.Count);
                var second = factory.Created[1];
                Assert.NotSame(first, second);

                // Fresh simulator and a deterministic re-run of the same init sequence.
                Assert.Equal(firstCommands, second.SentCommands.ToArray());

                // Give the 10 Hz write loop a few ticks: a stale PendingWrites entry from the first
                // run must not produce an EXEC_MBUS_SLAVE_DATA_EXCH (108) in the fresh runtime.
                await Task.Delay(300, ct);
                Assert.DoesNotContain((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, second.SentCommands.ToArray());

                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            });
        }

        [Fact]
        public Task ThreeStartStopCycles_EachReturnsToStoppedAndReleasesTransport()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig());

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                for (int i = 0; i < 3; i++)
                {
                    await runtime.StartAsync(TrackControlMode.Real, ct);
                    Assert.Equal(TrackRuntimeState.Running, runtime.State);

                    await runtime.StopAsync(ct);
                    Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
                }

                Assert.Equal(3, factory.Created.Count);

                foreach (var simulator in factory.Created)
                {
                    Assert.Equal(1, simulator.OpenCallCount);
                    Assert.True(simulator.CloseCallCount >= 1);
                    Assert.True(simulator.IsDisposed);
                }
            });
        }

        [Fact]
        public Task TwoInits_AreDeterministic()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig());

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);
                var first = Assert.Single(factory.Created);
                var firstCommands = first.SentCommands.ToArray();
                var firstRegisters = new Dictionary<byte, ushort[]>();
                for (byte slave = 1; slave <= 3; slave++)
                {
                    firstRegisters[slave] = first.GetRegisters(slave);
                }

                await runtime.StopAsync(ct);

                await runtime.StartAsync(TrackControlMode.Real, ct);
                Assert.Equal(TrackRuntimeState.Running, runtime.State);

                var second = factory.Created[1];
                Assert.Equal(firstCommands, second.SentCommands.ToArray());
                for (byte slave = 1; slave <= 3; slave++)
                {
                    Assert.Equal(firstRegisters[slave], second.GetRegisters(slave));
                }

                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            });
        }

        // -----------------------------------------------------------------
        // Observed-neutral restart/stop safety (software half)
        // -----------------------------------------------------------------

        private static TrackAmplifierGroups Groups(params int[] mainRailway)
            => TrackAmplifierGroups.Create(mainRailway, null, null);

        [Fact]
        public Task RealMode_AllDomainSlavesNeutral_MovementPermissionGranted_AndNonNeutralWrites108()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig()); // detected 1,2,3
            var groups = Groups(1, 2, 3);

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);

                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.True(runtime.IsMovementSafe);

                var simulator = Assert.Single(factory.Created);

                // A non-neutral control command now produces a 108 write with the non-neutral HR0.
                // (This exercises the shared funnel; the ECoS SetLocoSpeed path is covered at the
                // backend/interlock unit level, since this deterministic harness has no ECoS host.)
                runtime.SetAmplifierControl(1, 500, false);

                await WaitUntilAsync(
                    () => simulator.GetRegisters(1)[0] == 0x01F4,
                    TimeSpan.FromSeconds(5));
                Assert.Contains((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, simulator.SentCommands.ToArray());

                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            }, groups);
        }

        [Fact]
        public Task RealMode_Stop_CommandsAndObservesNeutral_ThenTearsDown()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig());
            var groups = Groups(1, 2, 3);

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);

                // Put a non-neutral value on an amplifier so the stop must neutralize it.
                runtime.SetAmplifierControl(1, 500, false);
                await WaitUntilAsync(
                    () => factory.Created[0].GetRegisters(1)[0] == 0x01F4,
                    TimeSpan.FromSeconds(5));

                await runtime.StopAsync(ct);

                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);

                var simulator = factory.Created[0];
                // Neutral was written back and observed before teardown, and the transport is disposed.
                Assert.Equal(AmplifierSpeedMapper.NeutralPwm, simulator.GetRegisters(1)[0] & 0x03FF);
                Assert.True(simulator.IsDisposed);
            }, groups);
        }

        [Fact]
        public Task RealMode_WrongReadback_NeverGrants_AndRefusesNonNeutral()
        {
            var config = new TrackSimulatorConfig(
                hr0ReadbackOverrides: new Dictionary<byte, ushort> { { 1, 400 } });
            var factory = new SimulatorFactory(config);
            var groups = Groups(1, 2, 3);
            var faults = new List<RuntimeFaultEventArgs>();

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                runtime.Faulted += (_, e) => faults.Add(e);

                await runtime.StartAsync(TrackControlMode.Real, ct);

                // Running does not imply movement-safe: neutral was NOT observed (readback 400).
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);
                Assert.NotEmpty(faults);

                var simulator = Assert.Single(factory.Created);

                // A non-neutral command is refused by the gate, so amplifier 1 stays at neutral
                // (399) instead of being overwritten with 500.
                runtime.SetAmplifierControl(1, 500, false);
                await Task.Delay(300, ct);

                Assert.Equal(AmplifierSpeedMapper.NeutralPwm, simulator.GetRegisters(1)[0] & 0x03FF);

                // A stop that cannot observe neutral is not a proven clean stop: it maps to Failed.
                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Failed, runtime.State);
            }, groups);
        }

        [Fact]
        public Task RealMode_StaleReadback_NeverGrants_AndMovementBlocked()
        {
            var config = new TrackSimulatorConfig(droppedEchoSlaves: new byte[] { 1 });
            var factory = new SimulatorFactory(config);
            var groups = Groups(1, 2, 3);
            var faults = new List<RuntimeFaultEventArgs>();

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                runtime.Faulted += (_, e) => faults.Add(e);

                await runtime.StartAsync(TrackControlMode.Real, ct);

                // Slave 1 never echoes its write, so its readback goes stale: no grant.
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);
                Assert.NotEmpty(faults);

                // The stop also cannot observe neutral, so it maps to Failed (never Stopped).
                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Failed, runtime.State);
            }, groups);
        }

        [Fact]
        public Task RealMode_ConfiguredButAbsentAmplifier_BlocksGrant()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig()); // detected 1,2,3
            var groups = Groups(1, 2, 4); // 4 is configured but never detected
            var faults = new List<RuntimeFaultEventArgs>();

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                runtime.Faulted += (_, e) => faults.Add(e);

                await runtime.StartAsync(TrackControlMode.Real, ct);

                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);
                Assert.NotEmpty(faults);

                // The absent amplifier also prevents observing neutral on stop: Failed, not Stopped.
                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Failed, runtime.State);
            }, groups);
        }

        [Fact]
        public Task RealMode_DetectedButUnconfiguredAmplifier_IsNotRequired()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig()); // detected 1,2,3
            var groups = Groups(1, 2); // 3 is detected but unconfigured: not required

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);

                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.True(runtime.IsMovementSafe);

                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            }, groups);
        }

        [Fact]
        public Task RealMode_Restart_FreshPermissionAndNoStalePendingWrites()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig());
            var groups = Groups(1, 2, 3);

            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.Real, ct);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);

                // Queue a non-neutral command so there is a pending write before restart.
                runtime.SetAmplifierControl(1, 500, false);

                await runtime.RestartAsync(ct);

                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                // A fresh start re-established neutral and re-created the permission.
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.True(runtime.IsMovementSafe);

                Assert.Equal(2, factory.Created.Count);
                var second = factory.Created[1];

                // Give the 10 Hz write loop a few ticks: no stale non-neutral write from the first
                // run reaches the fresh transport, so amplifier 1 stays at neutral.
                await Task.Delay(300, ct);
                Assert.Equal(AmplifierSpeedMapper.NeutralPwm, second.GetRegisters(1)[0] & 0x03FF);

                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            }, groups);
        }

        [Fact]
        public Task RealMode_EmptySafetyDomain_DoesNotGrant_AndBlocksMovement()
        {
            var factory = new SimulatorFactory(new TrackSimulatorConfig()); // detected 1,2,3
            var faults = new List<RuntimeFaultEventArgs>();

            // No amplifier groups configured: the safety domain is empty, so movement permission
            // must never be granted vacuously.
            return RunWithRuntimeAsync(factory, async (runtime, ct) =>
            {
                runtime.Faulted += (_, e) => faults.Add(e);

                await runtime.StartAsync(TrackControlMode.Real, ct);

                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);
                Assert.NotEmpty(faults);

                // Movement is blocked: a non-neutral command is refused, so the amplifier stays at
                // its initial value (never neutral-commanded, never non-neutral).
                runtime.SetAmplifierControl(1, 500, false);
                await Task.Delay(300, ct);

                var simulator = Assert.Single(factory.Created);
                Assert.Equal(0, simulator.GetRegisters(1)[0] & 0x03FF);

                await runtime.StopAsync(ct);
                Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            });
        }
    }
}
