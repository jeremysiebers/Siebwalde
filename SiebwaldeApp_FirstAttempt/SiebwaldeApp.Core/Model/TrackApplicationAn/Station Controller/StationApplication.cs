using System.ComponentModel;
using static System.Collections.Specialized.BitVector32;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// This is the application that executes all station traffic in one direction, either the TOP or BOT part.
    /// </summary>
    public class StationApplication
    {
        #region private properties

        private Station _station;

        // logging local variables
        private ILogger mStationApplicationLogging;

        // Logger instance
        private string LoggerInstance { get; set; }

        // Basic string for file name
        private NewLogFileBasics m_LogFileBasics;

        // Get a new log factory
        static ILogger GetLogger(string file, string loggerinstance)
        {
            return new FileLogger(file, loggerinstance);
        }

        // State of the Station
        private enum State
        {
            Idle,
            Init,
            Run
        }

        // private variable holding the state
        private State m_State;

        #endregion

        #region public properties

        /// <summary>
        /// Get the Station instance
        /// </summary>
        public Station Stations
        {
            get => _station;
        }
        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="station"></param>
        public StationApplication(Station station)
        {
            // Store the reference to a station
            _station = station;

            // Set logger instance
            LoggerInstance = _station.Name.ToString();
            // create new basics for log file properties
            m_LogFileBasics = new NewLogFileBasics();
            // Set the Station application logger
            mStationApplicationLogging = GetLogger(m_LogFileBasics.getLogFile("StationApplication" + LoggerInstance + ".txt"), LoggerInstance);
            // Add the logger to the logging factory
            IoC.Logger.AddLogger(mStationApplicationLogging);

            // Log it
            IoC.Logger.Log(LoggerInstance + $" StationApplication created", LoggerInstance);

            // Set state
            m_State = State.Idle;

            foreach (var trackEntry in _station.StnTracks)
            {
                trackEntry.Value.PropertyChanged += TrackVar_PropertyChanged;
            }                       

        }

        #endregion

        /// <summary>
        /// Catching event from track internal vars and update the program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void TrackVar_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_station == null) { return; }

            //StationTrack stnTrack = sender as StationTrack;

            //bool value = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);

            if (m_State == State.Idle)
            {
                return;
            }
            else if (m_State == State.Init)
            {
                // Set both hall sensors debounced-latched values to false
                _station.getFreightEnterStation = false;
                _station.getFreightLeaveStation = false;

                // Set the occupied state of the track to true
                _station.StnTracks[Stn.TRACK1].setOccStn = true;

            }
            else if (m_State == State.Run)
            {

            }

        }
        public void Start()
        {
            // Log the start of the StationApplication
            IoC.Logger.Log(LoggerInstance + $" StationApplication started", LoggerInstance);
            m_State = State.Init;
            _station.StnTracks[Stn.TRACK1].setOccStn = true;
            _station.getFreightLeaveStation = true;
        }
    }
}
