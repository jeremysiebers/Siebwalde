using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// How a single-session READY-endurance hold ended. This is the authorized FALLBACK to the
    /// blocked repeated-init test — it is explicitly NOT a repeated-init test and must be labelled
    /// as such everywhere.
    /// </summary>
    public enum ReadyEnduranceDisposition
    {
        /// <summary>The hold ran the full configured duration with no deviation.</summary>
        DurationBoundReached,

        /// <summary>The operator STOP signal (cancellation) ended the hold cleanly with no deviation.</summary>
        OperatorStop,

        /// <summary>The first unexpected deviation ended the hold fail-closed (no retry).</summary>
        FirstFailure
    }

    /// <summary>Per-amplifier readback snapshot for one endurance poll.</summary>
    public sealed class ReadyEnduranceAmp
    {
        /// <summary>ModBus slave address.</summary>
        public byte Slave { get; init; }

        /// <summary>Whether the amplifier was detected by the controller.</summary>
        public bool Detected { get; init; }

        /// <summary>Whether Phase-1 config download + APPLY + readback verification completed.</summary>
        public bool ConfigurationVerified { get; init; }

        /// <summary>Phase-1 identity/protocol failure reason, or null when compatible.</summary>
        public string? Phase1Failure { get; init; }

        /// <summary>HR_STATE (HR8).</summary>
        public ushort Hr8 { get; init; }

        /// <summary>HR_FAULT (HR9).</summary>
        public ushort Hr9 { get; init; }

        /// <summary>HR firmware patch/protocol (HR22).</summary>
        public ushort Hr22 { get; init; }

        /// <summary>HR_STATE low nibble (READY == 1).</summary>
        public int Hr8LowNibble { get; init; }

        /// <summary>True when HR_STATE low nibble reports READY (group-ready, no movement).</summary>
        public bool Ready { get; init; }

        /// <summary>True when HR_FAULT is non-zero (any amplifier fault).</summary>
        public bool Fault => Hr9 != 0;

        /// <summary>
        /// Data/lease freshness: a fresh, genuine protocol SLAVEINFO readback — the observable proxy
        /// that the PIC32 cyclic motion-lease renewal / comm is alive. Lease expiry additionally
        /// latches HR8 -> LEASE_EXPIRED (7) and sets the HR9 lease-expired fault, both caught above.
        /// </summary>
        public bool LeaseFresh { get; init; }
    }

    /// <summary>One endurance poll snapshot, emitted as one append-only JSONL evidence line.</summary>
    public sealed class ReadyEnduranceSample
    {
        public DateTimeOffset Timestamp { get; init; }
        public TimeSpan Uptime { get; init; }
        public string State { get; init; } = string.Empty;
        public string RunStatus { get; init; } = string.Empty;
        public string GlobalStopState { get; init; } = string.Empty;
        public bool HeartbeatActive { get; init; }
        public bool TrackPowerOn { get; init; }
        public IReadOnlyList<ReadyEnduranceAmp> Amps { get; init; } = Array.Empty<ReadyEnduranceAmp>();

        /// <summary>True when every health predicate held for this poll.</summary>
        public bool Healthy { get; init; }

        /// <summary>The first failure reason for this poll, or null when healthy.</summary>
        public string? Failure { get; init; }
    }

    /// <summary>Final summary of one endurance hold, emitted as one append-only JSONL evidence line.</summary>
    public sealed class ReadyEnduranceSummary
    {
        public ReadyEnduranceDisposition Disposition { get; init; }
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset EndedAt { get; init; }
        public TimeSpan Duration { get; init; }
        public int PollCount { get; init; }

        /// <summary>True when no deviation was observed for the whole hold.</summary>
        public bool Healthy { get; init; }

        /// <summary>The first deviation reason, or null when the hold was healthy throughout.</summary>
        public string? FirstFailureReason { get; init; }

        /// <summary>
        /// The final runtime shutdown result (e.g. "Stopped" / "Failed: ..."). Populated by the
        /// runner after it disposes the runtime; not populated by the monitor itself.
        /// </summary>
        public string? FinalShutdownResult { get; set; }
    }

    /// <summary>Injectable sink that receives typed endurance evidence (samples + summary).</summary>
    public interface IReadyEnduranceSink
    {
        void WriteSample(ReadyEnduranceSample sample);
        void WriteSummary(ReadyEnduranceSummary summary);
    }

    /// <summary>
    /// Single-session READY-endurance hold monitor (the authorized FALLBACK — NOT a repeated-init
    /// test). After a successful Stage 1-5 init reaching <see cref="InitializationStatus.StoppedAtGlobalReady"/>
    /// with <see cref="TrackRuntimeState.Running"/>, it HOLDS the session (never disposing the
    /// runtime itself) and polls at a fixed interval, emitting one evidence sample per poll. On the
    /// first unexpected deviation it fails closed — records the first failure reason and returns
    /// immediately with no retry. No REARM (112), no Stage 6/7 (track power / normal operation), no
    /// controller reset and no watchdog bypass are performed here: the monitor only reads the
    /// runtime's public observability surface.
    ///
    /// The monitor owns no shutdown: the caller is responsible for disposing the runtime after
    /// <see cref="RunAsync"/> returns and for recording the final shutdown result.
    /// </summary>
    public sealed class ReadyEnduranceMonitor
    {
        /// <summary>Default poll interval (30 s).</summary>
        public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(30);

        /// <summary>Default maximum hold duration (8 h).</summary>
        public static readonly TimeSpan DefaultMaxDuration = TimeSpan.FromHours(8);

        /// <summary>
        /// Freshness window used to declare the host-authority heartbeat (110) "active". The
        /// heartbeat is sent every <see cref="GlobalStopMonitor.HeartbeatInterval"/> (250 ms), so
        /// this is a generous 8x margin.
        /// </summary>
        public static readonly TimeSpan HeartbeatFreshWindow = TimeSpan.FromSeconds(2);

        private readonly ITrackApplicationRuntime _runtime;
        private readonly IReadOnlyCollection<byte> _domain;
        private readonly IReadyEnduranceSink _sink;
        private readonly Func<DateTimeOffset> _clock;
        private readonly TimeSpan _pollInterval;
        private readonly TimeSpan _maxDuration;
        private readonly Func<bool>? _heartbeatActive;

        /// <summary>
        /// Creates the monitor.
        /// </summary>
        /// <param name="runtime">The running runtime to observe (Real mode, stopped at GlobalReady).</param>
        /// <param name="domain">The configured amplifier domain to hold (e.g. {1,3,4,6}).</param>
        /// <param name="sink">Receives typed per-poll samples and the final summary.</param>
        /// <param name="clock">Injectable time source (testable). Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
        /// <param name="pollInterval">Poll cadence. Defaults to 30 s.</param>
        /// <param name="maxDuration">Hard hold duration. Defaults to 8 h.</param>
        /// <param name="heartbeatActive">
        /// Optional direct heartbeat-liveness signal. When null, the monitor falls back to the
        /// runtime's own signal — an un-latched global stop — because a heartbeat task fault latches
        /// <see cref="GlobalStopReason.Pic32Lost"/> and a stopped heartbeat lets the PIC32 host-
        /// authority watchdog (2 s) latch <see cref="GlobalStopReason.Pic32HostLoss"/>.
        /// </param>
        public ReadyEnduranceMonitor(
            ITrackApplicationRuntime runtime,
            IReadOnlyCollection<byte> domain,
            IReadyEnduranceSink sink,
            Func<DateTimeOffset>? clock = null,
            TimeSpan? pollInterval = null,
            TimeSpan? maxDuration = null,
            Func<bool>? heartbeatActive = null)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _domain = domain ?? throw new ArgumentNullException(nameof(domain));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
            _pollInterval = pollInterval ?? DefaultPollInterval;
            _maxDuration = maxDuration ?? DefaultMaxDuration;
            _heartbeatActive = heartbeatActive;
        }

        /// <summary>
        /// Runs the hold until the configured maximum duration elapses, the caller cancels
        /// <paramref name="ct"/> (operator STOP), or the first unexpected deviation (fail-closed).
        /// Emits one sample per poll and returns the summary (the summary is NOT written to the sink
        /// here — the caller finalizes <see cref="ReadyEnduranceSummary.FinalShutdownResult"/> and
        /// writes it).
        /// </summary>
        public async Task<ReadyEnduranceSummary> RunAsync(CancellationToken ct)
        {
            var startedAt = _clock();
            int pollCount = 0;
            string? firstFailureReason = null;
            ReadyEnduranceDisposition disposition;
            DateTimeOffset endedAt;

            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    // A cancellation at/after the max duration is the 8 h hard bound (CancelAfter),
                    // not an operator stop; before it, it is the operator STOP signal.
                    var cancelNow = _clock();
                    disposition = cancelNow - startedAt >= _maxDuration
                        ? ReadyEnduranceDisposition.DurationBoundReached
                        : ReadyEnduranceDisposition.OperatorStop;
                    endedAt = cancelNow;
                    break;
                }

                var now = _clock();
                if (now - startedAt >= _maxDuration)
                {
                    disposition = ReadyEnduranceDisposition.DurationBoundReached;
                    endedAt = now;
                    break;
                }

                var sample = BuildSample(now, startedAt);
                pollCount++;

                _sink.WriteSample(sample);

                if (!sample.Healthy)
                {
                    firstFailureReason = sample.Failure;
                    disposition = ReadyEnduranceDisposition.FirstFailure;
                    endedAt = now;
                    break;
                }

                try
                {
                    await Task.Delay(_pollInterval, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Fall through to the next loop iteration: the cancellation is observed there.
                }
            }

            return new ReadyEnduranceSummary
            {
                Disposition = disposition,
                StartedAt = startedAt,
                EndedAt = endedAt,
                Duration = endedAt - startedAt,
                PollCount = pollCount,
                Healthy = firstFailureReason is null,
                FirstFailureReason = firstFailureReason
            };
        }

        /// <summary>Builds the health snapshot for one poll, classifying the first deviation (stable order).</summary>
        private ReadyEnduranceSample BuildSample(DateTimeOffset now, DateTimeOffset startedAt)
        {
            var listing = _runtime.TrackAmplifiers;
            var amps = new List<ReadyEnduranceAmp>(_domain.Count);
            foreach (var slave in _domain)
            {
                amps.Add(BuildAmpSnapshot(slave, listing));
            }

            var state = _runtime.State;
            var runStatus = _runtime.RunStatus;
            var globalStopState = _runtime.GlobalStopState.State;
            var trackPowerOn = _runtime.TrackPowerOn;
            bool heartbeatActive = HeartbeatIsActive(globalStopState);

            string? failure = null;
            if (globalStopState == GlobalStopState.Latched)
            {
                failure = $"global stop latched (reason={_runtime.GlobalStopState.Reason})";
            }
            else if (state != TrackRuntimeState.Running)
            {
                failure = $"runtime state {state} (expected Running)";
            }
            else if (runStatus != InitializationStatus.StoppedAtGlobalReady)
            {
                failure = $"run status {runStatus} (expected StoppedAtGlobalReady)";
            }
            else if (trackPowerOn)
            {
                failure = "track power unexpectedly ON (Stage 6 must never run during the hold)";
            }
            else if (!heartbeatActive)
            {
                failure = "host-authority heartbeat (110) not active";
            }
            else
            {
                foreach (var amp in amps)
                {
                    if (!amp.Detected)
                    {
                        failure = $"amp{amp.Slave} not detected";
                        break;
                    }

                    if (amp.Phase1Failure is not null)
                    {
                        failure = $"amp{amp.Slave} Phase1Failure={amp.Phase1Failure}";
                        break;
                    }

                    if (!amp.ConfigurationVerified)
                    {
                        failure = $"amp{amp.Slave} configuration not verified";
                        break;
                    }

                    if (!amp.Ready)
                    {
                        failure = $"amp{amp.Slave} HR8=0x{amp.Hr8:X4} low-nibble={amp.Hr8LowNibble} (expected READY=1)";
                        break;
                    }

                    if (amp.Fault)
                    {
                        failure = $"amp{amp.Slave} HR9=0x{amp.Hr9:X4} (fault)";
                        break;
                    }

                    if (!amp.LeaseFresh)
                    {
                        failure = $"amp{amp.Slave} data/lease not fresh";
                        break;
                    }
                }
            }

            return new ReadyEnduranceSample
            {
                Timestamp = now,
                Uptime = now - startedAt,
                State = state.ToString(),
                RunStatus = runStatus.ToString(),
                GlobalStopState = globalStopState.ToString(),
                HeartbeatActive = heartbeatActive,
                TrackPowerOn = trackPowerOn,
                Amps = amps,
                Healthy = failure is null,
                Failure = failure
            };
        }

        private ReadyEnduranceAmp BuildAmpSnapshot(byte slave, IReadOnlyList<TrackAmplifierItem> listing)
        {
            var amp = slave < listing.Count ? listing[slave] : null;
            if (amp is null)
            {
                return new ReadyEnduranceAmp
                {
                    Slave = slave,
                    Detected = false,
                    Hr8LowNibble = 0,
                    Ready = false,
                    LeaseFresh = false
                };
            }

            var regs = amp.HoldingReg ?? new ushort[TrackAmplifierRegisters.Count];
            ushort hr8 = regs.Length > TrackAmplifierRegisters.State ? regs[TrackAmplifierRegisters.State] : (ushort)0;
            ushort hr9 = regs.Length > TrackAmplifierRegisters.Fault ? regs[TrackAmplifierRegisters.Fault] : (ushort)0;
            ushort hr22 = regs.Length > TrackAmplifierRegisters.FwVersionPatchProtocol ? regs[TrackAmplifierRegisters.FwVersionPatchProtocol] : (ushort)0;
            int lowNibble = hr8 & 0xF;

            return new ReadyEnduranceAmp
            {
                Slave = slave,
                Detected = amp.SlaveDetected != 0,
                ConfigurationVerified = amp.ConfigurationVerified,
                Phase1Failure = amp.Phase1Failure,
                Hr8 = hr8,
                Hr9 = hr9,
                Hr22 = hr22,
                Hr8LowNibble = lowNibble,
                Ready = lowNibble == TrackAmplifierRegisters.ReadyState,
                LeaseFresh = TrackAmplifierDataFreshness.HasFreshProtocolReadback(
                    amp, now: _clock(), TrackAmplifierDataFreshness.DefaultStaleAfter)
            };
        }

        private bool HeartbeatIsActive(GlobalStopState globalStopState)
        {
            if (_heartbeatActive is { } direct)
            {
                return direct();
            }

            // Fallback: an un-latched global stop means the heartbeat monitor task has not faulted
            // (a task fault latches Pic32Lost) and the PIC32 host-authority watchdog has not expired
            // (which would latch Pic32HostLoss within ~2 s of the last heartbeat).
            return globalStopState != GlobalStopState.Latched;
        }
    }
}
