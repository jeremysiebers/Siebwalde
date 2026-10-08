using System;
using System.IO;
using System.Text;
using System.Text.Json;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.Koploper.Validation;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper.Validation
{
    /// <summary>
    /// Unit tests for <see cref="KoploperJsonlTraceWriter"/>: exactly one JSON object per line,
    /// valid JSONL for samples and segment boundaries, and flush-on-dispose.
    /// </summary>
    public class KoploperJsonlTraceWriterTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void WriteSample_WritesOneJsonObjectPerLine()
        {
            var buffer = new StringWriter();
            var writer = new KoploperJsonlTraceWriter(buffer, leaveOpen: true);

            writer.WriteSample(Sample());
            writer.WriteSample(Sample(KoploperValidationResult.CurrentPositionMismatch));

            string[] lines = SplitLines(buffer.ToString());
            Assert.Equal(2, lines.Length);
            foreach (string line in lines)
            {
                using JsonDocument document = JsonDocument.Parse(line);
                Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
                Assert.True(document.RootElement.TryGetProperty("type", out JsonElement type));
                Assert.Equal("sample", type.GetString());
            }
        }

        [Fact]
        public void WriteSampleAndSegmentBoundary_ProduceValidJsonl()
        {
            var buffer = new StringWriter();
            var writer = new KoploperJsonlTraceWriter(buffer, leaveOpen: true);

            writer.WriteSample(Sample());
            writer.WriteSegmentBoundary("segment-1");
            writer.WriteScenarioStart("scenario-1");
            writer.WriteScenarioEnd("scenario-1");

            string[] lines = SplitLines(buffer.ToString());
            Assert.Equal(4, lines.Length);

            foreach (string line in lines)
            {
                using JsonDocument document = JsonDocument.Parse(line);
                Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
                Assert.True(document.RootElement.TryGetProperty("type", out JsonElement type));
                Assert.Contains(
                    type.GetString(),
                    new[] { "sample", "segmentBoundary", "scenarioStart", "scenarioEnd" });
            }
        }

        [Fact]
        public void Dispose_Flushes()
        {
            var stream = new MemoryStream();
            var inner = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 1024, leaveOpen: true);
            var writer = new KoploperJsonlTraceWriter(inner); // leaveOpen: false -> disposes inner on Dispose()

            writer.WriteSample(Sample());
            writer.Dispose();

            string content = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Contains("\"type\":\"sample\"", content);
            Assert.Single(SplitLines(content));
        }

        private static KoploperValidationSample Sample(KoploperValidationResult result = KoploperValidationResult.Match)
        {
            return new KoploperValidationSample(
                ValidatedAtUtc: Now,
                ProcessId: 100,
                Generation: new KoploperProcessGeneration(100, new DateTimeOffset(1000, TimeSpan.Zero)),
                SourceSequence: 1,
                ObserverSequence: 1,
                SourceHealth: KoploperSourceHealth.Healthy,
                IsAuthoritative: true,
                AuthorityReason: KoploperReservationAuthorityReason.Authoritative,
                LocId: 5,
                OccupiedBlock: 17,
                ReservedBlocks: new[] { 19, 20 },
                Port5700CurrentBlock: 17,
                Port5700Loc: 5,
                CrossCheckSource: KoploperCrossCheckSource.Port5700,
                GuiMarker: null,
                ScenarioId: "unit-test",
                Result: result,
                Note: "hello");
        }

        private static string[] SplitLines(string text)
            => text.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }
}
