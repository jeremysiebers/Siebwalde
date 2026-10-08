using System;

namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// Configuration for cross-check comparison and contradiction classification. All windows are
    /// policy placeholders, not proven physical timing budgets.
    /// </summary>
    public sealed record KoploperValidationOptions
    {
        /// <summary>Default window in which a block transition may legitimately skew a comparison.</summary>
        public static readonly TimeSpan DefaultTransitionSkewWindow = TimeSpan.FromMilliseconds(1500);

        /// <summary>Default window for correlating two samples in time.</summary>
        public static readonly TimeSpan DefaultCorrelationWindow = TimeSpan.FromMilliseconds(750);

        /// <summary>Default number of consecutive contradicting samples before a contradiction persists.</summary>
        public const int DefaultContradictionPersistenceSamples = 3;

        /// <summary>The window in which a block transition may legitimately skew a comparison.</summary>
        public TimeSpan TransitionSkewWindow { get; init; }

        /// <summary>The window used to correlate two samples in time.</summary>
        public TimeSpan CorrelationWindow { get; init; }

        /// <summary>Number of samples a contradiction must persist across before it is treated as durable.</summary>
        public int ContradictionPersistenceSamples { get; init; }

        public KoploperValidationOptions(
            TimeSpan? transitionSkewWindow = null,
            TimeSpan? correlationWindow = null,
            int contradictionPersistenceSamples = DefaultContradictionPersistenceSamples)
        {
            TransitionSkewWindow = transitionSkewWindow ?? DefaultTransitionSkewWindow;
            CorrelationWindow = correlationWindow ?? DefaultCorrelationWindow;

            if (contradictionPersistenceSamples < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contradictionPersistenceSamples),
                    contradictionPersistenceSamples,
                    "ContradictionPersistenceSamples must be >= 1.");
            }

            ContradictionPersistenceSamples = contradictionPersistenceSamples;
        }
    }
}
