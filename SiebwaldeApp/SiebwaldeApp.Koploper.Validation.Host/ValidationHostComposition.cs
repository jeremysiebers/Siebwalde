using System;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.Koploper.Validation;
using SiebwaldeApp.Koploper.Windows;

namespace SiebwaldeApp.Koploper.Validation.Host
{
    /// <summary>
    /// Wires the real, read-only Koploper internal-state pipeline for the cross-validation
    /// harness: Windows process locator + memory reader + the 9.4 memory layout + raw
    /// object-graph reader + block-state decoder + snapshot reader + reservation observer, all
    /// sharing one <see cref="Func{DateTimeOffset}"/> clock, plus a JSONL validation-trace writer.
    /// This is composition/configuration only; no domain logic lives here.
    /// </summary>
    public sealed class ValidationHostComposition
    {
        /// <summary>The shared clock injected into the snapshot reader and observer.</summary>
        public Func<DateTimeOffset> Clock { get; }

        /// <summary>The composed reservation observer (owns the read pipeline).</summary>
        public KoploperReservationObserver Observer { get; }

        /// <summary>The JSONL trace writer used to emit <see cref="KoploperValidationSample"/> lines.</summary>
        public KoploperJsonlTraceWriter TraceWriter { get; }

        private ValidationHostComposition(
            Func<DateTimeOffset> clock,
            KoploperReservationObserver observer,
            KoploperJsonlTraceWriter traceWriter)
        {
            Clock = clock;
            Observer = observer;
            TraceWriter = traceWriter;
        }

        /// <summary>
        /// Composes the real components. The <paramref name="clock"/> (defaults to
        /// <c>DateTimeOffset.UtcNow</c>) is shared by the snapshot reader and the reservation
        /// observer so freshness and observation time share one time source.
        /// </summary>
        /// <param name="traceFilePath">JSONL trace output path (UTF-8, no BOM, append mode).</param>
        /// <param name="clock">Optional shared clock; defaults to <c>DateTimeOffset.UtcNow</c>.</param>
        /// <param name="observerOptions">Optional reservation-observer options.</param>
        /// <param name="snapshotReadOptions">Optional snapshot-read options.</param>
        public static ValidationHostComposition Create(
            string traceFilePath,
            Func<DateTimeOffset>? clock = null,
            KoploperReservationObserverOptions? observerOptions = null,
            KoploperSnapshotReadOptions? snapshotReadOptions = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(traceFilePath);

            Func<DateTimeOffset> sharedClock = clock ?? (() => DateTimeOffset.UtcNow);

            IKoploperMemoryLayout layout = Koploper94MemoryLayout.Instance;
            IKoploperProcessLocator processLocator = new WindowsKoploperProcessLocator();
            IKoploperExecutableVerifier executableVerifier = new KoploperExecutableVerifier(layout);
            Func<IKoploperMemoryReader> memoryReaderFactory = () => new WindowsKoploperMemoryReader();
            IKoploperRawObjectGraphReader rawGraphReader =
                new KoploperRawObjectGraphReader(layout, new KoploperDecodePlausibility());
            IKoploperBlockStateDecoder blockStateDecoder = new KoploperBlockStateDecoder();

            var snapshotReader = new KoploperSnapshotReader(
                processLocator,
                executableVerifier,
                memoryReaderFactory,
                rawGraphReader,
                blockStateDecoder,
                snapshotReadOptions,
                sharedClock);

            var observer = new KoploperReservationObserver(snapshotReader, observerOptions, sharedClock);
            var traceWriter = new KoploperJsonlTraceWriter(traceFilePath);

            return new ValidationHostComposition(sharedClock, observer, traceWriter);
        }
    }
}
