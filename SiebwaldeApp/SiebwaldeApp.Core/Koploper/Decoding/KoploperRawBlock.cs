namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Raw block fields read from a <c>TBlok</c> object. Values are stored verbatim with no
    /// state semantics; the raw state byte is deliberately not interpreted here.
    /// </summary>
    public readonly record struct KoploperRawBlock(
        uint InternalBlockId,
        uint DisplayBlockNumber,
        uint OwnerPointer,
        byte RawState,
        byte ChangedFlag,
        uint UpdateTick,
        byte ManualBlockedRaw);
}
