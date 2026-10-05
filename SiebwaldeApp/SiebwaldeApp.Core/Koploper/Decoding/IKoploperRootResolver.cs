namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Resolves the central root pointer for a running Koploper instance from the module base
    /// and the profile's root-pointer RVA.
    /// </summary>
    public interface IKoploperRootResolver
    {
        /// <summary>
        /// Reads the root pointer at <c>moduleBase + RootPointerRva</c>. Returns false when the
        /// read failed; otherwise writes the resolved root pointer to <paramref name="rootPointer"/>.
        /// </summary>
        bool TryResolveRoot(nuint moduleBase, out nuint rootPointer);
    }
}
