using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for <see cref="LayoutProfile.ToKoploperBlockBindingMap"/>: it projects the
    /// declarative <see cref="LayoutProfile.KoploperBlockBindings"/> into a 1:N dictionary keyed by
    /// the Koploper INTERNAL block id (the machine identity), not the display block number.
    /// </summary>
    public class KoploperBlockBindingMapTests
    {
        [Fact]
        public void SimpleLoopProfile_HasFourKoploperBlockBindings()
        {
            Assert.True(
                LayoutProfileLoader.TryLoadFromFile(LayoutProfileLoader.SimpleLoopPath, out var profile, out var errors),
                string.Join("; ", errors));
            Assert.NotNull(profile);

            Assert.Equal(4, profile!.KoploperBlockBindings.Count);
        }

        [Fact]
        public void SimpleLoopProfile_BindingMap_IsIdentityMapping()
        {
            Assert.True(
                LayoutProfileLoader.TryLoadFromFile(LayoutProfileLoader.SimpleLoopPath, out var profile, out var errors),
                string.Join("; ", errors));
            Assert.NotNull(profile);

            var map = profile!.ToKoploperBlockBindingMap();

            Assert.Equal(4, map.Count);
            Assert.Equal(new[] { 1 }, map[1]);
            Assert.Equal(new[] { 2 }, map[2]);
            Assert.Equal(new[] { 3 }, map[3]);
            Assert.Equal(new[] { 4 }, map[4]);
        }

        [Fact]
        public void Map_IsKeyedByKoploperInternalBlockId()
        {
            var profile = new LayoutProfile
            {
                KoploperBlockBindings = new[] { new KoploperBlockBinding(30, new[] { 2 }) }
            };

            var map = profile.ToKoploperBlockBindingMap();

            Assert.Single(map);
            Assert.True(map.ContainsKey(30));
            Assert.Equal(new[] { 2 }, map[30]);
        }

        [Fact]
        public void Map_PreservesMultipleSectionsPerBlock()
        {
            var profile = new LayoutProfile
            {
                KoploperBlockBindings = new[] { new KoploperBlockBinding(30, new[] { 2, 5 }) }
            };

            var map = profile.ToKoploperBlockBindingMap();

            Assert.Single(map);
            Assert.True(map.ContainsKey(30));
            Assert.Equal(new[] { 2, 5 }, map[30]);
        }

        [Fact]
        public void NonEqualBinding_DoesNotCollapseToNumericEquality()
        {
            var profile = new LayoutProfile
            {
                KoploperBlockBindings = new[] { new KoploperBlockBinding(30, new[] { 2 }) }
            };

            var map = profile.ToKoploperBlockBindingMap();

            // The key (internal id 30) and value (section 2) are distinct: the map must be keyed
            // by the internal id, so it is NOT keyed by the section id 2.
            Assert.False(map.ContainsKey(2));
            Assert.Equal(new[] { 2 }, map[30]);
        }

        [Fact]
        public void EmptyBindings_YieldEmptyMap()
        {
            var profile = new LayoutProfile();

            var map = profile.ToKoploperBlockBindingMap();

            Assert.Empty(map);
        }
    }
}
