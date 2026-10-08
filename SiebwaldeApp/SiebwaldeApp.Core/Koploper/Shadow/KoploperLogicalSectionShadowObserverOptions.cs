using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Configuration for a <see cref="KoploperLogicalSectionShadowObserver"/>. Only the maximum
    /// acceptable manual-state snapshot age is configurable; the background polling cadence is a
    /// fixed placeholder internal to the observer (not a proven control timing budget).
    /// </summary>
    public sealed record KoploperLogicalSectionShadowObserverOptions(TimeSpan? MaxSnapshotAge = null)
    {
        /// <summary>
        /// The default maximum snapshot age used by tests. This is a placeholder test budget —
        /// it is NOT a proven physical-control timing budget.
        /// </summary>
        public static readonly TimeSpan DefaultTestMaxSnapshotAge = TimeSpan.FromSeconds(5);
    }
}
