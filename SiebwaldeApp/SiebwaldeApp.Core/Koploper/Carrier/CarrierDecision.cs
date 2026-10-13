using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>What the ownership-aware locomotive carrier commands for one amplifier.</summary>
    public enum CarrierVerdict
    {
        /// <summary>Drive this amplifier with the owner's reconciled HR0.</summary>
        Drive,

        /// <summary>Command neutral (standstill) without a specific release/revoke label.</summary>
        Neutral,

        /// <summary>Command neutral because the locomotive departed (a previously-driven target is no longer one).</summary>
        Release,

        /// <summary>Command neutral and escalate locally (contradiction / unexpected occupancy).</summary>
        Revoke,

        /// <summary>Two logical sections with different owners map to one amplifier (command neutral, escalate).</summary>
        Conflict,

        /// <summary>Do not command this amplifier for a given locomotive (someone else owns it / manual block).</summary>
        Deny,

        /// <summary>
        /// Keep driving a departed source section (non-neutral) because it is still physically
        /// occupied OR within the post-clear release tail hold. KIS removing the section from the
        /// locomotive's logical set does not by itself neutralize it; only the two-stage physical
        /// release (deassert filter + tail hold) does.
        /// </summary>
        DepartingPhysicalHold,

        /// <summary>
        /// A departed or reconstructed source section is still physically ATTRIBUTED to a locomotive
        /// (its physical footprint), but that locomotive's speed is zero, so it is commanded neutral
        /// (399) while the attribution is retained. Distinct from <see cref="Release"/>, which drops
        /// attribution and would allow the same physical occupancy to be reclassified next cycle as
        /// an unrelated <c>Free+Occupied</c> intrusion.
        /// </summary>
        DepartingPhysicalStop
    }

    /// <summary>Why the carrier made a decision for an amplifier.</summary>
    public enum CarrierReason
    {
        None,
        ShadowInvalidLayoutStop,
        ReservedPreEnergize,
        ExpectedBoundaryLead,
        OccupiedConsistent,
        SpeedZeroNeutral,
        FreeClearDeny,
        FreeOccupiedRevoke,
        ReservedNotExpectedRevoke,
        OccupiedClearRelease,
        OtherLocoOwns,
        ManualBlocked,
        MappingCollisionConflict,
        PhysicalUnknownStale,
        MissingBinding,
        PreviousTargetDeparted,

        /// <summary>A departed source is still physically occupied (held driven, not released).</summary>
        DepartedPhysicalHold,

        /// <summary>A departed source is filtered Clear but still within the post-clear release tail hold.</summary>
        DepartedTailHold,

        /// <summary>A departed source's physical telemetry is Unknown/stale: fail closed (held driven, not released).</summary>
        DepartedSourceStale,

        /// <summary>
        /// A departed or reconstructed source is still physically occupied (or stale) and remains
        /// attributed to its owner, but the owner's speed is zero: command neutral, keep the
        /// attribution. This is a stable condition (a stopped locomotive spanning two sections), not
        /// an intrusion and not a release.
        /// </summary>
        DepartingSourceStopped,

        /// <summary>
        /// The locomotive's reconstructed physical footprint is PARTIAL: its occupied anchor has an
        /// adjacent section that is physically Occupied but NOT safely attributable to it (conflicting
        /// ownership / ambiguity). Driving the subset forward would be unsafe, so the affected
        /// footprint members are commanded neutral and the locomotive is contained (escalated locally,
        /// no layout stop, no latch).
        /// </summary>
        PartialFootprintContain
    }

    /// <summary>One amplifier's carrier decision.</summary>
    public sealed record CarrierAmplifierDecision(
        int Amplifier,
        int? OwnerDcc,
        int Hr0,
        CarrierVerdict Verdict,
        CarrierReason Reason,
        string? EvidenceLabel);

    /// <summary>
    /// The complete, deterministic carrier decision for one reconciliation cycle. It is the pure
    /// output of <see cref="LocomotiveCarrierReconciliation.Reconcile"/> and carries no side effects.
    /// </summary>
    public sealed record CarrierDecision(
        IReadOnlyList<CarrierAmplifierDecision> Amplifiers,
        bool LayoutStopRequired,
        bool AnyLocoStopRequired,
        IReadOnlyList<int> LatchedLocoIds,
        IReadOnlyList<string> Diagnostics,
        IReadOnlyDictionary<int, int> PhysicalFootprint);
}
