using System;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// A PASSIVE KIS-09 observer that reconciles the production track application's shared
    /// <see cref="TrackApplicationVariables"/> container (populated by the production comm client's
    /// SLAVEINFO frames) against the KIS-07 logical-section shadow (read from the Koploper process
    /// memory by the injected observers). It composes ONLY the KIS-08 physical source and
    /// reconciliation observer; it NEVER instantiates a comm client, transport, session guard, or any
    /// write path — there is no second PIC32 client.
    /// </summary>
    public sealed class Kis09PassiveObserver : IDisposable
    {
        private readonly TrackApplicationVariables _variables;
        private readonly LayoutProfile _profile;
        private readonly IKoploperReservationObserver _reservationObserver;
        private readonly IKoploperLogicalSectionShadowObserver _shadowObserver;
        private readonly TrackAmplifierPhysicalSectionOccupancySource _physicalSource;
        private readonly LogicalPhysicalReconciliationObserver _reconciliationObserver;

        public Kis09PassiveObserver(
            TrackApplicationVariables variables,
            LayoutProfile profile,
            IKoploperReservationObserver reservationObserver,
            IKoploperLogicalSectionShadowObserver shadowObserver,
            LogicalPhysicalReconciliationObserverOptions? options = null,
            Func<DateTimeOffset>? clock = null,
            TimeSpan? physicalStaleAfter = null)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _reservationObserver = reservationObserver ?? throw new ArgumentNullException(nameof(reservationObserver));
            _shadowObserver = shadowObserver ?? throw new ArgumentNullException(nameof(shadowObserver));
            Func<DateTimeOffset> sharedClock = clock ?? (() => DateTimeOffset.UtcNow);

            // KIS-08 physical source reads the PRODUCTION variables container directly (the same
            // list the comm client writes), so the observer sees the live amplifier state.
            _physicalSource = new TrackAmplifierPhysicalSectionOccupancySource(
                profile.Name,
                profile.PhysicalAmplifierMapping,
                slave => _variables.trackAmpItems.FirstOrDefault(a => a.SlaveNumber == slave),
                sourceGenerationProvider: null,
                clock: sharedClock,
                staleAfter: physicalStaleAfter);

            _reconciliationObserver = new LogicalPhysicalReconciliationObserver(
                _shadowObserver,
                _physicalSource,
                profile.Name,
                profile.ToKoploperBlockBindingMap(),
                options,
                sharedClock);
        }

        /// <summary>The production variables container this observer reads (read-only use).</summary>
        public TrackApplicationVariables Variables => _variables;

        /// <summary>The loaded profile driving the physical/reconciliation projection.</summary>
        public LayoutProfile Profile => _profile;

        /// <summary>The KIS-08 physical section occupancy source (reads the production variables).</summary>
        public IPhysicalSectionOccupancySource PhysicalSource => _physicalSource;

        /// <summary>The KIS-08 logical/physical reconciliation observer.</summary>
        public LogicalPhysicalReconciliationObserver ReconciliationObserver => _reconciliationObserver;

        /// <summary>One full read → project → reconcile cycle (read-only, no transmission).</summary>
        public LogicalPhysicalReconciliationObservation Refresh()
        {
            _reservationObserver.Refresh();
            _shadowObserver.Refresh();
            return _reconciliationObserver.Refresh();
        }

        /// <summary>One physical section occupancy observation (read-only, no transmission).</summary>
        public PhysicalSectionOccupancyObservation ReadPhysicalObservation() => _physicalSource.Read();

        /// <summary>The current KIS-07 logical-section shadow (read-only, no transmission).</summary>
        public KoploperLogicalSectionShadowObservation? ReadShadowObservation() => _shadowObserver.CurrentShadow;

        /// <summary>Builds a machine-readable trace record with NO session/command data.</summary>
        public KoploperTelemetryTraceRecord BuildTraceRecord(
            long cycle,
            DateTimeOffset timestampUtc,
            LogicalPhysicalReconciliationObservation? reconciliation,
            PhysicalSectionOccupancyObservation? physical)
            => KoploperTelemetryTraceBuilder.BuildPassive(cycle, timestampUtc, physical, reconciliation, _profile, _variables);

        public void Dispose()
        {
            _reconciliationObserver.Dispose();
            _shadowObserver.Dispose();
            _reservationObserver.Dispose();
        }
    }
}
