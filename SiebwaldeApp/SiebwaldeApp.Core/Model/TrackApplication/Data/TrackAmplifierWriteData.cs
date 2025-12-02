namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Holds pending write data for a single track amplifier.
    /// 
    /// For now we only support HoldingReg0 (PWM setpoint + EmoStop bit),
    /// but this class can be extended to include more registers later.
    /// </summary>
    public sealed class TrackAmplifierWriteData
    {
        /// <summary>
        /// Modbus slave number (1..50 for track amplifiers).
        /// </summary>
        public ushort SlaveNumber { get; }

        /// <summary>
        /// True when HoldingReg0 has a new value that must be sent to the slave.
        /// </summary>
        public bool HasPendingHr0 { get; private set; }

        /// <summary>
        /// Desired value for HoldingReg0.
        /// Bits 0..9 : PWM setpoint (0..799)
        /// Bit 15    : EmoStop (1 = stop as fast as possible)
        /// </summary>
        public ushort Hr0Value { get; private set; }

        public TrackAmplifierWriteData(ushort slaveNumber)
        {
            SlaveNumber = slaveNumber;
        }

        /// <summary>
        /// Updates the desired value for HoldingReg0.
        /// Marks a write as pending only if the value actually changed.
        /// </summary>
        public void SetDesiredHr0(ushort value)
        {
            if (Hr0Value != value)
            {
                Hr0Value = value;
                HasPendingHr0 = true;
            }
        }

        /// <summary>
        /// Consumes the pending HoldingReg0 write if present.
        /// Returns true if a new value was available and resets the pending flag.
        /// </summary>
        public bool TryConsumeHr0(out ushort value)
        {
            value = Hr0Value;

            if (!HasPendingHr0)
                return false;

            HasPendingHr0 = false;
            return true;
        }
    }
}
