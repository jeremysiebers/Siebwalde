using System.Collections.Generic;
using SiebwaldeApp.Core.Koploper.Validation;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper.Validation
{
    /// <summary>
    /// Unit tests for <see cref="KoploperContradictionClassifier"/>: which mismatch-class
    /// results persist after crossing the configured persistence threshold, and that benign
    /// outcomes are never reported as persisted contradictions.
    /// </summary>
    public class KoploperContradictionClassifierTests
    {
        private static readonly KoploperValidationOptions Options =
            new(contradictionPersistenceSamples: 3);

        [Fact]
        public void Classify_MismatchAtOrAbovePersistenceSamples_IsPersisted()
        {
            var perSample = new[]
            {
                KoploperValidationResult.CurrentPositionMismatch,
                KoploperValidationResult.CurrentPositionMismatch,
                KoploperValidationResult.CurrentPositionMismatch,
            };

            IReadOnlyList<KoploperValidationResult> persisted =
                KoploperContradictionClassifier.Classify(perSample, Options);

            Assert.Equal(new[] { KoploperValidationResult.CurrentPositionMismatch }, persisted);
        }

        [Fact]
        public void Classify_MismatchBelowPersistenceSamples_IsNotPersisted()
        {
            var perSample = new[]
            {
                KoploperValidationResult.CurrentPositionMismatch,
                KoploperValidationResult.CurrentPositionMismatch,
            };

            IReadOnlyList<KoploperValidationResult> persisted =
                KoploperContradictionClassifier.Classify(perSample, Options);

            Assert.Empty(persisted);
        }

        [Fact]
        public void Classify_Match_IsNeverPersisted()
        {
            var perSample = new[]
            {
                KoploperValidationResult.Match,
                KoploperValidationResult.Match,
                KoploperValidationResult.Match,
                KoploperValidationResult.Match,
            };

            IReadOnlyList<KoploperValidationResult> persisted =
                KoploperContradictionClassifier.Classify(perSample, Options);

            Assert.Empty(persisted);
        }
    }
}
