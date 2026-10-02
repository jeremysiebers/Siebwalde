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
        /// The current movement-permission state (observed neutral). Not granted until neutral has
        /// been commanded to and observed on every configured amplifier.
        /// </summary>
        MovementPermissionState MovementPermission { get; }

        /// <summary>Explicit recovery for a latched safety fault. A latched fault never clears itself.</summary>
        bool ResetControlSafety();

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
    }
}
