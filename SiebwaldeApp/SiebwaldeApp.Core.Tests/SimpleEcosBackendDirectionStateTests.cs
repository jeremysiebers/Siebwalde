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
    /// Regression tests for the confirmed "direction before known block" defect: a direction
    /// command must be retained as requested/logical state even when no physical amplifier is
    /// addressable, and a later movement must use the most recently requested direction.
    ///
    /// The tests also pin the related command-result semantics: an unmapped/no-target command is
    /// not falsely reported as a latched <c>SAFETY_INTERLOCK</c>, while a real safety latch still
    /// refuses non-zero movement. No hardware is involved.
    /// </summary>
    public class SimpleEcosBackendDirectionStateTests
    {
        private const int EcosId = 1001;
        private const int LocoAddress = 2;

        // -----------------------------------------------------------------
        // Shared fakes
        // -----------------------------------------------------------------

        private sealed class RecordingHardwareBackend : IHardwareBackend
        {
            public List<(int Address, int Speed, int Direction)> LocoCommands { get; } = new();

            /// <summary>
            /// Simulates whether a physical target existed. <see langword="false"/> mirrors the
            /// real backend returning false because the locomotive has no known block.
            /// </summary>
            public bool Applied { get; set; } = true;

            public bool SetPower(bool on) => true;

            public bool SetLocoSpeed(int address, int ecosSpeed, int direction)
            {
                LocoCommands.Add((address, ecosSpeed, direction));
                return Applied;
            }

            public bool SetSwitch(int decoderAddress, int outputIndex, bool on) => true;
        }

        private sealed class MutableBlockPositionProvider : IBlockPositionProvider
        {
            public int? Block { get; set; }

            public event Action<int, int>? BlockEntered;

            public int? TryGetBlockForLoc(int loc) => Block;

            public void EnterBlock(int locoAddress, int block)
            {
                Block = block;
                BlockEntered?.Invoke(locoAddress, block);
            }
        }

        private sealed class NullStopSink : ISafetyStopSink
        {
            public SafetyStopResult StopLoco(int address) => SafetyStopResult.Commanded(new ushort[] { 1 });

            public SafetyStopResult StopLayout() => SafetyStopResult.Commanded(new ushort[] { 1 });
        }

        private static ControlDiagnostic LocoFault(int loco) => new()
        {
            Code = DiagnosticCode.RouteSwitchMismatch,
            Severity = DiagnosticSeverity.StopRequired,
            Subject = "switch 1",
            LocoAddress = loco,
            Detail = "test latch"
        };

        // -----------------------------------------------------------------
        // Fake-backend harness (logical behaviour)
        // -----------------------------------------------------------------

        private sealed class Harness : IDisposable
        {
            private readonly string _path;
            private readonly JsonLocoRepository _repository;

            public RecordingHardwareBackend Hardware { get; } = new();

            public MutableBlockPositionProvider Blocks { get; } = new();

            public SimpleEcosBackend Backend { get; }

            public Harness(
                string protocol = "DCC28",
                int ecosId = EcosId,
                int address = LocoAddress,
                Func<IHardwareBackend, IHardwareBackend>? wrap = null)
            {
                _path = Path.Combine(Path.GetTempPath(), $"dirstate-{Guid.NewGuid():N}.json");

                _repository = new JsonLocoRepository(_path);
                _repository.LoadAsync().GetAwaiter().GetResult();
                _repository.AddOrUpdate(new LocoInfo { EcosId = ecosId, Address = address, Protocol = protocol });

                var target = wrap is null ? Hardware : wrap(Hardware);
                Backend = new SimpleEcosBackend(target, _repository, Blocks);
            }

            public void AddLoco(int ecosId, int address, string protocol = "DCC28")
                => _repository.AddOrUpdate(new LocoInfo { EcosId = ecosId, Address = address, Protocol = protocol });

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

        // -----------------------------------------------------------------
        // Real amplifier-translation harness (end-to-end through SimpleEcosBackend)
        // -----------------------------------------------------------------

        private sealed class AmplifierHarness : IDisposable
        {
            private readonly string _path;

            public MutableBlockPositionProvider Blocks { get; } = new();

            public TrackApplicationVariables Variables { get; } = new();

            public SimpleEcosBackend Backend { get; }

            public AmplifierHarness(string topology = "1:1,2:2")
            {
                _path = Path.Combine(Path.GetTempPath(), $"dirstate-amp-{Guid.NewGuid():N}.json");

                var repository = new JsonLocoRepository(_path);
                repository.LoadAsync().GetAwaiter().GetResult();
                repository.AddOrUpdate(new LocoInfo { EcosId = EcosId, Address = LocoAddress, Protocol = "DCC28" });

                var amplifier = new TrackAmplifierHardwareBackend(
                    Blocks, BlockTopology.Parse(topology), Variables);

                Backend = new SimpleEcosBackend(amplifier, repository, Blocks);
            }

            public async Task<string> SendAsync(string command)
            {
                using var writer = new StringWriter();
                var parsed = new SimpleEcosCommandParser().Parse(command);
                await Backend.HandleAsync(parsed!, writer, CancellationToken.None);
                return writer.ToString();
            }

            /// <summary>Last desired HoldingReg0 PWM for an amplifier (does not consume the write).</summary>
            public int AmplifierHr0(int amplifier)
                => Variables.PendingWrites[(ushort)amplifier].Hr0Value & 0x03FF;

            public void Dispose()
            {
                try { File.Delete(_path); } catch { /* temp cleanup */ }
            }
        }

        // -----------------------------------------------------------------
        // Core defect: direction while unmapped
        // -----------------------------------------------------------------

        [Fact]
        public async Task UnmappedDirectionForward_IsRetainedAndUsedOnceBlockAppears()
        {
            using var harness = new AmplifierHarness();
            harness.Blocks.Block = null;

            var dirOutput = await harness.SendAsync("set(1001,dir[0])");

            // Logically accepted: OK and the direction state event is emitted.
            Assert.Contains("<END 0 (OK)>", dirOutput);
            Assert.Contains("1001 dir[0]", dirOutput);

            // No physical amplifier command was generated while there is no known block.
            Assert.Empty(harness.Variables.PendingWrites);

            // Block becomes known; the first low movement must use the retained forward direction.
            harness.Blocks.EnterBlock(LocoAddress, 1);

            var speedOutput = await harness.SendAsync("set(1001,speedstep[1])");

            Assert.Contains("<END 0 (OK)>", speedOutput);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(5, 0), harness.AmplifierHr0(1));
            Assert.Equal(416, harness.AmplifierHr0(1)); // forward band; stale reverse default would be 382
        }

        [Fact]
        public async Task UnmappedDirectionReverse_IsRetainedAndUsedOnceBlockAppears()
        {
            using var harness = new AmplifierHarness();
            harness.Blocks.Block = null;

            var dirOutput = await harness.SendAsync("set(1001,dir[1])");

            Assert.Contains("<END 0 (OK)>", dirOutput);
            Assert.Contains("1001 dir[1]", dirOutput);
            Assert.Empty(harness.Variables.PendingWrites);

            harness.Blocks.EnterBlock(LocoAddress, 1);

            await harness.SendAsync("set(1001,speedstep[1])");

            Assert.Equal(AmplifierSpeedMapper.ToPwm(5, 1), harness.AmplifierHr0(1));
            Assert.Equal(382, harness.AmplifierHr0(1)); // reverse band
        }

        [Fact]
        public async Task UnmappedForwardThenReverse_UsesTheMostRecentDirection()
        {
            using var harness = new AmplifierHarness();
            harness.Blocks.Block = null;

            await harness.SendAsync("set(1001,dir[0])");
            await harness.SendAsync("set(1001,dir[1])");

            harness.Blocks.EnterBlock(LocoAddress, 1);
            await harness.SendAsync("set(1001,speedstep[1])");

            Assert.Equal(382, harness.AmplifierHr0(1)); // reverse, the most recent request
        }

        [Fact]
        public async Task UnmappedReverseThenForward_UsesTheMostRecentDirection()
        {
            using var harness = new AmplifierHarness();
            harness.Blocks.Block = null;

            await harness.SendAsync("set(1001,dir[1])");
            await harness.SendAsync("set(1001,dir[0])");

            harness.Blocks.EnterBlock(LocoAddress, 1);
            await harness.SendAsync("set(1001,speedstep[1])");

            Assert.Equal(416, harness.AmplifierHr0(1)); // forward, the most recent request
        }

        [Fact]
        public async Task DefaultDirection_IsReverse_ButAnExplicitUnmappedRequestOverridesIt()
        {
            // Without an explicit direction the stale default (Direction = 1, reverse) is used.
            using (var plain = new AmplifierHarness())
            {
                plain.Blocks.Block = 1;
                await plain.SendAsync("set(1001,speedstep[1])");
                Assert.Equal(AmplifierSpeedMapper.ToPwm(5, 1), plain.AmplifierHr0(1));
                Assert.Equal(382, plain.AmplifierHr0(1));
            }

            // An explicit forward request while unmapped must win over that default.
            using var harness = new AmplifierHarness();
            harness.Blocks.Block = null;
            await harness.SendAsync("set(1001,dir[0])");
            harness.Blocks.EnterBlock(LocoAddress, 1);
            await harness.SendAsync("set(1001,speedstep[1])");
            Assert.Equal(416, harness.AmplifierHr0(1));
        }

        [Fact]
        public async Task LiveCommandOrder_UnmappedDirectionWithStop_IsNotFalselyRefusedAndIsRetained()
        {
            // Mirrors the live reproduction: set(1001,dir[...],speedstep[0]) while block 0 has no
            // amplifier mapping, then the locomotive is placed in block 1 and speedstep[1] is sent.
            using var harness = new AmplifierHarness();
            harness.Blocks.Block = null;

            var output = await harness.SendAsync("set(1001,dir[0],speedstep[0])");

            Assert.Contains("<END 0 (OK)>", output);
            Assert.DoesNotContain("SAFETY_INTERLOCK", output);
            Assert.Contains("1001 dir[0]", output);
            Assert.Empty(harness.Variables.PendingWrites);

            harness.Blocks.EnterBlock(LocoAddress, 1);

            var speedOutput = await harness.SendAsync("set(1001,speedstep[1])");

            Assert.Contains("<END 0 (OK)>", speedOutput);
            Assert.Equal(416, harness.AmplifierHr0(1));
        }

        [Fact]
        public async Task UnmappedStop_IsLogicallyAcceptedAndProducesNoPhysicalMovement()
        {
            using var harness = new AmplifierHarness();
            harness.Blocks.Block = null;

            var output = await harness.SendAsync("set(1001,speedstep[0])");

            Assert.Contains("<END 0 (OK)>", output);
            Assert.Contains("1001 speed[0]", output);
            Assert.Empty(harness.Variables.PendingWrites);
        }

        [Fact]
        public async Task KnownBlock_DirectionChange_StillAppliesTheDirectionPhysically()
        {
            using var harness = new AmplifierHarness();
            harness.Blocks.Block = 1;

            await harness.SendAsync("set(1001,dir[0])"); // explicit forward
            await harness.SendAsync("set(1001,speed[64])");
            Assert.True(harness.AmplifierHr0(1) > AmplifierSpeedMapper.NeutralPwm); // forward band

            var output = await harness.SendAsync("set(1001,dir[1])");

            Assert.Contains("<END 0 (OK)>", output);
            Assert.Contains("1001 dir[1]", output);
            Assert.Equal(AmplifierSpeedMapper.ToPwm(64, 1), harness.AmplifierHr0(1));
            Assert.True(harness.AmplifierHr0(1) < AmplifierSpeedMapper.NeutralPwm); // reverse band
        }

        // -----------------------------------------------------------------
        // Logical vs physical semantics
        // -----------------------------------------------------------------

        [Fact]
        public async Task UnmappedDirectionAndStop_AreNotFalselyReportedAsSafetyInterlock()
        {
            using var harness = new Harness();

            // The backend cannot apply anything: it mirrors "no known block".
            harness.Hardware.Applied = false;

            var dir = await harness.SendAsync("set(1001,dir[0])");
            Assert.DoesNotContain("SAFETY_INTERLOCK", dir);
            Assert.Contains("<END 0 (OK)>", dir);
            Assert.Contains("1001 dir[0]", dir);

            var stop = await harness.SendAsync("set(1001,speedstep[0])");
            Assert.DoesNotContain("SAFETY_INTERLOCK", stop);
            Assert.Contains("<END 0 (OK)>", stop);
        }

        [Fact]
        public async Task UnmappedNonZeroSpeed_IsLogicallyAcceptedWithoutAPhysicalTarget()
        {
            using var harness = new Harness();
            harness.Hardware.Applied = false;

            var output = await harness.SendAsync("set(1001,speed[64])");

            // The requested logical state is retained; there is no physical target, but that is
            // not a safety refusal and must not be reported as one.
            Assert.DoesNotContain("SAFETY_INTERLOCK", output);
            Assert.Contains("<END 0 (OK)>", output);
            Assert.Contains("1001 speed[64]", output);
        }

        [Fact]
        public async Task UnmappedDirection_IsReportedByRequestState()
        {
            using var harness = new Harness();
            await harness.SendAsync("set(1001,dir[0])");

            var output = await harness.SendAsync("request(1001,view)");

            Assert.Contains("1001 dir[0]", output);
        }

        [Fact]
        public async Task UnmappedDirection_IsIndependentPerLocomotive()
        {
            using var harness = new Harness();
            harness.AddLoco(1000, 1);

            await harness.SendAsync("set(1000,dir[0])");
            await harness.SendAsync("set(1001,dir[1])");

            await harness.SendAsync("set(1000,speed[64])");
            await harness.SendAsync("set(1001,speed[64])");

            var first = harness.Hardware.LocoCommands.Last(c => c.Address == 1);
            var second = harness.Hardware.LocoCommands.Last(c => c.Address == 2);

            Assert.Equal(0, first.Direction);
            Assert.Equal(1, second.Direction);
        }

        // -----------------------------------------------------------------
        // Safety interlock is preserved
        // -----------------------------------------------------------------

        [Fact]
        public async Task LatchedLoco_DirectionIsRetainedButNonZeroMovementIsStillRejected()
        {
            var diagnostics = new ControlDiagnostics();
            var guard = new ControlSafetyGuard(new NullStopSink(), diagnostics);
            guard.Apply(LocoFault(LocoAddress));

            using var harness = new Harness(
                wrap: inner => new ControlSafetyInterlockBackend(inner, guard, diagnostics));

            harness.Blocks.Block = 1;

            // A real safety latch still refuses non-zero movement with the safety response.
            var rejected = await harness.SendAsync("set(1001,speed[64])");
            Assert.Contains("<END 8 (SAFETY_INTERLOCK)>", rejected);
            Assert.DoesNotContain("1001 speed[", rejected);

            // A direction command is logical state: it is retained even while latched, and the
            // physical application is still blocked by the interlock.
            var dirOutput = await harness.SendAsync("set(1001,dir[0])");
            Assert.Contains("<END 0 (OK)>", dirOutput);
            Assert.Contains("1001 dir[0]", dirOutput);

            // The interlock never let a non-zero movement reach the inner backend.
            Assert.DoesNotContain(harness.Hardware.LocoCommands, c => c.Speed != 0);

            // After the latch is cleared, the retained forward direction is used.
            guard.RevalidationCheck = _ => true;
            Assert.True(guard.Reset());

            var accepted = await harness.SendAsync("set(1001,speed[64])");
            Assert.Contains("<END 0 (OK)>", accepted);

            var last = harness.Hardware.LocoCommands.Last();
            Assert.Equal(64, last.Speed);
            Assert.Equal(0, last.Direction);
        }
    }
}
