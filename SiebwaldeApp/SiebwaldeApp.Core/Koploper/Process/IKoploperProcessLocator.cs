using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Identifies one located Koploper process instance. Process identity (process id + start
    /// time) is kept separate from the run-specific module base address so that a restart can be
    /// distinguished from an address change caused by ASLR.
    /// </summary>
    public sealed record KoploperProcessInfo(
        int ProcessId,
        DateTimeOffset ProcessStartTimeUtc,
        nuint ModuleBaseAddress,
        string ExecutablePath,
        string ExecutableName);

    /// <summary>
    /// Result of locating the Koploper process.
    /// </summary>
    public enum KoploperProcessStatus
    {
        /// <summary>Exactly one Koploper process was found.</summary>
        Found = 0,

        /// <summary>No Koploper process is running.</summary>
        NotFound = 1,

        /// <summary>A process was found but could not be inspected (for example, access is denied).</summary>
        AccessDenied = 2,

        /// <summary>More than one candidate process matched; the result is ambiguous.</summary>
        MultipleMatches = 3
    }

    /// <summary>
    /// Locates the Koploper process and answers process-lifetime questions.
    /// </summary>
    public interface IKoploperProcessLocator
    {
        /// <summary>Attempts to locate the Koploper process and return its identity.</summary>
        KoploperProcessStatus TryLocate(out KoploperProcessInfo? info);

        /// <summary>
        /// True when <paramref name="current"/> is a different process instance than
        /// <paramref name="previous"/> (a restart), which invalidates any previously resolved
        /// memory layout.
        /// </summary>
        bool IsRestart(KoploperProcessInfo? previous, KoploperProcessInfo current);
    }
}
