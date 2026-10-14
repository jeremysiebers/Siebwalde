using System.Linq;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class OvaaltjeLoop2ProfileTests
    {
        private static LayoutProfile LoadProfile()
        {
            Assert.True(LayoutProfileLoader.TryLoadFromFile(
                LayoutProfileLoader.OvaaltjeLoop2Path, out var profile, out var errors),
                string.Join("; ", errors));
            Assert.Empty(errors);
            Assert.NotNull(profile);
            return profile!;
        }

        [Fact]
        public void Profile_LoadsFromFileAndByName_WithoutErrors()
        {
            Assert.Equal("Ovaaltje Loop 2", LoadProfile().Name);
            Assert.True(LayoutProfileLoader.TryLoadByName(
                "Ovaaltje Loop 2", out var profile, out var errors), string.Join("; ", errors));
            Assert.Empty(errors);
            Assert.NotNull(profile);
            Assert.Equal("Ovaaltje Loop 2", profile!.Name);
        }

        [Fact]
        public void Profile_HasFourSectionsThreeBlocksNoSwitchesAndThreeRoutes()
        {
            var profile = LoadProfile();
            Assert.Equal(4, profile.Sections.Count);
            Assert.Equal(3, profile.Blocks.Count);
            Assert.Empty(profile.Switches);
            Assert.Equal(3, profile.Routes.Count);
            Assert.Equal(new[] { 1, 2, 3 }, profile.Blocks.Select(b => b.Id));
            Assert.Equal(new[] { 1 }, profile.Blocks[0].SectionIds);
            Assert.Equal(new[] { 2 }, profile.Blocks[1].SectionIds);
            Assert.Equal(new[] { 3, 4 }, profile.Blocks[2].SectionIds);
        }

        [Fact]
        public void Routes_PreserveCollapsedDirectedCycleInOrder()
        {
            var routes = LoadProfile().Routes;
            Assert.Equal(new[] { (1, 2), (2, 3), (3, 1) },
                routes.Select(r => (r.FromBlock, r.ToBlock)));
            Assert.All(routes, r => Assert.Null(r.SwitchId));
        }

        [Fact]
        public void KoploperBindings_MapBlockThreeToSectionsThreeAndFour()
        {
            var profile = LoadProfile();
            Assert.Equal(new[] { 1, 2, 3 },
                profile.KoploperBlockBindings.Select(b => b.KoploperInternalBlockId));
            Assert.Equal(new[] { 1 }, profile.KoploperBlockBindings[0].SectionIds);
            Assert.Equal(new[] { 2 }, profile.KoploperBlockBindings[1].SectionIds);
            Assert.Equal(new[] { 3, 4 }, profile.KoploperBlockBindings[2].SectionIds);
            var map = profile.ToKoploperBlockBindingMap();
            Assert.Equal(new[] { 1, 2, 3 }, map.Keys.OrderBy(id => id));
            Assert.Equal(new[] { 1 }, map[1]);
            Assert.Equal(new[] { 2 }, map[2]);
            Assert.Equal(new[] { 3, 4 }, map[3]);
        }

        [Fact]
        public void PhysicalDomain_PreservesSimpleLoopAmplifierBinding()
        {
            var profile = LoadProfile();
            Assert.Equal(new byte[] { 1, 3, 4, 6 }, profile.ToPhysicalDomain());
            Assert.Equal(new[] { (1, 1), (2, 3), (3, 4), (4, 6) },
                profile.PhysicalAmplifierMapping.Select(b => (b.SectionId, b.PhysicalAmplifier)));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, profile.DetectedSlaves);
            Assert.Equal(new[] { (1, 1), (2, 2), (3, 3), (4, 4) },
                profile.Sections.Select(s => (s.Id, s.AmplifierSlave)));
        }

        [Fact]
        public void DriveGroups_ResolveWholeBlockThreeToAmplifiersFourAndSix()
        {
            var profile = LoadProfile();
            var map = profile.ToKoploperBlockBindingMap();
            Assert.Equal(new[] { 4, 6 }, KoploperDriveGroupProjector.Project(3, map, profile.PhysicalAmplifierMapping));
            Assert.Equal(new[] { 1 }, KoploperDriveGroupProjector.Project(1, map, profile.PhysicalAmplifierMapping));
            Assert.Equal(new[] { 3 }, KoploperDriveGroupProjector.Project(2, map, profile.PhysicalAmplifierMapping));
        }

        [Fact]
        public void RealKoploperBlockMap_ProjectsAllThreePhysicalDriveGroups()
        {
            var map = LoadProfile().ToRealKoploperBlockMap();
            Assert.Equal(3, map.Blocks.Count);
            Assert.True(map.TryGetByBlock(1, out var block1));
            Assert.True(map.TryGetByBlock(2, out var block2));
            Assert.True(map.TryGetByBlock(3, out var block3));
            Assert.Equal(new ushort[] { 1 }, block1.AmplifierSections);
            Assert.Equal(new ushort[] { 3 }, block2.AmplifierSections);
            Assert.Equal(new ushort[] { 4, 6 }, block3.AmplifierSections);
            Assert.False(map.TryGetByBlock(4, out _));
        }

        [Fact]
        public void LocomotiveBindings_PreserveDccAddressesOneAndTwo()
        {
            var profile = LoadProfile();
            var map = profile.ToLocomotiveBindingMap();
            Assert.Equal(new[] { 1, 2 }, map.Keys.OrderBy(id => id));
            Assert.Equal(1, map[1]);
            Assert.Equal(2, map[2]);
            Assert.Equal(new[] { (1, 1), (2, 3) },
                profile.Locomotives.Select(l => (l.Address, l.InitialBlock)));
        }

        [Fact]
        public void Bezetmelders_RemainSeparatePerPhysicalSection()
        {
            var profile = LoadProfile();
            Assert.Equal(new[] { "1.01" }, profile.TryGetSection(1)!.Bezetmelders);
            Assert.Equal(new[] { "1.02" }, profile.TryGetSection(2)!.Bezetmelders);
            Assert.Equal(new[] { "1.03" }, profile.TryGetSection(3)!.Bezetmelders);
            Assert.Equal(new[] { "1.04" }, profile.TryGetSection(4)!.Bezetmelders);
        }

        [Fact]
        public void AvailableProfileNames_ContainsOvaaltjeLoopTwo()
        {
            Assert.Contains("Ovaaltje Loop 2", LayoutProfileLoader.GetAvailableProfileNames());
        }
    }
}
