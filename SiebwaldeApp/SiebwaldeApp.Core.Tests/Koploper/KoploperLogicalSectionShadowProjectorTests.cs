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
    /// conflict, set semantics, generation pass-through, and the identity-not-equal regression
    /// (mapping by binding, not internal-id == section-id). KIS10-02: the shadow is block-level —
    /// a locomotive's occupied/reserved members are Koploper internal BLOCK ids (1 block resolves to
    /// 1..N sections), and a locomotive occupying two mapped blocks is a fail-closed conflict.
    /// </summary>
    public class KoploperLogicalSectionShadowProjectorTests
    {
        private static readonly DateTimeOffset Now = KoploperReservationTestFixtures.Now;

        private static readonly KoploperProcessGeneration Gen1 = KoploperReservationTestFixtures.Generation(processId: 100, utcTicks: 1000);
        private static readonly KoploperProcessGeneration Gen2 = KoploperReservationTestFixtures.Generation(processId: 200, utcTicks: 2000);

        private static IReadOnlyDictionary<int, IReadOnlyList<int>> Map(params (int Block, int[] Sections)[] entries)
        {
            var map = new Dictionary<int, IReadOnlyList<int>>();
            foreach ((int block, int[] sections) in entries)
            {
                map[block] = sections;
            }

            return map;
        }

        [Fact]
        public void Project_OccupiedMappedBlock_ProjectsOccupiedLogicalBlock()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = Map((30, new[] { 2 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.True(shadow.ShadowValid);
            var loco = Assert.Single(shadow.Locomotives);
            Assert.Equal(24, loco.InternalLocomotiveId);
            Assert.Equal(30, loco.OccupiedLogicalBlock);
            Assert.Empty(loco.ReservedLogicalBlocks);
        }

        [Fact]
        public void Project_OccupiedMultiSectionBlock_IsBlockLevelNotPerSection()
        {
            // One block (30) resolves to two sections (2, 3); the locomotive's occupancy is the BLOCK
            // id, never a per-section occupancy.
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = Map((30, new[] { 2, 3 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.True(shadow.ShadowValid);
            var loco = Assert.Single(shadow.Locomotives);
            Assert.Equal(30, loco.OccupiedLogicalBlock);
            Assert.Empty(loco.ReservedLogicalBlocks);
            Assert.Empty(shadow.Conflicts);
        }

        [Fact]
        public void Project_ReservedMappedBlock_ProjectsReservedLogicalBlock()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(31, 24, KoploperBlockState.Reserved) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = Map((31, new[] { 3 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            var loco = Assert.Single(shadow.Locomotives);
            Assert.Equal(new[] { 31 }, loco.ReservedLogicalBlocks.ToArray());
            Assert.Null(loco.OccupiedLogicalBlock);
        }

        [Fact]
        public void Project_ReservedMultiSectionBlock_ProjectsReservedLogicalBlock()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(31, 24, KoploperBlockState.Reserved) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = Map((31, new[] { 3, 4 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            var loco = Assert.Single(shadow.Locomotives);
            Assert.Equal(new[] { 31 }, loco.ReservedLogicalBlocks.ToArray());
            Assert.Null(loco.OccupiedLogicalBlock);
        }

        [Fact]
        public void Project_LocoOccupiesTwoBlocks_FiresMultipleOccupiedBlocksConflict()
        {
            // A locomotive occupying two DIFFERENT mapped blocks is a fail-closed shadow conflict
            // (KIS10-02: MultipleOccupiedBlocks, formerly MultipleOccupiedSections).
            var snapshot = Snapshot(
                new[]
                {
                    KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied),
                    KoploperReservationTestFixtures.Block(31, 24, KoploperBlockState.Occupied),
                },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = Map((30, new[] { 2 }), (31, new[] { 3 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.ShadowValid);
            var conflict = Assert.Single(shadow.Conflicts);
            Assert.Equal(KoploperLogicalSectionConflictKind.MultipleOccupiedBlocks, conflict.Kind);
            Assert.Equal(31, conflict.InternalBlockId);
            Assert.Equal(24, conflict.OwnerLocomotiveId);
        }

        [Fact]
        public void Project_ManualBlockedFreeBlock_ProjectsManualBlockedSection()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free, KoploperManualBlockState.Blocked) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = Map((30, new[] { 2 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.True(shadow.ShadowValid);
            Assert.Equal(new[] { 2 }, shadow.ManualBlockedSections.ToArray());
        }

        [Fact]
        public void Project_ManualBlockedMultiSectionBlock_FlattensAllSections()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free, KoploperManualBlockState.Blocked) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = Map((30, new[] { 2, 3 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.True(shadow.ShadowValid);
            Assert.Equal(new[] { 2, 3 }, shadow.ManualBlockedSections.ToArray());
        }

        [Fact]
        public void Project_ManualBlockedPlusOccupied_IsManualVsAutomaticConflict()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied, KoploperManualBlockState.Blocked) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = Map((30, new[] { 2 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.ShadowValid);
            var conflict = Assert.Single(shadow.Conflicts);
            Assert.Equal(KoploperLogicalSectionConflictKind.ManualVsAutomatic, conflict.Kind);
            Assert.Equal(30, conflict.InternalBlockId);
            Assert.Equal(24, conflict.OwnerLocomotiveId);
            Assert.Contains(KoploperDiagnosticCode.KOPLOPER_MANUAL_AUTOMATIC_CONFLICT, shadow.Diagnostics);

            // The block contributes nothing: the locomotive has no occupied/reserved projection.
            var loco = Assert.Single(shadow.Locomotives);
            Assert.Null(loco.OccupiedLogicalBlock);
            Assert.Empty(loco.ReservedLogicalBlocks);
            Assert.Empty(shadow.ManualBlockedSections);
        }

        [Fact]
        public void Project_UnmappedFreeBlock_IsIgnored()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(99, null, KoploperBlockState.Free) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = Map((30, new[] { 2 }));

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
            var map = Map((30, new[] { 2 }));

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
            var map = Map((30, new[] { 2 }));

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
            var map = Map();

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.MappingValid);
            Assert.False(shadow.ShadowValid);
        }

        [Fact]
        public void Project_EmptyBlockList_MappingValidFalse()
        {
            // IsValidBlockSectionMap rejects a block that binds an EMPTY section list.
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = Map((30, Array.Empty<int>()));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.MappingValid);
            Assert.False(shadow.ShadowValid);
        }

        [Fact]
        public void Project_SectionInTwoBlocks_MappingValidFalse()
        {
            // IsValidBlockSectionMap rejects a section bound to two different blocks.
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = Map((30, new[] { 2 }), (31, new[] { 2 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.MappingValid);
            Assert.False(shadow.ShadowValid);
        }

        [Fact]
        public void Project_ManualStateInvalid_ManualStateValidFalse()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free, KoploperManualBlockState.Invalid) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = Map((30, new[] { 2 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.False(shadow.ManualStateValid);
            Assert.False(shadow.ShadowValid);
        }

        [Fact]
        public void Project_NonAuthoritativeObservation_ProducesEmptyShadow()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied) },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = Map((30, new[] { 2 }));

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
            var map = Map((30, new[] { 2 }));

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
            var map = Map((30, new[] { 2 }), (31, new[] { 3 }), (32, new[] { 4 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.Equal(new[] { 2, 3 }, shadow.ManualBlockedSections.ToArray());
            var loco = Assert.Single(shadow.Locomotives);
            Assert.Equal(new[] { 32 }, loco.ReservedLogicalBlocks.ToArray());
        }

        [Fact]
        public void Project_GenerationRestart_PassesThroughGeneration()
        {
            var snapshot = Snapshot(new[] { KoploperReservationTestFixtures.Block(30, null, KoploperBlockState.Free) },
                Array.Empty<KoploperLocomotiveSnapshot>());
            var map = Map((30, new[] { 2 }));

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
            // The shadow records the BLOCK ids 30 and 22, resolved via the binding map.
            var snapshot = Snapshot(
                new[]
                {
                    KoploperReservationTestFixtures.Block(30, 24, KoploperBlockState.Occupied),
                    KoploperReservationTestFixtures.Block(22, 24, KoploperBlockState.Reserved),
                },
                new[] { KoploperReservationTestFixtures.Locomotive(24) });
            var map = Map((30, new[] { 2 }), (22, new[] { 5 }));

            var shadow = KoploperLogicalSectionShadowProjector.Project(AuthoritativeObservation(), snapshot, "p", map, 1, Now, null);

            Assert.True(shadow.ShadowValid);
            var loco = Assert.Single(shadow.Locomotives);
            Assert.Equal(30, loco.OccupiedLogicalBlock);
            Assert.Equal(new[] { 22 }, loco.ReservedLogicalBlocks.ToArray());
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
