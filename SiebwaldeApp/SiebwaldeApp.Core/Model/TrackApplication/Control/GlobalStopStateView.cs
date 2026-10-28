namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Read-only, point-in-time projection of the process-lifetime <see cref="GlobalStopController"/>
    /// for presentation surfaces (WPF). It exposes only observability — the current state, the latch
    /// reason and whether an operator-authorized recovery is still pending — and deliberately omits
    /// the controller's mutating <c>Arm</c>/<c>Latch</c> authority, so the UI cannot arm or latch the
    /// global stop on its own. The runtime host is the only production producer of this view.
    /// </summary>
    public sealed class GlobalStopStateView
    {
        /// <summary>
        /// Builds a snapshot from the controller's current observables.
        /// </summary>
        /// <param name="state">The current global-stop state (Disarmed/Armed/Latched).</param>
        /// <param name="reason">The latch reason, or null when not latched.</param>
        /// <param name="recoveryPending">
        /// True while an operator-authorized recovery has not yet cleared the latch (the startup
        /// precondition is still re-arming / re-confirming).
        /// </param>
        public GlobalStopStateView(GlobalStopState state, GlobalStopReason? reason, bool recoveryPending)
        {
            State = state;
            Reason = reason;
            RecoveryPending = recoveryPending;
        }

        /// <summary>The current global-stop state (Disarmed/Armed/Latched).</summary>
        public GlobalStopState State { get; }

        /// <summary>The latch reason, or null when not latched.</summary>
        public GlobalStopReason? Reason { get; }

        /// <summary>True when the global stop is latched.</summary>
        public bool IsLatched => State == GlobalStopState.Latched;

        /// <summary>
        /// True while an operator-authorized recovery has not yet cleared the latch.
        /// </summary>
        public bool RecoveryPending { get; }
    }
}
