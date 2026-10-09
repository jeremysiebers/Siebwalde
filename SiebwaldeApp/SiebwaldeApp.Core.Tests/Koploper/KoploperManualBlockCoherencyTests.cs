using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for <see cref="KoploperCoherencyValidator"/>: a torn read of the manual-blocked
    /// byte at +0x198 (PASS A vs PASS B disagree) must be detected as incoherent.
    /// </summary>
    public class KoploperManualBlockCoherencyTests
    {
        [Fact]
        public void IsCoherent_ManualBlockedRawDiffers_False()
        {
            var first = Baseline();
            var second = WithBlock(first, first.Registry.Blocks[0] with { ManualBlockedRaw = 1 });

            Assert.False(KoploperCoherencyValidator.IsCoherent(first, second));
        }

        [Fact]
        public void IsCoherent_ManualBlockedRawSame_True()
        {
            Assert.True(KoploperCoherencyValidator.IsCoherent(Baseline(), Baseline()));
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
    }
}
