using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Stage 6 action: write <c>HR_TRACK_POWER</c> (<c>TRACK_POWER_ENABLE</c>) to every required
    /// amplifier via the existing <c>EXEC_MBUS_SLAVE_DATA_EXCH</c> (108) point-write path, then read
    /// back <c>HR_STATE</c> per amplifier and only declare <c>TRACK_POWER_ON</c> when every required
    /// amplifier reports <c>inhibited == 0</c> AND <c>enable_active</c> AND <c>config_ok</c>.
    ///
    /// It emits NO non-neutral PWM: the energize happens at neutral (399) only, mirroring the
    /// guarded firmware mechanism in <c>amp_core.c</c>. If any amplifier fails to reach the verified
    /// state within the bounded window the step fails closed (never claims TRACK_POWER_ON).
    /// </summary>
    public sealed class TrackPowerEnableStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly IReadOnlyList<byte> _requiredAmps;
        private readonly string _loggerInstance;
        private readonly TimeSpan _verifyWindow;

        private int _subState;
        private DateTimeOffset _deadline;

        public string Name => "TrackPowerEnable";

        public TrackPowerEnableStep(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            IReadOnlyList<byte> requiredAmps,
            string loggerInstance = "Track",
            TimeSpan? verifyWindow = null)
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _requiredAmps = requiredAmps ?? throw new ArgumentNullException(nameof(requiredAmps));
            _loggerInstance = loggerInstance ?? "Track";
            _verifyWindow = verifyWindow ?? TimeSpan.FromSeconds(5);
            _subState = 0;
        }

        public async Task<InitStepResult> ExecuteAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            if (_subState == 0)
            {
                // No required amplifiers: nothing to energize, nothing to claim. Fail closed
                // rather than declaring a vacuous TRACK_POWER_ON.
                if (_requiredAmps.Count == 0)
                {
                    IoC.Logger.Log("TrackPowerEnable: no required amplifiers; failing closed.", _loggerInstance);
                    return InitStepResult.Error("No required amplifiers to energize.");
                }

                // F3 fail-closed gate: every required amplifier must advertise track-power support
                // (PROTOCOL_VERSION >= 0x02). A Phase-1 firmware at 0x01 has no HR_TRACK_POWER, so
                // no write is issued and no TRACK_POWER_ON is claimed.
                var unsupported = _requiredAmps
                    .Where(amp => !SupportsTrackPowerOnAmp(amp))
                    .ToArray();
                if (unsupported.Length > 0)
                {
                    _subState = 0;
                    var message =
                        $"Track power blocked: amplifier(s) {string.Join(",", unsupported)} do not support HR_TRACK_POWER.";
                    IoC.Logger.Log($"TrackPowerEnable => {message}", _loggerInstance);
                    return InitStepResult.Error(message);
                }

                foreach (var amp in _requiredAmps)
                {
                    await _commClient.SendAsync(
                        AmplifierConfigurationDownloader.Exchange(
                            amp,
                            (byte)TrackAmplifierRegisters.TrackPower,
                            TrackAmplifierRegisters.TrackPowerEnableBit),
                        cancellationToken).ConfigureAwait(false);
                }

                IoC.Logger.Log(
                    $"TrackPowerEnable => HR_TRACK_POWER write to amps [{string.Join(",", _requiredAmps)}].",
                    _loggerInstance);

                _deadline = DateTimeOffset.UtcNow + _verifyWindow;
                _subState = 1;
                return InitStepResult.Continue();
            }

            // substate 1: issue a fresh HR_STATE read and verify every required amplifier.
            foreach (var amp in _requiredAmps)
            {
                await _commClient.SendAsync(
                    AmplifierConfigurationDownloader.Exchange(
                        amp,
                        (byte)TrackAmplifierRegisters.State,
                        0,
                        read: true),
                    cancellationToken).ConfigureAwait(false);
            }

            var failures = _requiredAmps
                .Where(amp => !IsTrackPowerOn(_variables.trackAmpItems[amp]))
                .Select(amp => amp)
                .ToArray();

            if (failures.Length == 0)
            {
                IoC.Logger.Log(
                    "TrackPowerEnable => TRACK_POWER_ON verified for every required amplifier.",
                    _loggerInstance);
                _subState = 0;
                return InitStepResult.Completed();
            }

            if (DateTimeOffset.UtcNow >= _deadline)
            {
                _subState = 0;
                var message = $"Track power did not reach TRACK_POWER_ON for amplifier(s) {string.Join(",", failures)}.";
                IoC.Logger.Log($"TrackPowerEnable => {message}", _loggerInstance);
                return InitStepResult.Error(message);
            }

            return InitStepResult.Continue();
        }

        /// <summary>True when HR_STATE reports inhibited==0 AND enable_active AND config_ok.</summary>
        public static bool IsTrackPowerOn(TrackAmplifierItem item)
        {
            if (item is null || item.HoldingReg is null || item.HoldingReg.Length <= TrackAmplifierRegisters.State)
            {
                return false;
            }

            var state = item.HoldingReg[TrackAmplifierRegisters.State];
            var inhibited = (state & TrackAmplifierRegisters.InhibitedBit) != 0;
            var enableActive = (state & TrackAmplifierRegisters.EnableActiveBit) != 0;
            var configOk = (state & TrackAmplifierRegisters.ConfigOkBit) != 0;

            return !inhibited && enableActive && configOk;
        }

        /// <summary>
        /// True when HR_STATE reports inhibited==1 (the H-bridge is de-energized; inhibit re-latched).
        /// This is the inverted form of <see cref="IsTrackPowerOn"/>: de-energization is confirmed by
        /// the inhibit bit being SET, independent of enable/config state. Used by
        /// <see cref="TrackInitStateMachine.DisableTrackPowerAsync"/> to verify a group clear.
        /// </summary>
        public static bool IsInhibited(TrackAmplifierItem item)
        {
            if (item is null || item.HoldingReg is null || item.HoldingReg.Length <= TrackAmplifierRegisters.State)
            {
                return false;
            }

            return (item.HoldingReg[TrackAmplifierRegisters.State] & TrackAmplifierRegisters.InhibitedBit) != 0;
        }

        /// <summary>True when the given amplifier advertises track-power support (PROTOCOL_VERSION &gt;= 0x02).</summary>
        private bool SupportsTrackPowerOnAmp(byte amp)
        {
            var item = _variables.trackAmpItems[amp];
            if (item is null || item.HoldingReg is null ||
                item.HoldingReg.Length <= TrackAmplifierRegisters.FwVersionPatchProtocol)
            {
                return false;
            }

            return TrackAmplifierRegisters.SupportsTrackPower(
                item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol]);
        }
    }
}
