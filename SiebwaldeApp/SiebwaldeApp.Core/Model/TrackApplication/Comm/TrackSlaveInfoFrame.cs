using System;
using System.IO;

namespace SiebwaldeApp.Core
{
    /// <summary>Register-count-derived SLAVEINFO envelope. Does not establish FC03 freshness.</summary>
    public static class TrackSlaveInfoFrame
    {
        public const int RegisterOffset = 6;

        // --------------------------------------------------------------------
        // PHASE-1 23-register SLAVEINFO (the deployed PIC32 1.0.1 firmware,
        // PetitModbus.h with NUMBER_OF_HOLDING_PETITREGISTERS = 23 and the 4-byte
        // SLAVE_DATA enum; XC32 without -fshort-enums). mbus.c memcpy's the inner
        // SLAVE_INFO struct into the udpTrans_t data region at wire offset 2, and the
        // full datagram is sizeof(udpTrans_t) = 82 bytes (header + command + data[80]).
        //
        // Inner SLAVE_INFO struct-relative layout (add +2 for the wire offset):
        //   Header(1) SlaveNumber(1) SlaveDetected(1) pad(1)              -> 0..3
        //   HoldingReg[0..22] (23 x uint16 LE)                            -> 4..49
        //   MbReceiveCounter(2) MbSentCounter(2)                          -> 50..53
        //   pad(2) — aligns the 4-byte SLAVE_DATA enum                    -> 54..55
        //   MbCommError (uint32 LE) MbExceptionCode(1) SpiCommErrorCounter(1) Footer(1)
        //                                                                 -> 56..62
        // Wire offsets (SLAVE_INFO starts at 2): footer at 64, MbCommError at 58,
        // MbExceptionCode at 62, SpiCommErrorCounter at 63, HoldingReg at 6..51
        // (HR7 @20, HR11 @28, HR21 @48, HR22 @50 — confirmed by physical readback).
        // The real 82-byte datagram carries trailing garbage at 65..81.
        // --------------------------------------------------------------------
        public const int MbReceiveCounterOffset = RegisterOffset + 2 * TrackAmplifierRegisters.Count; // 52
        public const int MbSentCounterOffset = MbReceiveCounterOffset + 2;                             // 54
        public const int MbCommErrorOffset = MbSentCounterOffset + 4;                                  // 58 (2-byte pad + enum)
        public const int MbExceptionCodeOffset = MbCommErrorOffset + 4;                                // 62
        public const int SpiCommErrorCounterOffset = MbExceptionCodeOffset + 1;                        // 63
        public const int FooterOffset = SpiCommErrorCounterOffset + 1;                                 // 64
        public const int Length = FooterOffset + 1;                                                    // 65

        // --------------------------------------------------------------------
        // LEGACY 12-register SLAVEINFO (the previously deployed PIC32 firmware, PetitModbus.h
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
            writer.Write(received);      // MbReceiveCounter @52
            writer.Write(sent);          // MbSentCounter    @54
            writer.Write((ushort)0);     // 2-byte alignment padding @56..57 (before the 4-byte enum)
            writer.Write(error);         // MbCommError (uint32 LE) @58
            writer.Write(exception);     // MbExceptionCode @62
            writer.Write(spiError);      // SpiCommErrorCounter @63
            writer.Write((byte)0x55);    // Footer @64
            return stream.ToArray();     // length 65
        }
    }
}
