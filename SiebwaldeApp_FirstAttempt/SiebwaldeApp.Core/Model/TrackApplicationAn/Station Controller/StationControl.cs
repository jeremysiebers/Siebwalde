using System;
using System.Collections.Generic;
using System.ComponentModel;
using static System.Net.Mime.MediaTypeNames;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initializes all Station related properties and data
    /// </summary>
    public class StationControl
    {
        #region private properties
        // Logger instance
        private string _mLoggerInstance { get; set; }
        // Dictonary to hold all stations
        private Dictionary<string, Station> _mStations;
        // Dictonary to hold all StationApplications
        private Dictionary<string, StationApplication> _application;

        #endregion

        #region public properties

        /// <summary>
        /// Dictonary to hold the station instances
        /// </summary>
        public Dictionary<string, Station> mStations
        {
            get { return _mStations; }
            private set { _mStations = value; }
        }

        #endregion

        #region constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="LoggerInstance"></param>
        public StationControl(string LoggerInstance)
        {
            // couple and hold local variables
            _mLoggerInstance = LoggerInstance;
            _mStations = new Dictionary<string, Station>();

            // Instantiate the Stations with their properties
            Station stationTOP = new Station("TOP");
            stationTOP.addStationTrack("Track10");
            stationTOP.addStationTrack("Track11");
            stationTOP.addStationTrack("Track12");
            _mStations["TOP"] = stationTOP;

            Station stationBOT = new Station("BOT");
            stationBOT.addStationTrack("Track1");
            stationBOT.addStationTrack("Track2");
            stationBOT.addStationTrack("Track3");
            _mStations["BOT"] = stationBOT;

            _application = new Dictionary<string, StationApplication>();

            StationApplication stationApplicationTop = new StationApplication(stationTOP);
            StationApplication stationApplicationBot = new StationApplication(stationBOT);

            _application[nameof(stationTOP)] = stationApplicationTop;
            _application[nameof(stationBOT)] = stationApplicationBot;

            // Subscribe to external IO updates
            IoC.TrackVariables.PropertyChanged += ExternalTrackVar_PropertyChanged;

            foreach (var stationEntry in _mStations)
            {
                //stationEntry.Value.PropertyChanged += Station_PropertyChanged;

                foreach (var trackEntry in stationEntry.Value.StnTracks)
                {
                    trackEntry.Value.PropertyChanged += InternalTrackVar_PropertyChanged;
                }
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Catching event from track internal vars and sets the correct instance data to be send to Ethernet controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void InternalTrackVar_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (mStations == null) { return; }

            StationTrack stnTrack = sender as StationTrack;

            bool value = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);

            switch (stnTrack.stnTrackName)
            {
                case "Track1":
                    switch(e.PropertyName)
                    {
                        case "setOccStn":
                            IoC.TrackVariables.OccToStn1 = value;
                            break;
                        default: break;
                    }
                    break;

                    default: break;
            }
        }
                
        /// <summary>
        /// Catching event from track io and sets the correct instance variables of station and tracks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExternalTrackVar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (mStations == null) { return; }

            bool value = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);

            switch (e.PropertyName)
            {
                case "OccFromStn1":
                    _mStations["BOT"].StnTracks["Track1"].getOccStn = value;
                    break;
                case "OccFromStn2":
                    _mStations["BOT"].StnTracks["Track2"].getOccStn = value;
                    break;
                case "OccFromStn3":
                    _mStations["BOT"].StnTracks["Track3"].getOccStn = value;
                    break;
                case "OccFromStn10":
                    _mStations["TOP"].StnTracks["Track10"].getOccStn = value;
                    break;
                case "OccFromStn11":
                    _mStations["TOP"].StnTracks["Track11"].getOccStn = value;
                    break;
                case "OccFromStn12":
                    _mStations["TOP"].StnTracks["Track12"].getOccStn = value;
                    break;

                default:
                    break;
            }

        }

        internal void Start()
        {
            // simulate data
            IoC.TrackVariables.OccFromStn1 = true;

            _mStations["BOT"].StnTracks["Track1"].setOccStn = true;
        }

        #endregion
    }
}
