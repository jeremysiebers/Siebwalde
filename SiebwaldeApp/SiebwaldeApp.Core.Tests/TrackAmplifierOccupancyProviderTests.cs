using System.Collections.Generic;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class TrackAmplifierOccupancyProviderTests
    {
        private const string OvalMapping =
            "1:1.01+1.02:1, 2:1.03+1.04:2, 3:1.05+1.06:3, 4:1.07+1.08:4, 5:1.09+1.10:5";

        private static ushort[] Registers(bool occupied)
        {
            var regs = new ushort[12];
            if (occupied)
            {
                regs[TrackAmplifierRegisters.Status] = TrackAmplifierRegisters.OccupiedBit;
            }
            return regs;
        }

        private static TrackAmplifierOccupancyProvider CreateProvider(Dictionary<ushort, ushort[]> sections)
            => new(KoploperBlockMap.Parse(OvalMapping), section =>
                sections.TryGetValue(section, out var regs) ? regs : null);

        [Fact]
        public void BlockIsOccupied_WhenItsAmplifierReportsOccupied()
        {
            var provider = CreateProvider(new Dictionary<ushort, ushort[]>
            {
                [3] = Registers(occupied: true)
            });

            Assert.True(provider.IsBlockOccupied(3));
        }

        [Fact]
        public void BlockIsFree_WhenItsAmplifierReportsFree()
        {
            var provider = CreateProvider(new Dictionary<ushort, ushort[]>
            {
                [3] = Registers(occupied: false)
            });

            Assert.False(provider.IsBlockOccupied(3));
        }

        [Fact]
        public void BlockIsFree_WhenAmplifierIsUnknown()
        {
            var provider = CreateProvider(new Dictionary<ushort, ushort[]>());

            Assert.False(provider.IsBlockOccupied(3));
        }

        [Fact]
        public void UnknownBlock_IsReportedFree()
        {
            var provider = CreateProvider(new Dictionary<ushort, ushort[]>());

            Assert.False(provider.IsBlockOccupied(99));
        }

        [Fact]
        public void BlockWithMultipleSections_IsOccupiedWhenAnySectionIsOccupied()
        {
            var map = KoploperBlockMap.Parse("10:1.11+1.12:6+7");
            var provider = new TrackAmplifierOccupancyProvider(map, section => section switch
            {
                6 => Registers(occupied: false),
                7 => Registers(occupied: true),
                _ => null
            });

            Assert.True(provider.IsBlockOccupied(10));
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
