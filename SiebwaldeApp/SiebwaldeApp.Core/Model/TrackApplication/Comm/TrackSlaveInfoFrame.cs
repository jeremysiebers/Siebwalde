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

        public static byte[] Encode(byte slave, byte detected, ushort[] registers,
            ushort received = 0, ushort sent = 0, uint error = 0, byte exception = 0, byte spiError = 0)
        {
            if (registers.Length != TrackAmplifierRegisters.Count)
                throw new ArgumentException("Phase-1 requires exactly 21 registers.", nameof(registers));
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
