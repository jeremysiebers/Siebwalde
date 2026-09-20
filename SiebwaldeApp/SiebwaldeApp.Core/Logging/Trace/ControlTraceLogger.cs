using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The single production implementation of <see cref="IControlTrace"/>.
    ///
    /// It reuses the existing logging infrastructure: it is constructed with an
    /// <see cref="ILogFactory"/> and writes every event through
    /// <see cref="ILogFactory.Log"/> under the dedicated logger instance
    /// <see cref="LoggerInstance"/>, so the existing <c>FileLogger</c> behaviour, settings,
    /// lifecycle and prefix apply unchanged. No direct file access, no second logging library.
    ///
    /// The <see cref="ILogFactory"/> is used as-is; the internal gate only serializes this
    /// trace's own writes so a single event cannot be interleaved with another trace event.
    /// It does not change the shared logger (see the recorded <c>FileLogger</c> limitations).
    /// </summary>
    public sealed class ControlTraceLogger : IControlTrace
    {
        /// <summary>The dedicated logger instance / component name for the control trace.</summary>
        public const string LoggerInstance = "ControlTraceLog";

        /// <summary>Payload format version, so a parser can detect a future format change.</summary>
        public const int FormatVersion = 1;

        private readonly ILogFactory _factory;
        private readonly object _gate = new();

        /// <summary>Creates the trace over the existing log factory.</summary>
        public ControlTraceLogger(ILogFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc />
        public void SessionStart(string application, string? mode, string? version)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("CONTROL_TRACE_START"),
                ControlTraceFormat.Field("app", application),
                ControlTraceFormat.Field("mode", mode),
                ControlTraceFormat.Field("version", version),
                ControlTraceFormat.Field("format", FormatVersion)));

        /// <inheritdoc />
        public void EcosCommand(string command, int? objectId, IReadOnlyList<string> options)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("ECOS_COMMAND"),
                ControlTraceFormat.Field("protocol", "ECOS"),
                ControlTraceFormat.Field("cmd", command),
                ControlTraceFormat.Field("object", objectId),
                ControlTraceFormat.Field("options", FormatOptions(options))));

        /// <inheritdoc />
        public void SpeedDecision(
            string protocol,
            int? ecosId,
            int address,
            string rawKind,
            int rawValue,
            int normalizedSpeed)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("SPEED_DECISION"),
                ControlTraceFormat.Field("protocol", protocol),
                ControlTraceFormat.Field("ecos", ecosId),
                ControlTraceFormat.Field("address", address),
                ControlTraceFormat.Field("rawkind", rawKind),
                ControlTraceFormat.Field("raw", rawValue),
                ControlTraceFormat.Field("normalized", normalizedSpeed)));

        /// <inheritdoc />
        public void BlockTransition(int locoAddress, int? previousBlock, int newBlock, string source)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("BLOCK_TRANSITION"),
                ControlTraceFormat.Field("loco", locoAddress),
                ControlTraceFormat.Field("previous", previousBlock),
                ControlTraceFormat.Field("block", newBlock),
                ControlTraceFormat.Field("source", source)));

        /// <inheritdoc />
        public void AmplifierCommand(
            string source,
            int? locoAddress,
            ushort amplifier,
            int? block,
            string purpose,
            int pwm,
            int hr0,
            TrackAmplifierOperationalGroup group,
            int? normalizedSpeed,
            int? direction)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("AMPLIFIER_COMMAND"),
                ControlTraceFormat.Field("source", source),
                ControlTraceFormat.Field("loco", locoAddress),
                ControlTraceFormat.Field("amp", (int)amplifier),
                ControlTraceFormat.Field("block", block),
                ControlTraceFormat.Field("purpose", purpose),
                ControlTraceFormat.Field("pwm", pwm),
                ControlTraceFormat.Field("hr0", hr0),
                ControlTraceFormat.Field("group", group),
                ControlTraceFormat.Field("speed", normalizedSpeed),
                ControlTraceFormat.Field("dir", direction)));

        /// <inheritdoc />
        public void AmplifierWrite(ushort amplifier, int hr0)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("AMPLIFIER_WRITE"),
                ControlTraceFormat.Field("amp", (int)amplifier),
                ControlTraceFormat.Field("register", "HR0"),
                ControlTraceFormat.Field("hr0", hr0)));

        /// <inheritdoc />
        public void TrackerAdd(int locoAddress, ushort amplifier, IReadOnlyList<ushort> outstanding)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("TRACKER_ADD"),
                ControlTraceFormat.Field("loco", locoAddress),
                ControlTraceFormat.Field("amp", (int)amplifier),
                ControlTraceFormat.Field("outstanding", ControlTraceFormat.List(outstanding))));

        /// <inheritdoc />
        public void TrackerRemove(
            int locoAddress,
            ushort amplifier,
            string reason,
            IReadOnlyList<ushort> outstanding)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("TRACKER_REMOVE"),
                ControlTraceFormat.Field("loco", locoAddress),
                ControlTraceFormat.Field("amp", (int)amplifier),
                ControlTraceFormat.Field("reason", reason),
                ControlTraceFormat.Field("outstanding", ControlTraceFormat.List(outstanding))));

        /// <inheritdoc />
        public void TrackerTransfer(ushort amplifier, int fromLocoAddress, int toLocoAddress)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("TRACKER_TRANSFER"),
                ControlTraceFormat.Field("amp", (int)amplifier),
                ControlTraceFormat.Field("from", fromLocoAddress),
                ControlTraceFormat.Field("to", toLocoAddress)));

        /// <inheritdoc />
        public void SafetyStop(string scope, int? locoAddress, string reason, int? block)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("SAFETY_STOP"),
                ControlTraceFormat.Field("scope", scope),
                ControlTraceFormat.Field("loco", locoAddress),
                ControlTraceFormat.Field("reason", reason),
                ControlTraceFormat.Field("block", block)));

        /// <inheritdoc />
        public void SafetyStopResult(
            string scope,
            int? locoAddress,
            bool succeeded,
            bool applied,
            bool backendUnavailable,
            IReadOnlyList<ushort> commanded,
            IReadOnlyList<ushort> failed,
            IReadOnlyList<ushort> retained,
            IReadOnlyList<ushort> staleFailed)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("SAFETY_STOP_RESULT"),
                ControlTraceFormat.Field("scope", scope),
                ControlTraceFormat.Field("loco", locoAddress),
                ControlTraceFormat.Field("succeeded", succeeded),
                ControlTraceFormat.Field("applied", applied),
                ControlTraceFormat.Field("backendunavailable", backendUnavailable),
                ControlTraceFormat.Field("commanded", ControlTraceFormat.List(commanded)),
                ControlTraceFormat.Field("failed", ControlTraceFormat.List(failed)),
                ControlTraceFormat.Field("retained", ControlTraceFormat.List(retained)),
                ControlTraceFormat.Field("stale", ControlTraceFormat.List(staleFailed))));

        /// <inheritdoc />
        public void SafetyEscalation(int? locoAddress, string reason, string result)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("SAFETY_ESCALATION"),
                ControlTraceFormat.Field("loco", locoAddress),
                ControlTraceFormat.Field("reason", reason),
                ControlTraceFormat.Field("result", result)));

        /// <inheritdoc />
        public void EmergencyTargetSet(IReadOnlyList<ushort> targets, string excluded)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("EMERGENCY_TARGET_SET"),
                ControlTraceFormat.Field("targets", ControlTraceFormat.List(targets)),
                ControlTraceFormat.Field("excluded", excluded)));

        /// <inheritdoc />
        public void ManualControl(ushort amplifier, int pwm, int hr0, bool emoStop)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("MANUAL_CONTROL"),
                ControlTraceFormat.Field("source", "Manual"),
                ControlTraceFormat.Field("amp", (int)amplifier),
                ControlTraceFormat.Field("pwm", pwm),
                ControlTraceFormat.Field("hr0", hr0),
                ControlTraceFormat.Field("emostop", emoStop)));

        /// <inheritdoc />
        public void Abnormal(string kind, ushort? amplifier, string detail)
            => Emit(ControlTraceFormat.Join(
                ControlTraceFormat.Event("ABNORMAL"),
                ControlTraceFormat.Field("kind", kind),
                ControlTraceFormat.Field("amp", amplifier.HasValue ? (int?)amplifier.Value : null),
                ControlTraceFormat.Field("detail", detail)));

        private void Emit(string payload)
        {
            lock (_gate)
            {
                _factory.Log(payload, LoggerInstance);
            }
        }

        private static string FormatOptions(IReadOnlyList<string>? options)
        {
            if (options is null || options.Count == 0)
            {
                return ControlTraceFormat.None;
            }

            return string.Join(
                ControlTraceFormat.ListSeparator,
                options.Select(o => ControlTraceFormat.Sanitize(o)));
        }
    }
}
