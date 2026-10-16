using System;

namespace SiebwaldeApp.Core
{
    /// <summary>A2 host checks. PIC32 owns the two-fresh-FC03 identity disambiguation; the C# host
    /// applies its own fail-closed compatibility gate on the SLAVEINFO identity field.</summary>
    public static class AmplifierPhase1Safety
    {
        public const string IncompatibleFirmware = "incompatible firmware";

        /// <summary>True when the amplifier is detected, its data is fresh and genuine, and its HR7
        /// identity field has the compatible shape (version 0x01 + class 1 + non-zero capability).
        /// Shape check only — the PIC32 two-fresh-read disambiguation is the identity authority.</summary>
        public static bool Compatible(TrackAmplifierItem? item)
            => item is not null &&
                TrackAmplifierDataFreshness.HasCurrentProtocolReadback(item, DateTimeOffset.UtcNow,
                    TrackAmplifierDataFreshness.DefaultStaleAfter) &&
                item.HoldingReg.Length == TrackAmplifierRegisters.Count &&
                TrackAmplifierRegisters.HasCompatibleIdentityFields(item.HoldingReg[TrackAmplifierRegisters.Identity]);

        /// <summary>Run-gate: identity compatible + config APPLYed and verified + INIT_DONE/CONFIG_OK +
        /// not STOPPING + no latched fault + not INHIBITED/LEASE_EXPIRED. <paramref name="releaseCommand"/>
        /// is true only for the confirmed release transaction (fresh command after REARM + renewal).</summary>
        public static bool Ready(TrackAmplifierItem? item, bool releaseCommand = false)
        {
            if (!Compatible(item) || !item!.ConfigurationVerified) return false;
            var r = item.HoldingReg;
            const ushort initialized = TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit;
            var state = r[TrackAmplifierRegisters.State];
            var fault = r[TrackAmplifierRegisters.Fault];
            if ((state & initialized) != initialized || (state & TrackAmplifierRegisters.StoppingActiveBit) != 0)
                return false;
            // Only the explicit, confirmed release transaction may submit the fresh command that
            // exits INHIBITED/LEASE_EXPIRED. It never clears a local fault.
            if ((fault & ~(releaseCommand ? TrackAmplifierRegisters.LeaseExpiredFaultBit : 0)) != 0) return false;
            if (!releaseCommand && ((state & TrackAmplifierRegisters.InhibitedBit) != 0 || (state & 15) == 7)) return false;
            return (state & 15) is not (0 or 4 or 5 or 6);
        }

        /// <summary>True when the amplifier reports a genuine applied-PWM readback at neutral, with the
        /// applied-readback capability present. Never treats the command echo as applied evidence.</summary>
        public static bool AppliedNeutral(ushort[] r)
            => r.Length == TrackAmplifierRegisters.Count &&
                (r[TrackAmplifierRegisters.Identity] & TrackAmplifierRegisters.AppliedReadbackCapability) != 0 &&
                (r[TrackAmplifierRegisters.AppliedPwm] & TrackAmplifierRegisters.PwmMask) == AmplifierSpeedMapper.NeutralPwm;
    }
}
