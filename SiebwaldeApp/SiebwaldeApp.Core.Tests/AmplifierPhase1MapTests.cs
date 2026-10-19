using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class AmplifierPhase1MapTests
    {
        [Fact]
        public void SlaveInfoRoundTrip_PreservesAll23RegistersAndTrailer()
        {
            var registers = Enumerable.Range(0, TrackAmplifierRegisters.Count)
                .Select(i => (ushort)(0x1000 + i)).ToArray();
            var frame = TrackSlaveInfoFrame.Encode(3, 1, registers, 123, 456, 0x11223344, 5, 6);
            Assert.Equal(63, frame.Length);
            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(new UnusedTransport(), variables);
            Parse(client, frame);
            var item = variables.trackAmpItems[3];
            Assert.Equal(registers, item.HoldingReg);
            Assert.Equal(123, item.MbReceiveCounter);
            Assert.Equal(456, item.MbSentCounter);
            Assert.Equal(0x11223344u, item.MbCommError);
            Assert.Equal(5, item.MbExceptionCode);
            Assert.Equal(6, item.SpiCommErrorCounter);
            Assert.True(item.ProtocolReadbackObserved);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(41)]
        [InlineData(58)]
        [InlineData(60)]
        public void WrongLength_DoesNotEstablishReadback(int length)
        {
            var frame = new byte[length];
            if (length > 0) frame[0] = 0xAA;
            if (length > 1) frame[1] = 0xFF;
            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(new UnusedTransport(), variables);
            Parse(client, frame);
            Assert.All(variables.trackAmpItems, item => Assert.False(item.ProtocolReadbackObserved));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(62)]
        public void WrongEnvelope_DoesNotEstablishReadback(int offset)
        {
            var frame = TrackSlaveInfoFrame.Encode(1, 1, new ushort[23]);
            frame[offset] = 0;
            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(new UnusedTransport(), variables);
            Parse(client, frame);
            Assert.False(variables.trackAmpItems[1].ProtocolReadbackObserved);
        }

        [Theory]
        [InlineData(0x0000, false)]
        [InlineData(0x0101, false)]
        [InlineData(0xF102, false)]
        [InlineData(0xF201, false)]
        [InlineData(0xF101, true)]
        [InlineData(0x1101, true)] // Legacy counter can mimic these fields: NOT authorization.
        public void IdentityShapeCheck_IsNotAFreshReadAuthorization(int value, bool expected)
            => Assert.Equal(expected, TrackAmplifierRegisters.HasCompatibleIdentityFields((ushort)value));

        [Fact]
        public void DefaultRegisterArrays_Are23AndNotSharedBetweenAmplifiers()
        {
            var variables = new TrackApplicationVariables();
            Assert.All(variables.trackAmpItems, item => Assert.Equal(23, item.HoldingReg.Length));
            variables.trackAmpItems[1].HoldingReg[22] = 42;
            Assert.Equal(0, variables.trackAmpItems[2].HoldingReg[22]);
            Assert.Equal(23, new TrackAmplifierItem().HoldingReg.Length);
        }

        [Fact]
        public void Encoder_RejectsLegacyRegisterArray()
            => Assert.Throws<ArgumentException>(() => TrackSlaveInfoFrame.Encode(1, 1, new ushort[12]));

        [Fact]
        public void ControllerVersionReply_SurfacesVersionWithoutGating()
        {
            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(new UnusedTransport(), variables);

            // sender=CONTROLLER(10), data[0]=echo 113, data[1..3]=MAJOR/MINOR/PATCH,
            // data[4]=PROTOCOL_VERSION, data[5]=0, data[6..N]=build date.
            var frame = new List<byte> { 0xAA, TaskId.CONTROLLER, TrackCommand.GET_CONTROLLER_VERSION, 1, 0, 0, 1, 0 };
            frame.AddRange(System.Text.Encoding.ASCII.GetBytes("Oct 17 2026 12:34:56"));
            frame.Add(0);
            Parse(client, frame.ToArray());

            var version = variables.trackControllerCommands.ControllerVersion;
            Assert.NotNull(version);
            Assert.Equal(1, version!.Value.Major);
            Assert.Equal(0, version.Value.Minor);
            Assert.Equal(0, version.Value.Patch);
            Assert.Equal(1, version.Value.ProtocolVersion);
            Assert.Equal("Oct 17 2026 12:34:56", version.Value.BuildDate);
            Assert.Equal("1.0.0", version.Value.ToString());
        }

        [Fact]
        public void Firmware82ByteFrame_ParsesIdentityVersionAndTrailerAtCorrectOffsets()
        {
            var registers = new ushort[TrackAmplifierRegisters.Count];
            registers[TrackAmplifierRegisters.Identity] = 0xF101;               // HR7
            registers[TrackAmplifierRegisters.SwChecksum] = 0x251F;             // HR11 (legacy)
            registers[TrackAmplifierRegisters.FwVersionMajorMinor] = 0x0100;    // HR21
            registers[TrackAmplifierRegisters.FwVersionPatchProtocol] = 0x0001; // HR22

            var frame = BuildFirmwareSlaverInfoFrame(1, 1, registers, received: 123, sent: 456,
                error: 0x11223344u, exception: 5, spiError: 6);

            // Byte-exact structural assertions: header/sender at 0..1, SLAVE_INFO.Header at 2,
            // 2-byte alignment padding at 56..57, and the footer at offset 64.
            Assert.Equal(82, frame.Length);
            Assert.Equal(0xAA, frame[0]);
            Assert.Equal(0xFF, frame[1]);
            Assert.Equal(0xAA, frame[2]);
            Assert.Equal(0x55, frame[64]);

            var variables = new TrackApplicationVariables();
            Parse(new TrackCommClientAsync(new UnusedTransport(), variables), frame);
            var item = variables.trackAmpItems[1];

            Assert.Equal((ushort)0xF101, item.HoldingReg[TrackAmplifierRegisters.Identity]);
            Assert.Equal((ushort)0x251F, item.HoldingReg[TrackAmplifierRegisters.SwChecksum]);
            Assert.Equal((ushort)0x0100, item.HoldingReg[TrackAmplifierRegisters.FwVersionMajorMinor]);
            Assert.Equal((ushort)0x0001, item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol]);
            Assert.Equal(123, item.MbReceiveCounter);
            Assert.Equal(456, item.MbSentCounter);
            Assert.Equal(0x11223344u, item.MbCommError);
            Assert.Equal(5, item.MbExceptionCode);
            Assert.Equal(6, item.SpiCommErrorCounter);
            Assert.True(item.ProtocolReadbackObserved);
        }

        [Fact]
        public void Firmware82ByteFrame_AndPacked63ByteFrame_ParseIdentically()
        {
            var registers = new ushort[TrackAmplifierRegisters.Count];
            registers[TrackAmplifierRegisters.Identity] = 0xF101;
            registers[TrackAmplifierRegisters.SwChecksum] = 0xE906;
            registers[TrackAmplifierRegisters.FwVersionMajorMinor] = 0x0100;
            registers[TrackAmplifierRegisters.FwVersionPatchProtocol] = 0x0001;

            var firmwareFrame = BuildFirmwareSlaverInfoFrame(1, 1, registers, 123, 456, 0x11223344u, 5, 6);
            var packedFrame = TrackSlaveInfoFrame.Encode(1, 1, registers, 123, 456, 0x11223344u, 5, 6);
            Assert.Equal(82, firmwareFrame.Length);
            Assert.Equal(63, packedFrame.Length);

            var fwVariables = new TrackApplicationVariables();
            Parse(new TrackCommClientAsync(new UnusedTransport(), fwVariables), firmwareFrame);
            var packedVariables = new TrackApplicationVariables();
            Parse(new TrackCommClientAsync(new UnusedTransport(), packedVariables), packedFrame);

            var fw = fwVariables.trackAmpItems[1];
            var packed = packedVariables.trackAmpItems[1];
            Assert.Equal(packed.HoldingReg, fw.HoldingReg);
            Assert.Equal(packed.MbReceiveCounter, fw.MbReceiveCounter);
            Assert.Equal(packed.MbSentCounter, fw.MbSentCounter);
            Assert.Equal(packed.MbCommError, fw.MbCommError);
            Assert.Equal(packed.MbExceptionCode, fw.MbExceptionCode);
            Assert.Equal(packed.SpiCommErrorCounter, fw.SpiCommErrorCounter);
        }

        /// <summary>
        /// Builds the real firmware SLAVEINFO envelope byte-exactly: the 82-byte udpTrans_t
        /// (header, command, data[80]) with the SLAVE_INFO struct memcpy'd into data[]. Layout:
        /// [0]=HEADER, [1]=SLAVEINFO, [2]=SLAVE_INFO.Header, [3]=slave, [4]=detected,
        /// [5]=padding, [6..51]=23 registers (LE), [52..55]=receive/sent counters,
        /// [56..57]=2-byte alignment padding before MbCommError, [58..61]=MbCommError (LE uint32),
        /// [62]=exception, [63]=spiError, [64]=FOOTER, [65..81]=unused.
        /// </summary>
        private static byte[] BuildFirmwareSlaverInfoFrame(byte slave, byte detected, ushort[] registers,
            ushort received, ushort sent, uint error, byte exception, byte spiError)
        {
            var frame = new byte[82];
            frame[0] = 0xAA; // HEADER
            frame[1] = 0xFF; // SLAVEINFO
            frame[2] = 0xAA; // SLAVE_INFO.Header (mbHeader)
            frame[3] = slave;
            frame[4] = detected;
            frame[5] = 0x00; // alignment padding before HoldingReg
            for (int i = 0; i < registers.Length; i++)
            {
                frame[6 + 2 * i] = (byte)(registers[i] & 0xFF);
                frame[6 + 2 * i + 1] = (byte)(registers[i] >> 8);
            }
            frame[52] = (byte)(received & 0xFF);
            frame[53] = (byte)(received >> 8);
            frame[54] = (byte)(sent & 0xFF);
            frame[55] = (byte)(sent >> 8);
            // [56..57] = 2-byte alignment padding before the 4-byte MbCommError (left zero).
            frame[58] = (byte)(error & 0xFF);
            frame[59] = (byte)((error >> 8) & 0xFF);
            frame[60] = (byte)((error >> 16) & 0xFF);
            frame[61] = (byte)((error >> 24) & 0xFF);
            frame[62] = exception;
            frame[63] = spiError;
            frame[64] = 0x55; // FOOTER
            return frame;
        }

        private static void Parse(TrackCommClientAsync client, byte[] frame)
            => typeof(TrackCommClientAsync).GetMethod("HandleNewData", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(client, new object[] { frame });

        private sealed class UnusedTransport : ITrackTransport
        {
            public System.Threading.Tasks.Task OpenAsync(System.Threading.CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
            public System.Threading.Tasks.Task CloseAsync(System.Threading.CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
            public System.Threading.Tasks.Task SendAsync(byte[] buffer, int offset, int count,
                System.Threading.CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public System.Collections.Generic.IAsyncEnumerable<byte[]> ReceiveAsync(
                System.Threading.CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public System.Threading.Tasks.ValueTask DisposeAsync() => default;
        }
    }
}
