using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>The final outcome of one outbound session command.</summary>
    public enum KoploperSessionCommandOutcome
    {
        /// <summary>Transmitted and its matching DONE acknowledgement was received.</summary>
        Acknowledged,

        /// <summary>Transmitted but no matching DONE arrived within the bounded acknowledgement timeout.</summary>
        NotAcknowledged,

        /// <summary>Refused by the allowlist (108/107/arbitrary); never transmitted.</summary>
        Rejected,

        /// <summary>Denied for an order/state violation; never transmitted.</summary>
        OrderViolation
    }

    /// <summary>
    /// The lifecycle of one outbound session command, captured for a machine-readable trace so a
    /// re-run can conclusively distinguish "requested", "transmitted", "acknowledged" and
    /// "not-acknowledged".
    /// </summary>
    public sealed record KoploperSessionCommandEvent(
        long Sequence,
        byte Command,
        DateTimeOffset RequestedAtUtc,
        DateTimeOffset? TransmittedAtUtc,
        DateTimeOffset? ResponseAtUtc,
        KoploperSessionCommandOutcome Outcome,
        ReceivedMessage? Acknowledgement);
}
