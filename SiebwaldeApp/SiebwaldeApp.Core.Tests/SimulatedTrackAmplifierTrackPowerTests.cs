using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// F4 harness-fidelity tests for <see cref="SimulatedTrackAmplifier"/>: the simulator must mirror
    /// the PIC18 <c>AmpCore_Inhibit()</c> F5 behaviour — any safety inhibit (EMO, STOP_REQUEST, lease
    /// expiry, DISABLE) drops the track-power request, so the simulated bridge stays de-energized
    /// after the inhibit cause clears until a fresh HR_TRACK_POWER=1 write.
    /// </summary>
    public class SimulatedTrackAmplifierTrackPowerTests
    {
        private static bool IsInhibited(SimulatedTrackAmplifier amp)
            => (amp.Snapshot()[TrackAmplifierRegisters.State] & TrackAmplifierRegisters.InhibitedBit) != 0;

        /// <summary>Configures + APPLYs + enables + arms the lease, so the amp can energize at neutral.</summary>
        private static void ConfigureAndEnable(SimulatedTrackAmplifier amp)
        {
            amp.ApplyWrite(3, new ushort[] { 0x0202 });   // accel 2, decel 2
            amp.ApplyWrite(4, new ushort[] { 8 });        // fast stop 8
            amp.ApplyWrite(5, new ushort[] { 250 });      // watchdog 250
            amp.ApplyWrite(1, new ushort[] { TrackAmplifierRegisters.ApplyConfigBit });
            amp.ApplyWrite(1, new ushort[] { TrackAmplifierRegisters.EnableBit | TrackAmplifierRegisters.MotionPermissionBit });
        }

        [Fact]
        public void TrackPowerRequest_DroppedOnEmo_StaysDeEnergizedUntilFreshWrite()
        {
            var amp = new SimulatedTrackAmplifier(1, protocolVersion: TrackAmplifierRegisters.TrackPowerProtocolVersion);
            ConfigureAndEnable(amp);

            // Energize at neutral via track power.
            amp.ApplyWrite(0, new ushort[] { AmplifierSpeedMapper.NeutralPwm });
            amp.ApplyWrite(23, new ushort[] { TrackAmplifierRegisters.TrackPowerEnableBit });
            amp.Step();
            Assert.False(IsInhibited(amp)); // energized

            // EMO inhibits and (F5) drops the track-power request.
            amp.ApplyWrite(0, new ushort[] { TrackAmplifierRegisters.EmergencyStopBit });
            Assert.True(IsInhibited(amp));

            // Release EMO and renew the lease: the bridge must STAY de-energized.
            amp.ApplyWrite(0, new ushort[] { AmplifierSpeedMapper.NeutralPwm });
            amp.ApplyWrite(1, new ushort[] { TrackAmplifierRegisters.MotionPermissionBit });
            amp.Step();
            Assert.True(IsInhibited(amp)); // still de-energized (request was dropped)

            // A fresh HR_TRACK_POWER=1 write re-energizes.
            amp.ApplyWrite(23, new ushort[] { TrackAmplifierRegisters.TrackPowerEnableBit });
            amp.Step();
            Assert.False(IsInhibited(amp));
        }

        [Fact]
        public void TrackPowerRequest_DroppedOnDisable_StaysDeEnergizedUntilFreshWrite()
        {
            var amp = new SimulatedTrackAmplifier(1, protocolVersion: TrackAmplifierRegisters.TrackPowerProtocolVersion);
            ConfigureAndEnable(amp);

            amp.ApplyWrite(0, new ushort[] { AmplifierSpeedMapper.NeutralPwm });
            amp.ApplyWrite(23, new ushort[] { TrackAmplifierRegisters.TrackPowerEnableBit });
            amp.Step();
            Assert.False(IsInhibited(amp));

            // DISABLE inhibits and drops the request.
            amp.ApplyWrite(1, new ushort[] { TrackAmplifierRegisters.DisableBit });
            Assert.True(IsInhibited(amp));

            // Re-enable + renew lease: stays de-energized (request was dropped).
            amp.ApplyWrite(1, new ushort[] { TrackAmplifierRegisters.EnableBit | TrackAmplifierRegisters.MotionPermissionBit });
            amp.Step();
            Assert.True(IsInhibited(amp));

            amp.ApplyWrite(23, new ushort[] { TrackAmplifierRegisters.TrackPowerEnableBit });
            amp.Step();
            Assert.False(IsInhibited(amp));
        }
    }
}
