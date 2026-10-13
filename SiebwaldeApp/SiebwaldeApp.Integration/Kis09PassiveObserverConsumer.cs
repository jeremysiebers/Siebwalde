using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// A bounded background consumer that drives a <see cref="Kis09PassiveObserver"/> during a
    /// production Real-mode run: it periodically calls <see cref="Kis09PassiveObserver.Refresh"/> and
    /// <see cref="Kis09PassiveObserver.ReadPhysicalObservation"/>, builds a passive trace record via
    /// <see cref="KoploperTelemetryTraceBuilder.BuildPassive"/>, and appends one machine-readable JSONL
    /// line to a trace file. It is strictly PASSIVE: it references no comm client, transport, session
    /// guard, or control/write type.
    /// </summary>
    public sealed class Kis09PassiveObserverConsumer : IAsyncDisposable
    {
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(500);

        private readonly Kis09PassiveObserver _observer;
        private readonly string _traceFilePath;
        private readonly TimeSpan _interval;
        private readonly Func<DateTimeOffset> _clock;
        private readonly object _sync = new();

        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public Kis09PassiveObserverConsumer(
            Kis09PassiveObserver observer,
            string traceFilePath,
            TimeSpan? interval = null,
            Func<DateTimeOffset>? clock = null)
        {
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
            _traceFilePath = traceFilePath ?? throw new ArgumentNullException(nameof(traceFilePath));
            _interval = interval ?? DefaultInterval;
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Begins the background trace loop. Idempotent: a second call while running is a no-op.
        /// The supplied token is linked, so cancelling the runtime cancels the loop.
        /// </summary>
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

        /// <summary>Cancels and awaits the background loop (writes the scenario-end line). Idempotent.</summary>
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
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                Converters = { new JsonStringEnumConverter() }
            };

            // Open with FileShare.ReadWrite + AutoFlush so an operator (or a test) can tail/read the
            // trace while the run is still writing it.
            using var fileStream = new FileStream(
                _traceFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite);
            using var file = new StreamWriter(
                fileStream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            {
                AutoFlush = true
            };

            WriteLine(file, jsonOptions, new { type = "kis09PassiveScenarioStart", timestampUtc = _clock().ToString("O") });

            long cycle = 0;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        LogicalPhysicalReconciliationObservation reconciliation = _observer.Refresh();
                        PhysicalSectionOccupancyObservation physical = _observer.ReadPhysicalObservation();
                        KoploperTelemetryTraceRecord trace = _observer.BuildTraceRecord(cycle, _clock(), reconciliation, physical);
                        WriteLine(file, jsonOptions, trace);
                        cycle++;
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
            finally
            {
                WriteLine(file, jsonOptions, new { type = "kis09PassiveScenarioEnd", timestampUtc = _clock().ToString("O") });
            }
        }

        private static void WriteLine(TextWriter writer, JsonSerializerOptions options, object value)
            => writer.WriteLine(JsonSerializer.Serialize(value, options));
    }
}
