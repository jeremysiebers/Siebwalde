using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Comm;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.Koploper.Windows;

namespace SiebwaldeApp.Koploper.Validation.Host
{
    /// <summary>
    /// Profile-driven composition for the KIS-09 "part C" LIVE_HARDWARE bounded telemetry-enablement
    /// validation host.
    /// <para>
    /// <b>Bounded telemetry-enablement scope.</b> The only outbound transmissions this host can ever
    /// perform are the six telemetry-enablement session commands —
    /// <see cref="KoploperSessionCommandAllowlist.ClientConnectionRequest"/> (250),
    /// <see cref="KoploperSessionCommandAllowlist.ExecMbusStateSlavesOn"/> (100),
    /// <see cref="KoploperSessionCommandAllowlist.ExecMbusStateSlaveDetect"/> (101),
    /// <see cref="KoploperSessionCommandAllowlist.ExecMbusStateSlaveInit"/> (104),
    /// <see cref="KoploperSessionCommandAllowlist.ExecMbusStateStartDataUpload"/> (106) and
    /// <see cref="KoploperSessionCommandAllowlist.ExecMbusStateSlaveEnable"/> (105) — sent in that
    /// exact order exclusively through <see cref="KoploperSessionCommandGuard"/>, which fail-closes
    /// everything else AND waits for each command's master DONE acknowledgement before sending the
    /// next. 105 (SLAVE_ENABLE) writes amplifier HR1=0x8000 (BEMF enable); it does NOT itself write
    /// HR0/neutral 399 — neutral 399 is a separate runtime-writer (108) concern that this host never
    /// runs. 108 (EXEC_MBUS_SLAVE_DATA_EXCH, the only HR0/PWM/movement write vector) plus 107 (RESET)
    /// remain structurally impossible. No control-stack component is instantiated and no HR0/movement
    /// write path exists.
    /// </para>
    /// </summary>
    public sealed class ReadOnlyTelemetryValidationHost : IAsyncDisposable
    {
        /// <summary>The dummy payload used by the three session commands (mirrors the init-step templates).</summary>
        private static readonly byte[] SessionCommandData = new byte[80];

        private readonly TrackApplicationVariables _variables;
        private readonly KoploperSessionCommandGuard _sessionCommandGuard;
        private readonly KoploperReservationObserver _reservationObserver;
        private readonly KoploperLogicalSectionShadowObserver _shadowObserver;
        private readonly TrackAmplifierPhysicalSectionOccupancySource _physicalSource;
        private readonly LogicalPhysicalReconciliationObserver _reconciliationObserver;

        private ReadOnlyTelemetryValidationHost(
            LayoutProfile profile,
            IReadOnlyDictionary<int, int> koploperBlockBindingMap,
            TrackApplicationVariables variables,
            KoploperSessionCommandGuard sessionCommandGuard,
            KoploperReservationObserver reservationObserver,
            KoploperLogicalSectionShadowObserver shadowObserver,
            TrackAmplifierPhysicalSectionOccupancySource physicalSource,
            LogicalPhysicalReconciliationObserver reconciliationObserver)
        {
            Profile = profile;
            KoploperBlockBindingMap = koploperBlockBindingMap;
            _variables = variables;
            _sessionCommandGuard = sessionCommandGuard;
            _reservationObserver = reservationObserver;
            _shadowObserver = shadowObserver;
            _physicalSource = physicalSource;
            _reconciliationObserver = reconciliationObserver;
        }

        /// <summary>The loaded "Simple Loop" profile (the declarative source of the bindings).</summary>
        public LayoutProfile Profile { get; }

        /// <summary>The profile's explicit Koploper internal-block -&gt; logical-section binding map.</summary>
        public IReadOnlyDictionary<int, int> KoploperBlockBindingMap { get; }

        /// <summary>The read-side track-amplifier data container feeding the physical source.</summary>
        public TrackApplicationVariables Variables => _variables;

        /// <summary>The only outbound boundary. Host code never touches the inner client or transport.</summary>
        public KoploperSessionCommandGuard SessionCommandGuard => _sessionCommandGuard;

        /// <summary>The KIS-05/06 reservation observer (read side).</summary>
        public KoploperReservationObserver ReservationObserver => _reservationObserver;

        /// <summary>The KIS-07 logical-section shadow observer.</summary>
        public KoploperLogicalSectionShadowObserver ShadowObserver => _shadowObserver;

        /// <summary>The physical section occupancy source (KIS-08, read side only).</summary>
        public IPhysicalSectionOccupancySource PhysicalSource => _physicalSource;

        /// <summary>The KIS-08 logical/physical reconciliation observer.</summary>
        public LogicalPhysicalReconciliationObserver ReconciliationObserver => _reconciliationObserver;

        /// <summary>
        /// Performs one full read → project → reconcile cycle and returns the result: refresh the
        /// reservation observer, then the logical-section shadow observer, then reconcile against the
        /// physical source. This mirrors the production background-loop ordering; every stage performs
        /// no outbound transmission.
        /// </summary>
        public LogicalPhysicalReconciliationObservation Refresh()
        {
            _reservationObserver.Refresh();
            _shadowObserver.Refresh();
            return _reconciliationObserver.Refresh();
        }

        /// <summary>Reads one physical section occupancy observation (read side only, no transmission).</summary>
        public PhysicalSectionOccupancyObservation ReadPhysicalObservation() => _physicalSource.Read();

        /// <summary>
        /// Opens the read-side transport and starts the receive loop. Opening/receiving performs no
        /// outbound transmission; the only transmissions are the six telemetry-enablement commands in
        /// <see cref="EnableSessionAsync"/> (cold) or the single 250 in
        /// <see cref="EnableWarmReconnectAsync"/> (warm).
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _sessionCommandGuard.StartAsync(realHardwareMode: true, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Begins a fresh cold session and sends the six telemetry-enablement commands in the exact
        /// mandated order through the guard: 250 → 100 → 101 → 104 → 106 → 105. Each command is sent
        /// only after the previous command's master DONE acknowledgement is received (bounded timeout).
        /// The final 105 (SLAVE_ENABLE) writes amplifier HR1=0x8000 (BEMF enable); it does NOT write
        /// HR0/neutral 399 (a separate runtime-writer concern this host never runs). Any other command
        /// (including 108/107) is fail-closed by the guard.
        /// </summary>
        public async Task EnableSessionAsync(CancellationToken cancellationToken = default)
        {
            _sessionCommandGuard.BeginNewSession();

            await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ClientConnectionRequest, cancellationToken).ConfigureAwait(false);
            await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ExecMbusStateSlavesOn, cancellationToken).ConfigureAwait(false);
            await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ExecMbusStateSlaveDetect, cancellationToken).ConfigureAwait(false);
            await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ExecMbusStateSlaveInit, cancellationToken).ConfigureAwait(false);
            await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ExecMbusStateStartDataUpload, cancellationToken).ConfigureAwait(false);
            await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ExecMbusStateSlaveEnable, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Begins a warm-reconnect session and sends only CLIENT_CONNECTION_REQUEST (250), waiting for
        /// the CONTROLLER/CONNECTED/DONE acknowledgement; no enablement command is re-sent. Use this
        /// ONLY when the caller has explicitly established that the master is already in SERVICE_TASKS
        /// with telemetry flowing.
        /// </summary>
        public async Task EnableWarmReconnectAsync(CancellationToken cancellationToken = default)
        {
            _sessionCommandGuard.BeginWarmReconnect();

            await SendSessionCommandAsync(KoploperSessionCommandAllowlist.ClientConnectionRequest, cancellationToken).ConfigureAwait(false);
        }

        private Task SendSessionCommandAsync(byte command, CancellationToken cancellationToken)
            => _sessionCommandGuard.SendAsync(new SendMessage(command, SessionCommandData), cancellationToken);

        /// <summary>Stops the read-side comm client and the (never-started) observer loops. Idempotent.</summary>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _reconciliationObserver.StopAsync().ConfigureAwait(false);
            await _shadowObserver.StopAsync().ConfigureAwait(false);
            await _sessionCommandGuard.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            await _sessionCommandGuard.DisposeAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Composes the real bounded telemetry-enablement host from the named profile (default "Simple Loop").
        /// <list type="bullet">
        ///   <item>Loads the profile and reads <see cref="LayoutProfile.KoploperBlockBindings"/> via
        ///       <see cref="LayoutProfile.ToKoploperBlockBindingMap"/> and
        ///       <see cref="LayoutProfile.PhysicalAmplifierMapping"/> (no hardcoded dictionary).</item>
        ///   <item>Wires a real comm client (read side) behind a <see cref="KoploperSessionCommandGuard"/>.</item>
        ///   <item>Composes the KIS-07 shadow observer from the real Koploper process pipeline with the
        ///       profile binding map.</item>
        ///   <item>Composes the KIS-08 physical source from the read-side amplifier data and the
        ///       profile physical mapping.</item>
        ///   <item>Composes the KIS-08 reconciliation observer.</item>
        /// </list>
        /// It does NOT instantiate any control-stack component (no <c>TrackControlMain</c>,
        /// <c>TrackControlHost</c>, init steps, firmware/recovery steps, movement gate, or
        /// <c>SetDesiredAmplifierControl</c>).
        /// </summary>
        /// <param name="profileName">Profile display name to load (default "Simple Loop").</param>
        /// <param name="transportFactory">
        /// Optional transport seam (tests / in-memory fakes). When null, the real UDP transport is
        /// built from <see cref="CoreConfiguration"/>. Supplying a fake keeps every test offline.
        /// </param>
        /// <param name="clock">Optional shared clock; defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
        /// <param name="acknowledgementTimeout">
        /// Optional per-command acknowledgement timeout; defaults to 10 seconds. This is a bounded
        /// session timeout (NOT a safety/control budget).
        /// </param>
        public static ReadOnlyTelemetryValidationHost Create(
            string profileName = "Simple Loop",
            Func<ITrackTransport>? transportFactory = null,
            Func<DateTimeOffset>? clock = null,
            KoploperSnapshotReadOptions? snapshotReadOptions = null,
            KoploperReservationObserverOptions? reservationOptions = null,
            KoploperLogicalSectionShadowObserverOptions? shadowOptions = null,
            LogicalPhysicalReconciliationObserverOptions? reconciliationOptions = null,
            TimeSpan? physicalStaleAfter = null,
            TimeSpan? acknowledgementTimeout = null)
        {
            if (!LayoutProfileLoader.TryLoadByName(profileName, out LayoutProfile? profile, out IReadOnlyList<string> errors) ||
                profile is null)
            {
                throw new InvalidOperationException(
                    $"Cannot compose the read-only telemetry host: {string.Join("; ", errors)}");
            }

            Func<DateTimeOffset> sharedClock = clock ?? (() => DateTimeOffset.UtcNow);

            var variables = new TrackApplicationVariables();

            ITrackTransport transport = transportFactory?.Invoke()
                ?? new RawUdpTrackTransport(new RawUdpTransport(
                    CoreConfiguration.TrackControllerIpAddress,
                    CoreConfiguration.TrackControllerSendingPort,
                    CoreConfiguration.TrackControllerReceivingPort));

            var commClient = new TrackCommClientAsync(transport, variables);
            var sessionCommandGuard = new KoploperSessionCommandGuard(commClient, acknowledgementTimeout);

            // KIS-06 read pipeline: reservation observer over its own snapshot reader.
            IKoploperSnapshotReader reservationReader = BuildSnapshotReader(sharedClock, snapshotReadOptions);
            var reservationObserver = new KoploperReservationObserver(reservationReader, reservationOptions, sharedClock);

            // KIS-07 shadow observer uses an independent snapshot reader (manual-state read) plus the
            // reservation observer, with the profile's explicit binding map.
            IKoploperSnapshotReader shadowReader = BuildSnapshotReader(sharedClock, snapshotReadOptions);
            var shadowObserver = new KoploperLogicalSectionShadowObserver(
                shadowReader,
                reservationObserver,
                profile.Name,
                profile.ToKoploperBlockBindingMap(),
                shadowOptions,
                sharedClock);

            var physicalSource = new TrackAmplifierPhysicalSectionOccupancySource(
                profile.Name,
                profile.PhysicalAmplifierMapping,
                slave => variables.trackAmpItems.FirstOrDefault(a => a.SlaveNumber == slave),
                sourceGenerationProvider: null,
                clock: sharedClock,
                staleAfter: physicalStaleAfter);

            var reconciliationObserver = new LogicalPhysicalReconciliationObserver(
                shadowObserver,
                physicalSource,
                profile.Name,
                profile.KoploperBlockBindings.Select(b => b.LogicalSectionId).ToArray(),
                reconciliationOptions,
                sharedClock);

            return new ReadOnlyTelemetryValidationHost(
                profile,
                profile.ToKoploperBlockBindingMap(),
                variables,
                sessionCommandGuard,
                reservationObserver,
                shadowObserver,
                physicalSource,
                reconciliationObserver);
        }

        private static IKoploperSnapshotReader BuildSnapshotReader(
            Func<DateTimeOffset> clock,
            KoploperSnapshotReadOptions? options)
        {
            IKoploperMemoryLayout layout = Koploper94MemoryLayout.Instance;
            IKoploperProcessLocator processLocator = new WindowsKoploperProcessLocator();
            IKoploperExecutableVerifier executableVerifier = new KoploperExecutableVerifier(layout);
            Func<IKoploperMemoryReader> memoryReaderFactory = () => new WindowsKoploperMemoryReader();
            IKoploperRawObjectGraphReader rawGraphReader =
                new KoploperRawObjectGraphReader(layout, new KoploperDecodePlausibility());
            IKoploperBlockStateDecoder blockStateDecoder = new KoploperBlockStateDecoder();

            return new KoploperSnapshotReader(
                processLocator,
                executableVerifier,
                memoryReaderFactory,
                rawGraphReader,
                blockStateDecoder,
                options,
                clock);
        }
    }
}
