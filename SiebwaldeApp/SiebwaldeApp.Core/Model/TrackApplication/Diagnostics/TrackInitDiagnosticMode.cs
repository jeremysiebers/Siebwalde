namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Diagnostic instrumentation mode for the controlled initialization runtime.
    ///
    /// <see cref="None"/> is the production default: the initialization sequence is unchanged and
    /// proceeds automatically from Stage 4 (configure/validate) to Stage 5 (GROUP_READY) with no
    /// pause. <see cref="PauseAfterStage4"/> activates the Variant-A diagnostic pause (see
    /// <c>docs/PV07-SINGLE-STEP-AMPLIFIER-DIAGNOSTICS-DESIGN.md</c> §5): after Stage 4 passes (every
    /// required amplifier STANDBY, GROUP_READY not yet issued) the runtime waits for an explicit
    /// operator action instead of issuing the domain-wide GROUP_READY.
    ///
    /// The mode is selected before <c>StartAsync</c> (while the runtime is stopped). It never
    /// enables an automatic Stage 5, an automatic GROUP_READY, an automatic retry or an automatic
    /// REARM; it only inserts an operator gate at the Stage-4/Stage-5 seam.
    /// </summary>
    public enum TrackInitDiagnosticMode
    {
        /// <summary>Production default. No pause; initialization proceeds unchanged.</summary>
        None = 0,

        /// <summary>Pause after Stage 4 (all amplifiers STANDBY), before Stage 5 (GROUP_READY).</summary>
        PauseAfterStage4 = 1
    }
}
