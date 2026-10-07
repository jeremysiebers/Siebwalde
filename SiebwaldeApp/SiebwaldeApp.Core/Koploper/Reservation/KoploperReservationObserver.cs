using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Polling reservation observer. Each cycle performs one synchronous snapshot read, evaluates
    /// it with <see cref="KoploperReservationAggregator"/>, and publishes the observation via a
    /// volatile swap. A non-authoritative cycle publishes a fresh empty observation — ownership is
    /// never inherited from a previous cycle, and there is no last-known-good cache. The background
    /// loop never performs a sleep inside the read path; the only delay is the post-cycle
    /// <see cref="KoploperReservationObserverOptions.PollingInterval"/>.
    /// </summary>
    public sealed class KoploperReservationObserver : IKoploperReservationObserver
    {
        private readonly IKoploperSnapshotReader _snapshotReader;
        private readonly KoploperReservationObserverOptions _options;
        private readonly Func<DateTimeOffset> _clock;

        private volatile KoploperReservationObservation? _current;
        private long _observerSequence;

        private readonly object _lifecycleLock = new object();
        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public KoploperReservationObserver(
            IKoploperSnapshotReader snapshotReader,
            KoploperReservationObserverOptions? options = null,
            Func<DateTimeOffset>? clock = null)
        {
            _snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
            _options = options ?? new KoploperReservationObserverOptions();
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        /// <inheritdoc />
        public KoploperReservationObservation? CurrentObservation => _current;

        /// <inheritdoc />
        public KoploperReservationObservation Refresh()
        {
            KoploperReservationObservation observation;
            try
            {
                observation = ReadAndEvaluate();
            }
            catch (Exception)
            {
                // An unexpected exception (for example a clock seam throwing) must still publish a
                // non-authoritative observation rather than surfacing the fault or reusing old state.
                observation = UnavailableObservation(_observerSequence, DateTimeOffset.UtcNow);
            }

            _current = observation;
            return observation;
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

        private KoploperReservationObservation ReadAndEvaluate()
        {
            DateTimeOffset observedAtUtc = _clock();

            if (!TryIncrementObserverSequence(out long observerSequence))
            {
                // Sequence overflow: fail closed with a non-authoritative Unavailable observation.
                return UnavailableObservation(observerSequence, observedAtUtc);
            }

            KoploperSnapshotReadResult readResult = _snapshotReader.ReadSnapshot();
            return KoploperReservationAggregator.Evaluate(readResult, observerSequence, observedAtUtc, _options.MaxSnapshotAge);
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
                    // Refresh already publishes an unavailable observation on unexpected exceptions;
                    // this guard only keeps the loop alive if a fault escapes it somehow.
                    _current = UnavailableObservation(_observerSequence, DateTimeOffset.UtcNow);
                }

                try
                {
                    await Task.Delay(_options.PollingInterval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private bool TryIncrementObserverSequence(out long sequence)
        {
            try
            {
                sequence = checked(_observerSequence + 1);
                _observerSequence = sequence;
                return true;
            }
            catch (OverflowException)
            {
                sequence = _observerSequence;
                return false;
            }
        }

        private static KoploperReservationObservation UnavailableObservation(long observerSequence, DateTimeOffset observedAtUtc)
        {
            return new KoploperReservationObservation(
                Generation: null,
                SourceSequence: 0,
                ObserverSequence: observerSequence,
                CapturedAtUtc: observedAtUtc,
                ObservedAtUtc: observedAtUtc,
                IsFresh: false,
                IsConsistent: false,
                SourceHealth: KoploperSourceHealth.Unavailable,
                IsAuthoritative: false,
                AuthorityReason: KoploperReservationAuthorityReason.Unavailable,
                Locomotives: Array.Empty<KoploperLocomotiveTrajectory>(),
                Conflicts: Array.Empty<KoploperReservationConflict>(),
                Diagnostics: Array.Empty<KoploperDiagnosticCode>());
        }
    }
}
