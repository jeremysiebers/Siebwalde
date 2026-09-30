using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The single, Core-level definition of "observed neutral" for a set of amplifiers.
    ///
    /// Movement permission may only be granted when <b>every</b> amplifier in the effective safety
    /// domain reports a genuine, current, protocol SLAVEINFO readback whose PWM field equals the
    /// neutral setpoint. "Current" means detected and fresh per
    /// <see cref="TrackAmplifierDataFreshness"/>; "genuine protocol readback" means the
    /// <see cref="TrackAmplifierItem.ProtocolReadbackObserved"/> marker was stamped by the comm
    /// client when it parsed a SLAVEINFO frame in this runtime (never a locally-written default).
    ///
    /// This predicate works identically for the deterministic simulator and for the future real
    /// PIC32/PIC18 readback, because both populate the amplifier container through the same comm
    /// client parsing path.
    /// </summary>
    public static class TrackAmplifierNeutralObservation
    {
        /// <summary>
        /// True when every amplifier in <paramref name="domain"/> has a genuine, current protocol
        /// readback whose PWM command field equals <see cref="AmplifierSpeedMapper.NeutralPwm"/>.
        /// An empty domain is vacuously true ("all zero amplifiers are observed neutral"); the
        /// movement-grant path must still fail closed on an empty domain, which
        /// <c>EstablishObservedNeutralAsync</c> does explicitly before it can ever grant. A missing
        /// item, stale data, non-protocol data or a non-neutral PWM all return false.
        /// </summary>
        public static bool AreAllObservedNeutral(
            IReadOnlyList<TrackAmplifierItem>? items,
            IReadOnlyCollection<byte> domain,
            DateTimeOffset now,
            TimeSpan staleAfter)
        {
            // An empty domain is vacuously true ("all zero amplifiers are observed neutral"). The
            // movement-grant path fails closed on an empty domain separately, before this predicate
            // can be used to grant.
            if (domain is null || domain.Count == 0)
            {
                return true;
            }

            // A non-empty domain with no amplifier container cannot be observed: fail closed.
            if (items is null)
            {
                return false;
            }

            foreach (var slave in domain)
            {
                var item = items.FirstOrDefault(a => a is not null && a.SlaveNumber == slave);
                if (!TrackAmplifierDataFreshness.HasCurrentProtocolReadback(item, now, staleAfter))
                {
                    return false;
                }

                var registers = item!.HoldingReg;
                if (registers is null || registers.Length == 0)
                {
                    return false;
                }

                if ((registers[TrackAmplifierRegisters.PwmCommand] & 0x03FF) != AmplifierSpeedMapper.NeutralPwm)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>True when every amplifier in <paramref name="domain"/> is observed neutral as of now.</summary>
        public static bool AreAllObservedNeutral(
            IReadOnlyList<TrackAmplifierItem>? items,
            IReadOnlyCollection<byte> domain)
            => AreAllObservedNeutral(items, domain, DateTimeOffset.UtcNow, TrackAmplifierDataFreshness.DefaultStaleAfter);
    }
}
