using System;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Configuration for a <see cref="LogicalPhysicalReconciliationObserver"/>: the background
    /// polling cadence and the physical staleness bound. The reconciliation projector itself trusts
    /// the physical observation's per-section <c>IsFresh</c> (the physical source applies its own
    /// freshness policy); <see cref="PhysicalStaleAfter"/> is carried here as the physical-source
    /// configuration boundary and is not consumed by the reconciliation projector.
    /// </summary>
    public sealed record LogicalPhysicalReconciliationObserverOptions
    {
        /// <summary>
        /// The default background polling interval. This is a placeholder cadence for the observer
        /// loop — it is NOT a proven physical-control timing budget.
        /// </summary>
        public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMilliseconds(500);

        /// <summary>The delay between reconciliation-observer cycles. This is a placeholder, NOT a proven cadence.</summary>
        public TimeSpan PollingInterval { get; init; }

        /// <summary>The physical staleness bound carried for the physical-source configuration boundary.</summary>
        public TimeSpan PhysicalStaleAfter { get; init; }

        public LogicalPhysicalReconciliationObserverOptions(
            TimeSpan? pollingInterval = null,
            TimeSpan? physicalStaleAfter = null)
        {
            TimeSpan interval = pollingInterval ?? DefaultPollingInterval;
            if (interval < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(pollingInterval), interval, "PollingInterval must be non-negative.");
            }

            PollingInterval = interval;
            PhysicalStaleAfter = physicalStaleAfter ?? TrackAmplifierDataFreshness.DefaultStaleAfter;
        }
    }
}
