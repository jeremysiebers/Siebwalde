using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Configuration for a <see cref="IKoploperSnapshotReader"/>: how many read attempts to make
    /// and (optionally) the maximum acceptable snapshot age for freshness evaluation.
    /// </summary>
    public sealed record KoploperSnapshotReadOptions
    {
        /// <summary>
        /// A default maximum snapshot age used by deterministic tests and configuration samples.
        /// This is a test/config placeholder only — it is NOT a physical-control timing budget.
        /// </summary>
        public static readonly TimeSpan DefaultTestMaxSnapshotAge = TimeSpan.FromSeconds(5);

        /// <summary>Maximum number of coherent-read attempts per snapshot read.</summary>
        public int MaxAttempts { get; init; }

        /// <summary>Optional maximum acceptable snapshot age; <c>null</c> disables the age check.</summary>
        public TimeSpan? MaxSnapshotAge { get; init; }

        public KoploperSnapshotReadOptions(int maxAttempts = 4, TimeSpan? maxSnapshotAge = null)
        {
            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "MaxAttempts must be at least 1.");
            }

            MaxAttempts = maxAttempts;
            MaxSnapshotAge = maxSnapshotAge;
        }
    }
}
