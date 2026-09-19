namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Register and bit definitions for the track amplifier ModBus holding registers,
    /// mirroring TrackAmplifier4.X/modbus/General.h.
    /// NOTE: the register description is provisional and may change with new firmware.
    /// </summary>
    public static class TrackAmplifierRegisters
    {
        /// <summary>HoldingReg0: PWM command word.</summary>
        public const int PwmCommand = 0;

        /// <summary>HoldingReg1: BEMF speed / control word.</summary>
        public const int BemfControl = 1;

        /// <summary>HoldingReg2: status / feedback (BEMF, occupied, thermal, overcurrent).</summary>
        public const int Status = 2;

        /// <summary>HoldingReg3: amplifier status word.</summary>
        public const int AmplifierStatus = 3;

        /// <summary>HoldingReg4: H-bridge fuse voltage.</summary>
        public const int FuseVoltage = 4;

        /// <summary>HoldingReg5: H-bridge temperature.</summary>
        public const int HBridgeTemperature = 5;

        /// <summary>HoldingReg6: H-bridge current.</summary>
        public const int HBridgeCurrent = 6;

        /// <summary>HoldingReg7: messages received from master.</summary>
        public const int MessagesReceived = 7;

        /// <summary>HoldingReg8: messages sent to master.</summary>
        public const int MessagesSent = 8;

        /// <summary>HoldingReg9: amplifier ID, PWM mode, reset.</summary>
        public const int ConfigIdPwm = 9;

        /// <summary>HoldingReg10: acceleration / deceleration parameters.</summary>
        public const int AccelParams = 10;

        /// <summary>HoldingReg11: firmware checksum.</summary>
        public const int SwChecksum = 11;

        /// <summary>HR_STATUS bit 10: track occupied flag.</summary>
        public const ushort OccupiedBit = 1 << 10;

        /// <summary>HR_STATUS bit 11: H-bridge thermal flag.</summary>
        public const ushort ThermalBit = 1 << 11;

        /// <summary>HR_STATUS bit 12: overcurrent detected.</summary>
        public const ushort OverCurrentBit = 1 << 12;

        /// <summary>HR_STATUS bit 13: amplifier ID programmed by master.</summary>
        public const ushort IdSetBit = 1 << 13;

        /// <summary>Returns true when the status register reports the track as occupied.</summary>
        public static bool IsOccupied(ushort[]? holdingRegisters)
            => holdingRegisters is { Length: > Status } &&
               (holdingRegisters[Status] & OccupiedBit) != 0;
    }
}
