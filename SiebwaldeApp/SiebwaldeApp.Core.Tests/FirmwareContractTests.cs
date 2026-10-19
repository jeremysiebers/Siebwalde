using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class FirmwareContractTests
    {
        [Fact]
        public void Version100_WithExactValues_IsVerified()
        {
            var item = Amp(detected: 1, hr11: 0xE906, hr7: 0xF101, hr21: 0x0100, hr22: 0x0001);
            Assert.True(FirmwareContract.IsVerified(item));
        }

        [Fact]
        public void Version100_WithWrongHr7_IsNotVerified()
        {
            var item = Amp(detected: 1, hr11: 0xE906, hr7: 0x0000, hr21: 0x0100, hr22: 0x0001);
            Assert.False(FirmwareContract.IsVerified(item));
        }

        [Theory]
        [InlineData(0xE906, 0xF101, 0x0000, 0x0001)] // HR21 wrong
        [InlineData(0xE906, 0xF101, 0x0100, 0x0000)] // HR22 wrong
        [InlineData(0xE906, 0xF101, 0x0100, 0x0002)] // HR22 off-by-one
        public void Version100_WithMismatchedVersion_IsNotVerified(ushort hr11, ushort hr7, ushort hr21, ushort hr22)
        {
            var item = Amp(detected: 1, hr11, hr7, hr21, hr22);
            Assert.False(FirmwareContract.IsVerified(item));
        }

        [Fact]
        public void Legacy_WithIdentityVersionZero_IsVerified()
        {
            // Legacy Baseline-0 does not expose HR7/HR21/HR22; zero is the correct contract.
            var item = Amp(detected: 1, hr11: 0x251F, hr7: 0, hr21: 0, hr22: 0);
            Assert.True(FirmwareContract.IsVerified(item));
        }

        [Fact]
        public void UndetectedAmplifier_IsNotVerified()
        {
            var item = Amp(detected: 0, hr11: 0xE906, hr7: 0xF101, hr21: 0x0100, hr22: 0x0001);
            Assert.False(FirmwareContract.IsVerified(item));
        }

        [Theory]
        [InlineData(0x0000)] // undetected/unknown (all-zero registers)
        [InlineData(0x1234)] // unknown checksum
        public void UnknownChecksum_IsNotVerified(ushort hr11)
        {
            var item = Amp(detected: 1, hr11, hr7: 0, hr21: 0, hr22: 0);
            Assert.False(FirmwareContract.IsVerified(item));
        }

        [Fact]
        public void NullItem_IsNotVerified()
            => Assert.False(FirmwareContract.IsVerified(null!));

        [Theory]
        [InlineData(0xE906, true)]
        [InlineData(0x251F, true)]
        [InlineData(0x0000, false)]
        [InlineData(0x1234, false)]
        public void FromChecksum_ResolvesKnownContracts(ushort checksum, bool known)
        {
            var contract = FirmwareContract.FromChecksum(checksum);
            Assert.Equal(known, contract != null);
        }

        [Fact]
        public void FromChecksum_ReturnsExactInstances()
        {
            Assert.Same(FirmwareContract.Version100, FirmwareContract.FromChecksum(0xE906));
            Assert.Same(FirmwareContract.Legacy, FirmwareContract.FromChecksum(0x251F));
        }

        [Fact]
        public void RequiresIdentityVersion_IsTrueOnlyForVersion100()
        {
            Assert.True(FirmwareContract.Version100.RequiresIdentityVersion);
            Assert.False(FirmwareContract.Legacy.RequiresIdentityVersion);
        }

        [Fact]
        public void Version100_ExposesExactContractValues()
        {
            Assert.Equal((ushort)0xE906, FirmwareContract.Version100.TargetChecksum);
            Assert.Equal((ushort)0xF101, FirmwareContract.Version100.Identity);
            Assert.Equal((ushort)0x0100, FirmwareContract.Version100.FwVersionMajorMinor);
            Assert.Equal((ushort)0x0001, FirmwareContract.Version100.FwVersionPatchProtocol);
        }

        private static TrackAmplifierItem Amp(ushort detected, ushort hr11, ushort hr7, ushort hr21, ushort hr22)
        {
            var item = new TrackAmplifierItem();
            item.SlaveDetected = detected;
            item.HoldingReg[TrackAmplifierRegisters.SwChecksum] = hr11;
            item.HoldingReg[TrackAmplifierRegisters.Identity] = hr7;
            item.HoldingReg[TrackAmplifierRegisters.FwVersionMajorMinor] = hr21;
            item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol] = hr22;
            return item;
        }
    }
}
