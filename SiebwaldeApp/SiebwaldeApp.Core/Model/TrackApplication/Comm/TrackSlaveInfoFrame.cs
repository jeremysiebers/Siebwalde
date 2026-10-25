using System;
using System.IO;

namespace SiebwaldeApp.Core
{
    /// <summary>Register-count-derived SLAVEINFO envelope. Does not establish FC03 freshness.</summary>
    public static class TrackSlaveInfoFrame
    {
        public const int RegisterOffset = 6;
        public const int TrailerOffset = RegisterOffset + 2 * TrackAmplifierRegisters.Count;
        public const int Length = TrailerOffset + 11;

        // --------------------------------------------------------------------
        // LEGACY 12-register SLAVEINFO (the DEPLOYED PIC32 firmware, PetitModbus.h
        // with NUMBER_OF_HOLDING_PETITREGISTERS = 12). Wire layout of the 82-byte
        // UDP frame (udpTrans header + command + 80-byte data buffer):
        //   [0] udpTrans.header = 0xAA      [1] udpTrans.command = SLAVEINFO (0xFF)
        //   [2] SLAVE_INFO.Header = 0xAA     [3] SlaveNumber
        //   [4] SlaveDetected                [5] alignment padding
        //   [6..29]  HoldingReg[0..11] (12 x uint16 little-endian)
        //   [30..31] MbReceiveCounter        [32..33] MbSentCounter
        //   [34..37] MbCommError (uint32 LE) [38] MbExceptionCode
        //   [39] SpiCommErrorCounter         [40] Footer = 0x55
        //   [41..81] uninitialized trailing garbage.
        // Distinct from the Phase-1 23-register layout produced by Encode().
        // --------------------------------------------------------------------
        public const int LegacyRegisterCount = 12;
        public const int LegacyRegisterOffset = 6;
        public const int LegacyMbReceiveCounterOffset = 30;
        public const int LegacyMbSentCounterOffset = 32;
        public const int LegacyMbCommErrorOffset = 34;
        public const int LegacyMbExceptionCodeOffset = 38;
        public const int LegacySpiCommErrorCounterOffset = 39;
        public const int LegacyFooterOffset = 40;
        public const int LegacyMinLength = 41; // indices 0..40 inclusive

        public static byte[] Encode(byte slave, byte detected, ushort[] registers,
            ushort received = 0, ushort sent = 0, uint error = 0, byte exception = 0, byte spiError = 0)
        {
            if (registers.Length != TrackAmplifierRegisters.Count)
                throw new ArgumentException("Phase-1 requires exactly 23 registers.", nameof(registers));
            using var stream = new MemoryStream(Length);
            using var writer = new BinaryWriter(stream);
            writer.Write(new byte[] { 0xAA, 0xFF, 0xAA, slave, detected, 0 });
            foreach (var value in registers) writer.Write(value);
            writer.Write(received);
            writer.Write(sent);
            writer.Write(error);
            writer.Write(exception);
            writer.Write(spiError);
            writer.Write((byte)0x55);
            return stream.ToArray();
        }
    }
}
