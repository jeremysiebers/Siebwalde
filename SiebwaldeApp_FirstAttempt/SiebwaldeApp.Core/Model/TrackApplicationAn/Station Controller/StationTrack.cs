using System;
using System.ComponentModel;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Stationtrack class holds all data and properties of a station track
    /// </summary>
    public class StationTrack : INotifyPropertyChanged
    {
        #region Private properties

        private bool _getOccStn;
        private bool _setOccStn;
        private bool _setSignalRed;
        private bool _setSignalGreen;
        private bool _trackOccupied;
        private TASK_STATE _taskSTATE;
        private Int32 _tWaitTime;
        public System.Timers.Timer _timer;

        #endregion

        #region Public properties

        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };

        /// <summary>
        /// Set the station track name
        /// </summary>
        public Stn stnTrackName { get; private set; }

        /// <summary>
        /// Set the station track number
        /// </summary>
        public Stn stnTrackNumber { get; private set; }

        /// <summary>
        /// Set a time for the track to wait
        /// </summary>
        public int tWaitTime
        {
            get => _tWaitTime;
            set
            {
                if (_tWaitTime == value) return;
                _tWaitTime = value;
                if (_timer != null)
                {
                    _timer.Interval = _tWaitTime;
                    _timer.Start();
                }
            }
        }

        /// <summary>
        /// Set the state of the track
        /// </summary>
        public TASK_STATE TaskState { 
            get => _taskSTATE; 
            set
            {
                if(_taskSTATE == value) return;
                _taskSTATE = value;
            }
         }

        /// <summary>
        /// Set when the track is occupied
        /// </summary>
        public bool trackOccupied
        {
            get => _trackOccupied;
            set
            {
                if (_trackOccupied == value) return;
                _trackOccupied = value;
            }
        }

        /// <summary>
        /// Control the red sigal of the track
        /// </summary>
        public bool setSignalRed
        {
            get => _setSignalRed;
            set
            {
                if (value == _setSignalRed) return;
                _setSignalRed = value;
                _setSignalGreen = !value;
            }
        }
        /// <summary>
        /// Control the green signal of the track
        /// </summary>
        public bool setSignalGreen
        {
            get => _setSignalGreen;
            set
            {
                if (value == _setSignalGreen) return;
                _setSignalGreen = value;
                _setSignalRed = !value;
            }
        }

        /// <summary>
        /// Get the occupied state of the track
        /// </summary>
        public bool getOccStn
        {
            get => _getOccStn;
            set
            {
                if (value == _getOccStn)
                {
                    return;
                }
                else
                {
                    _getOccStn = value;
                }
            }
        }

        /// <summary>
        /// Set the occupied state of the track
        /// </summary>
        public bool setOccStn
        {
            get => _setOccStn;
            set
            {
                if (value == _setOccStn)
                {
                    return;
                }
                else
                {
                    _setOccStn = value;
                }
            }
        }

        #endregion

        #region constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        public StationTrack(Stn name, Stn trackNr)
        {
            stnTrackName = name;
            stnTrackNumber = trackNr;

            _timer = new System.Timers.Timer();
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(_Timer_Elapsed);
            _timer.Interval = 5000;
            _timer.Start();
        }

        void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Stop the timer
            _timer.Stop();
            // Set the new desired state
            TaskState = TASK_STATE.WAIT_DONE;
        }

        #endregion
    }
}
