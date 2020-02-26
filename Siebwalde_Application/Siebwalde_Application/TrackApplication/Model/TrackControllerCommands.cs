using System.ComponentModel;

namespace Siebwalde_Application
{
    /// <summary>
    /// TrackDataItems uses BaseViewModel for proprty notified events only. This class is data only!
    /// </summary>
    public class TrackControllerCommands : BaseViewModel
    {
        #region TrackController Commands Methods

        /// <summary>
        /// Holds the start init track amplifier command state
        /// </summary>
        public bool StartInitializeTrackAmplifiers { get; set; }

        #endregion

        #region Ethernet Target received message Method

        private ReceivedMessage EthernetTargetRecv;
        public ReceivedMessage ReceivedMessages
        {
            get { return EthernetTargetRecv; }
            set { EthernetTargetRecv = value; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize all variables as required
        /// </summary>
        public TrackControllerCommands()
        {
            //public ReceivedMessage EthernetTarget = new ReceivedMessage();
            EthernetTargetRecv = new ReceivedMessage(0, 0, 0, 0);
        }

        #endregion

    }
}
