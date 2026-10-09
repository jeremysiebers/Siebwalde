using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A polling logical/physical reconciliation observer: it repeatedly pulls the current
    /// logical-section shadow and a physical section occupancy read, reconciles them via
    /// <see cref="LogicalPhysicalReconciliationProjector"/>, and publishes the resulting
    /// <see cref="LogicalPhysicalReconciliationObservation"/>. It owns the lifecycle of its
    /// background loop but performs no polling sleeps inside the reconciliation path.
    /// </summary>
    public interface ILogicalPhysicalReconciliationObserver : IDisposable
    {
        /// <summary>The most recently published observation, or <c>null</c> before the first refresh.</summary>
        LogicalPhysicalReconciliationObservation? CurrentObservation { get; }

        /// <summary>Performs one read → reconcile → publish cycle and returns the result.</summary>
        LogicalPhysicalReconciliationObservation Refresh();

        /// <summary>Begins the background polling loop. Idempotent; cancels the supplied token when it is canceled.</summary>
        void Start(CancellationToken cancellationToken);

        /// <summary>Cancels and awaits the background loop. Idempotent.</summary>
        Task StopAsync();
    }
}
