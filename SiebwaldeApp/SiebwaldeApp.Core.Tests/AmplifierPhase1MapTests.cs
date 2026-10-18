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
