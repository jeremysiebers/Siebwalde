
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Owns 3 tracks and runs its own local loop.
    /// Implements dwell/priority logic: Passenger first when requested,
    /// Freight only when exit is free (and min dwell elapsed).
    /// </summary>
    public class StationSide
    {
        #region private properties

        private readonly string _name;
        private readonly List<StationTrack> _tracks;

        private CancellationTokenSource _localCts;

        // Exit availability is provided by StationControl (e.g., block/signal ahead).
        private volatile bool _exitFree;

        // Pending preference from SW (e.g., "depart a passenger now" vs "freight").
        private readonly object _sync = new object();
        private TrainType? _preferredDeparture;

        // Simple local state machine for demonstration; you can expand if needed.
        private StationState _currentState = StationState.Idle;

        private readonly string _loggerInstance;

        #endregion

        #region public properties

        // Name of this station side (Top or Bottom)
        public string Name { get { return _name; } }
                
        #endregion

        #region constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public StationSide(string name, IEnumerable<int> trackNumbers, string loggerInstance)
        {   
            _name = name;
            _loggerInstance = loggerInstance;
            _tracks = trackNumbers.Select(num => new StationTrack(num, _loggerInstance)).ToList();
            

            IoC.Logger.Log($"Instantiate " + _name + " Tracks..." , _loggerInstance);
        }

        #endregion

        #region public methods

        /// <summary>
        /// Starts the execution of the run loop for this instance.
        /// </summary>
        /// <remarks>This method initializes a linked <see cref="CancellationTokenSource"/> using the
        /// provided <paramref name="globalToken"/>  and begins the run loop on a separate task. The run loop will
        /// continue until the cancellation token is triggered.</remarks>
        /// <param name="globalToken">A <see cref="CancellationToken"/> that can be used to signal a request to cancel the operation.</param>
        public void Start(CancellationToken globalToken)
        {
            IoC.Logger.Log($"Start " + _name + " RunLoop.", _loggerInstance);
            _localCts = CancellationTokenSource.CreateLinkedTokenSource(globalToken);
            Task.Run(() => RunLoop(_localCts.Token));
        }

        public void Stop() => _localCts?.Cancel();

        /// <summary>
        /// Called by StationControl when a train was routed to this side.
        /// Reserves the chosen track and transitions local flow.
        /// </summary>
        public void HandleIncomingTrain(StationTrack reservedTrack, TrainType type)
        {
            if (reservedTrack == null) return;

            // At this moment the track is reserved; when the train hits the entry sensor,
            // StationSide should call "StopTrain()" and then mark Occupy(type).
            // For now, we go to the TrainIncoming state; sensors should promote to Stopping/Occupied.
            _currentState = StationState.TrainIncoming;

            // Optional log:
            IoC.Logger.Log($"{_name}: incoming {type} assigned to track {reservedTrack.Number}", _loggerInstance);
        }

        /// <summary>
        /// StationControl informs this side whether the exit block/signal is currently free.
        /// </summary>
        public void SetExitAvailability(bool isFree)
        {
            _exitFree = isFree;
        }

        /// <summary>
        /// StationControl (SW) can nudge a preferred departure type.
        /// We keep this lightweight; it only influences the next selection.
        /// </summary>
        public void RequestPreferredDeparture(TrainType type)
        {
            lock (_sync)
            {
                _preferredDeparture = type;
            }

            // Also set "intent" on all tracks that match the type and are occupied.
            foreach (var t in _tracks.Where(t => t.IsOccupied && t.OccupantType == type))
                t.RequestDeparture();
        }

        /// <summary>
        /// Returns a free track. For freight → middle track only; for passenger → any free (outer) track.
        /// Returns null if none available.
        /// </summary>
        public StationTrack GetFreeTrack(bool isFreight)
        {
            if (isFreight)
            {
                var middle = _tracks[2]; // the middle one of the 3
                return middle.IsFree ? middle : null;
            }
            else
            {
                // Outer two: index 0 and 1
                var outer = new[] { _tracks[0], _tracks[1] };
                return outer.FirstOrDefault(t => t.IsFree);
            }
        }

        /// <summary>
        /// Call when entry sensor triggers for a specific reserved track.
        /// Typically: stop amplifier and mark occupied.
        /// </summary>
        public void ConfirmArrivalAndStop(StationTrack track, TrainType type)
        {
            if (track == null) return;
            track.StopTrain();
            track.Occupy(type);
            _currentState = StationState.TrainWaiting; // train is now at platform
        }

        /// <summary>
        /// Call when exit sensor shows train has left this track.
        /// </summary>
        public void ConfirmTrainLeft(StationTrack track)
        {
            track?.Release();
            _currentState = HasAnyOccupied() ? StationState.TrainWaiting : StationState.Idle;
        }

        /// <summary>
        /// Releases all occupied tracks in the station and transitions the station to the idle state.
        /// </summary>
        /// <remarks>This method should be used in emergency scenarios where all tracks need to be cleared
        /// immediately.  Tracks that are not occupied will remain unaffected.</remarks>
        public void EmergencyReleaseAllTracks()
        {
            foreach (var t in _tracks.Where(t => t.IsOccupied))
                t.Release();
            _currentState = StationState.Idle;
        }

        #endregion

        #region private methods

        private async Task RunLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                switch (_currentState)
                {
                    case StationState.Idle:
                        // Nothing to do until HandleIncomingTrain is called.
                        break;

                    case StationState.TrainIncoming:
                        // Waiting for sensor to confirm arrival -> external call ConfirmArrivalAndStop(...)
                        // We stay in this state until ConfirmArrivalAndStop moves us forward.
                        break;

                    case StationState.TrainWaiting:
                        // Evaluate if any train can depart based on dwell/priority/exit.
                        TryDepartOneIfPossible();
                        break;

                    case StationState.Departing:
                        // Transient; after StartTrain() we move back to Idle or stay in Waiting if others remain.
                        _currentState = HasAnyOccupied() ? StationState.TrainWaiting : StationState.Idle;
                        break;
                }

                await Task.Delay(200, token);
            }
        }

        private bool HasAnyOccupied() => _tracks.Any(t => t.IsOccupied);

        /// <summary>
        /// Core departure policy:
        /// 1) If SW prefers Passenger now and any Passenger is ready -> depart that passenger.
        /// 2) Else if any Passenger is ready -> depart a passenger.
        /// 3) Else if SW prefers Freight and any Freight is ready -> depart that freight.
        /// 4) Else if any Freight is ready -> depart a freight.
        /// </summary>
        private void TryDepartOneIfPossible()
        {
            TrainType? preferred;
            lock (_sync) { preferred = _preferredDeparture; }

            StationTrack PickReady(Func<StationTrack, bool> pred) =>
                _tracks.FirstOrDefault(t => t.IsOccupied && pred(t) && t.IsReadyToDepart(_exitFree));

            StationTrack candidate = null;

            // 1) Preferred passenger
            if (_preferredDeparture == TrainType.Passenger)
                candidate = PickReady(t => t.OccupantType == TrainType.Passenger && t.DepartureRequested);

            // 2) Any passenger ready
            if (candidate == null)
                candidate = PickReady(t => t.OccupantType == TrainType.Passenger);

            // 3) Preferred freight
            if (candidate == null && _preferredDeparture == TrainType.Freight)
                candidate = PickReady(t => t.OccupantType == TrainType.Freight && t.DepartureRequested);

            // 4) Any freight ready
            if (candidate == null)
                candidate = PickReady(t => t.OccupantType == TrainType.Freight);

            if (candidate == null) return;

            // We have a candidate: perform departure
            candidate.StartTrain();

            // After the train clears the block, sensors should call ReleaseFor(candidate)
            // For now we simulate immediate release or leave to external sensor logic.
            // candidate.Release();

            // Clear the one-shot preference once used (optional behavior).
            _preferredDeparture = null;

            _currentState = StationState.Departing;
            IoC.Logger.Log($"{_name}: departing track {candidate.Number} ({candidate.OccupantType})", _loggerInstance);
        }
        #endregion
    }
}
