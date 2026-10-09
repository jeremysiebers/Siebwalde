using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for <see cref="KoploperLogicalSectionShadowProjector"/>: the manual/automatic
    /// projection, the four validity flags, unmapped-block handling, the manual-vs-automatic
    /// conflict, set semantics, generation pass-through, the non-authoritative path, and the
    /// identity-not-equal regression (mapping by binding, not internal-id == section-id).
    /// </summary>
    public class KoploperLogicalSectionShadowProjectorTests
    {
        private static readonly DateTimeOffset Now = KoploperReservationTestFixtures.Now;

        private static readonly KoploperProcessGeneration Gen1 = KoploperReservationTestFixtures.Generation(processId: 100, utcTicks: 1000);
        private static readonly KoploperProcessGeneration Gen2 = KoploperReservationTestFixtures.Generation(processId: 200, utcTicks: 2000);

        [Fact]
        public void Project_OccupiedMappedBlock_ProjectsOccupiedLogicalSection()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = new Dictionary<int, int> { { 30, 2 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.True(shadow.ShadowValid);
            var loco = Assert.Single(shadow.Locomotives);
            Assert.Equal(24, loco.InternalLocomotiveId);
            Assert.Equal(2, loco.OccupiedLogicalSection);
            Assert.Empty(loco.ReservedLogicalSections);
        }

        [Fact]
        public void Project_ReservedMappedBlock_ProjectsReservedLogicalSection()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(31, 24, KoploperBlockState.Reserved) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = new Dictionary<int, int> { { 31, 3 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            var loco = Assert.Single(shadow.Locomotives);
            Assert.Equal(new[] { 3 }, loco.ReservedLogicalSections.ToArray());
            Assert.Null(loco.OccupiedLogicalSection);
        }

        [Fact]
        public void Project_ManualBlockedFreeBlock_ProjectsManualBlockedSection()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free, KoploperManualBlockState.Blocked) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = new Dictionary<int, int> { { 30, 2 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.True(shadow.ShadowValid);
            Assert.Equal(new[] { 2 }, shadow.ManualBlockedSections.ToArray());
        }

        [Fact]
        public void Project_ManualBlockedPlusOccupied_IsManualVsAutomaticConflict()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied, KoploperManualBlockState.Blocked) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = new Dictionary<int, int> { { 30, 2 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.ShadowValid);
            var conflict = Assert.Single(shadow.Conflicts);
            Assert.Equal(KoploperLogicalSectionConflictKind.ManualVsAutomatic, conflict.Kind);
            Assert.Equal(30, conflict.InternalBlockId);
            Assert.Equal(2, conflict.LogicalSectionId);
            Assert.Equal(24, conflict.OwnerLocomotiveId);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_MANUAL_AUTOMATIC_CONFLICT, shadow.Diagnostics);

            // The block contributes nothing: the locomotive has no occupied/reserved projection.
            var loco = Assert.Single(shadow.Locomotives);
            Assert.Null(loco.OccupiedLogicalSection);
            Assert.Empty(loco.ReservedLogicalSections);
            Assert.Empty(shadow.ManualBlockedSections);
        }

        [Fact]
        public void Project_UnmappedFreeBlock_IsIgnored()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(99, null, KoploperBlockState.Free) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = new Dictionary<int, int> { { 30, 2 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.True(shadow.ShadowValid);
            Assert.Empty(shadow.UnmappedBlocks);
            Assert.Empty(shadow.Diagnostics);
        }

        [Fact]
        public void Project_UnmappedOwnedBlock_IsDiagnosticAndInvalid()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(99, 24, KoploperBlockState.Occupied) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = new Dictionary<int, int> { { 30, 2 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.ShadowValid);
            var unmapped = Assert.Single(shadow.UnmappedBlocks);
            Assert.Equal(99, unmapped.InternalBlockId);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_UNMAPPED_OWNED_BLOCK, shadow.Diagnostics);
        }

        [Fact]
        public void Project_UnmappedManualBlock_IsDiagnosticAndInvalid()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(99, null, KoploperBlockState.Free, KoploperManualBlockState.Blocked) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = new Dictionary<int, int> { { 30, 2 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.ShadowValid);
            var unmapped = Assert.Single(shadow.UnmappedBlocks);
            Assert.Equal(99, unmapped.InternalBlockId);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_UNMAPPED_MANUAL_BLOCK, shadow.Diagnostics);
        }

        [Fact]
        public void Project_EmptyMap_MappingValidFalse()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = new Dictionary<int, int>();

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.MappingValid);
            Assert.False(shadow.ShadowValid);
        }

        [Fact]
        public void Project_NonOneToOneMap_MappingValidFalse()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = new Dictionary<int, int> { { 30, 2 }, { 31, 2 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.MappingValid);
            Assert.False(shadow.ShadowValid);
        }

        [Fact]
        public void Project_ManualStateInvalid_ManualStateValidFalse()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free, KoploperManualBlockState.Invalid) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = new Dictionary<int, int> { { 30, 2 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.ManualStateValid);
            Assert.False(shadow.ShadowValid);
        }

        [Fact]
        public void Project_NonAuthoritativeObservation_ProducesEmptyShadow()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = new Dictionary<int, int> { { 30, 2 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(NonAuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.SourceAuthoritative);
            Assert.False(shadow.ShadowValid);
            Assert.Empty(shadow.Locomotives);
            Assert.Empty(shadow.ManualBlockedSections);
            Assert.Empty(shadow.UnmappedBlocks);
            Assert.Empty(shadow.Conflicts);
        }

        [Fact]
        public void Project_ValidInputs_ShadowValidTrue()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = new Dictionary<int, int> { { 30, 2 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.True(shadow.SourceAuthoritative);
            Assert.True(shadow.MappingValid);
            Assert.True(shadow.ManualStateValid);
            Assert.True(shadow.ShadowValid);
            Assert.Empty(shadow.Conflicts);
        }

        [Fact]
        public void Project_ReservedAndManualSections_AreSortedSets()
        {
            // Blocks are deliberately listed in reverse section order; the reserved set and the
            // manual-blocked set must still come out sorted ascending.
            var snapshot = Snapshot(
                new[]
                {
                    KoploperReservationTestFixtures.Block(32, 24, KoploperBlockState.Reserved),                       // section 4
                    KoploperReservationTestFixtures.Block(31, null, KoploperBlockState.Free, KoploperManualBlockState.Blocked), // section 3
                    KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free, KoploperManualBlockState.Blocked), // section 2
                },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = new Dictionary<int, int> { { 30, 2 }, { 31, 3 }, { 32, 4 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.Equal(new[] { 2, 3 }, shadow.ManualBlockedSections.ToArray());
            var loco = Assert.Single(shadow.Locomotives);
            Assert.Equal(new[] { 4 }, loco.ReservedLogicalSections.ToArray());
        }

        [Fact]
        public void Project_GenerationRestart_PassesThroughGeneration()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = new Dictionary<int, int> { { 30, 2 } };

            var first = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(Gen1), snapshot, "p", map, 1, Now, null);
            var second = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(Gen2), snapshot, "p", map, 2, Now, null);

            Assert.Equal(Gen1, first.Generation);
            Assert.Equal(Gen2, second.Generation);
            Assert.Equal(1, first.ShadowSequence);
            Assert.Equal(2, second.ShadowSequence);
        }

        [Fact]
        public void Project_IdentityNotEqual_MapsViaBinding()
        {
            // Internal block 30 must map to section 2 and internal block 22 to section 5 — NOT via
            // internal-id == section-id equality (which would wrongly map 30 -> 30 and 22 -> 22).
            var snapshot = Snapshot(
                new[]
                {
                    KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied),
                    KoploperReservationTestFixtures.Block(22, 24, KoploperBlockState.Reserved),
                },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = new Dictionary<int, int> { { 30, 2 }, { 22, 5 } };

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.True(shadow.ShadowValid);
            var loco = Assert.Single(shadow.Locomotives);
            Assert.Equal(2, loco.OccupiedLogicalSection);
            Assert.Equal(new[] { 5 }, loco.ReservedLogicalSections.ToArray());
        }

        private static KoploperStateSnapshot Snapshot(
            IReadOnlyList<KoploperBlockSnapshot> blocks,
            IReadOnlyList<KoploperLocomotiveSnapshot> locomotives)
        {
            return KoploperReservationTestFixtures.Snapshot(
                blocks: blocks,
                locomotives: locomotives,
                capturedAtUtc: Now,
                generation: Gen1);
        }

        private static KoploperReservationObservation AuthoritativeObservation(KoploperProcessGeneration? generation = null)
        {
            return new KoploperReservationObservation(
                Generation: generation ?? Gen1,
                SourceSequence: 1,
                ObserverSequence: 1,
                CapturedAtUtc: Now,
                ObservedAtUtc: Now,
                IsFresh: true,
                IsConsistent: true,
                SourceHealth: KoploperSourceHealth.Healthy,
                IsAuthoritative: true,
                AuthorityReason: KoploperReservationAuthorityReason.Authoritative,
                Locomotives: Array.Empty<KoploperLocomotiveTrajectory>(),
                Conflicts: Array.Empty<KoploperReservationConflict>(),
                Diagnostics: Array.Empty<KoploperDiagnosticCode>());
        }

        private static KoploperReservationObservation NonAuthoritativeObservation()
        {
            return new KoploperReservationObservation(
                Generation: Gen1,
                SourceSequence: 1,
                ObserverSequence: 1,
                CapturedAtUtc: Now,
                ObservedAtUtc: Now,
                IsFresh: false,
                IsConsistent: false,
                SourceHealth: KoploperSourceHealth.ProcessNotFound,
                IsAuthoritative: false,
                AuthorityReason: KoploperReservationAuthorityReason.ProcessNotFound,
                Locomotives: Array.Empty<KoploperLocomotiveTrajectory>(),
                Conflicts: Array.Empty<KoploperReservationConflict>(),
                Diagnostics: Array.Empty<KoploperDiagnosticCode>());
        }
    }
}
