using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>Why a permitted outbound command was denied by the session-order guard.</summary>
    public enum KoploperSessionCommandOrderViolationReason
    {
        /// <summary>The cold-start sequence was violated (a command arrived before its expected predecessor).</summary>
        OutOfOrder,

        /// <summary>Warm-reconnect mode accepts only CLIENT_CONNECTION_REQUEST (250).</summary>
        WarmReconnectEnablementNotAllowed,

        /// <summary>The session was already stopped by a previous denial; a new session must be begun first.</summary>
        SessionStopped,

        /// <summary>The cold sequence is already complete; no further enablement command is allowed.</summary>
        ColdSequenceComplete
    }

    /// <summary>
    /// Raised by <see cref="KoploperSessionCommandGuard.SendAsync"/> when a permitted command arrives
    /// out of the mandated cold-start order, or violates the warm-reconnect or session-complete rule.
    /// The command is NOT transmitted and the session is STOPPED: no further command is forwarded until
    /// <see cref="KoploperSessionCommandGuard.BeginNewSession"/> or
    /// <see cref="KoploperSessionCommandGuard.BeginWarmReconnect"/> resets the order state.
    /// </summary>
    public sealed class KoploperSessionCommandOrderViolationException : InvalidOperationException
    {
        /// <summary>The denied outbound command byte.</summary>
        public byte Command { get; }

        /// <summary>Why the command was denied.</summary>
        public KoploperSessionCommandOrderViolationReason Reason { get; }

        /// <summary>The command that was expected instead (when meaningful), or <c>null</c>.</summary>
        public byte? ExpectedCommand { get; }

        /// <summary>The session mode at the time of the denial.</summary>
        public KoploperSessionCommandMode Mode { get; }

        public KoploperSessionCommandOrderViolationException(
            byte command,
            KoploperSessionCommandOrderViolationReason reason,
            byte? expectedCommand,
            KoploperSessionCommandMode mode)
            : base(BuildMessage(command, reason, expectedCommand, mode))
        {
            Command = command;
            Reason = reason;
            ExpectedCommand = expectedCommand;
            Mode = mode;
        }

        private static string BuildMessage(
            byte command,
            KoploperSessionCommandOrderViolationReason reason,
            byte? expectedCommand,
            KoploperSessionCommandMode mode)
        {
            string commandName = KoploperSessionCommandAllowlist.GetDisplayName(command);
            string expected = expectedCommand is byte e
                ? KoploperSessionCommandAllowlist.GetDisplayName(e)
                : "(none)";

            return $"Outbound command {commandName} (0x{command:X2}) was denied " +
                $"(reason={reason}, mode={mode}, expected={expected}); " +
                "it was NOT transmitted and the session was stopped.";
        }
    }
}
