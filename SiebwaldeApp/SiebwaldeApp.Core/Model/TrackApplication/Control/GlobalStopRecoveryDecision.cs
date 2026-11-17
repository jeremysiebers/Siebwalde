namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The recovery action an automated repeated-init campaign may take for a given PIC32
    /// global-stop state (the 111 readback value), derived solely from the observed state. This is
    /// the campaign's REARM policy: an automated loop may only clear a LATCHED-HOST-LOSS latch
    /// (state 1 — the expected consequence of the previous cycle's own controlled Stop), never a
    /// host-initiated / EMO latch (state 2) and never an unknown/unexpected state (fail closed).
    ///
    /// This is deliberately a CAMPAIGN policy, not a change to the production
    /// <see cref="GlobalStopPreconditionStep"/>, which continues to treat every latched state as an
    /// operator-recovery gate (the operator may legitimately re-arm a host-initiated latch after
    /// confirming the physical stop inputs are clear). The campaign cannot perform that physical
    /// confirmation, so it must stop on state 2 instead of blindly re-arming.
    /// </summary>
    public enum GlobalStopRecoveryAction
    {
        /// <summary>Controller Disarmed (0) or Armed (3): proceed with the init directly, no re-arm.</summary>
        ProceedNoRearm,

        /// <summary>Controller latched by host loss (1): a single REARM_GLOBAL_STOP (112) is permitted.</summary>
        Rearm,

        /// <summary>Controller latched host-initiated (2) or the state is unknown/unexpected: STOP, never re-arm.</summary>
        StopFailClosed
    }

    /// <summary>Pure, offline-testable classification of a 111 global-stop state into a recovery action.</summary>
    public static class GlobalStopRecoveryDecision
    {
        /// <summary>
        /// Maps an observed PIC32 <c>GLOBAL_STOP_STATE</c> (111) value to the campaign recovery action.
        /// A null state (no 111 reply — controller unreachable or the read timed out) is treated as
        /// unknown and therefore fail-closed: the campaign must stop, never assume a latch reason.
        /// </summary>
        public static GlobalStopRecoveryAction Decide(ushort? state) => state switch
        {
            GlobalStopStatusMessage.Disarmed => GlobalStopRecoveryAction.ProceedNoRearm,   // 0
            GlobalStopStatusMessage.Armed => GlobalStopRecoveryAction.ProceedNoRearm,      // 3
            GlobalStopStatusMessage.LatchedHostLoss => GlobalStopRecoveryAction.Rearm,     // 1
            GlobalStopStatusMessage.LatchedHostInitiated => GlobalStopRecoveryAction.StopFailClosed, // 2
            _ => GlobalStopRecoveryAction.StopFailClosed,
        };
    }
}
