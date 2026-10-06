using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Outcome of the typed block-state decoder: one snapshot per block (same order as the raw
    /// registry) plus one diagnostic per block that could not be published as a valid
    /// Free/Reserved/Occupied/Transition state.
    /// </summary>
    public sealed record KoploperBlockStateDecodeResult(
        IReadOnlyList<KoploperBlockSnapshot> Snapshots,
        IReadOnlyList<KoploperBlockDiagnostic> Diagnostics);

    /// <summary>
    /// A diagnostic for a single block whose raw state/owner combination did not map to a valid
    /// published state. <see cref="RawState"/> and <see cref="OwnerPointer"/> are preserved
    /// verbatim for traceability.
    /// </summary>
    public sealed record KoploperBlockDiagnostic(
        int InternalBlockId,
        KoploperDiagnosticCode Code,
        uint RawState,
        uint OwnerPointer);
}
