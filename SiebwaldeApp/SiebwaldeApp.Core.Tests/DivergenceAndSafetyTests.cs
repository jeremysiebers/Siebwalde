using System.Collections.Generic;
using SiebwaldeApp.Core;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Divergence detection, safety reaction and diagnostics behaviour. Software only: no
    /// hardware, no WPF.
    /// </summary>
    public class DivergenceAndSafetyTests
    {
        private const string OvalTopology =
            "amps: 1:1,2:2,3:3,4:4,5:5 ; routes: 1>2,2>3,3>4@1:0,3>5@1:1,4>1,5>1";

        private sealed class RecordingStopSink : ISafetyStopSink
        {
            public List<int> StoppedLocos { get; } = new();
            public int LayoutStops { get; private set; }

            public bool StopLoco(int address)
            {
                StoppedLocos.Add(address);
                return true;
            }

            public void StopLayout() => LayoutStops++;
        }

        private sealed class FakeOccupancy : IOccupancyProvider
        {
            private readonly HashSet<int> _occupied = new();

            public void Occupy(int block) => _occupied.Add(block);

            public bool IsBlockOccupied(int block) => _occupied.Contains(block);
        }

        private sealed class RecordingSwitchOutput : ISwitchOutput
        {
            public bool IsAvailable { get; set; } = true;
            public bool Applied { get; set; } = true;

            public bool SetPosition(int physicalAddress, SwitchPosition position) => Applied;
        }

        private sealed class FixedObserver : ISwitchObserver
        {
            public SwitchPosition? Observed { get; set; }

            public bool TryGetObservedPosition(int physicalAddress, out SwitchPosition position)
            {
                position = Observed ?? default;
                return Observed is not null;
            }
        }

        private static SwitchController CreateController(
            RecordingSwitchOutput output,
            ControlDiagnostics diagnostics,
            ControlSafetyGuard guard,
            ISwitchObserver? observer = null,
            IObservability? observability = null,
            string mapping = "1:1:keep")
            => new(
                SwitchMapping.Parse(mapping),
                output,
                log: null,
                diagnostics,
                guard,
                observer,
                observability);

        private static (DivergenceChecker Checker, SwitchController Switches, RecordingStopSink Stops, ControlDiagnostics Diagnostics, ControlSafetyGuard Guard, FakeOccupancy Occupancy)
            CreateChecker(bool occupancyAvailable = true, string mapping = "1:1:keep")
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink();
            var guard = new ControlSafetyGuard(stops, diagnostics);
            var occupancy = new FakeOccupancy();

            var switches = CreateController(
                new RecordingSwitchOutput(),
                diagnostics,
                guard,
                observability: new ModeObservability
                {
                    SwitchFeedbackAvailable = false,
                    OccupancyAvailable = occupancyAvailable
                },
                mapping: mapping);

            var checker = new DivergenceChecker(
                BlockTopology.Parse(OvalTopology),
                switches,
                occupancy,
                new ModeObservability { SwitchFeedbackAvailable = false, OccupancyAvailable = occupancyAvailable },
                diagnostics,
                guard);

            return (checker, switches, stops, diagnostics, guard, occupancy);
        }

        // ---------------------------------------------------------------------
        // Diagnostics surface
        // ---------------------------------------------------------------------

        [Fact]
        public void Diagnostics_LatchTheFirstStopRequiredAndKeepIt()
        {
            var diagnostics = new ControlDiagnostics();

            diagnostics.Report(new ControlDiagnostic
            {
                Code = DiagnosticCode.RouteSwitchMismatch,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "switch 1",
                Detail = "first"
            });

            diagnostics.Report(new ControlDiagnostic
            {
                Code = DiagnosticCode.OccupancyMismatch,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "block 4",
                Detail = "second"
            });

            Assert.True(diagnostics.IsUnsafe);
            // The root cause stays visible instead of being replaced by later faults.
            Assert.Equal("switch 1", diagnostics.LatchedUnsafe!.Subject);
            Assert.Equal(2, diagnostics.Recent.Count);
        }

        [Fact]
        public void Diagnostics_LowerSeverityNeverClearsTheLatch()
        {
            var diagnostics = new ControlDiagnostics();
            diagnostics.Report(new ControlDiagnostic
            {
                Code = DiagnosticCode.RouteSwitchMismatch,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "switch 1"
            });

            diagnostics.Report(new ControlDiagnostic
            {
                Code = DiagnosticCode.UnmappedAddress,
                Severity = DiagnosticSeverity.Warning,
                Subject = "switch 9"
            });
            diagnostics.Report(new ControlDiagnostic
            {
                Code = DiagnosticCode.UnmappedAddress,
                Severity = DiagnosticSeverity.Info,
                Subject = "switch 9"
            });

            Assert.True(diagnostics.IsUnsafe);
        }

        [Fact]
        public void Diagnostics_ClearLatchIsTheOnlyWayToRecover()
        {
            var diagnostics = new ControlDiagnostics();
            diagnostics.Report(new ControlDiagnostic
            {
                Code = DiagnosticCode.RouteSwitchMismatch,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "switch 1"
            });

            diagnostics.ClearLatch();

            Assert.False(diagnostics.IsUnsafe);
            Assert.Null(diagnostics.LatchedUnsafe);
            Assert.Equal(DiagnosticSeverity.StopRequired, diagnostics.CurrentSeverity);
        }

        [Fact]
        public void Diagnostics_HistoryIsBounded()
        {
            var diagnostics = new ControlDiagnostics(capacity: 3);

            for (var i = 0; i < 10; i++)
            {
                diagnostics.Report(new ControlDiagnostic
                {
                    Code = DiagnosticCode.UnmappedAddress,
                    Severity = DiagnosticSeverity.Warning,
                    Subject = $"switch {i}"
                });
            }

            Assert.Equal(3, diagnostics.Recent.Count);
            Assert.Equal("switch 9", diagnostics.Latest!.Subject);
        }

        // ---------------------------------------------------------------------
        // Safety guard
        // ---------------------------------------------------------------------

        [Fact]
        public void Guard_LocoScopedFaultStopsOnlyThatLoco()
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink();
            var guard = new ControlSafetyGuard(stops, diagnostics);

            var action = guard.Apply(new ControlDiagnostic
            {
                Code = DiagnosticCode.RouteSwitchMismatch,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "switch 1",
                LocoAddress = 7
            });

            Assert.Equal(SafetyAction.StopLoco, action);
            Assert.Equal(new[] { 7 }, stops.StoppedLocos);
            Assert.Equal(0, stops.LayoutStops);
        }

        [Fact]
        public void Guard_UnattributableFaultStopsTheLayout()
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink();
            var guard = new ControlSafetyGuard(stops, diagnostics);

            var action = guard.Apply(new ControlDiagnostic
            {
                Code = DiagnosticCode.BackendUnavailable,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "backend"
            });

            Assert.Equal(SafetyAction.StopLayout, action);
            Assert.Equal(1, stops.LayoutStops);
            Assert.Empty(stops.StoppedLocos);
        }

        [Fact]
        public void Guard_RepeatedSameFaultDoesNotRepeatTheStop()
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink();
            var guard = new ControlSafetyGuard(stops, diagnostics);

            var fault = new ControlDiagnostic
            {
                Code = DiagnosticCode.RouteSwitchMismatch,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "switch 1",
                LocoAddress = 7
            };

            Assert.Equal(SafetyAction.StopLoco, guard.Apply(fault));
            Assert.Equal(SafetyAction.None, guard.Apply(fault));
            Assert.Equal(SafetyAction.None, guard.Apply(fault));

            Assert.Single(stops.StoppedLocos);
        }

        [Fact]
        public void Guard_DifferentFaultIsStillActedOn()
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink();
            var guard = new ControlSafetyGuard(stops, diagnostics);

            guard.Apply(new ControlDiagnostic
            {
                Code = DiagnosticCode.RouteSwitchMismatch,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "switch 1",
                LocoAddress = 7
            });
            guard.Apply(new ControlDiagnostic
            {
                Code = DiagnosticCode.OccupancyMismatch,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "block 4",
                LocoAddress = 8
            });

            Assert.Equal(new[] { 7, 8 }, stops.StoppedLocos);
        }

        [Fact]
        public void Guard_ResetAllowsTheSameFaultToStopAgain()
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink();
            var guard = new ControlSafetyGuard(stops, diagnostics);

            var fault = new ControlDiagnostic
            {
                Code = DiagnosticCode.RouteSwitchMismatch,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = "switch 1",
                LocoAddress = 7
            };

            guard.Apply(fault);
            guard.Reset();

            Assert.False(guard.IsLatched);
            Assert.False(diagnostics.IsUnsafe);

            Assert.Equal(SafetyAction.StopLoco, guard.Apply(fault));
            Assert.Equal(2, stops.StoppedLocos.Count);
        }

        [Theory]
        [InlineData(DiagnosticSeverity.Info)]
        [InlineData(DiagnosticSeverity.Warning)]
        [InlineData(DiagnosticSeverity.Rejected)]
        public void Guard_NonStopSeveritiesNeverStop(DiagnosticSeverity severity)
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink();
            var guard = new ControlSafetyGuard(stops, diagnostics);

            var action = guard.Apply(new ControlDiagnostic
            {
                Code = DiagnosticCode.UnmappedAddress,
                Severity = severity,
                Subject = "switch 9"
            });

            Assert.Equal(SafetyAction.None, action);
            Assert.Empty(stops.StoppedLocos);
            Assert.Equal(0, stops.LayoutStops);
        }

        // ---------------------------------------------------------------------
        // Route divergence
        // ---------------------------------------------------------------------

        [Fact]
        public void SafeRoute_WhenSwitchMatches_ReportsNothing()
        {
            var (checker, switches, stops, _, _, _) = CreateChecker();
            switches.TryApply(1, SwitchPosition.Straight, out _);

            Assert.Null(checker.CheckTransition(locoAddress: 1, fromBlock: 3, toBlock: 4));
            Assert.Empty(stops.StoppedLocos);
        }

        [Fact]
        public void RouteWithMismatchedSwitch_StopsThatLocoAndCarriesContext()
        {
            var (checker, switches, stops, diagnostics, _, _) = CreateChecker();
            // Route 3->4 needs switch 1 straight; the known position is diverging.
            switches.TryApply(1, SwitchPosition.Diverging, out _);

            var diagnostic = checker.CheckTransition(locoAddress: 1, fromBlock: 3, toBlock: 4);

            Assert.NotNull(diagnostic);
            Assert.Equal(DiagnosticCode.RouteSwitchMismatch, diagnostic!.Code);
            Assert.Equal(DiagnosticSeverity.StopRequired, diagnostic.Severity);
            Assert.Equal(1, diagnostic.LocoAddress);
            Assert.Equal(3, diagnostic.Block);
            Assert.Equal(1, diagnostic.SwitchAddress);
            Assert.Equal(SafetyAction.StopLoco, diagnostic.SafetyAction);

            Assert.Equal(new[] { 1 }, stops.StoppedLocos);
            Assert.True(diagnostics.IsUnsafe);
        }

        [Fact]
        public void RouteSwitchMismatch_DoesNotStopUnrelatedLocos()
        {
            var (checker, switches, stops, _, _, _) = CreateChecker();
            switches.TryApply(1, SwitchPosition.Diverging, out _);

            checker.CheckTransition(locoAddress: 1, fromBlock: 3, toBlock: 4);

            Assert.DoesNotContain(2, stops.StoppedLocos);
        }

        [Fact]
        public void RouteWithUnmappedSwitch_IsRejectedNotSilentlySafe()
        {
            var (checker, _, stops, diagnostics, _, _) = CreateChecker(mapping: "2:2:keep");

            var diagnostic = checker.CheckTransition(locoAddress: 1, fromBlock: 3, toBlock: 4);

            Assert.NotNull(diagnostic);
            Assert.Equal(DiagnosticCode.UnmappedAddress, diagnostic!.Code);
            Assert.Equal(DiagnosticSeverity.Rejected, diagnostic.Severity);
            Assert.Empty(stops.StoppedLocos);
            Assert.False(diagnostics.IsUnsafe);
        }

        [Fact]
        public void RouteWithUnknownSwitchPosition_IsRejected()
        {
            var (checker, _, stops, _, _, _) = CreateChecker();

            // Switch 1 is mapped but its position was never established.
            var diagnostic = checker.CheckTransition(locoAddress: 1, fromBlock: 3, toBlock: 4);

            Assert.NotNull(diagnostic);
            Assert.Equal(DiagnosticCode.StateUnknown, diagnostic!.Code);
            Assert.Equal(DiagnosticSeverity.Rejected, diagnostic.Severity);
            Assert.Empty(stops.StoppedLocos);
        }

        [Fact]
        public void RouteWithoutSwitchCondition_IsNotContradicted()
        {
            var (checker, _, stops, _, _, _) = CreateChecker();

            Assert.Null(checker.CheckTransition(locoAddress: 1, fromBlock: 1, toBlock: 2));
            Assert.Empty(stops.StoppedLocos);
        }

        [Fact]
        public void OccupiedTargetBlock_StopsThatLoco()
        {
            var (checker, switches, stops, _, _, occupancy) = CreateChecker();
            switches.TryApply(1, SwitchPosition.Straight, out _);
            occupancy.Occupy(4);

            var diagnostic = checker.CheckTransition(locoAddress: 1, fromBlock: 3, toBlock: 4);

            Assert.NotNull(diagnostic);
            Assert.Equal(DiagnosticCode.OccupancyMismatch, diagnostic!.Code);
            Assert.Equal(SafetyAction.StopLoco, diagnostic.SafetyAction);
            Assert.Equal(new[] { 1 }, stops.StoppedLocos);
        }

        [Fact]
        public void UnavailableOccupancy_DoesNotFabricateAMismatch()
        {
            // Real mode: the amplifier occupancy bit is still a firmware TODO.
            var (checker, switches, stops, diagnostics, _, occupancy) = CreateChecker(occupancyAvailable: false);
            switches.TryApply(1, SwitchPosition.Straight, out _);
            occupancy.Occupy(4);

            Assert.Null(checker.CheckTransition(locoAddress: 1, fromBlock: 3, toBlock: 4));
            Assert.Empty(stops.StoppedLocos);
            Assert.False(diagnostics.IsUnsafe);
        }

        [Fact]
        public void PersistentRouteFault_DoesNotProduceAStopStorm()
        {
            var (checker, switches, stops, diagnostics, _, _) = CreateChecker();
            switches.TryApply(1, SwitchPosition.Diverging, out _);

            for (var i = 0; i < 5; i++)
            {
                checker.CheckTransition(locoAddress: 1, fromBlock: 3, toBlock: 4);
            }

            Assert.Single(stops.StoppedLocos);
            Assert.True(diagnostics.IsUnsafe);
            Assert.Equal(5, diagnostics.Recent.Count);
        }

        [Fact]
        public void BackendUnavailable_StopsTheLayout()
        {
            var (checker, _, stops, _, _, _) = CreateChecker();

            var diagnostic = checker.ReportBackendUnavailable("Track controller is not reachable.");

            Assert.Equal(DiagnosticCode.BackendUnavailable, diagnostic.Code);
            Assert.Equal(SafetyAction.StopLayout, diagnostic.SafetyAction);
            Assert.Equal(1, stops.LayoutStops);
        }

        // ---------------------------------------------------------------------
        // Switch-level divergence
        // ---------------------------------------------------------------------

        [Fact]
        public void UnmappedSwitchCommand_ReportsAWarningAndDrivesNothing()
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink();
            var guard = new ControlSafetyGuard(stops, diagnostics);
            var controller = CreateController(new RecordingSwitchOutput(), diagnostics, guard);

            Assert.False(controller.TryApply(9, SwitchPosition.Straight, out _));
            Assert.Contains(diagnostics.Recent, d => d.Code == DiagnosticCode.UnmappedAddress);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostics.Latest!.Severity);
            Assert.Empty(stops.StoppedLocos);
        }

        [Fact]
        public void UnavailableRealSwitchOutput_IsReportedAsUnknownNotAsSuccess()
        {
            var diagnostics = new ControlDiagnostics();
            var guard = new ControlSafetyGuard(new RecordingStopSink(), diagnostics);
            var output = new RecordingSwitchOutput { IsAvailable = false };
            var controller = CreateController(output, diagnostics, guard);

            Assert.False(controller.TryApply(1, SwitchPosition.Straight, out _));

            // Reported once as a warning, never as a confirmation, and it is not a stop fault.
            Assert.Single(diagnostics.Recent);
            Assert.Equal(DiagnosticCode.StateUnknown, diagnostics.Latest!.Code);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostics.Latest.Severity);
            Assert.False(diagnostics.IsUnsafe);

            // Repeated commands must not flood the diagnostics buffer.
            controller.TryApply(1, SwitchPosition.Diverging, out _);
            Assert.Single(diagnostics.Recent);
        }

        [Fact]
        public void BackendRejectingTheCommand_IsReportedAndNotCountedAsApplied()
        {
            var diagnostics = new ControlDiagnostics();
            var guard = new ControlSafetyGuard(new RecordingStopSink(), diagnostics);
            var output = new RecordingSwitchOutput { Applied = false };
            var controller = CreateController(output, diagnostics, guard);

            Assert.False(controller.TryApply(1, SwitchPosition.Straight, out _));
            Assert.Equal(DiagnosticCode.CommandNotApplied, diagnostics.Latest!.Code);
            Assert.Equal(DiagnosticSeverity.Rejected, diagnostics.Latest.Severity);
            Assert.Empty(controller.GetLogicalPositions());
        }

        [Fact]
        public void CommandedVersusObservedMismatch_StopsAndReports()
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink();
            var guard = new ControlSafetyGuard(stops, diagnostics);
            var observer = new FixedObserver { Observed = SwitchPosition.Diverging };
            var controller = CreateController(
                new RecordingSwitchOutput(),
                diagnostics,
                guard,
                observer,
                new ModeObservability { SwitchFeedbackAvailable = true, OccupancyAvailable = true });

            // Commanded straight, but the simulator reports diverging.
            Assert.True(controller.TryApply(1, SwitchPosition.Straight, out _));

            Assert.Equal(DiagnosticCode.CommandedObservedMismatch, diagnostics.Latest!.Code);
            Assert.Equal(DiagnosticSeverity.StopRequired, diagnostics.Latest.Severity);
            Assert.Equal(SafetyAction.StopLayout, diagnostics.Latest.SafetyAction);
        }

        [Fact]
        public void ObservedMatch_ReportsNoMismatch()
        {
            var diagnostics = new ControlDiagnostics();
            var guard = new ControlSafetyGuard(new RecordingStopSink(), diagnostics);
            var observer = new FixedObserver { Observed = SwitchPosition.Straight };
            var controller = CreateController(
                new RecordingSwitchOutput(),
                diagnostics,
                guard,
                observer,
                new ModeObservability { SwitchFeedbackAvailable = true, OccupancyAvailable = true });

            Assert.True(controller.TryApply(1, SwitchPosition.Straight, out _));
            Assert.Empty(diagnostics.Recent);
        }

        [Fact]
        public void RealModeWithoutFeedback_DoesNotFabricateAnObservedMismatch()
        {
            var diagnostics = new ControlDiagnostics();
            var guard = new ControlSafetyGuard(new RecordingStopSink(), diagnostics);
            var observer = new FixedObserver { Observed = SwitchPosition.Diverging };
            var controller = CreateController(
                new RecordingSwitchOutput(),
                diagnostics,
                guard,
                observer,
                new ModeObservability { SwitchFeedbackAvailable = false, OccupancyAvailable = false });

            Assert.True(controller.TryApply(1, SwitchPosition.Straight, out _));

            // Feedback is unavailable by design, so no mismatch may be claimed.
            Assert.Empty(diagnostics.Recent);
        }
    }
}
