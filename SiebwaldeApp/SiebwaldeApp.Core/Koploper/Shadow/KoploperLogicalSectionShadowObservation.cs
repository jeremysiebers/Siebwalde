using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// One logical-section shadow observation: the KIS-05 reservation observation projected through
    /// the KIS-04 per-block manual/automatic state onto Siebwalde logical sections. The shadow is
    /// rebuilt from scratch each cycle (no stale ownership is inherited). <see cref="ShadowValid"/>
    /// is the single consumer gate: when false, the projection must not be used to drive control.
    /// </summary>
    public sealed record KoploperLogicalSectionShadowObservation(
        KoploperProcessGeneration? Generation,
        long SourceSequence,
        long ShadowSequence,
        DateTimeOffset CapturedAtUtc,
        DateTimeOffset ObservedAtUtc,
        bool IsFresh,
        KoploperSourceHealth SourceHealth,
        bool SourceAuthoritative,
        string ProfileId,
        bool MappingValid,
        bool ManualStateValid,
        bool ShadowValid,
        IReadOnlyList<LogicalLocomotiveShadow> Locomotives,
        IReadOnlyList<int> ManualBlockedSections,
        IReadOnlyList<KoploperUnmappedBlock> UnmappedBlocks,
        IReadOnlyList<KoploperLogicalSectionConflict> Conflicts,
        IReadOnlyList<KoploperDiagnosticCode> Diagnostics)
    {
        /// <summary>Elapsed time between snapshot capture and observation.</summary>
        public TimeSpan Age => ObservedAtUtc - CapturedAtUtc;
    }
}
