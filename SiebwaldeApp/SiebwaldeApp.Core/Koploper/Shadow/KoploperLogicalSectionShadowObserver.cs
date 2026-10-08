using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Polling logical-section shadow observer. Each cycle reads a KIS-04 snapshot via its own
    /// <see cref="IKoploperSnapshotReader"/> and pulls the current KIS-05 reservation observation
    /// from the injected <see cref="IKoploperReservationObserver"/>, then projects both via
    /// <see cref="KoploperLogicalSectionShadowProjector"/> and publishes the result through a
    /// volatile swap. A non-authoritative cycle publishes a fresh empty shadow — ownership is never
    /// inherited from a previous cycle and there is no last-known-good cache.
    /// <para>
    /// Source-sequence note: the manual-state snapshot (own reader) and the reservation observation
    /// (injected observer) are read from two independent readers and may be one cycle apart. The
    /// reservation observation's <see cref="KoploperReservationObservation.IsAuthoritative"/> is the
    /// single authority gate; the manual-state snapshot is additionally validated for freshness via
    /// <see cref="KoploperLogicalSectionShadowObserverOptions.MaxSnapshotAge"/>. A consumer MUST gate
    /// on <see cref="KoploperLogicalSectionShadowObservation.ShadowValid"/>.
    /// </para>
    /// </summary>
    public sealed class KoploperLogicalSectionShadowObserver : IKoploperLogicalSectionShadowObserver
    {
        private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(500);

        private readonly IKoploperSnapshotReader _snapshotReader;
        private readonly IKoploperReservationObserver _reservationObserver;
        private readonly string _profileId;
        private readonly IReadOnlyDictionary<int, int> _internalBlockToSection;
        private readonly KoploperLogicalSectionShadowObserverOptions _options;
        private readonly Func<DateTimeOffset> _clock;

        private volatile KoploperLogicalSectionShadowObservation? _current;
        private long _shadowSequence;

        private readonly object _lifecycleLock = new object();
        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public KoploperLogicalSectionShadowObserver(
            IKoploperSnapshotReader snapshotReader,
            IKoploperReservationObserver reservationObserver,
            string profileId,
            IReadOnlyDictionary<int, int> internalBlockToSection,
            KoploperLogicalSectionShadowObserverOptions? options = null,
            Func<DateTimeOffset>? clock = null)
        {
            _snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
            _reservationObserver = reservationObserver ?? throw new ArgumentNullException(nameof(reservationObserver));
            _profileId = profileId ?? throw new ArgumentNullException(nameof(profileId));
            _internalBlockToSection = internalBlockToSection ?? throw new ArgumentNullException(nameof(internalBlockToSection));
            _options = options ?? new KoploperLogicalSectionShadowObserverOptions();
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        /// <inheritdoc />
        public KoploperLogicalSectionShadowObservation? CurrentShadow => _current;

        /// <inheritdoc />
        public KoploperLogicalSectionShadowObservation Refresh()
        {
            KoploperLogicalSectionShadowObservation shadow;
            try
            {
                shadow = ReadAndProject();
            }
            catch (Exception)
            {
                // An unexpected exception (for example a clock seam throwing) must still publish a
                // non-authoritative shadow rather than surfacing the fault or reusing old state.
                shadow = UnavailableShadow(_shadowSequence, _clock());
            }

            _current = shadow;
            return shadow;
        }

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken)
        {
            lock (_lifecycleLock)
            {
                if (_cts is not null)
                {
                    return; // already running; idempotent
                }

                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _cts = cts;
                _loopTask = Task.Run(() => RunLoopAsync(cts.Token), cts.Token);
            }
        }

        /// <inheritdoc />
        public async Task StopAsync()
        {
            CancellationTokenSource? cts;
            Task? loopTask;

            lock (_lifecycleLock)
            {
                cts = _cts;
                loopTask = _loopTask;
                _cts = null;
                _loopTask = null;
            }

            if (cts is null)
            {
                return; // not started; idempotent
            }

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The linked source may already be disposed; cancellation is best-effort here.
            }

            if (loopTask is not null)
            {
                try
                {
                    await loopTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected after cancellation.
                }
            }

            cts.Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        private KoploperLogicalSectionShadowObservation ReadAndProject()
        {
            DateTimeOffset observedAtUtc = _clock();

            if (!TryIncrementShadowSequence(out long shadowSequence))
            {
                return UnavailableShadow(_shadowSequence, observedAtUtc);
            }

            KoploperSnapshotReadResult readResult = _snapshotReader.ReadSnapshot();
            KoploperReservationObservation? observation = _reservationObserver.CurrentObservation;

            if (observation is null)
            {
                return UnavailableShadow(shadowSequence, observedAtUtc);
            }

            return KoploperLogicalSectionShadowProjector.Project(
                observation,
                readResult.Snapshot,
                _profileId,
                _internalBlockToSection,
                shadowSequence,
                observedAtUtc,
                _options.MaxSnapshotAge);
        }

        private async Task RunLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Refresh();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    // Refresh already publishes an unavailable shadow on unexpected exceptions;
                    // this guard only keeps the loop alive if a fault escapes it somehow.
                    _current = UnavailableShadow(_shadowSequence, _clock());
                }

                try
                {
                    await Task.Delay(PollingInterval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private bool TryIncrementShadowSequence(out long sequence)
        {
            try
            {
                sequence = checked(_shadowSequence + 1);
                _shadowSequence = sequence;
                return true;
            }
            catch (OverflowException)
            {
                sequence = _shadowSequence;
                return false;
            }
        }

        private KoploperLogicalSectionShadowObservation UnavailableShadow(long shadowSequence, DateTimeOffset observedAtUtc)
        {
            return new KoploperLogicalSectionShadowObservation(
                Generation: null,
                SourceSequence: 0,
                ShadowSequence: shadowSequence,
                CapturedAtUtc: observedAtUtc,
                ObservedAtUtc: observedAtUtc,
                IsFresh: false,
                SourceHealth: KoploperSourceHealth.Unavailable,
                SourceAuthoritative: false,
                ProfileId: _profileId,
                MappingValid: false,
                ManualStateValid: false,
                ShadowValid: false,
                Locomotives: Array.Empty<LogicalLocomotiveShadow>(),
                ManualBlockedSections: Array.Empty<int>(),
                UnmappedBlocks: Array.Empty<KoploperUnmappedBlock>(),
                Conflicts: Array.Empty<KoploperLogicalSectionConflict>(),
                Diagnostics: Array.Empty<KoploperDiagnosticCode>());
        }
    }
}
