using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Integration-style software test for the production control trace: it drives the real
    /// <see cref="SimpleEcosBackend"/>, <see cref="TrackAmplifierHardwareBackend"/>,
    /// <see cref="AmplifierCommandTracker"/>, <see cref="EcosHardwareStopSink"/> and
    /// <see cref="ControlSafetyGuard"/> with an in-memory trace and verifies that the causal
    /// sequence is reconstructable from the emitted events. No hardware is involved.
    /// </summary>
    public class ControlTraceCausalSequenceTests
    {
        private sealed class MutableBlockProvider : IBlockPositionProvider
        {
            private readonly Dictionary<int, int> _blocks = new();

            public event Action<int, int>? BlockEntered;

            public int? TryGetBlockForLoc(int loc)
                => _blocks.TryGetValue(loc, out var block) ? block : (int?)null;

            public void EnterBlock(int loco, int block)
            {
                _blocks[loco] = block;
                BlockEntered?.Invoke(loco, block);
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

            public SafetyStopResult StopLoco(int address) => _locoResult;

            public SafetyStopResult StopLayout() => _layoutResult;
        }

        private static void Seed(TrackApplicationVariables variables, ushort amplifier)
        {
            var item = variables.trackAmpItems.First(a => a.SlaveNumber == amplifier);
            item.SlaveDetected = 1;
            item.LastDataReceivedUtc = DateTimeOffset.UtcNow;
        }

        private static async Task<string> SendAsync(SimpleEcosBackend backend, string raw)
        {
            using var writer = new StringWriter();
            var command = new SimpleEcosCommandParser().Parse(raw);
            await backend.HandleAsync(command!, writer, CancellationToken.None);
            return writer.ToString();
        }

        private static string[] Trace(ControlTraceFormattingTests.CaptureLogFactory factory)
            => factory.Records
                .Where(r => r.Instance == ControlTraceLogger.LoggerInstance)
                .Select(r => r.Message)
                .ToArray();

        [Fact]
        public async Task RepresentativeSequence_EmitsTheCausalEvents()
        {
            var factory = new ControlTraceFormattingTests.CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);

            var variables = new TrackApplicationVariables();
            var blocks = new MutableBlockProvider();
            var tracker = new AmplifierCommandTracker(trace);
            var topology = BlockTopology.Parse("amps: 1:1,3:3");
            var backend = new TrackAmplifierHardwareBackend(
                blocks,
                topology,
                variables,
                lookAheadPlanner: new LookAheadPlanner(topology),
                occupancyProvider: new FakeOccupancy(),
                commandTracker: tracker,
                groups: TrackAmplifierGroups.Create(new[] { 1, 3 }, null, null),
                trace: trace);
            var diagnostics = new ControlDiagnostics();
            var sink = new EcosHardwareStopSink(null, trace)
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };
            var guard = new ControlSafetyGuard(sink, diagnostics, null, trace);

            Seed(variables, 1);
            Seed(variables, 3);

            var locoPath = Path.Combine(Path.GetTempPath(), $"trace-loco-{Guid.NewGuid():N}.json");
            try
            {
                var repository = new JsonLocoRepository(locoPath);
                await repository.LoadAsync();
                repository.AddOrUpdate(new LocoInfo { EcosId = 1000, Address = 2, Protocol = "DCC28" });

                var ecos = new SimpleEcosBackend(backend, repository, blocks, trace);

                // Incoming command, block 1, low speed, A -> B transition, low speed again.
                await SendAsync(ecos, "set(1000,dir[0],speedstep[0])");
                blocks.EnterBlock(2, 1);
                await SendAsync(ecos, "set(1000,speedstep[1])");
                blocks.EnterBlock(2, 3);
                await SendAsync(ecos, "set(1000,speedstep[1])");

                var fault = new ControlDiagnostic
                {
                    Code = DiagnosticCode.OccupancyMismatch,
                    Severity = DiagnosticSeverity.StopRequired,
                    Subject = "block 3",
                    LocoAddress = 2,
                    Block = 3,
                    Detail = "causal sequence test"
                };

                var action = guard.Apply(fault);
                Assert.Equal(SafetyAction.StopLoco, action);

                var payloads = Trace(factory);

                // Incoming command / normalization boundary.
                Assert.Contains(payloads, p => p.StartsWith("EVENT=ECOS_COMMAND "));
                Assert.Contains(
                    payloads,
                    p => p.StartsWith("EVENT=SPEED_DECISION ") && p.Contains(" protocol=DCC28") &&
                         p.Contains(" raw=1") && p.Contains(" normalized=5"));

                // Logical block transition with the previous block.
                Assert.Contains(
                    payloads,
                    p => p.StartsWith("EVENT=BLOCK_TRANSITION ") && p.Contains(" previous=1 block=3"));

                // Physical target decisions and retained-target bookkeeping.
                Assert.Contains(
                    payloads,
                    p => p.StartsWith("EVENT=AMPLIFIER_COMMAND ") && p.Contains(" amp=1") &&
                         p.Contains(" purpose=Movement") && p.Contains(" pwm=416"));
                Assert.Contains(payloads, p => p.StartsWith("EVENT=TRACKER_ADD ") && p.Contains(" amp=1"));
                Assert.Contains(payloads, p => p.StartsWith("EVENT=TRACKER_ADD ") && p.Contains(" amp=3"));

                // Safety stop and its command-level result, with the pre-stop retained set.
                Assert.Contains(
                    payloads,
                    p => p.StartsWith("EVENT=SAFETY_STOP ") && p.Contains(" scope=Loco") && p.Contains(" loco=2"));
                Assert.Contains(
                    payloads,
                    p => p.StartsWith("EVENT=SAFETY_STOP_RESULT ") && p.Contains(" succeeded=true") &&
                         p.Contains(" retained=1,3"));

                // The vacated (amp 1) and the current (amp 3) physical outputs are both neutralized.
                Assert.Contains(
                    payloads,
                    p => p.StartsWith("EVENT=AMPLIFIER_COMMAND ") && p.Contains(" amp=1") && p.Contains(" pwm=399"));
                Assert.Contains(
                    payloads,
                    p => p.StartsWith("EVENT=AMPLIFIER_COMMAND ") && p.Contains(" amp=3") && p.Contains(" pwm=399"));
                Assert.Contains(payloads, p => p.StartsWith("EVENT=TRACKER_REMOVE ") && p.Contains(" amp=1"));
                Assert.Contains(payloads, p => p.StartsWith("EVENT=TRACKER_REMOVE ") && p.Contains(" amp=3"));

                // Chronology: the transition must be recorded before the safety stop.
                Assert.True(
                    Array.FindIndex(payloads, p => p.Contains("previous=1 block=3")) <
                    Array.FindIndex(payloads, p => p.StartsWith("EVENT=SAFETY_STOP ")));
            }
            finally
            {
                try { File.Delete(locoPath); } catch { /* temp cleanup */ }
            }
        }

        [Fact]
        public void Escalation_EmitsRequestedAndEstablishedDistinctly()
        {
            var factory = new ControlTraceFormattingTests.CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink(
                SafetyStopResult.Partial(Array.Empty<ushort>(), new ushort[] { 1 }),
                SafetyStopResult.Commanded(new ushort[] { 1, 3 }));
            var guard = new ControlSafetyGuard(stops, diagnostics, null, trace);

            var action = guard.Apply(Fault(7));

            Assert.Equal(SafetyAction.StopLayoutEscalated, action);

            var payloads = Trace(factory);
            Assert.Contains(
                payloads,
                p => p.StartsWith("EVENT=SAFETY_STOP ") && p.Contains(" scope=Loco") && p.Contains(" loco=7"));
            Assert.Contains(
                payloads,
                p => p.StartsWith("EVENT=SAFETY_ESCALATION ") && p.Contains(" result=Requested"));
            Assert.Contains(
                payloads,
                p => p.StartsWith("EVENT=SAFETY_ESCALATION ") && p.Contains(" result=Established"));
        }

        [Fact]
        public void Escalation_WhenIncomplete_IsNotReportedAsEstablished()
        {
            var factory = new ControlTraceFormattingTests.CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);
            var diagnostics = new ControlDiagnostics();
            var stops = new RecordingStopSink(
                SafetyStopResult.Partial(Array.Empty<ushort>(), new ushort[] { 1 }),
                SafetyStopResult.Partial(Array.Empty<ushort>(), new ushort[] { 1 }));
            var guard = new ControlSafetyGuard(stops, diagnostics, null, trace);

            guard.Apply(Fault(7));

            var payloads = Trace(factory);
            Assert.Contains(
                payloads,
                p => p.StartsWith("EVENT=SAFETY_ESCALATION ") && p.Contains(" result=Requested"));
            Assert.Contains(
                payloads,
                p => p.StartsWith("EVENT=SAFETY_ESCALATION ") && p.Contains(" result=Incomplete"));
            Assert.DoesNotContain(
                payloads,
                p => p.StartsWith("EVENT=SAFETY_ESCALATION ") && p.Contains(" result=Established"));
        }

        private static ControlDiagnostic Fault(int loco) => new()
        {
            Code = DiagnosticCode.OccupancyMismatch,
            Severity = DiagnosticSeverity.StopRequired,
            Subject = "block 3",
            LocoAddress = loco,
            Block = 3,
            Detail = "escalation test"
        };
    }
}
