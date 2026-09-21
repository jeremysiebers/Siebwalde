using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// One dedicated production control-trace log for the normal Koploper/ECoS control path.
    ///
    /// The trace exists so a human investigator (and, later, a programmatic parser) can
    /// reconstruct the causal path:
    ///
    ///   Koploper/ECoS input -> C# logical processing -> block/movement decision ->
    ///   physical amplifier command -> safety decision -> safety neutralization/escalation/result.
    ///
    /// It is deliberately a focused event abstraction over the existing <see cref="ILogFactory"/>:
    /// one authoritative logger instance, stable event names and stable key/value fields. It is
    /// not a telemetry dump and not a second logging framework.
    ///
    /// Terminology (never collapsed):
    /// - Requested: the logical request as it arrived (for example a protocol speed step).
    /// - Commanded: a concrete setpoint was accepted at the defined physical command boundary
    ///   (the pending-write queue) or transmitted by the runtime writer. The protocol provides
    ///   no acknowledgement, so this is never a claim of hardware reception.
    /// - Observed: a fresh returned amplifier register reports the value. The trace never
    ///   fabricates observed state; when it is unavailable it is simply absent.
    ///
    /// See <see cref="ControlTraceFormat"/> for the payload convention.
    /// </summary>
    public interface IControlTrace
    {
        /// <summary>Application/control-session start marker.</summary>
        void SessionStart(string application, string? mode, string? version);

        /// <summary>
        /// A meaningful incoming ECoS/Koploper control command (state-changing commands only;
        /// polling chatter such as get/request/queryObjects is not traced).
        /// </summary>
        void EcosCommand(string command, int? objectId, IReadOnlyList<string> options);

        /// <summary>
        /// The protocol/parser boundary: the raw request and the normalized C# speed.
        /// </summary>
        void SpeedDecision(
            string protocol,
            int? ecosId,
            int address,
            string rawKind,
            int rawValue,
            int normalizedSpeed);

        /// <summary>The logical locomotive block transition reported by Koploper.</summary>
        void BlockTransition(int locoAddress, int? previousBlock, int newBlock, string source);

        /// <summary>
        /// A resolved physical amplifier target placed at the command-acceptance boundary.
        /// <paramref name="purpose"/> distinguishes movement, look-ahead, normal power-off and
        /// safety neutralization; <paramref name="source"/> distinguishes Loco, Layout, Safety.
        /// </summary>
        void AmplifierCommand(
            string source,
            int? locoAddress,
            ushort amplifier,
            int? block,
            string purpose,
            int pwm,
            int hr0,
            TrackAmplifierOperationalGroup group,
            int? normalizedSpeed,
            int? direction);

        /// <summary>
        /// The concrete transmission boundary: the runtime writer transmitted HoldingReg0.
        /// This corresponds to the existing <c>[WRITE]</c> component-log line.
        /// </summary>
        void AmplifierWrite(ushort amplifier, int hr0);

        /// <summary>A retained non-neutral physical target was added for a locomotive.</summary>
        void TrackerAdd(int locoAddress, ushort amplifier, IReadOnlyList<ushort> outstanding);

        /// <summary>
        /// A retained target was removed because a neutral command was established (or the
        /// bookkeeping was deliberately cleared). <paramref name="reason"/> distinguishes those.
        /// </summary>
        void TrackerRemove(
            int locoAddress,
            ushort amplifier,
            string reason,
            IReadOnlyList<ushort> outstanding);

        /// <summary>Ownership of a physical amplifier transferred to the most recent commanding loco.</summary>
        void TrackerTransfer(ushort amplifier, int fromLocoAddress, int toLocoAddress);

        /// <summary>A safety stop was requested for a scope (Loco or Layout), with its diagnostic.</summary>
        void SafetyStop(string scope, int? locoAddress, string reason, int? block);

        /// <summary>
        /// The command-level outcome of a safety stop. <paramref name="retained"/> is the
        /// pre-stop retained target set; <paramref name="staleFailed"/> records which failed
        /// targets were not fresh, so a freshness-influenced result is explicit.
        /// </summary>
        void SafetyStopResult(
            string scope,
            int? locoAddress,
            bool succeeded,
            bool applied,
            bool backendUnavailable,
            IReadOnlyList<ushort> commanded,
            IReadOnlyList<ushort> failed,
            IReadOnlyList<ushort> retained,
            IReadOnlyList<ushort> staleFailed);

        /// <summary>
        /// The escalation sequence: requested, then established or incomplete. Requested is never
        /// reported as established.
        /// </summary>
        void SafetyEscalation(int? locoAddress, string reason, string result);

        /// <summary>The final target set of the strongest emergency neutralization.</summary>
        void EmergencyTargetSet(IReadOnlyList<ushort> targets, string excluded);

        /// <summary>The manual/operator amplifier command, which is outside per-loco ownership.</summary>
        void ManualControl(ushort amplifier, int pwm, int hr0, bool emoStop);

        /// <summary>
        /// An abnormal rejection, for example an invalid/backplane address presented to a
        /// track-amplifier command API.
        /// </summary>
        void Abnormal(string kind, ushort? amplifier, string detail);
    }
}
