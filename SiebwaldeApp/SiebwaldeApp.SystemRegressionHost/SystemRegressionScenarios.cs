using System.Text;
using System.Text.Json;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Properties;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Integration;

namespace SiebwaldeApp.SystemRegressionHost
{
    /// <summary>
    /// Records one deterministic simulator transport per factory invocation, so a scenario can
    /// inspect <see cref="DeterministicTrackTransport.SentCommands"/> /
    /// <see cref="DeterministicTrackTransport.SentFrames"/> / <see cref="DeterministicTrackTransport.GetRegisters"/>
    /// after the production composition has consumed it.
    /// </summary>
    internal sealed class SimulatorFactory
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

    /// <summary>One named, evidence-bearing assertion within a scenario.</summary>
    internal sealed class Check
    {
        public Check(string name, bool pass, string detail)
        {
            Name = name;
            Pass = pass;
            Detail = detail;
        }

        public string Name { get; }
        public bool Pass { get; }
        public string Detail { get; }
    }

    /// <summary>Machine-readable outcome of one scenario, serialized as a single JSONL line.</summary>
    internal sealed class ScenarioResult
    {
        public ScenarioResult(string scenario) => Scenario = scenario;

        public string Scenario { get; }
        public List<Check> Checks { get; } = new();
        public List<string> Faults { get; set; } = new();
        public List<int> SentCommands { get; set; } = new();
        public Dictionary<string, string> Stages { get; set; } = new();
        public string RunStatus { get; set; } = string.Empty;
        public string MovementPermission { get; set; } = string.Empty;
        public bool TrackPowerOn { get; set; }
        public string FirmwareDeploymentAuthorized { get; set; } = string.Empty;
        public List<string> Traces { get; } = new();

        public bool Pass => Checks.Count > 0 && Checks.All(c => c.Pass);

        public void Check(string name, bool pass, string detail)
        {
            Checks.Add(new Check(name, pass, detail));
            Traces.Add($"{(pass ? "PASS" : "FAIL")} {name}: {detail}");
        }

        public Dictionary<string, object?> ToJson() => new()
        {
            ["scenario"] = Scenario,
            ["pass"] = Pass,
            ["sentCommands"] = SentCommands,
            ["runStatus"] = RunStatus,
            ["movementPermission"] = MovementPermission,
            ["trackPowerOn"] = TrackPowerOn,
            ["firmwareDeploymentAuthorized"] = FirmwareDeploymentAuthorized,
            ["stages"] = Stages,
            ["faults"] = Faults,
            ["checks"] = Checks.Select(c => new { c.Name, c.Pass, c.Detail }).ToList(),
        };
    }

    /// <summary>
    /// Runs the six headless OFFLINE system-regression scenarios. Each composes the REAL
    /// production initialization pipeline via <see cref="TrackApplicationRuntimeHost"/> (the same
    /// composition the WPF application uses) with an injected deterministic simulator transport,
    /// then asserts observable effects against the public <see cref="ITrackApplicationRuntime"/>
    /// surface. No flash, no live hardware, no UDP socket, no protocol reimplementation and no
    /// second init state machine.
    /// </summary>
    internal sealed class ScenarioRunner
    {
        private readonly int[] _domain;

        public ScenarioRunner(int[] domain)
        {
            _domain = domain;
        }

        // Fixed "absent amplifier" address used by scenario C(i)/E-run-2: detected slaves are the
        // default {1,2,3}, so any configured address outside that set is deterministically absent.
        private const int AbsentAmp = 4;

        private static readonly byte[] ExpectedInitPrefix = { 250, 113, 111, 107, 100, 106, 101, 104, 105 };

        private static readonly byte[] FlashCommands =
        {
            TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH,        // 103
            TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE,         // 120
            TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD,     // 121
            TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES,        // 122
            TrackCommand.EXEC_FW_STATE_ERASE_FLASH,             // 125
            TrackCommand.EXEC_FW_STATE_WRITE_FLASH,             // 126
            TrackCommand.EXEC_FW_STATE_WRITE_CONFIG,            // 127
            TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE,         // 131
        };

        public Task<ScenarioResult> RunAsync(string scenarioId) => scenarioId.ToUpperInvariant() switch
        {
            "A" => ScenarioAAsync(),
            "B" => ScenarioBAsync(),
            "C" => ScenarioCAsync(),
            "D" => ScenarioDAsync(),
            "E" => ScenarioEAsync(),
            "F" => ScenarioFAsync(),
            _ => throw new ArgumentException($"Unknown scenario '{scenarioId}'. Expected A|B|C|D|E|F."),
        };

        // ---------------------------------------------------------------------------------
        // Scenario A — successful init stopping at GLOBAL READY (non-energized)
        // ---------------------------------------------------------------------------------

        private async Task<ScenarioResult> ScenarioAAsync()
        {
            var result = new ScenarioResult("A");
            var faults = new List<RuntimeFaultEventArgs>();

            // Amps start at firmware checksum 0x0E39, protocol 0x02 (default simulator).
            var factory = new SimulatorFactory(new TrackSimulatorConfig());

            await RunWithRuntimeAsync(factory, Groups(), CommittedFirmwarePath(), faults, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.FullSimulation, ct);

                var sim = factory.Created.Single();
                result.SentCommands = sim.SentCommands.Select(b => (int)b).ToList();
                result.RunStatus = runtime.RunStatus.ToString();
                result.MovementPermission = runtime.MovementPermission.ToString();
                result.TrackPowerOn = runtime.TrackPowerOn;
                result.Stages = SnapshotStages(runtime);

                // Command prefix: the production init sequence.
                var prefix = sim.SentCommands.Take(ExpectedInitPrefix.Length).ToArray();
                result.Check("command-prefix",
                    prefix.SequenceEqual(ExpectedInitPrefix),
                    $"sent={string.Join(",", prefix)} expected={string.Join(",", ExpectedInitPrefix)}");

                // Non-energized success: stopped at GlobalReady, never Completed.
                result.Check("run-status-stopped-at-global-ready",
                    runtime.RunStatus == InitializationStatus.StoppedAtGlobalReady,
                    runtime.RunStatus.ToString());

                // Stage states.
                result.Check("stage-globalready-passed",
                    Stage(runtime, TrackInitStage.GlobalReady) == TrackInitStageState.Passed,
                    Stage(runtime, TrackInitStage.GlobalReady).ToString());
                result.Check("stage-normaloperation-not-passed",
                    Stage(runtime, TrackInitStage.NormalOperation) != TrackInitStageState.Passed,
                    Stage(runtime, TrackInitStage.NormalOperation).ToString());

                // Movement + power: never granted, never energized.
                result.Check("movement-not-granted",
                    runtime.MovementPermission == MovementPermissionState.NotGranted,
                    runtime.MovementPermission.ToString());
                result.Check("track-power-off", !runtime.TrackPowerOn, runtime.TrackPowerOn.ToString());

                // Per-amplifier detection, identity (HR7 compatible), checksum (HR11), config.
                for (byte slave = 1; slave <= 3; slave++)
                {
                    var amplifier = runtime.TrackAmplifiers[slave];
                    var regs = sim.GetRegisters(slave);
                    result.Check($"amp{slave}-detected",
                        amplifier.SlaveDetected == 1, $"SlaveDetected={amplifier.SlaveDetected}");
                    result.Check($"amp{slave}-hr11-checksum-0e39",
                        regs[TrackAmplifierRegisters.SwChecksum] == TrackSimulatorConfig.DefaultFirmwareChecksum,
                        $"HR11=0x{regs[TrackAmplifierRegisters.SwChecksum]:X4}");
                    result.Check($"amp{slave}-identity-compatible",
                        TrackAmplifierRegisters.HasCompatibleIdentityFields(regs[TrackAmplifierRegisters.Identity]),
                        $"HR7=0x{regs[TrackAmplifierRegisters.Identity]:X4} Phase1Failure={amplifier.Phase1Failure ?? "<null>"}");
                    result.Check($"amp{slave}-config-verified",
                        amplifier.ConfigurationVerified, amplifier.ConfigurationVerified.ToString());
                }

                // Protocol 0x02 recognized (HR22): track-power-capable firmware, Phase-1 compatible.
                var hr22 = sim.GetRegisters(1)[TrackAmplifierRegisters.FwVersionPatchProtocol];
                result.Check("protocol-0x02-recognized",
                    TrackAmplifierRegisters.FwProtocolVersion(hr22) == TrackAmplifierRegisters.TrackPowerProtocolVersion,
                    $"HR22=0x{hr22:X4} protocol={TrackAmplifierRegisters.FwProtocolVersion(hr22)}");

                // No physical-output (non-neutral PWM) command was ever written.
                result.Check("no-non-neutral-pwm-commands",
                    NoNonNeutralPwmCommands(sim),
                    "every 108 write to HR0 (PwmCommand) carried the neutral 399");
            });

            result.Faults = faults.Select(f => $"{f.Source}: {f.Exception.Message}").ToList();
            return result;
        }

        // ---------------------------------------------------------------------------------
        // Scenario B — firmware mismatch (and the legacy-checksum guard)
        // ---------------------------------------------------------------------------------

        private async Task<ScenarioResult> ScenarioBAsync()
        {
            var result = new ScenarioResult("B");
            var faults = new List<RuntimeFaultEventArgs>();

            // B1: non-legacy checksum mismatch — configured artifact 0x0E38 vs amp firmware 0x0E39.
            var mismatchPath = Path.Combine(Path.GetTempPath(), $"siebwalde-regression-{Guid.NewGuid():N}.hex");
            WriteFirmwareHex(mismatchPath, 0x0E38);
            try
            {
                var factory = new SimulatorFactory(new TrackSimulatorConfig()); // amp checksum 0x0E39
                await RunWithRuntimeAsync(factory, Groups(), mismatchPath, faults, async (runtime, ct) =>
                {
                    await runtime.StartAsync(TrackControlMode.FullSimulation, ct);
                    var sim = factory.Created.Single();
                    result.SentCommands = sim.SentCommands.Select(b => (int)b).ToList();

                    result.Check("b1-firmwareaddress-blocked",
                        Stage(runtime, TrackInitStage.FirmwareAddress) == TrackInitStageState.Blocked,
                        Stage(runtime, TrackInitStage.FirmwareAddress).ToString());

                    var reason = StageReason(runtime, TrackInitStage.FirmwareAddress);
                    result.Check("b1-mismatch-diagnostic",
                        reason is not null && reason.Contains("checksum mismatch", StringComparison.OrdinalIgnoreCase),
                        reason ?? "<null>");

                    result.Check("b1-no-flash-commands",
                        !sim.SentCommands.Any(FlashCommands.Contains),
                        $"flash commands present: {string.Join(",", sim.SentCommands.Where(FlashCommands.Contains))}");

                    result.FirmwareDeploymentAuthorized = runtime.FirmwareDeploymentAuthorized.ToString();
                    result.Check("b1-firmware-deployment-not-authorized",
                        !runtime.FirmwareDeploymentAuthorized, runtime.FirmwareDeploymentAuthorized.ToString());
                });
            }
            finally
            {
                try { File.Delete(mismatchPath); } catch { /* best-effort */ }
            }

            // B2: legacy-checksum guard — configured artifact 0x251F (a known legacy build).
            var legacyPath = Path.Combine(Path.GetTempPath(), $"siebwalde-regression-{Guid.NewGuid():N}.hex");
            WriteFirmwareHex(legacyPath, 0x251F);
            try
            {
                var factory = new SimulatorFactory(new TrackSimulatorConfig());
                await RunWithRuntimeAsync(factory, Groups(), legacyPath, faults, async (runtime, ct) =>
                {
                    await runtime.StartAsync(TrackControlMode.FullSimulation, ct);
                    var sim = factory.Created.Single();

                    result.Check("b2-firmwareaddress-blocked",
                        Stage(runtime, TrackInitStage.FirmwareAddress) == TrackInitStageState.Blocked,
                        Stage(runtime, TrackInitStage.FirmwareAddress).ToString());

                    var reason = StageReason(runtime, TrackInitStage.FirmwareAddress);
                    result.Check("b2-legacy-diagnostic",
                        reason is not null && reason.Contains("legacy", StringComparison.OrdinalIgnoreCase),
                        reason ?? "<null>");

                    result.Check("b2-no-flash-commands",
                        !sim.SentCommands.Any(FlashCommands.Contains),
                        $"flash commands present: {string.Join(",", sim.SentCommands.Where(FlashCommands.Contains))}");
                });
            }
            finally
            {
                try { File.Delete(legacyPath); } catch { /* best-effort */ }
            }

            result.Faults = faults.Select(f => $"{f.Source}: {f.Exception.Message}").ToList();
            return result;
        }

        // ---------------------------------------------------------------------------------
        // Scenario C — missing amp / comm failure
        // ---------------------------------------------------------------------------------

        private async Task<ScenarioResult> ScenarioCAsync()
        {
            var result = new ScenarioResult("C");
            var faults = new List<RuntimeFaultEventArgs>();

            // (i) A configured-domain amplifier is absent from DetectedSlaves: fail-closed.
            var factory = new SimulatorFactory(new TrackSimulatorConfig()); // detected {1,2,3}
            await RunWithRuntimeAsync(factory, GroupsWithAbsent(), CommittedFirmwarePath(), faults, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.FullSimulation, ct);
                result.MovementPermission = runtime.MovementPermission.ToString();
                result.RunStatus = runtime.RunStatus.ToString();
                result.Stages = SnapshotStages(runtime);
                result.SentCommands = factory.Created.Single().SentCommands.Select(b => (int)b).ToList();

                result.Check("c1-movement-not-granted",
                    runtime.MovementPermission == MovementPermissionState.NotGranted,
                    runtime.MovementPermission.ToString());
                result.Check("c1-fault-raised", faults.Count > 0, $"faults={faults.Count}");
                result.Check("c1-configurevalidate-failed",
                    Stage(runtime, TrackInitStage.ConfigureValidate) == TrackInitStageState.Failed,
                    Stage(runtime, TrackInitStage.ConfigureValidate).ToString());
            });

            // (ii) Comm failure via the controller mute: command 250 is transmitted but never
            // acknowledged (the silent controller emits no reply), so the production init is
            // legitimately stuck on the silent controller: 250 sent, no 113, no false completion,
            // connect stage not passed, no unauthorized recovery (112), state Starting. The run is
            // cancelled deterministically via a bounded CTS (linked into the runtime) and the host is
            // awaited and disposed in a finally, so nothing relies on process exit.
            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var muteFaults = new List<RuntimeFaultEventArgs>();
            TrackApplicationRuntimeHost? muteRuntime = null;
            try
            {
                CoreSettings.Default.TrackAmplifierFwPath = CommittedFirmwarePath();

                var muteFactory = new SimulatorFactory(new TrackSimulatorConfig(controllerMute: true));
                muteRuntime = new TrackApplicationRuntimeHost(
                    new NullEcosHostService(), controlTrace: null, transportFactory: muteFactory.Create, amplifierGroups: Groups());
                muteRuntime.Faulted += (_, e) => muteFaults.Add(e);

                // Bounded cancellation: the runtime composition is linked to this token (see
                // TrackApplicationRuntimeHost.StartAsync), so cancelling it deterministically aborts
                // the stuck init with no unbounded hang and no reliance on process exit.
                using var muteCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var startTask = muteRuntime.StartAsync(TrackControlMode.FullSimulation, muteCts.Token);

                // Wait until the connection request (250) has been sent, then confirm no progress.
                await WaitUntilAsync(
                    () => muteFactory.Created.Count == 1 && muteFactory.Created[0].SentCommands.Contains(EnumClientCommands.CLIENT_CONNECTION_REQUEST),
                    TimeSpan.FromSeconds(5));
                await Task.Delay(1500); // give the silent controller ample time to (not) reply

                var sim = muteFactory.Created[0];
                result.Check("c2-connect-request-sent",
                    sim.SentCommands.Contains(EnumClientCommands.CLIENT_CONNECTION_REQUEST),
                    $"sent={string.Join(",", sim.SentCommands)}");
                result.Check("c2-no-version-query",
                    !sim.SentCommands.Contains(TrackCommand.GET_CONTROLLER_VERSION),
                    $"113 present: {sim.SentCommands.Contains(TrackCommand.GET_CONTROLLER_VERSION)}");
                result.Check("c2-no-false-completion",
                    muteRuntime.RunStatus is not InitializationStatus.Completed and not InitializationStatus.StoppedAtGlobalReady,
                    muteRuntime.RunStatus.ToString());
                result.Check("c2-connect-stage-not-passed",
                    Stage(muteRuntime, TrackInitStage.ControllerConnection) != TrackInitStageState.Passed,
                    Stage(muteRuntime, TrackInitStage.ControllerConnection).ToString());
                result.Check("c2-no-unauthorized-recovery",
                    !sim.SentCommands.Contains(TrackCommand.REARM_GLOBAL_STOP),
                    $"112 present: {sim.SentCommands.Contains(TrackCommand.REARM_GLOBAL_STOP)}");
                result.Check("c2-runtime-still-starting",
                    muteRuntime.State == TrackRuntimeState.Starting,
                    muteRuntime.State.ToString());
                result.Check("c2-transmitted-never-acknowledged",
                    sim.SentCommands.Contains(EnumClientCommands.CLIENT_CONNECTION_REQUEST) &&
                    !sim.SentCommands.Contains(TrackCommand.GET_CONTROLLER_VERSION),
                    $"250 transmitted={sim.SentCommands.Contains(EnumClientCommands.CLIENT_CONNECTION_REQUEST)}; " +
                    $"never acknowledged (no 113 issued)={!sim.SentCommands.Contains(TrackCommand.GET_CONTROLLER_VERSION)}");

                result.RunStatus = muteRuntime.RunStatus.ToString();

                // Deterministic teardown: cancel the start (the runtime transitions fail-closed to
                // Failed), await it so no fire-and-forget task leaks, then dispose the host.
                muteCts.Cancel();
                try
                {
                    await startTask;
                }
                catch (OperationCanceledException)
                {
                    // The start was cancelled; the runtime ends fail-closed (Failed), never Running.
                }
            }
            finally
            {
                try { CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                if (muteRuntime is not null)
                {
                    try { await muteRuntime.DisposeAsync(); } catch { /* best-effort teardown */ }
                }
            }

            result.Faults = faults
                .Concat(muteFaults)
                .Select(f => $"{f.Source}: {f.Exception.Message}").ToList();
            return result;
        }

        // ---------------------------------------------------------------------------------
        // Scenario D — incorrect protocol / stale info
        // ---------------------------------------------------------------------------------

        private async Task<ScenarioResult> ScenarioDAsync()
        {
            var result = new ScenarioResult("D");
            var faults = new List<RuntimeFaultEventArgs>();

            // (i) Protocol mismatch: amp 1 advertises protocol 0x00 (Phase-1-incompatible).
            var cfg = new TrackSimulatorConfig(
                protocolVersionOverrides: new Dictionary<byte, ushort> { { 1, 0x00 } });
            var factory = new SimulatorFactory(cfg);
            await RunWithRuntimeAsync(factory, Groups(), CommittedFirmwarePath(), faults, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.FullSimulation, ct);
                result.Stages = SnapshotStages(runtime);
                result.MovementPermission = runtime.MovementPermission.ToString();

                var amp1 = runtime.TrackAmplifiers[1];
                result.Check("d1-phase1failure-incompatible-protocol",
                    amp1.Phase1Failure == AmplifierPhase1Safety.IncompatibleProtocolVersion,
                    amp1.Phase1Failure ?? "<null>");
                result.Check("d1-configurevalidate-failed",
                    Stage(runtime, TrackInitStage.ConfigureValidate) == TrackInitStageState.Failed,
                    Stage(runtime, TrackInitStage.ConfigureValidate).ToString());
                result.Check("d1-no-grant",
                    runtime.MovementPermission == MovementPermissionState.NotGranted,
                    runtime.MovementPermission.ToString());
            });
            result.SentCommands = factory.Created.Single().SentCommands.Select(b => (int)b).ToList();

            // (ii) Stale readback: amp 1 write produces no SLAVEINFO echo -> fail-closed.
            var faults2 = new List<RuntimeFaultEventArgs>();
            var cfg2 = new TrackSimulatorConfig(droppedEchoSlaves: new byte[] { 1 });
            var factory2 = new SimulatorFactory(cfg2);
            await RunWithRuntimeAsync(factory2, Groups(), CommittedFirmwarePath(), faults2, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.FullSimulation, ct);
                result.Check("d2-movement-not-granted",
                    runtime.MovementPermission == MovementPermissionState.NotGranted,
                    runtime.MovementPermission.ToString());
                result.Check("d2-neutralnotestablished-fault",
                    faults2.Any(f => f.Source.Contains("EstablishObservedNeutral", StringComparison.Ordinal)),
                    string.Join("; ", faults2.Select(f => f.Source)));
            });

            result.Faults = faults
                .Concat(faults2)
                .Select(f => $"{f.Source}: {f.Exception.Message}").ToList();
            return result;
        }

        // ---------------------------------------------------------------------------------
        // Scenario E — stage dependencies
        // ---------------------------------------------------------------------------------

        private async Task<ScenarioResult> ScenarioEAsync()
        {
            var result = new ScenarioResult("E");
            var faults = new List<RuntimeFaultEventArgs>();

            // Run 1: normal non-energized run — stopped at GlobalReady, Stage 6 operator-gated.
            var factory = new SimulatorFactory(new TrackSimulatorConfig());
            await RunWithRuntimeAsync(factory, Groups(), CommittedFirmwarePath(), faults, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.FullSimulation, ct);
                result.RunStatus = runtime.RunStatus.ToString();
                result.MovementPermission = runtime.MovementPermission.ToString();
                result.Stages = SnapshotStages(runtime);

                result.Check("e1-stopped-at-global-ready-not-completed",
                    runtime.RunStatus == InitializationStatus.StoppedAtGlobalReady,
                    runtime.RunStatus.ToString());
                result.Check("e1-globalready-passed",
                    Stage(runtime, TrackInitStage.GlobalReady) == TrackInitStageState.Passed,
                    Stage(runtime, TrackInitStage.GlobalReady).ToString());
                result.Check("e1-normaloperation-not-passed-before-stage6",
                    Stage(runtime, TrackInitStage.NormalOperation) != TrackInitStageState.Passed,
                    Stage(runtime, TrackInitStage.NormalOperation).ToString());

                // Stage 6 gated on operator authorization: enabling without authorize is refused.
                var refused = await runtime.EnableTrackPowerAsync(ct);
                result.Check("e1-stage6-refused-without-operator-auth",
                    !refused && !runtime.TrackPowerOn && runtime.MovementPermission == MovementPermissionState.NotGranted,
                    $"EnableTrackPowerAsync={refused} TrackPowerOn={runtime.TrackPowerOn} Movement={runtime.MovementPermission}");
            });
            result.SentCommands = factory.Created.Single().SentCommands.Select(b => (int)b).ToList();

            // Run 2: ConfigureValidate failure prevents GlobalReady, and Stage 6 is refused
            // even after operator authorization because Stage 5 is not Passed (fail-closed).
            var faults2 = new List<RuntimeFaultEventArgs>();
            var factory2 = new SimulatorFactory(new TrackSimulatorConfig());
            await RunWithRuntimeAsync(factory2, GroupsWithAbsent(), CommittedFirmwarePath(), faults2, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.FullSimulation, ct);

                result.Check("e2-configurevalidate-failed",
                    Stage(runtime, TrackInitStage.ConfigureValidate) == TrackInitStageState.Failed,
                    Stage(runtime, TrackInitStage.ConfigureValidate).ToString());
                result.Check("e2-globalready-failed",
                    Stage(runtime, TrackInitStage.GlobalReady) == TrackInitStageState.Failed,
                    Stage(runtime, TrackInitStage.GlobalReady).ToString());

                runtime.AuthorizeTrackPowerEnable();
                var refused = await runtime.EnableTrackPowerAsync(ct);
                result.Check("e2-stage6-refused-when-stage5-failed",
                    !refused && !runtime.TrackPowerOn && runtime.MovementPermission == MovementPermissionState.NotGranted,
                    $"EnableTrackPowerAsync={refused} TrackPowerOn={runtime.TrackPowerOn} Movement={runtime.MovementPermission}");
            });

            result.Faults = faults
                .Concat(faults2)
                .Select(f => $"{f.Source}: {f.Exception.Message}").ToList();
            return result;
        }

        // ---------------------------------------------------------------------------------
        // Scenario F — simulated Stage 6 (three concepts)
        // ---------------------------------------------------------------------------------

        private async Task<ScenarioResult> ScenarioFAsync()
        {
            var result = new ScenarioResult("F");
            var faults = new List<RuntimeFaultEventArgs>();

            // Concepts 1 + 2: normal run.
            var factory = new SimulatorFactory(new TrackSimulatorConfig());
            await RunWithRuntimeAsync(factory, Groups(), CommittedFirmwarePath(), faults, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.FullSimulation, ct);
                result.RunStatus = runtime.RunStatus.ToString();
                result.MovementPermission = runtime.MovementPermission.ToString();
                result.TrackPowerOn = runtime.TrackPowerOn;
                result.Stages = SnapshotStages(runtime);

                // Concept 1: neutral verified, but Stage 6 not run -> still NotGranted.
                result.Check("f1-neutral-verified-no-fault",
                    !faults.Any(f => f.Source.Contains("EstablishObservedNeutral", StringComparison.Ordinal)),
                    string.Join("; ", faults.Select(f => f.Source)));
                result.Check("f1-stage6-not-run-still-notgranted",
                    runtime.MovementPermission == MovementPermissionState.NotGranted &&
                    runtime.RunStatus == InitializationStatus.StoppedAtGlobalReady,
                    $"Movement={runtime.MovementPermission} RunStatus={runtime.RunStatus}");

                // Grant must never precede Stage-6 success.
                var beforeEnable = runtime.MovementPermission;
                result.Check("f1-grant-never-precedes-stage6",
                    beforeEnable != MovementPermissionState.Granted,
                    beforeEnable.ToString());

                // Concept 2: full sequence -> Granted + Completed.
                runtime.AuthorizeTrackPowerEnable();
                var ok = await runtime.EnableTrackPowerAsync(ct);
                result.Check("f2-enable-succeeds", ok, ok.ToString());
                result.Check("f2-granted",
                    runtime.MovementPermission == MovementPermissionState.Granted && runtime.IsMovementSafe,
                    runtime.MovementPermission.ToString());
                result.Check("f2-completed",
                    runtime.RunStatus == InitializationStatus.Completed, runtime.RunStatus.ToString());
                result.Check("f2-track-power-on", runtime.TrackPowerOn, runtime.TrackPowerOn.ToString());
                result.Check("f2-normaloperation-passed",
                    Stage(runtime, TrackInitStage.NormalOperation) == TrackInitStageState.Passed,
                    Stage(runtime, TrackInitStage.NormalOperation).ToString());

                result.MovementPermission = runtime.MovementPermission.ToString();
                result.TrackPowerOn = runtime.TrackPowerOn;
                result.RunStatus = runtime.RunStatus.ToString();
                result.Stages = SnapshotStages(runtime);
            });
            result.SentCommands = factory.Created.Single().SentCommands.Select(b => (int)b).ToList();

            // Concept 3: Stage 6 refused when neutral not verified (non-neutral readback).
            var faults3 = new List<RuntimeFaultEventArgs>();
            var cfg3 = new TrackSimulatorConfig(
                hr0ReadbackOverrides: new Dictionary<byte, ushort> { { 1, 400 } });
            var factory3 = new SimulatorFactory(cfg3);
            await RunWithRuntimeAsync(factory3, Groups(), CommittedFirmwarePath(), faults3, async (runtime, ct) =>
            {
                await runtime.StartAsync(TrackControlMode.FullSimulation, ct);
                runtime.AuthorizeTrackPowerEnable();
                var ok = await runtime.EnableTrackPowerAsync(ct);
                result.Check("f3-stage6-refused-when-neutral-not-verified",
                    !ok && !runtime.TrackPowerOn && runtime.MovementPermission == MovementPermissionState.NotGranted,
                    $"EnableTrackPowerAsync={ok} TrackPowerOn={runtime.TrackPowerOn} Movement={runtime.MovementPermission}");
            });

            result.Faults = faults
                .Concat(faults3)
                .Select(f => $"{f.Source}: {f.Exception.Message}").ToList();
            return result;
        }

        // ---------------------------------------------------------------------------------
        // Shared helpers
        // ---------------------------------------------------------------------------------

        /// <summary>Composes the production runtime and runs <paramref name="body"/> against it.</summary>
        private static async Task RunWithRuntimeAsync(
            SimulatorFactory factory,
            TrackAmplifierGroups groups,
            string firmwarePath,
            List<RuntimeFaultEventArgs> faults,
            Func<TrackApplicationRuntimeHost, CancellationToken, Task> body)
        {
            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            CoreSettings.Default.TrackAmplifierFwPath = firmwarePath;
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var runtime = new TrackApplicationRuntimeHost(
                    new NullEcosHostService(),
                    controlTrace: null,
                    transportFactory: factory.Create,
                    amplifierGroups: groups);
                runtime.Faulted += (_, e) => faults.Add(e);
                try
                {
                    await body(runtime, timeout.Token);
                }
                finally
                {
                    try { await runtime.DisposeAsync(); } catch { /* best-effort teardown */ }
                }
            }
            finally
            {
                try { CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
            }
        }

        private TrackAmplifierGroups Groups() => TrackAmplifierGroups.Create(_domain, null, null);

        private TrackAmplifierGroups GroupsWithAbsent()
            => TrackAmplifierGroups.Create(_domain.Concat(new[] { AbsentAmp }).Distinct(), null, null);

        private static TrackInitStageState Stage(TrackApplicationRuntimeHost runtime, TrackInitStage stage)
            => runtime.InitStages.First(s => s.Stage == stage).State;

        private static string? StageReason(TrackApplicationRuntimeHost runtime, TrackInitStage stage)
            => runtime.InitStages.First(s => s.Stage == stage).FailureReason;

        private static Dictionary<string, string> SnapshotStages(TrackApplicationRuntimeHost runtime)
            => runtime.InitStages.ToDictionary(s => s.Stage.ToString(), s => s.State.ToString());

        /// <summary>The committed 0x0E39 firmware fixture, copied next to the exe at build time.</summary>
        private static string CommittedFirmwarePath()
            => Path.Combine(AppContext.BaseDirectory, "Firmware-0x0E39.hex");

        /// <summary>
        /// Mirrors <c>WriteTestFirmwareHex</c> (DeterministicTrackTransportEndToEndTests): writes a
        /// minimal Intel HEX whose file checksum equals <paramref name="checksum"/> (last data row's
        /// first word little-endian, the per-row checksum word excluded from the sum), plus one
        /// config row so the bootloader helper finds the config word.
        /// </summary>
        private static void WriteFirmwareHex(string path, ushort checksum)
        {
            const int dataRows = (0x8000 - 0x800) / 16; // 1920, matching TrackAmplifierBootloaderHelpers.Execute

            var low = (byte)(checksum & 0xFF);
            var high = (byte)(checksum >> 8);
            var wordHex = low.ToString("X2") + high.ToString("X2");

            var sb = new StringBuilder();
            for (int row = 0; row < dataRows; row++)
            {
                string address = (row * 16).ToString("X4");
                string data = row == dataRows - 1
                    ? wordHex + new string('0', 28)
                    : new string('0', 32);

                sb.Append(':').Append("10").Append(address).Append("00").Append(data).Append("00").AppendLine();
            }

            sb.Append(':').Append("0C").Append("0000").Append("00").Append(new string('F', 24)).Append("00").AppendLine();
            File.WriteAllText(path, sb.ToString());
        }

        /// <summary>True when no 108 write to HR0 (PwmCommand) carried a non-neutral value.</summary>
        private static bool NoNonNeutralPwmCommands(DeterministicTrackTransport sim)
        {
            foreach (var frame in sim.SentFrames)
            {
                if (frame.Length >= 8 &&
                    frame[1] == TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH &&
                    frame[3] == 0xAA && // WRITE direction
                    frame[5] == TrackAmplifierRegisters.PwmCommand)
                {
                    ushort value = (ushort)(frame[6] | (frame[7] << 8));
                    if ((value & TrackAmplifierRegisters.PwmMask) != AmplifierSpeedMapper.NeutralPwm)
                    {
                        return false;
                    }
                }
            }

            return true;
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
    }
}
