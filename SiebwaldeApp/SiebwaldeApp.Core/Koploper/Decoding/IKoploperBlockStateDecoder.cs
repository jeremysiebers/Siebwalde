namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Decodes a raw Koploper object-graph registry into typed per-block state snapshots and
    /// diagnostics. Pure and stateless: no memory reading, process access, polling, freshness or
    /// hardware actions.
    /// </summary>
    public interface IKoploperBlockStateDecoder
    {
        /// <summary>
        /// Translates each raw block in <paramref name="registry"/> into a typed snapshot using
        /// the validated validation matrix, resolving owner pointers against the raw locomotive
        /// registry.
        /// </summary>
        KoploperBlockStateDecodeResult Decode(KoploperRawRegistry registry);
    }
}
