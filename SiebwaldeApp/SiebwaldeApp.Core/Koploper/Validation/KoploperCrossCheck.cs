namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// Identifies which source produced an independent sample used to cross-check the
    /// authoritative reservation observation. The numeric ordering is stable.
    /// </summary>
    public enum KoploperCrossCheckSource
    {
        /// <summary>The very source under validation (not independent).</summary>
        SubjectUnderValidation = 0,

        /// <summary>State rendered for the operator by the Koploper GUI.</summary>
        OperatorVisibleState = 1,

        /// <summary>The Koploper external-information stream on port 5700.</summary>
        Port5700 = 2,

        /// <summary>A legacy direct memory probe of the Koploper process.</summary>
        LegacyMemoryProbe = 3
    }

    /// <summary>
    /// How independent a cross-check source is from the subject under validation. The numeric
    /// ordering is stable and not a severity ranking.
    /// </summary>
    public enum KoploperSourceIndependence
    {
        /// <summary>Not independent: the sample comes from the same underlying source.</summary>
        NotIndependent = 0,

        /// <summary>An independent rendering of the same state (for example a GUI view).</summary>
        IndependentRendering = 1,

        /// <summary>An independent source of the same kind of data.</summary>
        IndependentSource = 2,

        /// <summary>A cross-implementation source (a different protocol/implementation).</summary>
        CrossImplementation = 3
    }

    /// <summary>
    /// Pure classification helpers for cross-check sources: how independent a source is, and a
    /// stable human-readable label for tracing/evidence.
    /// </summary>
    public static class KoploperCrossCheckClassification
    {
        /// <summary>Maps a cross-check source to its independence class.</summary>
        public static KoploperSourceIndependence GetIndependence(KoploperCrossCheckSource source) => source switch
        {
            KoploperCrossCheckSource.SubjectUnderValidation => KoploperSourceIndependence.NotIndependent,
            KoploperCrossCheckSource.OperatorVisibleState => KoploperSourceIndependence.IndependentRendering,
            KoploperCrossCheckSource.Port5700 => KoploperSourceIndependence.IndependentSource,
            KoploperCrossCheckSource.LegacyMemoryProbe => KoploperSourceIndependence.CrossImplementation,
            _ => KoploperSourceIndependence.NotIndependent
        };

        /// <summary>Returns a stable, human-readable label for a cross-check source.</summary>
        public static string GetMatchLabel(KoploperCrossCheckSource source) => source switch
        {
            KoploperCrossCheckSource.SubjectUnderValidation => "subject",
            KoploperCrossCheckSource.OperatorVisibleState => "operator-gui",
            KoploperCrossCheckSource.Port5700 => "port-5700",
            KoploperCrossCheckSource.LegacyMemoryProbe => "legacy-memory-probe",
            _ => "unknown"
        };
    }
}
