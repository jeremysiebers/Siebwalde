using System.Collections.Generic;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Real occupancy provider: reads the existing amplifier holding registers (HR_STATUS bit 10)
    /// and distinguishes "clear" from "unknown".
    /// </summary>
    public class TrackAmplifierOccupancyProviderTests
    {
        private const string OvalMapping =
            "1:1.01+1.02:1, 2:1.03+1.04:2, 3:1.05+1.06:3, 4:1.07+1.08:4, 5:1.09+1.10:5";

        /// <summary>Builds an amplifier as the comm client delivers it: registers plus detection.</summary>
        private static TrackAmplifierItem Amplifier(ushort section, bool occupied, bool detected = true)
        {
            var regs = new ushort[12];
            if (occupied)
            {
                regs[TrackAmplifierRegisters.Status] = TrackAmplifierRegisters.OccupiedBit;
            }

            return new TrackAmplifierItem
            {
                SlaveNumber = section,
                SlaveDetected = detected ? (ushort)1 : (ushort)0,
                HoldingReg = regs
            };
        }

        private static TrackAmplifierOccupancyProvider CreateProvider(Dictionary<ushort, TrackAmplifierItem> sections)
            => new(KoploperBlockMap.Parse(OvalMapping), section =>
                sections.TryGetValue(section, out var amplifier) ? amplifier : null);

        // ---------------------------------------------------------------------
        // Occupancy value
        // ---------------------------------------------------------------------

        [Fact]
        public void BlockIsOccupied_WhenItsAmplifierReportsOccupied()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Amplifier(3, occupied: true)
            });

            Assert.True(provider.IsBlockOccupied(3));
        }

        [Fact]
        public void BlockIsClear_WhenItsAmplifierReportsClear()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Amplifier(3, occupied: false)
            });

            Assert.False(provider.IsBlockOccupied(3));
        }

        [Fact]
        public void UnrelatedStatusBits_DoNotAffectOccupancy()
        {
            var amplifier = Amplifier(3, occupied: false);
            var regs = amplifier.HoldingReg;

            // Thermal, overcurrent and ID-set are set, the occupied bit is not.
            regs[TrackAmplifierRegisters.Status] = (ushort)(
                TrackAmplifierRegisters.ThermalBit |
                TrackAmplifierRegisters.OverCurrentBit |
                TrackAmplifierRegisters.IdSetBit);
            amplifier.HoldingReg = regs;

            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem> { [3] = amplifier });

            Assert.False(provider.IsBlockOccupied(3));
            Assert.True(provider.IsBlockOccupancyKnown(3));
        }

        [Fact]
        public void OccupancyUsesTheCorrectAmplifierForTheBlock()
        {
            // Block 4 is covered by section 4 only; section 3 being occupied must not leak into it.
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Amplifier(3, occupied: true),
                [4] = Amplifier(4, occupied: false)
            });

            Assert.True(provider.IsBlockOccupied(3));
            Assert.False(provider.IsBlockOccupied(4));
        }

        [Fact]
        public void BlockWithMultipleSections_IsOccupiedWhenAnySectionIsOccupied()
        {
            var map = KoploperBlockMap.Parse("10:1.11+1.12:6+7");
            var provider = new TrackAmplifierOccupancyProvider(map, section => section switch
            {
                6 => Amplifier(6, occupied: false),
                7 => Amplifier(7, occupied: true),
                _ => null
            });

            Assert.True(provider.IsBlockOccupied(10));
        }

        // ---------------------------------------------------------------------
        // Known versus unknown (unknown must never be read as clear)
        // ---------------------------------------------------------------------

        [Fact]
        public void OccupancyIsKnown_WhenValidAmplifierDataHasBeenReceived()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Amplifier(3, occupied: false)
            });

            Assert.True(provider.IsBlockOccupancyKnown(3));
        }

        [Fact]
        public void OccupancyIsUnknown_WhenNoAmplifierDataHasBeenReceived()
        {
            // No frame parsed yet: the registers still hold their initial values.
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>());

            Assert.False(provider.IsBlockOccupancyKnown(3));
            Assert.False(provider.IsBlockOccupied(3));
        }

        [Fact]
        public void OccupancyIsUnknown_WhenTheAmplifierWasNotDetected()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Amplifier(3, occupied: false, detected: false)
            });

            Assert.False(provider.IsBlockOccupancyKnown(3));
        }

        [Fact]
        public void OccupancyIsUnknown_WhenOnlyOneOfTwoSectionsIsReporting()
        {
            var map = KoploperBlockMap.Parse("10:1.11+1.12:6+7");
            var provider = new TrackAmplifierOccupancyProvider(map, section => section switch
            {
                6 => Amplifier(6, occupied: false),
                7 => null,
                _ => null
            });

            // One silent section is enough: the block cannot be declared clear.
            Assert.False(provider.IsBlockOccupancyKnown(10));
        }

        [Fact]
        public void OccupancyIsUnknown_ForAnUnmappedBlock()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>());

            Assert.False(provider.IsBlockOccupancyKnown(99));
            Assert.False(provider.IsBlockOccupied(99));
        }

        [Fact]
        public void OccupiedBlockIsAlsoKnown()
        {
            // An occupied observation is itself a positive, valid observation.
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Amplifier(3, occupied: true)
            });

            Assert.True(provider.IsBlockOccupancyKnown(3));
        }

        [Fact]
        public void OccupiedBit_MatchesGeneralHeader()
        {
            // HR_STATUS bit 10 per TrackAmplifier4.X/modbus/General.h
            Assert.Equal(1 << 10, TrackAmplifierRegisters.OccupiedBit);
            Assert.Equal(2, TrackAmplifierRegisters.Status);
        }
    }
}
