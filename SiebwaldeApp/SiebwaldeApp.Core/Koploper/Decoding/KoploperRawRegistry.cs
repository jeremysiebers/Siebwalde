using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A decoded raw object graph: the block registry and the locomotive registry, each read
    /// verbatim from Koploper memory without state interpretation.
    /// </summary>
    public record KoploperRawRegistry(
        IReadOnlyList<KoploperRawBlock> Blocks,
        IReadOnlyList<KoploperRawLocomotive> Locomotives);
}
