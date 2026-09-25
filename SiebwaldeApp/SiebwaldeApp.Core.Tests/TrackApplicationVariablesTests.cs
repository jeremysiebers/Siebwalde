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

            // Slave 1 is the first legitimate track amplifier; slave 0 is not a track amplifier.
            Assert.Equal(400, variables.trackAmpItems[1].HoldingReg[0] & 0x03FF);
        }

        [Fact]
        public void InitializeDefaultPwmSetpoints_ClampsAbove799()
        {
            var variables = new TrackApplicationVariables();

            variables.InitializeDefaultPwmSetpoints(5000);

            Assert.Equal(799, variables.trackAmpItems[1].HoldingReg[0] & 0x03FF);
        }

        [Fact]
        public void InitializeDefaultPwmSetpoints_DoesNotTouchBackplaneConfigurationSlaves()
        {
            var variables = new TrackApplicationVariables();
            var backplane = variables.trackAmpItems[51];
            backplane.HoldingReg[0] = 0x1234;

            variables.InitializeDefaultPwmSetpoints(400);

            // A backplane/configuration slave's HoldingReg0 is not a PWM setpoint.
            Assert.Equal(0x1234, backplane.HoldingReg[0]);
        }

        // -----------------------------------------------------------------
        // Movement-permission gate
        // -----------------------------------------------------------------

        [Fact]
        public void SetDesiredAmplifierControl_RefusesNonNeutral_WhenPermissionNotGranted()
        {
            var variables = new TrackApplicationVariables();
            variables.MovementPermission = new MovementPermissionController(); // NotGranted

            var accepted = variables.SetDesiredAmplifierControl(1, 400, false);

            Assert.False(accepted);
            Assert.False(variables.PendingWrites.ContainsKey(1));
        }

        [Fact]
        public void SetDesiredAmplifierControl_AcceptsNeutral_WhenPermissionNotGranted()
        {
            var variables = new TrackApplicationVariables();
            variables.MovementPermission = new MovementPermissionController(); // NotGranted

            var accepted = variables.SetDesiredAmplifierControl(1, AmplifierSpeedMapper.NeutralPwm, false);

            Assert.True(accepted);
            Assert.True(variables.PendingWrites[1].TryConsumeHr0(out var hr0));
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, hr0 & 0x03FF);
        }

        [Fact]
        public void SetDesiredAmplifierControl_AcceptsNonNeutral_WhenPermissionGranted()
        {
            var variables = new TrackApplicationVariables();
            var permission = new MovementPermissionController();
            permission.Grant();
            variables.MovementPermission = permission;

            var accepted = variables.SetDesiredAmplifierControl(1, 400, false);

            Assert.True(accepted);
            Assert.True(variables.PendingWrites[1].TryConsumeHr0(out var hr0));
            Assert.Equal(400, hr0 & 0x03FF);
        }

        [Fact]
        public void SetDesiredAmplifierControl_WithdrawnPermission_RefusesNonNeutral()
        {
            var variables = new TrackApplicationVariables();
            var permission = new MovementPermissionController();
            permission.Grant();
            permission.Withdraw();
            variables.MovementPermission = permission;

            var accepted = variables.SetDesiredAmplifierControl(1, 500, false);

            Assert.False(accepted);
            Assert.False(variables.PendingWrites.ContainsKey(1));
        }

        [Fact]
        public void SetDesiredAmplifierControl_NullPermission_DoesNotGate()
        {
            // No permission set: the gate is disabled, preserving pre-gate behavior.
            var variables = new TrackApplicationVariables();

            Assert.True(variables.SetDesiredAmplifierControl(1, 500, false));
        }

        [Fact]
        public void SetDesiredAmplifierControl_NonTrackAddress_ReturnsFalse()
        {
            var variables = new TrackApplicationVariables();
            variables.MovementPermission = new MovementPermissionController();

            Assert.False(variables.SetDesiredAmplifierControl(51, 500, false));
            Assert.False(variables.PendingWrites.ContainsKey(51));
        }

        // -----------------------------------------------------------------
        // HoldingReg isolation
        // -----------------------------------------------------------------

        [Fact]
        public void HoldingReg_MutatingOneItem_DoesNotAffectAnother()
        {
            var variables = new TrackApplicationVariables();

            var item1 = variables.trackAmpItems[1];
            var item2 = variables.trackAmpItems[2];

            item1.HoldingReg[0] = 0x0123;

            Assert.Equal(0x0123, item1.HoldingReg[0]);
            Assert.NotEqual(0x0123, item2.HoldingReg[0]);
        }
    }
}
