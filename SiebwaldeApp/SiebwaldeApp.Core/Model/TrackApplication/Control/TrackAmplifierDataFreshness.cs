using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Freshness policy for track-amplifier data.
    ///
    /// The amplifier container is written once per received frame and is never cleared, and the
    /// comm client keeps republishing the cached container, so a non-null value proves only that
    /// a frame was parsed at some point in the past. Staleness therefore has to be derived from
    /// the time of the last parsed frame.
    ///
    /// This is the single definition of that policy: <see cref="TrackAmplifierOccupancyProvider"/>
    /// and the mode observability both use it, so there is no second freshness rule.
    /// </summary>
    public static class TrackAmplifierDataFreshness
    {
        /// <summary>
        /// How long amplifier data stays valid after the last parsed frame. The comm client
        /// publishes its cached container at 10 Hz, so this is many publish cycles; it is a
        /// policy value, not a protocol value.
        /// </summary>
        public static readonly TimeSpan DefaultStaleAfter = TimeSpan.FromSeconds(2);

        /// <summary>
        /// True when the amplifier has delivered a frame recently enough to be trusted.
        /// Never-received and stale data are both "not fresh".
        /// </summary>
        public static bool IsFresh(
            TrackAmplifierItem? amplifier,
            DateTimeOffset now,
            TimeSpan staleAfter)
        {
            if (amplifier?.LastDataReceivedUtc is not DateTimeOffset received)
            {
                return false;
            }

            var age = now - received;

            // A clock that moved backwards must not make old data look fresh.
            if (age < TimeSpan.Zero)
            {
                return false;
            }

            return age <= staleAfter;
        }

        /// <summary>True when the amplifier data is fresh as of now.</summary>
        public static bool IsFresh(TrackAmplifierItem? amplifier)
            => IsFresh(amplifier, DateTimeOffset.UtcNow, DefaultStaleAfter);

        /// <summary>
        /// True when the amplifier is detected <b>and</b> its data is fresh, which together mean
        /// the register values may be trusted as the current state.
        /// </summary>
        public static bool IsCurrentData(
            TrackAmplifierItem? amplifier,
            DateTimeOffset now,
            TimeSpan staleAfter)
            => amplifier is not null &&
               amplifier.SlaveDetected != 0 &&
               IsFresh(amplifier, now, staleAfter);
    }
}
