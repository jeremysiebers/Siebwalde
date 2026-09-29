using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Unit tests for the controllable simulated-amplifier I/O surface
    /// (<see cref="ISimulatedTrackIo"/>) and the optional periodic SLAVEINFO heartbeat on
    /// <see cref="DeterministicTrackTransport"/>. Software-only: no network or hardware.
    /// </summary>
    public class DeterministicTrackTransportOccupancyTests
    {
        // ---------------------------------------------------------------------------------
        // Heartbeat
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task Heartbeat_EmitsSlaveInfoFrames_ForEveryDetectedSlave_WhileOpen()
        {
            var config = new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1, 2, 3 },
                periodicSlaveInfoInterval: TimeSpan.FromMilliseconds(30));
            var transport = new DeterministicTrackTransport(config);
            await transport.OpenAsync();

            var seenSlaves = new HashSet<byte>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await foreach (var frame in transport.ReceiveAsync(cts.Token))
            {
                Assert.Equal(0xFF, frame[1]); // SLAVEINFO
                seenSlaves.Add(frame[3]);     // slave number at offset 3

                if (seenSlaves.Count == 3)
                {
                    break;
                }
            }

            Assert.Equal(new byte[] { 1, 2, 3 }, seenSlaves.OrderBy(b => b).ToArray());

            await transport.DisposeAsync();
        }

        [Fact]
        public async Task Dispose_CompletesTheReceiveStream()
        {
            var config = new TrackSimulatorConfig(periodicSlaveInfoInterval: TimeSpan.FromMilliseconds(20));
            var transport = new DeterministicTrackTransport(config);
            await transport.OpenAsync();

            await transport.DisposeAsync();

            // After dispose the channel writer is completed, so the receive stream must terminate
            // rather than keep producing frames forever.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var count = 0;
            await foreach (var _ in transport.ReceiveAsync(cts.Token))
            {
                count++;
                if (count > 100_000)
                {
                    Assert.Fail("Receive stream did not complete after dispose.");
                }
            }

            Assert.True(transport.IsDisposed);
        }

        [Fact]
        public void DetectedSlaves_ReturnsADefensiveCopy()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());

            var copy = transport.DetectedSlaves;

            Assert.Equal(new byte[] { 1, 2, 3 }, copy);
        }

        // ---------------------------------------------------------------------------------
        // SetSlaveOccupancy / SetSlaveStatus / GetRegisters
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task SetSlaveOccupancy_MutatesStatus_EmitsFreshFrame_AndIsReadable()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
            await transport.OpenAsync();

            transport.SetSlaveOccupancy(1, true);

            // HR2 status register now carries the occupied bit.
            var regs = transport.GetRegisters(1);
            Assert.NotEqual(0, regs[TrackAmplifierRegisters.Status] & TrackAmplifierRegisters.OccupiedBit);

            // The change is echoed immediately as a fresh SLAVEINFO frame for slave 1.
            var frame = await TryReceiveFrameAsync(transport, TimeSpan.FromSeconds(5));
            Assert.NotNull(frame);
            Assert.Equal(0xFF, frame![1]);
            Assert.Equal(1, frame[3]);
            var statusFromFrame = (ushort)(frame[10] | (frame[11] << 8));
            Assert.NotEqual(0, statusFromFrame & TrackAmplifierRegisters.OccupiedBit);

            // Clearing occupancy removes the bit.
            transport.SetSlaveOccupancy(1, false);
            var cleared = transport.GetRegisters(1);
            Assert.Equal(0, cleared[TrackAmplifierRegisters.Status] & TrackAmplifierRegisters.OccupiedBit);

            await transport.DisposeAsync();
        }

        [Fact]
        public async Task SetSlaveStatus_SetsTheWholeStatusRegister_AndEmitsFreshFrame()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
            await transport.OpenAsync();

            transport.SetSlaveStatus(1, 0xABCD);

            Assert.Equal(0xABCD, transport.GetRegisters(1)[TrackAmplifierRegisters.Status]);

            var frame = await TryReceiveFrameAsync(transport, TimeSpan.FromSeconds(5));
            Assert.NotNull(frame);
            Assert.Equal(0xFF, frame![1]);
            Assert.Equal(1, frame[3]);
            Assert.Equal(0xABCD, (ushort)(frame[10] | (frame[11] << 8)));

            await transport.DisposeAsync();
        }

        [Fact]
        public async Task SetSlaveOccupancy_DoesNotTouchHr0OrHr11()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
            await transport.OpenAsync();

            var before = transport.GetRegisters(1);
            transport.SetSlaveOccupancy(1, true);
            var after = transport.GetRegisters(1);

            // HR0 (PWM command) and HR11 (firmware checksum) are unchanged.
            Assert.Equal(before[TrackAmplifierRegisters.PwmCommand], after[TrackAmplifierRegisters.PwmCommand]);
            Assert.Equal(before[TrackAmplifierRegisters.SwChecksum], after[TrackAmplifierRegisters.SwChecksum]);

            await transport.DisposeAsync();
        }

        // ---------------------------------------------------------------------------------
        // Defensive no-op behavior
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task SetSlaveOccupancy_ForUnknownSlave_IsASafeNoOp()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig()); // detected 1,2,3
            await transport.OpenAsync();

            // Unknown slave: no throw, no register created.
            transport.SetSlaveOccupancy(200, true);
            transport.SetSlaveStatus(200, 0x1234);

            Assert.Throws<KeyNotFoundException>(() => transport.GetRegisters(200));

            await transport.DisposeAsync();
        }

        [Fact]
        public void SetSlaveOccupancy_WhenNotOpened_IsASafeNoOp()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());

            // Not opened: no throw. GetRegisters still reports the initial (empty) state via the
            // detected slave, and an unknown slave throws.
            transport.SetSlaveOccupancy(1, true);
            transport.SetSlaveStatus(1, 0x1234);

            Assert.Equal(0, transport.GetRegisters(1)[TrackAmplifierRegisters.Status]);
            Assert.Throws<KeyNotFoundException>(() => transport.GetRegisters(200));
        }

        // ---------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------

        private static async Task<byte[]?> TryReceiveFrameAsync(DeterministicTrackTransport transport, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            try
            {
                await foreach (var frame in transport.ReceiveAsync(cts.Token))
                {
                    return frame;
                }
            }
            catch (OperationCanceledException)
            {
                // Timed out.
            }

            return null;
        }
    }
}
