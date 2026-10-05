using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    public class Koploper94RootResolverTests
    {
        private const uint ModuleBase = 0x00400000;
        private static readonly IKoploperMemoryLayout Layout = Koploper94MemoryLayout.Instance;

        // Proven runtime values from the live PID 1576 investigation. The root-pointer RVA is a
        // global pointer-cell; the first dereference yields the cell address, the second yields the
        // active heap root object.
        private const uint RootCell = 0x007287E4;
        private const uint FinalRoot = 0x027E981C;

        [Fact]
        public void ResolvesValidRootPointer()
        {
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootCell);
            reader.SetU32((nuint)RootCell, FinalRoot);

            var resolver = new Koploper94RootResolver(reader, Layout);

            bool resolved = resolver.TryResolveRoot((nuint)ModuleBase, out nuint root);

            Assert.True(resolved);
            Assert.Equal((nuint)FinalRoot, root);
        }

        [Fact]
        public void DoubleDereference_ReturnsFinalRoot_NotRootCell()
        {
            // Regression: a single-dereference implementation would return the root cell
            // (0x7287E4) instead of the actual heap root (0x027E981C). This test fails if the
            // resolver regresses to reading only the pointer at moduleBase + RootPointerRva.
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootCell);
            reader.SetU32((nuint)RootCell, FinalRoot);

            var resolver = new Koploper94RootResolver(reader, Layout);

            bool resolved = resolver.TryResolveRoot((nuint)ModuleBase, out nuint root);

            Assert.True(resolved);
            Assert.Equal((nuint)FinalRoot, root);
            Assert.NotEqual((nuint)RootCell, root);
        }

        [Fact]
        public void FirstPointerReadFailure_ReturnsFalse()
        {
            // [moduleBase + RootPointerRva] is unmapped -> the first dereference fails.
            var reader = new FakeKoploperMemoryReader();
            var resolver = new Koploper94RootResolver(reader, Layout);

            bool resolved = resolver.TryResolveRoot((nuint)ModuleBase, out nuint root);

            Assert.False(resolved);
            Assert.Equal((nuint)0, root);
        }

        [Fact]
        public void NullRootCell_ReturnsFalse()
        {
            // [moduleBase + RootPointerRva] holds 0 -> the root cell is null.
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), 0u);

            var resolver = new Koploper94RootResolver(reader, Layout);

            bool resolved = resolver.TryResolveRoot((nuint)ModuleBase, out nuint root);

            Assert.False(resolved);
            Assert.Equal((nuint)0, root);
        }

        [Fact]
        public void SecondPointerReadFailure_ReturnsFalse()
        {
            // The root cell is mapped but the address it points to is unmapped -> the second
            // dereference fails.
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootCell);

            var resolver = new Koploper94RootResolver(reader, Layout);

            bool resolved = resolver.TryResolveRoot((nuint)ModuleBase, out nuint root);

            Assert.False(resolved);
            Assert.Equal((nuint)0, root);
        }

        [Fact]
        public void NullFinalRoot_ReturnsFalse()
        {
            // The root cell holds 0 -> the final heap root is null.
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), RootCell);
            reader.SetU32((nuint)RootCell, 0u);

            var resolver = new Koploper94RootResolver(reader, Layout);

            bool resolved = resolver.TryResolveRoot((nuint)ModuleBase, out nuint root);

            Assert.False(resolved);
            Assert.Equal((nuint)0, root);
        }
    }
}
