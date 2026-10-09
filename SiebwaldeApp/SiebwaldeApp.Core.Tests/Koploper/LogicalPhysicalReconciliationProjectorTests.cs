using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for <see cref="LogicalPhysicalReconciliationProjector"/>. These exercise the
    /// global gates G1–G3 and the per-section precedence P1–P11, plus role inversion, ordering,
    /// identity and immutable publication. They always run the production projector — no bypass.
    /// </summary>
    public class LogicalPhysicalReconciliationProjectorTests
    {
        private static readonly DateTimeOffset Now = ReconciliationTestFixtures.Now;
        private static readonly int[] BoundSections = { 1, 2, 3, 4 };

        private static LogicalPhysicalReconciliationObservation Reconcile(
            KoploperLogicalSectionShadowObservation? shadow,
            PhysicalSectionOccupancyObservation? physical,
            IReadOnlyCollection<int>? bound = null,
            long sequence = 1,
            DateTimeOffset? evaluatedAt = null)
            => LogicalPhysicalReconciliationProjector.Reconcile(
                shadow,
                physical,
                bound ?? BoundSections,
                sequence,
                evaluatedAt ?? Now);

        private static LogicalSectionReconciliation Section(LogicalPhysicalReconciliationObservation obs, int id)
            => Assert.Single(obs.Sections, s => s.LogicalSectionId == id);

        // ---------------------------------------------------------------------
        // Global gates G1–G3
        // ---------------------------------------------------------------------

        [Fact]
        public void Reconcile_BothNullInputs_NonAssessable()
        {
            LogicalPhysicalReconciliationObservation obs = Reconcile(null, null);

            Assert.False(obs.ReconciliationAssessable);
            Assert.False(obs.LogicalSourceValid);
            Assert.False(obs.PhysicalSourceValid);
            Assert.Empty(obs.Sections);
            Assert.Empty(obs.Locomotives);
            Assert.Contains(ReconciliationDiagnosticCode.ReconLogicalSourceInvalid, obs.Diagnostics);
            Assert.Contains(ReconciliationDiagnosticCode.ReconPhysicalSourceInvalid, obs.Diagnostics);
        }

        [Fact]
        public void Reconcile_NullShadow_NonAssessableAllSectionsDenied()
        {
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Clear) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(null, physical);

            Assert.False(obs.ReconciliationAssessable);
            Assert.False(obs.LogicalSourceValid);
            Assert.True(obs.PhysicalSourceValid);
            Assert.Contains(ReconciliationDiagnosticCode.ReconLogicalSourceInvalid, obs.Diagnostics);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.NotAssessable, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.Denied, section.SectionSafetyEligibility);
            Assert.Empty(obs.Locomotives);
        }

        [Fact]
        public void Reconcile_NullPhysical_NonAssessablePerLocoFlagsFalse()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, null);

            Assert.False(obs.ReconciliationAssessable);
            Assert.True(obs.LogicalSourceValid);
            Assert.False(obs.PhysicalSourceValid);
            Assert.Contains(ReconciliationDiagnosticCode.ReconPhysicalSourceInvalid, obs.Diagnostics);

            var loco = Assert.Single(obs.Locomotives);
            Assert.Equal(10, loco.LocomotiveId);
            Assert.False(loco.AllOwnedSectionsAssessable);
            Assert.False(loco.AllOwnedSectionsConsistent);
        }

        [Fact]
        public void Reconcile_ShadowInvalid_NonAssessable()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                shadowValid: false,
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.False(obs.ReconciliationAssessable);
            Assert.False(obs.LogicalSourceValid);
            Assert.Contains(ReconciliationDiagnosticCode.ReconLogicalSourceInvalid, obs.Diagnostics);
        }

        [Fact]
        public void Reconcile_PhysicalSourceInvalid_NonAssessable()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sourceValid: false,
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.False(obs.ReconciliationAssessable);
            Assert.True(obs.LogicalSourceValid);
            Assert.False(obs.PhysicalSourceValid);
            Assert.Contains(ReconciliationDiagnosticCode.ReconPhysicalSourceInvalid, obs.Diagnostics);
        }

        [Fact]
        public void Reconcile_PhysicalSourceUnavailable_NonAssessable()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sourceHealth: PhysicalSourceHealth.Unavailable,
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.False(obs.ReconciliationAssessable);
            Assert.False(obs.PhysicalSourceValid);
            Assert.Contains(ReconciliationDiagnosticCode.ReconPhysicalSourceInvalid, obs.Diagnostics);
        }

        [Fact]
        public void Reconcile_ProfileMismatch_NonAssessable()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                profileId: "profile-a",
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                profileId: "profile-b",
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.False(obs.ReconciliationAssessable);
            Assert.True(obs.LogicalSourceValid);
            Assert.True(obs.PhysicalSourceValid);
            Assert.Contains(ReconciliationDiagnosticCode.ReconProfileMismatch, obs.Diagnostics);
        }

        [Fact]
        public void Reconcile_SourceAuthoritativeFalse_Denied()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                sourceAuthoritative: false,
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.False(obs.ReconciliationAssessable);
            Assert.False(obs.LogicalSourceValid);
            Assert.Contains(ReconciliationDiagnosticCode.ReconLogicalSourceInvalid, obs.Diagnostics);
        }

        [Fact]
        public void Reconcile_MappingValidFalse_Denied()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                mappingValid: false,
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.False(obs.ReconciliationAssessable);
            Assert.False(obs.LogicalSourceValid);
            Assert.Contains(ReconciliationDiagnosticCode.ReconLogicalSourceInvalid, obs.Diagnostics);
        }

        [Fact]
        public void Reconcile_ManualStateValidFalse_Denied()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                manualStateValid: false,
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.False(obs.ReconciliationAssessable);
            Assert.False(obs.LogicalSourceValid);
            Assert.Contains(ReconciliationDiagnosticCode.ReconLogicalSourceInvalid, obs.Diagnostics);
        }

        // ---------------------------------------------------------------------
        // Per-section precedence P1–P11
        // ---------------------------------------------------------------------

        [Fact]
        public void Reconcile_SectionNotBound_NotAssessable()
        {
            // Section 99 is owned logically but has no physical binding.
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 99) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(99, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 99);
            Assert.Equal(LogicalSectionReconciliationState.NotAssessable, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.NotAssessable, section.SectionSafetyEligibility);
            Assert.Contains(ReconciliationDiagnosticCode.ReconSectionBindingInvalid, section.Diagnostics);
        }

        [Fact]
        public void Reconcile_ManualBlockedClear_DeniedNoOwner()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                manualBlockedSections: new[] { 1 });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Clear) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.ManualBlocked, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.Denied, section.SectionSafetyEligibility);
            Assert.True(section.ManualBlocked);
            Assert.Equal(AutomaticSectionRole.None, section.AutomaticRole);
            Assert.Null(section.LogicalOwnerLocomotiveId);
            Assert.Contains(ReconciliationDiagnosticCode.ReconManualBlocked, section.Diagnostics);
        }

        [Fact]
        public void Reconcile_ManualBlockedOccupied_DeniedNoOwner()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                manualBlockedSections: new[] { 1 });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.ManualBlocked, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.Denied, section.SectionSafetyEligibility);
            Assert.Null(section.LogicalOwnerLocomotiveId);
            Assert.Contains(ReconciliationDiagnosticCode.ReconManualBlocked, section.Diagnostics);
        }

        [Fact]
        public void Reconcile_PhysicalMissing_NotAssessable()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(sections: null);

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.NotAssessable, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.NotAssessable, section.SectionSafetyEligibility);
            Assert.Equal(PhysicalOccupancy.Unknown, section.PhysicalOccupancy);
            Assert.Contains(ReconciliationDiagnosticCode.ReconPhysicalSectionMissing, section.Diagnostics);
        }

        [Fact]
        public void Reconcile_PhysicalUnknown_NotAssessable()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Unknown) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.NotAssessable, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.NotAssessable, section.SectionSafetyEligibility);
            Assert.Contains(ReconciliationDiagnosticCode.ReconPhysicalUnknown, section.Diagnostics);
        }

        [Fact]
        public void Reconcile_PhysicalStale_NotAssessable()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Clear, isFresh: false) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.NotAssessable, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.NotAssessable, section.SectionSafetyEligibility);
            Assert.False(section.PhysicalFresh);
            Assert.Contains(ReconciliationDiagnosticCode.ReconPhysicalStale, section.Diagnostics);
        }

        [Fact]
        public void Reconcile_FreeClear_ConsistFreeClearDenied()
        {
            var shadow = ReconciliationTestFixtures.Shadow();
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Clear) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.ConsistFreeClear, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.Denied, section.SectionSafetyEligibility);
            Assert.False(section.SectionSafetyEligibleForOwner);
            Assert.Empty(section.Diagnostics);
        }

        [Fact]
        public void Reconcile_ReservedClear_Eligible()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, null, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Clear) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.ConsistReservedClear, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.Eligible, section.SectionSafetyEligibility);
            Assert.True(section.SectionSafetyEligibleForOwner);
            Assert.Equal(AutomaticSectionRole.Reserved, section.AutomaticRole);
            Assert.Equal(10, section.LogicalOwnerLocomotiveId);
            Assert.Empty(section.Diagnostics);
        }

        [Fact]
        public void Reconcile_OccupiedOccupied_Eligible()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.ConsistOccupied, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.Eligible, section.SectionSafetyEligibility);
            Assert.True(section.SectionSafetyEligibleForOwner);
            Assert.Equal(AutomaticSectionRole.Occupied, section.AutomaticRole);
            Assert.Equal(10, section.LogicalOwnerLocomotiveId);
        }

        [Fact]
        public void Reconcile_FreeOccupied_ContradictionDenied()
        {
            var shadow = ReconciliationTestFixtures.Shadow();
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.ContradictionPhysicalWithoutLogicalOwner, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.Denied, section.SectionSafetyEligibility);
            Assert.Equal(AutomaticSectionRole.None, section.AutomaticRole);
            Assert.Null(section.LogicalOwnerLocomotiveId);
            Assert.Contains(ReconciliationDiagnosticCode.ReconFreePhysicallyOccupied, section.Diagnostics);
        }

        [Fact]
        public void Reconcile_OccupiedClear_ContradictionDenied()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Clear) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.ContradictionLogicalOccupiedPhysicalClear, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.Denied, section.SectionSafetyEligibility);
            Assert.False(section.SectionSafetyEligibleForOwner);
            Assert.Contains(ReconciliationDiagnosticCode.ReconOccupiedPhysicallyClear, section.Diagnostics);
        }

        [Fact]
        public void Reconcile_ReservedOccupied_ContradictionDenied()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, null, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 1);
            Assert.Equal(LogicalSectionReconciliationState.ContradictionReservedPhysicallyOccupied, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.Denied, section.SectionSafetyEligibility);
            Assert.Contains(ReconciliationDiagnosticCode.ReconReservedPhysicallyOccupied, section.Diagnostics);
        }

        // ---------------------------------------------------------------------
        // Role inversion
        // ---------------------------------------------------------------------

        [Fact]
        public void Reconcile_OccupiedWinsOverReservedConflict()
        {
            // Section 2 is occupied by loco 10 and reserved by loco 20: Occupied must win.
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[]
                {
                    ReconciliationTestFixtures.Loco(10, 2),
                    ReconciliationTestFixtures.Loco(20, null, 2)
                });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(2, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 2);
            Assert.Equal(AutomaticSectionRole.Occupied, section.AutomaticRole);
            Assert.Equal(10, section.LogicalOwnerLocomotiveId);
            Assert.Equal(LogicalSectionReconciliationState.ConsistOccupied, section.ReconciliationState);
        }

        [Fact]
        public void Reconcile_ReservedRoleOwner_Set()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(20, null, 3) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(3, PhysicalOccupancy.Clear) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            var section = Section(obs, 3);
            Assert.Equal(AutomaticSectionRole.Reserved, section.AutomaticRole);
            Assert.Equal(20, section.LogicalOwnerLocomotiveId);
        }

        // ---------------------------------------------------------------------
        // Ordering
        // ---------------------------------------------------------------------

        [Fact]
        public void Reconcile_SectionsSortedByLogicalSectionId()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, null, 3, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[]
                {
                    ReconciliationTestFixtures.PhysicalState(2, PhysicalOccupancy.Clear),
                    ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Clear),
                    ReconciliationTestFixtures.PhysicalState(3, PhysicalOccupancy.Clear)
                });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.Equal(new[] { 1, 2, 3 }, obs.Sections.Select(s => s.LogicalSectionId));
        }

        [Fact]
        public void Reconcile_LocomotivesSortedByLocomotiveId()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[]
                {
                    ReconciliationTestFixtures.Loco(20, null, 2),
                    ReconciliationTestFixtures.Loco(10, 1)
                });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[]
                {
                    ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied),
                    ReconciliationTestFixtures.PhysicalState(2, PhysicalOccupancy.Clear)
                });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.Equal(new[] { 10, 20 }, obs.Locomotives.Select(l => l.LocomotiveId));
        }

        // ---------------------------------------------------------------------
        // Diagnostics
        // ---------------------------------------------------------------------

        [Fact]
        public void Reconcile_GlobalDiagnostics_DistinctAndSorted()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                sourceAuthoritative: false,
                profileId: "profile-a",
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sourceValid: false,
                profileId: "profile-b",
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.Equal(
                new[]
                {
                    ReconciliationDiagnosticCode.ReconLogicalSourceInvalid,
                    ReconciliationDiagnosticCode.ReconPhysicalSourceInvalid,
                    ReconciliationDiagnosticCode.ReconProfileMismatch
                },
                obs.Diagnostics);
        }

        [Fact]
        public void Reconcile_SingleGateFailure_HasNoDuplicateDiagnostics()
        {
            var shadow = ReconciliationTestFixtures.Shadow(shadowValid: false);
            var physical = ReconciliationTestFixtures.Physical();

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.Equal(
                new[] { ReconciliationDiagnosticCode.ReconLogicalSourceInvalid },
                obs.Diagnostics);
        }

        // ---------------------------------------------------------------------
        // Multiple locomotives / multiple reserved sections
        // ---------------------------------------------------------------------

        [Fact]
        public void Reconcile_MultipleLocomotives_MultipleReservedSections()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[]
                {
                    ReconciliationTestFixtures.Loco(10, 1, 2, 3),
                    ReconciliationTestFixtures.Loco(20, null, 4)
                });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[]
                {
                    ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied),
                    ReconciliationTestFixtures.PhysicalState(2, PhysicalOccupancy.Clear),
                    ReconciliationTestFixtures.PhysicalState(3, PhysicalOccupancy.Clear),
                    ReconciliationTestFixtures.PhysicalState(4, PhysicalOccupancy.Clear)
                });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            Assert.Equal(2, obs.Locomotives.Count);

            var loco10 = Assert.Single(obs.Locomotives, l => l.LocomotiveId == 10);
            Assert.Equal(1, loco10.OccupiedSection);
            Assert.Equal(new[] { 2, 3 }, loco10.ReservedSections);
            Assert.True(loco10.AllOwnedSectionsAssessable);
            Assert.True(loco10.AllOwnedSectionsConsistent);

            var loco20 = Assert.Single(obs.Locomotives, l => l.LocomotiveId == 20);
            Assert.Null(loco20.OccupiedSection);
            Assert.Equal(new[] { 4 }, loco20.ReservedSections);
            Assert.True(loco20.AllOwnedSectionsAssessable);
            Assert.True(loco20.AllOwnedSectionsConsistent);
        }

        // ---------------------------------------------------------------------
        // Identity discipline
        // ---------------------------------------------------------------------

        [Fact]
        public void Reconcile_PhysicalOccupancyNeverAssignsLocomotiveIdentity()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[]
                {
                    ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Clear),
                    ReconciliationTestFixtures.PhysicalState(2, PhysicalOccupancy.Occupied)
                });

            LogicalPhysicalReconciliationObservation obs = Reconcile(shadow, physical);

            // Section 2 is physically occupied but has no logical owner: the reconciliation must not
            // invent a locomotive identity from the physical reading.
            var section2 = Section(obs, 2);
            Assert.Equal(AutomaticSectionRole.None, section2.AutomaticRole);
            Assert.Null(section2.LogicalOwnerLocomotiveId);

            // The physical model itself carries no locomotive/owner identity property at all.
            Assert.DoesNotContain(
                typeof(PhysicalSectionState).GetProperties(),
                p => p.Name.Contains("Loco", StringComparison.OrdinalIgnoreCase)
                     || p.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase)
                     || p.Name.Contains("Identity", StringComparison.OrdinalIgnoreCase));
        }

        // ---------------------------------------------------------------------
        // Immutable publication
        // ---------------------------------------------------------------------

        [Fact]
        public void Reconcile_ResultIsImmutableSnapshot()
        {
            var shadow = ReconciliationTestFixtures.Shadow(
                locomotives: new[] { ReconciliationTestFixtures.Loco(10, null, 1) });
            var physical = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Clear) });

            LogicalPhysicalReconciliationObservation first = Reconcile(shadow, physical, sequence: 1);
            IReadOnlyList<LogicalSectionReconciliation> firstSections = first.Sections;
            var firstState = Section(first, 1).ReconciliationState;

            // A second reconciliation with different input must not mutate the first snapshot.
            var physicalOccupied = ReconciliationTestFixtures.Physical(
                sections: new[] { ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied) });
            LogicalPhysicalReconciliationObservation second = Reconcile(shadow, physicalOccupied, sequence: 2);

            Assert.NotSame(first, second);
            Assert.NotSame(firstSections, second.Sections);
            Assert.Equal(LogicalSectionReconciliationState.ConsistReservedClear, firstState);
            Assert.Equal(LogicalSectionReconciliationState.ContradictionReservedPhysicallyOccupied, Section(second, 1).ReconciliationState);
        }
    }
}
