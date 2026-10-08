using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// Pure, stateless cross-check: compares an authoritative reservation observation against an
    /// independent sample and returns a single <see cref="KoploperValidationResult"/>. The
    /// comparison is order-insensitive for reserved block sets.
    /// </summary>
    public static class KoploperComparisonEngine
    {
        /// <summary>
        /// Compares <paramref name="observation"/> against <paramref name="independent"/>.
        /// Evaluation order: authority gate, source presence, process generation, then a
        /// per-locomotive occupied-block and reserved-block comparison.
        /// </summary>
        public static KoploperValidationResult Compare(
            KoploperReservationObservation observation,
            KoploperIndependentSample? independent,
            KoploperValidationOptions options)
        {
            ArgumentNullException.ThrowIfNull(observation);
            ArgumentNullException.ThrowIfNull(options);

            if (!observation.IsAuthoritative)
            {
                return KoploperValidationResult.NonAuthoritativeSource;
            }

            if (independent is null)
            {
                return KoploperValidationResult.SourceUnavailable;
            }

            if (independent.Generation != observation.Generation)
            {
                return KoploperValidationResult.GenerationMismatch;
            }

            bool hasComparableLocoData =
                independent.LocToCurrentBlock.Count > 0 ||
                independent.LocToReservedBlocks.Count > 0;

            if (!hasComparableLocoData)
            {
                return KoploperValidationResult.SourceUnavailable;
            }

            var observationByLoc = new Dictionary<int, KoploperLocomotiveTrajectory>();
            foreach (KoploperLocomotiveTrajectory locomotive in observation.Locomotives)
            {
                observationByLoc[locomotive.InternalLocomotiveId] = locomotive;
            }

            // Owner set: locomotives that own something (an occupied block or reserved blocks).
            var observationOwners = observation.Locomotives
                .Where(l => l.OccupiedBlock is not null || l.ReservedBlocks.Count > 0)
                .Select(l => l.InternalLocomotiveId)
                .ToHashSet();

            var independentOwners = new HashSet<int>(independent.LocToCurrentBlock.Keys);
            independentOwners.UnionWith(independent.LocToReservedBlocks.Keys);

            if (!observationOwners.SetEquals(independentOwners))
            {
                return KoploperValidationResult.OwnerMismatch;
            }

            foreach (int locomotiveId in observationOwners.OrderBy(id => id))
            {
                KoploperLocomotiveTrajectory locomotive = observationByLoc[locomotiveId];

                int? observedOccupied = locomotive.OccupiedBlock;
                bool independentHasCurrent = independent.LocToCurrentBlock.TryGetValue(locomotiveId, out int independentCurrent);
                int? independentOccupied = independentHasCurrent ? independentCurrent : null;

                if (observedOccupied != independentOccupied)
                {
                    return KoploperValidationResult.CurrentPositionMismatch;
                }

                var observedReserved = new HashSet<int>(locomotive.ReservedBlocks);
                var independentReserved = independent.LocToReservedBlocks.TryGetValue(locomotiveId, out IReadOnlyCollection<int>? reserved)
                    ? new HashSet<int>(reserved)
                    : new HashSet<int>();

                if (!observedReserved.SetEquals(independentReserved))
                {
                    return KoploperValidationResult.ReservationMismatch;
                }
            }

            return KoploperValidationResult.Match;
        }
    }
}
