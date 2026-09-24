using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

        /// <summary>The ECoS mode that is actually active, or null when the host is not running.</summary>
        TrackControlMode? ActiveEcosMode { get; }

        /// <summary>Diagnostics for the control path, or null when the ECoS host is not running.</summary>
        ControlDiagnostics? ControlDiagnostics { get; }

        /// <summary>True while an unsafe divergence is latched and not yet reset.</summary>
        bool IsControlPathUnsafe { get; }

        /// <summary>Explicit recovery for a latched safety fault. A latched fault never clears itself.</summary>
        bool ResetControlSafety();

        /// <summary>Raised when an amplifier data frame has been parsed and applied.</summary>
        event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;

        /// <summary>Read-only view of the current track amplifiers.</summary>
        IReadOnlyList<TrackAmplifierItem> TrackAmplifiers { get; }

        /// <summary>Returns the current list of track amplifiers (empty when not running).</summary>
        List<TrackAmplifierItem> GetAmplifierListing();

        /// <summary>
        /// Updates the desired control parameters (PWM setpoint + EmoStop) for a given amplifier.
        /// The values are queued and sent by the runtime write loop, not immediately.
        /// </summary>
        void SetAmplifierControl(ushort slaveNumber, int pwmSetpoint, bool emoStop);

        /// <summary>Sets the PWM set point (0..799) for a given amplifier.</summary>
        void SetAmplifierPwm(ushort slaveNumber, int pwm);

        /// <summary>Sets or clears the emergency-stop bit (HoldingReg0 bit 15) for a given amplifier.</summary>
        void SetAmplifierEmStop(ushort slaveNumber, bool isEmStop);
    }
}
