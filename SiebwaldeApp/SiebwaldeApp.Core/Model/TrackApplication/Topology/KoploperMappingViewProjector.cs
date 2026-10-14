using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiebwaldeApp.Core.TrackApplication.Topology
{
    /// <summary>
    /// A READ-ONLY projection of the Koploper logical-block -&gt; section -&gt; amplifier -&gt;
    /// bezetmelder mapping for the config/diagnostic surface (KIS10-08). It is a pure, immutable
    /// view: it holds no runtime state and carries no edit capability, so the
    /// <see cref="LayoutProfile"/> JSON remains the single runtime configuration authority.
    /// </summary>
    public sealed class KoploperMappingView
    {
        public string ProfileName { get; init; } = string.Empty;
        public IReadOnlyList<KoploperFeedbackBlock> Blocks { get; init; } = Array.Empty<KoploperFeedbackBlock>();

        /// <summary>Human-readable conflicts / missing-mapping problems, or empty when valid.</summary>
        public IReadOnlyList<string> ValidationIssues { get; init; } = Array.Empty<string>();

        public bool HasValidationIssues => ValidationIssues.Count > 0;

        /// <summary>Renders the mapping as a compact multi-line text for a read-only surface.</summary>
        public string ToDisplayText()
        {
            var sb = new StringBuilder();
            sb.Append("Profile: ").Append(string.IsNullOrWhiteSpace(ProfileName) ? "(none)" : ProfileName);

            if (Blocks.Count == 0)
            {
                sb.AppendLine();
                sb.Append("  (no Koploper blocks mapped)");
            }
            else
            {
                foreach (KoploperFeedbackBlock block in Blocks)
                {
                    sb.AppendLine();
                    sb.Append("  block ").Append(block.BlockNumber).Append(':');
                    if (block.Sections.Count == 0)
                    {
                        sb.Append(" (no sections)");
                        continue;
                    }

                    foreach (KoploperFeedbackSection section in block.Sections)
                    {
                        sb.AppendLine();
                        sb.Append("    section ").Append(section.SectionId)
                          .Append(" -> amp ").Append(section.Amplifier)
                          .Append(" -> bezetmelders [")
                          .Append(string.Join(", ", section.Bezetmelders))
                          .Append(']');
                    }
                }
            }

            sb.AppendLine();
            if (HasValidationIssues)
            {
                sb.Append("  validation: ").Append(string.Join("; ", ValidationIssues));
            }
            else
            {
                sb.Append("  validation: ok");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Builds a <see cref="KoploperMappingView"/> from a <see cref="LayoutProfile"/>. PURE and
    /// stateless: it reads the profile and produces the view; it never touches the runtime or any
    /// write authority, so the presentation logic stays separate from runtime authority (KIS10-08).
    /// </summary>
    public static class KoploperMappingViewProjector
    {
        public static KoploperMappingView Build(LayoutProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            var blocks = profile.ToKoploperFeedbackMap(usePhysical: true);
            var issues = CollectValidationIssues(profile, blocks);

            return new KoploperMappingView
            {
                ProfileName = profile.Name,
                Blocks = blocks,
                ValidationIssues = issues
            };
        }

        /// <summary>
        /// Surfaces conflicts and missing mapping as human-readable issues. A successfully loaded
        /// profile already passed the loader's exhaustive validation, so for production profiles this
        /// is empty; it remains meaningful for manually-assembled profiles and for making the "missing
        /// mapping / conflict" status explicit rather than implied.
        /// </summary>
        private static IReadOnlyList<string> CollectValidationIssues(
            LayoutProfile profile,
            IReadOnlyList<KoploperFeedbackBlock> blocks)
        {
            var issues = new List<string>();

            if (profile.KoploperBlockBindings.Count == 0)
            {
                issues.Add("no Koploper block bindings declared");
                return issues;
            }

            // One physical amplifier bound to more than one section is a conflict.
            var sectionsByAmp = new Dictionary<int, List<int>>();
            foreach (LayoutPhysicalAmplifierBinding binding in profile.PhysicalAmplifierMapping)
            {
                if (!sectionsByAmp.TryGetValue(binding.PhysicalAmplifier, out List<int>? list))
                {
                    list = new List<int>();
                    sectionsByAmp[binding.PhysicalAmplifier] = list;
                }

                list.Add(binding.SectionId);
            }

            foreach (KeyValuePair<int, List<int>> entry in sectionsByAmp)
            {
                if (entry.Value.Count > 1)
                {
                    issues.Add($"physical amplifier {entry.Key} is bound to multiple sections ({string.Join(", ", entry.Value)})");
                }
            }

            // Missing physical amplifier (the section is dropped from the view) or missing feedback.
            var sectionsInView = new HashSet<int>(blocks.SelectMany(b => b.Sections).Select(s => s.SectionId));
            var sectionById = profile.Sections.ToDictionary(s => s.Id);
            foreach (KoploperBlockBinding binding in profile.KoploperBlockBindings)
            {
                foreach (int sectionId in binding.SectionIds)
                {
                    if (!sectionsInView.Contains(sectionId))
                    {
                        issues.Add($"block {binding.KoploperInternalBlockId} section {sectionId} has no physical amplifier (missing mapping)");
                    }
                    else if (sectionById.TryGetValue(sectionId, out LayoutSection? section) &&
                             section.Bezetmelders.Count == 0)
                    {
                        issues.Add($"block {binding.KoploperInternalBlockId} section {sectionId} has no feedback contact (bezetmelder)");
                    }
                }
            }

            return issues;
        }
    }
}
