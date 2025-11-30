using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Information about a track amplifier.
    /// </summary>
    /// <remarks>
    /// This version no longer uses INotifyPropertyChanged or Fody.
    /// It is a plain data container used by the track application logic.
    /// </remarks>
    public class TrackAmplifierItem
    {
        #region Private fields

        private ushort _slaveNumber;
        private ushort[] _holdingReg = new ushort[12];
        private ushort _mbReceiveCounter;
        private ushort _slaveDetected;
        private ushort _spiCommErrorCounter;
        private ushort _mbExceptionCode;
        private uint _mbCommError;
        private ushort _mbSentCounter;

        #endregion

        #region Public properties

        /// <summary>
        /// Modbus slave number of this track amplifier.
        /// </summary>
        public ushort SlaveNumber
        {
            get => _slaveNumber;
            set => _slaveNumber = value;
        }

        /// <summary>
        /// Indicates if this slave has been detected by the controller.
        /// </summary>
        public ushort SlaveDetected
        {
            get => _slaveDetected;
            set => _slaveDetected = value;
        }

        /// <summary>
        /// Holding registers of this track amplifier.
        /// The backing array is kept alive; assigning copies into it.
        /// </summary>
        public ushort[] HoldingReg
        {
            get => _holdingReg;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                // Ensure internal array is large enough
                if (_holdingReg.Length != value.Length)
                    _holdingReg = new ushort[value.Length];

                Array.Copy(value, _holdingReg, value.Length);
            }
        }

        /// <summary>
        /// Number of Modbus messages received from this amplifier.
        /// </summary>
        public ushort MbReceiveCounter
        {
            get => _mbReceiveCounter;
            set => _mbReceiveCounter = value;
        }

        /// <summary>
        /// Number of Modbus messages sent to this amplifier.
        /// </summary>
        public ushort MbSentCounter
        {
            get => _mbSentCounter;
            set => _mbSentCounter = value;
        }

        /// <summary>
        /// Number of Modbus communication errors.
        /// </summary>
        public uint MbCommError
        {
            get => _mbCommError;
            set => _mbCommError = value;
        }

        /// <summary>
        /// Last Modbus exception code that occurred.
        /// </summary>
        public ushort MbExceptionCode
        {
            get => _mbExceptionCode;
            set => _mbExceptionCode = value;
        }

        /// <summary>
        /// SPI communication error counter between controller and amplifier.
        /// </summary>
        public ushort SpiCommErrorCounter
        {
            get => _spiCommErrorCounter;
            set => _spiCommErrorCounter = value;
        }

        #endregion
    }
}
