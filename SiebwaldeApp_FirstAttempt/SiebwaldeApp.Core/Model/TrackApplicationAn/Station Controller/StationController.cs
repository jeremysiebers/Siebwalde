// File: SiebwaldeApp.Core/Station/StationController.cs

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Coordinates both sides of the station.
    /// Listens to track I/O events and delegates per-side logic to StationSide.
    /// </summary>
    public class StationController
    {
        private readonly TrackApplication _app;
        private readonly ITrackIn _in;
        private readonly ITrackOut _out;
        private readonly string _loggerInstance;

        public StationSide TopStation { get; }
        public StationSide BottomStation { get; }

        // Track -> reserved train type (so on entry sensor we know Passenger vs Freight)
        private readonly Dictionary<int, TrainType> _reservedTypes = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="StationController"/> class, which manages the operations of a
        /// station, including its top and bottom sides, and facilitates interactions with track input and output
        /// systems.
        /// </summary>
        /// <remarks>This constructor initializes the top and bottom sides of the station with predefined
        /// zones and middle track numbers. It also sets up event wiring and logs the initialization process using the
        /// specified logger instance.</remarks>
        /// <param name="app">The application instance that provides core functionality and services for the station.</param>
        /// <param name="trackIn">The track input system used to handle incoming operations at the station.</param>
        /// <param name="trackOut">The track output system used to handle outgoing operations from the station.</param>
        /// <param name="loggerInstance">The name of the logger instance used for logging station-related events. Defaults to "Station" if not
        /// specified.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/>, <paramref name="trackIn"/>, or <paramref name="trackOut"/> is <see
        /// langword="null"/>.</exception>
        public StationController(TrackApplication app, ITrackIn trackIn, ITrackOut trackOut, string loggerInstance)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _in = trackIn ?? throw new ArgumentNullException(nameof(trackIn));
            _out = trackOut ?? throw new ArgumentNullException(nameof(trackOut));
            _loggerInstance = loggerInstance;

            // Create sides based on zones + middle track numbers
            TopStation = new StationSide("Top", zone: "StationTop", middleTrackNumber: 12, app: _app, loggerInstance: _loggerInstance);
            BottomStation = new StationSide("Bottom", zone: "StationBottom", middleTrackNumber: 3, app: _app, loggerInstance: _loggerInstance);

            WireEvents();
            IoC.Logger.Log("StationController initialized", _loggerInstance);
        }

        /// <summary>
        /// Starts the operations of both the top and bottom stations.
        /// </summary>
        /// <remarks>This method initiates the start process for both the top and bottom stations.  The
        /// operation respects the provided <paramref name="token"/> to allow for cancellation.</remarks>
        /// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        public void Start(CancellationToken token)
        {
            TopStation.Start(token);
            BottomStation.Start(token);
        }

        /// <summary>
        /// Stops the operation of both the top and bottom stations.
        /// </summary>
        /// <remarks>This method halts the processes of the top and bottom stations by invoking their
        /// respective stop operations. Ensure that any ongoing tasks or operations are completed or safely interrupted
        /// before calling this method.</remarks>
        public void Stop()
        {
            TopStation.Stop();
            BottomStation.Stop();
        }

        private void WireEvents()
        {
            // Train approaching before station
            _in.IncomingDetected += e =>
            {
                var side = e.IsTopSide ? TopStation : BottomStation;
                var type = e.IsFreight ? TrainType.Freight : TrainType.Passenger;

                var free = side.GetFreeTrack(isFreight: e.IsFreight);
                if (free == null)
                {
                    // No capacity: stop before station and hold entry signal at red
                    _out.StopBeforeStation(e.IsTopSide);
                    _app.SetEntrySignal(e.IsTopSide, green: false);
                    IoC.Logger.Log($"{side.Name}: no free track, stopping before station", _loggerInstance);
                    return;
                }

                // Reserve locally (track knows only Reserved state; we keep the type here)
                free.Reserve();
                _reservedTypes[free.Number] = type;

                side.HandleIncomingTrain(free, type);

                // Allow entry (route safety checks can be added later)
                _app.SetEntrySignal(e.IsTopSide, green: true);
                IoC.Logger.Log($"{side.Name}: reserved track {free.Number} for {type}, entry signal GREEN", _loggerInstance);
            };

            // Entry sensor at a specific track fired → stop amplifier and mark occupied
            _in.EntrySensorTriggered += e =>
            {
                var side = e.IsTopSide ? TopStation : BottomStation;
                var track = side.GetByNumber(e.TrackNumber);
                if (track == null)
                {
                    IoC.Logger.Log($"{side.Name}: entry sensor on unknown track {e.TrackNumber}", _loggerInstance);
                    return;
                }

                if (!_reservedTypes.TryGetValue(e.TrackNumber, out var type))
                {
                    // Fallback if missing reservation — assume Passenger to be safe
                    type = TrainType.Passenger;
                }

                side.ConfirmArrivalAndStop(track, type);

                // Close entry again by default
                _app.SetEntrySignal(e.IsTopSide, green: false);
            };

            // Exit block (beyond station) availability
            _in.ExitBlockFreeChanged += e =>
            {
                var side = e.IsTopSide ? TopStation : BottomStation;
                side.SetExitAvailability(e.IsFree);

                // Mirror on hardware exit signal for this side
                _app.SetExitSignal(e.IsTopSide, e.IsFree);
                IoC.Logger.Log($"{side.Name}: exit is {(e.IsFree ? "FREE" : "BLOCKED")}", _loggerInstance);
            };

            // Train has cleared a specific station track (amplifier OUT cleared / exit sensor)
            _in.TrainClearedFromBlock += e =>
            {
                var side = e.IsTopSide ? TopStation : BottomStation;
                var track = side.GetByNumber(e.TrackNumber);
                if (track == null) return;

                side.ConfirmTrainLeft(track);
                _reservedTypes.Remove(e.TrackNumber);
            };
        }

        /// <summary>
        /// Optional: pass departure preference down to a side (from UI, for example).
        /// </summary>
        public void RequestPreferredDeparture(bool isTopSide, TrainType type)
        {
            var side = isTopSide ? TopStation : BottomStation;
            side.RequestPreferredDeparture(type);
        }
    }
}
