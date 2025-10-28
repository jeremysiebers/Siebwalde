// File: SiebwaldeApp.Core/Station/StationSide.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    public enum StationState
    {
        Idle,
        TrainIncoming,
        AssigningTrack,
        Stopping,
        TrainWaiting,
        Departing
    }

    public enum StationDirection
    {
        Inbound,
        Outbound,
        Passing
    }

    /// <summary>
    /// One side of the station (Top or Bottom).
    /// Owns 3 tracks (resolved by zone via TrackApplication.Registry) and runs its own local loop.
    /// Implements dwell/priority logic: Passenger first when requested,
    /// Freight only when exit is free (and min dwell elapsed).
    /// 
    /// NOTE: StationTrack does not yet know TrackBlock/Amplifier. Therefore this class
    /// also issues amplifier commands by resolving the block from TrackApplication.Registry.
    /// </summary>
    public class StationSide
    {
        #region private fields

        private readonly string _name;
        private readonly string _zone;
        private readonly int _middleTrackNumber;
        private readonly TrackApplication _app; // root (to query registry / command amps)
        private readonly ITrackOut _trackOut;
        private readonly string _loggerInstance;

        private readonly List<StationTrack> _tracks = new();

        private CancellationTokenSource _localCts;

        // Exit availability is provided by StationControl (e.g., block/signal ahead).
        private volatile bool _exitFree;

        // Pending preference from SW (e.g., "depart a passenger now" vs "freight").
        private readonly object _sync = new();
        private TrainType? _preferredDeparture;

        // Simple local state machine for diagnostics.
        private StationState _currentState = StationState.Idle;

        // --- Signal & route helpers (defaults; make configurable later via WPF) ---
        private readonly TimeSpan _switchWait = TimeSpan.FromMilliseconds(350);      // wait after setting switches
        private readonly TimeSpan _outboundSettleWait = TimeSpan.FromMilliseconds(80); // short settle before signal change

        #endregion

        #region public api

        // Expose middle track (passing track) and current exit availability to the controller.
        public int MiddleTrackNumber => _middleTrackNumber;
        public bool ExitIsFree => _exitFree;
        public bool IsTopSide { get;  }

        public string Name => _name;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Side name (e.g., "Top", "Bottom")</param>
        /// <param name="zone">Registry zone that contains this side's 3 station tracks (e.g., "StationTop")</param>
        /// <param name="middleTrackNumber">Physical number of the middle (freight) track (e.g., 12 or 3)</param>
        /// <param name="app">TrackApplication root (to query the registry)</param>
        /// <param name="loggerInstance">Logger category/instance name</param>
        public StationSide(string name, string zone, int middleTrackNumber, TrackApplication app, bool isTopSide, ITrackOut trackOut, string loggerInstance)
        {
            _name = name;
            _zone = zone;
            _middleTrackNumber = middleTrackNumber;
            _app = app ?? throw new ArgumentNullException(nameof(app));
            IsTopSide = isTopSide;
            _trackOut = trackOut ?? throw new ArgumentNullException(nameof(trackOut));
            _loggerInstance = loggerInstance;

            // Resolve this side's blocks by zone and create StationTrack models (by number)
            var blocks = _app.Registry.Query(zone: _zone).OrderBy(b => b.Id).ToList();
            foreach (var b in blocks)
                _tracks.Add(new StationTrack(b.Id, _loggerInstance));

            IoC.Logger.Log(
                $"Instantiate {_name} Tracks from zone '{_zone}' [{string.Join(",", _tracks.Select(t => t.Number))}]...",
                _loggerInstance);
        }

        /// <summary>
        /// Starts the execution loop for this side.
        /// </summary>
        public void Start(CancellationToken globalToken)
        {
            IoC.Logger.Log($"Start {_name} RunLoop.", _loggerInstance);
            _localCts = CancellationTokenSource.CreateLinkedTokenSource(globalToken);
            Task.Run(() => RunLoop(_localCts.Token));
        }

        public void Stop() => _localCts?.Cancel();

        /// <summary>
        /// Called by StationControl when a train is routed to this side.
        /// </summary>
        public void HandleIncomingTrain(StationTrack reservedTrack, TrainType type)
        {
            if (reservedTrack == null) return;

            // Passing is only allowed on the middle track (12 or 3) AND if exit is free AND (policy) typically for freight
            bool isMiddle = reservedTrack.Number == MiddleTrackNumber;
            bool isPassing = isMiddle && _exitFree && (type == TrainType.Freight);

            // Kick off the inbound preparation (route + entry signal policy)
            _ = PrepareInboundAsync(reservedTrack, isPassing);

            _currentState = StationState.TrainIncoming;
            IoC.Logger.Log($"{_name}: incoming {type} assigned to track {reservedTrack.Number}", _loggerInstance);
        }

        /// <summary>
        /// StationControl informs this side whether the exit block/signal is currently free.
        /// </summary>
        public void SetExitAvailability(bool isFree)
        {
            _exitFree = isFree;
            if (isFree)
            {
                // Try depart immediately when exit becomes free
                TryDepartOneIfPossible();
            }
        }

        /// <summary>
        /// SW can hint which type to prioritize for the next departure.
        /// </summary>
        public void RequestPreferredDeparture(TrainType type)
        {
            lock (_sync) _preferredDeparture = type;

            foreach (var t in _tracks.Where(t => t.IsOccupied && t.OccupantType == type))
                t.RequestDeparture();

            IoC.Logger.Log($"{_name}: preferred departure requested for {type}", _loggerInstance);
        }

        /// <summary>
        /// Returns a free track. For freight → middle track only; for passenger → any free (outer) track.
        /// Returns null if none available.
        /// </summary>
        public StationTrack GetFreeTrack(bool isFreight)
        {
            if (isFreight)
            {
                var mid = GetByNumber(_middleTrackNumber);
                return (mid != null && mid.IsFree) ? mid : null;
            }

            // Passenger: prefer outers (anything except the middle)
            return _tracks.Where(t => t.Number != _middleTrackNumber)
                          .FirstOrDefault(t => t.IsFree);
        }

        /// <summary>
        /// Track lookup by physical number; returns null if not part of this side.
        /// </summary>
        public StationTrack GetByNumber(int trackNumber) =>
            _tracks.FirstOrDefault(t => t.Number == trackNumber);

        /// <summary>
        /// Call when entry sensor triggers for a specific reserved track.
        /// - Sends amplifier STOP via registry (Occupied IN)
        /// - Marks StationTrack as occupied (with dwell start)
        /// </summary>
        public void ConfirmArrivalAndStop(StationTrack track, TrainType type)
        {
            if (track == null) return;

            // Stop the train on the assigned station track.
            // Keep entry signal RED for storage tracks; entry OCC_TO re-engaged elsewhere.
            track.StopTrain(_trackOut);

            // Mark track as occupied (starts dwell timer).
            track.Occupy(type);

            _currentState = StationState.TrainWaiting; // now at platform
            IoC.Logger.Log($"{_name}: track {track.Number} occupied by {type}", _loggerInstance);
        }

        /// <summary>
        /// Call when the train has fully left the track (exit sensor cleared / amplifier OUT cleared).
        /// </summary>
        public void ConfirmTrainLeft(StationTrack track)
        {
            if (track == null) return;

            track.Release();
            SetExitSignal(track, green: false);
            _currentState = HasAnyOccupied() ? StationState.TrainWaiting : StationState.Idle;

            IoC.Logger.Log($"{_name}: track {track.Number} cleared", _loggerInstance);
        }

        /// <summary>
        /// Emergency: release all occupied tracks (logic side only).
        /// </summary>
        public void EmergencyReleaseAllTracks()
        {
            foreach (var t in _tracks.Where(t => t.IsOccupied))
                t.Release();

            _currentState = StationState.Idle;
            IoC.Logger.Log($"{_name}: EMERGENCY RELEASE executed", _loggerInstance);
        }

        #endregion

        #region private loop & policy

        private async Task RunLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    switch (_currentState)
                    {
                        case StationState.Idle:
                            // Wait for HandleIncomingTrain(...)
                            break;

                        case StationState.TrainIncoming:
                            // Waiting for sensor -> ConfirmArrivalAndStop(...)
                            break;

                        case StationState.TrainWaiting:
                            // Evaluate departures regularly
                            TryDepartOneIfPossible();
                            break;

                        case StationState.Departing:
                            // After StartTrain() we fall back based on remaining occupancy
                            _currentState = HasAnyOccupied() ? StationState.TrainWaiting : StationState.Idle;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    IoC.Logger.Log($"{_name}: RunLoop error: {ex.Message}", _loggerInstance);
                }

                await Task.Delay(200, token);
            }
        }

        private bool HasAnyOccupied() => _tracks.Any(t => t.IsOccupied);

        /// <summary>
        /// Core departure policy:
        /// 1) If preferred Passenger and any Passenger is ready -> depart that passenger.
        /// 2) Else any Passenger ready -> depart passenger.
        /// 3) Else if preferred Freight and any Freight is ready -> depart freight.
        /// 4) Else any Freight ready -> depart freight.
        /// Also issues amplifier START for the chosen track.
        /// </summary>
        private void TryDepartOneIfPossible()
        {
            if (!_exitFree) return;

            TrainType? preferred;
            lock (_sync) preferred = _preferredDeparture;

            StationTrack PickReady(Func<StationTrack, bool> pred) =>
                _tracks.FirstOrDefault(t => t.IsOccupied && pred(t) && t.IsReadyToDepart(_exitFree));

            StationTrack candidate = null;

            if (preferred == TrainType.Passenger)
                candidate = PickReady(t => t.OccupantType == TrainType.Passenger && t.DepartureRequested);

            if (candidate == null)
                candidate = PickReady(t => t.OccupantType == TrainType.Passenger);

            if (candidate == null && preferred == TrainType.Freight)
                candidate = PickReady(t => t.OccupantType == TrainType.Freight && t.DepartureRequested);

            if (candidate == null)
                candidate = PickReady(t => t.OccupantType == TrainType.Freight);

            if (candidate == null) return;

            // Fire the async departure sequence (route -> wait -> exit signal GREEN -> start)
            _ = DepartSequenceAsync(candidate);

            _currentState = StationState.Departing;

            // One-shot; clear once used
            lock (_sync) _preferredDeparture = null;

            IoC.Logger.Log($"{_name}: departing track {candidate.Number} ({candidate.OccupantType})", _loggerInstance);
        }

        private async Task DepartSequenceAsync(StationTrack track)
        {
            try
            {
                // 1) Set outbound route (switches) and wait a moment (servo travel)
                await SetPathAsync(track, StationDirection.Outbound);

                // 2) Short settle; then set EXIT signal GREEN for this side
                await Delay(_outboundSettleWait);
                SetExitSignal(track, green: true);

                // 3) Release the track stop so the train can leave
                track.StartTrain(_trackOut);

                // Signal back to RED is handled when the train clears (OnTrackCleared).
            }
            catch (Exception ex)
            {
                IoC.Logger.Log($"{_name}: DepartSequence error on track {track.Number} -> {ex.Message}", _loggerInstance);
            }
        }                

        private void SetEntrySignal(bool green)
        {
            _trackOut.SetSignalEntry(IsTopSide /* station side */, green);
        }

        private void SetExitSignal(StationTrack track, bool green)
        {
            _trackOut.SetSignalExit(IsTopSide /* station side */, green);
        }            

        // Utility: tiny, local delay (later: consider a CancellationToken if you have one in StationSide)
        private static Task Delay(TimeSpan t) => Task.Delay(t);

        /// <summary>
        /// Prepares the inbound route and entry signal policy *before* the train arrives at the station track.
        /// - Sets the switch route towards the chosen track and waits for servo travel.
        /// - Entry signal policy:
        ///   * Storage tracks (10/11 or 1/2): keep entry RED
        ///   * Passing on the middle track (12 or 3) with free exit: entry GREEN
        /// </summary>
        private async Task PrepareInboundAsync(StationTrack track, bool isPassing)
        {
            try
            {
                // 1) Set inbound route (switches) to this track and wait briefly
                await SetPathAsync(track, StationDirection.Inbound, isPassing);
                
                // 2) Entry signal policy (GREEN only if passing on the middle track and exit is free)
                //SetEntrySignal(green: isPassing);

                // NOTE:
                // We do NOT release/engage inbound OCC_TO here; keep that in your existing inbound logic.
                // We also do NOT touch the station track stop here; ConfirmArrivalAndStop() will STOP on arrival.
            }
            catch (Exception ex)
            {
                IoC.Logger.Log($"{_name}: PrepareInbound error for track {track?.Number} -> {ex.Message}", _loggerInstance);
            }
        }

        // Nullable = "don’t touch this switch in this direction"
        public readonly record struct PathState(bool? W1, bool? W2, bool? W3, bool? W4);

        private PathState ComputePathState(StationTrack track, StationDirection dir)
        {
            var t = track.Number; // 1,2,3,10,11,12

            switch (dir)
            {
                case StationDirection.Inbound:
                    return new PathState(
                        W1: t is 1 or 10 or 11 ? true : t is 2 or 3 or 12 ? false : null,
                        W2: t is 2 or 10 ? true : t is 3 or 11 ? false : null,
                        W3: null, // not touched by inbound in your C
                        W4: null  // not touched by inbound in your C
                    );

                case StationDirection.Outbound:
                    return new PathState(
                        W1: null, // not touched by outbound in your C
                        W2: null,
                        W3: t is 1 or 11 ? true : t is 2 or 12 ? false : null,
                        W4: t is 1 or 2 or 10 ? true : t is 3 or 11 or 12 ? false : null
                    );

                case StationDirection.Passing:
                    return new PathState(
                        W1: t is 1 or 10 or 11 ? true : t is 2 or 3 or 12 ? false : null,
                        W2: t is 2 or 10 ? true : t is 3 or 11 ? false : null,
                        W3: t is 1 or 11 ? true : t is 2 or 12 ? false : null,
                        W4: t is 1 or 2 or 10 ? true : t is 3 or 11 or 12 ? false : null
                    );

                default:
                    return new PathState(null, null, null, null);
            }
        }

        private void ApplyPath(PathState state)
        {
            // Map to your ITrackOut / pin writes. Only touch when HasValue.
            if (state.W1.HasValue) _trackOut.SetSwitch(1, state.W1.Value); // W1/W5
            if (state.W2.HasValue) _trackOut.SetSwitch(2, state.W2.Value); // W2/W6
            if (state.W3.HasValue) _trackOut.SetSwitch(3, state.W3.Value); // W3/W7
            if (state.W4.HasValue) _trackOut.SetSwitch(4, state.W4.Value); // W4/W8
        }

        // Unified entry point: replaces SetPathTo(...) and SetPathOutboundFrom(...)
        public async Task SetPathAsync(StationTrack track, StationDirection dir, bool isPassing = false)
        {
            try
            {
                var path = ComputePathState(track, dir);
                ApplyPath(path);
                await Delay(_switchWait);

                // Entry-signal policy: you previously set it only for inbound prepare.
                // Keep that here so the caller doesn’t need a second call.
                // If you want the “only green when passing” rule, keep as below:
                if (dir is StationDirection.Inbound or StationDirection.Passing)
                {
                    SetEntrySignal(green: dir == StationDirection.Passing && isPassing);
                }
            }
            catch (Exception ex)
            {
                IoC.Logger.Log($"{_name}: SetPath error for track {track?.Number} ({dir}) -> {ex.Message}", _loggerInstance);
            }
        }

        #endregion
    }
}
