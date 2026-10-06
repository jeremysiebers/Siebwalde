namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A locomotive as published in a state snapshot: the internal locomotive id and the
    /// locomotive object's own memory address. Block reference fields (54/58) are deliberately
    /// not published here.
    /// </summary>
    public readonly record struct KoploperLocomotiveSnapshot(int InternalLocomotiveId, uint ObjectAddress);
}
