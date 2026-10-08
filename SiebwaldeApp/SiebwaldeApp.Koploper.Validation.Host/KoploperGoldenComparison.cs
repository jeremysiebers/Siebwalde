using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.Koploper.Validation;

namespace SiebwaldeApp.Koploper.Validation.Host
{
    /// <summary>
    /// One golden comparison input for the <c>replay</c> command: the authoritative reservation
    /// observation plus the independent cross-check sample (possibly null when no independent
    /// source was captured), and an optional expected <see cref="KoploperValidationResult"/> to
    /// verify against. This is the lossless golden JSONL record consumed by the replay command —
    /// unlike the flattened <see cref="KoploperValidationSample"/>, it carries the complete
    /// comparison inputs so <see cref="KoploperComparisonEngine.Compare"/> can be re-run
    /// deterministically.
    /// </summary>
    public sealed record KoploperGoldenComparison(
        KoploperReservationObservation Observation,
        KoploperIndependentSample? Independent,
        KoploperValidationResult? Expected);
}
