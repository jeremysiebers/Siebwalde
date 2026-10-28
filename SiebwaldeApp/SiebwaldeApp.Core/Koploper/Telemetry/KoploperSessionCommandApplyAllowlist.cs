using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The outbound session-command allowlist for the state-changing safe CONFIG/APPLY diagnostic
    /// (the <c>apply-config</c> host). It is a DISTINCT, wider command set than the read-only
    /// <see cref="KoploperSessionCommandAllowlist"/>: it additionally permits 107
    /// (<see cref="KoploperSessionCommandAllowlist.ExecMbusStateReset"/>, EXEC_MBUS_STATE_RESET),
    /// which is never permitted in the read-only telemetry session.
    /// <para>
    /// The deployed PIC32 requires the full init order
    /// <see cref="ApplyConfigOrder"/>: 250 CONNECT → 107 RESET → 100 SLAVES_ON →
    /// 106 DATA_UPLOAD → 101 DETECT → 104 INIT → 105 ENABLE. Without 107 RESET first, a controller
    /// already in SERVICE_TASKS does not re-ack 100 (SLAVES_ON), and the amplifiers are not returned
    /// to 0xAA so the 104 INIT identity verification (two fresh FC03 HR7 reads @ 0xAA) can succeed.
    /// </para>
    /// <para>
    /// 108 (EXEC_MBUS_SLAVE_DATA_EXCH, the register write vector used for CONFIG/APPLY and the
    /// DISABLE safety gate) and 110 (HOST_HEARTBEAT) are NOT part of this session-command set; they
    /// are sent by the orchestrator through the RAW comm client, bypassing the guard's session
    /// boundary, and are covered by the separate apply-config grant scope.
    /// </para>
    /// </summary>
    public static class KoploperSessionCommandApplyAllowlist
    {
        /// <summary>
        /// The mandatory apply-config init sequence, in the exact required order
        /// (250 → 107 → 100 → 106 → 101 → 104 → 105).
        /// </summary>
        public static readonly IReadOnlyList<byte> ApplyConfigOrder = new byte[]
        {
            KoploperSessionCommandAllowlist.ClientConnectionRequest,      // 250
            KoploperSessionCommandAllowlist.ExecMbusStateReset,           // 107
            KoploperSessionCommandAllowlist.ExecMbusStateSlavesOn,        // 100
            KoploperSessionCommandAllowlist.ExecMbusStateStartDataUpload, // 106
            KoploperSessionCommandAllowlist.ExecMbusStateSlaveDetect,     // 101
            KoploperSessionCommandAllowlist.ExecMbusStateSlaveInit,       // 104
            KoploperSessionCommandAllowlist.ExecMbusStateSlaveEnable      // 105
        };

        /// <summary>True only for the seven apply-config init command types (the six read-only
        /// telemetry commands plus 107 RESET).</summary>
        public static bool IsPermitted(byte command)
            => command == KoploperSessionCommandAllowlist.ClientConnectionRequest
                || command == KoploperSessionCommandAllowlist.ExecMbusStateReset
                || command == KoploperSessionCommandAllowlist.ExecMbusStateSlavesOn
                || command == KoploperSessionCommandAllowlist.ExecMbusStateStartDataUpload
                || command == KoploperSessionCommandAllowlist.ExecMbusStateSlaveDetect
                || command == KoploperSessionCommandAllowlist.ExecMbusStateSlaveInit
                || command == KoploperSessionCommandAllowlist.ExecMbusStateSlaveEnable;
    }
}
