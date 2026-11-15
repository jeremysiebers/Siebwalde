using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Lifecycle and amplifier-data contract for the in-process track-control runtime.
    ///
    /// Implemented by a single coordinator that owns composition and lifetime, so WPF can start,
    /// stop and restart the whole track-control runtime (comm, initialization, runtime write loop
    /// and ECoS host) without rebuilding the application. The implementation lives in the
    /// Integration layer; Core only defines the contract so the dependency direction is preserved.
    /// </summary>
    public interface ITrackApplicationRuntime
    {
        /// <summary>Current lifecycle state of the runtime.</summary>
        TrackRuntimeState State { get; }

        /// <summary>Raised on every lifecycle-state transition.</summary>
        event EventHandler<TrackRuntimeStatusChangedEventArgs>? StateChanged;

        /// <summary>Raised when a runtime fault occurs (start/stop/restart failure).</summary>
        event EventHandler<RuntimeFaultEventArgs>? Faulted;

        /// <summary>
        /// Starts the runtime in the requested mode. Only valid from <see cref="TrackRuntimeState.Stopped"/>.
        /// </summary>
        Task StartAsync(TrackControlMode mode, CancellationToken ct = default);

        /// <summary>
        /// Stops the runtime. Idempotent from <see cref="TrackRuntimeState.Stopped"/>. A stop that
        /// cannot complete cleanly transitions to <see cref="TrackRuntimeState.Failed"/>, never
        /// silently to <see cref="TrackRuntimeState.Stopped"/>.
        /// </summary>
        Task StopAsync(CancellationToken ct = default);
        /// <summary>Sticky controlled emergency stop; does not tear down occupancy/admin communication.</summary>
        void EmergencyStop() => throw new NotSupportedException("This runtime does not implement global stop.");

        /// <summary>Controlled stop followed by a fresh start reusing the last started mode.</summary>
        Task RestartAsync(CancellationToken ct = default);

        /// <summary>The mode last passed to <see cref="StartAsync"/>, or null if the runtime has never been started.</summary>
        TrackControlMode? LastRequestedMode { get; }

        /// <summary>The ECoS mode that is actually active, or null when the host is not running.</summary>
        TrackControlMode? ActiveEcosMode { get; }

        /// <summary>Diagnostics for the control path, or null when the ECoS host is not running.</summary>
        ControlDiagnostics? ControlDiagnostics { get; }

        /// <summary>True while an unsafe divergence is latched and not yet reset.</summary>
        bool IsControlPathUnsafe { get; }

        /// <summary>
        /// The current movement-permission state. Granted only after BOTH observed neutral has been
        /// established (command echo AND applied PWM on every configured amplifier) AND Stage 6
        /// (track power) has been authorized and executed — never merely because neutral was
        /// observed, and never merely to start the write loop.
        /// </summary>
        MovementPermissionState MovementPermission { get; }

        /// <summary>Explicit recovery for a latched safety fault. A latched fault never clears itself.</summary>
        bool ResetControlSafety();

        /// <summary>
        /// Explicit operator action that authorizes a global-stop re-arm during startup recovery.
        /// Without this call the startup precondition keeps blocking and never emits
        /// <c>REARM_GLOBAL_STOP</c> (112). The physical-stop-clear evidence source must be supplied;
        /// when null it defaults to fail-closed (never clear), so recovery cannot proceed without
        /// positive physical evidence. Never auto-releases a latched controller.
        /// </summary>
        void AuthorizeGlobalStopRecovery(Func<bool>? physicalStopInputsClear = null);

        /// <summary>
        /// Read-only view of the process-lifetime global-stop controller for presentation surfaces
        /// (WPF). The UI can observe the disarmed/armed/latched state and the latch reason and
        /// request operator recovery via <see cref="AuthorizeGlobalStopRecovery"/>, but cannot arm
        /// or latch the controller itself (the mutating <c>Arm</c>/<c>Latch</c> authority is not
        /// exposed through this surface).
        /// </summary>
        GlobalStopStateView GlobalStopState { get; }

        /// <summary>Raised when an amplifier data frame has been parsed and applied.</summary>
        event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;

        /// <summary>Read-only view of the current track amplifiers.</summary>
        IReadOnlyList<TrackAmplifierItem> TrackAmplifiers { get; }

        /// <summary>
        /// The controllable simulated-amplifier I/O surface, or null unless the runtime is running
        /// in <see cref="TrackControlMode.FullSimulation"/>.
        /// </summary>
        ISimulatedTrackIo? SimulatedTrackIo { get; }

        /// <summary>
        /// The movement simulator driving <see cref="TrackControlMode.FullSimulation"/>, or null
        /// when the runtime is not running in FullSimulation. Read/write surface for the simulated
        /// locomotives (positions, placement, speed).
        /// </summary>
        IMovementSimulation? MovementSimulation { get; }

        /// <summary>
        /// The name of the active layout profile driving FullSimulation, or null when no profile
        /// is loaded. Used by the Simulation tab to show what the simulation is running against.
        /// </summary>
        string? ActiveProfileName { get; }

        /// <summary>
        /// The layout profile that drives FullSimulation: the profile last selected via
        /// <see cref="SelectFullSimulationProfile"/>, or the constructor-supplied default when
        /// none was selected.
        /// </summary>
        LayoutProfile? ActiveFullSimulationProfile { get; }

        /// <summary>
        /// The REAL physical amplifier addresses bound by the active layout profile's
        /// physical-amplifier mapping (the observed-neutral domain for Real mode), or empty when
        /// no physical binding is declared / no profile is loaded.
        /// </summary>
        IReadOnlyList<int> PhysicalAmplifierBinding { get; }

        /// <summary>
        /// Selects the layout profile to use for the next <see cref="StartAsync"/> in
        /// <see cref="TrackControlMode.FullSimulation"/>. It also updates the underlying ECoS host's
        /// composition inputs; only valid while the runtime is stopped.
        /// </summary>
        void SelectFullSimulationProfile(LayoutProfile profile);

        /// <summary>Returns the current list of track amplifiers (empty when not running).</summary>
        List<TrackAmplifierItem> GetAmplifierListing();

        /// <summary>
        /// Updates the desired control parameters (PWM setpoint + EmoStop) for a given amplifier.
        /// The values are queued and sent by the runtime write loop, not immediately.
        /// </summary>
        void SetAmplifierControl(ushort slaveNumber, int pwmSetpoint, bool emoStop);

        /// <summary>
        /// Read-only snapshot of the seven controlled-initialization stages, ordered by stage id.
        /// Empty until the track part has been composed.
        /// </summary>
        IReadOnlyList<TrackInitStageStatus> InitStages { get; }

        /// <summary>Raised when an initialization stage's status changes.</summary>
        event EventHandler<TrackInitStageStatusChangedEventArgs>? InitStageStatusChanged;

        /// <summary>True when the operator has authorized the Stage 6 track-power transition.</summary>
        bool TrackPowerAuthorized { get; }

        /// <summary>True when track power is ON (TRACK_POWER_ON verified on every required amplifier).</summary>
        bool TrackPowerOn { get; }

        /// <summary>
        /// The current initialization run status, or <see cref="InitializationStatus.Idle"/> before
        /// the track part has been composed. <see cref="InitializationStatus.Completed"/> is reached
        /// only after Stage 6 (track power) completes; a non-energized run stops at
        /// <see cref="InitializationStatus.StoppedAtGlobalReady"/>.
        /// </summary>
        InitializationStatus RunStatus { get; }

        /// <summary>Explicit operator authorization for the Stage 6 track-power transition (never auto-energized).</summary>
        void AuthorizeTrackPowerEnable();

        /// <summary>Runs the gated Stage 6 track-power action (write HR_TRACK_POWER + verify HR_STATE per amplifier).</summary>
        Task<bool> EnableTrackPowerAsync(CancellationToken ct = default);

        /// <summary>
        /// Clears HR_TRACK_POWER on every required amplifier, de-energizing the bridges, and verifies
        /// de-energization (inhibited==1) per amplifier. Returns true when every required amplifier
        /// is confirmed de-energized; false (fail-closed) otherwise.
        /// </summary>
        Task<bool> DisableTrackPowerAsync(CancellationToken ct = default);

        /// <summary>Manual initialization: run exactly one stage (step).</summary>
        Task<TrackInitStageState> StepInitAsync(CancellationToken ct = default);

        /// <summary>Manual initialization: re-run the current stage (retry).</summary>
        Task<TrackInitStageState> RetryInitAsync(CancellationToken ct = default);

        /// <summary>Manual initialization: move back one stage and mark it Pending for re-execution.</summary>
        void BacktrackInit();

        /// <summary>
        /// Re-validates a run that entered <see cref="InitializationStatus.RecoveryHold"/> after a
        /// comm/host-authority fault: re-runs Stage 1 (connect 250 / version 113 / global-stop
        /// precondition 111), re-confirms Stages 2/3, re-runs Stage 4 (config APPLY + verify) and
        /// Stage 5 (GROUP_READY + verify), and stops before the operator-gated Stage 6. The reconnect
        /// does NOT clear a latched global stop, does NOT re-arm, and does NOT re-grant movement.
        /// </summary>
        Task<InitializationStatus> RevalidateAsync(CancellationToken ct = default);

        /// <summary>
        /// True when an explicit firmware deployment is currently authorized (in-memory,
        /// session-scoped). Never set by automatic/manual init or recovery; cleared on completion
        /// or restart.
        /// </summary>
        bool FirmwareDeploymentAuthorized { get; }

        /// <summary>
        /// Explicit operator action that authorizes exactly ONE bounded firmware deployment for the
        /// given target amplifiers against the given expected firmware SHA-256. Reuses the proven
        /// per-amplifier command-131 flash path (never 122). The deployment additionally requires
        /// the existing controller-session lease (acquired by the shared runner), so no caller can
        /// bypass both the explicit authorization and the ownership rules. The grant is consumed
        /// (one-shot) regardless of the outcome.
        /// </summary>
        Task<IReadOnlyList<FlashOneRunResult>> AuthorizeFirmwareDeploymentAsync(
            IReadOnlyCollection<byte> targetAmps,
            string expectedSha256Hex,
            CancellationToken ct = default);

        /// <summary>
        /// The active diagnostic instrumentation mode. <see cref="TrackInitDiagnosticMode.None"/>
        /// (production default) leaves the initialization sequence unchanged;
        /// <see cref="TrackInitDiagnosticMode.PauseAfterStage4"/> inserts an operator gate after
        /// Stage 4 (configure/validate) before Stage 5 (GROUP_READY). Set while the runtime is
        /// stopped, before <see cref="StartAsync"/>.
        /// </summary>
        TrackInitDiagnosticMode DiagnosticMode { get; set; }

        /// <summary>
        /// True while the Variant-A diagnostic pause is active: Stage 4 has passed (every required
        /// amplifier STANDBY), GROUP_READY has NOT been issued, and the heartbeat monitor keeps
        /// running. The pause is an await on the live runtime cancellation token — never a blocking
        /// call and never a cancellation of that token by the pause itself.
        /// </summary>
        bool IsPausedAtStage4 { get; }

        /// <summary>
        /// Explicit operator action that ends the Stage-4 diagnostic pause and resumes the normal
        /// Stage 5 (domain-wide GROUP_READY). A no-op when the runtime is not paused. This is the
        /// "diagnostic-termination" path, distinct from an operator stop (cancel) and from a real
        /// init failure.
        /// </summary>
        void ResumeStage4Pause();

        /// <summary>
        /// Read-only diagnostic read of a single amplifier (HR1 control/lease, HR8 state, HR9 fault,
        /// HR22 protocol) via the existing 108 read + SLAVEINFO cache. No second Modbus master, no
        /// competing UDP session, no backdoor. The returned reading carries an explicit
        /// FRESH / CACHED / STALE / UNKNOWN label; only a FRESH reading may be treated as live PIC18
        /// state. Never presents a stale cache value as the live state.
        /// </summary>
        Task<SingleAmplifierDiagnosticReading> ReadAmplifierDiagnosticsAsync(
            byte slaveNumber,
            CancellationToken ct = default);

        /// <summary>
        /// Typed single-amplifier GROUP_READY diagnostic operation: writes HR_TRACK_POWER bit 1 to
        /// exactly one target amplifier and observes its HR_STATE. Exactly ONE transaction per
        /// authorization, no auto retry, no generic register-write console, no direct HR23 write
        /// outside the runtime owner, and no config/lease guard bypass. The control layer (this
        /// runtime) enforces the gate: the operation is refused unless the runtime is paused at
        /// Stage 4 and the target is a valid, in-domain track amplifier. GROUP_READY never energizes.
        /// </summary>
        Task<SingleAmplifierGroupReadyResult> IssueSingleGroupReadyAsync(
            byte targetAmp,
            CancellationToken ct = default);
    }
}
