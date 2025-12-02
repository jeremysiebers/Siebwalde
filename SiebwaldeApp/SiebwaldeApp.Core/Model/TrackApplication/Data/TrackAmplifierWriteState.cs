namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Holds a single pending write request for a track amplifier.
    /// 
    /// For now we support writing 1 or 2 consecutive holding registers.
    /// The track main loop will send this to the Modbus master using
    /// the EXEC_MBUS_SLAVE_DATA_EXCH command when HasPendingWrite is true.
    /// </summary>
    public struct TrackAmplifierWriteState
    {
        /// <summary>
        /// True when there is a pending write that has not been sent yet.
        /// </summary>
        public bool HasPendingWrite;

        /// <summary>
        /// First holding register index to write (0..11).
        /// </summary>
        public byte StartRegister;

        /// <summary>
        /// Number of registers to write (1 or 2).
        /// </summary>
        public byte RegisterCount;

        /// <summary>
        /// First register value (HoldingReg[StartRegister]).
        /// </summary>
        public ushort Register0;

        /// <summary>
        /// Second register value (HoldingReg[StartRegister+1]) if RegisterCount == 2.
        /// Unused when RegisterCount == 1.
        /// </summary>
        public ushort Register1;
    }
}
