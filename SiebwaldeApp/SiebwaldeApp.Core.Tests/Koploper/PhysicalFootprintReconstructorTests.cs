using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Deterministic offline tests for the pure, stateless <see cref="PhysicalFootprintReconstructor"/>:
    /// startup/recovery reconstruction of a plausible two-section physical footprint. Reconstruction
    /// answers ONLY "which sections contain which locomotive"; it never produces drive authority.
    /// </summary>
    public class PhysicalFootprintReconstructorTests
    {
        private static BlockTopology Topology(string text) => BlockTopology.Parse(text);

        private static IReadOnlyList<LayoutPhysicalAmplifierBinding> Mapping(params (int Section, int Amp)[] pairs)
            => pairs.Select(p => new LayoutPhysicalAmplifierBinding { SectionId = p.Section, PhysicalAmplifier = p.Amp }).ToArray();

        private static PhysicalSectionOccupancyObservation Physical(params (int Section, PhysicalOccupancy Occ)[] states)
            => ReconciliationTestFixtures.Physical(
                states.Select(s => ReconciliationTestFixtures.PhysicalState(s.Section, s.Occ)).ToArray());

        private static LogicalLocomotiveShadow Loco(int id, int? occupied, params int[] reserved)
            => ReconciliationTestFixtures.Loco(id, occupied, reserved);

        private static KoploperLogicalSectionShadowObservation Shadow(params LogicalLocomotiveShadow[] locos)
            => ReconciliationTestFixtures.Shadow(locomotives: locos, profileId: "Simple Loop");

        private static IReadOnlyDictionary<int, int> Reconstruct(
            KoploperLogicalSectionShadowObservation? shadow,
            PhysicalSectionOccupancyObservation? physical,
            string topology,
            (int Section, int Amp)[] mapping,
            Dictionary<int, int> identity,
            IReadOnlyDictionary<int, IReadOnlyList<int>>? blockToSections = null)
            => PhysicalFootprintReconstructor.Reconstruct(
                shadow, physical, Topology(topology), Mapping(mapping),
                blockToSections ?? ReconciliationTestFixtures.BlockMap(mapping.Select(p => p.Section).ToArray()),
                identity);

        [Fact]
        public void Reconstruct_OccupiedReservedAdjacentOccupied_AttributesBothSections()
        {
            var footprint = Reconstruct(
                Shadow(Loco(10, 1, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                new Dictionary<int, int> { [10] = 1 });

            Assert.Equal(1, footprint[1]);
            Assert.Equal(1, footprint[2]);
        }

        [Fact]
        public void Reconstruct_UserPlacedFreeAdjacent_AttributesBothSections()
        {
            // User places a stopped locomotive over two adjacent amps; KIS has not yet reserved the
            // second section (it is Free). Reconstruction still attributes both to A (neutral only).
            var footprint = Reconstruct(
                Shadow(Loco(10, 1)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                new Dictionary<int, int> { [10] = 1 });

            Assert.Equal(1, footprint[1]);
            Assert.Equal(1, footprint[2]);
        }

        [Fact]
        public void Reconstruct_NonAdjacentDualOccupancy_DoesNotAttribute()
        {
            var footprint = Reconstruct(
                Shadow(Loco(10, 1, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                "amps: 1:1,2:3", // no route 1>2
                new[] { (1, 1), (2, 3) },
                new Dictionary<int, int> { [10] = 1 });

            Assert.Equal(1, footprint[1]);
            Assert.False(footprint.ContainsKey(2));
        }

        [Fact]
        public void Reconstruct_ConflictingOwnership_DoesNotAttribute()
        {
            // B RESERVES section 2 while A occupies section 1; A may not claim section 2 as a
            // footprint extension because it relates to a different locomotive.
            var footprint = Reconstruct(
                Shadow(Loco(10, 1), Loco(20, null, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                new Dictionary<int, int> { [10] = 1, [20] = 2 });

            Assert.Equal(1, footprint[1]);
            Assert.False(footprint.ContainsKey(2));
        }

        [Fact]
        public void Reconstruct_StalePhysical_DoesNotReconstruct()
        {
            var stale = ReconciliationTestFixtures.Physical(new[]
            {
                ReconciliationTestFixtures.PhysicalState(1, PhysicalOccupancy.Occupied, isFresh: false),
                ReconciliationTestFixtures.PhysicalState(2, PhysicalOccupancy.Occupied)
            });

            var footprint = Reconstruct(
                Shadow(Loco(10, 1, 2)),
                stale,
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                new Dictionary<int, int> { [10] = 1 });

            Assert.Empty(footprint);
        }

        [Fact]
        public void Reconstruct_InvalidShadow_DoesNotReconstruct()
        {
            var invalidShadow = ReconciliationTestFixtures.Shadow(
                shadowValid: false, locomotives: new[] { Loco(10, 1, 2) });

            var footprint = Reconstruct(
                invalidShadow,
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                new Dictionary<int, int> { [10] = 1 });

            Assert.Empty(footprint);
        }

        [Fact]
        public void Reconstruct_MissingBinding_DoesNotReconstruct()
        {
            // Internal id 10 has no LocomotiveBinding -> fail closed.
            var footprint = Reconstruct(
                Shadow(Loco(10, 1, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                "amps: 1:1,2:3 ; routes: 1>2",
                new[] { (1, 1), (2, 3) },
                new Dictionary<int, int> { [99] = 1 });

            Assert.Empty(footprint);
        }

        [Fact]
        public void Reconstruct_MappingCollision_DoesNotAttributeSecondSection()
        {
            // Both sections map to the SAME amplifier: not a two-section footprint.
            var footprint = Reconstruct(
                Shadow(Loco(10, 1, 2)),
                Physical((1, PhysicalOccupancy.Occupied), (2, PhysicalOccupancy.Occupied)),
                "amps: 1:1,2:1 ; routes: 1>2",
                new[] { (1, 1), (2, 1) },
                new Dictionary<int, int> { [10] = 1 });

            Assert.Equal(1, footprint[1]);
            Assert.False(footprint.ContainsKey(2));
        }

        [Fact]
        public void Reconstruct_TwoLocosIndependentPairs_NoCrossAttribution()
        {
            // Loc A spans sections 1+2; Loc B occupies section 3. No cross-loco attribution.
            var footprint = Reconstruct(
                Shadow(Loco(10, 1, 2), Loco(20, 3)),
                Physical(
                    (1, PhysicalOccupancy.Occupied),
                    (2, PhysicalOccupancy.Occupied),
                    (3, PhysicalOccupancy.Occupied)),
                "amps: 1:1,2:3,3:4 ; routes: 1>2,2>3",
                new[] { (1, 1), (2, 3), (3, 4) },
                new Dictionary<int, int> { [10] = 1, [20] = 2 });

            Assert.Equal(1, footprint[1]);
            Assert.Equal(1, footprint[2]);
            Assert.Equal(2, footprint[3]);
        }

        [Fact]
        public void Reconstruct_TwoLocosEachSpanningPair_AllFourAttributedIndependently()
        {
            var footprint = Reconstruct(
                Shadow(Loco(10, 1, 2), Loco(20, 3, 4)),
                Physical(
                    (1, PhysicalOccupancy.Occupied),
                    (2, PhysicalOccupancy.Occupied),
                    (3, PhysicalOccupancy.Occupied),
                    (4, PhysicalOccupancy.Occupied)),
                "amps: 1:1,2:3,3:4,4:6 ; routes: 1>2,2>3,3>4,4>1",
                new[] { (1, 1), (2, 3), (3, 4), (4, 6) },
                new Dictionary<int, int> { [10] = 1, [20] = 2 });

            Assert.Equal(1, footprint[1]);
            Assert.Equal(1, footprint[2]);
            Assert.Equal(2, footprint[3]);
            Assert.Equal(2, footprint[4]);
        }

        // -----------------------------------------------------------------
        // (F) Three-section safely-attributable startup footprint (not hard-coded to two sections)
        // -----------------------------------------------------------------

        [Fact]
        public void Reconstruct_ThreeSectionChain_AttributesAllThree()
        {
            // A physically spans three adjacent sections 1 -> 2 -> 3; KIS only reports Occupied(1)
            // and sections 2 and 3 are Free. Reconstruction must attribute all three, proving the
            // model is a set, not a hard-coded two-section pair.
            var footprint = Reconstruct(
                Shadow(Loco(10, 1)),
                Physical(
                    (1, PhysicalOccupancy.Occupied),
                    (2, PhysicalOccupancy.Occupied),
                    (3, PhysicalOccupancy.Occupied)),
                "amps: 1:1,2:3,3:4 ; routes: 1>2,2>3",
                new[] { (1, 1), (2, 3), (3, 4) },
                new Dictionary<int, int> { [10] = 1 });

            Assert.Equal(1, footprint[1]);
            Assert.Equal(1, footprint[2]);
            Assert.Equal(1, footprint[3]);
        }

        // -----------------------------------------------------------------
        // (G) Ambiguous third occupied section is NOT added to A's footprint
        // -----------------------------------------------------------------

        [Fact]
        public void Reconstruct_AmbiguousThirdOccupiedSection_IsNotAttributed()
        {
            // A spans 1 -> 2 (both Free), but section 3 (adjacent to 2) is reserved by B and
            // physically occupied. Section 3 must NOT be attributed to A (it is not safely
            // attributable), while 1 and 2 remain A's footprint.
            var footprint = Reconstruct(
                Shadow(Loco(10, 1), Loco(20, null, 3)),
                Physical(
                    (1, PhysicalOccupancy.Occupied),
                    (2, PhysicalOccupancy.Occupied),
                    (3, PhysicalOccupancy.Occupied)),
                "amps: 1:1,2:3,3:4 ; routes: 1>2,2>3",
                new[] { (1, 1), (2, 3), (3, 4) },
                new Dictionary<int, int> { [10] = 1, [20] = 2 });

            Assert.Equal(1, footprint[1]);
            Assert.Equal(1, footprint[2]);
            Assert.False(footprint.ContainsKey(3));
        }
    }
}
