using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initializes all Station related properties and data, sets up the applicaton per station
    /// </summary>
    public class StationControl
    {
        #region private properties
        // Logger instance
        private string _mLoggerInstance { get; set; }
        //private Station _stationTOP;
        //private Station _stationBOT;
        //private StationApplication _stationApplicationTOP;
        //private StationApplication _stationApplicationBOT;
        // Dictonary to hold all stations
        //private Dictionary<Stn, Station> _mStations;
        // Dictonary to hold all StationApplications
        private Dictionary<Stn, StationApplication> _application;

        #endregion

        #region public properties

        /// <summary>
        /// Dictonary to hold the station instances
        /// </summary>
        //public Dictionary<Stn, Station> mStations
        //{
        //    get { return _mStations; }
        //    private set { _mStations = value; }
        //}

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
            Dictionary<Stn, Station>  mStations = new Dictionary<Stn, Station>();

            // Instantiate the Stations with their properties
            Station stationTOP = new Station(Stn.TOP);
            stationTOP.addStationTrack(Stn.TRACK1, Stn.TRACK10);
            stationTOP.addStationTrack(Stn.TRACK2, Stn.TRACK11);
            stationTOP.addStationTrack(Stn.TRACK3, Stn.TRACK12);
            //stationTOP.addStationVar(nameof(Stn.HallBlock13), Stn.HallBlock13, false);
            mStations[Stn.TOP] = stationTOP;

            Station stationBOT = new Station(Stn.BOT);
            stationBOT.addStationTrack(Stn.TRACK1, Stn.TRACK1);
            stationBOT.addStationTrack(Stn.TRACK2, Stn.TRACK2);
            stationBOT.addStationTrack(Stn.TRACK3, Stn.TRACK3);
            mStations[Stn.BOT] = stationBOT;

            // Instantiate the StationApplications
            _application = new Dictionary<Stn, StationApplication>();

            StationApplication stationApplicationTOP = new StationApplication(stationTOP);
            _application[Stn.TOP] = stationApplicationTOP;

            StationApplication stationApplicationBOT = new StationApplication(stationBOT);
            _application[Stn.BOT] = stationApplicationBOT;
            
            // Subscribe to external IO updates
            IoC.TrackVariables.PropertyChanged += ExternalTrackVar_PropertyChanged;

            // Subscribe to internal IO updates
            foreach (var applicationEntry in _application)
            {
                foreach (var stationApplicatonEntry in applicationEntry.Value.Stations.StnTracks)
                {
                    stationApplicatonEntry.Value.PropertyChanged += InternalTrackVar_PropertyChanged;
                }

                applicationEntry.Value.Stations.PropertyChanged += InternalTrackVar_PropertyChanged;
            }                
        }

        #endregion

        #region methods

        // <summary>
        // Catching event from track internal vars and sets the correct instance data to be send to Ethernet controller
        // </summary>
        // <param name="sender"></param>
        // <param name="e"></param>
        private void InternalTrackVar_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_application == null) { return; }

            // Get the value
            bool value = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);

            // Check if sender is of type Station
            if (sender is Station station)
            {
                // Handle the case where sender is a Station
                // You can access station properties and methods here
                switch (e.PropertyName)
                {
                    case "getFreightLeaveStation":
                        if(station.Name == Stn.TOP)
                        {
                            IoC.TrackVariables.HallBlock13 = value;
                        }
                        else if (station.Name == Stn.BOT)
                        {
                            IoC.TrackVariables.HallBlock4A = value;
                        }
                        break;
                    default: break;
                }
            }
            else if (sender is StationTrack stnTrack)
            {
                // Handle the case where sender is a StationTrack
                        
                switch (stnTrack.stnTrackNumber)
                {
                    case Stn.TRACK1:
                        switch (e.PropertyName)
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
        }
                
        /// <summary>
        /// Catching event from track io and sets the correct instance variables of station and tracks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExternalTrackVar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_application == null) { return; }

            bool value = (bool)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);

            switch (e.PropertyName)
            {
                case "OccFromStn1":
                    _application[Stn.BOT].Stations.StnTracks[Stn.TRACK1].getOccStn = value;
                    break;
                case "OccFromStn2":
                    _application[Stn.BOT].Stations.StnTracks[Stn.TRACK2].getOccStn = value;
                    break;
                case "OccFromStn3":
                    _application[Stn.BOT].Stations.StnTracks[Stn.TRACK3].getOccStn = value;
                    break;
                case "OccFromStn10":
                    _application[Stn.TOP].Stations.StnTracks[Stn.TRACK1].getOccStn = value;
                    break;
                case "OccFromStn11":
                    _application[Stn.TOP].Stations.StnTracks[Stn.TRACK2].getOccStn = value;
                    break;
                case "OccFromStn12":
                    _application[Stn.TOP].Stations.StnTracks[Stn.TRACK3].getOccStn = value;
                    break;

                default:
                    break;
            }

        }

        #endregion

        internal void Start()
        {
            // simulate data external
            //IoC.TrackVariables.OccFromStn1 = true;

            _application[Stn.TOP].Start();
            _application[Stn.BOT].Start();
        }

        
    }
}
