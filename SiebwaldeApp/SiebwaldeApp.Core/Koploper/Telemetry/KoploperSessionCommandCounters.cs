namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// An immutable, read-only audit snapshot of a <see cref="KoploperSessionCommandGuard"/>: the
    /// per-command telemetry-enablement TRANSMITTED and ACKNOWLEDGED counts (kept separate so the
    /// two are never conflated), the denial/timeout counts, and the application/movement-control
    /// counters. The application/movement-control counters are always zero by construction — the
    /// guard has no write path for 108 (EXEC_MBUS_SLAVE_DATA_EXCH), HR0, PWM, direction, brake, EMO,
    /// switch, or locomotive commands.
    /// </summary>
    public sealed record KoploperSessionCommandCounters(
        long ClientConnectionRequestTransmitted,
        long ClientConnectionRequestAcknowledged,
        long ExecMbusStateSlavesOnTransmitted,
        long ExecMbusStateSlavesOnAcknowledged,
        long ExecMbusStateSlaveDetectTransmitted,
        long ExecMbusStateSlaveDetectAcknowledged,
        long ExecMbusStateSlaveInitTransmitted,
        long ExecMbusStateSlaveInitAcknowledged,
        long ExecMbusStateStartDataUploadTransmitted,
        long ExecMbusStateStartDataUploadAcknowledged,
        long ExecMbusStateSlaveEnableTransmitted,
        long ExecMbusStateSlaveEnableAcknowledged,
        long RejectedCommands,
        long OrderViolations,
        long AcknowledgementTimeouts,
        long AmplifierRegisterWrite,
        long AmplifierHr0Writes,
        long PwmWrites,
        long DirectionWrites,
        long BrakeWrites,
        long EmoStopWrites,
        long SwitchWrites,
        long LocomotiveCommands)
    {
        /// <summary>Total telemetry-enablement (session) commands forwarded to the transport.</summary>
        public long TotalTransmitted =>
            ClientConnectionRequestTransmitted + ExecMbusStateSlavesOnTransmitted +
            ExecMbusStateSlaveDetectTransmitted + ExecMbusStateSlaveInitTransmitted +
            ExecMbusStateStartDataUploadTransmitted + ExecMbusStateSlaveEnableTransmitted;

        /// <summary>Total telemetry-enablement commands whose matching DONE acknowledgement was received.</summary>
        public long TotalAcknowledged =>
            ClientConnectionRequestAcknowledged + ExecMbusStateSlavesOnAcknowledged +
            ExecMbusStateSlaveDetectAcknowledged + ExecMbusStateSlaveInitAcknowledged +
            ExecMbusStateStartDataUploadAcknowledged + ExecMbusStateSlaveEnableAcknowledged;

        /// <summary>Total application/movement-control writes transmitted (always 0).</summary>
        public long TotalApplicationMovementControl =>
            AmplifierRegisterWrite + AmplifierHr0Writes + PwmWrites + DirectionWrites +
            BrakeWrites + EmoStopWrites + SwitchWrites + LocomotiveCommands;
    }
}
