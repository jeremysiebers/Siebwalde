using System;
using System.Collections.Generic;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A PURE, STATELESS projector that resolves the physical drive group of one Koploper block:
    /// <c>DriveGroup(block) = ordered set of PhysicalAmplifier(section)</c> for each section in the
    /// block's 1..N section list, in section order. It performs no I/O, holds no state, and decides
    /// no authority — it only resolves the geometry of a block's drive group. (KIS10-03.)
    /// </summary>
    public static class KoploperDriveGroupProjector
    {
        /// <summary>
        /// Resolves <paramref name="blockId"/> to its ordered physical amplifier addresses (1..N).
        /// <paramref name="blockToSections"/> is the 1:N Koploper internal block -&gt; logical
        /// section(s) map and <paramref name="sectionToAmplifier"/> the physical section -&gt;
        /// amplifier binding.
        /// <para>
        /// <b>Fail-closed contract:</b> the result is EMPTY when (a) the block is not in the map,
        /// (b) the block binds an empty section list, or (c) any bound section has no physical
        /// amplifier binding. A section without a physical amplifier is NOT excluded — it makes the
        /// whole drive group empty, because driving only the resolvable subset of a block would
        /// power part of a section set that a locomotive spans as one unit. Callers treat an empty
        /// result as "block is not drivable".
        /// </para>
        /// </summary>
        public static IReadOnlyList<int> Project(
            int blockId,
            IReadOnlyDictionary<int, IReadOnlyList<int>> blockToSections,
            IReadOnlyList<LayoutPhysicalAmplifierBinding> sectionToAmplifier)
        {
            ArgumentNullException.ThrowIfNull(blockToSections);
            ArgumentNullException.ThrowIfNull(sectionToAmplifier);

            if (!blockToSections.TryGetValue(blockId, out IReadOnlyList<int>? sectionIds) ||
                sectionIds is null ||
                sectionIds.Count == 0)
            {
                return Array.Empty<int>();
            }

            var amplifierBySection = new Dictionary<int, int>();
            foreach (LayoutPhysicalAmplifierBinding binding in sectionToAmplifier)
            {
                amplifierBySection[binding.SectionId] = binding.PhysicalAmplifier;
            }

            var amplifiers = new List<int>(sectionIds.Count);
            foreach (int sectionId in sectionIds)
            {
                if (!amplifierBySection.TryGetValue(sectionId, out int amplifier))
                {
                    // A section without a physical amplifier: fail closed (empty drive group).
                    return Array.Empty<int>();
                }

                amplifiers.Add(amplifier);
            }

            return amplifiers;
        }
    }
}
