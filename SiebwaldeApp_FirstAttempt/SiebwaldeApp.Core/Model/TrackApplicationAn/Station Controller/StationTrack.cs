using System;
using System.ComponentModel;

namespace SiebwaldeApp.Core
{
    public enum TrackState
    {
        Free,
        Reserved,
        Occupied
    }

    public enum TrainType
    {
        Passenger,
        Freight
    }

    /// <summary>
    /// Represents a single physical station track (with its amplifier + sensors abstraction).
    /// Keeps local state (free/reserved/occupied), the occupant train type,
    /// arrival time, and whether a departure has been requested.
    /// </summary>
    public class StationTrack
    {
        #region Private properties

        private readonly string _loggerInstance;

        #endregion

        #region Public properties

        public int Number { get; }
        public TrackState State { get; private set; } = TrackState.Free;

        public bool IsFree => State == TrackState.Free;
        public bool IsOccupied => State == TrackState.Occupied;

        public TrainType? OccupantType { get; private set; }
        public DateTime? ArrivalUtc { get; private set; }
        public bool DepartureRequested { get; private set; }

        // Configurable dwell times (tweak per your real timetable)
        public TimeSpan PassengerDwell { get; set; } = TimeSpan.FromSeconds(12);
        public TimeSpan FreightMinDwell { get; set; } = TimeSpan.FromSeconds(6); // optional minimum

        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event Action<int, TrackState> StateChanged;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="StationTrack"/> class with the specified track number and
        /// logger instance.
        /// </summary>
        /// <param name="number">The track number associated with this station track.</param>
        /// <param name="loggerInstance">The identifier for the logger instance used to log operations related to this station track. Cannot be null
        /// or empty.</param>
        public StationTrack(int number, string loggerInstance)
        {
            Number = number;
            _loggerInstance = loggerInstance;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Reserves the track if it is currently free.
        /// </summary>
        /// <remarks>Changes the track's state to <see cref="TrackState.Reserved"/> and raises the <see
        /// cref="StateChanged"/> event.  If the track is not in the <see cref="TrackState.Free"/> state, the method
        /// does nothing.</remarks>
        public void Reserve()
        {
            if (State != TrackState.Free) return;
            State = TrackState.Reserved;
            StateChanged?.Invoke(Number, State);
            IoC.Logger.Log($"Track{Number} has changed state to {State}", _loggerInstance);            
        }

        /// <summary>
        /// Marks the track as occupied and updates its state and metadata.
        /// </summary>
        /// <remarks>This method sets the track's state to <see cref="TrackState.Occupied"/> and records
        /// the  time of arrival in UTC. It also resets the departure request flag and raises the  <see
        /// cref="StateChanged"/> event to notify subscribers of the state change.</remarks>
        public void Occupy(TrainType type)
        {
            // Called when the train actually reaches the track and is stopped by the amplifier.
            State = TrackState.Occupied;
            OccupantType = type;
            ArrivalUtc = DateTime.UtcNow;
            DepartureRequested = false;
            StateChanged?.Invoke(Number, State);
            IoC.Logger.Log($"Track{Number} has changed state to {State}", _loggerInstance);
        }

        /// <summary>
        /// Requests a departure for the current station if it is occupied.
        /// </summary>
        /// <remarks>This method sets the intent to depart by marking the departure as requested.  It has
        /// no effect if the station is not occupied.</remarks>
        public void RequestDeparture()
        {
            // Called by StationSide when SW requested a departure (this sets intent/priority).
            if (IsOccupied) DepartureRequested = true;
        }

        /// <summary>
        /// Determines whether the occupant of the track is ready to depart based on its type, dwell time, and exit
        /// availability.
        /// </summary>
        /// <remarks>The readiness to depart depends on the type of the occupant: - Passenger trains must
        /// complete their dwell time and require the exit to be free. - Freight trains require the exit to be free and
        /// may also need to complete a minimal dwell time. If the track is unoccupied or the occupant type is unknown,
        /// the method will return <see langword="false"/>.</remarks>
        /// <param name="exitFree">A value indicating whether the exit path is clear for departure.  Must be <see langword="true"/> for
        /// departure to be allowed.</param>
        /// <returns><see langword="true"/> if the occupant is ready to depart; otherwise, <see langword="false"/>. For passenger
        /// trains, this requires the dwell time to be completed and the exit to be free. For freight trains, this
        /// requires the exit to be free and optionally a minimal dwell time to be completed.</returns>
        public bool IsReadyToDepart(bool exitFree)
        {
            if (!IsOccupied || OccupantType is null) return false;

            var now = DateTime.UtcNow;

            if (OccupantType == TrainType.Passenger)
            {
                // Passenger must finish dwell AND exit must be free.
                var sinceArrival = ArrivalUtc.HasValue ? now - ArrivalUtc.Value : TimeSpan.MaxValue;
                return sinceArrival >= PassengerDwell && exitFree;
            }
            else // Freight
            {
                // Freight can depart when exit is free (and optionally past a minimal dwell).
                var sinceArrival = ArrivalUtc.HasValue ? now - ArrivalUtc.Value : TimeSpan.MaxValue;
                return exitFree && sinceArrival >= FreightMinDwell;
            }
        }

        /// <summary>
        /// Releases the track, marking it as free and resetting its state.
        /// </summary>
        /// <remarks>This method is typically called when a train leaves the track. It resets the track's
        /// state,  clears any occupant information, and raises the <see cref="StateChanged"/> event to notify 
        /// listeners of the state change.</remarks>
        public void Release()
        {
            // Called when train left the track.
            State = TrackState.Free;
            OccupantType = null;
            ArrivalUtc = null;
            DepartureRequested = false;
            StateChanged?.Invoke(Number, State);
        }

        /// <summary>
        /// Stops the train by sending a stop command to the amplifier.
        /// </summary>
        /// <remarks>This method halts the train's operation. Ensure that the train is in a state where
        /// stopping is safe  before calling this method. The exact behavior depends on the amplifier's
        /// implementation.</remarks>
        public void StopTrain()
        {
            // The actual amplifier STOP is executed by StationSide (via Registry).
            IoC.TrackAdapter.RequireOut().SetAmplifierStop(Number, true);
            IoC.Logger.Log($"Track{Number}: STOP (storage stop engaged)", _loggerInstance);
        }

        /// <summary>
        /// Sends a command to start the training process.
        /// </summary>
        /// <remarks>This method initiates the training process by sending a start command to the
        /// amplifier.  Ensure that the amplifier is properly configured and ready to receive commands before calling
        /// this method.</remarks>
        public void StartTrain()
        {
            // The actual amplifier START is executed by StationSide (via Registry).
            IoC.TrackAdapter.RequireOut().SetAmplifierStop(Number, false);
            IoC.Logger.Log($"Track{Number}: START (departure released)", _loggerInstance);
        }

        #endregion
    }
}
