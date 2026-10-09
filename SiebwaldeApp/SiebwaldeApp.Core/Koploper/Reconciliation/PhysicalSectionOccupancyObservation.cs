using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// One physical section occupancy observation for a profile: the per-section tri-state
    /// occupancy projected from the physical track amplifiers, plus the provenance that decides
    /// whether the observation may be trusted. No section state carries a locomotive identity.
    /// </summary>
    public sealed record PhysicalSectionOccupancyObservation(
        string ProfileId,
        long SourceGeneration,
        long Sequence,
        DateTimeOffset CapturedAtUtc,
        bool SourceValid,
        PhysicalSourceHealth SourceHealth,
        IReadOnlyList<PhysicalSectionState> Sections);
}
