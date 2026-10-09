using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The logical-section projection for one locomotive: its currently occupied logical section
    /// (if exactly one mapped occupied block) and the set of logical sections reserved for it.
    /// <see cref="ReservedLogicalSections"/> is a set, sorted ascending for determinism.
    /// </summary>
    public sealed record LogicalLocomotiveShadow(
        int InternalLocomotiveId,
        int? OccupiedLogicalSection,
        IReadOnlyCollection<int> ReservedLogicalSections);
}
