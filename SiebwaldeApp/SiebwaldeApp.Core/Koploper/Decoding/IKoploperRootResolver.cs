namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Resolves the active heap root object for a running Koploper instance from the module
    /// base and the profile's root-pointer RVA (double dereference).
    /// </summary>
    public interface IKoploperRootResolver
    {
        /// <summary>
        /// Resolves the root with a double dereference: reads the global pointer-cell at
        /// <c>moduleBase + RootPointerRva</c>, then reads the pointer at that cell. Returns false
        /// when either read failed or an intermediate pointer is null; otherwise writes the final
        /// heap root object address to <paramref name="root"/>.
        /// </summary>
        bool TryResolveRoot(nuint moduleBase, out nuint root);
    }
}
