using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// One independent cross-check sample captured at a point in time. The dictionaries map
    /// internal locomotive ids to their current block and reserved block set; a locomotive that
    /// owns nothing is simply absent from both dictionaries.
    /// </summary>
    public sealed record KoploperIndependentSample(
        KoploperCrossCheckSource Source,
        DateTimeOffset CapturedAtUtc,
        KoploperProcessGeneration? Generation,
        IReadOnlyDictionary<int, int> LocToCurrentBlock,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>> LocToReservedBlocks);
}
