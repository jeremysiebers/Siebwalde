using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Applies the safety reaction for a diagnostic, exactly once per persistent fault.
    ///
    /// A fault is identified by <see cref="ControlDiagnostic.Key"/> (code + subject). Once a
    /// fault has caused a stop, repeated evaluations of the same fault do nothing, so a
    /// persistent condition cannot produce a stop storm. A different fault is still acted on,
    /// and nothing clears the latch except an explicit <see cref="Reset"/>.
    /// </summary>
    public sealed class ControlSafetyGuard
    {
        private readonly ISafetyStopSink _stops;
        private readonly ControlDiagnostics _diagnostics;
        private readonly Action<string>? _log;
        private readonly Dictionary<string, ControlDiagnostic> _latched = new();
        private readonly object _lock = new();

        public ControlSafetyGuard(
            ISafetyStopSink stops,
            ControlDiagnostics diagnostics,
            Action<string>? log = null)
        {
            _stops = stops ?? throw new ArgumentNullException(nameof(stops));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _log = log;
        }

        /// <summary>True while at least one safety fault is latched.</summary>
        public bool IsLatched
        {
            get
            {
                lock (_lock)
                {
                    return _latched.Count > 0;
                }
            }
        }

        /// <summary>
        /// True when a latched fault cannot be attributed to a single locomotive, so it applies
        /// to the whole layout.
        /// </summary>
        public bool IsLayoutLatched
        {
            get
            {
                lock (_lock)
                {
                    return _latched.Values.Any(d => d.LocoAddress is null);
                }
            }
        }

        /// <summary>The locomotives that have a latched fault of their own.</summary>
        public IReadOnlyCollection<int> LatchedLocos
        {
            get
            {
                lock (_lock)
                {
                    return _latched.Values
                        .Where(d => d.LocoAddress is not null)
                        .Select(d => d.LocoAddress!.Value)
                        .Distinct()
                        .ToList();
                }
            }
        }

        /// <summary>The latched fault keys, for diagnostics.</summary>
        public IReadOnlyCollection<string> LatchedKeys
        {
            get
            {
                lock (_lock)
                {
                    return new List<string>(_latched.Keys);
                }
            }
        }

        /// <summary>The latched faults themselves, used for revalidation.</summary>
        public IReadOnlyCollection<ControlDiagnostic> LatchedFaults
        {
            get
            {
                lock (_lock)
                {
                    return new List<ControlDiagnostic>(_latched.Values);
                }
            }
        }

        /// <summary>
        /// Revalidates a latched fault before recovery is allowed. Returns true when the
        /// condition is resolved. Set by the composition root; when unset, a fault is treated
        /// as resolvable.
        /// </summary>
        public Func<ControlDiagnostic, bool>? RevalidationCheck { get; set; }

        /// <summary>
        /// True when a locomotive may be commanded to move. A layout-wide latch blocks every
        /// locomotive; a loco-scoped latch blocks only the locomotives it names.
        /// </summary>
        public bool AllowsMovement(int locoAddress)
        {
            lock (_lock)
            {
                if (_latched.Count == 0)
                {
                    return true;
                }

                if (_latched.Values.Any(d => d.LocoAddress is null))
                {
                    return false;
                }

                return !_latched.Values.Any(d => d.LocoAddress == locoAddress);
            }
        }

        /// <summary>
        /// True when power may be switched back on. Only a layout-wide latch refuses this,
        /// because only then was a layout stop part of the safety action.
        /// </summary>
        public bool AllowsPowerOn() => !IsLayoutLatched;

        /// <summary>
        /// Switch commands stay possible during a latch: a corrective switch change is often the
        /// only way to resolve the divergence. Exposed so the rule is explicit and testable.
        /// </summary>
        public bool AllowsSwitchCommand() => true;

        /// <summary>
        /// Applies the safety action for a diagnostic and returns the action that was taken.
        /// Idempotent per fault key.
        /// </summary>
        public SafetyAction Apply(ControlDiagnostic diagnostic)
        {
            if (diagnostic is null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            if (diagnostic.Severity != DiagnosticSeverity.StopRequired)
            {
                return SafetyAction.None;
            }

            lock (_lock)
            {
                if (_latched.ContainsKey(diagnostic.Key))
                {
                    _log?.Invoke(
                        $"Safety fault '{diagnostic.Key}' is already latched; no repeated stop is issued.");
                    return SafetyAction.None;
                }

                _latched[diagnostic.Key] = diagnostic;
            }

            // A locomotive-scoped fault stops that locomotive so unrelated trains keep running.
            // Only a fault that cannot be attributed to one locomotive stops the whole layout.
            var action = diagnostic.LocoAddress is int loco
                ? StopLoco(diagnostic, loco)
                : StopLayout(diagnostic);

            return action;
        }

        private SafetyAction StopLoco(ControlDiagnostic diagnostic, int loco)
        {
            SafetyStopResult result;

            try
            {
                result = _stops.StopLoco(loco);
            }
            catch (Exception ex)
            {
                _log?.Invoke($"Safety stop for loco {loco} failed: {ex.Message}");
                result = SafetyStopResult.NotApplied();
            }

            if (result.Succeeded)
            {
                _log?.Invoke($"Safety stop issued for loco {loco} ({diagnostic.Key}).");
                return SafetyAction.StopLoco;
            }

            // The loco-scoped stop could not be delivered for every required physical output.
            // Report it and escalate conservatively: the physical evidence showed the layout /
            // amplifier-centric neutralization can reach outputs the loco stop cannot.
            _log?.Invoke(
                $"Safety stop for loco {loco} did not neutralize every required physical output; escalating to amplifier-centric neutralization.");

            _diagnostics.Report(new ControlDiagnostic
            {
                Code = result.BackendUnavailable
                    ? DiagnosticCode.BackendUnavailable
                    : DiagnosticCode.CommandNotApplied,
                Severity = DiagnosticSeverity.Rejected,
                Subject = $"loco {loco} safety stop",
                LocoAddress = loco,
                Block = diagnostic.Block,
                Detail = result.BackendUnavailable
                    ? "Loco-scoped neutralization could not be delivered: no hardware backend is available."
                    : $"Loco-scoped neutralization was incomplete (uncommanded amplifier(s): {DescribeAmplifiers(result.FailedAmplifiers)}); escalating to amplifier-centric neutralization.",
                SafetyAction = SafetyAction.StopLayoutEscalated
            });

            return EscalateToLayoutNeutralization(diagnostic, loco);
        }

        private SafetyAction EscalateToLayoutNeutralization(ControlDiagnostic diagnostic, int loco)
        {
            SafetyStopResult result;

            try
            {
                result = _stops.StopLayout();
            }
            catch (Exception ex)
            {
                _log?.Invoke($"Amplifier-centric safety stop failed: {ex.Message}");
                result = SafetyStopResult.NotApplied();
            }

            if (result.Succeeded)
            {
                _log?.Invoke($"Amplifier-centric neutralization issued for the layout ({diagnostic.Key}).");
                return SafetyAction.StopLayoutEscalated;
            }

            _log?.Invoke(
                "Amplifier-centric neutralization was incomplete; the safety concern remains latched.");

            _diagnostics.Report(new ControlDiagnostic
            {
                Code = result.BackendUnavailable
                    ? DiagnosticCode.BackendUnavailable
                    : DiagnosticCode.CommandNotApplied,
                Severity = DiagnosticSeverity.Rejected,
                Subject = "layout safety stop",
                LocoAddress = loco,
                Block = diagnostic.Block,
                Detail = result.BackendUnavailable
                    ? "Amplifier-centric neutralization could not be delivered: no hardware backend is available; the safety concern remains latched."
                    : $"Amplifier-centric neutralization was incomplete (uncommanded amplifier(s): {DescribeAmplifiers(result.FailedAmplifiers)}); the safety concern remains latched.",
                SafetyAction = SafetyAction.StopLayoutEscalated
            });

            return SafetyAction.StopLayoutEscalated;
        }

        private SafetyAction StopLayout(ControlDiagnostic diagnostic)
        {
            SafetyStopResult result;

            try
            {
                result = _stops.StopLayout();
            }
            catch (Exception ex)
            {
                _log?.Invoke($"Layout safety stop failed: {ex.Message}");
                result = SafetyStopResult.NotApplied();
            }

            if (result.Succeeded)
            {
                _log?.Invoke($"Safety stop issued for the whole layout ({diagnostic.Key}).");
                return SafetyAction.StopLayout;
            }

            _log?.Invoke("Layout safety stop was incomplete; the safety concern remains latched.");

            _diagnostics.Report(new ControlDiagnostic
            {
                Code = result.BackendUnavailable
                    ? DiagnosticCode.BackendUnavailable
                    : DiagnosticCode.CommandNotApplied,
                Severity = DiagnosticSeverity.Rejected,
                Subject = "layout safety stop",
                Block = diagnostic.Block,
                Detail = result.BackendUnavailable
                    ? "Layout neutralization could not be delivered: no hardware backend is available; the safety concern remains latched."
                    : $"Layout neutralization was incomplete (uncommanded amplifier(s): {DescribeAmplifiers(result.FailedAmplifiers)}); the safety concern remains latched.",
                SafetyAction = SafetyAction.StopLayout
            });

            return SafetyAction.StopLayout;
        }

        private static string DescribeAmplifiers(IReadOnlyList<ushort> amplifiers)
            => amplifiers.Count == 0 ? "<none>" : string.Join(",", amplifiers);

        /// <summary>
        /// Clears the latches and the latched unsafe state, but only when every latched fault
        /// revalidates as resolved. A persistent fault never clears itself, and a reset without
        /// a correction is refused so movement permission is not restored prematurely.
        /// </summary>
        /// <returns>True when the reset was applied, false when it was refused.</returns>
        public bool Reset()
        {
            List<ControlDiagnostic> unresolved;

            lock (_lock)
            {
                unresolved = _latched.Values
                    .Where(d => RevalidationCheck is not null && !RevalidationCheck(d))
                    .ToList();
            }

            if (unresolved.Count > 0)
            {
                foreach (var fault in unresolved)
                {
                    _log?.Invoke($"Safety reset refused: '{fault.Key}' is still not resolved.");

                    _diagnostics.Report(new ControlDiagnostic
                    {
                        Code = DiagnosticCode.ResetRefused,
                        Severity = DiagnosticSeverity.Rejected,
                        Subject = fault.Subject,
                        Detail = $"Safety reset refused: the condition behind '{fault.Key}' is still present.",
                        LocoAddress = fault.LocoAddress,
                        Block = fault.Block,
                        SwitchAddress = fault.SwitchAddress,
                        RequiredSwitchPosition = fault.RequiredSwitchPosition
                    });
                }

                return false;
            }

            lock (_lock)
            {
                _latched.Clear();
            }

            _diagnostics.ClearLatch();
            _log?.Invoke("Safety latches cleared by explicit reset.");
            return true;
        }
    }
}
