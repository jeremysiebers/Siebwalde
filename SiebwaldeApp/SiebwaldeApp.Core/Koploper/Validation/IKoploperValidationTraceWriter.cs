namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// Writes a structured validation trace: samples plus scenario/segment boundaries. Implementations
    /// are responsible for serializing each event as a self-contained record.
    /// </summary>
    public interface IKoploperValidationTraceWriter
    {
        /// <summary>Writes one validation sample.</summary>
        void WriteSample(KoploperValidationSample sample);

        /// <summary>Writes a boundary between segments of the trace.</summary>
        void WriteSegmentBoundary(string segmentName);

        /// <summary>Writes the start of a validation scenario.</summary>
        void WriteScenarioStart(string scenarioId);

        /// <summary>Writes the end of a validation scenario.</summary>
        void WriteScenarioEnd(string scenarioId);
    }
}
