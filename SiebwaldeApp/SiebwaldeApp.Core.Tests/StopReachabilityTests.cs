using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Software-only regression tests for the confirmed physical stop-reachability defect.
    ///
    /// A locomotive whose block mapping changed (or disappeared) must still have its previously
    /// commanded physical amplifiers neutralized by a safety stop, and an incomplete stop must
    /// never be reported as success. These tests use the real <see cref="TrackAmplifierHardwareBackend"/>,
    /// <see cref="AmplifierCommandTracker"/>, <see cref="EcosHardwareStopSink"/> and
    /// <see cref="ControlSafetyGuard"/>; only the block-location source and the failure injection
    /// are substituted. No hardware is touched.
    /// </summary>
    public class StopReachabilityTests
    {
        private static readonly int Neutral = AmplifierSpeedMapper.NeutralPwm;

        private sealed class MutableBlockPositionProvider : IBlockPositionProvider
        {
            private readonly Dictionary<int, int> _blocks = new();

            // Explicit accessors: this provider never raises the event, and this avoids CS0067.
            public event Action<int, int>? BlockEntered
            {
                add { }
                remove { }
            }

            public int? TryGetBlockForLoc(int loc)
                => _blocks.TryGetValue(loc, out var block) ? block : (int?)null;

            public void Set(int loco, int? block)
            {
                if (block is int value)
                {
                    _blocks[loco] = value;
                }
                else
                {
                    _blocks.Remove(loco);
                }
            }
        }

        private sealed class FakeOccupancy : IOccupancyProvider
        {
            public bool IsBlockOccupied(int block) => false;

            public bool IsBlockOccupancyKnown(int block) => true;
        }

        private sealed class RecordingStopSink : ISafetyStopSink
        {
            private readonly SafetyStopResult _locoResult;
            private readonly SafetyStopResult _layoutResult;

            public RecordingStopSink(SafetyStopResult locoResult, SafetyStopResult layoutResult)
            {
                _locoResult = locoResult;
                _layoutResult = layoutResult;
            }

            public int LocoStops { get; private set; }
            public int LayoutStops { get; private set; }

            public SafetyStopResult StopLoco(int address)
            {
                LocoStops++;
                return _locoResult;
            }

            public SafetyStopResult StopLayout()
            {
                LayoutStops++;
                return _layoutResult;
            }
        }

        /// <summary>A neutralizer that reports a configurable subset of amplifiers as not accepted.</summary>
        private sealed class FailingNeutralizer : IAmplifierNeutralizer
        {
            private readonly ushort[] _known;
            private readonly HashSet<ushort> _fail;

            public FailingNeutralizer(IEnumerable<ushort> known, IEnumerable<ushort> fail)
            {
                _known = known.ToArray();
                _fail = new HashSet<ushort>(fail);
            }

            public List<ushort[]> Requests { get; } = new();

            public IReadOnlyList<ushort> GetKnownPhysicalAmplifiers() => _known;

            public IReadOnlyList<ushort> NeutralizeAmplifiers(IReadOnlyCollection<ushort> amplifiers)
            {
                Requests.Add(amplifiers.ToArray());
                return amplifiers.Where(a => _fail.Contains(a)).ToArray();
            }

            // The fake reports every legitimate track amplifier as fresh; the physical
            // classification is still the real one.
            public AmplifierCommunicationState GetAmplifierCommunicationState(ushort amplifier)
                => TrackAmplifierAddress.IsTrackAmplifierAddress(amplifier)
                    ? AmplifierCommunicationState.Fresh
                    : AmplifierCommunicationState.Invalid;
        }

        private static ControlDiagnostic LocoFault(int loco) => new()
        {
            Code = DiagnosticCode.OccupancyMismatch,
            Severity = DiagnosticSeverity.StopRequired,
            Subject = $"block 3",
            LocoAddress = loco,
            Block = 3,
            Detail = "test loco-scoped safety fault"
        };

        private static ushort Pwm(TrackApplicationVariables variables, ushort amplifier)
            => (ushort)(variables.PendingWrites[amplifier].Hr0Value & 0x03FF);

        /// <summary>Marks a physical track amplifier as detected with fresh data.</summary>
        private static void Seed(TrackApplicationVariables variables, ushort amplifier)
        {
            var item = variables.trackAmpItems.First(a => a.SlaveNumber == amplifier);
            item.SlaveDetected = 1;
            item.LastDataReceivedUtc = DateTimeOffset.UtcNow;
        }

        // -----------------------------------------------------------------
        // A -> B orphaned actuator
        // -----------------------------------------------------------------

        [Fact]
        public void LocoStop_AfterBlockTransition_NeutralizesVacatedAndCurrentAmplifiers()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var blocks = new MutableBlockPositionProvider();
            var backend = new TrackAmplifierHardwareBackend(
                blocks, BlockTopology.Parse("amps: 1:1,2:2,3:3"), variables, commandTracker: tracker);
            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            Seed(variables, 1);
            Seed(variables, 3);

            // Loco drives in block 1 (amp 1 non-neutral), then logically moves to block 3.
            blocks.Set(7, 1);
            Assert.True(backend.SetLocoSpeed(7, 1, 0));
            Assert.Equal(new ushort[] { 1 }, tracker.GetOutstanding(7));

            blocks.Set(7, 3);
            Assert.True(backend.SetLocoSpeed(7, 1, 0));
            Assert.Equal(new ushort[] { 1, 3 }, tracker.GetOutstanding(7));
            Assert.Equal(AmplifierSpeedMapper.ToPwm(1, 0), Pwm(variables, 1) & 0x03FF);

            var result = sink.StopLoco(7);

            Assert.True(result.Succeeded);
            Assert.Equal(Neutral, Pwm(variables, 1));
            Assert.Equal(Neutral, Pwm(variables, 3));
            Assert.Empty(tracker.GetOutstanding(7));
        }

        [Fact]
        public void LocoStop_ReachesRetainedTarget_WhenCurrentBlockHasNoMapping()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var blocks = new MutableBlockPositionProvider();
            var backend = new TrackAmplifierHardwareBackend(
                blocks, BlockTopology.Parse("amps: 1:1,2:2,3:3"), variables, commandTracker: tracker);
            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            Seed(variables, 1);

            blocks.Set(7, 1);
            Assert.True(backend.SetLocoSpeed(7, 1, 0));

            // The loco is now in a block with no amplifier mapping.
            blocks.Set(7, 9);

            var result = sink.StopLoco(7);

            Assert.True(result.Succeeded);
            Assert.Equal(Neutral, Pwm(variables, 1));
            Assert.Empty(tracker.GetOutstanding(7));
        }

        [Fact]
        public void LocoStop_ReachesRetainedTarget_WhenBlockMappingDisappearsEntirely()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var blocks = new MutableBlockPositionProvider();
            var backend = new TrackAmplifierHardwareBackend(
                blocks, BlockTopology.Parse("amps: 1:1,2:2,3:3"), variables, commandTracker: tracker);
            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            Seed(variables, 1);

            blocks.Set(7, 1);
            Assert.True(backend.SetLocoSpeed(7, 1, 0));

            blocks.Set(7, null);

            var result = sink.StopLoco(7);

            Assert.True(result.Succeeded);
            Assert.Equal(Neutral, Pwm(variables, 1));
        }

        // -----------------------------------------------------------------
        // Multiple outstanding targets (look-ahead)
        // -----------------------------------------------------------------

        [Fact]
        public void LocoStop_NeutralizesMultipleOutstandingTargets_WhenMappingIsGone()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var blocks = new MutableBlockPositionProvider();
            var topology = BlockTopology.Parse("amps: 1:1,2:2 ; routes: 1>2");
            var backend = new TrackAmplifierHardwareBackend(
                blocks,
                topology,
                variables,
                lookAheadPlanner: new LookAheadPlanner(topology),
                occupancyProvider: new FakeOccupancy(),
                commandTracker: tracker);
            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            Seed(variables, 1);
            Seed(variables, 2);

            blocks.Set(7, 1);
            Assert.True(backend.SetLocoSpeed(7, 1, 0));

            // Current amplifier 1 plus look-ahead amplifier 2 are both non-neutral.
            Assert.Equal(new ushort[] { 1, 2 }, tracker.GetOutstanding(7));

            // The current mapping disappears, so only the retained targets can be reached.
            blocks.Set(7, null);

            var result = sink.StopLoco(7);

            Assert.True(result.Succeeded);
            Assert.Equal(Neutral, Pwm(variables, 1));
            Assert.Equal(Neutral, Pwm(variables, 2));
            Assert.Empty(tracker.GetOutstanding(7));
        }

        // -----------------------------------------------------------------
        // Failure reporting
        // -----------------------------------------------------------------

        [Fact]
        public void LocoStop_WithUnreachableRequiredAmplifier_IsNotReportedAsSuccess()
        {
            var tracker = new AmplifierCommandTracker();
            tracker.RecordNonNeutral(7, 1);

            var sink = new EcosHardwareStopSink
            {
                Neutralizer = new FailingNeutralizer(new ushort[] { 1 }, new ushort[] { 1 }),
                CommandTracker = tracker
            };

            var result = sink.StopLoco(7);

            Assert.False(result.Succeeded);
            Assert.Equal(new ushort[] { 1 }, result.FailedAmplifiers);
        }

        [Fact]
        public void LocoStop_WithoutAnyBackend_IsNotReportedAsSuccess()
        {
            var tracker = new AmplifierCommandTracker();
            tracker.RecordNonNeutral(7, 1);
            var sink = new EcosHardwareStopSink { CommandTracker = tracker };

            var result = sink.StopLoco(7);

            Assert.False(result.Succeeded);
            Assert.True(result.BackendUnavailable);
        }

        [Fact]
        public void LayoutStop_WhenNeutralizationCannotBeDelivered_IsNotReportedAsSuccess()
        {
            var tracker = new AmplifierCommandTracker();
            tracker.RecordNonNeutral(7, 1);

            var sink = new EcosHardwareStopSink
            {
                Neutralizer = new FailingNeutralizer(new ushort[] { 1, 2 }, new ushort[] { 1, 2 }),
                CommandTracker = tracker
            };

            var result = sink.StopLayout();

            Assert.False(result.Succeeded);
            Assert.Equal(new ushort[] { 1, 2 }, result.FailedAmplifiers);
        }

        // -----------------------------------------------------------------
        // ControlSafetyGuard escalation
        // -----------------------------------------------------------------

        [Fact]
        public void Guard_EscalatesToLayoutNeutralization_WhenLocoStopFails()
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink(
                SafetyStopResult.Partial(Array.Empty<ushort>(), new ushort[] { 1 }),
                SafetyStopResult.Commanded(new ushort[] { 1, 3 }));
            var guard = new ControlSafetyGuard(stops, diagnostics);

            var action = guard.Apply(LocoFault(7));

            Assert.Equal(SafetyAction.StopLayoutEscalated, action);
            Assert.Equal(1, stops.LayoutStops);
            Assert.Contains(diagnostics.Recent, d => d.Code == DiagnosticCode.CommandNotApplied);
            Assert.True(guard.IsLatched);
        }

        [Fact]
        public void Guard_WhenNeutralizationCannotBeDelivered_ReportsFailureAndKeepsTheLatch()
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink(
                SafetyStopResult.Partial(Array.Empty<ushort>(), new ushort[] { 1 }),
                SafetyStopResult.Partial(Array.Empty<ushort>(), new ushort[] { 1 }));
            var guard = new ControlSafetyGuard(stops, diagnostics);

            var action = guard.Apply(LocoFault(7));

            Assert.Equal(SafetyAction.StopLayoutEscalated, action);
            Assert.True(guard.IsLatched);
            Assert.False(guard.AllowsMovement(7));

            // Both the loco-scoped failure and the failed escalation are reported.
            Assert.Equal(2, diagnostics.Recent.Count(d => d.Code == DiagnosticCode.CommandNotApplied));
        }

        [Fact]
        public void Guard_WhenNoBackendExists_ReportsBackendUnavailableAndKeepsTheLatch()
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink(
                SafetyStopResult.Unavailable(),
                SafetyStopResult.Unavailable());
            var guard = new ControlSafetyGuard(stops, diagnostics);

            var action = guard.Apply(LocoFault(7));

            Assert.Equal(SafetyAction.StopLayoutEscalated, action);
            Assert.True(guard.IsLatched);
            Assert.Contains(diagnostics.Recent, d => d.Code == DiagnosticCode.BackendUnavailable);
        }

        [Fact]
        public void Guard_DoesNotClearTheLatchBecauseAStopWasAttempted()
        {
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink(
                SafetyStopResult.NotApplied(),
                SafetyStopResult.NotApplied());
            var guard = new ControlSafetyGuard(stops, diagnostics);

            guard.Apply(LocoFault(7));

            Assert.True(guard.IsLatched);
            Assert.False(guard.AllowsMovement(7));
        }

        // -----------------------------------------------------------------
        // Detected-but-unmapped amplifier coverage
        // -----------------------------------------------------------------

        [Fact]
        public void LayoutStop_IncludesDetectedButUnmappedAmplifier()
        {
            var variables = new TrackApplicationVariables();
            variables.trackAmpItems[6].SlaveDetected = 1;
            variables.trackAmpItems[6].LastDataReceivedUtc = DateTimeOffset.UtcNow;

            var tracker = new AmplifierCommandTracker();
            var backend = new TrackAmplifierHardwareBackend(
                new MutableBlockPositionProvider(),
                BlockTopology.Parse("amps: 1:1,2:2,3:3"),
                variables,
                commandTracker: tracker);

            var known = backend.GetKnownPhysicalAmplifiers();
            Assert.Contains((ushort)6, known);

            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            var result = sink.StopLayout();

            Assert.True(result.Succeeded);
            Assert.Equal(Neutral, Pwm(variables, 6));
            Assert.Equal(Neutral, Pwm(variables, 1));
            Assert.Equal(Neutral, Pwm(variables, 3));
        }

        [Fact]
        public void LayoutStop_NeutralizesRetainedTargets_WhenInventoryIsEmpty()
        {
            var tracker = new AmplifierCommandTracker();
            tracker.RecordNonNeutral(7, 4);

            var neutralizer = new FailingNeutralizer(Array.Empty<ushort>(), Array.Empty<ushort>());
            var sink = new EcosHardwareStopSink
            {
                Neutralizer = neutralizer,
                CommandTracker = tracker
            };

            var result = sink.StopLayout();

            Assert.True(result.Succeeded);
            Assert.Contains(neutralizer.Requests, r => r.Contains((ushort)4));
            Assert.Empty(tracker.GetAllOutstanding());
        }

        // -----------------------------------------------------------------
        // Multiple locomotives
        // -----------------------------------------------------------------

        [Fact]
        public void LocoStop_DoesNotNeutralizeOrClearAnotherLocosTrackedTarget()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var blocks = new MutableBlockPositionProvider();
            var backend = new TrackAmplifierHardwareBackend(
                blocks, BlockTopology.Parse("amps: 1:1,2:2,3:3"), variables, commandTracker: tracker);
            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            Seed(variables, 1);
            Seed(variables, 3);

            blocks.Set(7, 1);
            Assert.True(backend.SetLocoSpeed(7, 1, 0));

            blocks.Set(8, 3);
            Assert.True(backend.SetLocoSpeed(8, 1, 0));

            var result = sink.StopLoco(7);

            Assert.True(result.Succeeded);
            Assert.Equal(Neutral, Pwm(variables, 1));

            // Loco 8's target is untouched, and its ownership is not cleared by loco 7's stop.
            Assert.Equal(AmplifierSpeedMapper.ToPwm(1, 0), Pwm(variables, 3) & 0x03FF);
            Assert.Equal(new ushort[] { 3 }, tracker.GetOutstanding(8));
        }

        [Fact]
        public void OwnershipTransfers_WhenAnotherLocoCommandsTheSameAmplifier()
        {
            var tracker = new AmplifierCommandTracker();

            tracker.RecordNonNeutral(7, 1);
            Assert.Equal(new ushort[] { 1 }, tracker.GetOutstanding(7));

            // Loco 8 now drives the same amplifier: it owns the physical command.
            tracker.RecordNonNeutral(8, 1);

            Assert.Empty(tracker.GetOutstanding(7));
            Assert.Equal(new ushort[] { 1 }, tracker.GetOutstanding(8));
        }

        [Fact]
        public void RecordNeutralGlobally_ClearsEveryLocosOwnership()
        {
            var tracker = new AmplifierCommandTracker();
            tracker.RecordNonNeutral(7, 1);
            tracker.RecordNonNeutral(8, 2);

            tracker.RecordNeutralGlobally(new ushort[] { 1 });

            Assert.Empty(tracker.GetOutstanding(7));
            Assert.Equal(new ushort[] { 2 }, tracker.GetOutstanding(8));
        }

        // -----------------------------------------------------------------
        // Normal stop
        // -----------------------------------------------------------------

        [Fact]
        public void NormalLocoStop_RemainsSuccessful()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var blocks = new MutableBlockPositionProvider();
            var backend = new TrackAmplifierHardwareBackend(
                blocks, BlockTopology.Parse("amps: 1:1"), variables, commandTracker: tracker);
            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            Seed(variables, 1);

            blocks.Set(7, 1);
            Assert.True(backend.SetLocoSpeed(7, 1, 0));

            var result = sink.StopLoco(7);

            Assert.True(result.Succeeded);
            Assert.Equal(Neutral, Pwm(variables, 1));
            Assert.Empty(tracker.GetOutstanding(7));
        }
    }
}
