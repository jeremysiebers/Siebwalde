using System;
using System.Collections.Generic;
using System.ComponentModel;

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

        #endregion

        #region public properties

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

            switch (stnTrack.stnTrackName)
            {
                case "Track1":
                    switch(e.PropertyName)
                    {
                        case "setOccStn":
                            IoC.TrackVariables.OccToStn1 = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
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

            switch (e.PropertyName)
            {
                case "OccFromStn1":
                    _mStations["BOT"].StnTracks["Track1"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
                    break;
                case "OccFromStn2":
                    _mStations["BOT"].StnTracks["Track2"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
                    break;
                case "OccFromStn3":
                    _mStations["BOT"].StnTracks["Track3"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
                    break;
                case "OccFromStn10":
                    _mStations["TOP"].StnTracks["Track10"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
                    break;
                case "OccFromStn11":
                    _mStations["TOP"].StnTracks["Track11"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
                    break;
                case "OccFromStn12":
                    _mStations["TOP"].StnTracks["Track12"].getOccStn = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
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
