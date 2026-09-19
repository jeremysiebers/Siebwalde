using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Owns the ECoS host: the TCP server Koploper connects to (port 15471) and the
    /// translation layer behind it. The implementation lives outside the UI layer, so the
    /// composition, mode selection and lifetime stay out of WPF. WPF only initiates a
    /// start or a stop and reads back which mode is active.
    ///
    /// Mode semantics (see <see cref="StartAsync"/>):
    /// real mode is authoritative over the simulator, because a successfully started real
    /// track application must not stay hidden behind a simulator that was started earlier.
    /// </summary>
    public interface IEcosHostService
    {
        /// <summary>True while the ECoS server is listening.</summary>
        bool IsRunning { get; }

        /// <summary>The mode the host is running in, or null when it is not running.</summary>
        TrackControlMode? Mode { get; }

        /// <summary>
        /// Diagnostics surface for the running host, or null when it is not running. Consumers
        /// read structured diagnostics from here instead of parsing log text.
        /// </summary>
        ControlDiagnostics? Diagnostics { get; }

        /// <summary>True while an unsafe divergence is latched and not yet reset.</summary>
        bool IsUnsafe { get; }

        /// <summary>
        /// Explicit recovery: clears the latched safety state, but only when the underlying
        /// condition revalidates as resolved. A latched fault never clears itself, not even
        /// when a later command arrives.
        /// </summary>
        /// <returns>True when the reset was applied, false when it was refused.</returns>
        bool ResetSafety();

        /// <summary>
        /// Starts the host in the requested mode and reports what happened.
        ///
        /// - requesting the already active mode is an idempotent no-op (<see cref="EcosHostStartResult.AlreadyActive"/>);
        /// - requesting <see cref="TrackControlMode.Simulator"/> while
        ///   <see cref="TrackControlMode.Real"/> is active is refused
        ///   (<see cref="EcosHostStartResult.Rejected"/>);
        /// - requesting <see cref="TrackControlMode.Real"/> while
        ///   <see cref="TrackControlMode.Simulator"/> is active stops the simulator cleanly and
        ///   transitions to real mode (<see cref="EcosHostStartResult.Transitioned"/>).
        ///
        /// A rejected or invalid request must never take a running host down.
        /// </summary>
        /// <exception cref="System.ArgumentException">
        /// <see cref="TrackControlMode.Real"/> was requested without a track communication
        /// client or shared variables. The running host is left untouched.
        /// </exception>
        Task<EcosHostStartResult> StartAsync(
            TrackControlMode mode,
            ITrackCommClient? commClient,
            TrackApplicationVariables? variables,
            CancellationToken cancellationToken = default);

        /// <summary>Stops the ECoS host and releases everything it created.</summary>
        void Stop();
    }
}
