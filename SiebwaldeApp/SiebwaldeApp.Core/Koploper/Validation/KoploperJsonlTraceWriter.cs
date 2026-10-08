using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// Writes the validation trace as JSON Lines: exactly one self-contained JSON object per line,
    /// via <see cref="System.Text.Json"/>. Each line carries a <c>type</c> discriminator plus the
    /// event-specific fields. No Windows-specific APIs are used.
    /// </summary>
    public sealed class KoploperJsonlTraceWriter : IKoploperValidationTraceWriter, IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly TextWriter _writer;
        private readonly bool _leaveOpen;
        private bool _disposed;

        /// <summary>
        /// Creates a writer over an existing <see cref="TextWriter"/>. When
        /// <paramref name="leaveOpen"/> is <c>false</c> the underlying writer is disposed with this
        /// instance.
        /// </summary>
        public KoploperJsonlTraceWriter(TextWriter writer, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(writer);
            _writer = writer;
            _leaveOpen = leaveOpen;
        }

        /// <summary>Creates a writer that appends to a UTF-8 (no BOM) file.</summary>
        public KoploperJsonlTraceWriter(string filePath)
        {
            ArgumentException.ThrowIfNullOrEmpty(filePath);
            _writer = new StreamWriter(filePath, append: true, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            _leaveOpen = false;
        }

        public void WriteSample(KoploperValidationSample sample)
        {
            ArgumentNullException.ThrowIfNull(sample);
            WriteLine(JsonSerializer.Serialize(new { type = "sample", sample }, JsonOptions));
        }

        public void WriteSegmentBoundary(string segmentName)
        {
            ArgumentException.ThrowIfNullOrEmpty(segmentName);
            WriteLine(JsonSerializer.Serialize(new { type = "segmentBoundary", segmentName }, JsonOptions));
        }

        public void WriteScenarioStart(string scenarioId)
        {
            ArgumentException.ThrowIfNullOrEmpty(scenarioId);
            WriteLine(JsonSerializer.Serialize(new { type = "scenarioStart", scenarioId }, JsonOptions));
        }

        public void WriteScenarioEnd(string scenarioId)
        {
            ArgumentException.ThrowIfNullOrEmpty(scenarioId);
            WriteLine(JsonSerializer.Serialize(new { type = "scenarioEnd", scenarioId }, JsonOptions));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (!_leaveOpen)
            {
                _writer.Dispose();
            }
        }

        private void WriteLine(string json)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(KoploperJsonlTraceWriter));
            }

            _writer.WriteLine(json);
        }
    }
}
