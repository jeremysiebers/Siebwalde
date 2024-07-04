
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
        private string _name;

        /// <summary>
        /// Struct to hold the data of the station
        /// </summary>
        private Dictionary<string,StationTrack> _stnTracks;

        #endregion

        #region public properties

        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };

        public string Name { get { return _name; } }

        [DoNotNotify]
        public Dictionary<string, StationTrack> StnTracks
        {
            get { return _stnTracks; }
            set { _stnTracks = value; }
        }

        #endregion

        #region constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public Station(string name)
        {   
            _name = name;
            _stnTracks = new Dictionary<string, StationTrack> { };
        }

        public void addStationTrack(string stnTrackName)
        {
            StationTrack track = new StationTrack(stnTrackName);
            _stnTracks[stnTrackName] = track;
        }

        #endregion
    }
}
