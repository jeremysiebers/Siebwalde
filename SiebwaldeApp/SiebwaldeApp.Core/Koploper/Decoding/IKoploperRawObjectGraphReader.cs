namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Walks the Koploper raw object graph (root, block list, locomotive list) and returns the
    /// decoded registry together with the coherency anchors (resolved root, both TList headers
    /// and both item-address arrays). No state semantics and no hardware actions.
    /// </summary>
    public interface IKoploperRawObjectGraphReader
    {
        /// <summary>
        /// Resolves the root for <paramref name="moduleBase"/>, walks the block and locomotive
        /// lists and returns the raw observation (anchors + registry).
        /// </summary>
        KoploperObjectGraphDecodeResult Read(
            IKoploperMemoryReader reader,
            nuint moduleBase,
            out KoploperRawObjectGraphObservation? observation);
    }
}
