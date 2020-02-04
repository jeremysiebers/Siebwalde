using System;

namespace Siebwalde_Application
{
    /// <summary>
    /// Information about a Trackamplifier
    /// </summary>
    public class TrackAmplifierItem
    {
        public ushort SlaveNumber { get; set; }

        public ushort SlaveDetected { get; set; }

        public ushort[] HoldingReg { get; set; }

        public ushort MbReceiveCounter { get; set; }

        public ushort MbSentCounter { get; set; }

        public UInt32 MbCommError { get; set; }

        public ushort MbExceptionCode { get; set; }

        public ushort SpiCommErrorCounter { get; set; }
    }

    public struct TrackAmpItem
    {
        public ushort SlaveNumber, SlaveDetected, MbReceiveCounter, MbSentCounter, MbExceptionCode, SpiCommErrorCounter;
        public UInt32 MbCommError;
        public ushort[] HoldingReg;

        public TrackAmpItem(ushort SlaveNumber, ushort SlaveDetected, ushort[] HoldingReg, ushort MbReceiveCounter, ushort MbSentCounter, UInt32 MbCommError, ushort MbExceptionCode, ushort SpiCommErrorCounter)
        {
            this.SlaveNumber = SlaveNumber;
            this.SlaveDetected = SlaveDetected;
            this.HoldingReg = HoldingReg;
            this.MbReceiveCounter = MbReceiveCounter;
            this.MbSentCounter = MbSentCounter;
            this.MbCommError = MbCommError;
            this.MbExceptionCode = MbExceptionCode;
            this.SpiCommErrorCounter = SpiCommErrorCounter;
        }
    }
}
