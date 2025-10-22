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
            TopStation = new StationSide("Top", zone: "StationTop", middleTrackNumber: 12, app: _app, isTopSide: true, _out, loggerInstance: _loggerInstance);
            BottomStation = new StationSide("Bottom", zone: "StationBottom", middleTrackNumber: 3, app: _app, isTopSide: false, _out, loggerInstance: _loggerInstance);

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

                // Decide storage target (null means no capacity).
                var free = side.GetFreeTrack(isFreight: e.IsFreight);
                if (free == null)
                {
                    // No capacity: stop early before the station, keep entry at RED.
                    _out.StopBeforeStation(e.IsTopSide);
                    _app.SetEntrySignal(e.IsTopSide, green: false);
                    IoC.Logger.Log($"{side.Name}: no free track, stopping before station", _loggerInstance);
                    return;
                }

                // PASSING is only allowed on the middle track (12 or 3) and only if the exit block is free.
                // For now we allow Freight to pass; Passenger only stops on outer tracks by default.
                bool isMiddle = free.Number == side.MiddleTrackNumber;
                bool allowPassing = isMiddle && side.ExitIsFree && e.IsFreight;

                if (allowPassing)
                {
                    // Do NOT force a storage stop — let it pass through:
                    // - We still reserve locally to keep our bookkeeping simple (optional).
                    free.Reserve();
                    _reservedTypes[free.Number] = type;

                    side.HandleIncomingTrain(free, type);

                    // Entry may turn GREEN to allow immediate pass-through towards the exit
                    _app.SetEntrySignal(e.IsTopSide, green: true);
                    IoC.Logger.Log($"{side.Name}: passing via track {free.Number} ({type}), entry signal GREEN", _loggerInstance);
                }
                else
                {
                    // Storage (10/11 or 1/2) or exit blocked: keep entry RED, train will stop on the assigned track.
                    free.Reserve();
                    _reservedTypes[free.Number] = type;

                    side.HandleIncomingTrain(free, type);

                    // Keep entry RED for storage. The actual stop is executed on entry sensor (ConfirmArrivalAndStop).
                    _app.SetEntrySignal(e.IsTopSide, green: false);
                    IoC.Logger.Log($"{side.Name}: reserved track {free.Number} for {type}, entry signal RED (storage)", _loggerInstance);
                }
            };

            // Exit block free/occupied
            _in.ExitBlockFreeChanged += e =>
            {
                var side = e.IsTopSide ? TopStation : BottomStation;
                side.SetExitAvailability(e.IsFree);

                // Wees expliciet: bij bezet exit meteen sein uit
                if (!e.IsFree)
                    _app.SetExitSignal(e.IsTopSide, green: false);
            };

            // Entry sensor op specifiek station-spoor -> aankomst bevestigen & STOP
            _in.EntrySensorTriggered += e =>
            {
                var side = e.IsTopSide ? TopStation : BottomStation;
                var track = side.GetByNumber(e.TrackNumber);
                if (track == null) return;

                // Haal gereserveerd type op (default: Passenger als onbekend)
                var type = _reservedTypes.TryGetValue(track.Number, out var t) ? t : TrainType.Passenger;
                side.ConfirmArrivalAndStop(track, type);
            };

            // Amplifier OUT feedback (OccupiedOut == true zodra gestopt; false als weggereden)
            _in.AmplifierOccupiedChanged += e =>
            {
                // Top = 10/11/12, Bottom = 1/2/3
                var side = (e.TrackNumber >= 10) ? TopStation : BottomStation;
                var track = side.GetByNumber(e.TrackNumber);
                if (track == null) return;

                if (!e.OccupiedOut)
                {
                    // Trein is het spoor uit -> logische vrijgave + sein terug rood
                    side.ConfirmTrainLeft(track);
                    _reservedTypes.Remove(e.TrackNumber);
                }
            };

            // Downstream clear (redundant met amplifier false, maar veilig om ook te handelen)
            _in.TrainClearedFromBlock += e =>
            {
                var side = e.IsTopSide ? TopStation : BottomStation;
                var track = side.GetByNumber(e.TrackNumber);
                if (track == null) return;

                side.ConfirmTrainLeft(track);
                _reservedTypes.Remove(e.TrackNumber);
            };

            // Hardware heartbeat
            _in.HardwareAliveChanged += e =>
            {
                if (!e.IsAlive)
                {
                    // Fail-safe: alles rood en logische staat opruimen
                    TopStation.EmergencyReleaseAllTracks();
                    BottomStation.EmergencyReleaseAllTracks();
                    _app.SetEntrySignal(true, green: false);
                    _app.SetEntrySignal(false, green: false);
                    _app.SetExitSignal(true, green: false);
                    _app.SetExitSignal(false, green: false);
                }

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
