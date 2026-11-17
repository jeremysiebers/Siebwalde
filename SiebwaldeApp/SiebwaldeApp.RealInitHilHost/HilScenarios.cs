using System.Text;
using System.Text.Json;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Comm;
using SiebwaldeApp.Integration;

namespace SiebwaldeApp.RealInitHilHost
{
    /// <summary>One named, evidence-bearing assertion/observation within a subcommand run.</summary>
    internal sealed class HilCheck
    {
        public HilCheck(string name, bool pass, string detail)
        {
            Name = name;
            Pass = pass;
            Detail = detail;
        }

        public string Name { get; }
        public bool Pass { get; }
        public string Detail { get; }
    }

    /// <summary>Per-amplifier hardware readback snapshot (holding registers + status).</summary>
    internal sealed class AmpRecord
    {
        public int Slave { get; set; }
        public ushort SlaveDetected { get; set; }
        public bool ConfigurationVerified { get; set; }
        public string? Phase1Failure { get; set; }
        public string Hr7 { get; set; } = "0x0000";
        public string Hr11 { get; set; } = "0x0000";
        public string Hr1 { get; set; } = "0x0000";
        public string Hr8 { get; set; } = "0x0000";
        public string Hr9 { get; set; } = "0x0000";
        public string Hr21 { get; set; } = "0x0000";
        public string Hr22 { get; set; } = "0x0000";
        public int Hr22Protocol { get; set; }
        public bool Ready { get; set; }

        public Dictionary<string, object?> ToJson() => new()
        {
            ["slave"] = Slave,
            ["slaveDetected"] = SlaveDetected,
            ["configurationVerified"] = ConfigurationVerified,
            ["phase1Failure"] = Phase1Failure,
            ["hr7"] = Hr7,
            ["hr11"] = Hr11,
            ["hr1"] = Hr1,
            ["hr8"] = Hr8,
            ["hr9"] = Hr9,
            ["hr21"] = Hr21,
            ["hr22"] = Hr22,
            ["hr22Protocol"] = Hr22Protocol,
            ["ready"] = Ready,
        };
    }

    /// <summary>Machine-readable outcome of one subcommand run, serialized as a single JSONL line.</summary>
    internal sealed class HilResult
    {
        public HilResult(string subcommand)
        {
            Subcommand = subcommand;
            Timestamp = DateTimeOffset.UtcNow.ToString("O");
        }

        public string Subcommand { get; }
        public string Timestamp { get; }
        public bool Pass { get; set; }
        public List<HilCheck> Checks { get; } = new();
        public List<string> Faults { get; } = new();
        public List<string> Timeline { get; } = new();

        public string? ControllerVersion { get; set; }
        public string? GlobalStopState { get; set; }
        public string RuntimeState { get; set; } = string.Empty;
        public string RunStatus { get; set; } = string.Empty;
        public string MovementPermission { get; set; } = string.Empty;
        public bool TrackPowerOn { get; set; }
        public bool FirmwareDeploymentAuthorized { get; set; }
        public Dictionary<string, string> Stages { get; set; } = new();
        public List<AmpRecord> Amps { get; } = new();

        // Repeated-init campaign (hil-d) fields: one JSONL line per cycle records the cycle index,
        // the recovery action taken from the 111 read, and the first failure (if any).
        public int Cycle { get; set; }
        public string RecoveryAction { get; set; } = string.Empty;
        public string? FirstFailure { get; set; }

        public void Check(string name, bool pass, string detail) => Checks.Add(new HilCheck(name, pass, detail));

        public void AddTimeline(string entry) => Timeline.Add($"{DateTimeOffset.UtcNow:O} {entry}");

        public Dictionary<string, object?> ToJson() => new()
        {
            ["subcommand"] = Subcommand,
            ["timestamp"] = Timestamp,
            ["pass"] = Pass,
            ["controllerVersion"] = ControllerVersion,
            ["globalStopState"] = GlobalStopState,
            ["runtimeState"] = RuntimeState,
            ["runStatus"] = RunStatus,
            ["movementPermission"] = MovementPermission,
            ["trackPowerOn"] = TrackPowerOn,
            ["firmwareDeploymentAuthorized"] = FirmwareDeploymentAuthorized,
            ["stages"] = Stages,
            ["amps"] = Amps.Select(a => a.ToJson()).ToList(),
            ["checks"] = Checks.Select(c => new { c.Name, c.Pass, c.Detail }).ToList(),
            ["timeline"] = Timeline,
            ["faults"] = Faults,
            ["cycle"] = Cycle,
            ["recoveryAction"] = RecoveryAction,
            ["firstFailure"] = FirstFailure,
        };
    }

    /// <summary>
    /// Bounded, fail-closed command/acknowledgement probe over the REAL
    /// <see cref="TrackCommClientAsync"/> receive path. It sends exactly one command and waits
    /// (bounded) for a matching control message, never fabricating an acknowledgement that was not
    /// received. It is used only by <c>hil-a</c> and performs no amplifier-register write, no
    /// address-write (104), no enable (105), no data-exchange (108), no config, no firmware and no
    /// track-power command.
    /// </summary>
    internal sealed class BoundedCommProbe
    {
        private readonly TrackCommClientAsync _comm;
        private readonly List<ReceivedMessage> _received = new();
        private readonly object _lock = new();

        public BoundedCommProbe(TrackCommClientAsync comm)
        {
            _comm = comm;
            _comm.ControlMessageReceived += OnControlMessage;
        }

        private void OnControlMessage(object? sender, ControlMessageEventArgs e)
        {
            lock (_lock)
            {
                _received.Add(e.Message);
            }
        }

        public void Detach() => _comm.ControlMessageReceived -= OnControlMessage;

        private ReceivedMessage? TakeMatching(Func<ReceivedMessage, bool> done)
        {
            lock (_lock)
            {
                for (int i = 0; i < _received.Count; i++)
                {
                    if (done(_received[i]))
                    {
                        var match = _received[i];
                        _received.RemoveAt(i);
                        return match;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Sends one command, then waits (bounded by <paramref name="timeout"/>, cancelled by
        /// <paramref name="ct"/>) for the first received message matching <paramref name="done"/>.
        /// Returns the matching message, or null when the bound elapses or cancellation is requested.
        /// </summary>
        public async Task<ReceivedMessage?> SendAndWaitAsync(
            byte command,
            byte[] data,
            Func<ReceivedMessage, bool> done,
            TimeSpan timeout,
            CancellationToken ct)
        {
            await _comm.SendAsync(new SendMessage(command, data), ct).ConfigureAwait(false);

            var deadline = DateTimeOffset.UtcNow + timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (ct.IsCancellationRequested)
                {
                    return null;
                }

                var match = TakeMatching(done);
                if (match.HasValue)
                {
                    return match;
                }

                try
                {
                    await Task.Delay(20, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }

            return null;
        }
    }

    /// <summary>The two HIL subcommands.</summary>
    internal static class HilScenarios
    {
        private static readonly byte[] SessionCommandData = new byte[80];

        private static readonly TimeSpan PerCommandTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan SlaveInfoObservationWindow = TimeSpan.FromSeconds(3);

        // The expected Phase-1 hardware readbacks (offline scenario A).
        private const ushort ExpectedHr7 = 0xF101;
        private const ushort ExpectedHr11 = 0x0E39;

        // ---------------------------------------------------------------------------------
        // hil-a — passive diagnostics (read-only, bounded comm path)
        // ---------------------------------------------------------------------------------

        public static async Task<HilResult> RunHilAAsync(TimeSpan overallTimeout)
        {
            var result = new HilResult("hil-a");
            using var cts = new CancellationTokenSource(overallTimeout);

            var variables = new TrackApplicationVariables();
            TrackCommClientAsync comm;
            try
            {
                // Real UDP transport, bound to the production controller receive port. A bind
                // conflict (another session owns the controller) surfaces here as a socket exception.
                var transport = new RawUdpTrackTransport(new RawUdpTransport(
                    CoreConfiguration.TrackControllerIpAddress,
                    CoreConfiguration.TrackControllerSendingPort,
                    CoreConfiguration.TrackControllerReceivingPort));
                comm = new TrackCommClientAsync(transport, variables);
            }
            catch (Exception ex)
            {
                result.Check("transport", false, $"cannot open real UDP transport: {ex.Message}");
                result.Faults.Add($"transport: {ex.Message}");
                result.Pass = false;
                return result;
            }

            try
            {
                await comm.StartAsync(realHardwareMode: true, cts.Token).ConfigureAwait(false);
                var probe = new BoundedCommProbe(comm);

                // 250 — CLIENT_CONNECTION_REQUEST (establish the session).
                var connect = await probe.SendAndWaitAsync(
                    EnumClientCommands.CLIENT_CONNECTION_REQUEST,
                    SessionCommandData,
                    m => m.TaskId == TaskId.CONTROLLER && m.Taskcommand == TaskStates.CONNECTED && m.Taskstate == TaskStates.DONE,
                    PerCommandTimeout,
                    cts.Token).ConfigureAwait(false);
                result.Check("connect",
                    connect.HasValue,
                    connect.HasValue ? "CONNECTED/DONE acknowledged" : "no CONNECTED/DONE acknowledgement (controller unreachable)");

                // 113 — GET_CONTROLLER_VERSION (read-only controller identity).
                var versionReply = await probe.SendAndWaitAsync(
                    TrackCommand.GET_CONTROLLER_VERSION,
                    Array.Empty<byte>(),
                    m => m.TaskId == TaskId.CONTROLLER && m.Taskcommand == TrackCommand.GET_CONTROLLER_VERSION,
                    PerCommandTimeout,
                    cts.Token).ConfigureAwait(false);
                var version = variables.trackControllerCommands.ControllerVersion;
                result.ControllerVersion = version?.ToString();
                result.Check("controller-version",
                    versionReply.HasValue,
                    versionReply.HasValue ? $"version={version}" : "no 113 reply");

                // 111 — GLOBAL_STOP_STATE (read-only global-stop / host-authority state).
                var gsReply = await probe.SendAndWaitAsync(
                    TrackCommand.GLOBAL_STOP_STATE,
                    Array.Empty<byte>(),
                    m => m.TaskId == TaskId.CONTROLLER && m.Taskcommand == TrackCommand.GLOBAL_STOP_STATE && m.Taskstate == TaskStates.DONE,
                    PerCommandTimeout,
                    cts.Token).ConfigureAwait(false);
                if (gsReply is { } gs)
                {
                    result.GlobalStopState = DescribeGlobalStop(gs.Taskmessage);
                    result.Check("global-stop-state", true, $"state={gs.Taskmessage} ({DescribeGlobalStop(gs.Taskmessage)})");
                }
                else
                {
                    result.GlobalStopState = "unknown";
                    result.Check("global-stop-state", false, "no 111 reply");
                }

                // Bounded telemetry-enablement: 100 -> 106 -> 101 (no 104 address-write, no 105
                // enable, no 108 write, no config, no firmware, no track power).
                var slavesOn = await probe.SendAndWaitAsync(
                    TrackCommand.EXEC_MBUS_STATE_SLAVES_ON,
                    SessionCommandData,
                    m => m.TaskId == TaskId.MBUS && m.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVES_ON && m.Taskstate == TaskStates.DONE,
                    PerCommandTimeout,
                    cts.Token).ConfigureAwait(false);
                result.Check("slaves-on",
                    slavesOn.HasValue,
                    slavesOn.HasValue ? "MBUS_STATE_SLAVES_ON/DONE acknowledged" : "no 100 acknowledgement");

                var dataUpload = await probe.SendAndWaitAsync(
                    TrackCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD,
                    SessionCommandData,
                    m => m.TaskId == TaskId.MBUS && m.Taskcommand == EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD && m.Taskstate == TaskStates.DONE,
                    PerCommandTimeout,
                    cts.Token).ConfigureAwait(false);
                result.Check("data-upload",
                    dataUpload.HasValue,
                    dataUpload.HasValue ? "MBUS_STATE_START_DATA_UPLOAD/DONE acknowledged" : "no 106 acknowledgement");

                var slaveDetect = await probe.SendAndWaitAsync(
                    TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT,
                    SessionCommandData,
                    m => m.TaskId == TaskId.MBUS && m.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_DETECT && m.Taskstate == TaskStates.DONE,
                    PerCommandTimeout,
                    cts.Token).ConfigureAwait(false);
                result.Check("slave-detect",
                    slaveDetect.HasValue,
                    slaveDetect.HasValue ? "MBUS_STATE_SLAVE_DETECT/DONE acknowledged" : "no 101 acknowledgement");

                // Wait (bounded) for the master's cyclic SLAVEINFO frames to populate the read side.
                try
                {
                    await Task.Delay(SlaveInfoObservationWindow, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // overall bound elapsed; snapshot whatever was observed.
                }

                foreach (var amp in DetectedTrackAmplifiers(variables))
                {
                    result.Amps.Add(BuildAmpRecord(amp));
                }

                probe.Detach();
            }
            catch (OperationCanceledException)
            {
                result.Faults.Add("hil-a cancelled (timeout).");
            }
            catch (Exception ex)
            {
                result.Faults.Add($"hil-a failed: {ex.Message}");
            }
            finally
            {
                try { await comm.DisposeAsync().ConfigureAwait(false); }
                catch (Exception ex) { result.Faults.Add($"teardown: {ex.Message}"); }
            }

            // Gate: the controller must be reachable (250 acked) AND its identity obtained (113).
            result.Pass = result.Checks.Any(c => c.Name == "connect" && c.Pass) &&
                          result.Checks.Any(c => c.Name == "controller-version" && c.Pass);
            return result;
        }

        // ---------------------------------------------------------------------------------
        // hil-b — the single conditional Stage 1-5 init run through the REAL production runtime
        // ---------------------------------------------------------------------------------

        public static async Task<HilResult> RunHilBAsync(int[] domain, TimeSpan timeout)
        {
            var result = new HilResult("hil-b");
            var runtime = await StartRealInitAndVerifyAsync(domain, timeout, result).ConfigureAwait(false);
            if (runtime is not null)
            {
                // Best-effort bounded teardown (commands + observes neutral, never energizes).
                await ShutdownRuntimeAsync(runtime, result).ConfigureAwait(false);
            }

            return result;
        }

        // ---------------------------------------------------------------------------------
        // hil-c — single-session READY endurance (the authorized FALLBACK, NOT a repeated-init test)
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Runs the genuine Stage 1-5 init ONCE (via the existing hil-b composition), verifies the
        /// terminal state (Running + StoppedAtGlobalReady + track-power off + all amps READY), then
        /// HOLDS the session (heartbeat + lease + communication) and monitors for up to
        /// <paramref name="maxDuration"/>. This is an endurance test of the READY state + communication
        /// — explicitly NOT a repeated-init test. No REARM (112), no Stage 6/7, no controller reset,
        /// no watchdog bypass. Fails closed on the first unexpected deviation (no retry).
        /// </summary>
        public static async Task<HilResult> RunHilCAsync(
            int[] domain,
            TimeSpan initTimeout,
            TimeSpan pollInterval,
            TimeSpan maxDuration,
            string? jsonlPath,
            string? stopFilePath,
            CancellationToken operatorStopToken)
        {
            var result = new HilResult("hil-c");
            result.Check("endurance-mode",
                true,
                "single-session READY endurance (NOT a repeated-init test)");

            var runtime = await StartRealInitAndVerifyAsync(domain, initTimeout, result).ConfigureAwait(false);
            if (runtime is null || !result.Pass)
            {
                if (runtime is not null)
                {
                    result.Faults.Add(
                        $"hold not started: init verification failed (state={result.RuntimeState}, runStatus={result.RunStatus}).");
                    await ShutdownRuntimeAsync(runtime, result).ConfigureAwait(false);
                }

                return result;
            }

            // Hold phase: the runtime is Running + StoppedAtGlobalReady; it MUST NOT be disposed yet.
            var domainBytes = TrackAmplifierGroups.Create(domain, null, null)
                .AllConfigured.Select(a => (byte)a).ToArray();

            IReadyEnduranceSink sink = jsonlPath is not null
                ? new JsonlReadyEnduranceSink(jsonlPath)
                : new ConsoleReadyEnduranceSink();

            // 8 h hard wall-clock bound (CancelAfter) + operator STOP (token + stop file) linked into
            // one hold token. The monitor's internal max-duration check (via its clock) reports
            // DurationBoundReached; a cancellation before that reports OperatorStop.
            using var holdCts = CancellationTokenSource.CreateLinkedTokenSource(operatorStopToken);
            holdCts.CancelAfter(maxDuration);

            Task? stopFileTask = stopFilePath is not null
                ? WatchStopFileAsync(stopFilePath, holdCts)
                : null;

            var monitor = new ReadyEnduranceMonitor(
                runtime,
                domainBytes,
                sink,
                pollInterval: pollInterval,
                maxDuration: maxDuration,
                heartbeatActive: () => runtime.HeartbeatActive);

            ReadyEnduranceSummary summary;
            try
            {
                summary = await monitor.RunAsync(holdCts.Token).ConfigureAwait(false);
                result.AddTimeline($"ENDURANCE {summary.Disposition} polls={summary.PollCount} duration={summary.Duration}");
            }
            finally
            {
                if (stopFileTask is not null)
                {
                    // Stop the file watcher now that the hold has ended.
                    holdCts.Cancel();
                    try { await stopFileTask.ConfigureAwait(false); }
                    catch (OperationCanceledException) { /* watcher stopped */ }
                }
            }

            // Final shutdown: always dispose the runtime (never energize) and record the result.
            summary.FinalShutdownResult = await ShutdownRuntimeAsync(runtime, result).ConfigureAwait(false);
            sink.WriteSummary(summary);

            result.Check("hold-healthy",
                summary.Healthy,
                summary.Healthy ? "no deviation observed" : summary.FirstFailureReason ?? "deviation");
            if (!summary.Healthy)
            {
                result.Faults.Add($"READY endurance failed closed: {summary.FirstFailureReason}");
            }

            result.Pass = summary.Healthy;
            return result;
        }

        // ---------------------------------------------------------------------------------
        // hil-d — REARM + repeated Stage 1-5 init (the authorized campaign loop)
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// The REARM + repeated-init campaign runner. Per cycle it: (a) reads the 111 global-stop
        /// state (bounded); (b) classifies it — a LATCHED-HOST-LOSS latch (1) authorizes a single
        /// REARM (112), while a host-initiated/EMO latch (2) or an unknown/unexpected/unreachable
        /// state STOPS (never re-arms); (c) runs the real Stage 1-5 init via
        /// <see cref="StartRealInitAndVerifyAsync"/>; (d) verifies every domain amplifier is READY
        /// and records per-amp HR8/HR9/HR22; (e) performs a controlled Stop (DisposeAsync); (f) writes
        /// one JSONL line per cycle. It is bounded per cycle (<paramref name="initTimeout"/>) and
        /// overall (<paramref name="maxDuration"/>), honours an operator STOP file and Ctrl+C, and
        /// fails closed on the first unexpected deviation (no retry). No Stage 6/7, no track
        /// energization, no firmware flash.
        /// </summary>
        public static async Task<HilResult> RunHilDAsync(
            int[] domain,
            TimeSpan initTimeout,
            TimeSpan maxDuration,
            string? jsonlPath,
            string? stopFilePath,
            CancellationToken operatorStopToken)
        {
            var campaign = new HilResult("hil-d");
            campaign.Check("campaign-mode", true, "REARM + repeated Stage 1-5 init (no Stage 6/7, no flash)");

            var startedAt = DateTimeOffset.UtcNow;
            using var campaignCts = CancellationTokenSource.CreateLinkedTokenSource(operatorStopToken);
            campaignCts.CancelAfter(maxDuration);

            var stopFileTask = stopFilePath is not null
                ? WatchStopFileAsync(stopFilePath, campaignCts)
                : null;

            int cyclesRun = 0;
            string? firstFailure = null;
            string disposition = "DurationBoundReached";

            try
            {
                while (true)
                {
                    // Overall budget / operator STOP gate (checked before every cycle).
                    if (operatorStopToken.IsCancellationRequested)
                    {
                        disposition = "OperatorStop";
                        break;
                    }

                    if (DateTimeOffset.UtcNow - startedAt >= maxDuration)
                    {
                        disposition = "DurationBoundReached";
                        break;
                    }

                    cyclesRun++;

                    var result = new HilResult("hil-d") { Cycle = cyclesRun };

                    // (a) read the 111 global-stop state (bounded).
                    ushort? state;
                    try
                    {
                        state = await ProbeGlobalStopStateAsync(initTimeout, campaignCts.Token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        disposition = operatorStopToken.IsCancellationRequested
                            ? "OperatorStop"
                            : "DurationBoundReached";
                        break;
                    }

                    result.GlobalStopState = state.HasValue ? DescribeGlobalStop(state.Value) : "unknown";
                    var action = GlobalStopRecoveryDecision.Decide(state);
                    result.RecoveryAction = action switch
                    {
                        GlobalStopRecoveryAction.ProceedNoRearm => "none (not latched)",
                        GlobalStopRecoveryAction.Rearm => "REARM (112) once",
                        _ => "STOP (fail-closed, no REARM)",
                    };

                    // (b) host-initiated / EMO / unexpected / unreachable -> STOP, never re-arm.
                    if (action == GlobalStopRecoveryAction.StopFailClosed)
                    {
                        var why = state.HasValue
                            ? $"global-stop state {state.Value} ({result.GlobalStopState}) is not an expected host-loss latch"
                            : "controller unreachable (no 111 reply)";
                        result.FirstFailure = why;
                        result.Faults.Add(why);
                        result.Pass = false;
                        firstFailure ??= $"cycle {cyclesRun}: {why}";
                        AppendCampaignJsonl(jsonlPath, result);
                        disposition = "FirstFailure";
                        break;
                    }

                    // (c) run the real Stage 1-5 init; REARM only for a proven host-loss latch.
                    bool authorizeRecovery = action == GlobalStopRecoveryAction.Rearm;
                    var runtime = await StartRealInitAndVerifyAsync(
                        domain, initTimeout, result, authorizeRecovery).ConfigureAwait(false);

                    if (runtime is not null)
                    {
                        // (e) controlled Stop (commands + observes neutral, never energizes).
                        await ShutdownRuntimeAsync(runtime, result).ConfigureAwait(false);
                    }

                    // (d) verify all amps READY + record per-amp HR8/HR9/HR22 (already in result.Amps).
                    DeriveFirstFailure(result);

                    if (!result.Pass)
                    {
                        firstFailure ??= $"cycle {cyclesRun}: {result.FirstFailure ?? "init verification failed"}";
                    }

                    AppendCampaignJsonl(jsonlPath, result);

                    // Fail closed on the first unexpected deviation (no retry).
                    if (!result.Pass)
                    {
                        disposition = "FirstFailure";
                        break;
                    }
                }
            }
            finally
            {
                if (stopFileTask is not null)
                {
                    campaignCts.Cancel();
                    try { await stopFileTask.ConfigureAwait(false); }
                    catch (OperationCanceledException) { /* watcher stopped */ }
                }
            }

            campaign.Cycle = cyclesRun;
            campaign.Pass = firstFailure is null;
            campaign.FirstFailure = firstFailure;
            campaign.RecoveryAction = disposition;
            campaign.AddTimeline(
                $"CAMPAIGN {disposition} cycles={cyclesRun} duration={DateTimeOffset.UtcNow - startedAt}");

            if (firstFailure is not null)
            {
                campaign.Faults.Add($"campaign failed closed: {firstFailure}");
            }

            AppendCampaignJsonl(jsonlPath, campaign);
            return campaign;
        }

        /// <summary>Sets <see cref="HilResult.FirstFailure"/> from the first failing check or fault.</summary>
        private static void DeriveFirstFailure(HilResult result)
        {
            if (result.FirstFailure is not null)
            {
                return;
            }

            var failedCheck = result.Checks.FirstOrDefault(c => !c.Pass);
            if (failedCheck is not null)
            {
                result.FirstFailure = $"{failedCheck.Name}: {failedCheck.Detail}";
            }
            else if (result.Faults.Count > 0)
            {
                result.FirstFailure = result.Faults[0];
            }
        }

        /// <summary>
        /// Bounded, read-only read of the PIC32 <c>GLOBAL_STOP_STATE</c> (111). Establishes a session
        /// (250 CONNECT), issues one 111 query and returns the raw state (0/1/2/3), or null when the
        /// controller is unreachable or the reply does not arrive. No write, no re-arm, no slave
        /// register access. Disposes its own transport/comm before returning so the runtime can
        /// acquire the controller-session lease afterwards.
        /// </summary>
        private static async Task<ushort?> ProbeGlobalStopStateAsync(TimeSpan timeout, CancellationToken ct)
        {
            var variables = new TrackApplicationVariables();
            TrackCommClientAsync comm;
            try
            {
                var transport = new RawUdpTrackTransport(new RawUdpTransport(
                    CoreConfiguration.TrackControllerIpAddress,
                    CoreConfiguration.TrackControllerSendingPort,
                    CoreConfiguration.TrackControllerReceivingPort));
                comm = new TrackCommClientAsync(transport, variables);
            }
            catch
            {
                return null;
            }

            try
            {
                await comm.StartAsync(realHardwareMode: true, ct).ConfigureAwait(false);
                var probe = new BoundedCommProbe(comm);

                var connect = await probe.SendAndWaitAsync(
                    EnumClientCommands.CLIENT_CONNECTION_REQUEST,
                    SessionCommandData,
                    m => m.TaskId == TaskId.CONTROLLER && m.Taskcommand == TaskStates.CONNECTED && m.Taskstate == TaskStates.DONE,
                    timeout,
                    ct).ConfigureAwait(false);
                if (!connect.HasValue)
                {
                    return null;
                }

                var gs = await probe.SendAndWaitAsync(
                    TrackCommand.GLOBAL_STOP_STATE,
                    Array.Empty<byte>(),
                    m => m.TaskId == TaskId.CONTROLLER && m.Taskcommand == TrackCommand.GLOBAL_STOP_STATE && m.Taskstate == TaskStates.DONE,
                    timeout,
                    ct).ConfigureAwait(false);

                probe.Detach();
                return gs.HasValue ? gs.Value.Taskmessage : null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return null;
            }
            finally
            {
                try { await comm.DisposeAsync().ConfigureAwait(false); }
                catch { /* best-effort teardown */ }
            }
        }

        /// <summary>Appends one JSONL evidence line (one cycle or the campaign summary) to a file.</summary>
        private static void AppendCampaignJsonl(string? path, HilResult result)
        {
            if (path is null)
            {
                return;
            }

            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(
                fullPath,
                JsonSerializer.Serialize(result.ToJson()) + Environment.NewLine,
                Encoding.UTF8);
        }

        /// <summary>
        /// Composes the REAL production runtime host (transportFactory: null -> real UDP,
        /// TrackControlMode.Real), starts it ONCE, and verifies the expected terminal state
        /// (Stage 1-5 passed, StoppedAtGlobalReady, track power off, movement not granted, firmware
        /// not authorized, all domain amplifiers READY). The returned runtime is left RUNNING (not
        /// disposed): the caller owns the subsequent hold and shutdown. Returns null when the domain
        /// is empty (fail closed before any composition).
        ///
        /// <paramref name="authorizeRecovery"/>, when true, authorizes a global-stop re-arm (112)
        /// before the start. The caller (hil-d) sets this ONLY after a pre-read of 111 proved the
        /// latch reason is host-loss (state 1); it is never set for a host-initiated/unknown latch.
        /// </summary>
        private static async Task<TrackApplicationRuntimeHost?> StartRealInitAndVerifyAsync(
            int[] domain, TimeSpan timeout, HilResult result, bool authorizeRecovery = false)
        {
            // Real mode fails closed on an empty domain: never let a malformed --domain silently
            // become an empty safety domain.
            var groups = TrackAmplifierGroups.Create(domain, null, null);
            result.Check("domain-configured",
                groups.AllConfigured.Count > 0,
                $"domain={string.Join(",", groups.AllConfigured)}");
            if (groups.AllConfigured.Count == 0)
            {
                result.Faults.Add("Empty amplifier domain; Real mode fails closed. Refusing to start.");
                result.Pass = false;
                return null;
            }

            // Same production composition the WPF application uses, except transportFactory: null
            // (-> CreateRealTransport() real UDP) and TrackControlMode.Real. The controller-session
            // lease is acquired INSIDE StartAsync(Real); this harness never pre-acquires it.
            var runtime = new TrackApplicationRuntimeHost(
                new NullEcosHostService(),
                controlTrace: null,
                transportFactory: null,
                amplifierGroups: groups);

            if (authorizeRecovery)
            {
                // Campaign REARM: only for a proven host-loss latch (the caller's 111 pre-read).
                // The physical-stop-clear evidence source is the campaign's operator confirmation;
                // the precondition step still re-confirms 111 before emitting 112 and re-checks the
                // latch is still latched. Movement permission stays NotGranted regardless.
                runtime.AuthorizeGlobalStopRecovery(() => true);
                result.AddTimeline("RECOVERY authorized (REARM 112) for host-loss latch.");
            }

            // Capture per-amp readbacks and per-stage statuses from events so a FAILED start (whose
            // cleanup nulls the runtime's track-part fields) still leaves observable evidence. The
            // TrackAmplifierItem references captured here are the SAME mutable container objects the
            // comm client writes, so their final field values survive the host's field teardown.
            var ampSnapshot = new TrackAmplifierItem?[TrackAmplifierAddress.MaxTrackAmplifier + 1];
            var stageSnapshot = new Dictionary<TrackInitStage, TrackInitStageStatus>();

            runtime.StateChanged += (_, e) => result.AddTimeline(
                $"STATE {e.State}" + (e.FailureReason is null ? string.Empty : $" ({e.FailureReason})"));
            runtime.InitStageStatusChanged += (_, e) =>
            {
                stageSnapshot[e.Status.Stage] = e.Status;
                result.AddTimeline(
                    $"STAGE {e.Status.Stage} -> {e.Status.State}" +
                    (e.Status.FailureReason is null ? string.Empty : $" ({e.Status.FailureReason})"));
            };
            runtime.AmplifierDataReceived += (_, e) =>
            {
                if (e.Amplifier.SlaveNumber < ampSnapshot.Length)
                {
                    ampSnapshot[e.Amplifier.SlaveNumber] = e.Amplifier;
                }
            };
            runtime.Faulted += (_, e) =>
            {
                result.Faults.Add($"{e.Source}: {e.Exception.Message}");
                result.AddTimeline($"FAULT {e.Source}: {e.Exception.Message}");
            };

            using var cts = new CancellationTokenSource(timeout);
            try
            {
                await runtime.StartAsync(TrackControlMode.Real, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                result.Faults.Add("StartAsync cancelled (timeout).");
            }
            catch (Exception ex)
            {
                result.Faults.Add($"StartAsync threw: {ex.Message}");
            }

            // Terminal observations via the public ITrackApplicationRuntime surface. RunStatus /
            // MovementPermission / TrackPowerOn / FirmwareDeploymentAuthorized have safe fallbacks
            // when the start failed and the host nulled the state machine; stages/amps are merged
            // from the event snapshots below so they stay observable even after failed-start teardown.
            result.RuntimeState = runtime.State.ToString();
            result.RunStatus = runtime.RunStatus.ToString();
            result.MovementPermission = runtime.MovementPermission.ToString();
            result.TrackPowerOn = runtime.TrackPowerOn;
            result.FirmwareDeploymentAuthorized = runtime.FirmwareDeploymentAuthorized;

            TrackInitStageState StageOf(TrackInitStage stage)
            {
                var live = runtime.InitStages.FirstOrDefault(s => s.Stage == stage);
                if (live is not null)
                {
                    return live.State;
                }

                return stageSnapshot.TryGetValue(stage, out var snap)
                    ? snap.State
                    : TrackInitStageState.Pending;
            }

            TrackAmplifierItem? AmpOf(int slave)
            {
                var live = runtime.TrackAmplifiers;
                if (live.Count > slave)
                {
                    return live[slave];
                }

                return slave < ampSnapshot.Length ? ampSnapshot[slave] : null;
            }

            result.Stages = TrackInitStageInfo.All.ToDictionary(s => s.ToString(), s => StageOf(s).ToString());

            // Assert the expected terminal state (offline scenario A).
            result.Check("runtime-running",
                runtime.State == TrackRuntimeState.Running,
                runtime.State.ToString());
            result.Check("run-status-stopped-at-global-ready",
                runtime.RunStatus == InitializationStatus.StoppedAtGlobalReady,
                runtime.RunStatus.ToString());

            foreach (var stage in new[]
            {
                TrackInitStage.ControllerConnection,
                TrackInitStage.FirmwareAddress,
                TrackInitStage.AmplifierStandby,
                TrackInitStage.ConfigureValidate,
                TrackInitStage.GlobalReady,
            })
            {
                var state = StageOf(stage);
                result.Check($"stage-{stage}-passed",
                    state == TrackInitStageState.Passed,
                    state.ToString());
            }

            var ampPowerEnable = StageOf(TrackInitStage.AmpPowerEnable);
            result.Check("stage-amp-power-enable-not-passed",
                ampPowerEnable != TrackInitStageState.Passed,
                ampPowerEnable.ToString());

            var normalOperation = StageOf(TrackInitStage.NormalOperation);
            result.Check("stage-normal-operation-not-passed",
                normalOperation != TrackInitStageState.Passed,
                normalOperation.ToString());

            result.Check("track-power-off", !runtime.TrackPowerOn, runtime.TrackPowerOn.ToString());
            result.Check("movement-not-granted",
                runtime.MovementPermission == MovementPermissionState.NotGranted,
                runtime.MovementPermission.ToString());
            result.Check("firmware-deployment-not-authorized",
                !runtime.FirmwareDeploymentAuthorized,
                runtime.FirmwareDeploymentAuthorized.ToString());

            foreach (var slave in groups.AllConfigured)
            {
                var amp = AmpOf((int)slave);
                if (amp is null)
                {
                    result.Amps.Add(new AmpRecord
                    {
                        Slave = slave,
                        SlaveDetected = 0,
                        Phase1Failure = "no amplifier data observed",
                    });
                    result.Check($"amp{slave}-detected", false, "no amplifier data observed");
                    result.Check($"amp{slave}-config-verified", false, "no amplifier data");
                    result.Check($"amp{slave}-phase1failure-null", false, "no amplifier data");
                    result.Check($"amp{slave}-hr7-f101", false, "no HR7 readback");
                    result.Check($"amp{slave}-hr11-0e39", false, "no HR11 readback");
                    result.Check($"amp{slave}-hr22-protocol-0x02", false, "no HR22 readback");
                    continue;
                }

                result.Amps.Add(BuildAmpRecord(amp));

                var hr7 = amp.HoldingReg[TrackAmplifierRegisters.Identity];
                var hr11 = amp.HoldingReg[TrackAmplifierRegisters.SwChecksum];
                var hr22 = amp.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol];
                var protocol = TrackAmplifierRegisters.FwProtocolVersion(hr22);

                result.Check($"amp{slave}-detected",
                    amp.SlaveDetected == 1,
                    $"SlaveDetected={amp.SlaveDetected}");
                result.Check($"amp{slave}-config-verified",
                    amp.ConfigurationVerified,
                    amp.ConfigurationVerified.ToString());
                result.Check($"amp{slave}-phase1failure-null",
                    amp.Phase1Failure is null,
                    amp.Phase1Failure ?? "<null>");
                result.Check($"amp{slave}-hr7-f101",
                    hr7 == ExpectedHr7,
                    $"HR7=0x{hr7:X4} (expected 0x{ExpectedHr7:X4})");
                result.Check($"amp{slave}-hr11-0e39",
                    hr11 == ExpectedHr11,
                    $"HR11=0x{hr11:X4} (expected 0x{ExpectedHr11:X4})");
                result.Check($"amp{slave}-hr22-protocol-0x02",
                    protocol == TrackAmplifierRegisters.TrackPowerProtocolVersion,
                    $"HR22=0x{hr22:X4} protocol={protocol} (expected {TrackAmplifierRegisters.TrackPowerProtocolVersion})");
            }

            result.Pass = result.Checks.Count > 0 && result.Checks.All(c => c.Pass);
            return runtime;
        }

        /// <summary>
        /// Best-effort bounded teardown (commands + observes neutral, never energizes). Returns a
        /// human-readable shutdown result string for the endurance summary.
        /// </summary>
        private static async Task<string> ShutdownRuntimeAsync(TrackApplicationRuntimeHost runtime, HilResult result)
        {
            try
            {
                await runtime.DisposeAsync().ConfigureAwait(false);
                return runtime.State == TrackRuntimeState.Stopped
                    ? "Stopped"
                    : $"runtime ended in state {runtime.State}";
            }
            catch (Exception ex)
            {
                result.Faults.Add($"dispose: {ex.Message}");
                return $"shutdown fault: {ex.Message}";
            }
        }

        /// <summary>
        /// Watches for an operator STOP file; when it appears, cancels the hold token (clean stop).
        /// </summary>
        private static async Task WatchStopFileAsync(string stopFilePath, CancellationTokenSource holdCts)
        {
            var fullPath = Path.GetFullPath(stopFilePath);
            try
            {
                while (!holdCts.IsCancellationRequested)
                {
                    if (File.Exists(fullPath))
                    {
                        holdCts.Cancel();
                        return;
                    }

                    await Task.Delay(500, holdCts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // The hold already ended; nothing to stop.
            }
        }

        /// <summary>Appends one JSONL evidence line per typed sample/summary to a file.</summary>
        private sealed class JsonlReadyEnduranceSink : IReadyEnduranceSink
        {
            private static readonly JsonSerializerOptions JsonOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            private readonly string _path;

            public JsonlReadyEnduranceSink(string path) => _path = Path.GetFullPath(path);

            public void WriteSample(ReadyEnduranceSample sample) => AppendLine(sample);

            public void WriteSummary(ReadyEnduranceSummary summary) => AppendLine(summary);

            private void AppendLine(object value)
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(
                    _path,
                    JsonSerializer.Serialize(value, JsonOptions) + Environment.NewLine,
                    Encoding.UTF8);
            }
        }

        /// <summary>Writes a compact human-readable summary to the console when no JSONL path is given.</summary>
        private sealed class ConsoleReadyEnduranceSink : IReadyEnduranceSink
        {
            public void WriteSample(ReadyEnduranceSample sample)
                => Console.WriteLine(
                    $"ENDURANCE POLL {sample.Timestamp:O} uptime={sample.Uptime} state={sample.State} " +
                    $"runStatus={sample.RunStatus} globalStop={sample.GlobalStopState} heartbeat={sample.HeartbeatActive} " +
                    $"trackPower={sample.TrackPowerOn} healthy={sample.Healthy} failure={(sample.Failure ?? "-")}");

            public void WriteSummary(ReadyEnduranceSummary summary)
                => Console.WriteLine(
                    $"ENDURANCE SUMMARY disposition={summary.Disposition} duration={summary.Duration} " +
                    $"polls={summary.PollCount} healthy={summary.Healthy} " +
                    $"firstFailure={(summary.FirstFailureReason ?? "-")} shutdown={(summary.FinalShutdownResult ?? "-")}");
        }

        // ---------------------------------------------------------------------------------
        // Shared helpers
        // ---------------------------------------------------------------------------------

        private static IEnumerable<TrackAmplifierItem> DetectedTrackAmplifiers(TrackApplicationVariables variables)
            => variables.trackAmpItems
                .Where(a => a.SlaveDetected != 0 && TrackAmplifierAddress.IsTrackAmplifierAddress(a.SlaveNumber));

        private static AmpRecord BuildAmpRecord(TrackAmplifierItem amp)
        {
            var regs = amp.HoldingReg ?? new ushort[TrackAmplifierRegisters.Count];
            var hr1 = regs.Length > TrackAmplifierRegisters.Control ? regs[TrackAmplifierRegisters.Control] : (ushort)0;
            var hr7 = regs.Length > TrackAmplifierRegisters.Identity ? regs[TrackAmplifierRegisters.Identity] : (ushort)0;
            var hr8 = regs.Length > TrackAmplifierRegisters.State ? regs[TrackAmplifierRegisters.State] : (ushort)0;
            var hr9 = regs.Length > TrackAmplifierRegisters.Fault ? regs[TrackAmplifierRegisters.Fault] : (ushort)0;
            var hr11 = regs.Length > TrackAmplifierRegisters.SwChecksum ? regs[TrackAmplifierRegisters.SwChecksum] : (ushort)0;
            var hr21 = regs.Length > TrackAmplifierRegisters.FwVersionMajorMinor ? regs[TrackAmplifierRegisters.FwVersionMajorMinor] : (ushort)0;
            var hr22 = regs.Length > TrackAmplifierRegisters.FwVersionPatchProtocol ? regs[TrackAmplifierRegisters.FwVersionPatchProtocol] : (ushort)0;

            return new AmpRecord
            {
                Slave = amp.SlaveNumber,
                SlaveDetected = amp.SlaveDetected,
                ConfigurationVerified = amp.ConfigurationVerified,
                Phase1Failure = amp.Phase1Failure,
                Hr7 = $"0x{hr7:X4}",
                Hr11 = $"0x{hr11:X4}",
                Hr1 = $"0x{hr1:X4}",
                Hr8 = $"0x{hr8:X4}",
                Hr9 = $"0x{hr9:X4}",
                Hr21 = $"0x{hr21:X4}",
                Hr22 = $"0x{hr22:X4}",
                Hr22Protocol = TrackAmplifierRegisters.FwProtocolVersion(hr22),
                Ready = (hr8 & 0xF) == TrackAmplifierRegisters.ReadyState,
            };
        }

        private static string DescribeGlobalStop(ushort state) => state switch
        {
            GlobalStopStatusMessage.Disarmed => "Disarmed (0)",
            GlobalStopStatusMessage.LatchedHostLoss => "LatchedHostLoss (1)",
            GlobalStopStatusMessage.LatchedHostInitiated => "LatchedHostInitiated (2)",
            GlobalStopStatusMessage.Armed => "Armed (3)",
            _ => $"Unknown ({state})",
        };
    }
}
