using System.Linq;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for <see cref="KoploperRawObjectGraphReader"/>, verifying that the walk returns
    /// the coherency anchors (resolved root, both TList headers, both item-address arrays) plus
    /// the raw registry.
    /// </summary>
    public class KoploperRawObjectGraphReaderTests
    {
        private static readonly IKoploperMemoryLayout Layout = Koploper94MemoryLayout.Instance;
        private static readonly KoploperDecodePlausibility Plausibility = new();

        private static readonly nuint ModuleBase = (nuint)KoploperSnapshotTestFixtures.ModuleBase;

        [Fact]
        public void Read_ValidMap_ReturnsResolvedRoot()
        {
            using var reader = BuildValidMap();
            var graphReader = new KoploperRawObjectGraphReader(Layout, Plausibility);

            KoploperObjectGraphDecodeResult result = graphReader.Read(reader, ModuleBase, out KoploperRawObjectGraphObservation? observation);

            Assert.Equal(KoploperObjectGraphDecodeResult.Success, result);
            Assert.NotNull(observation);
            Assert.Equal((nuint)KoploperSnapshotTestFixtures.RootObject, observation.ResolvedRoot);
        }

        [Fact]
        public void Read_ValidMap_ReturnsBlockListAnchor()
        {
            using var reader = BuildValidMap();
            var graphReader = new KoploperRawObjectGraphReader(Layout, Plausibility);

            graphReader.Read(reader, ModuleBase, out KoploperRawObjectGraphObservation? observation);

            Assert.NotNull(observation);
            Assert.Equal((nuint)KoploperSnapshotTestFixtures.BlockItemsArray, observation.BlockList.ItemsArrayAddress);
            Assert.Equal(2u, observation.BlockList.Count);
            Assert.Equal(2u, observation.BlockList.Capacity);
        }

        [Fact]
        public void Read_ValidMap_ReturnsLocoListAnchor()
        {
            using var reader = BuildValidMap();
            var graphReader = new KoploperRawObjectGraphReader(Layout, Plausibility);

            graphReader.Read(reader, ModuleBase, out KoploperRawObjectGraphObservation? observation);

            Assert.NotNull(observation);
            Assert.Equal((nuint)KoploperSnapshotTestFixtures.LocoItemsArray, observation.LocoList.ItemsArrayAddress);
            Assert.Equal(1u, observation.LocoList.Count);
            Assert.Equal(1u, observation.LocoList.Capacity);
        }

        [Fact]
        public void Read_ValidMap_ReturnsBlockItemAddresses()
        {
            using var reader = BuildValidMap();
            var graphReader = new KoploperRawObjectGraphReader(Layout, Plausibility);

            graphReader.Read(reader, ModuleBase, out KoploperRawObjectGraphObservation? observation);

            Assert.NotNull(observation);
            Assert.Equal(
                new nuint[]
                {
                    (nuint)KoploperSnapshotTestFixtures.BlockObjectsBase,
                    (nuint)(KoploperSnapshotTestFixtures.BlockObjectsBase + KoploperSnapshotTestFixtures.BlockStride),
                },
                observation.BlockItemAddresses.ToArray());
        }

        [Fact]
        public void Read_ValidMap_ReturnsLocoItemAddresses()
        {
            using var reader = BuildValidMap();
            var graphReader = new KoploperRawObjectGraphReader(Layout, Plausibility);

            graphReader.Read(reader, ModuleBase, out KoploperRawObjectGraphObservation? observation);

            Assert.NotNull(observation);
            Assert.Equal(
                new nuint[] { (nuint)KoploperSnapshotTestFixtures.LocoObjectsBase },
                observation.LocoItemAddresses.ToArray());
        }

        [Fact]
        public void Read_ValidMap_ReturnsRegistry()
        {
            using var reader = BuildValidMap();
            var graphReader = new KoploperRawObjectGraphReader(Layout, Plausibility);

            graphReader.Read(reader, ModuleBase, out KoploperRawObjectGraphObservation? observation);

            Assert.NotNull(observation);
            Assert.Equal(2, observation.Registry.Blocks.Count);
            Assert.Single(observation.Registry.Locomotives);
            Assert.Equal(24u, observation.Registry.Locomotives[0].InternalLocomotiveId);
        }

        private static FakeKoploperMemoryReader BuildValidMap()
        {
            var reader = new FakeKoploperMemoryReader();
            KoploperSnapshotTestFixtures.BuildValidMap(reader);
            return reader;
        }
    }
}
