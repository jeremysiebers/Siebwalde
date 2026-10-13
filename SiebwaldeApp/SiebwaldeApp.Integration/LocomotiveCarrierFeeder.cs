using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// A bounded background loop that FEEDS the ownership-aware carrier from the production-held KIS-09
    /// passive observer: each cycle it refreshes the observer, feeds the KIS-07 shadow and the KIS-08
    /// physical occupancy into the carrier, emits the observed HR0, and routes the resulting escalation
    /// flags through the supplied delegate. It is PASSIVE: no comm client, transport, session guard or
    /// control write path (the carrier writes through <see cref="LocomotiveCarrierController"/>).
    /// </summary>
    public sealed class LocomotiveCarrierFeeder : IAsyncDisposable
    {
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(500);

        private readonly Kis09PassiveObserver _observer;
        private readonly LocomotiveCarrierController _carrier;
        private readonly Action<CarrierDecision>? _onEscalation;
        private readonly PhysicalOccupancyDeassertFilter _deassertFilter;
        private readonly CarrierTimingTrace? _timingTrace;
        private readonly Func<DateTimeOffset> _clock;
        private readonly TimeSpan _interval;
        private readonly object _sync = new();

        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private long _cycle;

        public LocomotiveCarrierFeeder(
            Kis09PassiveObserver observer,
            LocomotiveCarrierController carrier,
            Action<CarrierDecision>? onEscalation = null,
            TimeSpan? interval = null,
            TimeSpan? clearDebounceMs = null,
            Func<DateTimeOffset>? clock = null,
            CarrierTimingTrace? timingTrace = null)
        {
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
            _carrier = carrier ?? throw new ArgumentNullException(nameof(carrier));
            _onEscalation = onEscalation;
            _deassertFilter = new PhysicalOccupancyDeassertFilter(clearDebounceMs, clock);
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
            _timingTrace = timingTrace;
            _interval = interval ?? DefaultInterval;
        }

        /// <summary>Begins the background feed loop. Idempotent.</summary>
        public void Start(CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if (_cts is not null)
                {
                    return;
                }

                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _loopTask = Task.Run(() => RunLoopAsync(_cts.Token), _cts.Token);
            }
        }

        /// <summary>Cancels and awaits the background loop. Idempotent.</summary>
        public async Task StopAsync()
        {
            CancellationTokenSource? cts;
            Task? loopTask;

            lock (_sync)
            {
                cts = _cts;
                loopTask = _loopTask;
                _cts = null;
                _loopTask = null;
            }

            if (cts is null)
            {
                return;
            }

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Best-effort.
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
        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
        }

        private async Task RunLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _observer.Refresh();
                    var shadow = _observer.ReadShadowObservation();
                    var physical = _deassertFilter.Filter(_observer.ReadPhysicalObservation());

                    var sw = Stopwatch.StartNew();
                    _carrier.OnShadowChanged(shadow);
                    _carrier.OnOccupancyChanged(physical);
                    sw.Stop();

                    _carrier.EmitObservedHr0();
                    RecordTiming(shadow, physical, sw.Elapsed.TotalMilliseconds);

                    if (_onEscalation is not null && _carrier.LastDecision is { } decision)
                    {
                        _onEscalation(decision);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // A transient read/serialize fault must not kill the loop.
                }

                try
                {
                    await Task.Delay(_interval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void RecordTiming(
            KoploperLogicalSectionShadowObservation? shadow,
            PhysicalSectionOccupancyObservation physical,
            double reconcileDurationMs)
        {
            if (_timingTrace is null)
            {
                return;
            }

            DateTimeOffset now = _clock();
            double physicalAgeMs = physical.CapturedAtUtc == default
                ? -1
                : Math.Max(0, (now - physical.CapturedAtUtc).TotalMilliseconds);
            double shadowAgeMs = shadow is null
                ? -1
                : Math.Max(0, (now - shadow.ObservedAtUtc).TotalMilliseconds);

            CarrierDecision? decision = _carrier.LastDecision;
            int decisionCount = decision?.Amplifiers.Count ?? 0;

            _timingTrace.RecordCycle(
                _cycle++,
                now,
                physicalAgeMs,
                shadowAgeMs,
                reconcileDurationMs,
                decisionCount);

            // Record the command timestamp for every physical amplifier the carrier just commanded
            // (drive/hold/neutral), so the runtime writer can resolve the command -> write latency.
            if (decision is not null)
            {
                foreach (var d in decision.Amplifiers)
                {
                    if (TrackAmplifierAddress.IsTrackAmplifierAddress(d.Amplifier))
                    {
                        _timingTrace.RecordCommand((ushort)d.Amplifier, now);
                    }
                }
            }
        }
    }
}
