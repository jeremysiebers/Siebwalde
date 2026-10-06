using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A single raw walk of the Koploper object graph, with the coherency anchors captured
    /// alongside the decoded registry. The anchors (<see cref="ResolvedRoot"/>, both TList
    /// headers, both item-address arrays and every compared raw field) let a caller decide
    /// whether two walks observed the same in-memory graph.
    /// </summary>
    public sealed record KoploperRawObjectGraphObservation(
        nuint ResolvedRoot,
        KoploperTList BlockList,
        IReadOnlyList<nuint> BlockItemAddresses,
        KoploperTList LocoList,
        IReadOnlyList<nuint> LocoItemAddresses,
        KoploperRawRegistry Registry);
}
