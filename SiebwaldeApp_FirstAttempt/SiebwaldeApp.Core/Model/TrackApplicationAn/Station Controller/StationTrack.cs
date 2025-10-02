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
    /// <summary>
    /// Stationtrack class holds all data and properties of a station track
    /// </summary>
    public class StationTrack : INotifyPropertyChanged
    {
        #region Private properties

        private readonly string LoggerInstance;

        #endregion

        #region Public properties

        public int Number { get; }
        public TrackState State { get; private set; } = TrackState.Free;

        public bool IsFree => State == TrackState.Free;
        public bool IsOccupied => State == TrackState.Occupied;

        public event Action<int, TrackState> StateChanged;

        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };

        #endregion

        #region constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        public StationTrack(int number, string loggerInstance)
        {
            Number = number;
            LoggerInstance = loggerInstance;
        }

        #endregion

        #region public methods

        public void Reserve()
        {
            if (State == TrackState.Free)
            {
                State = TrackState.Reserved;
                StateChanged?.Invoke(Number, State);
                IoC.Logger.Log($"Track{Number} has changed state to {State}", LoggerInstance);
            }
        }

        public void Occupy()
        {
            if (State == TrackState.Reserved)
            {
                State = TrackState.Occupied;
                StateChanged?.Invoke(Number, State);
                IoC.Logger.Log($"Track{Number} has changed state to {State}", LoggerInstance);
            }
        }

        public void ReleaseTrain()
        {
            if (State == TrackState.Occupied)
            {
                State = TrackState.Free;
                StateChanged?.Invoke(Number, State);
                IoC.Logger.Log($"Track{Number} has changed state to {State}", LoggerInstance);
            }
        }

        public void StopTrain()
        {
            // TODO: send stop command to amplifier
        }

        public void StartTrain()
        {
            // TODO: send start command to amplifier
        }

        #endregion
    }
}
