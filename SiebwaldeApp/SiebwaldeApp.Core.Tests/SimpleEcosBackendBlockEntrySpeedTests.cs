using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Regression tests for the confirmed "stale duty cycle on section change" defect: when a
    /// locomotive enters a new section, the ECoS backend must re-apply the retained (non-zero) speed to
    /// the new section's amplifier(s) via <c>SetLocoSpeed</c>, otherwise the freshly-entered amplifier
    /// runs at a stale/wrong duty cycle. No hardware is involved (a recording <see cref="IHardwareBackend"/>).
    /// </summary>
    public class SimpleEcosBackendBlockEntrySpeedTests
    {
        private const int EcosId = 1001;
        private const int LocoAddress = 2;

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

        private sealed class Harness : IDisposable
        {
            private readonly string _path;

            public RecordingHardwareBackend Hardware { get; } = new();

            public MutableBlockPositionProvider Blocks { get; } = new();

            public SimpleEcosBackend Backend { get; }

            public Harness()
            {
                _path = Path.Combine(Path.GetTempPath(), $"blockentry-{Guid.NewGuid():N}.json");

                var repository = new JsonLocoRepository(_path);
                repository.LoadAsync().GetAwaiter().GetResult();
                repository.AddOrUpdate(new LocoInfo { EcosId = EcosId, Address = LocoAddress, Protocol = "DCC28" });

                Backend = new SimpleEcosBackend(Hardware, repository, Blocks);
            }

            public async Task SendAsync(string command)
            {
                using var writer = new StringWriter();
                var parsed = new SimpleEcosCommandParser().Parse(command);
                await Backend.HandleAsync(parsed!, writer, CancellationToken.None);
            }

            public void Dispose()
            {
                try { File.Delete(_path); } catch { /* temp cleanup */ }
            }
        }

        [Fact]
        public async Task NonZeroSpeed_BlockChange_ReissuesSetLocoSpeed()
        {
            using var harness = new Harness();
            harness.Blocks.Block = 1;

            await harness.SendAsync("set(1001,speed[64])");
            Assert.Single(harness.Hardware.LocoCommands); // initial speed command

            harness.Blocks.EnterBlock(LocoAddress, 2); // block change

            Assert.Equal(2, harness.Hardware.LocoCommands.Count);
            var last = harness.Hardware.LocoCommands.Last();
            Assert.Equal(LocoAddress, last.Address);
            Assert.Equal(64, last.Speed);
            Assert.Equal(1, last.Direction);
        }

        [Fact]
        public async Task ZeroSpeed_BlockChange_DoesNotReissue()
        {
            using var harness = new Harness();
            harness.Blocks.Block = 1;

            await harness.SendAsync("set(1001,speed[0])");
            int count = harness.Hardware.LocoCommands.Count;

            harness.Blocks.EnterBlock(LocoAddress, 2);

            Assert.Equal(count, harness.Hardware.LocoCommands.Count); // no re-issue for a stopped loco
        }

        [Fact]
        public async Task LatestRetainedSpeed_IsReappliedOnBlockChange()
        {
            using var harness = new Harness();
            harness.Blocks.Block = 1;

            await harness.SendAsync("set(1001,speed[64])");
            await harness.SendAsync("set(1001,speed[100])"); // latest retained speed = 100

            harness.Blocks.EnterBlock(LocoAddress, 2);

            var last = harness.Hardware.LocoCommands.Last();
            Assert.Equal(100, last.Speed);
        }

        [Fact]
        public async Task SameBlockReportedTwice_NoReapplication()
        {
            using var harness = new Harness();
            harness.Blocks.Block = 1;

            await harness.SendAsync("set(1001,speed[64])");
            int count = harness.Hardware.LocoCommands.Count;

            // First entry (block actually changes from "none") re-applies once.
            harness.Blocks.EnterBlock(LocoAddress, 1);
            Assert.Equal(count + 1, harness.Hardware.LocoCommands.Count);

            // Duplicate report of the same block must not re-apply again.
            harness.Blocks.EnterBlock(LocoAddress, 1);
            Assert.Equal(count + 1, harness.Hardware.LocoCommands.Count);
        }
    }
}
