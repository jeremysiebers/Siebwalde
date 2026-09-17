using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class AmplifierSpeedMapperTests
    {
        [Fact]
        public void SpeedZero_IsNeutral()
        {
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, AmplifierSpeedMapper.ToPwm(0, 0));
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, AmplifierSpeedMapper.ToPwm(0, 1));
        }

        [Fact]
        public void ForwardFullSpeed_IsMaxPwm()
        {
            Assert.Equal(AmplifierSpeedMapper.MaxPwm, AmplifierSpeedMapper.ToPwm(127, 0));
        }

        [Fact]
        public void ReverseFullSpeed_IsMinPwm()
        {
            Assert.Equal(AmplifierSpeedMapper.MinPwm, AmplifierSpeedMapper.ToPwm(127, 1));
        }

        [Fact]
        public void ForwardLowSpeed_IsAboveNeutral()
        {
            var pwm = AmplifierSpeedMapper.ToPwm(1, 0);

            Assert.True(pwm > AmplifierSpeedMapper.NeutralPwm);
            Assert.True(pwm <= AmplifierSpeedMapper.MaxPwm);
        }

        [Fact]
        public void ReverseLowSpeed_IsBelowNeutral()
        {
            var pwm = AmplifierSpeedMapper.ToPwm(1, 1);

            Assert.True(pwm < AmplifierSpeedMapper.NeutralPwm);
            Assert.True(pwm >= AmplifierSpeedMapper.MinPwm);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(64, 0)]
        [InlineData(64, 1)]
        [InlineData(127, 0)]
        [InlineData(127, 1)]
        public void PwmIsNeverZero(int speed, int direction)
        {
            Assert.NotEqual(0, AmplifierSpeedMapper.ToPwm(speed, direction));
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(1000)]
        public void OutOfRangeSpeed_IsClamped(int speed)
        {
            var forward = AmplifierSpeedMapper.ToPwm(speed, 0);
            var reverse = AmplifierSpeedMapper.ToPwm(speed, 1);

            Assert.InRange(forward, AmplifierSpeedMapper.MinPwm, AmplifierSpeedMapper.MaxPwm);
            Assert.InRange(reverse, AmplifierSpeedMapper.MinPwm, AmplifierSpeedMapper.MaxPwm);
        }
    }
}
