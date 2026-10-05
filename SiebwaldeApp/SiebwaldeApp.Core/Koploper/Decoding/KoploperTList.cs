namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A raw Delphi <c>TList</c> header: the 32-bit item-pointer array address, the item count
    /// and the array capacity. Values are stored verbatim; no domain interpretation.
    /// </summary>
    public readonly record struct KoploperTList(
        nuint ItemsArrayAddress,
        uint Count,
        uint Capacity);
}
