using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class SetDefaultPwmSetpointsStepTests
    {
        [Fact]
        public async Task ExecuteAsync_WritesNeutralSetpoint_Not400()
        {
            var variables = new TrackApplicationVariables();
            var step = new SetDefaultPwmSetpointsStep(variables, "test");

            var result = await step.ExecuteAsync(null, CancellationToken.None);

            Assert.Equal(InitStepResultKind.NextStep, result.Kind);
            Assert.Equal("EnableTrackamplifiers", result.NextStepName);

            // The idle/standstill default is the neutral PWM, not the old 400.
            Assert.Equal(
                AmplifierSpeedMapper.NeutralPwm,
                variables.trackAmpItems[1].HoldingReg[0] & 0x03FF);
        }
    }
}
