namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Raw locomotive fields read from a locomotive object. Values are stored verbatim;
    /// <see cref="BlockRef58"/> is the strong current-block candidate,
    /// <see cref="BlockRef54"/> has an unverified semantic, and
    /// <see cref="ObjectAddress"/> is the locomotive object's own address (the TList item
    /// pointer, which is what <c>TBlok+0x1AC</c> owner pointers point at).
    /// </summary>
    public readonly record struct KoploperRawLocomotive(
        uint InternalLocomotiveId,
        uint BlockRef54,
        uint BlockRef58,
        uint ObjectAddress);
}
