using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for the physical section occupancy projector. These build the mapping and
    /// amplifier containers directly and always run the production
    /// <see cref="PhysicalSectionOccupancyProjector"/> — there is no test-only bypass.
    /// </summary>
    public class PhysicalSectionOccupancyProjectorTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
        private static readonly TimeSpan StaleAfter = TimeSpan.FromSeconds(2);

        private static readonly LayoutPhysicalAmplifierBinding[] Mapping =
        {
            new() { SectionId = 1, PhysicalAmplifier = 1 },
            new() { SectionId = 2, PhysicalAmplifier = 3 },
            new() { SectionId = 3, PhysicalAmplifier = 4 },
            new() { SectionId = 4, PhysicalAmplifier = 6 }
        };

        private static TrackAmplifierItem Amplifier(
            ushort slave,
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
                SlaveNumber = slave,
                SlaveDetected = detected ? (ushort)1 : (ushort)0,
                HoldingReg = regs,
                LastDataReceivedUtc = receivedAt
            };
        }

        private static Func<ushort, TrackAmplifierItem?> Lookup(IReadOnlyDictionary<ushort, TrackAmplifierItem> items)
            => slave => items.TryGetValue(slave, out TrackAmplifierItem? item) ? item : null;

        private static PhysicalSectionOccupancyObservation Project(
            IReadOnlyList<LayoutPhysicalAmplifierBinding> mapping,
            IReadOnlyDictionary<ushort, TrackAmplifierItem> items,
            string profileId = "simple-loop",
            long sourceGeneration = 0,
            long sequence = 1,
            DateTimeOffset? capturedAtUtc = null,
            TimeSpan? staleAfter = null)
            => PhysicalSectionOccupancyProjector.Project(
                profileId,
                mapping,
                Lookup(items),
                sourceGeneration,
                sequence,
                capturedAtUtc ?? Now,
                staleAfter ?? StaleAfter);

        private static PhysicalSectionOccupancyObservation ProjectAllFresh(
            bool occupiedOnSection1,
            IReadOnlyList<LayoutPhysicalAmplifierBinding>? mapping = null)
        {
            mapping ??= Mapping;

            var items = new Dictionary<ushort, TrackAmplifierItem>();
            foreach (LayoutPhysicalAmplifierBinding binding in mapping)
            {
                bool occupied = binding.SectionId == 1 && occupiedOnSection1;
                items[(ushort)binding.PhysicalAmplifier] = Amplifier(
                    (ushort)binding.PhysicalAmplifier,
                    occupied,
                    receivedAt: Now);
            }

            return Project(mapping, items);
        }

        // ---------------------------------------------------------------------
        // Tri-state occupancy
        // ---------------------------------------------------------------------

        [Fact]
        public void Project_FreshClearSection_IsClear()
        {
            var result = ProjectAllFresh(occupiedOnSection1: false);

            Assert.True(result.SourceValid);
            Assert.Equal(PhysicalSourceHealth.Healthy, result.SourceHealth);

            var clear = Assert.Single(result.Sections, s => s.LogicalSectionId == 1);
            Assert.Equal(PhysicalOccupancy.Clear, clear.Occupancy);
            Assert.True(clear.IsFresh);
        }

        [Fact]
        public void Project_FreshOccupiedSection_IsOccupied()
        {
            var result = ProjectAllFresh(occupiedOnSection1: true);

            var occupied = Assert.Single(result.Sections, s => s.LogicalSectionId == 1);
            Assert.Equal(PhysicalOccupancy.Occupied, occupied.Occupancy);
            Assert.True(occupied.IsFresh);
        }

        [Fact]
        public void Project_StaleOccupiedSection_IsUnknown()
        {
            // The amplifier reported occupied, then communication stopped: the reading no longer
            // proves occupancy, so it must fall back to Unknown and never promote to Occupied.
            var items = new Dictionary<ushort, TrackAmplifierItem>
            {
                [1] = Amplifier(1, occupied: true, receivedAt: Now - TimeSpan.FromSeconds(30))
            };

            var result = Project(Mapping, items);

            var stale = Assert.Single(result.Sections, s => s.LogicalSectionId == 1);
            Assert.Equal(PhysicalOccupancy.Unknown, stale.Occupancy);
            Assert.False(stale.IsFresh);
            Assert.Equal(PhysicalSourceHealth.Degraded, stale.SourceHealth);
            Assert.Equal(PhysicalSourceHealth.Degraded, result.SourceHealth);
        }

        [Fact]
        public void Project_MissingAmplifier_IsUnknown()
        {
            // No frame has been parsed for this amplifier at all (lookup returns null).
            var result = Project(Mapping, new Dictionary<ushort, TrackAmplifierItem>());

            var missing = Assert.Single(result.Sections, s => s.LogicalSectionId == 1);
            Assert.Equal(PhysicalOccupancy.Unknown, missing.Occupancy);
            Assert.False(missing.IsFresh);
            Assert.Equal(Now, missing.ObservedAtUtc);
        }

        [Fact]
        public void Project_UndetectedAmplifier_IsUnknown()
        {
            // The amplifier exists but was never detected: not current, therefore unknown.
            var items = new Dictionary<ushort, TrackAmplifierItem>
            {
                [1] = Amplifier(1, occupied: true, receivedAt: Now, detected: false)
            };

            var result = Project(Mapping, items);

            var undetected = Assert.Single(result.Sections, s => s.LogicalSectionId == 1);
            Assert.Equal(PhysicalOccupancy.Unknown, undetected.Occupancy);
            Assert.False(undetected.IsFresh);
        }

        // ---------------------------------------------------------------------
        // Mapping validation
        // ---------------------------------------------------------------------

        [Fact]
        public void Project_EmptyMapping_SourceInvalidAndUnavailable()
        {
            var result = Project(Array.Empty<LayoutPhysicalAmplifierBinding>(), new Dictionary<ushort, TrackAmplifierItem>());

            Assert.False(result.SourceValid);
            Assert.Equal(PhysicalSourceHealth.Unavailable, result.SourceHealth);
            Assert.Empty(result.Sections);
        }

        [Fact]
        public void Project_DuplicateSectionId_SourceInvalid()
        {
            var mapping = new[]
            {
                new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 },
                new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 3 }
            };

            var result = Project(mapping, new Dictionary<ushort, TrackAmplifierItem>());

            Assert.False(result.SourceValid);
            Assert.Equal(PhysicalSourceHealth.Unavailable, result.SourceHealth);
            Assert.Empty(result.Sections);
        }

        [Fact]
        public void Project_DuplicatePhysicalAmplifier_SourceInvalid()
        {
            var mapping = new[]
            {
                new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 },
                new LayoutPhysicalAmplifierBinding { SectionId = 2, PhysicalAmplifier = 1 }
            };

            var result = Project(mapping, new Dictionary<ushort, TrackAmplifierItem>());

            Assert.False(result.SourceValid);
            Assert.Equal(PhysicalSourceHealth.Unavailable, result.SourceHealth);
            Assert.Empty(result.Sections);
        }

        [Fact]
        public void Project_PhysicalAmplifierOutOfRange_SourceInvalid()
        {
            var mapping = new[]
            {
                new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 0 },
                new LayoutPhysicalAmplifierBinding { SectionId = 2, PhysicalAmplifier = 3 }
            };

            var result = Project(mapping, new Dictionary<ushort, TrackAmplifierItem>());

            Assert.False(result.SourceValid);
            Assert.Equal(PhysicalSourceHealth.Unavailable, result.SourceHealth);
            Assert.Empty(result.Sections);
        }

        // ---------------------------------------------------------------------
        // Ordering and identity
        // ---------------------------------------------------------------------

        [Fact]
        public void Project_SectionsAreSortedByLogicalSectionId()
        {
            // Declare the mapping in non-sorted order; the result must be sorted by section id.
            var mapping = new[]
            {
                new LayoutPhysicalAmplifierBinding { SectionId = 3, PhysicalAmplifier = 4 },
                new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 },
                new LayoutPhysicalAmplifierBinding { SectionId = 2, PhysicalAmplifier = 3 }
            };

            var result = ProjectAllFresh(occupiedOnSection1: false, mapping);

            Assert.Equal(new[] { 1, 2, 3 }, result.Sections.Select(s => s.LogicalSectionId));
        }

        [Fact]
        public void Project_OccupiedCarriesNoLocomotiveIdentity()
        {
            var result = ProjectAllFresh(occupiedOnSection1: true);

            var occupied = Assert.Single(result.Sections, s => s.Occupancy == PhysicalOccupancy.Occupied);
            Assert.Equal(1, occupied.LogicalSectionId);

            // The physical model is a bare tri-state: no section state exposes a locomotive or
            // owner identity, so an occupied reading can never name a locomotive.
            Assert.DoesNotContain(
                typeof(PhysicalSectionState).GetProperties(),
                p => p.Name.Contains("Loco", StringComparison.OrdinalIgnoreCase)
                     || p.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase)
                     || p.Name.Contains("Identity", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Project_SourceHealth_HealthyWhenAllFresh()
        {
            var result = ProjectAllFresh(occupiedOnSection1: true);

            Assert.True(result.SourceValid);
            Assert.Equal(PhysicalSourceHealth.Healthy, result.SourceHealth);
            Assert.All(result.Sections, s => Assert.True(s.IsFresh));
        }

        [Fact]
        public void Project_SourceHealth_DegradedWhenAnyStale()
        {
            var items = new Dictionary<ushort, TrackAmplifierItem>
            {
                [1] = Amplifier(1, occupied: true, receivedAt: Now),
                [3] = Amplifier(3, occupied: false, receivedAt: Now),
                [4] = Amplifier(4, occupied: false, receivedAt: Now - TimeSpan.FromSeconds(30)),
                [6] = Amplifier(6, occupied: false, receivedAt: Now)
            };

            var result = Project(Mapping, items);

            Assert.True(result.SourceValid);
            Assert.Equal(PhysicalSourceHealth.Degraded, result.SourceHealth);
            Assert.Contains(result.Sections, s => s.LogicalSectionId == 3 && !s.IsFresh);
        }
    }
}
