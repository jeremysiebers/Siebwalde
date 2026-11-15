using System;
using System.Collections.Generic;
using System.Threading;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
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
        public void FreshnessPolicy_ResampleRejectsGenuinelyFutureTimestamp()
        {
            // A received timestamp genuinely ahead of the real current clock must still be
            // rejected: the re-sample only closes a sub-tick read-order race, it does not
            // accept a clock that genuinely moved backwards.
            var now = DateTimeOffset.UtcNow;
            var received = now + TimeSpan.FromMinutes(5);

            Assert.False(TrackAmplifierDataFreshness.IsFresh(
                Amplifier(3, false, receivedAt: received), now, TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void FreshnessPolicy_ResampleAcceptsSubMicrosecondAheadTimestamp()
        {
            // A stamp only a few ticks ahead of the caller's `now` (the read-order race, sub-µs)
            // is genuinely fresh, not a backward clock: it must be accepted after the re-sample.
            var now = DateTimeOffset.UtcNow;
            var received = now + TimeSpan.FromTicks(1);

            Assert.True(TrackAmplifierDataFreshness.IsFresh(
                Amplifier(3, false, receivedAt: received), now, TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void FreshnessPolicy_ConcurrentStamper_ReadOrderRace_IsAlwaysFresh()
        {
            // Deterministically reproduces the read-order TOCTOU behind the residual
            // Stage4DiagnosticPauseTests flake: a stamper thread re-stamps
            // LastDataReceivedUtc continuously while a checker samples `now` FIRST, then reads
            // the stamp via IsFresh — exactly the real caller order. A stamp landing in that
            // window makes a genuinely-fresh stamp a few ticks ahead of the checker's `now`, so
            // the un-fixed guard spuriously reports stale. The re-sample closes the race.
            var amplifier = new TrackAmplifierItem { SlaveNumber = 3 };
            amplifier.LastDataReceivedUtc = DateTimeOffset.UtcNow; // pre-stamp so the first read is fresh

            bool stop = false;
            var stamper = new Thread(() =>
            {
                while (!Volatile.Read(ref stop))
                {
                    amplifier.LastDataReceivedUtc = DateTimeOffset.UtcNow;
                }
            })
            {
                IsBackground = true,
                Name = "freshness-stamper"
            };
            stamper.Start();

            try
            {
                for (int i = 0; i < 100_000; i++)
                {
                    var now = DateTimeOffset.UtcNow; // sample `now` FIRST, matching the real caller
                    Assert.True(
                        TrackAmplifierDataFreshness.IsFresh(amplifier, now, TimeSpan.FromSeconds(2)),
                        "A freshly-stamped amplifier must never read as stale (read-order TOCTOU).");
                }
            }
            finally
            {
                Volatile.Write(ref stop, true);
                stamper.Join();
            }
        }

        [Fact]
        public void OccupiedBit_MatchesFrozenPhase1Map()
        {
            // R8-4: HR_VALIDITY bit 8 is occupancy-live.
            Assert.Equal(1 << 8, TrackAmplifierRegisters.OccupiedBit);
            Assert.Equal(10, TrackAmplifierRegisters.Status);
        }

        // ---------------------------------------------------------------------
        // Per-amplifier occupancy (IAmplifierOccupancyProvider, KIS10-06)
        // ---------------------------------------------------------------------

        [Fact]
        public void GetAmplifierOccupancy_FreshOccupied_ReturnsOccupied()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Fresh(3, occupied: true)
            });

            Assert.Equal(PhysicalOccupancy.Occupied, provider.GetAmplifierOccupancy(3));
        }

        [Fact]
        public void GetAmplifierOccupancy_FreshClear_ReturnsClear()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Fresh(3, occupied: false)
            });

            Assert.Equal(PhysicalOccupancy.Clear, provider.GetAmplifierOccupancy(3));
        }

        [Fact]
        public void GetAmplifierOccupancy_StaleOrMissing_ReturnsUnknown()
        {
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Amplifier(3, occupied: false, receivedAt: Stale)
            });

            Assert.Equal(PhysicalOccupancy.Unknown, provider.GetAmplifierOccupancy(3));
            Assert.Equal(PhysicalOccupancy.Unknown, provider.GetAmplifierOccupancy(99));
        }

        [Fact]
        public void GetAmplifierOccupancy_BackplaneAddress_ReturnsUnknown()
        {
            // KIS-09 invariant: backplane/configuration addresses (51..55) are never track sections.
            var provider = CreateProvider(new Dictionary<ushort, TrackAmplifierItem>
            {
                [3] = Fresh(3, occupied: true)
            });

            Assert.Equal(PhysicalOccupancy.Unknown, provider.GetAmplifierOccupancy(51));
            Assert.Equal(PhysicalOccupancy.Unknown, provider.GetAmplifierOccupancy(55));
        }

        [Fact]
        public void PerSectionAndBlockLevelDerivedStatus_CoexistCorrectly()
        {
            // Block 10 spans sections/amps 6 and 7: section 6 occupied, section 7 clear.
            var provider = CreateTwoSectionProvider(Fresh(6, true), Fresh(7, false));

            // Per-section: each amplifier reflects its own section (no aggregation).
            Assert.Equal(PhysicalOccupancy.Occupied, provider.GetAmplifierOccupancy(6));
            Assert.Equal(PhysicalOccupancy.Clear, provider.GetAmplifierOccupancy(7));

            // Block-level derived status remains correct for look-ahead/safety.
            Assert.True(provider.IsBlockOccupied(10));
            Assert.True(provider.IsBlockOccupancyKnown(10));
        }
    }
}
