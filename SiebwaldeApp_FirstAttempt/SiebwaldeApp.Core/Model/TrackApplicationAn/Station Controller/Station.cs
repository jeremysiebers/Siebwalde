
using System;
using System.Collections.Generic;
using System.ComponentModel;
using PropertyChanged;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Station class that holds all data and properties of the station
    /// </summary>
    public class Station : INotifyPropertyChanged
    {
        #region private properties

        /// <summary>
        /// name of the station
        /// </summary>
        private Stn _name;
        /// <summary>
        /// Get the occupied state of the input block to the station
        /// </summary>
        private bool _getOccBlkIn;
        /// <summary>
        /// Set the occupied state of the input block to the station
        /// </summary>
        private bool _setOccBlkIn;
        /// <summary>
        /// Get the occupied state of the output block away from the station
        /// </summary>
        private bool _getOccBlkOut;
        /// <summary>
        /// Get the state if a freighter has left the station
        /// </summary>
        private bool _getFreightLeaveStation;
        /// <summary>
        /// Get the state if a freighter is entering the station
        /// </summary>
        private bool _getFreightEnterStation;
        
        /// <summary>
        /// Struct to hold the data of the station track
        /// </summary>
        private Dictionary<Stn, StationTrack> _stnTracks;
        /// <summary>
        /// Struct to hold the vars of the station like block occupied etc
        /// </summary>
        private Dictionary<string, (Stn enums, bool value)> _stationVars;

        #endregion

        #region public properties

        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };

        /// <summary>
        /// Get the name of the station
        /// </summary>
        public Stn Name { get { return _name; } }

        /// <summary>
        /// Get/Set the occupied state of the input block to the station
        /// </summary>
        public bool getOccBlkIn
        {
            get => _getOccBlkIn;
            set
            {
                if (value == _getOccBlkIn)
                {
                    return;
                }
                else
                {
                    _getOccBlkIn = value;
                }
            }
        }

        /// <summary>
        /// Get/Set the occupied state of the input block to the station
        /// </summary>
        public bool setOccBlkIn
        {
            get => _setOccBlkIn;
            set
            {
                if (value == _setOccBlkIn)
                {
                    return;
                }
                else
                {
                    _setOccBlkIn = value;
                }
            }
        }

        /// <summary>
        /// Get/Set the occupied state of the input block to the station
        /// </summary>
        public bool getOccBlkOut
        {
            get => _getOccBlkOut;
            set
            {
                if (value == _getOccBlkOut)
                {
                    return;
                }
                else
                {
                    _getOccBlkOut = value;
                }
            }
        }

        /// <summary>
        /// Get/Set the occupied state of the input block to the station
        /// </summary>
        public bool getFreightLeaveStation
        {
            get => _getFreightLeaveStation;
            set
            {
                if (value == _getFreightLeaveStation)
                {
                    return;
                }
                else
                {
                    _getFreightLeaveStation = value;
                }
            }
        }

        /// <summary>
        /// Get/Set the occupied state of the input block to the station
        /// </summary>
        public bool getFreightEnterStation
        {
            get => _getFreightEnterStation;
            set
            {
                if (value == _getFreightEnterStation)
                {
                    return;
                }
                else
                {
                    _getFreightEnterStation = value;
                }
            }
        }

        [DoNotNotify]
        public Dictionary<Stn, StationTrack> StnTracks
        {
            get { return _stnTracks; }
            //set { _stnTracks = value; }
        }

        [DoNotNotify]
        public Dictionary<string, (Stn enums, bool value)> StationVars
        {
            get { return _stationVars; }
            //set { _stationVars = value; }
        }

        #endregion

        #region constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public Station(Stn name)
        {   
            _name = name;
            _stnTracks = new Dictionary<Stn, StationTrack> { };
            _stationVars = new Dictionary<string, (Stn enums, bool value)> { };
        }

        /// <summary>
        /// Add a station track to the station
        /// </summary>
        /// <param name="stnTrackName"></param>
        /// <param name="trackNr"></param>
        public void addStationTrack(Stn stnTrackName, Stn trackNr)
        {
            StationTrack track = new StationTrack(stnTrackName, trackNr);
            _stnTracks[stnTrackName] = track;
        }

        /// <summary>
        /// Add a station variable to the station
        /// </summary>
        /// <param name="varName"></param>
        /// <param name="enums"></param>
        /// <param name="value"></param>
        public void addStationVar(string varName, Stn enums, bool value)
        {
            _stationVars[varName] = (enums, value);
        }

        #endregion
    }
}
