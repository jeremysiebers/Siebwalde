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
    /// </summary>
    public sealed class GroupReadyStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly IReadOnlyList<byte> _requiredAmps;
        private readonly string _loggerInstance;
        private readonly TimeSpan _verifyWindow;

        private int _subState;
        private DateTimeOffset _deadline;

        public string Name => "GroupReady";

        public GroupReadyStep(
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

        public async Task<InitStepResult> ExecuteAsync(ReceivedMessage? lastMessage, CancellationToken ct)
        {
            if (_subState == 0)
            {
                if (_requiredAmps.Count == 0)
                {
                    return InitStepResult.Error("No required amplifiers to mark group-ready.");
                }

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

            var rejected = _requiredAmps
                .Where(amp => !IsGroupReady(_variables.trackAmpItems[amp]))
                .ToArray();

            if (rejected.Length == 0)
            {
                IoC.Logger.Log("GroupReady => every required amplifier accepted GROUP_READY.", _loggerInstance);
                _subState = 0;
                return InitStepResult.Completed();
            }

            if (DateTimeOffset.UtcNow >= _deadline)
            {
                _subState = 0;
                var message = $"GROUP_READY rejected by amplifier(s) {string.Join(",", rejected)}.";
                IoC.Logger.Log($"GroupReady => {message}", _loggerInstance);
                return InitStepResult.Error(message);
            }

            return InitStepResult.Continue();
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
