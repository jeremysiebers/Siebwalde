using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Stage 5 group-ready action: write GROUP_READY (HR_TRACK_POWER bit 1) to every required
    /// amplifier via the existing 108 write path, then read back HR_STATE and verify each amplifier
    /// accepted (left STANDBY -> READY). GROUP_READY never energizes the H-bridge; TRACK_POWER_ENABLE
    /// (bit 0, Stage 6) remains the only energize path. If any amplifier rejects, the step fails
    /// closed with the rejecting amplifier list.
    ///
    /// The optional <paramref name="log"/> sink is diagnostic-only: it reads the cached
    /// <see cref="TrackAmplifierItem"/> state (no additional controller transaction) and emits
    /// per-amplifier observations so a failing amplifier can be compared against the successful
    /// ones in the same verification window. When null (the default) the execution path is
    /// unchanged.
    ///
    /// Diagnostic accuracy limits: the HR1 lease-bit echo does NOT independently prove actual lease
    /// freshness; HR15 (write-result) may be overwritten by unrelated writes including the periodic
    /// lease renewal (HR1=0x4000); the cached HoldingReg is not proof of a fresh physical register
    /// observation; and a non-READY HR8 does not by itself prove firmware rejection of the
    /// GROUP_READY command. Describe command delivery, acceptance or lease validity as PROVEN only
    /// when the observed evidence actually supports it.
    /// </summary>
    public sealed class GroupReadyStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly IReadOnlyList<byte> _requiredAmps;
        private readonly string _loggerInstance;
        private readonly TimeSpan _verifyWindow;
        private readonly Action<string>? _log;
        private readonly Dictionary<byte, long> _observedSequence = new();

        private int _subState;
        private DateTimeOffset _deadline;

        public string Name => "GroupReady";

        public GroupReadyStep(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            IReadOnlyList<byte> requiredAmps,
            string loggerInstance = "Track",
            TimeSpan? verifyWindow = null,
            Action<string>? log = null)
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _requiredAmps = requiredAmps ?? throw new ArgumentNullException(nameof(requiredAmps));
            _loggerInstance = loggerInstance ?? "Track";
            _verifyWindow = verifyWindow ?? TimeSpan.FromSeconds(5);
            _log = log;
            _subState = 0;
        }

        public async Task<InitStepResult> ExecuteAsync(ReceivedMessage? lastMessage, CancellationToken ct)
        {
            if (_subState == 0)
            {
                if (_requiredAmps.Count == 0)
                {
                    return InitStepResult.Error("No required amplifiers to mark group-ready.");
                }

                // Diagnostic: pre-write snapshot of every required amplifier.
                LogObservations("BEFORE");

                foreach (var amp in _requiredAmps)
                {
                    await _commClient.SendAsync(
                        AmplifierConfigurationDownloader.Exchange(
                            amp,
                            (byte)TrackAmplifierRegisters.TrackPower,
                            TrackAmplifierRegisters.GroupReadyBit),
                        ct).ConfigureAwait(false);
                }

                IoC.Logger.Log(
                    $"GroupReady => GROUP_READY write to amps [{string.Join(",", _requiredAmps)}].",
                    _loggerInstance);

                _log?.Invoke(
                    $"REQUESTED GROUP_READY HR23=0x{TrackAmplifierRegisters.GroupReadyBit:X4} " +
                    $"to amps [{string.Join(",", _requiredAmps)}]");

                _deadline = DateTimeOffset.UtcNow + _verifyWindow;
                _subState = 1;
                return InitStepResult.Continue();
            }

            // substate 1: read back HR_STATE and verify each amplifier accepted GROUP_READY.
            foreach (var amp in _requiredAmps)
            {
                await _commClient.SendAsync(
                    AmplifierConfigurationDownloader.Exchange(
                        amp,
                        (byte)TrackAmplifierRegisters.State,
                        0,
                        read: true),
                    ct).ConfigureAwait(false);
            }

            // Diagnostic: fresh per-amplifier observations (logged only on readback-sequence change).
            LogObservations("OBSERVED");

            var rejected = _requiredAmps
                .Where(amp => !IsGroupReady(_variables.trackAmpItems[amp]))
                .ToArray();

            if (rejected.Length == 0)
            {
                IoC.Logger.Log("GroupReady => every required amplifier accepted GROUP_READY.", _loggerInstance);
                _log?.Invoke("ACCEPTED every required amplifier reached READY.");
                _subState = 0;
                return InitStepResult.Completed();
            }

            if (DateTimeOffset.UtcNow >= _deadline)
            {
                _subState = 0;
                var message = $"GROUP_READY rejected by amplifier(s) {string.Join(",", rejected)}.";
                IoC.Logger.Log($"GroupReady => {message}", _loggerInstance);
                _log?.Invoke(
                    $"TIMEOUT after {_verifyWindow.TotalSeconds:F0}s: amplifier(s) [{string.Join(",", rejected)}] " +
                    "did not reach READY within the verification window.");
                return InitStepResult.Error(message);
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// Diagnostic-only per-amplifier snapshot. Reads the cached HoldingReg (no controller
        /// transaction) and, for the OBSERVED verb, logs only when the readback sequence advanced
        /// since the last observation, so a single cached value is not re-emitted every poll. The
        /// reported values are cache content, not proof of fresh physical register state.
        /// </summary>
        private void LogObservations(string verb)
        {
            if (_log is null)
            {
                return;
            }

            foreach (var amp in _requiredAmps)
            {
                var item = _variables.trackAmpItems[amp];
                if (item is null || item.HoldingReg is null ||
                    item.HoldingReg.Length <= TrackAmplifierRegisters.WriteResult)
                {
                    _log($"{verb} slave={amp} no-data");
                    continue;
                }

                long seq = item.ProtocolReadbackSequence;
                if (verb == "OBSERVED" && _observedSequence.TryGetValue(amp, out var last) && last == seq)
                {
                    continue;
                }

                _observedSequence[amp] = seq;
                double freshMs = item.LastDataReceivedUtc is DateTimeOffset received
                    ? (DateTimeOffset.UtcNow - received).TotalMilliseconds
                    : double.NaN;

                _log(
                    $"{verb} slave={amp} " +
                    $"HR8=0x{item.HoldingReg[TrackAmplifierRegisters.State]:X4} " +
                    $"HR9=0x{item.HoldingReg[TrackAmplifierRegisters.Fault]:X4} " +
                    $"HR1=0x{item.HoldingReg[TrackAmplifierRegisters.Control]:X4} " +
                    $"HR15=0x{item.HoldingReg[TrackAmplifierRegisters.WriteResult]:X4} " +
                    $"seq={seq} freshMs={freshMs:F0} " +
                    $"ready={(IsGroupReady(item) ? "YES" : "no")}");
            }
        }

        /// <summary>True when the amplifier accepted GROUP_READY (its HR_STATE reflects READY).</summary>
        public static bool IsGroupReady(TrackAmplifierItem item)
        {
            if (item is null || item.HoldingReg is null || item.HoldingReg.Length <= TrackAmplifierRegisters.State)
            {
                return false;
            }

            return (item.HoldingReg[TrackAmplifierRegisters.State] & 0xF) == TrackAmplifierRegisters.ReadyState;
        }
    }
}
