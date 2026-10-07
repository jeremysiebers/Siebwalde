using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Configuration for a <see cref="KoploperReservationObserver"/>: the background polling
    /// cadence, the maximum acceptable snapshot age for freshness evaluation, and the snapshot
    /// read options used to construct the underlying reader.
    /// </summary>
    public sealed record KoploperReservationObserverOptions
    {
        /// <summary>
        /// The default background polling interval. This is a placeholder cadence for the
        /// observer loop — it is NOT a proven physical-control timing budget.
        /// </summary>
        public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMilliseconds(500);

        /// <summary>The delay between reservation-observer cycles. This is a placeholder, NOT a proven cadence.</summary>
        public TimeSpan PollingInterval { get; init; }

        /// <summary>Optional maximum acceptable snapshot age; <c>null</c> disables the age check.</summary>
        public TimeSpan? MaxSnapshotAge { get; init; }

        /// <summary>Snapshot read options for the underlying reader (not the polling cadence).</summary>
        public KoploperSnapshotReadOptions SnapshotReadOptions { get; init; }

        public KoploperReservationObserverOptions(
            TimeSpan? pollingInterval = null,
            TimeSpan? maxSnapshotAge = null,
            KoploperSnapshotReadOptions? snapshotReadOptions = null)
        {
            TimeSpan interval = pollingInterval ?? DefaultPollingInterval;
            if (interval < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(pollingInterval), interval, "PollingInterval must be non-negative.");
            }

            PollingInterval = interval;
            MaxSnapshotAge = maxSnapshotAge;
            SnapshotReadOptions = snapshotReadOptions ?? new KoploperSnapshotReadOptions();
        }
    }
}
