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
    /// Deterministic regression tests for the block-entry speed/direction re-application fix in
    /// <see cref="SimpleEcosBackend.OnBlockEntered"/>. No hardware is involved: a recording
    /// <see cref="IHardwareBackend"/> (or the real <see cref="TrackAmplifierHardwareBackend"/> with a
    /// look-ahead planner + occupancy fake) observes the physical commands.
    /// </summary>
    public class SimpleEcosBackendBlockEntryRegressionTests
    {
        private const int EcosId = 1001;
        private const int LocoAddress = 2;

        // -----------------------------------------------------------------
        // Shared fakes
        // -----------------------------------------------------------------

        private sealed class RecordingHardwareBackend : IHardwareBackend
        {
            public List<(int Address, int Speed, int Direction)> LocoCommands { get; } = new();

            public bool SetPower(bool on) => true;

            public bool SetLocoSpeed(int address, int ecosSpeed, int direction)
            {
                LocoCommands.Add((address, ecosSpeed, direction));
                return true;
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

        private sealed class MutableOccupancy : IOccupancyProvider
        {
            public HashSet<int> Occupied { get; } = new();

            public bool IsBlockOccupied(int block) => Occupied.Contains(block);

            public bool IsBlockOccupancyKnown(int block) => true;
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
        // Recording-hardware harness
        // -----------------------------------------------------------------

        private sealed class Harness : IDisposable
        {
            private readonly string _path;
            private readonly JsonLocoRepository _repository;

            public RecordingHardwareBackend Hardware { get; } = new();

            public MutableBlockPositionProvider Blocks { get; } = new();

            public SimpleEcosBackend Backend { get; }

            public Harness(Func<IHardwareBackend, IHardwareBackend>? wrap = null)
            {
                _path = Path.Combine(Path.GetTempPath(), $"blockentry-adv-{Guid.NewGuid():N}.json");

                _repository = new JsonLocoRepository(_path);
                _repository.LoadAsync().GetAwaiter().GetResult();
                _repository.AddOrUpdate(new LocoInfo { EcosId = EcosId, Address = LocoAddress, Protocol = "DCC28" });

                var target = wrap is null ? Hardware : wrap(Hardware);
                Backend = new SimpleEcosBackend(target, _repository, Blocks);
            }

            public void AddLoco(int ecosId, int address)
                => _repository.AddOrUpdate(new LocoInfo { EcosId = ecosId, Address = address, Protocol = "DCC28" });

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
        // Real amplifier-translation harness (look-ahead + occupancy)
        // -----------------------------------------------------------------

        private sealed class AmplifierHarness : IDisposable
        {
            private readonly string _path;

            public MutableBlockPositionProvider Blocks { get; } = new();

            public MutableOccupancy Occupancy { get; } = new();

            public TrackApplicationVariables Variables { get; } = new();

            public SimpleEcosBackend Backend { get; }

            public AmplifierHarness()
            {
                _path = Path.Combine(Path.GetTempPath(), $"blockentry-pwm-{Guid.NewGuid():N}.json");

                var repository = new JsonLocoRepository(_path);
                repository.LoadAsync().GetAwaiter().GetResult();
                repository.AddOrUpdate(new LocoInfo { EcosId = EcosId, Address = LocoAddress, Protocol = "DCC28" });

                var topology = BlockTopology.Parse("amps: 1:1,2:3,3:4,4:6 ; routes: 1>2,2>3,3>4,4>1");
                var hardware = new TrackAmplifierHardwareBackend(
                    Blocks,
                    topology,
                    Variables,
                    lookAheadPlanner: new LookAheadPlanner(topology),
                    occupancyProvider: Occupancy);

                Backend = new SimpleEcosBackend(hardware, repository, Blocks);
            }

            public async Task SendAsync(string command)
            {
                using var writer = new StringWriter();
                var parsed = new SimpleEcosCommandParser().Parse(command);
                await Backend.HandleAsync(parsed!, writer, CancellationToken.None);
            }

            public int AmplifierHr0(int amplifier)
                => Variables.PendingWrites[(ushort)amplifier].Hr0Value & 0x03FF;

            public void Dispose()
            {
                try { File.Delete(_path); } catch { /* temp cleanup */ }
            }
        }

        // -----------------------------------------------------------------
        // 1) Stale-PWM regression (live defect shape)
        // -----------------------------------------------------------------

        [Fact]
        public async Task StalePwm_OnBlockEntry_ReappliesLatestSpeed_WithoutNewSpeedCommand()
        {
            using var harness = new AmplifierHarness();
            harness.Blocks.Block = 1;

            const int faster = 100;
            const int slower = 50;

            // Forward direction, then faster speed -> block 1 (amp1) + look-ahead block 2 (amp3).
            await harness.SendAsync("set(1001,dir[0])");
            await harness.SendAsync($"set(1001,speed[{faster}])");
            int pwmFaster = AmplifierSpeedMapper.ToPwm(faster, 0);
            Assert.Equal(pwmFaster, harness.AmplifierHr0(1));
            Assert.Equal(pwmFaster, harness.AmplifierHr0(3));

            // Block 2 becomes occupied: lowering the speed re-commands amp1 but NOT the look-ahead
            // amp3, so amp3 keeps the stale (faster) duty cycle.
            harness.Occupancy.Occupied.Add(2);
            await harness.SendAsync($"set(1001,speed[{slower}])");
            int pwmSlower = AmplifierSpeedMapper.ToPwm(slower, 0);
            Assert.Equal(pwmSlower, harness.AmplifierHr0(1));
            Assert.Equal(pwmFaster, harness.AmplifierHr0(3)); // stale

            // Block 2 clears and the loco enters it. NO new ECoS speed command is sent; OnBlockEntered
            // must re-apply the LATEST retained speed to amp3 (now current) and amp4 (new look-ahead).
            harness.Occupancy.Occupied.Clear();
            harness.Blocks.EnterBlock(LocoAddress, 2);

            Assert.Equal(pwmSlower, harness.AmplifierHr0(3)); // corrected from stale (faster)
            Assert.Equal(pwmSlower, harness.AmplifierHr0(4)); // new look-ahead (block 3 -> amp4)
        }

        // -----------------------------------------------------------------
        // 2) Direction regression
        // -----------------------------------------------------------------

        [Fact]
        public async Task DirectionChange_LaterBlockTransition_ReappliesLatestDirection()
        {
            using var harness = new Harness();
            harness.Blocks.Block = 1;

            await harness.SendAsync("set(1001,speed[100])"); // retained speed 100, default direction 1
            await harness.SendAsync("set(1001,dir[0])");     // latest direction 0

            harness.Blocks.EnterBlock(LocoAddress, 2); // block transition, NO new direction command

            var last = harness.Hardware.LocoCommands.Last();
            Assert.Equal(LocoAddress, last.Address);
            Assert.Equal(100, last.Speed);
            Assert.Equal(0, last.Direction); // LATEST direction, not the stale default
        }

        // -----------------------------------------------------------------
        // 3) Safety interlock
        // -----------------------------------------------------------------

        [Fact]
        public async Task LatchedLoco_BlockTransition_DoesNotProduceNonNeutralWrite()
        {
            var diagnostics = new ControlDiagnostics();
            var guard = new ControlSafetyGuard(new NullStopSink(), diagnostics);

            using var harness = new Harness(
                wrap: inner => new ControlSafetyInterlockBackend(inner, guard, diagnostics));
            harness.Blocks.Block = 1;

            await harness.SendAsync("set(1001,speed[100])");
            Assert.Single(harness.Hardware.LocoCommands); // speed 100 retained before the latch

            guard.Apply(LocoFault(LocoAddress)); // latch non-zero movement for this loco

            int countBefore = harness.Hardware.LocoCommands.Count;
            harness.Blocks.EnterBlock(LocoAddress, 2);

            // The interlock blocked the re-apply: the inner hardware received nothing new.
            Assert.Equal(countBefore, harness.Hardware.LocoCommands.Count);

            // The logical retained speed is unchanged.
            var view = await harness.SendAsync("request(1001,speed)");
            Assert.Contains("1001 speed[100]", view);
        }

        // -----------------------------------------------------------------
        // 4) Two-loco isolation
        // -----------------------------------------------------------------

        [Fact]
        public async Task TwoLocos_BlockTransition_ReappliesOnlyThatLocosSpeed()
        {
            using var harness = new Harness();
            harness.AddLoco(1000, 1); // Loco B, address 1
            harness.Blocks.Block = 1;

            await harness.SendAsync("set(1001,speed[100])"); // Loco A (address 2) speed 100
            await harness.SendAsync("set(1000,speed[50])");  // Loco B (address 1) speed 50

            int countBefore = harness.Hardware.LocoCommands.Count;

            harness.Blocks.EnterBlock(LocoAddress, 2); // Loco A moves

            // Exactly one new command: Loco A's re-application (not Loco B's).
            Assert.Equal(countBefore + 1, harness.Hardware.LocoCommands.Count);
            var last = harness.Hardware.LocoCommands.Last();
            Assert.Equal(LocoAddress, last.Address);
            Assert.Equal(100, last.Speed); // Loco A's retained speed, NOT Loco B's 50

            // No new command was issued for Loco B (address 1) as a result of Loco A's transition.
            Assert.DoesNotContain(harness.Hardware.LocoCommands.Skip(countBefore), c => c.Address == 1);
        }
    }
}
