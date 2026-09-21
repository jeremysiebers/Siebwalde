using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using SiebwaldeApp.StopReachabilityHarness;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Focused software tests for the clean-live strongest-emergency harness trigger.
    ///
    /// They prove that the harness command reaches the production strongest-emergency/layout
    /// neutralization abstraction (<see cref="ISafetyStopSink.StopLayout"/>), performs no synthetic
    /// detection seeding, and propagates the production <see cref="SafetyStopResult"/>. The
    /// production target set, the backplane/configuration exclusion and the neutral-only write are
    /// exercised through the real <see cref="EcosHardwareStopSink"/> and
    /// <see cref="TrackAmplifierHardwareBackend"/>, never through a harness-side filter.
    ///
    /// No hardware is touched.
    /// </summary>
    public class StrongestEmergencyHarnessTests
    {
        private const int Neutral = AmplifierSpeedMapper.NeutralPwm;

        /// <summary>A sink that records the production call and returns a configurable result.</summary>
        private sealed class RecordingSink : ISafetyStopSink
        {
            public int StopLayoutCalls { get; private set; }

            public SafetyStopResult LayoutResult { get; set; } =
                SafetyStopResult.Commanded(Array.Empty<ushort>());

            public SafetyStopResult StopLoco(int address)
                => throw new InvalidOperationException(
                    "The strongest-emergency trigger must only invoke the production layout neutralization.");

            public SafetyStopResult StopLayout()
            {
                StopLayoutCalls++;
                return LayoutResult;
            }
        }

        private sealed class NoBlockProvider : IBlockPositionProvider
        {
            public event Action<int, int>? BlockEntered
            {
                add { }
                remove { }
            }

            public int? TryGetBlockForLoc(int loc) => null;
        }

        private static void Seed(TrackApplicationVariables variables, ushort amplifier)
        {
            var item = variables.trackAmpItems.First(a => a.SlaveNumber == amplifier);
            item.SlaveDetected = 1;
            item.LastDataReceivedUtc = DateTimeOffset.UtcNow;
        }

        private static void SeedStale(TrackApplicationVariables variables, ushort amplifier)
        {
            var item = variables.trackAmpItems.First(a => a.SlaveNumber == amplifier);
            item.SlaveDetected = 1;
            item.LastDataReceivedUtc = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(1);
        }

        private static string[] Trace(ControlTraceFormattingTests.CaptureLogFactory factory)
            => factory.Records
                .Where(r => r.Instance == ControlTraceLogger.LoggerInstance)
                .Select(r => r.Message)
                .ToArray();

        private static string DetectionSnapshot(TrackApplicationVariables variables)
            => string.Join(
                ",",
                variables.trackAmpItems
                    .OrderBy(a => a.SlaveNumber)
                    .Select(a => $"{a.SlaveNumber}:{a.SlaveDetected}"));

        private static EcosHardwareStopSink BuildProductionSink(
            TrackApplicationVariables variables,
            out TrackAmplifierHardwareBackend backend,
            out AmplifierCommandTracker tracker,
            IControlTrace? trace = null)
        {
            tracker = new AmplifierCommandTracker(trace);
            backend = new TrackAmplifierHardwareBackend(
                new NoBlockProvider(),
                BlockTopology.Parse("amps: 1:1"),
                variables,
                commandTracker: tracker,
                trace: trace);
            return new EcosHardwareStopSink(null, trace)
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };
        }

        // -----------------------------------------------------------------
        // 1. The command reaches the production strongest-emergency path.
        // 6. The production SafetyStopResult is propagated unchanged.
        // -----------------------------------------------------------------

        [Fact]
        public void Trigger_InvokesProductionStopLayout_AndPropagatesTheResult()
        {
            var expected = SafetyStopResult.Partial(new ushort[] { 1 }, new ushort[] { 3 });
            var sink = new RecordingSink { LayoutResult = expected };

            var result = StrongestEmergencyTrigger.Invoke(sink);

            Assert.Equal(1, sink.StopLayoutCalls);
            Assert.Same(expected, result);
        }

        // -----------------------------------------------------------------
        // 2. No synthetic detection seeding by the new command.
        // -----------------------------------------------------------------

        [Fact]
        public void Trigger_DoesNotSeedOrMutateDetectedInventory()
        {
            var variables = new TrackApplicationVariables();

            // Test fixture inventory only: a detected unmapped track amplifier and detected
            // backplane/configuration slaves. The trigger itself has no inventory access.
            Seed(variables, 6);
            Seed(variables, 51);
            Seed(variables, 52);
            Seed(variables, 55);

            var sink = BuildProductionSink(variables, out var backend, out _);
            var knownBefore = backend.GetKnownPhysicalAmplifiers();
            var detectionBefore = DetectionSnapshot(variables);

            StrongestEmergencyTrigger.Invoke(sink);

            var knownAfter = backend.GetKnownPhysicalAmplifiers();
            var detectionAfter = DetectionSnapshot(variables);

            Assert.Equal(detectionBefore, detectionAfter);
            Assert.Equal(knownBefore, knownAfter);

            // The production inventory already excludes the backplane class and includes the
            // detected unmapped amplifier; the trigger added nothing.
            Assert.Contains((ushort)6, knownAfter);
            Assert.DoesNotContain((ushort)51, knownAfter);
            Assert.DoesNotContain((ushort)52, knownAfter);
            Assert.DoesNotContain((ushort)55, knownAfter);
        }

        // -----------------------------------------------------------------
        // 3. Backplane exclusion and unmapped inclusion are production-owned.
        // 5. The new command produces only neutral HR0 semantics.
        // -----------------------------------------------------------------

        [Fact]
        public void ProductionStrongestStop_ExcludesBackplane_IncludesUnmapped_AndWritesOnlyNeutral()
        {
            var factory = new ControlTraceFormattingTests.CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);

            var variables = new TrackApplicationVariables();
            Seed(variables, 1);   // mapped in BlockTopology
            Seed(variables, 6);   // detected, unmapped (the installed prototype amplifier)
            Seed(variables, 50);  // detected, unmapped
            Seed(variables, 51);  // backplane/configuration
            Seed(variables, 52);  // backplane/configuration
            Seed(variables, 55);  // backplane/configuration

            var sink = BuildProductionSink(variables, out _, out _, trace);

            var result = StrongestEmergencyTrigger.Invoke(sink);

            Assert.True(result.Succeeded);
            Assert.Contains((ushort)1, result.CommandedAmplifiers);
            Assert.Contains((ushort)6, result.CommandedAmplifiers);
            Assert.Contains((ushort)50, result.CommandedAmplifiers);
            Assert.DoesNotContain((ushort)51, result.CommandedAmplifiers);
            Assert.DoesNotContain((ushort)52, result.CommandedAmplifiers);
            Assert.DoesNotContain((ushort)55, result.CommandedAmplifiers);

            // No HR0/PWM write at all for the backplane/configuration class.
            Assert.False(variables.PendingWrites.ContainsKey(51));
            Assert.False(variables.PendingWrites.ContainsKey(52));
            Assert.False(variables.PendingWrites.ContainsKey(55));

            // Every queued command for a legitimate track amplifier is the neutral setpoint; the
            // command cannot produce a non-neutral value.
            foreach (var amplifier in result.CommandedAmplifiers)
            {
                Assert.Equal(Neutral, variables.PendingWrites[amplifier].Hr0Value & 0x03FF);
            }

            var payloads = Trace(factory);

            // The authoritative production target set already excludes 51..55 and includes the
            // unmapped detected amplifiers.
            Assert.Contains(
                payloads,
                p => p.StartsWith("EVENT=EMERGENCY_TARGET_SET ") &&
                     p.Contains(" targets=1,6,50") &&
                     p.Contains(" excluded=51-55"));

            Assert.Contains(
                payloads,
                p => p.StartsWith("EVENT=AMPLIFIER_COMMAND ") &&
                     p.Contains(" amp=6") &&
                     p.Contains(" purpose=EmergencyNeutral") &&
                     p.Contains(" pwm=399") &&
                     p.Contains(" source=Safety"));

            Assert.Contains(
                payloads,
                p => p.StartsWith("EVENT=SAFETY_STOP_RESULT ") &&
                     p.Contains(" scope=Layout") &&
                     p.Contains(" succeeded=true"));

            // The backplane class never appears as a trace target or command.
            Assert.DoesNotContain(payloads, p => p.Contains("amp=51") || p.Contains("amp=52") || p.Contains("amp=55"));
        }

        // -----------------------------------------------------------------
        // 6. Result propagation through the real production sink.
        // -----------------------------------------------------------------

        [Fact]
        public void ProductionStrongestStop_PropagatesFailedRequiredTargets()
        {
            var variables = new TrackApplicationVariables();
            Seed(variables, 1);
            SeedStale(variables, 3);

            var sink = BuildProductionSink(variables, out _, out _);

            var result = StrongestEmergencyTrigger.Invoke(sink);

            Assert.False(result.Succeeded);
            Assert.Contains((ushort)1, result.CommandedAmplifiers);
            Assert.Contains((ushort)3, result.FailedAmplifiers);
        }
    }
}
