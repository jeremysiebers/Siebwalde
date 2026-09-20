using SiebwaldeApp.EcosEmu;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Regression tests for the confirmed DCC28 speed-scaling defect: a protocol-specific speed
    /// step must be normalized to the 0..127 domain before it reaches the hardware backend.
    /// </summary>
    public class ProtocolSpeedNormalizerTests
    {
        [Fact]
        public void Dcc28_Stop_NormalizesToZero()
        {
            Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", 0, out var normalized));
            Assert.Equal(0, normalized);
        }

        [Fact]
        public void Dcc28_FullThrottle_NormalizesToNormalizedMax()
        {
            Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", 28, out var normalized));
            Assert.Equal(127, normalized);
        }

        [Fact]
        public void Dcc28_LowStep_IsScaled()
        {
            // round(1 * 127 / 28) = round(4.54) = 5
            Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", 1, out var normalized));
            Assert.Equal(5, normalized);
        }

        [Fact]
        public void Dcc28_HalfThrottle_IsAboutHalfRange()
        {
            // round(14 * 127 / 28) = 63.5, round-half-up -> 64
            Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", 14, out var normalized));
            Assert.Equal(64, normalized);
        }

        [Fact]
        public void Dcc28_EveryStep_IsMonotonicAndInsideNormalizedRange()
        {
            var previous = -1;

            for (var step = 0; step <= ProtocolSpeedNormalizer.Dcc28MaxStep; step++)
            {
                Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", step, out var normalized));
                Assert.InRange(normalized, 0, ProtocolSpeedNormalizer.NormalizedMaxSpeed);
                Assert.True(normalized >= previous, $"Step {step} normalized to {normalized}, below previous {previous}.");
                previous = normalized;
            }

            Assert.Equal(127, previous);
        }

        [Theory]
        [InlineData(29)]
        [InlineData(127)]
        [InlineData(1000)]
        public void Dcc28_StepAboveRange_IsClampedToNormalizedMax(int step)
        {
            Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", step, out var normalized));
            Assert.Equal(127, normalized);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(64, 64)]
        [InlineData(100, 100)]
        [InlineData(127, 127)]
        public void Dcc128_IsAlreadyNormalized_AndIsNotScaledTwice(int step, int expected)
        {
            Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC128", step, out var normalized));
            Assert.Equal(expected, normalized);
        }

        [Fact]
        public void Dcc128_StepAboveRange_IsClamped()
        {
            Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC128", 1000, out var normalized));
            Assert.Equal(127, normalized);
        }

        [Theory]
        [InlineData("dcc28")]
        [InlineData("Dcc28")]
        [InlineData(" DCC28 ")]
        public void ProtocolName_IsCaseAndWhitespaceInsensitive(string protocol)
        {
            Assert.True(ProtocolSpeedNormalizer.TryNormalize(protocol, 28, out var normalized));
            Assert.Equal(127, normalized);
        }

        [Theory]
        [InlineData("MMF")]
        [InlineData("DCC14")]
        [InlineData("")]
        [InlineData(null)]
        public void UnknownProtocol_NonZeroStep_IsRejected(string? protocol)
        {
            Assert.False(ProtocolSpeedNormalizer.TryNormalize(protocol, 10, out var normalized));
            Assert.Equal(0, normalized);
        }

        [Theory]
        [InlineData("MMF")]
        [InlineData("DCC14")]
        [InlineData("")]
        [InlineData(null)]
        public void UnknownProtocol_Stop_IsStillAccepted(string? protocol)
        {
            Assert.True(ProtocolSpeedNormalizer.TryNormalize(protocol, 0, out var normalized));
            Assert.Equal(0, normalized);
        }

        [Fact]
        public void ScaleStep_UsesRoundHalfUp()
        {
            // 14 * 127 / 28 = 63.5 exactly; round-half-up yields 64.
            Assert.Equal(64, ProtocolSpeedNormalizer.ScaleStep(14, 28));
            // 2 * 127 / 28 = 9.07 -> 9
            Assert.Equal(9, ProtocolSpeedNormalizer.ScaleStep(2, 28));
        }
    }
}
