using System;
using System.Collections.Generic;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// KIS10-03 tests for <see cref="KoploperDriveGroupProjector"/>: the pure, stateless projection
    /// of a Koploper block onto its ordered physical drive group. Fail-closed cases (unknown block,
    /// empty section list, and a section without a physical amplifier) all project to an EMPTY drive
    /// group.
    /// </summary>
    public class KoploperDriveGroupProjectorTests
    {
        private static IReadOnlyDictionary<int, IReadOnlyList<int>> Map(params (int Block, int[] Sections)[] entries)
        {
            var map = new Dictionary<int, IReadOnlyList<int>>();
            foreach ((int block, int[] sections) in entries)
            {
                map[block] = sections;
            }

            return map;
        }

        private static LayoutPhysicalAmplifierBinding Binding(int section, int amplifier)
            => new() { SectionId = section, PhysicalAmplifier = amplifier };

        [Fact]
        public void N1Block_ResolvesSingleAmplifier()
        {
            var map = Map((10, new[] { 1 }));
            var bindings = new[] { Binding(1, 7) };

            var group = KoploperDriveGroupProjector.Project(10, map, bindings);

            Assert.Equal(new[] { 7 }, group);
        }

        [Fact]
        public void N2Block_ResolvesTwoAmplifiersInSectionOrder()
        {
            var map = Map((10, new[] { 1, 2 }));
            var bindings = new[] { Binding(1, 7), Binding(2, 9) };

            var group = KoploperDriveGroupProjector.Project(10, map, bindings);

            Assert.Equal(new[] { 7, 9 }, group);
        }

        [Fact]
        public void N3Block_ResolvesThreeAmplifiersInSectionOrder()
        {
            var map = Map((10, new[] { 1, 2, 3 }));
            var bindings = new[] { Binding(1, 7), Binding(2, 9), Binding(3, 11) };

            var group = KoploperDriveGroupProjector.Project(10, map, bindings);

            Assert.Equal(new[] { 7, 9, 11 }, group);
        }

        [Fact]
        public void OrderFollowsSectionList_NotSortedAmplifierOrder()
        {
            // Sections listed 3,1,2 -> amplifiers must come out 11,7,9 (section order), not sorted.
            var map = Map((10, new[] { 3, 1, 2 }));
            var bindings = new[] { Binding(1, 7), Binding(2, 9), Binding(3, 11) };

            var group = KoploperDriveGroupProjector.Project(10, map, bindings);

            Assert.Equal(new[] { 11, 7, 9 }, group);
        }

        [Fact]
        public void UnknownBlock_ResolvesEmpty()
        {
            var map = Map((10, new[] { 1 }));
            var bindings = new[] { Binding(1, 7) };

            var group = KoploperDriveGroupProjector.Project(99, map, bindings);

            Assert.Empty(group);
        }

        [Fact]
        public void EmptyBlockList_ResolvesEmpty()
        {
            var map = Map((10, Array.Empty<int>()));
            var bindings = new[] { Binding(1, 7) };

            var group = KoploperDriveGroupProjector.Project(10, map, bindings);

            Assert.Empty(group);
        }

        [Fact]
        public void SectionWithoutPhysicalAmplifier_FailsClosedEmpty()
        {
            // Block 10 spans sections 1 and 2, but section 2 has no physical amplifier binding. The
            // whole drive group is EMPTY (fail closed): driving only section 1 would power a partial
            // block.
            var map = Map((10, new[] { 1, 2 }));
            var bindings = new[] { Binding(1, 7) };

            var group = KoploperDriveGroupProjector.Project(10, map, bindings);

            Assert.Empty(group);
        }

        [Fact]
        public void NullMap_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                KoploperDriveGroupProjector.Project(10, null!, new[] { Binding(1, 7) }));
        }

        [Fact]
        public void NullBindings_Throws()
        {
            var map = Map((10, new[] { 1 }));
            Assert.Throws<ArgumentNullException>(() =>
                KoploperDriveGroupProjector.Project(10, map, null!));
        }
    }
}
