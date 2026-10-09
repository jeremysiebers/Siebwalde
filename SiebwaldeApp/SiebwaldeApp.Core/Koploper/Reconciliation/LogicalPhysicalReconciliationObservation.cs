using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// One logical/physical reconciliation observation: the per-section reconciliation produced by
    /// comparing a logical-section shadow against a physical section occupancy observation, plus
    /// the provenance used to decide whether that comparison may be trusted. Consumers gate on
    /// <see cref="ReconciliationAssessable"/>; <see cref="LogicalSourceValid"/> and
    /// <see cref="PhysicalSourceValid"/> name which source (if any) failed.
    /// </summary>
    public sealed record LogicalPhysicalReconciliationObservation(
        string ProfileId,
        KoploperProcessGeneration? LogicalGeneration,
        long LogicalSourceSequence,
        long LogicalShadowSequence,
        long PhysicalGeneration,
        long PhysicalSequence,
        long ReconciliationSequence,
        DateTimeOffset EvaluatedAtUtc,
        bool LogicalSourceValid,
        bool PhysicalSourceValid,
        bool ReconciliationAssessable,
        IReadOnlyList<LogicalSectionReconciliation> Sections,
        IReadOnlyList<LogicalLocomotiveReconciliation> Locomotives,
        IReadOnlyList<ReconciliationDiagnosticCode> Diagnostics);
}
