namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Pure state-transition policy for the runtime lifecycle. This is the single source of truth
    /// for which lifecycle operations are legal in which state; the coordinator enforces it and the
    /// UI uses it to enable/disable commands.
    /// </summary>
    public static class TrackRuntimeControlPolicy
    {
        /// <summary>Start is legal only from <see cref="TrackRuntimeState.Stopped"/>.</summary>
        public static bool CanStart(TrackRuntimeState state) => state == TrackRuntimeState.Stopped;

        /// <summary>Stop is legal from <see cref="TrackRuntimeState.Running"/> and <see cref="TrackRuntimeState.Failed"/>.</summary>
        public static bool CanStop(TrackRuntimeState state)
            => state == TrackRuntimeState.Running || state == TrackRuntimeState.Failed;

        /// <summary>Restart is legal from <see cref="TrackRuntimeState.Running"/> and <see cref="TrackRuntimeState.Failed"/>.</summary>
        public static bool CanRestart(TrackRuntimeState state)
            => state == TrackRuntimeState.Running || state == TrackRuntimeState.Failed;
    }
}
