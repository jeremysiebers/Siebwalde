using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A compact, stable identity for one running Koploper process instance: the process id plus
    /// the process start time (UTC). This is the unit the snapshot reader retains across calls
    /// so it can detect a restart and reset the per-generation sequence.
    /// </summary>
    public readonly record struct KoploperProcessGeneration(int ProcessId, DateTimeOffset ProcessStartTimeUtc)
    {
        /// <summary>Projects a full <see cref="KoploperProcessInfo"/> to its generation identity.</summary>
        public static KoploperProcessGeneration From(KoploperProcessInfo info)
        {
            ArgumentNullException.ThrowIfNull(info);
            return new KoploperProcessGeneration(info.ProcessId, info.ProcessStartTimeUtc);
        }
    }
}
