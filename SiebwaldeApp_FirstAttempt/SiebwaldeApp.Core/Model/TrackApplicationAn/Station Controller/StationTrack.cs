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

        #endregion

        #region Public properties

        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };

        /// <summary>
        /// Set the station track name
        /// </summary>
        public string stnTrackName { get; private set; }
        
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
        public StationTrack(string name)
        {
            stnTrackName = name;
        }

        #endregion
    }
}
