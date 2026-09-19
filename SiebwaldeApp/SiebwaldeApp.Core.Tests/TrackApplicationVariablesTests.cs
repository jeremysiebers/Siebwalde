using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class TrackApplicationVariablesTests
    {
        [Fact]
        public void SetDesiredAmplifierControl_IgnoresSlaveZero()
        {
            var variables = new TrackApplicationVariables();

            variables.SetDesiredAmplifierControl(0, 400, false);

            Assert.Empty(variables.PendingWrites);
        }

        [Theory]
        [InlineData(-5, 0)]
        [InlineData(0, 0)]
        [InlineData(400, 400)]
        [InlineData(799, 799)]
        [InlineData(1000, 799)]
        public void SetDesiredAmplifierControl_ClampsPwmTo0To799(int pwm, int expected)
        {
            var variables = new TrackApplicationVariables();

            // Move away from the initial 0 first so the pending flag is set.
            variables.SetDesiredAmplifierControl(1, 500, false);
            variables.PendingWrites[1].TryConsumeHr0(out _);

            variables.SetDesiredAmplifierControl(1, pwm, false);

            Assert.True(variables.PendingWrites[1].TryConsumeHr0(out var hr0));
            Assert.Equal(expected, hr0 & 0x03FF);
            Assert.Equal(0, hr0 & (1 << 15));
        }

        [Fact]
        public void SetDesiredAmplifierControl_ZeroOnFreshAmplifier_IsNotPending()
        {
            // Documents current behavior: a new write item starts at Hr0Value 0, so requesting
            // PWM 0 on a fresh amplifier is treated as "no change" and is not queued.
            var variables = new TrackApplicationVariables();

            variables.SetDesiredAmplifierControl(1, 0, false);

            Assert.False(variables.PendingWrites[1].TryConsumeHr0(out _));
        }

        [Fact]
        public void SetDesiredAmplifierControl_SetsEmoStopBit15()
        {
            var variables = new TrackApplicationVariables();

            variables.SetDesiredAmplifierControl(7, 100, true);

            Assert.True(variables.PendingWrites[7].TryConsumeHr0(out var hr0));
            Assert.Equal(100, hr0 & 0x03FF);
            Assert.Equal(1 << 15, hr0 & (1 << 15));
        }

        [Fact]
        public void SetDesiredAmplifierControl_MarksPendingOnlyOnChange()
        {
            var variables = new TrackApplicationVariables();

            variables.SetDesiredAmplifierControl(3, 200, false);
            Assert.True(variables.PendingWrites[3].TryConsumeHr0(out _));

            // Same value again must not create a new pending write.
            variables.SetDesiredAmplifierControl(3, 200, false);
            Assert.False(variables.PendingWrites[3].TryConsumeHr0(out _));
        }

        [Fact]
        public void SetDesiredAmplifierControl_StoresSeparateWritesPerSlave()
        {
            var variables = new TrackApplicationVariables();

            variables.SetDesiredAmplifierControl(1, 100, false);
            variables.SetDesiredAmplifierControl(2, 200, false);

            Assert.Equal(2, variables.PendingWrites.Count);
            Assert.Equal(100, variables.PendingWrites[1].Hr0Value & 0x03FF);
            Assert.Equal(200, variables.PendingWrites[2].Hr0Value & 0x03FF);
        }

        [Fact]
        public void InitializeDefaultPwmSetpoints_SetsBits0To9()
        {
            var variables = new TrackApplicationVariables();

            variables.InitializeDefaultPwmSetpoints(400);

            Assert.Equal(400, variables.trackAmpItems[0].HoldingReg[0] & 0x03FF);
        }

        [Fact]
        public void InitializeDefaultPwmSetpoints_ClampsAbove799()
        {
            var variables = new TrackApplicationVariables();

            variables.InitializeDefaultPwmSetpoints(5000);

            Assert.Equal(799, variables.trackAmpItems[0].HoldingReg[0] & 0x03FF);
        }
    }
}
