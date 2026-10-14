using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.TrackApplication.Topology
{
    /// <summary>
    /// One logical section's per-section feedback identity (KIS10-06/KIS10-08): the logical section
    /// id, the amplifier address whose occupancy it reflects (the REAL physical amplifier when the
    /// profile projection is <c>usePhysical</c>, the logical/simulated amplifier otherwise), and the
    /// Koploper bezetmelder(s) attached to that section.
    ///
    /// This is the unambiguous "section -&gt; amplifier -&gt; bezetmelder" resolution. The block-level
    /// aggregate (<see cref="SiebwaldeApp.Core.KoploperBlockMap"/>) cannot express it because a
    /// Koploper block may span multiple sections, each with its own amplifier and feedback contact.
    /// </summary>
    public sealed class KoploperFeedbackSection
    {
        /// <summary>The logical section id (the identity used by blocks, routes and occupancy).</summary>
        public int SectionId { get; init; }

        /// <summary>
        /// The amplifier address whose fresh occupancy this section reflects. Physical in the Real
        /// profile projection, logical/simulated otherwise. Never a backplane/configuration address
        /// (51..55): those are not track sections/amplifiers and project to no feedback.
        /// </summary>
        public int Amplifier { get; init; }

        /// <summary>The Koploper bezetmelder name(s) attached to this section.</summary>
        public IReadOnlyList<string> Bezetmelders { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// One Koploper logical block's ordered feedback sections. <see cref="BlockNumber"/> is the
    /// Koploper block identity (the INTERNAL block id from <c>KoploperBlockBinding</c> in the
    /// profile-driven projection, or the legacy display block number in the
    /// <see cref="SiebwaldeApp.Core.KoploperBlockMap"/> fallback). <see cref="Sections"/> are the
    /// bound sections in declaration order, each with its amplifier + bezetmelder identity.
    /// </summary>
    public sealed class KoploperFeedbackBlock
    {
        public int BlockNumber { get; init; }
        public IReadOnlyList<KoploperFeedbackSection> Sections { get; init; } = Array.Empty<KoploperFeedbackSection>();
    }
}
