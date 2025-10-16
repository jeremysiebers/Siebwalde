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

        #endregion

        #region public api

        public string Name => _name;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Side name (e.g., "Top", "Bottom")</param>
        /// <param name="zone">Registry zone that contains this side's 3 station tracks (e.g., "StationTop")</param>
        /// <param name="middleTrackNumber">Physical number of the middle (freight) track (e.g., 12 or 3)</param>
        /// <param name="app">TrackApplication root (to query the registry)</param>
        /// <param name="loggerInstance">Logger category/instance name</param>
        public StationSide(string name, string zone, int middleTrackNumber, TrackApplication app, string loggerInstance)
        {
            _name = name;
            _zone = zone;
            _middleTrackNumber = middleTrackNumber;
            _app = app ?? throw new ArgumentNullException(nameof(app));
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

            // 1) Stop via amplifier (resolve block by number)
            var reg = _app.Registry.Get(track.Number);
            if (reg != null)
            {
                reg.Value.Block.CommandStop();
                IoC.Logger.Log($"{_name}: amplifier STOP on track {track.Number}", _loggerInstance);
            }

            // 2) Maintain StationTrack local state/logging
            track.StopTrain(); // keeps your existing hooks/logs inside StationTrack
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

            // 1) Start via amplifier (resolve block by number)
            var reg = _app.Registry.Get(candidate.Number);
            if (reg != null)
            {
                reg.Value.Block.CommandStart();
                IoC.Logger.Log($"{_name}: amplifier START on track {candidate.Number}", _loggerInstance);
            }

            // 2) Maintain StationTrack local state/logging
            candidate.StartTrain();

            _currentState = StationState.Departing;

            // One-shot; clear once used
            lock (_sync) _preferredDeparture = null;

            IoC.Logger.Log($"{_name}: departing track {candidate.Number} ({candidate.OccupantType})",
                _loggerInstance);
        }

        #endregion
    }
}
