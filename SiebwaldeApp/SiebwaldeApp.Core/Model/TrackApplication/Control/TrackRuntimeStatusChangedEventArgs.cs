using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Raised by the runtime coordinator whenever the lifecycle state changes. The optional
    /// <see cref="FailureReason"/> is set when the transition enters <see cref="TrackRuntimeState.Failed"/>.
    /// </summary>
    public sealed class TrackRuntimeStatusChangedEventArgs : EventArgs
    {
        public TrackRuntimeState State { get; }

        /// <summary>Human-readable reason, set only when <see cref="State"/> is <see cref="TrackRuntimeState.Failed"/>.</summary>
        public string? FailureReason { get; }

        public TrackRuntimeStatusChangedEventArgs(TrackRuntimeState state, string? failureReason = null)
        {
            State = state;
            FailureReason = failureReason;
        }
    }

    /// <summary>
    /// Raised by the runtime coordinator (and re-raised by the ECoS host) when a runtime fault
    /// occurs. The <see cref="Source"/> identifies the component that reported the fault.
    /// </summary>
    public sealed class RuntimeFaultEventArgs : EventArgs
    {
        public string Source { get; }

        public Exception Exception { get; }

        public RuntimeFaultEventArgs(string source, Exception exception)
        {
            Source = source ?? string.Empty;
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }
}
