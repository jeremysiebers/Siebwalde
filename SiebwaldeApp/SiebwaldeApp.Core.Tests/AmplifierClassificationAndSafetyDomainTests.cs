using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Regression tests for the independent review finding that the amplifier-centric safety
    /// fallback wrongly treated backplane/configuration slaves 51..55 as track amplifiers, and
    /// for the physical-device-class / operational-group / block-mapping separation.
    ///
    /// Software only: the real <see cref="TrackAmplifierHardwareBackend"/>,
    /// <see cref="AmplifierCommandTracker"/> and <see cref="EcosHardwareStopSink"/> are used; only
    /// the block-location source is substituted. No hardware is touched.
    /// </summary>
    public class AmplifierClassificationAndSafetyDomainTests
    {
        private const int Neutral = AmplifierSpeedMapper.NeutralPwm;

        private sealed class NoBlockProvider : IBlockPositionProvider
        {
            public event Action<int, int>? BlockEntered
            {
                add { }
                remove { }
            }

            public int? TryGetBlockForLoc(int loc) => null;
        }

        private sealed class MutableBlockProvider : IBlockPositionProvider
        {
            private readonly Dictionary<int, int> _blocks = new();

            public event Action<int, int>? BlockEntered
            {
                add { }
                remove { }
            }

            public int? TryGetBlockForLoc(int loc)
                => _blocks.TryGetValue(loc, out var block) ? block : (int?)null;

            public void Set(int loco, int? block)
            {
                if (block is int value)
                {
                    _blocks[loco] = value;
                }
                else
                {
                    _blocks.Remove(loco);
                }
            }
        }

        private static void Seed(TrackApplicationVariables variables, ushort amplifier)
        {
            var item = variables.trackAmpItems.First(a => a.SlaveNumber == amplifier);
            item.SlaveDetected = 1;
            item.LastDataReceivedUtc = DateTimeOffset.UtcNow;
        }

        private static void SeedStale(TrackApplicationVariables variables, ushort amplifier)
        {
            var item = variables.trackAmpItems.First(a => a.SlaveNumber == amplifier);
            item.SlaveDetected = 1;
            item.LastDataReceivedUtc = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(1);
        }

        private static ushort Pwm(TrackApplicationVariables variables, ushort amplifier)
            => (ushort)(variables.PendingWrites[amplifier].Hr0Value & 0x03FF);

        // -----------------------------------------------------------------
        // Physical device class (the authoritative boundary)
        // -----------------------------------------------------------------

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(50, true)]
        [InlineData(51, false)]
        [InlineData(52, false)]
        [InlineData(55, false)]
        [InlineData(56, false)]
        [InlineData(-1, false)]
        public void PhysicalDeviceClass_MatchesTheAuthoritativeRanges(int address, bool expected)
        {
            Assert.Equal(expected, TrackAmplifierAddress.IsTrackAmplifierAddress(address));
        }

        [Fact]
        public void BackplaneClassification_CoversOnly51To55()
        {
            Assert.True(TrackAmplifierAddress.IsBackplaneConfigurationSlave(51));
            Assert.True(TrackAmplifierAddress.IsBackplaneConfigurationSlave(55));
            Assert.False(TrackAmplifierAddress.IsBackplaneConfigurationSlave(50));
            Assert.False(TrackAmplifierAddress.IsBackplaneConfigurationSlave(56));
            Assert.False(TrackAmplifierAddress.IsTrackAmplifierAddress(51));
            Assert.False(TrackAmplifierAddress.IsTrackAmplifierAddress(55));
        }

        // -----------------------------------------------------------------
        // Backplane detection must never become a track-amplifier target
        // -----------------------------------------------------------------

        [Fact]
        public void StrongestNeutralization_ExcludesBackplaneSlaves_AndWritesNoPwmToThem()
        {
            var variables = new TrackApplicationVariables();
            Seed(variables, 1);
            Seed(variables, 6);
            Seed(variables, 50);
            Seed(variables, 51);
            Seed(variables, 52);
            Seed(variables, 55);

            var tracker = new AmplifierCommandTracker();
            var backend = new TrackAmplifierHardwareBackend(
                new NoBlockProvider(),
                BlockTopology.Parse("amps: 1:1"),
                variables,
                commandTracker: tracker);
            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            var known = backend.GetKnownPhysicalAmplifiers();
            Assert.Contains((ushort)1, known);
            Assert.Contains((ushort)6, known);
            Assert.Contains((ushort)50, known);
            Assert.DoesNotContain((ushort)51, known);
            Assert.DoesNotContain((ushort)52, known);
            Assert.DoesNotContain((ushort)55, known);

            var result = sink.StopLayout();

            Assert.True(result.Succeeded);
            Assert.Contains((ushort)1, result.CommandedAmplifiers);
            Assert.Contains((ushort)6, result.CommandedAmplifiers);
            Assert.Contains((ushort)50, result.CommandedAmplifiers);
            Assert.DoesNotContain((ushort)51, result.CommandedAmplifiers);
            Assert.DoesNotContain((ushort)52, result.CommandedAmplifiers);
            Assert.DoesNotContain((ushort)55, result.CommandedAmplifiers);

            // No HR0/PWM write may be produced for any backplane slave.
            Assert.False(variables.PendingWrites.ContainsKey(51));
            Assert.False(variables.PendingWrites.ContainsKey(52));
            Assert.False(variables.PendingWrites.ContainsKey(55));
        }

        [Fact]
        public void NeutralizeAmplifiers_RefusesBackplaneSlaves_AndProducesNoWrite()
        {
            var variables = new TrackApplicationVariables();
            Seed(variables, 51);
            Seed(variables, 52);
            Seed(variables, 55);

            var backend = new TrackAmplifierHardwareBackend(
                new NoBlockProvider(),
                BlockTopology.Parse("amps: 1:1"),
                variables);

            var refused = backend.NeutralizeAmplifiers(new ushort[] { 51, 52, 55 });

            Assert.Equal(new ushort[] { 51, 52, 55 }, refused.OrderBy(a => a).ToArray());
            Assert.False(variables.PendingWrites.ContainsKey(51));
            Assert.False(variables.PendingWrites.ContainsKey(52));
            Assert.False(variables.PendingWrites.ContainsKey(55));
        }

        [Fact]
        public void TopologyMappingABackplaneSlave_IsIgnoredForLocoCommands()
        {
            var variables = new TrackApplicationVariables();
            var backend = new TrackAmplifierHardwareBackend(
                new MutableBlockProvider(),
                BlockTopology.Parse("amps: 1:51"),
                variables);

            var applied = backend.SetLocoSpeed(7, 1, 0);

            Assert.False(applied);
            Assert.False(variables.PendingWrites.ContainsKey(51));
        }

        // -----------------------------------------------------------------
        // Unmapped / spare amplifiers and operational grouping
        // -----------------------------------------------------------------

        [Fact]
        public void UnmappedTrackAmplifier_IsLegitimate_WithoutBlockTopologyMembership()
        {
            var variables = new TrackApplicationVariables();
            Seed(variables, 50);

            var backend = new TrackAmplifierHardwareBackend(
                new NoBlockProvider(),
                BlockTopology.Parse("amps: 1:1"),
                variables);

            Assert.Contains((ushort)50, backend.GetKnownPhysicalAmplifiers());
            Assert.True(TrackAmplifierAddress.IsTrackAmplifierAddress(50));
        }

        [Fact]
        public void SpareTrackAmplifier_IsPhysicallyATrackAmplifier_IndependentOfItsGroup()
        {
            var groups = TrackAmplifierGroups.Create(null, null, new[] { 50 });

            Assert.Equal(TrackAmplifierOperationalGroup.Spare, groups.Classify(50));
            Assert.True(TrackAmplifierAddress.IsTrackAmplifierAddress(50));
            Assert.True(groups.IsConfigured(50));

            // A spare with no block mapping and no loco ownership is still a legitimate track
            // amplifier, so the strongest physical neutralization can reach it when it is detected.
            var variables = new TrackApplicationVariables();
            Seed(variables, 50);
            var backend = new TrackAmplifierHardwareBackend(
                new NoBlockProvider(),
                BlockTopology.Parse("amps: 1:1"),
                variables,
                groups: groups);
            Assert.Contains((ushort)50, backend.GetKnownPhysicalAmplifiers());
        }

        [Fact]
        public void OperationalGrouping_IsIndependentOfPhysicalClassAndBlockMapping()
        {
            var groups = TrackAmplifierGroups.Create(new[] { 1, 3, 4 }, new[] { 6 }, new[] { 50 });

            Assert.Equal(TrackAmplifierOperationalGroup.MainRailway, groups.Classify(1));
            Assert.Equal(TrackAmplifierOperationalGroup.MountainRailway, groups.Classify(6));
            Assert.Equal(TrackAmplifierOperationalGroup.Spare, groups.Classify(50));

            // A legitimate track amplifier with no configured group stays Unassigned; it is never
            // inferred as main railway from its address range.
            Assert.Equal(TrackAmplifierOperationalGroup.Unassigned, groups.Classify(2));

            // A backplane slave can never carry an operational group.
            Assert.Equal(TrackAmplifierOperationalGroup.Unassigned, groups.Classify(51));
        }

        [Fact]
        public void BlockTopologyMembership_DoesNotImplyMainRailway()
        {
            var variables = new TrackApplicationVariables();
            var backend = new TrackAmplifierHardwareBackend(
                new NoBlockProvider(),
                BlockTopology.Parse("amps: 1:1,3:3"),
                variables,
                groups: TrackAmplifierGroups.Empty);

            Assert.Equal(TrackAmplifierOperationalGroup.Unassigned, backend.GetOperationalGroup(1));
            Assert.Equal(TrackAmplifierOperationalGroup.Unassigned, backend.GetOperationalGroup(3));
        }

        [Fact]
        public void GroupsParse_RejectsBackplaneAddressesAndDuplicates()
        {
            var groups = TrackAmplifierGroups.Parse("main: 1,51 ; mountain: 1,6 ; spare: 55,50");

            Assert.Equal(new ushort[] { 1 }, groups.MainRailway);
            Assert.Equal(new ushort[] { 6 }, groups.MountainRailway);
            Assert.Equal(new ushort[] { 50 }, groups.Spare);
            Assert.NotEmpty(groups.Errors);
        }

        // -----------------------------------------------------------------
        // Stale / unavailable required targets
        // -----------------------------------------------------------------

        [Fact]
        public void LocoStop_WithNeverSeenRequiredTarget_IsNotReportedAsSuccess_AndKeepsTheTarget()
        {
            var variables = new TrackApplicationVariables();
            var tracker = new AmplifierCommandTracker();
            var blocks = new MutableBlockProvider();
            var backend = new TrackAmplifierHardwareBackend(
                blocks,
                BlockTopology.Parse("amps: 1:1,3:3"),
                variables,
                commandTracker: tracker);
            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            // Amp 1 is commanded non-neutral but never delivered a frame.
            blocks.Set(7, 1);
            Assert.True(backend.SetLocoSpeed(7, 1, 0));
            Assert.Equal(new ushort[] { 1 }, tracker.GetOutstanding(7));

            var result = sink.StopLoco(7);

            Assert.False(result.Succeeded);
            Assert.Contains((ushort)1, result.FailedAmplifiers);
            Assert.Equal(new ushort[] { 1 }, tracker.GetOutstanding(7));
        }

        [Fact]
        public void LocoStop_WithDetectedButStaleRequiredTarget_IsNotReportedAsSuccess()
        {
            var variables = new TrackApplicationVariables();
            SeedStale(variables, 1);

            var tracker = new AmplifierCommandTracker();
            var blocks = new MutableBlockProvider();
            var backend = new TrackAmplifierHardwareBackend(
                blocks,
                BlockTopology.Parse("amps: 1:1,3:3"),
                variables,
                commandTracker: tracker);
            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            blocks.Set(7, 1);
            Assert.True(backend.SetLocoSpeed(7, 1, 0));
            Assert.Equal(new ushort[] { 1 }, tracker.GetOutstanding(7));

            var result = sink.StopLoco(7);

            Assert.False(result.Succeeded);
            Assert.Contains((ushort)1, result.FailedAmplifiers);
            Assert.Equal(new ushort[] { 1 }, tracker.GetOutstanding(7));
        }

        [Fact]
        public void LayoutStop_WithOneStaleDetectedAmplifier_KeepsItAsAFailedSafetyTarget()
        {
            var variables = new TrackApplicationVariables();
            Seed(variables, 1);
            SeedStale(variables, 3);

            var tracker = new AmplifierCommandTracker();
            var backend = new TrackAmplifierHardwareBackend(
                new NoBlockProvider(),
                BlockTopology.Parse("amps: 1:1"),
                variables,
                commandTracker: tracker);
            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            var result = sink.StopLayout();

            Assert.False(result.Succeeded);
            Assert.Contains((ushort)3, result.FailedAmplifiers);
            Assert.Contains((ushort)1, result.CommandedAmplifiers);
            // The neutral command is still queued best-effort for the stale amplifier.
            Assert.Equal(Neutral, Pwm(variables, 3));
        }

        // -----------------------------------------------------------------
        // Manual SetAmplifierControl path
        // -----------------------------------------------------------------

        [Fact]
        public void ManualAmplifierControl_CannotWriteTrackPwmToABackplaneSlave()
        {
            var variables = new TrackApplicationVariables();

            variables.SetDesiredAmplifierControl(51, 500, false);

            Assert.False(variables.PendingWrites.ContainsKey(51));
        }

        [Fact]
        public void ManualAmplifierControl_IsReachedByTheStrongestPhysicalNeutralization()
        {
            var variables = new TrackApplicationVariables();
            Seed(variables, 1);

            var tracker = new AmplifierCommandTracker();
            var backend = new TrackAmplifierHardwareBackend(
                new NoBlockProvider(),
                BlockTopology.Parse(""),
                variables,
                commandTracker: tracker);
            var sink = new EcosHardwareStopSink
            {
                Hardware = backend,
                Neutralizer = backend,
                CommandTracker = tracker
            };

            // The manual/diagnostic page queues a non-neutral command with no locomotive owner.
            variables.SetDesiredAmplifierControl(1, 500, false);
            Assert.Equal(500, Pwm(variables, 1));
            Assert.False(tracker.HasOutstanding(7));

            // The strongest physical neutralization still reaches it.
            var result = sink.StopLayout();

            Assert.True(result.Succeeded);
            Assert.Contains((ushort)1, result.CommandedAmplifiers);
            Assert.Equal(Neutral, Pwm(variables, 1));
        }

        // -----------------------------------------------------------------
        // Ownership transfer across operational groups
        // -----------------------------------------------------------------

        [Fact]
        public void OwnershipTransfer_IsPerAmplifier_AndDoesNotCrossIntoAnotherGroup()
        {
            var tracker = new AmplifierCommandTracker();

            // Loco 7 owns a main-railway amplifier, loco 8 owns a mountain-railway amplifier
            // (identical hardware type, different operational domain).
            tracker.RecordNonNeutral(7, 1);
            tracker.RecordNonNeutral(8, 6);

            // Loco 8 later commands the same main-railway amplifier; only that amplifier transfers.
            tracker.RecordNonNeutral(8, 1);

            Assert.Empty(tracker.GetOutstanding(7));
            Assert.Equal(new ushort[] { 1, 6 }, tracker.GetOutstanding(8));
        }

        // -----------------------------------------------------------------
        // Startup contract (device-class scoping)
        // -----------------------------------------------------------------

        [Fact]
        public void StartupDefaultSetpoints_ApplyToTrackAmplifiersOnly()
        {
            var variables = new TrackApplicationVariables();
            var backplane = variables.trackAmpItems.First(a => a.SlaveNumber == 51);
            backplane.HoldingReg[0] = 0x1234;

            variables.InitializeDefaultPwmSetpoints(Neutral);

            var amp1 = variables.trackAmpItems.First(a => a.SlaveNumber == 1);
            Assert.Equal(Neutral, amp1.HoldingReg[0] & 0x03FF);

            // The backplane configuration word is left untouched.
            Assert.Equal(0x1234, backplane.HoldingReg[0]);
        }
    }
}
