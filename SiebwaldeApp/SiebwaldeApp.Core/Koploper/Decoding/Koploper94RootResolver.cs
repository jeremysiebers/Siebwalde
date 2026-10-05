using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Root resolver for the Koploper 9.4 layout: reads the central root pointer at
    /// <c>moduleBase + RootPointerRva</c>.
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
        public bool TryResolveRoot(nuint moduleBase, out nuint rootPointer)
        {
            rootPointer = 0;

            nuint rootPointerAddress = checked(moduleBase + _layout.RootPointerRva);
            if (!_reader.TryReadPointer32(rootPointerAddress, out uint root))
            {
                return false;
            }

            rootPointer = root;
            return true;
        }
    }
}
