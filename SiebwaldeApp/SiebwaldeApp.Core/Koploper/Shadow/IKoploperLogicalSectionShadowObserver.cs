using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A polling logical-section shadow observer: it repeatedly reads a KIS-04 snapshot and the
    /// current KIS-05 reservation observation, projects them onto logical sections, and publishes
    /// the resulting <see cref="KoploperLogicalSectionShadowObservation"/>. It owns the lifecycle of
    /// its background loop but performs no polling sleeps inside the projection path.
    /// </summary>
    public interface IKoploperLogicalSectionShadowObserver : IDisposable
    {
        /// <summary>The most recently published shadow, or <c>null</c> before the first refresh.</summary>
        KoploperLogicalSectionShadowObservation? CurrentShadow { get; }

        /// <summary>Performs one read → project → publish cycle and returns the result.</summary>
        KoploperLogicalSectionShadowObservation Refresh();

        /// <summary>Begins the background polling loop. Idempotent; cancels the supplied token when it is canceled.</summary>
        void Start(CancellationToken cancellationToken);

        /// <summary>Cancels and awaits the background loop. Idempotent.</summary>
        Task StopAsync();
    }
}
