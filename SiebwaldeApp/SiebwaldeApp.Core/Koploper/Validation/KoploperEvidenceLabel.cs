namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// Strength of evidence backing a validated claim. The numeric ordering is stable and is not a
    /// severity ranking.
    /// </summary>
    public enum KoploperEvidenceLabel
    {
        /// <summary>Proven against a live running system.</summary>
        LiveProven = 0,

        /// <summary>Proven by a synthetic test/simulation.</summary>
        SyntheticallyProven = 1,

        /// <summary>Proven by a golden proof-of-concept (reference capture).</summary>
        GoldenPocProven = 2,

        /// <summary>Confirmed by an independent cross-implementation source.</summary>
        CrossImplementationConfirmed = 3,

        /// <summary>Not observed in any evidence source.</summary>
        NotObserved = 4,

        /// <summary>Not proven (insufficient or contradictory evidence).</summary>
        NotProven = 5
    }
}
