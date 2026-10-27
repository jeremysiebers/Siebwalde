using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Comm;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Per-amplifier structured result of the safe CONFIG/APPLY diagnostic.
    /// Software/protocol evidence only: "disabled", "inhibited" and "config verified" are the
    /// values the PIC18 <em>reports</em> in its holding registers — they are never physical proof
    /// that the H-bridge output is electrically inert.
    /// </summary>
    public sealed class AmplifierConfigApplyAmplifierResult
    {
        public int SlaveNumber { get; set; }

        /// <summary>A fresh genuine SLAVEINFO readback exists (not merely a DETECT latch).</summary>
        public bool Detected { get; set; }

        /// <summary>C# fail-closed compatibility gate (secondary to the PIC32 two-fresh-read authority).</summary>
        public bool Compatible { get; set; }
        public string? IncompatibilityReason { get; set; }

        /// <summary>HR7 raw identity value.</summary>
        public int IdentityRaw { get; set; }
        public string IdentityRawHex => "0x" + ((ushort)IdentityRaw).ToString("X4");

        /// <summary>HR22 PROTOCOL_VERSION low byte.</summary>
        public int ProtocolVersion { get; set; }

        /// <summary>HR7 identity shape + HR22 protocol shape both hold (C# secondary identity check).</summary>
        public bool IdentityVerified { get; set; }

        /// <summary>The DISABLE command (108 write HR1=0x0400) was transmitted without transport error.</summary>
        public bool DisableAccepted { get; set; }

        /// <summary>A fresh readback confirmed enable-active cleared AND output inhibited after DISABLE.</summary>
        public bool DisableVerified { get; set; }
        public int StateAfterDisable { get; set; }
        public bool EnableActiveAfterDisable { get; set; }
        public bool InhibitedAfterDisable { get; set; }

        /// <summary>AmplifierConfigurationDownloader.DownloadAsync returned true.</summary>
        public bool ConfigDownloaded { get; set; }

        /// <summary>item.ConfigurationVerified after download.</summary>
        public bool ConfigVerified { get; set; }

        /// <summary>HR8 INIT_DONE|CONFIG_OK (0x0600) set after APPLY.</summary>
        public bool InitDoneConfigOk { get; set; }
        public int StateAfterApply { get; set; }
        public int FaultRaw { get; set; }
        public bool Inhibited { get; set; }
        public int AppliedPwm { get; set; }
        public int PwmCommand { get; set; }

        /// <summary>Effective PWM3-enable state (HR8 EnableActiveBit).</summary>
        public bool Pwm3Enabled { get; set; }

        /// <summary>Brake state (HR8 BrakeActiveBit).</summary>
        public bool BrakeActive { get; set; }

        public string? FailureCommand { get; set; }
        public string? FailureResponse { get; set; }
        public string? Failure { get; set; }
    }

    /// <summary>Per-command init report for the apply-config session.</summary>
    public sealed record AmplifierConfigApplyCommandReport(
        int Command, string Name, long Transmitted, long Acknowledged, string State);

    /// <summary>Init summary (mirrors the guard counters; movement writes are always 0 by construction).</summary>
    public sealed class AmplifierConfigApplyInitReport
    {
        public IReadOnlyList<AmplifierConfigApplyCommandReport> Commands { get; set; } =
            Array.Empty<AmplifierConfigApplyCommandReport>();
        public int RejectedCommands { get; set; }
        public int OrderViolations { get; set; }
        public int AcknowledgementTimeouts { get; set; }
        public string Readiness { get; set; } = "INCOMPLETE";
    }

    /// <summary>Heartbeat + global-stop latch supervision report.</summary>
    public sealed class AmplifierConfigApplyHeartbeatReport
    {
        public bool Supervised { get; set; }
        public long HeartbeatSent { get; set; }
        public long GlobalStopSent { get; set; }
        public string GlobalStopState { get; set; } = "Disarmed";
        public string? GlobalStopReason { get; set; }
        public int? LastObservedPic32Status { get; set; }
        public bool Latched { get; set; }

        /// <summary>True once REARM_GLOBAL_STOP (112) was emitted on the raw comm in this run.</summary>
        public bool Rearmed { get; set; }
    }

    /// <summary>Top-level structured result of the apply-config session.</summary>
    public sealed class AmplifierConfigApplyResult
    {
        public string Status { get; set; } = "FAILED";
        public string? Failure { get; set; }
        public int? FailedCommand { get; set; }
        public string? FailedCommandName { get; set; }
        public AmplifierConfigApplyInitReport Init { get; set; } = new();
        public AmplifierConfigApplyHeartbeatReport Heartbeat { get; set; } = new();
        public IReadOnlyList<AmplifierConfigApplyAmplifierResult> Amplifiers { get; set; } =
            Array.Empty<AmplifierConfigApplyAmplifierResult>();
    }

    /// <summary>
    /// Reusable, grant-gated safe CONFIG/APPLY orchestration for the diagnostics host.
    /// <para>
    /// Reuses the production components without a second implementation:
    /// <see cref="TrackCommClientAsync"/> (transport + receive loop),
    /// <see cref="KoploperSessionCommandGuard"/>/<see cref="KoploperSessionCommandApplyAllowlist"/>
    /// (the seven-command init 250→107→100→106→101→104→105),
    /// <see cref="GlobalStopMonitor"/> (HOST_HEARTBEAT 110 supervision),
    /// <see cref="AmplifierConfigurationDownloader"/> (108 CONFIG/APPLY, echo-verified), and
    /// <see cref="AmplifierPhase1Safety"/>/<see cref="TrackAmplifierRegisters"/> (identity +
    /// compatibility gate). It never writes HR0 and never sends ENABLE (105 / HR1=0x8000) after the
    /// DISABLE.
    /// </para>
    /// <para>
    /// <b>Re-arm safety note:</b> before the 107 RESET the orchestrator emits
    /// <c>REARM_GLOBAL_STOP</c> (112) directly on the raw comm to clear a latched host-loss stop
    /// left by a prior diagnostic session. 112 only transitions the PIC32 latch LATCHED→ARMED
    /// (controller.c <c>CONTROLLERxHANDLExSAFETYxCOMMAND</c>); it does NOT energize the amplifier
    /// bridge, because the amplifier H-bridge remains inhibited by <c>config_ok=0</c> →
    /// <c>PWM3EN=0</c> (<c>AmpCore_Update</c>/<c>AmpHw_Enable</c> in
    /// <c>TrackAmplifier4.X/amp_core.c</c>), an independent output-inhibition barrier the re-arm
    /// does not touch.
    /// </para>
    /// <para>
    /// <b>Reset idempotence:</b> safety does NOT depend on the amplifier already being at
    /// <c>config_ok=0</c>. 107 RESET asserts <c>Slaves_Disable_On()</c> (RD1 HIGH,
    /// <c>mbus.c:377-386</c>, <c>system_config/Production/system_config.h:515</c>), power-cycling
    /// every amplifier so <c>AmpCore_Init</c> (<c>amp_core.c:12-27</c>) zeroes
    /// <c>config_ok</c>/<c>enable</c>/<c>inhibited</c> on every run. <c>config_ok</c> becomes 1 only
    /// via this harness's own CONFIG/APPLY (108), strictly after the DISABLE gate, so the bridge is
    /// never energized during init/config even after a prior successful CONFIG/APPLY, a retained
    /// ENABLE, or a partial prior success.
    /// </para>
    /// </summary>
    public sealed class AmplifierConfigApplyOrchestrator : IAsyncDisposable
    {
        public static readonly IReadOnlyList<byte> DefaultTargetSlaves = new byte[] { 1, 3, 4, 6 };
        public static readonly TimeSpan DefaultReadbackTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan DefaultDisableVerifyTimeout = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan DefaultRearmConfirmTimeout = TimeSpan.FromSeconds(2);

        private readonly ITrackCommClient _rawComm;
        private readonly TrackApplicationVariables _variables;
        private readonly KoploperSessionCommandGuard _guard;
        private readonly GlobalStopMonitor _heartbeat;
        private readonly AmplifierConfigurationDownloader _downloader;
        private readonly CountingCommClient _heartbeatComm;
        private readonly Func<DateTimeOffset> _clock;
        private readonly TimeSpan _heartbeatInterval;
        private readonly TimeSpan _readbackTimeout;
        private readonly TimeSpan _disableVerifyTimeout;
        private readonly TimeSpan _rearmConfirmTimeout;
        private bool _rearmed;

        private AmplifierConfigApplyOrchestrator(
            ITrackCommClient rawComm,
            TrackApplicationVariables variables,
            KoploperSessionCommandGuard guard,
            CountingCommClient heartbeatComm,
            GlobalStopMonitor heartbeat,
            AmplifierConfigurationDownloader downloader,
            Func<DateTimeOffset> clock,
            TimeSpan heartbeatInterval,
            TimeSpan readbackTimeout,
            TimeSpan disableVerifyTimeout,
            TimeSpan rearmConfirmTimeout)
        {
            _rawComm = rawComm;
            _variables = variables;
            _guard = guard;
            _heartbeatComm = heartbeatComm;
            _heartbeat = heartbeat;
            _downloader = downloader;
            _clock = clock;
            _heartbeatInterval = heartbeatInterval;
            _readbackTimeout = readbackTimeout;
            _disableVerifyTimeout = disableVerifyTimeout;
            _rearmConfirmTimeout = rearmConfirmTimeout;
        }

        /// <summary>
        /// Composes the orchestrator from a transport seam (or the real UDP transport when null).
        /// The guard, heartbeat monitor and config downloader all share one raw
        /// <see cref="TrackCommClientAsync"/>.
        /// </summary>
        public static AmplifierConfigApplyOrchestrator Create(
            Func<ITrackTransport>? transportFactory = null,
            Func<DateTimeOffset>? clock = null,
            TimeSpan? acknowledgementTimeout = null,
            TimeSpan? heartbeatInterval = null,
            TimeSpan? readbackTimeout = null,
            TimeSpan? disableVerifyTimeout = null,
            TimeSpan? rearmConfirmTimeout = null)
        {
            Func<DateTimeOffset> sharedClock = clock ?? (() => DateTimeOffset.UtcNow);

            var variables = new TrackApplicationVariables();

            ITrackTransport transport = transportFactory?.Invoke()
                ?? new RawUdpTrackTransport(new RawUdpTransport(
                    CoreConfiguration.TrackControllerIpAddress,
                    CoreConfiguration.TrackControllerSendingPort,
                    CoreConfiguration.TrackControllerReceivingPort));

            var commClient = new TrackCommClientAsync(transport, variables);
            var guard = new KoploperSessionCommandGuard(commClient, acknowledgementTimeout, sharedClock);

            // The heartbeat monitor + downloader talk to the RAW comm (bypassing the init guard's
            // six-command allowlist). The counting wrapper only observes heartbeat/stop sends.
            var heartbeatComm = new CountingCommClient(commClient);
            var heartbeat = new GlobalStopMonitor(heartbeatComm, variables);
            var downloader = new AmplifierConfigurationDownloader(commClient);

            return new AmplifierConfigApplyOrchestrator(
                commClient, variables, guard, heartbeatComm, heartbeat, downloader, sharedClock,
                heartbeatInterval ?? GlobalStopMonitor.HeartbeatInterval,
                readbackTimeout ?? DefaultReadbackTimeout,
                disableVerifyTimeout ?? DefaultDisableVerifyTimeout,
                rearmConfirmTimeout ?? DefaultRearmConfirmTimeout);
        }

        /// <summary>
        /// Establishes the session + heartbeat, verifies identity/compatibility per target
        /// amplifier, performs the DISABLE safety gate, runs CONFIG/APPLY for each compatible
        /// amplifier, and returns the structured result. Idempotent stop of the comm client on
        /// completion or failure; never disposes (the host owns disposal).
        /// </summary>
        public async Task<AmplifierConfigApplyResult> RunAsync(
            IReadOnlyList<byte>? targetSlaves = null,
            AmplifierConfiguration? configuration = null,
            CancellationToken ct = default)
        {
            IReadOnlyList<byte> slaves = targetSlaves is null || targetSlaves.Count == 0
                ? DefaultTargetSlaves
                : targetSlaves;
            AmplifierConfiguration config = configuration ?? new AmplifierConfiguration();

            using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Task? heartbeatTask = null;

            try
            {
                await _guard.StartAsync(realHardwareMode: true, ct).ConfigureAwait(false);

                // Apply-config init (250→107→100→106→101→104→105), each command ack-gated by the
                // guard. 107 (RESET) returns the amplifiers to 0xAA so the 104 INIT identity
                // verification (two fresh FC03 HR7 reads @ 0xAA) can succeed, and forces a
                // controller already in SERVICE_TASKS to re-ack 100 (SLAVES_ON).
                _guard.BeginApplyConfigSession();
                await SendSessionCommandAsync(
                    KoploperSessionCommandAllowlist.ClientConnectionRequest, ct).ConfigureAwait(false);

                // Heartbeat supervision starts ONLY after a successful 250 connect.
                _heartbeat.EstablishLink(_clock());
                heartbeatTask = Task.Run(() => HeartbeatLoopAsync(heartbeatCts.Token), CancellationToken.None);

                // Re-arm the PIC32 global-stop latch (112) on the raw comm BEFORE the 107 RESET.
                // A latched host-loss stop left by a prior diagnostic session forces the MBUS
                // handler into SERVICE_TASKS (mbus.c MBUS_Tasks), so the 107 DONE ack is never
                // sent and init times out. 112 only transitions LATCHED→ARMED; it never energizes
                // the bridge (see the re-arm safety note on the class).
                await RearmGlobalStopAsync(ct).ConfigureAwait(false);

                await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ExecMbusStateReset, ct).ConfigureAwait(false);
                await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ExecMbusStateSlavesOn, ct).ConfigureAwait(false);
                await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ExecMbusStateStartDataUpload, ct).ConfigureAwait(false);
                await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ExecMbusStateSlaveDetect, ct).ConfigureAwait(false);
                await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ExecMbusStateSlaveInit, ct).ConfigureAwait(false);
                await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ExecMbusStateSlaveEnable, ct).ConfigureAwait(false);

                // Wait for a fresh genuine SLAVEINFO readback per target slave (bounded).
                await WaitForReadbackAsync(slaves, ct).ConfigureAwait(false);

                var amplifierResults = new List<AmplifierConfigApplyAmplifierResult>();
                foreach (byte slave in slaves)
                {
                    amplifierResults.Add(await ApplyConfigOneAsync(slave, config, ct).ConfigureAwait(false));
                }

                string readiness = BuildInitReport(null).Readiness;
                int compatibleCount = amplifierResults.Count(r => r.Compatible);
                int configuredCount = amplifierResults.Count(r => r.Compatible && r.ConfigVerified && r.DisableVerified);
                string status = readiness == "READY" && compatibleCount > 0 && configuredCount == compatibleCount
                    ? "PASS"
                    : "FAILED";

                return BuildResult(
                    status,
                    status == "FAILED" ? FailureSummary(amplifierResults, compatibleCount) : null,
                    null, null,
                    BuildInitReport(readiness), CaptureHeartbeat(),
                    amplifierResults);
            }
            catch (KoploperSessionCommandAcknowledgementTimeoutException ex)
            {
                return BuildResult(
                    "FAILED", "init_ack_timeout", ex.Command,
                    KoploperSessionCommandAllowlist.GetDisplayName(ex.Command),
                    BuildInitReport("FAILED"), CaptureHeartbeat(),
                    Array.Empty<AmplifierConfigApplyAmplifierResult>());
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return BuildResult(
                    "FAILED", "init_failed: " + ex.Message, null, null,
                    BuildInitReport("FAILED"), CaptureHeartbeat(),
                    Array.Empty<AmplifierConfigApplyAmplifierResult>());
            }
            finally
            {
                // Heartbeat supervision stops on every exit path (success, init failure, throw).
                heartbeatCts.Cancel();
                if (heartbeatTask is not null)
                {
                    try
                    {
                        await heartbeatTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // expected shutdown
                    }
                }
            }
        }

        private static string? FailureSummary(
            IReadOnlyList<AmplifierConfigApplyAmplifierResult> results, int compatibleCount)
        {
            if (compatibleCount == 0)
            {
                return "no_compatible_amplifiers";
            }

            var failures = results.Where(r => r.Compatible && r.Failure is not null).ToList();
            return failures.Count == 0 ? null : string.Join("; ", failures.Select(f => $"amp {f.SlaveNumber}: {f.Failure}"));
        }

        // ------------------------------------------------------------------ per-amplifier

        private async Task<AmplifierConfigApplyAmplifierResult> ApplyConfigOneAsync(
            byte slave, AmplifierConfiguration config, CancellationToken ct)
        {
            TrackAmplifierItem item = _variables.trackAmpItems[slave];

            bool detected = TrackAmplifierDataFreshness.HasFreshProtocolReadback(item);
            bool compatible = AmplifierPhase1Safety.Compatible(item);
            string? incompatReason = compatible ? null : AmplifierPhase1Safety.IncompatibilityReason(item);

            ushort[] regs = item.HoldingReg;
            int identityRaw = regs[TrackAmplifierRegisters.Identity];
            int protocolVersion = TrackAmplifierRegisters.FwProtocolVersion(
                regs[TrackAmplifierRegisters.FwVersionPatchProtocol]);
            bool identityVerified =
                TrackAmplifierRegisters.HasCompatibleIdentityFields((ushort)identityRaw) &&
                TrackAmplifierRegisters.HasCompatibleProtocolVersion(
                    regs[TrackAmplifierRegisters.FwVersionPatchProtocol]);

            var result = new AmplifierConfigApplyAmplifierResult
            {
                SlaveNumber = slave,
                Detected = detected,
                Compatible = compatible,
                IncompatibilityReason = incompatReason,
                IdentityRaw = identityRaw,
                ProtocolVersion = protocolVersion,
                IdentityVerified = identityVerified,
            };

            if (!compatible)
            {
                FillState(result, item);
                result.Failure = incompatReason ?? AmplifierPhase1Safety.IncompatibleFirmware;
                result.FailureCommand = "EXEC_MBUS_SLAVE_DATA_EXCH(DISABLE)";
                result.FailureResponse = "compatibility gate rejected (no write attempted)";
                return result;
            }

            // --- DISABLE safety gate: HR1=0x0400, then verify enable=0 + inhibited BEFORE APPLY.
            long sequence = item.ProtocolReadbackSequence;
            bool disableAccepted = true;
            string? disableFailure = null;
            string? disableFailureResponse = null;
            try
            {
                await _rawComm.SendAsync(AmplifierConfigurationDownloader.Exchange(
                    (ushort)slave, (byte)TrackAmplifierRegisters.Control,
                    TrackAmplifierRegisters.DisableBit, read: false), ct).ConfigureAwait(false);
                // Force a fresh readback (FC03) so HR8 reflects the post-DISABLE state.
                await _rawComm.SendAsync(AmplifierConfigurationDownloader.Exchange(
                    (ushort)slave, (byte)TrackAmplifierRegisters.State, 0, read: true), ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                disableAccepted = false;
                disableFailure = "disable write failed: " + ex.Message;
            }

            bool disableVerified = false;
            int stateAfterDisable = regs[TrackAmplifierRegisters.State];
            if (disableAccepted)
            {
                disableVerified = await WaitForSequenceAsync(item, sequence, _disableVerifyTimeout, ct)
                    .ConfigureAwait(false);
                if (disableVerified)
                {
                    stateAfterDisable = regs[TrackAmplifierRegisters.State];
                    bool enableActive = (stateAfterDisable & TrackAmplifierRegisters.EnableActiveBit) != 0;
                    bool inhibited = (stateAfterDisable & TrackAmplifierRegisters.InhibitedBit) != 0;
                    disableVerified = !enableActive && inhibited;
                    if (!disableVerified)
                    {
                        disableFailure = "DISABLE not verified: enable still active or output not inhibited";
                        disableFailureResponse =
                            $"HR8=0x{stateAfterDisable:X4} enableActive={enableActive} inhibited={inhibited}";
                    }
                }
                else
                {
                    disableFailure = "DISABLE verification timed out (no fresh readback)";
                }
            }

            bool enableActiveAfterDisable =
                (stateAfterDisable & TrackAmplifierRegisters.EnableActiveBit) != 0;
            bool inhibitedAfterDisable =
                (stateAfterDisable & TrackAmplifierRegisters.InhibitedBit) != 0;

            result.DisableAccepted = disableAccepted;
            result.DisableVerified = disableVerified;
            result.StateAfterDisable = stateAfterDisable;
            result.EnableActiveAfterDisable = enableActiveAfterDisable;
            result.InhibitedAfterDisable = inhibitedAfterDisable;

            if (!disableVerified)
            {
                FillState(result, item);
                result.Failure = disableFailure;
                result.FailureCommand = "EXEC_MBUS_SLAVE_DATA_EXCH(DISABLE)";
                result.FailureResponse = disableFailureResponse;
                return result;
            }

            // --- CONFIG + APPLY. DownloadAsync writes HR3..6, HR1=0, HR1=0x2000 (APPLY) only; it
            // never writes HR0 (PWM) and never writes ENABLE (0x8000). The DISABLE above guarantees
            // enable-active stays cleared, so APPLY can never turn a retained-enable bridge on.
            bool downloaded = await _downloader.DownloadAsync(item, config, ct).ConfigureAwait(false);

            FillState(result, item);
            result.ConfigDownloaded = downloaded;
            result.ConfigVerified = item.ConfigurationVerified;
            result.InitDoneConfigOk =
                (regs[TrackAmplifierRegisters.State] &
                 (TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit)) ==
                (TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit);
            if (!downloaded)
            {
                result.Failure = item.Phase1Failure ?? "configuration download failed";
                result.FailureCommand = "EXEC_MBUS_SLAVE_DATA_EXCH(CONFIG/APPLY)";
                result.FailureResponse = $"HR15=0x{regs[TrackAmplifierRegisters.WriteResult]:X4}";
            }

            return result;
        }

        private static void FillState(AmplifierConfigApplyAmplifierResult result, TrackAmplifierItem item)
        {
            ushort[] regs = item.HoldingReg;
            int state = regs[TrackAmplifierRegisters.State];
            result.StateAfterApply = state;
            result.FaultRaw = regs[TrackAmplifierRegisters.Fault];
            result.Inhibited = (state & TrackAmplifierRegisters.InhibitedBit) != 0;
            result.AppliedPwm = regs[TrackAmplifierRegisters.AppliedPwm] & TrackAmplifierRegisters.PwmMask;
            result.PwmCommand = regs[TrackAmplifierRegisters.PwmCommand] & TrackAmplifierRegisters.PwmMask;
            result.Pwm3Enabled = (state & TrackAmplifierRegisters.EnableActiveBit) != 0;
            result.BrakeActive = (state & TrackAmplifierRegisters.BrakeActiveBit) != 0;
            result.InitDoneConfigOk =
                (state & (TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit)) ==
                (TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit);
        }

        // ------------------------------------------------------------------ helpers

        private Task SendSessionCommandAsync(byte command, CancellationToken ct)
            => _guard.SendAsync(new SendMessage(command, new byte[80]), ct);

        /// <summary>
        /// Emits REARM_GLOBAL_STOP (112) directly on the raw comm (never through the guard, which
        /// does not know 112 and would reject it), then waits a bounded, best-effort time for the
        /// PIC32 to report non-latched via its GLOBAL_STOP_STATE (111) status (0 Disarmed or
        /// 3 Armed). Re-arm is idempotent and the 107 ack is the ultimate verification, so the
        /// session is never blocked on this confirmation.
        /// </summary>
        private async Task RearmGlobalStopAsync(CancellationToken ct)
        {
            await _rawComm.SendAsync(
                new SendMessage(TrackCommand.REARM_GLOBAL_STOP, Array.Empty<byte>()), ct)
                .ConfigureAwait(false);
            _rearmed = true;

            DateTimeOffset deadline = _clock() + _rearmConfirmTimeout;
            while (_clock() < deadline && !ct.IsCancellationRequested)
            {
                byte? status = _heartbeat.LastObservedPic32Status;
                if (status == GlobalStopStatusMessage.Disarmed ||
                    status == GlobalStopStatusMessage.Armed)
                {
                    break;
                }

                await Task.Delay(20, ct).ConfigureAwait(false);
            }
        }

        private async Task HeartbeatLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await _heartbeat.TickAsync(_clock(), ct).ConfigureAwait(false);
                    await Task.Delay(_heartbeatInterval, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
        }

        private async Task WaitForReadbackAsync(IReadOnlyList<byte> slaves, CancellationToken ct)
        {
            DateTimeOffset deadline = _clock() + _readbackTimeout;
            while (_clock() < deadline && !ct.IsCancellationRequested)
            {
                bool allFresh = slaves.All(slave =>
                    TrackAmplifierDataFreshness.HasFreshProtocolReadback(_variables.trackAmpItems[slave]));
                if (allFresh)
                {
                    return;
                }

                await Task.Delay(50, ct).ConfigureAwait(false);
            }
        }

        private static async Task<bool> WaitForSequenceAsync(
            TrackAmplifierItem item, long sequence, TimeSpan timeout, CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            try
            {
                while (item.ProtocolReadbackSequence <= sequence)
                {
                    await Task.Delay(10, cts.Token).ConfigureAwait(false);
                }

                return true;
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                return false; // verification timeout
            }
        }

        private AmplifierConfigApplyInitReport BuildInitReport(string? overrideReadiness)
        {
            KoploperSessionCommandCounters counters = _guard.GetCounters();
            var commands = new[]
            {
                BuildCommandReport(250, "CLIENT_CONNECTION_REQUEST",
                    counters.ClientConnectionRequestTransmitted, counters.ClientConnectionRequestAcknowledged),
                BuildCommandReport(107, "EXEC_MBUS_STATE_RESET",
                    counters.ExecMbusStateResetTransmitted, counters.ExecMbusStateResetAcknowledged),
                BuildCommandReport(100, "EXEC_MBUS_STATE_SLAVES_ON",
                    counters.ExecMbusStateSlavesOnTransmitted, counters.ExecMbusStateSlavesOnAcknowledged),
                BuildCommandReport(106, "EXEC_MBUS_STATE_START_DATA_UPLOAD",
                    counters.ExecMbusStateStartDataUploadTransmitted, counters.ExecMbusStateStartDataUploadAcknowledged),
                BuildCommandReport(101, "EXEC_MBUS_STATE_SLAVE_DETECT",
                    counters.ExecMbusStateSlaveDetectTransmitted, counters.ExecMbusStateSlaveDetectAcknowledged),
                BuildCommandReport(104, "EXEC_MBUS_STATE_SLAVE_INIT",
                    counters.ExecMbusStateSlaveInitTransmitted, counters.ExecMbusStateSlaveInitAcknowledged),
                BuildCommandReport(105, "EXEC_MBUS_STATE_SLAVE_ENABLE",
                    counters.ExecMbusStateSlaveEnableTransmitted, counters.ExecMbusStateSlaveEnableAcknowledged),
            };

            string readiness = overrideReadiness ??
                (counters.AcknowledgementTimeouts > 0
                    ? "FAILED"
                    : (counters.TotalAcknowledged == 7 ? "READY" : "INCOMPLETE"));

            return new AmplifierConfigApplyInitReport
            {
                Commands = commands,
                RejectedCommands = (int)counters.RejectedCommands,
                OrderViolations = (int)counters.OrderViolations,
                AcknowledgementTimeouts = (int)counters.AcknowledgementTimeouts,
                Readiness = readiness,
            };
        }

        private static AmplifierConfigApplyCommandReport BuildCommandReport(
            int command, string name, long transmitted, long acknowledged)
            => new(command, name, transmitted, acknowledged, acknowledged > 0 ? "DONE" : "PENDING");

        private AmplifierConfigApplyHeartbeatReport CaptureHeartbeat()
        {
            GlobalStopState state = _variables.GlobalStop.State;
            return new AmplifierConfigApplyHeartbeatReport
            {
                Supervised = true,
                HeartbeatSent = _heartbeatComm.HeartbeatSent,
                GlobalStopSent = _heartbeatComm.GlobalStopSent,
                GlobalStopState = state.ToString(),
                GlobalStopReason = _variables.GlobalStop.Reason?.ToString(),
                LastObservedPic32Status = _heartbeat.LastObservedPic32Status,
                Latched = _variables.GlobalStop.IsLatched,
                Rearmed = _rearmed,
            };
        }

        private static AmplifierConfigApplyResult BuildResult(
            string status,
            string? failure,
            int? failedCommand,
            string? failedCommandName,
            AmplifierConfigApplyInitReport init,
            AmplifierConfigApplyHeartbeatReport heartbeat,
            IReadOnlyList<AmplifierConfigApplyAmplifierResult> amplifiers)
            => new()
            {
                Status = status,
                Failure = failure,
                FailedCommand = failedCommand,
                FailedCommandName = failedCommandName,
                Init = init,
                Heartbeat = heartbeat,
                Amplifiers = amplifiers,
            };

        /// <summary>
        /// Stops the comm client (idempotent). Does not dispose; <see cref="DisposeAsync"/> owns
        /// disposal of the shared comm client + transport.
        /// </summary>
        public async Task StopAsync(CancellationToken ct = default)
        {
            try
            {
                await _guard.StopAsync(ct).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // best-effort idempotent stop
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _guard.StopAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignore stop errors during disposal
            }

            _heartbeat.Dispose();
            await _guard.DisposeAsync().ConfigureAwait(false);
        }

        /// <summary>Thin observable wrapper over the raw comm that counts heartbeat/stop sends only.
        /// It never owns the inner client (no disposal).</summary>
        private sealed class CountingCommClient : ITrackCommClient
        {
            private readonly ITrackCommClient _inner;
            public long HeartbeatSent { get; private set; }
            public long GlobalStopSent { get; private set; }

            public CountingCommClient(ITrackCommClient inner) => _inner = inner;

            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived
            {
                add => _inner.AmplifierDataReceived += value;
                remove => _inner.AmplifierDataReceived -= value;
            }

            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived
            {
                add => _inner.ControlMessageReceived += value;
                remove => _inner.ControlMessageReceived -= value;
            }

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
                => _inner.StartAsync(realHardwareMode, cancellationToken);

            public Task StopAsync(CancellationToken cancellationToken = default)
                => _inner.StopAsync(cancellationToken);

            public async Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
            {
                if (message.Command == TrackCommand.HOST_HEARTBEAT)
                {
                    HeartbeatSent++;
                }
                else if (message.Command == TrackCommand.EXEC_GLOBAL_STOP)
                {
                    GlobalStopSent++;
                }

                await _inner.SendAsync(message, cancellationToken).ConfigureAwait(false);
            }

            public ValueTask DisposeAsync() => default; // does not own the inner client
        }
    }
}
