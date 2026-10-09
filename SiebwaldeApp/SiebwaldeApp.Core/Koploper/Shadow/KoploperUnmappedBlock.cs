namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A Koploper block that carries ownership/manual state but has no logical-section binding, so
    /// its state cannot be projected onto a Siebwalde logical section. Carried for diagnostics only.
    /// </summary>
    public sealed record KoploperUnmappedBlock(
        int InternalBlockId,
        KoploperBlockState AutomaticState,
        int? OwnerLocomotiveId,
        KoploperManualBlockState ManualBlockState);
}
