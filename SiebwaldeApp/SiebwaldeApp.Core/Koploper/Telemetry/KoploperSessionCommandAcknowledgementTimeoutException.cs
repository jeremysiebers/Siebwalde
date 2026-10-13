using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Raised by <see cref="KoploperSessionCommandGuard.SendAsync"/> when a permitted command was
    /// transmitted but its expected master acknowledgement (a status frame with the matching
    /// TaskId/Taskcommand/Taskstate == DONE) did not arrive within the bounded timeout. The command
    /// is counted as NOT-acknowledged (distinct from "transmitted") and the session is STOPPED: no
    /// further command is forwarded until a new session is begun.
    /// </summary>
    public sealed class KoploperSessionCommandAcknowledgementTimeoutException : InvalidOperationException
    {
        /// <summary>The transmitted command whose acknowledgement timed out.</summary>
        public byte Command { get; }

        /// <summary>The expected acknowledgement TaskId.</summary>
        public ushort ExpectedTaskId { get; }

        /// <summary>The expected acknowledgement Taskcommand.</summary>
        public ushort ExpectedTaskCommand { get; }

        /// <summary>The expected acknowledgement Taskstate.</summary>
        public ushort ExpectedTaskState { get; }

        public KoploperSessionCommandAcknowledgementTimeoutException(
            byte command,
            ushort expectedTaskId,
            ushort expectedTaskCommand,
            ushort expectedTaskState)
            : base(
                $"Command {KoploperSessionCommandAllowlist.GetDisplayName(command)} (0x{command:X2}) was transmitted " +
                $"but no matching acknowledgement (TaskId={expectedTaskId}, Taskcommand={expectedTaskCommand}, " +
                $"Taskstate={expectedTaskState}) arrived within the bounded timeout; the session was stopped.")
        {
            Command = command;
            ExpectedTaskId = expectedTaskId;
            ExpectedTaskCommand = expectedTaskCommand;
            ExpectedTaskState = expectedTaskState;
        }
    }
}
