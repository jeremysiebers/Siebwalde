using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Amplifier-centric physical neutralization that is independent of the logical block
    /// mapping, the current locomotive position and the configured topology.
    ///
    /// It exists so a safety stop can reach a physical output that the normal
    /// locomotive -> block -> amplifier resolution can no longer resolve, and so the conservative
    /// fallback can include every track amplifier the control path can communicate with.
    /// </summary>
    public interface IAmplifierNeutralizer
    {
        /// <summary>
        /// Every physical track amplifier the control path currently knows about: detected
        /// hardware plus the configured topology. Used as the conservative safety fallback
        /// target set, so a detected-but-unmapped amplifier is not silently excluded.
        /// </summary>
        IReadOnlyList<ushort> GetKnownPhysicalAmplifiers();

        /// <summary>
        /// Commands every given physical track amplifier to its neutral setpoint.
        /// </summary>
        /// <param name="amplifiers">Physical slave numbers to neutralize.</param>
        /// <returns>
        /// The amplifiers that could not be accepted for a neutral write. An empty result means
        /// every requested amplifier was commanded neutral at the command level; it is not an
        /// observed confirmation that the amplifier is physically at neutral.
        /// </returns>
        IReadOnlyList<ushort> NeutralizeAmplifiers(IReadOnlyCollection<ushort> amplifiers);
    }
}
