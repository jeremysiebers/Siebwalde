using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Tests for <see cref="KoploperTelemetryTraceBuilder"/>: the machine-readable trace must
    /// include the per-command session outcome (requested/transmitted/acknowledged), the physical
    /// source health, the raw per-amplifier readback (SlaveDetected, HoldingReg[2], freshness), and
    /// the per-section reconciliation, so a re-run can distinguish a flowing telemetry session from
    /// an all-Unknown/Degraded one.
    /// </summary>
    public class KoploperTelemetryTraceTests
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void TraceRecord_IncludesSessionOutcome_PhysicalHealth_SlaveDetected_AndSectionOccupancy()
        {
            var guard = BuildAcknowledgedSession();

            var profile = new LayoutProfile
            {
                Name = "Simple Loop",
                PhysicalAmplifierMapping = new[]
                {
                    new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 }
                }
            };

            var variables = new TrackApplicationVariables();
            TrackAmplifierItem amp = variables.trackAmpItems.First(a => a.SlaveNumber == 1);
            amp.SlaveDetected = 1;
            amp.ProtocolReadbackObserved = true;
            amp.LastDataReceivedUtc = DateTimeOffset.UtcNow; // real now, so freshness is true
            var regs = new ushort[12];
            regs[TrackAmplifierRegisters.Status] = TrackAmplifierRegisters.OccupiedBit; // occupied
            amp.HoldingReg = regs;

            var physical = new PhysicalSectionOccupancyObservation(
                "Simple Loop", 1, 1, Now, true, PhysicalSourceHealth.Healthy,
                new[]
                {
                    new PhysicalSectionState(1, PhysicalOccupancy.Occupied, Now, TimeSpan.Zero, true, PhysicalSourceHealth.Healthy)
                });

            var reconciliation = new LogicalPhysicalReconciliationObservation(
                "Simple Loop", null, 0, 0, 1, 1, 1, Now, true, true, true,
                new[]
                {
                    new LogicalSectionReconciliation(
                        1, AutomaticSectionRole.Occupied, 10, false, PhysicalOccupancy.Occupied, true,
                        LogicalSectionReconciliationState.ConsistOccupied, SectionSafetyEligibility.Eligible,
                        Array.Empty<ReconciliationDiagnosticCode>())
                },
                Array.Empty<LogicalLocomotiveReconciliation>(),
                Array.Empty<ReconciliationDiagnosticCode>());

            KoploperTelemetryTraceRecord trace =
                KoploperTelemetryTraceBuilder.Build(0, Now, guard, physical, reconciliation, profile, variables);

            Assert.Equal("reconciliation", trace.Type);
            Assert.Equal(0, trace.Cycle);
            Assert.Equal("Simple Loop", trace.ProfileId);

            // Session: per-command outcome + transmitted vs acknowledged.
            Assert.Equal(6, trace.SessionCommands.Count);
            Assert.All(trace.SessionCommands, e => Assert.Equal(KoploperSessionCommandOutcome.Acknowledged, e.Outcome));
            Assert.Equal(6, trace.TotalTransmitted);
            Assert.Equal(6, trace.TotalAcknowledged);
            Assert.Equal(0, trace.AcknowledgementTimeouts);

            // Physical source health + provenance.
            Assert.Equal(PhysicalSourceHealth.Healthy, trace.PhysicalSourceHealth);
            Assert.True(trace.PhysicalSourceValid);
            Assert.Equal(1, trace.PhysicalSourceGeneration);
            Assert.Equal(1, trace.PhysicalSequence);

            // Per-amplifier raw readback.
            KoploperAmplifierTrace amplifier = Assert.Single(trace.Amplifiers);
            Assert.Equal((ushort)1, amplifier.SlaveNumber);
            Assert.Equal((ushort)1, amplifier.SlaveDetected);
            Assert.Equal(TrackAmplifierRegisters.OccupiedBit, amplifier.HoldingReg2Status);
            Assert.True(amplifier.Occupied);
            Assert.True(amplifier.IsFresh);

            // Per-section reconciliation.
            KoploperLogicalSectionTrace section = Assert.Single(trace.Sections);
            Assert.Equal(1, section.LogicalSectionId);
            Assert.Equal(1, section.PhysicalAmplifier);
            Assert.Equal(PhysicalOccupancy.Occupied, section.PhysicalOccupancy);
            Assert.True(section.PhysicalFresh);
            Assert.Equal(LogicalSectionReconciliationState.ConsistOccupied, section.ReconciliationState);
            Assert.Equal(SectionSafetyEligibility.Eligible, section.SectionSafetyEligibility);
        }

        [Fact]
        public void TraceRecord_DistinguishesFlowingTelemetryFromAllUnknown()
        {
            var guard = BuildAcknowledgedSession();

            var profile = new LayoutProfile
            {
                Name = "Simple Loop",
                PhysicalAmplifierMapping = new[]
                {
                    new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 }
                }
            };

            // Never-seen amplifier: no detection, no fresh readback.
            var variables = new TrackApplicationVariables();

            var physical = new PhysicalSectionOccupancyObservation(
                "Simple Loop", 0, 1, Now, true, PhysicalSourceHealth.Degraded,
                new[]
                {
                    new PhysicalSectionState(1, PhysicalOccupancy.Unknown, Now, TimeSpan.FromSeconds(5), false, PhysicalSourceHealth.Degraded)
                });

            var reconciliation = new LogicalPhysicalReconciliationObservation(
                "Simple Loop", null, 0, 0, 0, 1, 1, Now, true, false, false,
                new[]
                {
                    new LogicalSectionReconciliation(
                        1, AutomaticSectionRole.None, null, false, PhysicalOccupancy.Unknown, false,
                        LogicalSectionReconciliationState.NotAssessable, SectionSafetyEligibility.NotAssessable,
                        new[] { ReconciliationDiagnosticCode.ReconPhysicalUnknown })
                },
                Array.Empty<LogicalLocomotiveReconciliation>(),
                Array.Empty<ReconciliationDiagnosticCode>());

            KoploperTelemetryTraceRecord trace =
                KoploperTelemetryTraceBuilder.Build(0, Now, guard, physical, reconciliation, profile, variables);

            Assert.Equal(PhysicalSourceHealth.Degraded, trace.PhysicalSourceHealth);
            Assert.True(trace.PhysicalSourceValid);

            KoploperAmplifierTrace amplifier = Assert.Single(trace.Amplifiers);
            Assert.Equal((ushort)0, amplifier.SlaveDetected);
            Assert.False(amplifier.IsFresh);
            Assert.False(amplifier.Occupied);

            KoploperLogicalSectionTrace section = Assert.Single(trace.Sections);
            Assert.Equal(PhysicalOccupancy.Unknown, section.PhysicalOccupancy);
            Assert.False(section.PhysicalFresh);
            Assert.Equal(LogicalSectionReconciliationState.NotAssessable, section.ReconciliationState);
        }

        [Fact]
        public void TraceRecord_AllSlaves_IncludesOccupiedUnmappedSlave()
        {
            var guard = BuildAcknowledgedSession();

            var profile = new LayoutProfile
            {
                Name = "Simple Loop",
                PhysicalAmplifierMapping = new[]
                {
                    new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 }
                }
            };

            var variables = new TrackApplicationVariables();

            // Unmapped slave 2 shows an occupied reading; it must appear in AllSlaves even though
            // it is not part of the profile mapping.
            TrackAmplifierItem slave2 = variables.trackAmpItems.First(a => a.SlaveNumber == 2);
            slave2.SlaveDetected = 0;
            slave2.ProtocolReadbackObserved = true;
            slave2.LastDataReceivedUtc = DateTimeOffset.UtcNow;
            var regs = new ushort[12];
            regs[TrackAmplifierRegisters.Status] = TrackAmplifierRegisters.OccupiedBit;
            slave2.HoldingReg = regs;

            KoploperTelemetryTraceRecord trace = KoploperTelemetryTraceBuilder.Build(
                0, DateTimeOffset.UtcNow, guard, physical: null, reconciliation: null, profile, variables);

            // Mapped array: only the profile-mapped slave (1).
            KoploperAmplifierTrace mapped = Assert.Single(trace.Amplifiers);
            Assert.Equal((ushort)1, mapped.SlaveNumber);

            // AllSlaves: includes the occupied unmapped slave 2.
            Assert.Contains(trace.AllSlaves, a =>
                a.SlaveNumber == 2
                && a.Occupied
                && a.HoldingReg2Status == TrackAmplifierRegisters.OccupiedBit);
        }

        private static KoploperSessionCommandGuard BuildAcknowledgedSession()
        {
            var inner = new FakeCommClient { AutoAck = m => AckFor(m.Command) };
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5), () => Now);
            guard.BeginNewSession();

            foreach (byte command in KoploperSessionCommandAllowlist.ColdStartOrder)
            {
                guard.SendAsync(new SendMessage(command, new byte[] { 0, 0, 0 })).GetAwaiter().GetResult();
            }

            return guard;
        }

        private static ReceivedMessage AckFor(byte command) => command switch
        {
            250 => new ReceivedMessage(TaskId.CONTROLLER, TaskStates.CONNECTED, TaskStates.DONE, 0),
            100 => new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVES_ON, TaskStates.DONE, 0),
            101 => new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_DETECT, TaskStates.DONE, 0),
            104 => new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_INIT, TaskStates.DONE, 0),
            106 => new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD, TaskStates.DONE, 0),
            105 => new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_ENABLE, TaskStates.DONE, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(command))
        };

        private sealed class FakeCommClient : ITrackCommClient
        {
            public List<SendMessage> Sent { get; } = new List<SendMessage>();

            public Func<SendMessage, ReceivedMessage?>? AutoAck { get; set; }

            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
            {
                Sent.Add(message);

                if (AutoAck is not null)
                {
                    ReceivedMessage? ack = AutoAck(message);
                    if (ack is ReceivedMessage a)
                    {
                        ControlMessageReceived?.Invoke(this, new ControlMessageEventArgs(a));
                    }
                }

                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
