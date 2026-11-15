using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Four-way honesty label for a single register value read back from a track amplifier.
    ///
    /// The amplifier container is written once per received frame and never cleared, so a non-null
    /// register value proves only that a frame was parsed at some point in the past. This label
    /// (with <see cref="AmplifierRegisterFreshnessClassifier"/>) makes that distinction explicit so
    /// a stale cache value is never presented as the live PIC18 state.
    /// </summary>
    public enum AmplifierRegisterFreshness
    {
        /// <summary>
        /// A genuine protocol SLAVEINFO readback was observed within the freshness window. This is
        /// the only label that may be presented as the live PIC18 register state.
        /// </summary>
        Fresh,

        /// <summary>
        /// A genuine protocol readback exists but is older than the freshness window: the value is a
        /// cached copy, NOT the live state.
        /// </summary>
        Cached,

        /// <summary>
        /// Register data is present but was NOT populated by a genuine protocol SLAVEINFO readback
        /// (for example a locally written default). Never trusted as live hardware state.
        /// </summary>
        Stale,

        /// <summary>
        /// No data is available: the amplifier was never detected or never produced a frame.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Single classification policy for <see cref="AmplifierRegisterFreshness"/>, built only on the
    /// existing freshness helpers (<see cref="TrackAmplifierDataFreshness"/>,
    /// <see cref="TrackAmplifierItem.ProtocolReadbackObserved"/>,
    /// <see cref="TrackAmplifierItem.LastDataReceivedUtc"/> and
    /// <see cref="TrackAmplifierItem.SlaveDetected"/>) so there is no second freshness rule.
    /// </summary>
    public static class AmplifierRegisterFreshnessClassifier
    {
        /// <summary>Classifies the amplifier's current register readback as of <paramref name="now"/>.</summary>
        public static AmplifierRegisterFreshness Classify(TrackAmplifierItem? item, DateTimeOffset now)
        {
            // No detection or no frame: nothing to read.
            if (item is null || item.SlaveDetected == 0)
            {
                return AmplifierRegisterFreshness.Unknown;
            }

            if (item.LastDataReceivedUtc is null)
            {
                return AmplifierRegisterFreshness.Unknown;
            }

            // A local default write (or any non-protocol mutation) is never a genuine readback.
            if (!item.ProtocolReadbackObserved)
            {
                return AmplifierRegisterFreshness.Stale;
            }

            // Fresh: genuine readback within the policy window — trustable as live.
            if (TrackAmplifierDataFreshness.IsFresh(item, now, TrackAmplifierDataFreshness.DefaultStaleAfter))
            {
                return AmplifierRegisterFreshness.Fresh;
            }

            // Genuine readback but the window has elapsed: a cache, not live.
            return AmplifierRegisterFreshness.Cached;
        }
    }
}
