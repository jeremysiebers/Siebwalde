using System;
using System.Collections.Generic;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Translates ECoS/Koploper switch commands into physical switch output drives, using the
    /// configured <see cref="SwitchMapping"/>. One instance is shared by real and simulator
    /// mode: only the <see cref="ISwitchOutput"/> behind it differs, so there is no separate
    /// control path for the simulator.
    ///
    /// The logical (ECoS) position and the physical position are tracked separately, because
    /// a mapping may invert them. The logical positions feed the routing/look-ahead model,
    /// which speaks Koploper's `g`/`r` semantics.
    /// </summary>
    public sealed class SwitchController
    {
        private readonly ISwitchOutput _output;
        private readonly Action<string>? _log;
        private readonly ControlDiagnostics? _diagnostics;
        private readonly ControlSafetyGuard? _guard;
        private readonly ISwitchObserver? _observer;
        private readonly IObservability? _observability;

        private readonly HashSet<int> _reportedUnavailable = new();
        private readonly Dictionary<int, SwitchPosition> _logicalByEcosAddress = new();
        private readonly Dictionary<int, SwitchPosition> _physicalByAddress = new();
        private readonly object _lock = new();

        public SwitchController(
            SwitchMapping mapping,
            ISwitchOutput output,
            Action<string>? log = null,
            ControlDiagnostics? diagnostics = null,
            ControlSafetyGuard? guard = null,
            ISwitchObserver? observer = null,
            IObservability? observability = null)
        {
            Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _log = log;
            _diagnostics = diagnostics;
            _guard = guard;
            _observer = observer;
            _observability = observability;

            foreach (var error in Mapping.Errors)
            {
                _log?.Invoke($"Switch mapping problem: {error}");
                _diagnostics?.Report(new ControlDiagnostic
                {
                    Code = DiagnosticCode.InvalidConfiguration,
                    Severity = DiagnosticSeverity.Rejected,
                    Subject = "switch mapping",
                    Detail = error
                });
            }
        }

        /// <summary>The mapping this controller translates with.</summary>
        public SwitchMapping Mapping { get; }

        /// <summary>
        /// Translates and applies an ECoS switch request. Returns false (and drives nothing)
        /// when the ECoS address is not mapped, so an unmapped command can never reach an
        /// unrelated output.
        /// </summary>
        public bool TryApply(int ecosAddress, SwitchPosition requested, out SwitchPosition applied)
        {
            applied = requested;

            if (!Mapping.TryGetEntry(ecosAddress, out var entry))
            {
                Report(new ControlDiagnostic
                {
                    Code = DiagnosticCode.UnmappedAddress,
                    Severity = DiagnosticSeverity.Warning,
                    Subject = $"switch {ecosAddress}",
                    SwitchAddress = ecosAddress,
                    Detail = $"Switch {ecosAddress} is not mapped; the command is ignored and no output is driven."
                });

                return false;
            }

            applied = entry.ToPhysical(requested);

            if (!_output.IsAvailable)
            {
                // The real switch output path is not wired yet. That is a known limitation, not
                // a fault, so it is reported once as a warning and never as a confirmation.
                ReportOnce(ecosAddress, new ControlDiagnostic
                {
                    Code = DiagnosticCode.StateUnknown,
                    Severity = DiagnosticSeverity.Warning,
                    Subject = $"switch {ecosAddress}",
                    SwitchAddress = ecosAddress,
                    Detail = $"Physical switch output {entry.PhysicalAddress} is not wired yet; {applied} was not driven and cannot be confirmed."
                });

                return false;
            }

            if (!_output.SetPosition(entry.PhysicalAddress, applied))
            {
                Report(new ControlDiagnostic
                {
                    Code = DiagnosticCode.CommandNotApplied,
                    Severity = DiagnosticSeverity.Rejected,
                    Subject = $"switch {ecosAddress}",
                    SwitchAddress = ecosAddress,
                    Detail = $"The backend did not apply {applied} to physical switch output {entry.PhysicalAddress}."
                });

                return false;
            }

            lock (_lock)
            {
                // Koploper's logical state (drives routing/look-ahead).
                _logicalByEcosAddress[ecosAddress] = requested;
                // What was actually driven, for diagnostics.
                _physicalByAddress[entry.PhysicalAddress] = applied;
            }

            _log?.Invoke(
                $"Switch {ecosAddress} -> physical {entry.PhysicalAddress}: ECoS {requested} = physical {applied}.");

            CheckObservedPosition(ecosAddress, entry, applied);

            return true;
        }

        /// <summary>
        /// Compares the commanded position with what can actually be observed. Only runs when
        /// switch feedback exists; where it does not, no mismatch is fabricated.
        /// </summary>
        private void CheckObservedPosition(int ecosAddress, SwitchMappingEntry entry, SwitchPosition commanded)
        {
            if (_observer is null || _observability is null || !_observability.SwitchFeedbackAvailable)
            {
                return;
            }

            if (!_observer.TryGetObservedPosition(entry.PhysicalAddress, out var observed))
            {
                return;
            }

            if (observed == commanded)
            {
                return;
            }

            var diagnostic = new ControlDiagnostic
            {
                Code = DiagnosticCode.CommandedObservedMismatch,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = $"switch {ecosAddress}",
                SwitchAddress = ecosAddress,
                Detail = $"Switch {ecosAddress} was commanded to {commanded} but the observed position is {observed}."
            };

            var action = _guard?.Apply(diagnostic) ?? SafetyAction.None;
            Report(diagnostic.WithSafetyAction(action));
        }

        private void Report(ControlDiagnostic diagnostic)
        {
            _log?.Invoke(diagnostic.ToString());
            _diagnostics?.Report(diagnostic);
        }

        private void ReportOnce(int ecosAddress, ControlDiagnostic diagnostic)
        {
            lock (_lock)
            {
                if (!_reportedUnavailable.Add(ecosAddress))
                {
                    return;
                }
            }

            Report(diagnostic);
        }

        /// <summary>
        /// Drives every mapped switch that has a configured default position, so the layout
        /// starts from a known state. Entries configured as <c>keep</c> are deliberately left
        /// alone and are not reported as known, because guessing a physical rest position
        /// would let the logical and physical state disagree silently.
        /// </summary>
        public void Initialize()
        {
            foreach (var entry in Mapping.Entries)
            {
                if (entry.DefaultPosition is null)
                {
                    _log?.Invoke(
                        $"Switch {entry.EcosAddress} default is 'keep'; leaving physical {entry.PhysicalAddress} untouched.");
                    continue;
                }

                var logical = entry.DefaultPosition.Value;
                var physical = entry.ToPhysical(logical);

                if (!_output.IsAvailable)
                {
                    ReportOnce(entry.EcosAddress, new ControlDiagnostic
                    {
                        Code = DiagnosticCode.StateUnknown,
                        Severity = DiagnosticSeverity.Warning,
                        Subject = $"switch {entry.EcosAddress}",
                        SwitchAddress = entry.EcosAddress,
                        Detail = $"Physical switch output {entry.PhysicalAddress} is not wired yet; the configured default {logical} was not driven."
                    });

                    continue;
                }

                _output.SetPosition(entry.PhysicalAddress, physical);

                lock (_lock)
                {
                    _logicalByEcosAddress[entry.EcosAddress] = logical;
                    _physicalByAddress[entry.PhysicalAddress] = physical;
                }

                _log?.Invoke(
                    $"Switch {entry.EcosAddress} initialized to {logical} (physical {entry.PhysicalAddress} = {physical}).");
            }
        }

        /// <summary>
        /// The logical switch positions known so far, keyed by ECoS/Koploper switch address.
        /// Used as the switch-position source for routing and look-ahead.
        /// </summary>
        public IReadOnlyDictionary<int, SwitchPosition> GetLogicalPositions()
        {
            lock (_lock)
            {
                return new Dictionary<int, SwitchPosition>(_logicalByEcosAddress);
            }
        }

        /// <summary>The physical position last driven to an output, for diagnostics.</summary>
        public bool TryGetPhysicalPosition(int physicalAddress, out SwitchPosition position)
        {
            lock (_lock)
            {
                return _physicalByAddress.TryGetValue(physicalAddress, out position);
            }
        }
    }
}
