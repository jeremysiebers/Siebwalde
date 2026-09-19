using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Owns the ECoS host: the TCP server Koploper connects to (port 15471) and the
    /// translation layer behind it. The implementation lives outside the UI layer, so the
    /// composition, mode selection and lifetime stay out of WPF. WPF only initiates a
    /// start or a stop.
    /// </summary>
    public interface IEcosHostService
    {
        /// <summary>True while the ECoS server is listening.</summary>
        bool IsRunning { get; }

        /// <summary>The mode the host is running in, or null when it is not running.</summary>
        TrackControlMode? Mode { get; }

        /// <summary>
        /// Starts the ECoS host. <see cref="TrackControlMode.Real"/> requires the track
        /// communication client and the shared track variables; in
        /// <see cref="TrackControlMode.Simulator"/> both may be null. Starting an already
        /// running host is ignored so the caller cannot accidentally take down Koploper.
        /// </summary>
        Task StartAsync(
            TrackControlMode mode,
            ITrackCommClient? commClient,
            TrackApplicationVariables? variables,
            CancellationToken cancellationToken = default);

        /// <summary>Stops the ECoS host and releases everything it created.</summary>
        void Stop();
    }
}
