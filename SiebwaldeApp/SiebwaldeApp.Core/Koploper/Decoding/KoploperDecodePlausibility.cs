namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Upper bounds used to reject implausible object-graph sizes before allocating or walking
    /// lists. These are sanity limits, not product constants.
    /// </summary>
    public record KoploperDecodePlausibility(
        uint MaxBlockCount = 4096,
        uint MaxLocoCount = 512);
}
