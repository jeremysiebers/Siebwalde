using System;
using System.Threading;

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
        private ushort[] _holdingReg = new ushort[TrackAmplifierRegisters.Count];
        public long ProtocolReadbackSequence { get; set; }
        public bool ConfigurationVerified { get; set; }
        public string? Phase1Failure { get; set; }
        private ushort _mbReceiveCounter;
        private ushort _slaveDetected;
        private ushort _spiCommErrorCounter;
        private ushort _mbExceptionCode;
        private uint _mbCommError;
        private ushort _mbSentCounter;

        /// <summary>
        /// Sentinel for "no frame received yet". A <see cref="DateTimeOffset"/> tick count is always
        /// non-negative, so a negative sentinel cannot collide with any real timestamp.
        /// </summary>
        private const long NoDataTicks = long.MinValue;

        /// <summary>
        /// Monotonic <see cref="DateTimeOffset.UtcTicks"/> of the last parsed frame, stored as a
        /// single 64-bit value so a concurrent reader can never observe a torn timestamp.
        /// <see cref="NoDataTicks"/> represents "no frame received yet" (null).
        /// </summary>
        private long _lastDataReceivedUtcTicks = NoDataTicks;

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
        /// UTC time at which the last amplifier frame for this item was parsed, or null when no
        /// frame has been received yet.
        ///
        /// This is the only freshness signal for the amplifier data: <see cref="SlaveDetected"/>
        /// and <see cref="HoldingReg"/> are written once and are never cleared, and the comm
        /// client keeps republishing the cached container, so neither of those proves that the
        /// data is current.
        ///
        /// The value is backed by a single 64-bit tick count (<see cref="_lastDataReceivedUtcTicks"/>)
        /// read and written with <see cref="Volatile.Read(ref long)"/>/<see cref="Volatile.Write(ref long, long)"/>
        /// so a concurrent reader can never observe a torn <see cref="DateTimeOffset"/> (the comm-client
        /// receive loop writes this value while the downloader/monitor read it without synchronization).
        /// </summary>
        public DateTimeOffset? LastDataReceivedUtc
        {
            get
            {
                long ticks = Volatile.Read(ref _lastDataReceivedUtcTicks);
                return ticks == NoDataTicks ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
            }
            set
            {
                Volatile.Write(ref _lastDataReceivedUtcTicks,
                    value is DateTimeOffset received ? received.UtcTicks : NoDataTicks);
            }
        }

        /// <summary>
        /// True only when the current <see cref="HoldingReg"/> content was populated by a genuine
        /// protocol SLAVEINFO readback in this runtime instance.
        ///
        /// This is the explicit evidence that separates "a register value is present" from "the
        /// register value was actually received via protocol". <see cref="LastDataReceivedUtc"/>
        /// alone proves a frame was parsed, not that the current <see cref="HoldingReg"/> values
        /// are that frame: a local default write (for example
        /// <c>InitializeDefaultPwmSetpoints</c>) overwrites the register values without touching
        /// the timestamp, so a stale timestamp could otherwise make a locally-written default look
        /// like a fresh protocol observation.
        ///
        /// The flag is stamped only by the comm client when it parses a SLAVEINFO frame, and is
        /// cleared whenever the register content is overwritten outside the protocol path. A
        /// neutral observer must require it (together with freshness) instead of trusting a
        /// timestamp that could belong to an unrelated frame.
        /// </summary>
        public bool ProtocolReadbackObserved { get; set; }

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
