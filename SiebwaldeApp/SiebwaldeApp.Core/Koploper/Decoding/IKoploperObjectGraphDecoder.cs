namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Decodes the Koploper raw object graph (root, block list, locomotive list) into a raw
    /// registry. No state semantics and no hardware actions.
    /// </summary>
    public interface IKoploperObjectGraphDecoder
    {
        /// <summary>
        /// Resolves the root for <paramref name="moduleBase"/>, walks the block and locomotive
        /// lists and returns the raw registry.
        /// </summary>
        KoploperObjectGraphDecodeResult Decode(
            IKoploperMemoryReader reader,
            nuint moduleBase,
            out KoploperRawRegistry? registry);
    }
}
