namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Lifecycle state of the in-process track-control runtime.
    ///
    /// This is the explicit state machine the runtime coordinator owns. It is distinct from the
    /// Workflow v1 workflow states and from the latched safety condition (which is orthogonal and
    /// already surfaced through <see cref="ControlDiagnostics"/>).
    ///
    /// Legal transitions (see <see cref="TrackRuntimeControlPolicy"/>):
    /// <code>
    ///   Stopped  -> Starting -> Running
    ///   Running  -> Stopping -> Stopped
    ///   (any)    -> Failed    (start/stop/restart failure)
    ///   Failed   -> Stopping  (restart or idempotent stop)
    /// </code>
    /// </summary>
    public enum TrackRuntimeState
    {
        /// <summary>No track runtime objects exist; no comm; no ECoS host.</summary>
        Stopped = 0,

        /// <summary>Composition and initialization are in progress.</summary>
        Starting = 1,

        /// <summary>The runtime loop and ECoS host are active in the requested mode.</summary>
        Running = 2,

        /// <summary>Cancellation, awaiting and disposal are in progress.</summary>
        Stopping = 3,

        /// <summary>A fatal start/stop/restart failure occurred; explicit operator restart is required.</summary>
        Failed = 4
    }
}
