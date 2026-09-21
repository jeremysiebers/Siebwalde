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
    /// End-to-end regression tests for the confirmed DCC28 speed-scaling defect. Koploper sends
    /// <c>set(&lt;id&gt;, speedstep[&lt;n&gt;])</c> with a protocol-specific step; the ECoS backend must
    /// normalize it to the <c>0..127</c> domain before the hardware backend sees it, while the
    /// already-normalized <c>speed[...]</c> form and the safety interlock behaviour stay intact.
    /// No hardware is involved.
    /// </summary>
    public class SimpleEcosBackendSpeedNormalizationTests
    {
        private sealed class RecordingHardwareBackend : IHardwareBackend
        {
            public List<(int Address, int Speed, int Direction)> LocoCommands { get; } = new();

            public bool Applied { get; set; } = true;

            public bool SetPower(bool on) => true;

            public bool SetLocoSpeed(int address, int ecosSpeed, int direction)
            {
                LocoCommands.Add((address, ecosSpeed, direction));
                return Applied;
            }

            public bool SetSwitch(int decoderAddress, int outputIndex, bool on) => true;
        }

        private sealed class NullStopSink : ISafetyStopSink
        {
            public SafetyStopResult StopLoco(int address) => SafetyStopResult.Commanded(new ushort[] { 1 });

            public SafetyStopResult StopLayout() => SafetyStopResult.Commanded(new ushort[] { 1 });
        }

        private sealed class StubBlockPositionProvider : IBlockPositionProvider
        {
            // Explicit accessors: the stub never raises the event, and this avoids CS0067.
            public event Action<int, int>? BlockEntered
            {
                add { }
                remove { }
            }

            public int? TryGetBlockForLoc(int loc) => 1;
        }

        private sealed class Harness : IDisposable
        {
            private readonly string _path;

            public RecordingHardwareBackend Hardware { get; } = new();

            public SimpleEcosBackend Backend { get; }

            public Harness(string protocol = "DCC28", Func<IHardwareBackend, IHardwareBackend>? wrap = null)
            {
                _path = Path.Combine(Path.GetTempPath(), $"speednorm-{Guid.NewGuid():N}.json");

                var repository = new JsonLocoRepository(_path);
                repository.LoadAsync().GetAwaiter().GetResult();
                repository.AddOrUpdate(new LocoInfo { EcosId = 1000, Address = 1, Protocol = protocol });

                var target = wrap is null ? Hardware : wrap(Hardware);
                Backend = new SimpleEcosBackend(target, repository, new StubBlockPositionProvider());
            }

            public async Task<string> SendAsync(string command)
            {
                using var writer = new StringWriter();
                var parsed = new SimpleEcosCommandParser().Parse(command);
                await Backend.HandleAsync(parsed!, writer, CancellationToken.None);
                return writer.ToString();
            }

            public void Dispose()
            {
                try { File.Delete(_path); } catch { /* temp cleanup */ }
            }
        }

        private static ControlDiagnostic LocoFault(int loco) => new()
        {
            Code = DiagnosticCode.RouteSwitchMismatch,
            Severity = DiagnosticSeverity.StopRequired,
            Subject = "switch 1",
            LocoAddress = loco,
            Detail = "test latch"
        };

        private static (ControlSafetyGuard Guard, ControlDiagnostics Diagnostics) CreateLatchedGuard(int loco)
        {
            var diagnostics = new ControlDiagnostics();
            var guard = new ControlSafetyGuard(new NullStopSink(), diagnostics);
            guard.Apply(LocoFault(loco));
            return (guard, diagnostics);
        }

        [Fact]
        public async Task Dcc28_SpeedStep28_ReachesBackendAsNormalizedFullThrottle()
        {
            using var harness = new Harness();

            var output = await harness.SendAsync("set(1000,speedstep[28])");

            Assert.Equal(127, harness.Hardware.LocoCommands.Single().Speed);
            Assert.Contains("1000 speed[127]", output);
        }

        [Fact]
        public async Task Dcc28_SpeedStep0_ReachesBackendAsStop()
        {
            using var harness = new Harness();

            var output = await harness.SendAsync("set(1000,speedstep[0])");

            Assert.Equal(0, harness.Hardware.LocoCommands.Single().Speed);
            Assert.Contains("<END 0 (OK)>", output);
        }

        [Fact]
        public async Task Dcc28_LowSpeedStep_IsScaled()
        {
            using var harness = new Harness();

            await harness.SendAsync("set(1000,speedstep[1])");

            // round(1 * 127 / 28) = 5
            Assert.Equal(5, harness.Hardware.LocoCommands.Single().Speed);
        }

        [Fact]
        public async Task Dcc28_HalfSpeedStep_IsScaled()
        {
            using var harness = new Harness();

            await harness.SendAsync("set(1000,speedstep[14])");

            Assert.Equal(64, harness.Hardware.LocoCommands.Single().Speed);
        }

        [Fact]
        public async Task Dcc28_DirectionChange_ReusesTheNormalizedSpeed()
        {
            using var harness = new Harness();

            await harness.SendAsync("set(1000,speedstep[14])");
            await harness.SendAsync("set(1000,dir[0])");

            var last = harness.Hardware.LocoCommands.Last();

            // The direction command must send the stored normalized speed (64), never the
            // raw DCC28 step (14), so changing direction does not alter speed normalization.
            Assert.Equal(1, last.Address);
            Assert.Equal(64, last.Speed);
            Assert.Equal(0, last.Direction);
        }

        [Fact]
        public async Task NormalizedSpeedForm_IsNotScaledTwice()
        {
            using var harness = new Harness();

            await harness.SendAsync("set(1000,speed[100])");

            Assert.Equal(100, harness.Hardware.LocoCommands.Single().Speed);
        }

        [Fact]
        public async Task Dcc128_SpeedStep_IsAlreadyNormalizedAndPassedThrough()
        {
            using var harness = new Harness("DCC128");

            await harness.SendAsync("set(1000,speedstep[100])");

            Assert.Equal(100, harness.Hardware.LocoCommands.Single().Speed);
        }

        [Fact]
        public async Task UnsupportedProtocol_NonZeroSpeedStep_IsRefusedAndNotAcknowledged()
        {
            using var harness = new Harness("MMF");

            var output = await harness.SendAsync("set(1000,speedstep[10])");

            Assert.Empty(harness.Hardware.LocoCommands);
            Assert.Contains("<END 1 (UNSUPPORTED_PROTOCOL)>", output);
            Assert.DoesNotContain("1000 speed[", output);
        }

        [Fact]
        public async Task UnsupportedProtocol_StopSpeedStep_IsStillApplied()
        {
            using var harness = new Harness("MMF");

            var output = await harness.SendAsync("set(1000,speedstep[0])");

            Assert.Equal(0, harness.Hardware.LocoCommands.Single().Speed);
            Assert.Contains("<END 0 (OK)>", output);
        }

        [Fact]
        public void NormalizedDcc28Speeds_ProduceTheExpectedPwmThroughTheExistingMapper()
        {
            // Neutral.
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, AmplifierSpeedMapper.ToPwm(0, 0));

            // Full DCC28 throttle normalizes to 127 and must reach the top of the forward range.
            Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", 28, out var full));
            Assert.Equal(AmplifierSpeedMapper.MaxPwm, AmplifierSpeedMapper.ToPwm(full, 0));

            // Half DCC28 throttle normalizes to 64, about halfway through the usable forward range.
            Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", 14, out var half));
            var halfPwm = AmplifierSpeedMapper.ToPwm(half, 0);
            Assert.Equal(601, halfPwm);
            Assert.InRange(halfPwm, 597, 602);
        }

        [Fact]
        public void Dcc28Step24_NoLongerUnderScales_ComparedToTheRawDefect()
        {
            // The live defect: step 24 was passed downstream as 24, producing ~PWM 475.
            Assert.Equal(475, AmplifierSpeedMapper.ToPwm(24, 0));

            // After normalization step 24 becomes 109 and uses much more of the range.
            Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", 24, out var normalized));
            Assert.Equal(109, normalized);
            Assert.Equal(742, AmplifierSpeedMapper.ToPwm(normalized, 0));
        }

        [Fact]
        public async Task LatchedLoco_NonZeroDcc28SpeedStep_IsRejectedAndNotAcknowledged()
        {
            var (guard, diagnostics) = CreateLatchedGuard(1);
            using var harness = new Harness(
                "DCC28",
                inner => new ControlSafetyInterlockBackend(inner, guard, diagnostics));

            var output = await harness.SendAsync("set(1000,speedstep[14])");

            Assert.Contains("<END 8 (SAFETY_INTERLOCK)>", output);
            Assert.DoesNotContain("1000 speed[", output);
            Assert.Empty(harness.Hardware.LocoCommands);
        }

        [Fact]
        public async Task LatchedLoco_NonZeroNormalizedSpeed_IsStillRejected()
        {
            var (guard, diagnostics) = CreateLatchedGuard(1);
            using var harness = new Harness(
                "DCC28",
                inner => new ControlSafetyInterlockBackend(inner, guard, diagnostics));

            var output = await harness.SendAsync("set(1000,speed[20])");

            Assert.Contains("<END 8 (SAFETY_INTERLOCK)>", output);
            Assert.Empty(harness.Hardware.LocoCommands);
        }

        [Fact]
        public async Task LatchedLoco_ZeroDcc28SpeedStep_IsStillAccepted()
        {
            var (guard, diagnostics) = CreateLatchedGuard(1);
            using var harness = new Harness(
                "DCC28",
                inner => new ControlSafetyInterlockBackend(inner, guard, diagnostics));

            var output = await harness.SendAsync("set(1000,speedstep[0])");

            Assert.Contains("<END 0 (OK)>", output);
            Assert.Equal(0, harness.Hardware.LocoCommands.Single().Speed);
        }
    }
}
