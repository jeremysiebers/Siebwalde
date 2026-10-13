using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The raw physical readback of one track amplifier, for a machine-readable trace. A fresh,
    /// protocol-observed amplifier readback is what lets a re-run conclusively distinguish
    /// "telemetry flowing (SlaveDetected=1, fresh HoldingReg)" from "all Unknown/Degraded".
    /// </summary>
    public sealed record KoploperAmplifierTrace(
        ushort SlaveNumber,
        ushort SlaveDetected,
        ushort HoldingReg2Status,
        bool Occupied,
        DateTimeOffset? LastDataReceivedUtc,
        bool IsFresh);

    /// <summary>
    /// The reconciled state of one logical section, joined with its physical amplifier binding, for
    /// a machine-readable trace.
    /// </summary>
    public sealed record KoploperLogicalSectionTrace(
        int LogicalSectionId,
        int PhysicalAmplifier,
        PhysicalOccupancy PhysicalOccupancy,
        bool PhysicalFresh,
        AutomaticSectionRole LogicalRole,
        int? LogicalOwnerLocomotiveId,
        LogicalSectionReconciliationState ReconciliationState,
        SectionSafetyEligibility SectionSafetyEligibility,
        IReadOnlyList<string> Diagnostics);

    /// <summary>
    /// One machine-readable trace record for a reconciliation cycle: the session-command lifecycle
    /// (requested/transmitted/acknowledged per command), the last master state, the physical source
    /// health/provenance, the raw per-amplifier readbacks, and the per-section reconciliation.
    /// </summary>
    public sealed record KoploperTelemetryTraceRecord(
        string Type,
        long Cycle,
        string ProfileId,
        DateTimeOffset TimestampUtc,
        KoploperSessionCommandMode SessionMode,
        bool SessionStopped,
        byte? SessionExpectedCommand,
        ushort? MasterTaskId,
        ushort? MasterTaskCommand,
        ushort? MasterTaskState,
        IReadOnlyList<KoploperSessionCommandEvent> SessionCommands,
        long TotalTransmitted,
        long TotalAcknowledged,
        long AcknowledgementTimeouts,
        PhysicalSourceHealth? PhysicalSourceHealth,
        long PhysicalSourceGeneration,
        long PhysicalSequence,
        bool PhysicalSourceValid,
        IReadOnlyList<KoploperAmplifierTrace> Amplifiers,
        IReadOnlyList<KoploperAmplifierTrace> AllSlaves,
        IReadOnlyList<KoploperLogicalSectionTrace> Sections);

    /// <summary>
    /// Builds a <see cref="KoploperTelemetryTraceRecord"/> from a session guard, a physical
    /// observation, a reconciliation observation, the profile and the amplifier data container. This
    /// is the single projection that turns the live inputs into the machine-readable JSONL trace, so
    /// a re-run can conclusively distinguish a flowing telemetry session from an all-Unknown one.
    /// </summary>
    public static class KoploperTelemetryTraceBuilder
    {
        public static KoploperTelemetryTraceRecord Build(
            long cycle,
            DateTimeOffset timestampUtc,
            KoploperSessionCommandGuard session,
            PhysicalSectionOccupancyObservation? physical,
            LogicalPhysicalReconciliationObservation? reconciliation,
            LayoutProfile profile,
            TrackApplicationVariables variables)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(variables);

            BuildData(profile, variables, physical, reconciliation,
                out var amplifiers, out var allSlaves, out var sections);

            KoploperSessionCommandCounters counters = session.GetCounters();
            ReceivedMessage? master = session.LastMasterState;

            return new KoploperTelemetryTraceRecord(
                Type: "reconciliation",
                Cycle: cycle,
                ProfileId: profile.Name,
                TimestampUtc: timestampUtc,
                SessionMode: session.Mode,
                SessionStopped: session.IsStopped,
                SessionExpectedCommand: session.ExpectedCommand,
                MasterTaskId: master?.TaskId,
                MasterTaskCommand: master?.Taskcommand,
                MasterTaskState: master?.Taskstate,
                SessionCommands: session.SessionCommandEvents,
                TotalTransmitted: counters.TotalTransmitted,
                TotalAcknowledged: counters.TotalAcknowledged,
                AcknowledgementTimeouts: counters.AcknowledgementTimeouts,
                PhysicalSourceHealth: physical?.SourceHealth,
                PhysicalSourceGeneration: physical?.SourceGeneration ?? 0,
                PhysicalSequence: physical?.Sequence ?? 0,
                PhysicalSourceValid: physical?.SourceValid ?? false,
                Amplifiers: amplifiers,
                AllSlaves: allSlaves,
                Sections: sections);
        }

        /// <summary>
        /// Builds a passive trace record with NO session/command data (a passive observer has no
        /// PIC32 client). Session fields are empty/zero and the session mode is
        /// <see cref="KoploperSessionCommandMode.None"/>.
        /// </summary>
        public static KoploperTelemetryTraceRecord BuildPassive(
            long cycle,
            DateTimeOffset timestampUtc,
            PhysicalSectionOccupancyObservation? physical,
            LogicalPhysicalReconciliationObservation? reconciliation,
            LayoutProfile profile,
            TrackApplicationVariables variables)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(variables);

            BuildData(profile, variables, physical, reconciliation,
                out var amplifiers, out var allSlaves, out var sections);

            return new KoploperTelemetryTraceRecord(
                Type: "reconciliation",
                Cycle: cycle,
                ProfileId: profile.Name,
                TimestampUtc: timestampUtc,
                SessionMode: KoploperSessionCommandMode.None,
                SessionStopped: false,
                SessionExpectedCommand: null,
                MasterTaskId: null,
                MasterTaskCommand: null,
                MasterTaskState: null,
                SessionCommands: Array.Empty<KoploperSessionCommandEvent>(),
                TotalTransmitted: 0,
                TotalAcknowledged: 0,
                AcknowledgementTimeouts: 0,
                PhysicalSourceHealth: physical?.SourceHealth,
                PhysicalSourceGeneration: physical?.SourceGeneration ?? 0,
                PhysicalSequence: physical?.Sequence ?? 0,
                PhysicalSourceValid: physical?.SourceValid ?? false,
                Amplifiers: amplifiers,
                AllSlaves: allSlaves,
                Sections: sections);
        }

        private static void BuildData(
            LayoutProfile profile,
            TrackApplicationVariables variables,
            PhysicalSectionOccupancyObservation? physical,
            LogicalPhysicalReconciliationObservation? reconciliation,
            out List<KoploperAmplifierTrace> amplifiers,
            out List<KoploperAmplifierTrace> allSlaves,
            out List<KoploperLogicalSectionTrace> sections)
        {
            var sectionToAmplifier = profile.PhysicalAmplifierMapping
                .ToDictionary(b => b.SectionId, b => b.PhysicalAmplifier);

            amplifiers = new List<KoploperAmplifierTrace>();
            foreach (byte address in profile.ToPhysicalDomain())
            {
                TrackAmplifierItem? item = variables.trackAmpItems
                    .FirstOrDefault(a => a.SlaveNumber == address);

                ushort hr2 = item is not null && item.HoldingReg.Length > TrackAmplifierRegisters.Status
                    ? item.HoldingReg[TrackAmplifierRegisters.Status]
                    : (ushort)0;

                amplifiers.Add(new KoploperAmplifierTrace(
                    SlaveNumber: address,
                    SlaveDetected: item?.SlaveDetected ?? 0,
                    HoldingReg2Status: hr2,
                    Occupied: TrackAmplifierRegisters.IsOccupied(item?.HoldingReg),
                    LastDataReceivedUtc: item?.LastDataReceivedUtc,
                    IsFresh: TrackAmplifierDataFreshness.IsFresh(item)));
            }

            sections = new List<KoploperLogicalSectionTrace>();
            if (reconciliation is not null)
            {
                foreach (LogicalSectionReconciliation section in reconciliation.Sections)
                {
                    int amplifier = sectionToAmplifier.TryGetValue(section.LogicalSectionId, out int a) ? a : 0;
                    sections.Add(new KoploperLogicalSectionTrace(
                        LogicalSectionId: section.LogicalSectionId,
                        PhysicalAmplifier: amplifier,
                        PhysicalOccupancy: section.PhysicalOccupancy,
                        PhysicalFresh: section.PhysicalFresh,
                        LogicalRole: section.AutomaticRole,
                        LogicalOwnerLocomotiveId: section.LogicalOwnerLocomotiveId,
                        ReconciliationState: section.ReconciliationState,
                        SectionSafetyEligibility: section.SectionSafetyEligibility,
                        Diagnostics: section.Diagnostics.Select(d => d.ToString()).ToArray()));
                }
            }

            // Diagnostic: dump EVERY slave slot that shows any sign of life (detected, non-zero
            // HoldingReg[2], or occupied), not just the profile-mapped amplifiers. This lets an
            // operator see an occupied slave (e.g. "amp2") that is NOT in the profile mapping.
            allSlaves = new List<KoploperAmplifierTrace>();
            foreach (TrackAmplifierItem item in variables.trackAmpItems)
            {
                ushort hr2 = item.HoldingReg.Length > TrackAmplifierRegisters.Status
                    ? item.HoldingReg[TrackAmplifierRegisters.Status]
                    : (ushort)0;

                if (item.SlaveDetected != 0
                    || hr2 != 0
                    || TrackAmplifierRegisters.IsOccupied(item.HoldingReg))
                {
                    allSlaves.Add(new KoploperAmplifierTrace(
                        SlaveNumber: item.SlaveNumber,
                        SlaveDetected: item.SlaveDetected,
                        HoldingReg2Status: hr2,
                        Occupied: TrackAmplifierRegisters.IsOccupied(item.HoldingReg),
                        LastDataReceivedUtc: item.LastDataReceivedUtc,
                        IsFresh: TrackAmplifierDataFreshness.IsFresh(item)));
                }
            }
        }
    }
}
