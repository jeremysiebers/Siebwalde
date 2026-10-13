using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Raised by <see cref="KoploperSessionCommandGuard.SendAsync"/> when an outbound command is not
    /// in <see cref="KoploperSessionCommandAllowlist"/> at all (for example 108 EXEC_MBUS_SLAVE_DATA_EXCH,
    /// 107 EXEC_MBUS_STATE_RESET, or an arbitrary byte). The command is NOT transmitted. This is the
    /// distinct fail-closed stop signal for any attempted application/movement-control write.
    /// </summary>
    public sealed class KoploperSessionCommandRejectedException : InvalidOperationException
    {
        /// <summary>The refused outbound command byte.</summary>
        public byte Command { get; }

        public KoploperSessionCommandRejectedException(byte command)
            : base(
                $"Outbound command {KoploperSessionCommandAllowlist.GetDisplayName(command)} (0x{command:X2}) " +
                "is not in the bounded telemetry-enablement session-command allowlist; it was NOT transmitted.")
        {
            Command = command;
        }
    }
}
