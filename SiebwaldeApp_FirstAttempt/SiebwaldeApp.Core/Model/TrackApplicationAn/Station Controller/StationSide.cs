
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
    /// Station class that holds all data and properties of the station
    /// </summary>
    public class StationSide : INotifyPropertyChanged
    {
        #region private properties

        private readonly string _name;
        private readonly List<StationTrack> _tracks;
        private StationState _currentState = StationState.Idle;

        private CancellationTokenSource _localCts;

        private readonly string LoggerInstance;

        #endregion

        #region public properties

        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };

        public string Name { get { return _name; } }
                
        #endregion

        #region constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public StationSide(string name, IEnumerable<int> trackNumbers, string loggerInstance)
        {   
            _name = name;
            LoggerInstance = loggerInstance;
            _tracks = trackNumbers.Select(num => new StationTrack(num, LoggerInstance)).ToList();
            

            IoC.Logger.Log($"Instantiate " + _name + " Tracks..." , LoggerInstance);
        }

        #endregion

        #region public methods

        public void Start(CancellationToken globalToken)
        {
            IoC.Logger.Log($"Start " + _name + " RunLoop.", LoggerInstance);
            _localCts = CancellationTokenSource.CreateLinkedTokenSource(globalToken);
            Task.Run(() => RunLoop(_localCts.Token));
        }

        /// <summary>
        /// Returns a free track. For freight trains → middle track. Returns null if no free track available.
        /// </summary>
        public StationTrack GetFreeTrack(bool isFreight)
        {
            if (isFreight)
            {
                var middle = _tracks[2]; // track 2 of 11 (middelste)
                return middle.IsFree ? middle : null;
            }
            else
            {
                return _tracks.FirstOrDefault(t => t.IsFree);
            }
        }

        public void HandleIncomingTrain(bool isFreight)
        {
            if (isFreight) ReserveMiddleTrack();
            else ReserveFreeTrack();

            _currentState = StationState.TrainIncoming;
        }

        #endregion

        #region private methods

        private async Task RunLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                switch (_currentState)
                {
                    case StationState.TrainIncoming:
                        // Wait for arrival sensor, then stop
                        // ...
                        _currentState = StationState.Stopping;
                        break;

                    case StationState.Stopping:
                        // Stop amplifier, mark track occupied
                        _currentState = StationState.TrainWaiting;
                        break;

                    case StationState.TrainWaiting:
                        await Task.Delay(TimeSpan.FromSeconds(15), token);
                        _currentState = StationState.Departing;
                        break;

                    case StationState.Departing:
                        ReleaseOccupiedTracks();
                        _currentState = StationState.Idle;
                        break;
                }

                await Task.Delay(200, token);
            }
        }

        private void ReserveMiddleTrack() => _tracks[2].Reserve();
        private void ReserveFreeTrack() => _tracks.FirstOrDefault(t => t.IsFree)?.Reserve();
        private void ReleaseOccupiedTracks()
        {
            foreach (var t in _tracks.Where(t => t.IsOccupied))
                t.ReleaseTrain();
        }

        #endregion
    }
}
