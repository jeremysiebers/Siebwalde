using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    public enum AmplifierReleaseResultKind
    {
        /// <summary>REARM_GLOBAL_STOP (112) emitted followed by a fresh non-neutral command.</summary>
        Released,
        /// <summary>Physical stop inputs were not confirmed clear; release refused (fail closed).</summary>
        BlockedStopInputsNotClear,
        /// <summary>A detected amplifier was not stop-confirmed; release/exit refused (fail closed).</summary>
        BlockedUnconfirmedAmplifier,
        /// <summary>The command could not be emitted.</summary>
        Failed
    }

    public readonly struct AmplifierReleaseResult
    {
        public AmplifierReleaseResultKind Kind { get; }
        public string? Detail { get; }

        public AmplifierReleaseResult(AmplifierReleaseResultKind kind, string? detail = null)
        {
            Kind = kind;
            Detail = detail;
        }
    }

    /// <summary>
    /// The C# release-authority path (frozen R8-2 / Contract Amendment A2 item 5): (a) confirm
    /// physical stop inputs are clear, (b) emit <c>REARM_GLOBAL_STOP</c> (112), then (c) emit a
    /// fresh valid non-neutral movement command. It never auto-restores a previous PWM: the fresh
    /// command is the only movement output, and the amplifier's prior PWM is not retained, queued
    /// or replayed.
    ///
    /// Software/protocol authority only. "Physical stop inputs clear" is injected by the caller
    /// (the V4 physical evidence boundary is not crossed here).
    /// </summary>
    public sealed class TrackAmplifierReleaseAuthority
    {
        private readonly ITrackCommClient _comm;
        private readonly Func<bool> _physicalStopInputsClear;
        private readonly Func<bool> _allAmplifiersConfirmed;

        public TrackAmplifierReleaseAuthority(
            ITrackCommClient comm,
            Func<bool> physicalStopInputsClear,
            Func<bool>? allAmplifiersConfirmed = null)
        {
            _comm = comm ?? throw new ArgumentNullException(nameof(comm));
            _physicalStopInputsClear = physicalStopInputsClear
                ?? throw new ArgumentNullException(nameof(physicalStopInputsClear));
            _allAmplifiersConfirmed = allAmplifiersConfirmed ?? (() => true);
        }

        /// <summary>
        /// Runs the release sequence for one amplifier. <paramref name="freshPwmSetpoint"/> must be
        /// a valid non-neutral duty (400..799 or 398..1); neutral 399 is rejected because release
        /// must produce a fresh non-neutral movement command, not a no-op.
        /// </summary>
        public async Task<AmplifierReleaseResult> ReleaseAsync(
            ushort slaveNumber,
            int freshPwmSetpoint,
            CancellationToken ct = default)
        {
            if (!TrackAmplifierAddress.IsTrackAmplifierAddress(slaveNumber))
            {
                return new AmplifierReleaseResult(AmplifierReleaseResultKind.Failed,
                    $"slave {slaveNumber} is not a track amplifier.");
            }

            if (freshPwmSetpoint == AmplifierSpeedMapper.NeutralPwm)
            {
                return new AmplifierReleaseResult(AmplifierReleaseResultKind.Failed,
                    "release requires a fresh non-neutral command; neutral 399 is not a release.");
            }

            if (!_physicalStopInputsClear())
            {
                return new AmplifierReleaseResult(AmplifierReleaseResultKind.BlockedStopInputsNotClear,
                    "physical stop inputs are not confirmed clear.");
            }

            if (!_allAmplifiersConfirmed())
            {
                return new AmplifierReleaseResult(AmplifierReleaseResultKind.BlockedUnconfirmedAmplifier,
                    "a detected amplifier is not stop-confirmed; release refused.");
            }

            // (b) REARM_GLOBAL_STOP (112): PIC32 LATCHED -> ARMED. No payload.
            await _comm.SendAsync(new SendMessage(TrackCommand.REARM_GLOBAL_STOP, Array.Empty<byte>()), ct)
                .ConfigureAwait(false);

            // (c) A fresh valid non-neutral command. This is a concrete wire write, not a flush of
            // any previously queued PWM: no old PWM is restored.
            var message = BuildHr0Write(slaveNumber, freshPwmSetpoint);
            await _comm.SendAsync(message, ct).ConfigureAwait(false);

            return new AmplifierReleaseResult(AmplifierReleaseResultKind.Released);
        }

        /// <summary>Builds a single-register HR0 write for a fresh command (little-endian value).</summary>
        private static SendMessage BuildHr0Write(ushort slaveNumber, int pwmSetpoint)
        {
            ushort value = (ushort)(pwmSetpoint & TrackAmplifierRegisters.PwmMask);
            return new SendMessage(
                TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH,
                new byte[]
                {
                    (byte)slaveNumber,
                    0xAA, // write
                    1,    // one register
                    TrackAmplifierRegisters.PwmCommand,
                    (byte)(value & 0xFF),
                    (byte)(value >> 8)
                });
        }
    }
}
