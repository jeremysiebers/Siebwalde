using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The hard outbound session-command allowlist for a LIVE_HARDWARE bounded telemetry-enablement
    /// session. These six command bytes are the ONLY outbound commands the
    /// <see cref="KoploperSessionCommandGuard"/> will forward to the real track transport.
    /// <para>
    /// Mandatory cold-start order (<see cref="ColdStartOrder"/>):
    /// <see cref="ClientConnectionRequest"/> (250) →
    /// <see cref="ExecMbusStateSlavesOn"/> (100) →
    /// <see cref="ExecMbusStateSlaveDetect"/> (101) →
    /// <see cref="ExecMbusStateSlaveInit"/> (104) →
    /// <see cref="ExecMbusStateStartDataUpload"/> (106) →
    /// <see cref="ExecMbusStateSlaveEnable"/> (105).
    /// <b>106 MUST precede 105</b>: 106 sets state=WAIT, and 105 is the only path into SERVICE_TASKS.
    /// </para>
    /// Everything else — in particular 108 (EXEC_MBUS_SLAVE_DATA_EXCH, the only HR0/PWM/movement
    /// write vector), 107 (EXEC_MBUS_STATE_RESET), and every firmware/recovery/control-stack command —
    /// is a hard stop and is never transmitted.
    /// </summary>
    /// <remarks>
    /// 107 (<see cref="ExecMbusStateReset"/>) is declared here ONLY as a named constant for the
    /// shared acknowledgement/display mapping. It is deliberately NOT a member of
    /// <see cref="ColdStartOrder"/> or <see cref="IsPermitted"/>: the read-only telemetry session
    /// never sends RESET. The state-changing apply-config session uses
    /// <see cref="KoploperSessionCommandApplyAllowlist"/> instead.
    /// </remarks>
    public static class KoploperSessionCommandAllowlist
    {
        /// <summary>Establish the Ethernet-target connection. Command byte 250.</summary>
        public const byte ClientConnectionRequest = EnumClientCommands.CLIENT_CONNECTION_REQUEST;

        /// <summary>Physically power the MBUS slave bus. Command byte 100.</summary>
        public const byte ExecMbusStateSlavesOn = TrackCommand.EXEC_MBUS_STATE_SLAVES_ON;

        /// <summary>Detect the MBUS slaves. Command byte 101.</summary>
        public const byte ExecMbusStateSlaveDetect = TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT;

        /// <summary>Initialize the MBUS slaves. Command byte 104.</summary>
        public const byte ExecMbusStateSlaveInit = TrackCommand.EXEC_MBUS_STATE_SLAVE_INIT;

        /// <summary>Start MBUS data upload (telemetry; sets state=WAIT). Command byte 106.</summary>
        public const byte ExecMbusStateStartDataUpload = TrackCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD;

        /// <summary>Enable the MBUS slaves (only path into SERVICE_TASKS). Command byte 105.</summary>
        public const byte ExecMbusStateSlaveEnable = TrackCommand.EXEC_MBUS_STATE_SLAVE_ENABLE;

        /// <summary>Reset the MBUS slaves to 0xAA. Command byte 107. NOT in the read-only allowlist.</summary>
        public const byte ExecMbusStateReset = TrackCommand.EXEC_MBUS_STATE_RESET;

        /// <summary>The mandatory cold-start sequence, in the exact required order.</summary>
        public static readonly IReadOnlyList<byte> ColdStartOrder = new byte[]
        {
            ClientConnectionRequest,      // 250
            ExecMbusStateSlavesOn,        // 100
            ExecMbusStateSlaveDetect,     // 101
            ExecMbusStateSlaveInit,       // 104
            ExecMbusStateStartDataUpload, // 106
            ExecMbusStateSlaveEnable      // 105
        };

        /// <summary>True only for the six permitted telemetry-enablement command types.</summary>
        public static bool IsPermitted(byte command)
            => command == ClientConnectionRequest
                || command == ExecMbusStateSlavesOn
                || command == ExecMbusStateSlaveDetect
                || command == ExecMbusStateSlaveInit
                || command == ExecMbusStateStartDataUpload
                || command == ExecMbusStateSlaveEnable;

        /// <summary>A stable, human-readable name for a command byte (permitted or not).</summary>
        public static string GetDisplayName(byte command) => command switch
        {
            ClientConnectionRequest => nameof(EnumClientCommands.CLIENT_CONNECTION_REQUEST),
            ExecMbusStateSlavesOn => nameof(TrackCommand.EXEC_MBUS_STATE_SLAVES_ON),
            ExecMbusStateSlaveDetect => nameof(TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT),
            ExecMbusStateSlaveInit => nameof(TrackCommand.EXEC_MBUS_STATE_SLAVE_INIT),
            ExecMbusStateStartDataUpload => nameof(TrackCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD),
            ExecMbusStateSlaveEnable => nameof(TrackCommand.EXEC_MBUS_STATE_SLAVE_ENABLE),
            ExecMbusStateReset => nameof(TrackCommand.EXEC_MBUS_STATE_RESET),
            _ => $"0x{command:X2}"
        };

        /// <summary>
        /// The expected per-command acknowledgement (a master status frame parsed into
        /// <see cref="ReceivedMessage"/>) for a permitted session command. The host must wait for
        /// <c>(TaskId, Taskcommand, Taskstate)</c> to equal the returned values (Taskstate = DONE)
        /// before sending the next command. Returns false for a command that has no acknowledgement
        /// (which never occurs for the six permitted commands).
        /// </summary>
        public static bool TryGetExpectedAcknowledgement(
            byte command,
            out ushort taskId,
            out ushort taskCommand,
            out ushort taskState)
        {
            switch (command)
            {
                case ClientConnectionRequest:
                    taskId = TaskId.CONTROLLER;
                    taskCommand = TaskStates.CONNECTED;
                    taskState = TaskStates.DONE;
                    return true;

                case ExecMbusStateSlavesOn:
                    taskId = TaskId.MBUS;
                    taskCommand = EnumMbusStatus.MBUS_STATE_SLAVES_ON;
                    taskState = TaskStates.DONE;
                    return true;

                case ExecMbusStateSlaveDetect:
                    taskId = TaskId.MBUS;
                    taskCommand = EnumMbusStatus.MBUS_STATE_SLAVE_DETECT;
                    taskState = TaskStates.DONE;
                    return true;

                case ExecMbusStateSlaveInit:
                    taskId = TaskId.MBUS;
                    taskCommand = EnumMbusStatus.MBUS_STATE_SLAVE_INIT;
                    taskState = TaskStates.DONE;
                    return true;

                case ExecMbusStateStartDataUpload:
                    taskId = TaskId.MBUS;
                    taskCommand = EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD;
                    taskState = TaskStates.DONE;
                    return true;

                case ExecMbusStateSlaveEnable:
                    taskId = TaskId.MBUS;
                    taskCommand = EnumMbusStatus.MBUS_STATE_SLAVE_ENABLE;
                    taskState = TaskStates.DONE;
                    return true;

                case ExecMbusStateReset:
                    taskId = TaskId.MBUS;
                    taskCommand = EnumMbusStatus.MBUS_STATE_RESET;
                    taskState = TaskStates.DONE;
                    return true;

                default:
                    taskId = 0;
                    taskCommand = 0;
                    taskState = 0;
                    return false;
            }
        }
    }

    /// <summary>
    /// The session mode of a <see cref="KoploperSessionCommandGuard"/>.
    /// </summary>
    public enum KoploperSessionCommandMode
    {
        /// <summary>The full six-command cold-start sequence must be sent in order.</summary>
        ColdStart = 0,

        /// <summary>
        /// Warm reconnect: the master is already in SERVICE_TASKS with telemetry flowing; only
        /// CLIENT_CONNECTION_REQUEST (250) is accepted and no enablement command is re-sent.
        /// </summary>
        WarmReconnect = 1,

        /// <summary>No outbound session (a passive observer has no PIC32 client).</summary>
        None = 2,

        /// <summary>
        /// Safe CONFIG/APPLY: the seven-command init sequence
        /// (250 → 107 → 100 → 106 → 101 → 104 → 105) must be sent in order, including 107 RESET.
        /// </summary>
        ApplyConfig = 3
    }
}
