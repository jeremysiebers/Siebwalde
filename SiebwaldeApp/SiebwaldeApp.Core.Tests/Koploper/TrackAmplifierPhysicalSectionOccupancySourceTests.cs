using System;
using System.Collections.Generic;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for the track-amplifier physical section occupancy source. These run the
    /// production <see cref="TrackAmplifierPhysicalSectionOccupancySource"/> with injected clock and
    /// generation providers — there is no test-only bypass.
    /// </summary>
    public class TrackAmplifierPhysicalSectionOccupancySourceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

        private static readonly LayoutPhysicalAmplifierBinding[] Mapping =
        {
            new() { SectionId = 1, PhysicalAmplifier = 1 },
            new() { SectionId = 2, PhysicalAmplifier = 3 }
        };

        private static TrackAmplifierItem Amplifier(ushort slave, bool occupied, DateTimeOffset? receivedAt = null)
        {
            var regs = new ushort[12];
            if (occupied)
            {
                regs[TrackAmplifierRegisters.Status] = TrackAmplifierRegisters.OccupiedBit;
            }

            return new TrackAmplifierItem
            {
                SlaveNumber = slave,
                SlaveDetected = 1,
                HoldingReg = regs,
                LastDataReceivedUtc = receivedAt
            };
        }

        private static Func<ushort, TrackAmplifierItem?> Lookup(IReadOnlyDictionary<ushort, TrackAmplifierItem> items)
            => slave => items.TryGetValue(slave, out TrackAmplifierItem? item) ? item : null;

        [Fact]
        public void Read_AdvancesSequenceEachCall()
        {
            var source = new TrackAmplifierPhysicalSectionOccupancySource(
                "simple-loop",
                Mapping,
                Lookup(new Dictionary<ushort, TrackAmplifierItem>
                {
                    [1] = Amplifier(1, occupied: false, receivedAt: Now),
                    [3] = Amplifier(3, occupied: false, receivedAt: Now)
                }),
                clock: () => Now);

            var first = source.Read();
            var second = source.Read();

            Assert.Equal(1, first.Sequence);
            Assert.Equal(2, second.Sequence);
        }

        [Fact]
        public void Read_UsesGenerationProvider()
        {
            const long generation = 987654321;
            var source = new TrackAmplifierPhysicalSectionOccupancySource(
                "simple-loop",
                Mapping,
                Lookup(new Dictionary<ushort, TrackAmplifierItem>()),
                sourceGenerationProvider: () => generation,
                clock: () => Now);

            var result = source.Read();

            Assert.Equal(generation, result.SourceGeneration);
        }

        [Fact]
        public void Read_ProjectsTrackAmplifierItemOccupancy()
        {
            var source = new TrackAmplifierPhysicalSectionOccupancySource(
                "simple-loop",
                Mapping,
                Lookup(new Dictionary<ushort, TrackAmplifierItem>
                {
                    [1] = Amplifier(1, occupied: true, receivedAt: Now),
                    [3] = Amplifier(3, occupied: false, receivedAt: Now)
                }),
                clock: () => Now);

            var result = source.Read();

            Assert.True(result.SourceValid);
            var section1 = Assert.Single(result.Sections, s => s.LogicalSectionId == 1);
            Assert.Equal(PhysicalOccupancy.Occupied, section1.Occupancy);

            var section2 = Assert.Single(result.Sections, s => s.LogicalSectionId == 2);
            Assert.Equal(PhysicalOccupancy.Clear, section2.Occupancy);
        }

        [Fact]
        public void Read_UsesInjectedClock()
        {
            var capturedAt = Now.AddHours(1);
            var source = new TrackAmplifierPhysicalSectionOccupancySource(
                "simple-loop",
                Mapping,
                Lookup(new Dictionary<ushort, TrackAmplifierItem>
                {
                    [1] = Amplifier(1, occupied: false, receivedAt: capturedAt),
                    [3] = Amplifier(3, occupied: false, receivedAt: capturedAt)
                }),
                clock: () => capturedAt);

            var result = source.Read();

            Assert.Equal(capturedAt, result.CapturedAtUtc);
            Assert.True(result.SourceValid);
            Assert.Equal(PhysicalSourceHealth.Healthy, result.SourceHealth);
        }

        [Fact]
        public void Read_UsesDefaultStaleAfterWhenNotProvided()
        {
            // A frame just beyond the default staleness policy must be unknown; this proves the
            // source applies a real (default) freshness bound rather than treating every value fresh.
            var stale = Now - TrackAmplifierDataFreshness.DefaultStaleAfter - TimeSpan.FromMilliseconds(1);

            var source = new TrackAmplifierPhysicalSectionOccupancySource(
                "simple-loop",
                Mapping,
                Lookup(new Dictionary<ushort, TrackAmplifierItem>
                {
                    [1] = Amplifier(1, occupied: true, receivedAt: stale),
                    [3] = Amplifier(3, occupied: false, receivedAt: stale)
                }),
                clock: () => Now);

            var result = source.Read();

            var section1 = Assert.Single(result.Sections, s => s.LogicalSectionId == 1);
            Assert.Equal(PhysicalOccupancy.Unknown, section1.Occupancy);
            Assert.Equal(PhysicalSourceHealth.Degraded, result.SourceHealth);
        }
    }
}
