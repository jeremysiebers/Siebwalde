using System;
using SiebwaldeApp.Core.Koploper.Validation;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper.Validation
{
    /// <summary>
    /// Unit tests for <see cref="KoploperMergeBlockPolicy"/>: blocking contradictions, failed
    /// cancellation scenarios, and the default merge-ready disposition.
    /// </summary>
    public class KoploperMergeBlockPolicyTests
    {
        [Fact]
        public void Evaluate_PersistedCurrentPositionMismatch_ReturnsBlocked()
        {
            var persisted = new[] { KoploperValidationResult.CurrentPositionMismatch };

            KoploperMergeDisposition disposition =
                KoploperMergeBlockPolicy.Evaluate(persisted, cancellationProducedExpectedTransition: null);

            Assert.Equal(KoploperMergeDisposition.Blocked, disposition);
        }

        [Fact]
        public void Evaluate_CancellationProducedExpectedTransitionFalse_ReturnsOpenAcceptanceItem()
        {
            KoploperMergeDisposition disposition =
                KoploperMergeBlockPolicy.Evaluate(
                    Array.Empty<KoploperValidationResult>(),
                    cancellationProducedExpectedTransition: false);

            Assert.Equal(KoploperMergeDisposition.OpenAcceptanceItem, disposition);
        }

        [Fact]
        public void Evaluate_NoContradictionsAndNoCancellation_ReturnsMergeReadyCandidate()
        {
            KoploperMergeDisposition disposition =
                KoploperMergeBlockPolicy.Evaluate(
                    Array.Empty<KoploperValidationResult>(),
                    cancellationProducedExpectedTransition: null);

            Assert.Equal(KoploperMergeDisposition.MergeReadyCandidate, disposition);
        }
    }
}
