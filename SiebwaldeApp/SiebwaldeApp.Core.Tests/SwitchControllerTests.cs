using System.Collections.Generic;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Behaviour tests for the switch translation path: ECoS switch commands in, physical
    /// switch output drives out. No hardware and no WPF are involved.
    /// </summary>
    public class SwitchControllerTests
    {
        private sealed class RecordingSwitchOutput : ISwitchOutput
        {
            public bool IsAvailable { get; set; } = true;

            public bool Applied { get; set; } = true;

            public List<(int Address, SwitchPosition Position)> Drives { get; } = new();

            public bool SetPosition(int physicalAddress, SwitchPosition position)
            {
                Drives.Add((physicalAddress, position));
                return Applied;
            }
        }

        private sealed class RecordingHardwareBackend : IHardwareBackend
        {
            public bool? Power { get; private set; }
            public List<(int Address, int Speed, int Direction)> LocoCommands { get; } = new();

            public bool SetPower(bool on)
            {
                Power = on;
                return true;
            }

            public bool SetLocoSpeed(int address, int ecosSpeed, int direction)
            {
                LocoCommands.Add((address, ecosSpeed, direction));
                return true;
            }
            public bool SetSwitch(int decoderAddress, int outputIndex, bool on)
            {
                SwitchCalls.Add((decoderAddress, outputIndex, on));
                return true;
            }

            public List<(int DecoderAddress, int OutputIndex, bool On)> SwitchCalls { get; } = new();
        }

        private static SwitchController CreateController(
            string configuration,
            ISwitchOutput output,
            List<string>? log = null)
            => new(SwitchMapping.Parse(configuration), output, log is null ? null : log.Add);

        [Theory]
        [InlineData(SwitchPosition.Straight)]
        [InlineData(SwitchPosition.Diverging)]
        public void MappedCommand_DrivesThePhysicalOutput(SwitchPosition requested)
        {
            var output = new RecordingSwitchOutput();
            var controller = CreateController("1:7:keep", output);

            var applied = controller.TryApply(1, requested, out var physical);

            Assert.True(applied);
            Assert.Equal(requested, physical);
            Assert.Single(output.Drives);
            Assert.Equal(7, output.Drives[0].Address);
            Assert.Equal(requested, output.Drives[0].Position);
        }

        [Fact]
        public void InvertedMapping_DrivesTheOppositePhysicalPosition()
        {
            var output = new RecordingSwitchOutput();
            var controller = CreateController("1:7:inverted:keep", output);

            controller.TryApply(1, SwitchPosition.Straight, out var physical);

            Assert.Equal(SwitchPosition.Diverging, physical);
            Assert.Equal(SwitchPosition.Diverging, output.Drives[0].Position);
        }

        [Fact]
        public void UnmappedAddress_IsIgnoredAndDrivesNothing()
        {
            var output = new RecordingSwitchOutput();
            var log = new List<string>();
            var controller = CreateController("1:1:keep", output, log);

            var applied = controller.TryApply(42, SwitchPosition.Diverging, out _);

            Assert.False(applied);
            Assert.Empty(output.Drives);
            Assert.Contains(log, m => m.Contains("42") && m.Contains("not mapped"));
        }

        [Fact]
        public void UnmappedCommand_NeverReachesAnUnrelatedOutput()
        {
            var output = new RecordingSwitchOutput();
            var controller = CreateController("1:1:keep, 2:2:keep", output);

            // Address 3 is not configured; nothing may move.
            controller.TryApply(3, SwitchPosition.Straight, out _);

            Assert.Empty(output.Drives);
        }

        [Fact]
        public void Initialize_DrivesConfiguredDefaults()
        {
            var output = new RecordingSwitchOutput();
            var controller = CreateController("1:1:g, 2:2:r", output);

            controller.Initialize();

            Assert.Equal(2, output.Drives.Count);
            Assert.Equal((1, SwitchPosition.Straight), output.Drives[0]);
            Assert.Equal((2, SwitchPosition.Diverging), output.Drives[1]);
        }

        [Fact]
        public void Initialize_LeavesKeepEntriesAlone()
        {
            var output = new RecordingSwitchOutput();
            var log = new List<string>();
            var controller = CreateController("1:1:keep, 2:2:r", output, log);

            controller.Initialize();

            Assert.Single(output.Drives);
            Assert.Equal(2, output.Drives[0].Address);
            Assert.Contains(log, m => m.Contains("keep") && m.Contains("untouched"));
        }

        [Fact]
        public void Initialize_DoesNotReportKeepEntriesAsKnown()
        {
            var output = new RecordingSwitchOutput();
            var controller = CreateController("1:1:keep", output);

            controller.Initialize();

            Assert.Empty(controller.GetLogicalPositions());
        }

        [Fact]
        public void LogicalPositions_FeedRoutingWhilePhysicalIsTrackedSeparately()
        {
            var output = new RecordingSwitchOutput();
            var controller = CreateController("1:9:inverted:keep", output);

            controller.TryApply(1, SwitchPosition.Straight, out _);

            // Routing speaks Koploper's logical state ...
            Assert.Equal(SwitchPosition.Straight, controller.GetLogicalPositions()[1]);
            // ... while the output was physically driven to the opposite position.
            Assert.True(controller.TryGetPhysicalPosition(9, out var physical));
            Assert.Equal(SwitchPosition.Diverging, physical);
        }

        [Fact]
        public void ConfigurationErrors_AreSurfacedToTheLog()
        {
            var output = new RecordingSwitchOutput();
            var log = new List<string>();
            var controller = CreateController("1:nope:keep, 2:2:keep", output, log);

            Assert.Contains(log, m => m.Contains("Switch mapping problem"));
            Assert.True(controller.Mapping.IsMapped(2));
        }

        // ---------------------------------------------------------------------
        // The ECoS-shaped call path used by SimpleEcosBackend
        // ---------------------------------------------------------------------

        [Fact]
        public void TranslatingBackend_StraightCoilMapsToStraight()
        {
            var output = new RecordingSwitchOutput();
            var controller = CreateController("1:1:keep", output);
            var backend = new SwitchTranslatingHardwareBackend(new RecordingHardwareBackend(), controller);

            // SimpleEcosBackend calls SetSwitch(addr, outputIndex, on) with on = true for
            // switch[<addr>g] (index 0) and switch[<addr>r] (index 1).
            backend.SetSwitch(1, 0, true);

            Assert.Single(output.Drives);
            Assert.Equal(SwitchPosition.Straight, output.Drives[0].Position);
        }

        [Fact]
        public void TranslatingBackend_DivergingCoilMapsToDiverging()
        {
            var output = new RecordingSwitchOutput();
            var controller = CreateController("1:1:keep", output);
            var backend = new SwitchTranslatingHardwareBackend(new RecordingHardwareBackend(), controller);

            backend.SetSwitch(1, 1, true);

            Assert.Single(output.Drives);
            Assert.Equal(SwitchPosition.Diverging, output.Drives[0].Position);
        }

        [Fact]
        public void TranslatingBackend_DeEnergisedCoilDoesNotMoveTheSwitch()
        {
            var output = new RecordingSwitchOutput();
            var controller = CreateController("1:1:keep", output);
            var backend = new SwitchTranslatingHardwareBackend(new RecordingHardwareBackend(), controller);

            backend.SetSwitch(1, 0, false);

            Assert.Empty(output.Drives);
        }

        [Fact]
        public void TranslatingBackend_ForwardsPowerAndLocoCommands()
        {
            var output = new RecordingSwitchOutput();
            var controller = CreateController("1:1:keep", output);
            var inner = new RecordingHardwareBackend();
            var backend = new SwitchTranslatingHardwareBackend(inner, controller);

            backend.SetPower(true);
            backend.SetLocoSpeed(1002, 40, 1);

            Assert.True(inner.Power);
            Assert.Equal((1002, 40, 1), inner.LocoCommands[0]);
            // Power/loco commands must not touch the switch mapping.
            Assert.Empty(output.Drives);
        }

        [Fact]
        public void TranslatingBackend_DoesNotForwardSwitchCallsToTheInnerBackend()
        {
            var output = new RecordingSwitchOutput();
            var controller = CreateController("1:1:keep", output);
            var inner = new RecordingHardwareBackend();
            var backend = new SwitchTranslatingHardwareBackend(inner, controller);

            backend.SetSwitch(1, 0, true);

            // The mapping owns the physical side; the wrapped backend must not also act.
            Assert.Empty(inner.SwitchCalls);
            Assert.Single(output.Drives);
        }
    }
}
