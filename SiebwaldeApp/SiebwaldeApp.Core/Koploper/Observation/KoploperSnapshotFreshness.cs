using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Freshness evaluation for a captured snapshot. Pure and stateless: given a snapshot and a
    /// "now" instant, it answers the age, whether the snapshot is still current, and what health
    /// to publish given a maximum acceptable age.
    /// </summary>
    public static class KoploperSnapshotFreshness
    {
        /// <summary>Elapsed time between the snapshot capture and <paramref name="now"/>.</summary>
        public static TimeSpan Age(KoploperStateSnapshot snapshot, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            return now - snapshot.CapturedAtUtc;
        }

        /// <summary>True when the snapshot age is within (or equal to) <paramref name="maxSnapshotAge"/>.</summary>
        public static bool IsCurrent(KoploperStateSnapshot snapshot, DateTimeOffset now, TimeSpan maxSnapshotAge)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            return Age(snapshot, now) <= maxSnapshotAge;
        }

        /// <summary>
        /// Resolves the health to publish: <see cref="KoploperSourceHealth.Stale"/> when the age
        /// exceeds <paramref name="maxSnapshotAge"/>, otherwise the snapshot's own health.
        /// </summary>
        public static KoploperSourceHealth ResolveHealth(KoploperStateSnapshot snapshot, DateTimeOffset now, TimeSpan maxSnapshotAge)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            if (Age(snapshot, now) > maxSnapshotAge)
            {
                return KoploperSourceHealth.Stale;
            }

            return snapshot.SourceHealth;
        }
    }
}
