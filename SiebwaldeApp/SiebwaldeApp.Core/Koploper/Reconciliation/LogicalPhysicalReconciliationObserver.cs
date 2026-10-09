using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Polling logical/physical reconciliation observer. Each cycle pulls the current logical-section
    /// shadow (<see cref="IKoploperLogicalSectionShadowObserver.CurrentShadow"/>) and a fresh physical
    /// section occupancy read (<see cref="IPhysicalSectionOccupancySource.Read"/>), reconciles both via
    /// <see cref="LogicalPhysicalReconciliationProjector"/>, and publishes the result through a
    /// volatile swap. A null shadow or an unexpected exception publishes a fresh non-assessable
    /// observation — no previous reconciliation is ever inherited, and there is no last-known-good cache.
    /// </summary>
    public sealed class LogicalPhysicalReconciliationObserver : ILogicalPhysicalReconciliationObserver
    {
        private readonly IKoploperLogicalSectionShadowObserver _shadowObserver;
        private readonly IPhysicalSectionOccupancySource _physicalSource;
        private readonly string _profileId;
        private readonly IReadOnlyCollection<int> _logicallyBoundSectionIds;
        private readonly LogicalPhysicalReconciliationObserverOptions _options;
        private readonly Func<DateTimeOffset> _clock;

        private volatile LogicalPhysicalReconciliationObservation? _current;
        private long _reconciliationSequence;

        private readonly object _lifecycleLock = new object();
        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public LogicalPhysicalReconciliationObserver(
            IKoploperLogicalSectionShadowObserver shadowObserver,
            IPhysicalSectionOccupancySource physicalSource,
            string profileId,
            IReadOnlyCollection<int> logicallyBoundSectionIds,
            LogicalPhysicalReconciliationObserverOptions? options = null,
            Func<DateTimeOffset>? clock = null)
        {
            _shadowObserver = shadowObserver ?? throw new ArgumentNullException(nameof(shadowObserver));
            _physicalSource = physicalSource ?? throw new ArgumentNullException(nameof(physicalSource));
            _profileId = profileId ?? throw new ArgumentNullException(nameof(profileId));
            _logicallyBoundSectionIds = logicallyBoundSectionIds ?? throw new ArgumentNullException(nameof(logicallyBoundSectionIds));
            _options = options ?? new LogicalPhysicalReconciliationObserverOptions();
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        /// <inheritdoc />
        public LogicalPhysicalReconciliationObservation? CurrentObservation => _current;

        /// <inheritdoc />
        public LogicalPhysicalReconciliationObservation Refresh()
        {
            LogicalPhysicalReconciliationObservation observation;
            try
            {
                observation = ReadAndReconcile();
            }
            catch (Exception)
            {
                // An unexpected exception (for example a physical read or clock seam throwing) must
                // still publish a fresh non-assessable observation rather than surfacing the fault or
                // reusing old state.
                observation = UnavailableObservation(_reconciliationSequence, _clock());
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

        private LogicalPhysicalReconciliationObservation ReadAndReconcile()
        {
            DateTimeOffset evaluatedAtUtc = _clock();

            if (!TryIncrementReconciliationSequence(out long reconciliationSequence))
            {
                return UnavailableObservation(_reconciliationSequence, evaluatedAtUtc);
            }

            KoploperLogicalSectionShadowObservation? shadow = _shadowObserver.CurrentShadow;
            PhysicalSectionOccupancyObservation physical = _physicalSource.Read();

            return LogicalPhysicalReconciliationProjector.Reconcile(
                shadow,
                physical,
                _logicallyBoundSectionIds,
                reconciliationSequence,
                evaluatedAtUtc);
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
                    // Refresh already publishes a non-assessable observation on unexpected exceptions;
                    // this guard only keeps the loop alive if a fault escapes it somehow.
                    _current = UnavailableObservation(_reconciliationSequence, _clock());
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

        private bool TryIncrementReconciliationSequence(out long sequence)
        {
            try
            {
                sequence = checked(_reconciliationSequence + 1);
                _reconciliationSequence = sequence;
                return true;
            }
            catch (OverflowException)
            {
                sequence = _reconciliationSequence;
                return false;
            }
        }

        private LogicalPhysicalReconciliationObservation UnavailableObservation(long reconciliationSequence, DateTimeOffset evaluatedAtUtc)
        {
            return new LogicalPhysicalReconciliationObservation(
                ProfileId: _profileId,
                LogicalGeneration: null,
                LogicalSourceSequence: 0,
                LogicalShadowSequence: 0,
                PhysicalGeneration: 0,
                PhysicalSequence: 0,
                ReconciliationSequence: reconciliationSequence,
                EvaluatedAtUtc: evaluatedAtUtc,
                LogicalSourceValid: false,
                PhysicalSourceValid: false,
                ReconciliationAssessable: false,
                Sections: Array.Empty<LogicalSectionReconciliation>(),
                Locomotives: Array.Empty<LogicalLocomotiveReconciliation>(),
                Diagnostics: Array.Empty<ReconciliationDiagnosticCode>());
        }
    }
}
