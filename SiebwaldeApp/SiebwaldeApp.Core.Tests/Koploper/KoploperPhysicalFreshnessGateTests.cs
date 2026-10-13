using System;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Verifies the Architect-approved freshness-gate fix: the KIS-08 physical source now uses
    /// <see cref="TrackAmplifierDataFreshness.HasFreshProtocolReadback"/> (a genuine SLAVEINFO
    /// protocol readback AND fresh, WITHOUT requiring <see cref="TrackAmplifierItem.SlaveDetected"/>
    /// != 0). This fixes live run #3, where a re-run DETECT cleared <c>SlaveDetected</c> for
    /// configured amplifiers while the SLAVEINFO stream still carried valid, fresh occupancy.
    /// </summary>
    public class KoploperPhysicalFreshnessGateTests
    {
        [Fact]
        public void FreshReadbackWithUndetectedSlave_NewHelperTrue_OldGatesStillFalse()
        {
            TrackAmplifierItem item = BuildUndetectedOccupiedAmplifier();

            // OLD gates unchanged: they still require SlaveDetected != 0.
            Assert.False(TrackAmplifierDataFreshness.IsCurrentData(item));
            Assert.False(TrackAmplifierDataFreshness.HasCurrentProtocolReadback(item));

            // NEW helper: genuine protocol readback + fresh, no SlaveDetected requirement.
            Assert.True(TrackAmplifierDataFreshness.HasFreshProtocolReadback(item));
            Assert.True(TrackAmplifierDataFreshness.IsFresh(item));
            Assert.True(TrackAmplifierRegisters.IsOccupied(item.HoldingReg));
        }

        [Fact]
        public void FreshReadbackWithUndetectedSlave_ProjectorProjectsOccupied()
        {
            TrackAmplifierItem item = BuildUndetectedOccupiedAmplifier();
            var binding = new LayoutPhysicalAmplifierBinding { SectionId = 2, PhysicalAmplifier = 3 };

            PhysicalSectionOccupancyObservation observation = PhysicalSectionOccupancyProjector.Project(
                "Simple Loop",
                new[] { binding },
                slave => slave == 3 ? item : null,
                sourceGeneration: 0,
                sequence: 1,
                capturedAtUtc: DateTimeOffset.UtcNow,
                staleAfter: TrackAmplifierDataFreshness.DefaultStaleAfter);

            PhysicalSectionState section = Assert.Single(observation.Sections);
            Assert.Equal(PhysicalOccupancy.Occupied, section.Occupancy);
            Assert.True(section.IsFresh);
        }

        [Fact]
        public void FreshReadbackWithUndetectedAmplifier_ClearOccupancyProjectsClear()
        {
            TrackAmplifierItem item = BuildUndetectedClearAmplifier();
            var binding = new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 };

            PhysicalSectionOccupancyObservation observation = PhysicalSectionOccupancyProjector.Project(
                "Simple Loop",
                new[] { binding },
                slave => slave == 1 ? item : null,
                sourceGeneration: 0,
                sequence: 1,
                capturedAtUtc: DateTimeOffset.UtcNow,
                staleAfter: TrackAmplifierDataFreshness.DefaultStaleAfter);

            PhysicalSectionState section = Assert.Single(observation.Sections);
            // Residual-risk note: a never-detected but genuinely fresh-clear amplifier projects Clear.
            Assert.Equal(PhysicalOccupancy.Clear, section.Occupancy);
            Assert.True(section.IsFresh);
        }

        private static TrackAmplifierItem BuildUndetectedOccupiedAmplifier()
        {
            TrackAmplifierItem item = NewUndetectedAmplifier(slaveNumber: 3);
            var regs = new ushort[12];
            regs[TrackAmplifierRegisters.Status] = TrackAmplifierRegisters.OccupiedBit; // bit 10 set
            item.HoldingReg = regs;
            return item;
        }

        private static TrackAmplifierItem BuildUndetectedClearAmplifier()
        {
            TrackAmplifierItem item = NewUndetectedAmplifier(slaveNumber: 1);
            item.HoldingReg = new ushort[12]; // occupancy bit clear
            return item;
        }

        private static TrackAmplifierItem NewUndetectedAmplifier(ushort slaveNumber)
        {
            return new TrackAmplifierItem
            {
                SlaveNumber = slaveNumber,
                SlaveDetected = 0,                 // DETECT flag cleared by a re-run of command 101
                ProtocolReadbackObserved = true,   // HoldingReg is a genuine SLAVEINFO readback
                LastDataReceivedUtc = DateTimeOffset.UtcNow
            };
        }
    }
}
