using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A polling reservation observer: it repeatedly reads a coherent snapshot and publishes the
    /// resulting <see cref="KoploperReservationObservation"/>. It owns the lifecycle of its
    /// background loop but performs no polling sleeps inside the read path (the snapshot reader
    /// is a single synchronous call per cycle).
    /// </summary>
    public interface IKoploperReservationObserver : IDisposable
    {
        /// <summary>The most recently published observation, or <c>null</c> before the first refresh.</summary>
        KoploperReservationObservation? CurrentObservation { get; }

        /// <summary>Performs one read → evaluate → aggregate → publish cycle and returns the result.</summary>
        KoploperReservationObservation Refresh();

        /// <summary>Begins the background polling loop. Idempotent; cancels the supplied token when it is canceled.</summary>
        void Start(CancellationToken cancellationToken);

        /// <summary>Cancels and awaits the background loop. Idempotent.</summary>
        Task StopAsync();
    }
}
