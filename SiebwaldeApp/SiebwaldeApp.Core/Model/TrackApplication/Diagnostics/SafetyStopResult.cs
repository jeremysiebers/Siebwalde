using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// What a safety stop achieved at the command level.
    ///
    /// "Commanded" means a neutral setpoint was accepted for a concrete physical amplifier; it is
    /// not an observed confirmation that the amplifier is physically at neutral. Observed state
    /// remains separate, exactly as requested / commanded / observed stay separate elsewhere in
    /// the control path.
    /// </summary>
    public sealed class SafetyStopResult
    {
        /// <summary>True when at least one neutral command was accepted (or power was removed).</summary>
        public bool Applied { get; init; }

        /// <summary>The physical amplifiers that were commanded neutral.</summary>
        public IReadOnlyList<ushort> CommandedAmplifiers { get; init; } = Array.Empty<ushort>();

        /// <summary>The required amplifiers that could not be commanded neutral.</summary>
        public IReadOnlyList<ushort> FailedAmplifiers { get; init; } = Array.Empty<ushort>();

        /// <summary>True when no hardware backend or neutralizer existed to attempt the stop.</summary>
        public bool BackendUnavailable { get; init; }

        /// <summary>
        /// True when the stop was delivered for every required physical amplifier. A stop that
        /// could not be attempted, or that left a required amplifier uncommanded, is not a success.
        /// </summary>
        public bool Succeeded => Applied && !BackendUnavailable && FailedAmplifiers.Count == 0;

        /// <summary>Every required amplifier was commanded neutral.</summary>
        public static SafetyStopResult Commanded(IReadOnlyList<ushort> commanded)
            => new() { Applied = true, CommandedAmplifiers = commanded };

        /// <summary>Some required amplifiers were commanded neutral, others were not.</summary>
        public static SafetyStopResult Partial(IReadOnlyList<ushort> commanded, IReadOnlyList<ushort> failed)
            => new() { Applied = commanded.Count > 0, CommandedAmplifiers = commanded, FailedAmplifiers = failed };

        /// <summary>Nothing could be applied (for example the locomotive has no resolvable target).</summary>
        public static SafetyStopResult NotApplied() => new();

        /// <summary>No hardware backend or neutralizer was available.</summary>
        public static SafetyStopResult Unavailable() => new() { BackendUnavailable = true };
    }
}
