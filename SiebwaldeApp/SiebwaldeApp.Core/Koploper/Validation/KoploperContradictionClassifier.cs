using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// Pure, stateless reducer that turns a sequence of per-sample validation results into the
    /// set of contradictions that have persisted across enough samples to be treated as durable.
    /// Only mismatch-class results are considered contradictions; benign outcomes (Match,
    /// ExpectedTransitionSkew) and source-level failures are not reported as persisted
    /// contradictions here.
    /// </summary>
    public static class KoploperContradictionClassifier
    {
        /// <summary>
        /// Returns the distinct contradiction results that each appear at least
        /// <see cref="KoploperValidationOptions.ContradictionPersistenceSamples"/> times in
        /// <paramref name="perSample"/>, ordered by enum value for determinism.
        /// </summary>
        public static IReadOnlyList<KoploperValidationResult> Classify(
            IReadOnlyList<KoploperValidationResult> perSample,
            KoploperValidationOptions options)
        {
            ArgumentNullException.ThrowIfNull(perSample);
            ArgumentNullException.ThrowIfNull(options);

            var counts = new Dictionary<KoploperValidationResult, int>();
            foreach (KoploperValidationResult result in perSample)
            {
                counts[result] = counts.TryGetValue(result, out int current) ? current + 1 : 1;
            }

            var persisted = new List<KoploperValidationResult>();
            foreach (KoploperValidationResult result in counts.Keys.OrderBy(r => (int)r))
            {
                if (counts[result] >= options.ContradictionPersistenceSamples && IsContradiction(result))
                {
                    persisted.Add(result);
                }
            }

            return persisted;
        }

        private static bool IsContradiction(KoploperValidationResult result) => result switch
        {
            KoploperValidationResult.CurrentPositionMismatch => true,
            KoploperValidationResult.OwnerMismatch => true,
            KoploperValidationResult.ReservationMismatch => true,
            KoploperValidationResult.GenerationMismatch => true,
            _ => false
        };
    }
}
