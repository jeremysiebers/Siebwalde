using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Root resolver for the Koploper 9.4 layout. The proven runtime layout uses a double
    /// dereference: <c>moduleBase + RootPointerRva</c> is a global pointer-cell, and that cell
    /// holds the active heap root object address.
    /// </summary>
    public sealed class Koploper94RootResolver : IKoploperRootResolver
    {
        private readonly IKoploperMemoryReader _reader;
        private readonly IKoploperMemoryLayout _layout;

        public Koploper94RootResolver(IKoploperMemoryReader reader, IKoploperMemoryLayout layout)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }

        /// <inheritdoc />
        public bool TryResolveRoot(nuint moduleBase, out nuint root)
        {
            root = 0;

            // First dereference: the profile RVA points at a global pointer-cell holding the
            // address of the active root pointer slot.
            nuint rootPointerAddress = checked(moduleBase + _layout.RootPointerRva);
            if (!_reader.TryReadPointer32(rootPointerAddress, out uint rootCell))
            {
                return false;
            }
            if (rootCell == 0)
            {
                return false;
            }

            // Second dereference: the root cell holds the actual heap root object address.
            if (!_reader.TryReadPointer32(rootCell, out uint finalRoot))
            {
                return false;
            }
            if (finalRoot == 0)
            {
                return false;
            }

            root = finalRoot;
            return true;
        }
    }
}
