using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for <see cref="KoploperCoherencyValidator"/>. Each test mutates exactly one
    /// coherency anchor and asserts the validator reports the two observations as incoherent.
    /// </summary>
    public class KoploperCoherencyValidatorTests
    {
        [Fact]
        public void IsCoherent_Identical_True()
        {
            Assert.True(KoploperCoherencyValidator.IsCoherent(Baseline(), Baseline()));
        }

        [Fact]
        public void IsCoherent_DifferentResolvedRoot_False()
        {
            var first = Baseline();
            var second = first with { ResolvedRoot = first.ResolvedRoot + 0x1000 };
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentBlockListItemsArray_False()
        {
            var first = Baseline();
            var second = first with { BlockList = first.BlockList with { ItemsArrayAddress = first.BlockList.ItemsArrayAddress + 0x10 } };
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentBlockListCount_False()
        {
            var first = Baseline();
            var second = first with { BlockList = first.BlockList with { Count = first.BlockList.Count + 1 } };
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentBlockListCapacity_False()
        {
            var first = Baseline();
            var second = first with { BlockList = first.BlockList with { Capacity = first.BlockList.Capacity + 1 } };
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentLocoListItemsArray_False()
        {
            var first = Baseline();
            var second = first with { LocoList = first.LocoList with { ItemsArrayAddress = first.LocoList.ItemsArrayAddress + 0x10 } };
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentBlockItemAddresses_False()
        {
            var first = Baseline();
            var second = first with { BlockItemAddresses = new nuint[] { 0x4100 } };
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentLocoItemAddresses_False()
        {
            var first = Baseline();
            var second = first with { LocoItemAddresses = new nuint[] { 0x5100 } };
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentBlockInternalId_False()
        {
            var first = Baseline();
            var second = WithBlock(first, first.Registry.Blocks[0] with { InternalBlockId = 99 });
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentBlockDisplayNumber_False()
        {
            var first = Baseline();
            var second = WithBlock(first, first.Registry.Blocks[0] with { DisplayBlockNumber = 999 });
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentBlockOwnerPointer_False()
        {
            var first = Baseline();
            var second = WithBlock(first, first.Registry.Blocks[0] with { OwnerPointer = 0xB000 });
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentBlockRawState_False()
        {
            var first = Baseline();
            var second = WithBlock(first, first.Registry.Blocks[0] with { RawState = 2 });
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentBlockChangedFlag_False()
        {
            var first = Baseline();
            var second = WithBlock(first, first.Registry.Blocks[0] with { ChangedFlag = 1 });
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentBlockUpdateTick_False()
        {
            var first = Baseline();
            var second = WithBlock(first, first.Registry.Blocks[0] with { UpdateTick = 0x2000 });
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentLocoInternalId_False()
        {
            var first = Baseline();
            var second = WithLoco(first, first.Registry.Locomotives[0] with { InternalLocomotiveId = 25 });
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentLocoBlockRef54_False()
        {
            var first = Baseline();
            var second = WithLoco(first, first.Registry.Locomotives[0] with { BlockRef54 = 0x55 });
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_DifferentLocoBlockRef58_False()
        {
            var first = Baseline();
            var second = WithLoco(first, first.Registry.Locomotives[0] with { BlockRef58 = 0x59 });
            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        private static KoploperRawObjectGraphObservation Baseline()
        {
            return new KoploperRawObjectGraphObservation(
                ResolvedRoot: 0x1000,
                BlockList: new KoploperTList(0x2000, 1, 1),
                BlockItemAddresses: new nuint[] { 0x4000 },
                LocoList: new KoploperTList(0x3000, 1, 1),
                LocoItemAddresses: new nuint[] { 0x5000 },
                Registry: new KoploperRawRegistry(
                    new[] { new KoploperRawBlock(1, 101, 0xA000, 1, 0, 0x1000, 0) },
                    new[] { new KoploperRawLocomotive(24, 0x54, 0x58, 0x5000) }));
        }

        private static KoploperRawObjectGraphObservation WithBlock(KoploperRawObjectGraphObservation source, KoploperRawBlock block)
        {
            return source with { Registry = source.Registry with { Blocks = new[] { block } } };
        }

        private static KoploperRawObjectGraphObservation WithLoco(KoploperRawObjectGraphObservation source, KoploperRawLocomotive loco)
        {
            return source with { Registry = source.Registry with { Locomotives = new[] { loco } } };
        }
    }
}
