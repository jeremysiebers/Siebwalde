// File: SiebwaldeApp.Core/TrackApplication.cs

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Aggregate root: owns blocks and subsystem controllers.
    /// </summary>
    public class TrackApplication
    {
        private readonly ITrackIn _trackIn;
        private readonly ITrackOut _trackOut;
        private CancellationTokenSource _cts;
        private ILogger _trackapplicationloging;

        // Logger instance
        private static string? _loggerInstance;

        // Get a new log factory
        static ILogger GetLogger(string file, string loggerinstance)
        {
            return new FileLogger(file, loggerinstance);
        }

        public TrackRegistry Registry { get; } = new();

        public StationController Station { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackApplication"/> class.
        /// </summary>
        /// <param name="trackIn">The input track interface used for receiving data or operations related to the track.</param>
        /// <param name="trackOut">The output track interface used for sending data or operations related to the track.</param>
        /// <param name="yardIn">The input yard interface used for receiving data or operations related to the yard.</param>
        /// <param name="yardOut">The output yard interface used for sending data or operations related to the yard.</param>
        public TrackApplication(ITrackIn trackIn, ITrackOut trackOut)
        {
            _trackIn = trackIn;
            _trackOut = trackOut;

            // Set logger instance
            _loggerInstance = "TrackApplication";
            // create logging instance for Track application
            _trackapplicationloging = GetLogger(SiebwaldeApp.Core.Properties.CoreSettings.Default.LogDirectory + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + "TrackApplication.txt", _loggerInstance);
            IoC.Logger.AddLogger(_trackapplicationloging);

            IoC.Logger.Log("Track Application instantiated.", _loggerInstance);

            Station = new StationController(this, _trackIn, _trackOut, _loggerInstance);
            WireBuses();
        }

        /// <summary>
        /// Subscribes to the amplifier occupancy change event and updates the feedback state of the corresponding
        /// amplifier based on the track number.
        /// </summary>
        /// <remarks>This method wires the amplifier occupancy change event to dynamically update the
        /// feedback state of the amplifier associated with the specified track number. The event handler retrieves the
        /// corresponding registry entry and updates the amplifier's feedback state if the entry exists.</remarks>
        private void WireBuses()
        {
            _trackIn.AmplifierOccupiedChanged += e =>
            {
                var entry = Registry.Get(e.TrackNumber);
                entry?.Block.Amplifier.SetFeedback(e.OccupiedOut);
            };
        }

        /// <summary>
        /// Starts the station's processing loops asynchronously.
        /// </summary>
        /// <remarks>This method initializes a linked <see cref="CancellationTokenSource"/> using the
        /// provided token and starts the station's processing loops. The operation completes immediately, and the
        /// processing loops run independently.</remarks>
        /// <param name="token">An optional <see cref="CancellationToken"/> that can be used to cancel the operation. If not provided, a
        /// default token is used.</param>
        /// <returns>A completed <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task StartAsync(CancellationToken token = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            IoC.Logger.Log("Track Application started StationConroller.", _loggerInstance);
            Station.Start(_cts.Token);   // start Top/Bottom loops
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the current operation and releases associated resources.
        /// </summary>
        /// <remarks>This method cancels any ongoing tasks and stops the station.  It is safe to call this
        /// method multiple times.</remarks>
        public void Stop()
        {
            try { _cts?.Cancel(); } catch { }
            Station.Stop();
        }

        public void SetEntrySignal(bool isTopSide, bool green) => _trackOut.SetSignalEntry(isTopSide, green);
        public void SetExitSignal(bool isTopSide, bool green) => _trackOut.SetSignalExit(isTopSide, green);
    }
}
