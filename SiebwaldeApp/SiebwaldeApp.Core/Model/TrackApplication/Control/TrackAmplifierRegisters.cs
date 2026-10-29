namespace SiebwaldeApp.Core
{
    /// <summary>Frozen Phase-1 map; docs/amplifier-system-architecture.md R8-4.</summary>
    public static class TrackAmplifierRegisters
    {
        public const int Count = 23;
        public const int PwmCommand = 0, Control = 1, Address = 2;
        public const int ConfigRamp = 3, ConfigFastStop = 4, ConfigWatchdog = 5, ConfigTag = 6;
        public const int Identity = 7, State = 8, Fault = 9, Validity = 10, SwChecksum = 11;
        public const int RequestedPwm = 12, TargetPwm = 13, AppliedPwm = 14, WriteResult = 15;
        public const int FuseVoltage = 16, HBridgeTemperature = 17, HBridgeCurrent = 18;
        public const int MessagesReceived = 19, MessagesSent = 20;
        public const int FwVersionMajorMinor = 21, FwVersionPatchProtocol = 22;

        /// <summary>
        /// HR_TRACK_POWER (register 23): the dedicated, safety-gated neutral track-power request.
        /// This register is OUTSIDE the 23-register SLAVEINFO frame (it is only addressed via the
        /// point 108 EXEC_MBUS_SLAVE_DATA_EXCH write/read path), so <see cref="Count"/> stays 23.
        /// </summary>
        public const int TrackPower = 23;

        /// <summary>HR_TRACK_POWER bit 0 = TRACK_POWER_ENABLE. Bits 1..15 are reserved (must be 0).</summary>
        public const ushort TrackPowerEnableBit = 0x0001;

        // Existing consumer names now refer to the clean-sheet map, not the legacy addresses.
        public const int BemfControl = Control, Status = Validity, AmplifierStatus = State;
        public const int ConfigIdPwm = Address, AccelParams = ConfigRamp;
        public const ushort PwmMask = 0x03FF;
        public const ushort DirectionBit = 1 << 10, BrakeBit = 1 << 11;
        public const ushort StopRequestBit = 1 << 12, EmergencyStopBit = 1 << 15;
        public const ushort EnableBit = 1 << 15, MotionPermissionBit = 1 << 14;
        public const ushort ApplyConfigBit = 1 << 13, ClearFaultBit = 1 << 12;
        public const ushort ResetCountersBit = 1 << 11, DisableBit = 1 << 10;
        public const ushort EnableActiveBit = 1 << 8, InitDoneBit = 1 << 9, ConfigOkBit = 1 << 10;
        public const ushort StoppingActiveBit = 1 << 11, BrakeActiveBit = 1 << 12;
        public const ushort DirectionActiveBit = 1 << 13, IdSetBit = 1 << 14, InhibitedBit = 1 << 15;
        public const ushort ConfigInvalidFaultBit = 1 << 4, CommsLostFaultBit = 1 << 5;
        public const ushort LeaseExpiredFaultBit = 1 << 7;
        public const ushort OccupiedBit = 1 << 8, ThermalBit = 1 << 9, OverCurrentBit = 1 << 10;
        public const ushort AppliedBrakeBit = 1 << 10, AppliedDirectionBit = 1 << 11;
        public const ushort AppliedReadbackCapability = 1 << 12, RampCapability = 1 << 13;
        public const ushort WatchdogCapability = 1 << 14, TwoPhaseConfigCapability = 1 << 15;
        public const ushort WriteRetainedBit = 1 << 15;
        public const int LeaseExpiredState = 7;

        /// <summary>Shape check only. NOT identity authorization: requires PIC32 fresh-read evidence.</summary>
        public static bool HasCompatibleIdentityFields(ushort identity)
            => (identity & 0xFF) == 1 && ((identity >> 8) & 0xF) == 1 && (identity & 0xF000) != 0;

        /// <summary>Firmware MAJOR from the HR21 (major&lt;&lt;8 | minor) register.</summary>
        public static int FwMajor(ushort fwVersionMajorMinor) => (fwVersionMajorMinor >> 8) & 0xFF;

        /// <summary>Firmware MINOR from the HR21 (major&lt;&lt;8 | minor) register.</summary>
        public static int FwMinor(ushort fwVersionMajorMinor) => fwVersionMajorMinor & 0xFF;

        /// <summary>Firmware PATCH from the HR22 (patch&lt;&lt;8 | protocolVersion) register.</summary>
        public static int FwPatch(ushort fwVersionPatchProtocol) => (fwVersionPatchProtocol >> 8) & 0xFF;

        /// <summary>Wire PROTOCOL_VERSION from the HR22 (patch&lt;&lt;8 | protocolVersion) register.</summary>
        public static int FwProtocolVersion(ushort fwVersionPatchProtocol) => fwVersionPatchProtocol & 0xFF;

        /// <summary>Frozen Phase-1 wire PROTOCOL_VERSION (0x01): the base protocol without track power.</summary>
        public const ushort Phase1ProtocolVersion = 0x01;

        /// <summary>
        /// Wire PROTOCOL_VERSION (0x02) that a track-power-capable firmware advertises: it adds the
        /// HR_TRACK_POWER register (23). Any firmware at 0x01 is Phase-1 compatible but does NOT
        /// support track power, so Stage 6 must fail closed against it.
        /// </summary>
        public const ushort TrackPowerProtocolVersion = 0x02;

        /// <summary>
        /// True when HR22 reports a compatible Phase-1 PROTOCOL_VERSION (0x01 or 0x02). This is a
        /// SECONDARY compatibility check, meaningful only after the HR7 identity check has passed:
        /// a legacy 12-register device never reaches HR22 (it already fails HR7), and an
        /// absent/exception HR22 read (0x00) is UNKNOWN/INCOMPATIBLE, never "compatible by default".
        /// </summary>
        public static bool HasCompatibleProtocolVersion(ushort fwVersionPatchProtocol)
        {
            var protocol = FwProtocolVersion(fwVersionPatchProtocol);
            return protocol == Phase1ProtocolVersion || protocol == TrackPowerProtocolVersion;
        }

        /// <summary>
        /// True when the firmware advertises track-power support (PROTOCOL_VERSION &gt;= 0x02). A
        /// firmware at 0x01 (or an absent 0x00) does NOT support HR_TRACK_POWER and must be blocked
        /// from the Stage 6 track-power transition (fail-closed).
        /// </summary>
        public static bool SupportsTrackPower(ushort fwVersionPatchProtocol)
            => FwProtocolVersion(fwVersionPatchProtocol) >= TrackPowerProtocolVersion;

        /// <summary>True when the identity reports the applied-readback capability (HR7 bit 12).</summary>
        public static bool HasAppliedReadbackCapability(ushort identity)
            => (identity & AppliedReadbackCapability) != 0;

        public static bool IsOccupied(ushort[]? holdingRegisters)
            => holdingRegisters is { Length: > Validity } && (holdingRegisters[Validity] & OccupiedBit) != 0;
    }
}
