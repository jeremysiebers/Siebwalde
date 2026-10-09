using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Shared deterministic fixtures for the logical/physical reconciliation tests. These build
    /// <see cref="KoploperLogicalSectionShadowObservation"/> and
    /// <see cref="PhysicalSectionOccupancyObservation"/> directly, bypassing the memory reader and
    /// the physical source, so the reconciliation rules can be exercised without any process,
    /// memory or hardware dependency.
    /// </summary>
    internal static class ReconciliationTestFixtures
    {
        public static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

        public static KoploperProcessGeneration Generation(int processId = 100, long utcTicks = 1000)
            => new(processId, new DateTimeOffset(utcTicks, TimeSpan.Zero));

        public static LogicalLocomotiveShadow Loco(int id, int? occupied = null, params int[] reserved)
            => new(id, occupied, reserved);

        /// <summary>
        /// Builds a logical-section shadow. <see cref="KoploperLogicalSectionShadowObservation.ShadowValid"/>
        /// defaults to <c>sourceAuthoritative &amp;&amp; mappingValid &amp;&amp; manualStateValid</c>,
        /// matching the shadow projector's validity rule (minus unmapped/conflict cases).
        /// </summary>
        public static KoploperLogicalSectionShadowObservation Shadow(
            IReadOnlyList<LogicalLocomotiveShadow>? locomotives = null,
            IReadOnlyList<int>? manualBlockedSections = null,
            bool? shadowValid = null,
            bool sourceAuthoritative = true,
            bool mappingValid = true,
            bool manualStateValid = true,
            string profileId = "simple-loop",
            KoploperProcessGeneration? generation = null,
            long sourceSequence = 1,
            long shadowSequence = 1)
        {
            bool sv = shadowValid ?? (sourceAuthoritative && mappingValid && manualStateValid);

            return new KoploperLogicalSectionShadowObservation(
                Generation: generation ?? Generation(),
                SourceSequence: sourceSequence,
                ShadowSequence: shadowSequence,
                CapturedAtUtc: Now,
                ObservedAtUtc: Now,
                IsFresh: true,
                SourceHealth: KoploperSourceHealth.Healthy,
                SourceAuthoritative: sourceAuthoritative,
                ProfileId: profileId,
                MappingValid: mappingValid,
                ManualStateValid: manualStateValid,
                ShadowValid: sv,
                Locomotives: locomotives ?? Array.Empty<LogicalLocomotiveShadow>(),
                ManualBlockedSections: manualBlockedSections ?? Array.Empty<int>(),
                UnmappedBlocks: Array.Empty<KoploperUnmappedBlock>(),
                Conflicts: Array.Empty<KoploperLogicalSectionConflict>(),
                Diagnostics: Array.Empty<KoploperDiagnosticCode>());
        }

        public static PhysicalSectionState PhysicalState(
            int sectionId,
            PhysicalOccupancy occupancy,
            bool isFresh = true,
            DateTimeOffset? observedAt = null)
            => new(
                sectionId,
                occupancy,
                observedAt ?? Now,
                TimeSpan.Zero,
                isFresh,
                isFresh ? PhysicalSourceHealth.Healthy : PhysicalSourceHealth.Degraded);

        public static PhysicalSectionOccupancyObservation Physical(
            IReadOnlyList<PhysicalSectionState>? sections = null,
            bool sourceValid = true,
            PhysicalSourceHealth? sourceHealth = null,
            string profileId = "simple-loop",
            long sourceGeneration = 0,
            long sequence = 1)
            => new(
                ProfileId: profileId,
                SourceGeneration: sourceGeneration,
                Sequence: sequence,
                CapturedAtUtc: Now,
                SourceValid: sourceValid,
                SourceHealth: sourceHealth ?? (sourceValid ? PhysicalSourceHealth.Healthy : PhysicalSourceHealth.Unavailable),
                Sections: sections ?? Array.Empty<PhysicalSectionState>());
    }

    /// <summary>
    /// Scriptable <see cref="IPhysicalSectionOccupancySource"/>: serves queued results first, then a
    /// settable fallback. Supports a queued throw for exception-path tests.
    /// </summary>
    internal sealed class FakePhysicalSectionOccupancySource : IPhysicalSectionOccupancySource
    {
        private readonly Queue<Func<PhysicalSectionOccupancyObservation>> _script = new();

        public FakePhysicalSectionOccupancySource(Func<PhysicalSectionOccupancyObservation> fallback)
        {
            Fallback = fallback;
        }

        public Func<PhysicalSectionOccupancyObservation> Fallback { get; set; }

        public int CallCount { get; private set; }

        public void Enqueue(Func<PhysicalSectionOccupancyObservation> result)
            => _script.Enqueue(result);

        public void EnqueueThrow()
            => _script.Enqueue(() => throw new InvalidOperationException("boom"));

        public PhysicalSectionOccupancyObservation Read()
        {
            CallCount++;
            if (_script.Count > 0)
            {
                return _script.Dequeue()();
            }

            return Fallback();
        }
    }

    /// <summary>
    /// Settable <see cref="IKoploperLogicalSectionShadowObserver"/> fake for reconciliation tests.
    /// </summary>
    internal sealed class FakeKoploperLogicalSectionShadowObserver : IKoploperLogicalSectionShadowObserver
    {
        public FakeKoploperLogicalSectionShadowObserver(KoploperLogicalSectionShadowObservation? current = null)
        {
            CurrentShadow = current;
        }

        public KoploperLogicalSectionShadowObservation? CurrentShadow { get; set; }

        public KoploperLogicalSectionShadowObservation Refresh() => CurrentShadow!;

        public void Start(CancellationToken cancellationToken)
        {
        }

        public Task StopAsync() => Task.CompletedTask;

        public void Dispose()
        {
        }
    }

    /// <summary>Settable clock seam for deterministic time control.</summary>
    internal sealed class MutableClock
    {
        public MutableClock(DateTimeOffset initial)
        {
            UtcNow = initial;
        }

        public DateTimeOffset UtcNow { get; set; }
    }
}
