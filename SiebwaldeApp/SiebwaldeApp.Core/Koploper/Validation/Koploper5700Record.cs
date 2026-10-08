namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// One parsed record from the Koploper external-information stream (port 5700). The raw
    /// framing is five ASCII fields separated by <c>0x1B</c> and terminated by <c>0x00</c>;
    /// this record carries the parsed locomotive and block identity plus the raw auxiliary fields.
    /// </summary>
    public sealed record Koploper5700Record(
        int LocomotiveId,
        int BlockId,
        string? ModelTime,
        string? PcTime,
        string? Description);
}
