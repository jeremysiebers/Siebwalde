using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    public class Koploper94RootResolverTests
    {
        private const uint ModuleBase = 0x00400000;
        private static readonly IKoploperMemoryLayout Layout = Koploper94MemoryLayout.Instance;

        [Fact]
        public void ResolvesValidRootPointer()
        {
            var reader = new FakeKoploperMemoryReader();
            const nuint expected = 0x00500000;
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), (uint)expected);

            var resolver = new Koploper94RootResolver(reader, Layout);

            bool resolved = resolver.TryResolveRoot((nuint)ModuleBase, out nuint root);

            Assert.True(resolved);
            Assert.Equal(expected, root);
        }

        [Fact]
        public void UnmappedRootPointerAddress_Fails()
        {
            var reader = new FakeKoploperMemoryReader();
            var resolver = new Koploper94RootResolver(reader, Layout);

            bool resolved = resolver.TryResolveRoot((nuint)ModuleBase, out nuint root);

            Assert.False(resolved);
            Assert.Equal((nuint)0, root);
        }

        [Fact]
        public void ZeroRootPointer_ResolvesToZero()
        {
            // The resolver reads the pointer verbatim and does not reject a zero value; a zero
            // root is the decoder's responsibility (it maps to RootInvalid). This documents the
            // implemented + documented resolver contract, distinct from the decoder's check.
            var reader = new FakeKoploperMemoryReader();
            reader.SetU32((nuint)(ModuleBase + Layout.RootPointerRva), 0u);

            var resolver = new Koploper94RootResolver(reader, Layout);

            bool resolved = resolver.TryResolveRoot((nuint)ModuleBase, out nuint root);

            Assert.True(resolved);
            Assert.Equal((nuint)0, root);
        }
    }
}
