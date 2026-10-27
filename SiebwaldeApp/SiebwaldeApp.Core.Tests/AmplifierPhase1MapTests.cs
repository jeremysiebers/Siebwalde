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
            Assert.Equal(65, frame.Length);
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

        [Fact]
        public void Phase1_82ByteFrame_DecodesRealPic32Layout()
        {
            // Real PIC32 1.0.1 82-byte SLAVEINFO datagram (udpTrans_t = header + command +
            // data[80]). The 23-register SLAVE_INFO carries a 4-byte SLAVE_DATA enum, so the
            // wire layout has 2 alignment pad bytes before MbCommError and the footer sits at
            // offset 64 (not 62). HR7/HR11/HR21/HR22 land at 20/28/48/50.
            var frame = new byte[82];
            frame[0] = 0xAA;               // udpTrans.header
            frame[1] = 0xFF;               // SLAVEINFO command
            frame[2] = 0x00;               // SLAVE_INFO.Header (patched below)
            frame[3] = 1;                  // SlaveNumber
            frame[4] = 1;                  // SlaveDetected
            frame[5] = 0x00;               // alignment pad
            WriteU16(frame, 20, 0xF101);   // HR7  @20 (identity)
            WriteU16(frame, 28, 0x251F);   // HR11 @28 (checksum)
            WriteU16(frame, 48, 0x0100);   // HR21 @48 (fw 1.0)
            WriteU16(frame, 50, 0x0001);   // HR22 @50 (patch 0, protocol 1)
            WriteU16(frame, 52, 123);      // MbReceiveCounter
            WriteU16(frame, 54, 456);      // MbSentCounter
            // 56..57 = 2-byte alignment padding (stays 0)
            WriteU32(frame, 58, 0x11223344); // MbCommError (uint32 LE)
            frame[62] = 5;                 // MbExceptionCode
            frame[63] = 6;                 // SpiCommErrorCounter
            frame[64] = 0x55;              // Footer
            for (int i = 65; i < frame.Length; i++) frame[i] = 0xEE; // trailing garbage

            Assert.Equal(0x55, frame[TrackSlaveInfoFrame.FooterOffset]);

            // Corrupting the inner header proves the byte-layout assumption is the one decoded.
            frame[2] = 0xAA;

            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(new UnusedTransport(), variables);
            Parse(client, frame);

            TrackAmplifierItem item = variables.trackAmpItems[1];
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
            // Compatible identity + protocol: no fail-closed block.
            Assert.Null(item.Phase1Failure);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(41)]
        [InlineData(58)]
        [InlineData(60)]
        [InlineData(64)]
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
        [InlineData(64)]
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
        public void Legacy12RegisterFrame_DecodesDeployedLayout()
        {
            // Real captured 82-byte legacy SLAVEINFO frame (slave 1) from the deployed PIC32
            // 12-register firmware. Footer must sit at wire offset 40, not 62.
            byte[] frame =
            {
                0xAA, 0xFF, 0xAA, 0x01, 0x00, 0x00, 0x8F, 0x01, 0x00, 0x80, 0x00, 0x2A,
                0x00, 0x00, 0x49, 0x02, 0x37, 0x00, 0x0C, 0x00, 0x49, 0x66, 0x3F, 0x66,
                0x00, 0x00, 0x00, 0x00, 0x1F, 0x25, 0x48, 0x66, 0x84, 0x66, 0x02, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x55, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x88, 0xFF, 0x07,
                0x80, 0xB8, 0x12, 0x01, 0x80, 0x00, 0x00, 0x00, 0x00, 0x20, 0x12, 0x01,
                0x80, 0x20, 0x12, 0x01, 0x80, 0x01, 0x00, 0x00, 0x25, 0x00
            };
            Assert.Equal(82, frame.Length);
            Assert.Equal(0x55, frame[TrackSlaveInfoFrame.LegacyFooterOffset]);

            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(new UnusedTransport(), variables);
            Parse(client, frame);

            TrackAmplifierItem item = variables.trackAmpItems[1];
            Assert.Equal((ushort)0, item.SlaveDetected);       // legacy reports 0 while polled
            Assert.Equal((ushort)0x018F, item.HoldingReg[0]); // HR0 neutral PWM
            Assert.Equal((ushort)0x6649, item.HoldingReg[7]); // HR7 identity (amp1)
            Assert.Equal((ushort)0x251F, item.HoldingReg[11]); // HR11 legacy checksum
            Assert.Equal(2u, item.MbCommError);                 // SLAVE_DATA_OK
            Assert.True(item.ProtocolReadbackObserved);
            // HR21/HR22 do not exist in the 12-register layout: never decoded as a version.
            Assert.Equal((ushort)0, item.HoldingReg[TrackAmplifierRegisters.FwVersionMajorMinor]);
            Assert.Equal((ushort)0, item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol]);
            // Legacy identity (0x6649) fails the Phase-1 shape check -> fail-closed.
            Assert.Equal(AmplifierPhase1Safety.IncompatibleFirmware, item.Phase1Failure);
        }

        [Fact]
        public void Legacy12RegisterFrame_WrongFooter_DoesNotEstablishReadback()
        {
            byte[] frame =
            {
                0xAA, 0xFF, 0xAA, 0x01, 0x00, 0x00, 0x8F, 0x01, 0x00, 0x80, 0x00, 0x2A,
                0x00, 0x00, 0x49, 0x02, 0x37, 0x00, 0x0C, 0x00, 0x49, 0x66, 0x3F, 0x66,
                0x00, 0x00, 0x00, 0x00, 0x1F, 0x25, 0x48, 0x66, 0x84, 0x66, 0x02, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x88, 0xFF, 0x07,
                0x80, 0xB8, 0x12, 0x01, 0x80, 0x00, 0x00, 0x00, 0x00, 0x20, 0x12, 0x01,
                0x80, 0x20, 0x12, 0x01, 0x80, 0x01, 0x00, 0x00, 0x25, 0x00
            };
            frame[TrackSlaveInfoFrame.LegacyFooterOffset] = 0x00; // corrupt footer at byte 40

            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(new UnusedTransport(), variables);
            Parse(client, frame);

            Assert.False(variables.trackAmpItems[1].ProtocolReadbackObserved);
        }


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

        private static void Parse(TrackCommClientAsync client, byte[] frame)
            => typeof(TrackCommClientAsync).GetMethod("HandleNewData", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(client, new object[] { frame });

        private static void WriteU16(byte[] frame, int offset, ushort value)
        {
            frame[offset] = (byte)(value & 0xFF);
            frame[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteU32(byte[] frame, int offset, uint value)
        {
            frame[offset] = (byte)(value & 0xFF);
            frame[offset + 1] = (byte)((value >> 8) & 0xFF);
            frame[offset + 2] = (byte)((value >> 16) & 0xFF);
            frame[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

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
