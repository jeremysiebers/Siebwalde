using System;
using System.Collections.Generic;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.Koploper.Validation;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper.Validation
{
    /// <summary>
    /// Unit tests for <see cref="KoploperComparisonEngine"/>: the pure cross-check between an
    /// authoritative reservation observation and an independent sample. Fixtures are built
    /// directly from <see cref="KoploperReservationObservation"/> and
    /// <see cref="KoploperIndependentSample"/>; no memory reader, process or network is involved.
    /// </summary>
    public class KoploperComparisonEngineTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
        private static readonly KoploperProcessGeneration Generation = new(100, new DateTimeOffset(1000, TimeSpan.Zero));
        private static readonly KoploperValidationOptions Options = new();

        [Fact]
        public void Compare_ExactMatch_ReturnsMatch()
        {
            KoploperReservationObservation observation = AuthoritativeObservation(Loco(5, 17, 19, 20));
            KoploperIndependentSample independent = Independent(
                Generation,
                new Dictionary<int, int> { [5] = 17 },
                new Dictionary<int, IReadOnlyCollection<int>> { [5] = new[] { 19, 20 } });

            KoploperValidationResult result = KoploperComparisonEngine.Compare(observation, independent, Options);

            Assert.Equal(KoploperValidationResult.Match, result);
        }

        [Fact]
        public void Compare_CurrentPositionMismatch_ReturnsCurrentPositionMismatch()
        {
            KoploperReservationObservation observation = AuthoritativeObservation(Loco(5, 17, 19, 20));
            KoploperIndependentSample independent = Independent(
                Generation,
                new Dictionary<int, int> { [5] = 18 },
                new Dictionary<int, IReadOnlyCollection<int>> { [5] = new[] { 19, 20 } });

            KoploperValidationResult result = KoploperComparisonEngine.Compare(observation, independent, Options);

            Assert.Equal(KoploperValidationResult.CurrentPositionMismatch, result);
        }

        [Fact]
        public void Compare_NonAuthoritativeObservation_ReturnsNonAuthoritativeSource()
        {
            var observation = new KoploperReservationObservation(
                Generation,
                SourceSequence: 1,
                ObserverSequence: 1,
                CapturedAtUtc: Now,
                ObservedAtUtc: Now,
                IsFresh: true,
                IsConsistent: true,
                SourceHealth: KoploperSourceHealth.Healthy,
                IsAuthoritative: false,
                AuthorityReason: KoploperReservationAuthorityReason.OwnershipConflict,
                Locomotives: Array.Empty<KoploperLocomotiveTrajectory>(),
                Conflicts: Array.Empty<KoploperReservationConflict>(),
                Diagnostics: Array.Empty<KoploperDiagnosticCode>());

            KoploperIndependentSample independent = Independent(
                Generation,
                new Dictionary<int, int> { [5] = 17 },
                new Dictionary<int, IReadOnlyCollection<int>>());

            KoploperValidationResult result = KoploperComparisonEngine.Compare(observation, independent, Options);

            Assert.Equal(KoploperValidationResult.NonAuthoritativeSource, result);
        }

        [Fact]
        public void Compare_NullIndependentSample_ReturnsSourceUnavailable()
        {
            KoploperReservationObservation observation = AuthoritativeObservation(Loco(5, 17, 19, 20));

            KoploperValidationResult result = KoploperComparisonEngine.Compare(observation, independent: null, Options);

            Assert.Equal(KoploperValidationResult.SourceUnavailable, result);
        }

        [Fact]
        public void Compare_GenerationMismatch_ReturnsGenerationMismatch()
        {
            KoploperReservationObservation observation = AuthoritativeObservation(Loco(5, 17, 19, 20));
            var otherGeneration = new KoploperProcessGeneration(101, new DateTimeOffset(2000, TimeSpan.Zero));
            KoploperIndependentSample independent = Independent(
                otherGeneration,
                new Dictionary<int, int> { [5] = 17 },
                new Dictionary<int, IReadOnlyCollection<int>> { [5] = new[] { 19, 20 } });

            KoploperValidationResult result = KoploperComparisonEngine.Compare(observation, independent, Options);

            Assert.Equal(KoploperValidationResult.GenerationMismatch, result);
        }

        [Fact]
        public void Compare_MultipleReservedBlocksOrderInsensitive_ReturnsMatch()
        {
            KoploperReservationObservation observation = AuthoritativeObservation(Loco(5, 17, 19, 20, 21));
            KoploperIndependentSample independent = Independent(
                Generation,
                new Dictionary<int, int> { [5] = 17 },
                new Dictionary<int, IReadOnlyCollection<int>> { [5] = new[] { 21, 19, 20 } });

            KoploperValidationResult result = KoploperComparisonEngine.Compare(observation, independent, Options);

            Assert.Equal(KoploperValidationResult.Match, result);
        }

        [Fact]
        public void Compare_EmptyIndependentMaps_ReturnsSourceUnavailable()
        {
            KoploperReservationObservation observation = AuthoritativeObservation(Loco(5, 17, 19, 20));
            KoploperIndependentSample independent = Independent(Generation);

            KoploperValidationResult result = KoploperComparisonEngine.Compare(observation, independent, Options);

            Assert.Equal(KoploperValidationResult.SourceUnavailable, result);
        }

        private static KoploperLocomotiveTrajectory Loco(int id, int? occupied, params int[] reserved)
            => new(id, occupied, reserved, IsAuthoritative: true, KoploperLocomotiveTrajectoryReason.Authoritative);

        private static KoploperReservationObservation AuthoritativeObservation(params KoploperLocomotiveTrajectory[] locomotives)
        {
            return new KoploperReservationObservation(
                Generation,
                SourceSequence: 1,
                ObserverSequence: 1,
                CapturedAtUtc: Now,
                ObservedAtUtc: Now,
                IsFresh: true,
                IsConsistent: true,
                SourceHealth: KoploperSourceHealth.Healthy,
                IsAuthoritative: true,
                AuthorityReason: KoploperReservationAuthorityReason.Authoritative,
                Locomotives: locomotives,
                Conflicts: Array.Empty<KoploperReservationConflict>(),
                Diagnostics: Array.Empty<KoploperDiagnosticCode>());
        }

        private static KoploperIndependentSample Independent(
            KoploperProcessGeneration? generation,
            IReadOnlyDictionary<int, int>? current = null,
            IReadOnlyDictionary<int, IReadOnlyCollection<int>>? reserved = null)
        {
            return new KoploperIndependentSample(
                KoploperCrossCheckSource.Port5700,
                Now,
                generation,
                current ?? new Dictionary<int, int>(),
                reserved ?? new Dictionary<int, IReadOnlyCollection<int>>());
        }
    }
}
