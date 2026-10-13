using System;
using System.IO;
using System.Text.Json;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Offline tests for <see cref="CarrierTimingTrace"/>: it writes one JSON object per line, records
    /// the cycle timing fields, and resolves command -> write latency from a small in-memory map.
    /// No hardware, no comm, no real time (injected clock).
    /// </summary>
    public class CarrierTimingTraceTests
    {
        private static string TempFile()
            => Path.Combine(Path.GetTempPath(), $"carrier-timing-{Guid.NewGuid():N}.jsonl");

        [Fact]
        public void RecordCycle_WritesCycleLine()
        {
            string path = TempFile();
            try
            {
                var clock = new MutableClock(new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero));
                var trace = new CarrierTimingTrace(path, () => clock.UtcNow);

                trace.RecordCycle(
                    cycle: 0,
                    timestampUtc: clock.UtcNow,
                    physicalDataAgeMs: 120.5,
                    shadowAgeMs: 40.0,
                    reconcileDurationMs: 0.8,
                    carrierDecisionCount: 3);

                using var doc = JsonDocument.Parse(Assert.Single(File.ReadAllLines(path)));
                JsonElement root = doc.RootElement;

                Assert.Equal("carrier_cycle", root.GetProperty("event").GetString());
                Assert.Equal(0L, root.GetProperty("cycle").GetInt64());
                Assert.Equal(120.5, root.GetProperty("physicalDataAgeMs").GetDouble());
                Assert.Equal(40.0, root.GetProperty("shadowAgeMs").GetDouble());
                Assert.Equal(0.8, root.GetProperty("reconcileDurationMs").GetDouble());
                Assert.Equal(3, root.GetProperty("carrierDecisionCount").GetInt32());
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void RecordCommandThenWrite_WritesLatencyLine()
        {
            string path = TempFile();
            try
            {
                var clock = new MutableClock(new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero));
                var trace = new CarrierTimingTrace(path, () => clock.UtcNow);

                trace.RecordCommand(1, clock.UtcNow);
                clock.UtcNow = clock.UtcNow.AddMilliseconds(97);
                trace.RecordWrite(1, 650);

                using var doc = JsonDocument.Parse(Assert.Single(File.ReadAllLines(path)));
                JsonElement root = doc.RootElement;

                Assert.Equal("carrier_write_latency", root.GetProperty("event").GetString());
                Assert.Equal(1, root.GetProperty("amplifier").GetInt32());
                Assert.Equal(650, root.GetProperty("hr0").GetInt32());
                Assert.Equal(97.0, root.GetProperty("commandToWriteMs").GetDouble());
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void RecordWrite_WithoutCommand_WritesNothing()
        {
            string path = TempFile();
            try
            {
                var trace = new CarrierTimingTrace(path, () => new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero));

                // No prior command -> no matching latency, and no file is created.
                trace.RecordWrite(1, 650);

                Assert.False(File.Exists(path));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
