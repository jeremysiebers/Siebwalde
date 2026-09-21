using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// How well the control path currently knows a physical amplifier. This is a communication
    /// state, not a hardware acknowledgement: the protocol provides no neutralization
    /// acknowledgement, so the strongest claim is "a fresh frame was received recently".
    /// </summary>
    public enum AmplifierCommunicationState
    {
        /// <summary>Not a legitimate physical track amplifier (includes address 0 and 51..55).</summary>
        Invalid = 0,

        /// <summary>A legitimate track amplifier that has never delivered a frame.</summary>
        NeverSeen = 1,

        /// <summary>Detected earlier, but no fresh frame within the freshness window.</summary>
        Stale = 2,

        /// <summary>Detected and fresh.</summary>
        Fresh = 3
    }

    /// <summary>
    /// Amplifier-centric physical neutralization that is independent of the logical block
    /// mapping, the current locomotive position and the configured topology.
    ///
    /// It exists so a safety stop can reach a physical output that the normal
    /// locomotive -> block -> amplifier resolution can no longer resolve, and so the conservative
    /// fallback can include every track amplifier the control path can communicate with.
    ///
    /// Only addresses that are legitimate physical track amplifiers
    /// (<see cref="TrackAmplifierAddress.IsTrackAmplifierAddress"/>) may receive a neutral write.
    /// Backplane/configuration slaves (51..55) use HoldingReg0 as a configuration word and must
    /// never receive track-amplifier PWM/neutral semantics.
    /// </summary>
    public interface IAmplifierNeutralizer
    {
        /// <summary>
        /// Every physical track amplifier the control path currently knows about: detected
        /// hardware plus the configured topology, restricted to the legitimate track-amplifier
        /// address range. Used as the conservative safety fallback target set, so a
        /// detected-but-unmapped amplifier is not silently excluded and a backplane slave is
        /// never included.
        /// </summary>
        IReadOnlyList<ushort> GetKnownPhysicalAmplifiers();

        /// <summary>
        /// Commands every given physical track amplifier to its neutral setpoint.
        ///
        /// An address that is not a legitimate physical track amplifier is rejected and returned
        /// as not commanded; no write is produced for it. This is the central guard that keeps
        /// track-amplifier PWM/neutral semantics off the backplane slaves.
        /// </summary>
        /// <param name="amplifiers">Physical slave numbers to neutralize.</param>
        /// <returns>
        /// The amplifiers that were not accepted for a neutral write (invalid device class, or a
        /// zero address). An empty result means every requested amplifier was a legitimate track
        /// amplifier and a neutral command was placed at the command-acceptance boundary; it is
        /// not an observed confirmation that the amplifier is physically at neutral, and it does
        /// not prove that the command was transmitted.
        /// </returns>
        IReadOnlyList<ushort> NeutralizeAmplifiers(IReadOnlyCollection<ushort> amplifiers);

        /// <summary>
        /// The current communication state of a physical amplifier, used to decide whether a
        /// required safety target can be considered established. A queued neutral command to a
        /// stale or never-seen amplifier is not an established neutralization.
        /// </summary>
        AmplifierCommunicationState GetAmplifierCommunicationState(ushort amplifier);
    }
}
