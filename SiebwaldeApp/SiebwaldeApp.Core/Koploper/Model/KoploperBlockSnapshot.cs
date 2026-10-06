namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A typed per-block state snapshot derived from one raw block. Raw fields are preserved
    /// alongside the typed interpretation. Invariant: <see cref="OwnerLocomotiveId"/> is non-null
    /// exactly when <see cref="State"/> is <see cref="KoploperBlockState.Reserved"/> or
    /// <see cref="KoploperBlockState.Occupied"/>.
    /// </summary>
    public sealed record KoploperBlockSnapshot(
        int InternalBlockId,
        int? DisplayBlockNumber,
        int? OwnerLocomotiveId,
        KoploperBlockState State,
        uint RawState,
        uint? RawUpdateTick);
}
