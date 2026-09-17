using System;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class TrackAmplifierHardwareBackendTests
    {
        private sealed class FakeBlockPositionProvider : IBlockPositionProvider
        {
            private readonly int? _block;

            public FakeBlockPositionProvider(int? block) => _block = block;

            public event Action<int, int>? BlockEntered;

            public int? TryGetBlockForLoc(int loc) => _block;
        }

        private static TrackAmplifierHardwareBackend CreateBackend(
            int? block,
            string topology,
            out TrackApplicationVariables variables)
        {
            variables = new TrackApplicationVariables();
            return new TrackAmplifierHardwareBackend(
                new FakeBlockPositionProvider(block),
                BlockTopology.Parse(topology),
                variables);
        }

        [Fact]
        public void SetLocoSpeed_ForwardsToAmplifierOfCurrentBlock()
        {
            var backend = CreateBackend(2, "1:1,2:2", out var variables);

            backend.SetLocoSpeed(address: 42, ecosSpeed: 127, direction: 0);

            Assert.True(variables.PendingWrites[2].TryConsumeHr0(out var hr0));
            Assert.Equal(AmplifierSpeedMapper.MaxPwm, hr0 & 0x03FF);
        }

        [Fact]
        public void SetLocoSpeed_ReverseDirection_ProducesReversePwm()
        {
            var backend = CreateBackend(1, "1:1", out var variables);

            backend.SetLocoSpeed(address: 42, ecosSpeed: 127, direction: 1);

            Assert.True(variables.PendingWrites[1].TryConsumeHr0(out var hr0));
            Assert.Equal(AmplifierSpeedMapper.MinPwm, hr0 & 0x03FF);
        }

        [Fact]
        public void SetLocoSpeed_UnknownBlock_IsIgnored()
        {
            var backend = CreateBackend(null, "1:1", out var variables);

            backend.SetLocoSpeed(address: 42, ecosSpeed: 100, direction: 0);

            Assert.Empty(variables.PendingWrites);
        }

        [Fact]
        public void SetLocoSpeed_BlockWithoutMapping_IsIgnored()
        {
            var backend = CreateBackend(9, "1:1", out var variables);

            backend.SetLocoSpeed(address: 42, ecosSpeed: 100, direction: 0);

            Assert.Empty(variables.PendingWrites);
        }

        [Fact]
        public void SetLocoSpeed_MultipleAmplifiersPerBlock_AllGetTheSetpoint()
        {
            var backend = CreateBackend(10, "10:1+2", out var variables);

            backend.SetLocoSpeed(address: 42, ecosSpeed: 64, direction: 0);

            Assert.Equal(2, variables.PendingWrites.Count);
            Assert.Equal(variables.PendingWrites[1].Hr0Value, variables.PendingWrites[2].Hr0Value);
        }

        [Fact]
        public void SetPowerOff_SetsMappedAmplifiersToNeutral()
        {
            var backend = CreateBackend(1, "1:1,2:2", out var variables);

            backend.SetPower(false);

            Assert.True(variables.PendingWrites[1].TryConsumeHr0(out var hr0Amp1));
            Assert.True(variables.PendingWrites[2].TryConsumeHr0(out var hr0Amp2));
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, hr0Amp1 & 0x03FF);
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, hr0Amp2 & 0x03FF);
        }

        [Fact]
        public void SetPowerOn_DoesNotQueueWrites()
        {
            var backend = CreateBackend(1, "1:1", out var variables);

            backend.SetPower(true);

            Assert.Empty(variables.PendingWrites);
        }
    }
}
