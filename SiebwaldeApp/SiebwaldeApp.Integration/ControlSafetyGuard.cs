using System;
using System.Collections.Generic;
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
        private readonly HashSet<string> _latchedKeys = new();
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
                    return _latchedKeys.Count > 0;
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
                    return new List<string>(_latchedKeys);
                }
            }
        }

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
                if (!_latchedKeys.Add(diagnostic.Key))
                {
                    _log?.Invoke(
                        $"Safety fault '{diagnostic.Key}' is already latched; no repeated stop is issued.");
                    return SafetyAction.None;
                }
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
            try
            {
                _stops.StopLoco(loco);
                _log?.Invoke($"Safety stop issued for loco {loco} ({diagnostic.Key}).");
                return SafetyAction.StopLoco;
            }
            catch (Exception ex)
            {
                _log?.Invoke($"Safety stop for loco {loco} failed: {ex.Message}");
                return SafetyAction.None;
            }
        }

        private SafetyAction StopLayout(ControlDiagnostic diagnostic)
        {
            try
            {
                _stops.StopLayout();
                _log?.Invoke($"Safety stop issued for the whole layout ({diagnostic.Key}).");
                return SafetyAction.StopLayout;
            }
            catch (Exception ex)
            {
                _log?.Invoke($"Layout safety stop failed: {ex.Message}");
                return SafetyAction.None;
            }
        }

        /// <summary>
        /// Clears the latches and the latched unsafe state. This is the explicit recovery step:
        /// a persistent fault never clears itself.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _latchedKeys.Clear();
            }

            _diagnostics.ClearLatch();
            _log?.Invoke("Safety latches cleared by explicit reset.");
        }
    }
}
