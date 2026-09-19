using System;
using System.Collections.Generic;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Real occupancy provider: reads the existing amplifier holding registers (HR_STATUS bit 10)
    /// and distinguishes known-clear from unknown, including data staleness.
    /// </summary>
    public class TrackAmplifierOccupancyProviderTests
    {
        private const string OvalMapping =
            "1:1.01+1.02:1, 2:1.03+1.04:2, 3:1.05+1.06:3, 4:1.07+1.08:4, 5:1.09+1.10:5";

        /// <summary>Fixed clock so freshness is deterministic in tests.</summary>
        private static readonly DateTimeOffset Now = new(2026, 9, 19, 20, 0, 0, TimeSpan.Zero);

        /// <summary>Comfortably older than the staleness policy.</summary>
        private static DateTimeOffset Stale => Now - TimeSpan.FromSeconds(30);

        /// <summary>Builds an amplifier as the comm client delivers it: registers, detection and the frame timestamp.</summary>
        private static TrackAmplifierItem Amplifier(
            ushort section,
            bool occupied,
            DateTimeOffset? receivedAt = null,
            bool detected = true)
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
                HoldingReg = regs,
                LastDataReceivedUtc = receivedAt
            };
        }

        private static TrackAmplifierOccupancyProvider CreateProvider(Dictionary<ushort, TrackAmplifierItem> sections)
            => new(
                KoploperBlockMap.Parse(OvalMapping),
                section => sections.TryGetValue(section, out var amplifier) ? amplifier : null,
                clock: () => Now);

        private static TrackAmplifierItem Fresh(ushort section, bool occupied)
            => Amplifier(section, occupied, receivedAt: Now);

        // ---------------------------------------------------------------------
        // Fresh data
        // ---------------------------------------------------------------------

        [Fact]
        public void FreshOccupiedData_IsOccupied()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Fresh(3, occupied: true)
            });

            Assert.True(provider.IsBlockOccupied(3));
            Assert.True(provider.IsBlockOccupancyKnown(3));
        }

        [Fact]
        public void FreshClearData_IsKnownClear()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Fresh(3, occupied: false)
            });

            Assert.False(provider.IsBlockOccupied(3));
            Assert.True(provider.IsBlockOccupancyKnown(3));
        }

        [Fact]
        public void UnrelatedStatusBits_DoNotAffectOccupancy()
        {
            var amplifier = Fresh(3, occupied: false);
            var regs = amplifier.HoldingReg;

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
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Fresh(3, occupied: true),
                [4] = Fresh(4, occupied: false)
            });

            Assert.True(provider.IsBlockOccupied(3));
            Assert.False(provider.IsBlockOccupied(4));
            Assert.True(provider.IsBlockOccupancyKnown(4));
        }

        // ---------------------------------------------------------------------
        // Never received and stale data are unknown, never clear
        // ---------------------------------------------------------------------

        [Fact]
        public void NeverReceivedData_IsUnknown()
        {
            // No frame parsed yet: registers still hold their initial values and there is no timestamp.
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>());

            Assert.False(provider.IsBlockOccupancyKnown(3));
            Assert.False(provider.IsBlockOccupied(3));
        }

        [Fact]
        public void StaleClearData_IsUnknownAndNotKnownClear()
        {
            // Was clear, then communication stopped.
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Amplifier(3, occupied: false, receivedAt: Stale)
            });

            Assert.False(provider.IsBlockOccupancyKnown(3));
            Assert.False(provider.IsBlockOccupied(3));
        }

        [Fact]
        public void StaleOccupiedData_IsNotPromotedToDefiniteOccupancy()
        {
            // Conservative semantics: a stale reading may describe a train that already left, so it
            // neither proves occupancy nor proves clear.
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Amplifier(3, occupied: true, receivedAt: Stale)
            });

            Assert.False(provider.IsBlockOccupied(3));
            Assert.False(provider.IsBlockOccupancyKnown(3));
        }

        [Fact]
        public void DataIsUnknown_WhenTheAmplifierWasNotDetected()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Amplifier(3, occupied: false, receivedAt: Now, detected: false)
            });

            Assert.False(provider.IsBlockOccupancyKnown(3));
        }

        [Fact]
        public void DataIsUnknown_ForAnUnmappedBlock()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>());

            Assert.False(provider.IsBlockOccupancyKnown(99));
            Assert.False(provider.IsBlockOccupied(99));
        }

        // ---------------------------------------------------------------------
        // Multi-section blocks
        // ---------------------------------------------------------------------

        private static TrackAmplifierOccupancyProvider CreateTwoSectionProvider(
            TrackAmplifierItem? section6,
            TrackAmplifierItem? section7)
            => new(
                KoploperBlockMap.Parse("10:1.11+1.12:6+7"),
                section => section switch
                {
                    6 => section6,
                    7 => section7,
                    _ => null
                },
                clock: () => Now);

        [Fact]
        public void MultiSection_AllFreshClear_IsKnownClear()
        {
            var provider = CreateTwoSectionProvider(Fresh(6, false), Fresh(7, false));

            Assert.False(provider.IsBlockOccupied(10));
            Assert.True(provider.IsBlockOccupancyKnown(10));
        }

        [Fact]
        public void MultiSection_OneStaleAndNoneOccupied_IsUnknown()
        {
            var provider = CreateTwoSectionProvider(Fresh(6, false), Amplifier(7, false, receivedAt: Stale));

            Assert.False(provider.IsBlockOccupied(10));
            Assert.False(provider.IsBlockOccupancyKnown(10));
        }

        [Fact]
        public void MultiSection_OneFreshOccupiedAndOneUnknown_IsOccupied()
        {
            // Definite occupancy from a fresh section must not be turned into unknown.
            var provider = CreateTwoSectionProvider(Fresh(6, true), Amplifier(7, false, receivedAt: Stale));

            Assert.True(provider.IsBlockOccupied(10));
            Assert.True(provider.IsBlockOccupancyKnown(10));
        }

        [Fact]
        public void MultiSection_OneFreshOccupiedAndOneMissing_IsOccupied()
        {
            var provider = CreateTwoSectionProvider(Fresh(6, true), section7: null);

            Assert.True(provider.IsBlockOccupied(10));
            Assert.True(provider.IsBlockOccupancyKnown(10));
        }

        // ---------------------------------------------------------------------
        // Freshness policy
        // ---------------------------------------------------------------------

        [Fact]
        public void FreshnessPolicy_RejectsMissingAndOldTimestamps()
        {
            Assert.False(TrackAmplifierDataFreshness.IsFresh(null, Now, TimeSpan.FromSeconds(2)));
            Assert.False(TrackAmplifierDataFreshness.IsFresh(
                Amplifier(3, false, receivedAt: null), Now, TimeSpan.FromSeconds(2)));
            Assert.False(TrackAmplifierDataFreshness.IsFresh(
                Amplifier(3, false, receivedAt: Stale), Now, TimeSpan.FromSeconds(2)));
            Assert.True(TrackAmplifierDataFreshness.IsFresh(
                Amplifier(3, false, receivedAt: Now), Now, TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void FreshnessPolicy_RejectsATimestampFromTheFuture()
        {
            // A clock that moved backwards must not make old data look fresh.
            var future = Now + TimeSpan.FromMinutes(5);

            Assert.False(TrackAmplifierDataFreshness.IsFresh(
                Amplifier(3, false, receivedAt: future), Now, TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void FreshnessPolicy_AcceptsDataExactlyAtTheLimit()
        {
            var atLimit = Now - TimeSpan.FromSeconds(2);

            Assert.True(TrackAmplifierDataFreshness.IsFresh(
                Amplifier(3, false, receivedAt: atLimit), Now, TimeSpan.FromSeconds(2)));
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
