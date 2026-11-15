namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Disposition of a single-amplifier GROUP_READY diagnostic operation (see
    /// <c>docs/PV07-SINGLE-STEP-AMPLIFIER-DIAGNOSTICS-DESIGN.md</c> §9). The operation writes
    /// HR_TRACK_POWER bit 1 (GROUP_READY) to exactly one amplifier and observes its HR_STATE.
    /// </summary>
    public enum SingleAmplifierGroupReadyDisposition
    {
        /// <summary>The operation was refused: no valid control-layer authorization (fail-closed).</summary>
        NotAuthorized,

        /// <summary>The target address is not a valid, in-domain track amplifier (1..50).</summary>
        InvalidTarget,

        /// <summary>The runtime was not paused at Stage 4, so the single GROUP_READY is refused.</summary>
        NotPaused,

        /// <summary>The GROUP_READY write was dispatched to the transport.</summary>
        Transmitted,

        /// <summary>The target's fresh HR_STATE readback shows READY (accepted).</summary>
        Accepted,

        /// <summary>The target's fresh HR_STATE readback shows STANDBY (rejected or not yet applied).</summary>
        Rejected,

        /// <summary>No fresh HR_STATE readback arrived within the window (not delivered / no echo).</summary>
        NotDelivered,

        /// <summary>An exception occurred while issuing the operation.</summary>
        Faulted
    }

    /// <summary>
    /// Result of a single-amplifier GROUP_READY diagnostic operation: the disposition, the target,
    /// the HR8 evidence and its freshness label. The write-result register (HR15) is intentionally
    /// NOT reported here as proof of the write outcome (it is clobbered by the PIC32 lease renewal,
    /// see <c>docs/PV06-AMP6-GROUPREADY-ROOT-CAUSE-ANALYSIS.md</c> §7 H6); the durable discriminator
    /// is the HR_STATE readback qualified by <see cref="Hr8Freshness"/>.
    /// </summary>
    public sealed record SingleAmplifierGroupReadyResult
    {
        /// <summary>The target amplifier slave address.</summary>
        public byte SlaveNumber { get; init; }

        /// <summary>The operation disposition.</summary>
        public SingleAmplifierGroupReadyDisposition Disposition { get; init; }

        /// <summary>Honesty label of the HR8 evidence.</summary>
        public AmplifierRegisterFreshness Hr8Freshness { get; init; }

        /// <summary>The HR8 (state) value observed after the write.</summary>
        public ushort Hr8Value { get; init; }

        /// <summary>Human-readable detail, or null.</summary>
        public string? Message { get; init; }

        /// <summary>True when the target was freshly observed in READY (accepted).</summary>
        public bool IsAccepted => Disposition == SingleAmplifierGroupReadyDisposition.Accepted;
    }
}
