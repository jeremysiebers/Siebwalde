using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>Disposition of a merge decision for validated internal-state behaviour.</summary>
    public enum KoploperMergeDisposition
    {
        /// <summary>The behaviour is a candidate for merging (no blocking contradiction).</summary>
        MergeReadyCandidate = 0,

        /// <summary>Blocked by a persisted contradiction.</summary>
        Blocked = 1,

        /// <summary>Not blocked, but requires explicit human acceptance before merging.</summary>
        OpenAcceptanceItem = 2
    }

    /// <summary>
    /// Pure, stateless policy that decides whether validated behaviour may be merged.
    /// Blocking contradictions are the four mismatch classes; otherwise a failed cancellation
    /// scenario (no expected 1-&gt;0 / 1-&gt;9 transition) keeps the behaviour an open acceptance item,
    /// and everything else is a merge-ready candidate.
    /// </summary>
    public static class KoploperMergeBlockPolicy
    {
        /// <summary>
        /// Evaluates the merge disposition.
        /// </summary>
        /// <param name="persistedContradictions">
        /// Persisted contradictions, typically the output of
        /// <see cref="KoploperContradictionClassifier.Classify"/>.
        /// </param>
        /// <param name="cancellationProducedExpectedTransition">
        /// <c>null</c> when no cancellation scenario ran; <c>true</c> when a cancellation attempt
        /// produced the expected 1-&gt;0 / 1-&gt;9 transition; <c>false</c> when a cancellation attempt did
        /// not. A <c>false</c> value yields <see cref="KoploperMergeDisposition.OpenAcceptanceItem"/>.
        /// </param>
        public static KoploperMergeDisposition Evaluate(
            IReadOnlyCollection<KoploperValidationResult> persistedContradictions,
            bool? cancellationProducedExpectedTransition)
        {
            ArgumentNullException.ThrowIfNull(persistedContradictions);

            foreach (KoploperValidationResult result in persistedContradictions)
            {
                if (IsBlocking(result))
                {
                    return KoploperMergeDisposition.Blocked;
                }
            }

            if (cancellationProducedExpectedTransition == false)
            {
                return KoploperMergeDisposition.OpenAcceptanceItem;
            }

            return KoploperMergeDisposition.MergeReadyCandidate;
        }

        private static bool IsBlocking(KoploperValidationResult result) => result switch
        {
            KoploperValidationResult.CurrentPositionMismatch => true,
            KoploperValidationResult.OwnerMismatch => true,
            KoploperValidationResult.ReservationMismatch => true,
            KoploperValidationResult.GenerationMismatch => true,
            _ => false
        };
    }
}
