using System;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Deterministic offline tests for <see cref="PhysicalOccupancyDeassertFilter"/> (Stage 1 of the
    /// two-stage physical-source release): the rising edge (Clear → Occupied) is immediate, only the
    /// falling edge (Occupied → Clear) is debounced (100 ms by default), an interrupted deassert
    /// resets the timer, and Unknown passes through while clearing the pending deassert.
    /// </summary>
    public class PhysicalOccupancyDeassertFilterTests
    {
        private static readonly DateTimeOffset Start = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        private static PhysicalSectionOccupancyObservation Obs(int section, PhysicalOccupancy occ)
            => ReconciliationTestFixtures.Physical(
                new[] { ReconciliationTestFixtures.PhysicalState(section, occ) });

        private static PhysicalOccupancy Debounced(PhysicalOccupancyDeassertFilter filter, int section, PhysicalOccupancy occ)
            => filter.Filter(Obs(section, occ)).Sections.Single().Occupancy;

        [Fact]
        public void Entry_ClearToOccupied_IsImmediate()
        {
            DateTimeOffset now = Start;
            var filter = new PhysicalOccupancyDeassertFilter(clock: () => now); // default 100 ms

            Assert.Equal(PhysicalOccupancy.Clear, Debounced(filter, 1, PhysicalOccupancy.Clear));

            now = Start.AddMilliseconds(1); // no window delay on entry
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Occupied));
        }

        [Fact]
        public void ClearShorterThanWindow_IsHeld()
        {
            DateTimeOffset now = Start;
            var filter = new PhysicalOccupancyDeassertFilter(clock: () => now);

            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Occupied)); // t=0
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Clear)); // first Clear: timer starts, held

            now = Start.AddMilliseconds(50); // Clear persisted < 100 ms
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Clear));
        }

        [Fact]
        public void Clear99ms_IsHeld()
        {
            DateTimeOffset now = Start;
            var filter = new PhysicalOccupancyDeassertFilter(clock: () => now);

            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Occupied));
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Clear)); // timer starts

            now = Start.AddMilliseconds(99); // boundary: still below the 100 ms window
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Clear));
        }

        [Fact]
        public void Clear100ms_IsFilteredClear()
        {
            DateTimeOffset now = Start;
            var filter = new PhysicalOccupancyDeassertFilter(clock: () => now);

            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Occupied));
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Clear)); // timer starts

            now = Start.AddMilliseconds(100); // exactly the window: deassert qualifies
            Assert.Equal(PhysicalOccupancy.Clear, Debounced(filter, 1, PhysicalOccupancy.Clear));
        }

        [Fact]
        public void ClearQualificationInterruptedByOccupied_ResetsTimer()
        {
            DateTimeOffset now = Start;
            var filter = new PhysicalOccupancyDeassertFilter(clock: () => now);

            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Occupied)); // t=0

            now = Start.AddMilliseconds(50);
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Clear)); // deassert starts

            now = Start.AddMilliseconds(60);
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Occupied)); // interrupt -> reset

            now = Start.AddMilliseconds(110);
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Clear)); // restart deassert

            now = Start.AddMilliseconds(160);
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Clear)); // 50 ms < 100 ms

            now = Start.AddMilliseconds(210); // 100 ms continuous Clear -> filtered Clear
            Assert.Equal(PhysicalOccupancy.Clear, Debounced(filter, 1, PhysicalOccupancy.Clear));
        }

        [Fact]
        public void Unknown_PassesThrough_AndClearsPendingDeassert()
        {
            DateTimeOffset now = Start;
            var filter = new PhysicalOccupancyDeassertFilter(clock: () => now);

            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Occupied));

            now = Start.AddMilliseconds(50);
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Clear)); // deassert pending

            now = Start.AddMilliseconds(60);
            Assert.Equal(PhysicalOccupancy.Unknown, Debounced(filter, 1, PhysicalOccupancy.Unknown)); // passes through + clears

            // A subsequent Clear starts a fresh deassert (not immediately Clear).
            now = Start.AddMilliseconds(70);
            Assert.Equal(PhysicalOccupancy.Occupied, Debounced(filter, 1, PhysicalOccupancy.Clear));
        }
    }
}
