using System;
using System.Linq;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Detects when the requested/logical state and the state the control system can actually
    /// guarantee stop agreeing, and hands the safety reaction to <see cref="ControlSafetyGuard"/>.
    ///
    /// It reuses <see cref="BlockTopology"/> for route conditions, the switch controller for the
    /// known logical switch positions and <see cref="IOccupancyProvider"/> for occupancy. It only
    /// checks what is observable: when occupancy or switch feedback is unavailable by design, no
    /// mismatch is fabricated.
    /// </summary>
    public sealed class DivergenceChecker
    {
        private readonly BlockTopology _topology;
        private readonly SwitchController _switches;
        private readonly IOccupancyProvider? _occupancy;
        private readonly IObservability _observability;
        private readonly ControlDiagnostics _diagnostics;
        private readonly ControlSafetyGuard _guard;
        private readonly Action<string>? _log;

        public DivergenceChecker(
            BlockTopology topology,
            SwitchController switches,
            IOccupancyProvider? occupancy,
            IObservability observability,
            ControlDiagnostics diagnostics,
            ControlSafetyGuard guard,
            Action<string>? log = null)
        {
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _switches = switches ?? throw new ArgumentNullException(nameof(switches));
            _occupancy = occupancy;
            _observability = observability ?? throw new ArgumentNullException(nameof(observability));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _guard = guard ?? throw new ArgumentNullException(nameof(guard));
            _log = log;
        }

        /// <summary>
        /// Checks whether a locomotive may move from one block into the next. Returns the
        /// diagnostic when a divergence was found, or null when the move is not contradicted.
        /// </summary>
        public ControlDiagnostic? CheckTransition(int locoAddress, int fromBlock, int toBlock)
        {
            var transition = _topology.GetTransitionsFrom(fromBlock).FirstOrDefault(t => t.ToBlock == toBlock);
            if (transition is null)
            {
                // Not a modelled route: there is nothing to contradict.
                return null;
            }

            if (transition.SwitchId is int switchId)
            {
                var switchDiagnostic = CheckSwitchCondition(locoAddress, fromBlock, toBlock, switchId, transition);
                if (switchDiagnostic is not null)
                {
                    return switchDiagnostic;
                }
            }

            if (_observability.OccupancyAvailable && _occupancy is not null && _occupancy.IsBlockOccupied(toBlock))
            {
                return Report(new ControlDiagnostic
                {
                    Code = DiagnosticCode.OccupancyMismatch,
                    Severity = DiagnosticSeverity.StopRequired,
                    Subject = $"block {toBlock}",
                    LocoAddress = locoAddress,
                    Block = toBlock,
                    Detail = $"Loco {locoAddress} is routed from block {fromBlock} into block {toBlock}, which is reported occupied."
                });
            }

            return null;
        }

        /// <summary>
        /// Reports that the control path cannot reach the hardware, so no state can be guaranteed.
        /// Not attributable to one locomotive, so the safety reaction is a layout stop.
        /// </summary>
        public ControlDiagnostic ReportBackendUnavailable(string detail)
            => Report(new ControlDiagnostic
            {
                Code = DiagnosticCode.BackendUnavailable,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "backend",
                Detail = detail
            });

        /// <summary>Reports an invalid or contradictory configuration.</summary>
        public ControlDiagnostic ReportInvalidConfiguration(string subject, string detail)
            => Report(new ControlDiagnostic
            {
                Code = DiagnosticCode.InvalidConfiguration,
                Severity = DiagnosticSeverity.Rejected,
                Subject = subject,
                Detail = detail
            });

        private ControlDiagnostic? CheckSwitchCondition(
            int locoAddress,
            int fromBlock,
            int toBlock,
            int switchId,
            BlockTransition transition)
        {
            if (!_switches.Mapping.TryGetEntry(switchId, out _))
            {
                return Report(new ControlDiagnostic
                {
                    Code = DiagnosticCode.UnmappedAddress,
                    Severity = DiagnosticSeverity.Rejected,
                    Subject = $"switch {switchId}",
                    LocoAddress = locoAddress,
                    Block = fromBlock,
                    SwitchAddress = switchId,
                    Detail = $"Route {fromBlock}->{toBlock} requires switch {switchId}, which is not mapped; the route cannot be verified."
                });
            }

            var positions = _switches.GetLogicalPositions();
            if (!positions.TryGetValue(switchId, out var known))
            {
                return Report(new ControlDiagnostic
                {
                    Code = DiagnosticCode.StateUnknown,
                    Severity = DiagnosticSeverity.Rejected,
                    Subject = $"switch {switchId}",
                    LocoAddress = locoAddress,
                    Block = fromBlock,
                    SwitchAddress = switchId,
                    Detail = $"Route {fromBlock}->{toBlock} requires switch {switchId} to be {transition.RequiredSwitchPosition}, but its position is not known."
                });
            }

            if (transition.RequiredSwitchPosition is SwitchPosition required && known != required)
            {
                return Report(new ControlDiagnostic
                {
                    Code = DiagnosticCode.RouteSwitchMismatch,
                    Severity = DiagnosticSeverity.StopRequired,
                    Subject = $"switch {switchId}",
                    LocoAddress = locoAddress,
                    Block = fromBlock,
                    SwitchAddress = switchId,
                    Detail = $"Route {fromBlock}->{toBlock} requires switch {switchId} to be {required}, but the known logical position is {known}."
                });
            }

            return null;
        }

        private ControlDiagnostic Report(ControlDiagnostic diagnostic)
        {
            var action = _guard.Apply(diagnostic);
            var withAction = diagnostic.WithSafetyAction(action);

            _log?.Invoke(withAction.ToString());
            _diagnostics.Report(withAction);

            return withAction;
        }
    }
}
