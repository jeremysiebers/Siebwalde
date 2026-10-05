namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Raw locomotive fields read from a locomotive object. Values are stored verbatim;
    /// <see cref="BlockRef58"/> is the strong current-block candidate and
    /// <see cref="BlockRef54"/> has an unverified semantic.
    /// </summary>
    public readonly record struct KoploperRawLocomotive(
        uint InternalLocomotiveId,
        uint BlockRef54,
        uint BlockRef58);
}
