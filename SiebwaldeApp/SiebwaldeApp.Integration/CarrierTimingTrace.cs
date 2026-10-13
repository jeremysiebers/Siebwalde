using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Lightweight, machine-readable carrier-chain timing trace. It writes one JSON object per line
    /// (JSONL) to <c>carrier-timing.jsonl</c> under the configured log directory, so the Project
    /// Lead/operator can judge whether the whole carrier chain (physical observation -> shadow ->
    /// reconcile -> apply -> physical write) is fast enough at high speed.
    ///
    /// It is deliberately NON-BLOCKING and BOUNDED: the feeder writes at most one cycle line per loop
    /// iteration (the loop already runs at ~500 ms = 2 Hz), and the command -> write latency is
    /// resolved inside a small in-memory map with no Task.Delay/sleep and no second comm client.
    /// File writes are best-effort: a write failure is swallowed so the diagnostic trace can never
    /// affect the carrier loop or the physical write path.
    /// </summary>
    public sealed class CarrierTimingTrace
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private readonly string _filePath;
        private readonly Func<DateTimeOffset> _clock;
        private readonly object _gate = new();
        private readonly Dictionary<int, DateTimeOffset> _commandAtUtc = new();

        public CarrierTimingTrace(string filePath, Func<DateTimeOffset>? clock = null)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        /// <summary>Records one carrier-cycle timing sample (at most once per feeder loop iteration).</summary>
        public void RecordCycle(
            long cycle,
            DateTimeOffset timestampUtc,
            double physicalDataAgeMs,
            double shadowAgeMs,
            double reconcileDurationMs,
            int carrierDecisionCount)
        {
            Write(new CycleLine(
                "carrier_cycle",
                cycle,
                timestampUtc,
                Round(physicalDataAgeMs),
                Round(shadowAgeMs),
                Round(reconcileDurationMs),
                carrierDecisionCount));
        }

        /// <summary>Records the command timestamp for an amplifier whose HR0 the carrier just commanded.</summary>
        public void RecordCommand(ushort amplifier, DateTimeOffset commandAtUtc)
        {
            lock (_gate)
            {
                _commandAtUtc[amplifier] = commandAtUtc;
            }
        }

        /// <summary>
        /// Resolves the command -> write latency for an amplifier whose HoldingReg0 was actually
        /// transmitted by the runtime writer. When a prior command exists, a latency line is written;
        /// otherwise nothing is written (the write had no matching carrier command).
        /// </summary>
        public void RecordWrite(ushort amplifier, int hr0)
        {
            DateTimeOffset writeAtUtc = _clock();
            DateTimeOffset commandAtUtc;

            lock (_gate)
            {
                if (!_commandAtUtc.TryGetValue(amplifier, out commandAtUtc))
                {
                    return;
                }

                _commandAtUtc.Remove(amplifier);
            }

            Write(new WriteLatencyLine(
                "carrier_write_latency",
                amplifier,
                hr0,
                commandAtUtc,
                writeAtUtc,
                Round(Math.Max(0, (writeAtUtc - commandAtUtc).TotalMilliseconds))));
        }

        private void Write<T>(T line)
        {
            string json;
            try
            {
                json = JsonSerializer.Serialize(line, JsonOptions);
            }
            catch
            {
                return; // best-effort: serialization must never affect the caller.
            }

            try
            {
                lock (_gate)
                {
                    File.AppendAllText(_filePath, json + Environment.NewLine);
                }
            }
            catch
            {
                // Best-effort: a timing-trace I/O fault must never affect the carrier loop.
            }
        }

        private static double Round(double value)
            => Math.Round(value, 3, MidpointRounding.AwayFromZero);

        private sealed record CycleLine(
            string Event,
            long Cycle,
            DateTimeOffset TimestampUtc,
            double PhysicalDataAgeMs,
            double ShadowAgeMs,
            double ReconcileDurationMs,
            int CarrierDecisionCount);

        private sealed record WriteLatencyLine(
            string Event,
            int Amplifier,
            int Hr0,
            DateTimeOffset CommandAtUtc,
            DateTimeOffset WriteAtUtc,
            double CommandToWriteMs);
    }
}
