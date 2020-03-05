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
        /// <summary>
        /// Holds the Ethernet Target connected state
        /// </summary>
        public bool EthernetTargetConnected { get; set; }

        #endregion

        #region Usermessage
        /// <summary>
        /// User message that can be shown on some user interface
        /// </summary>
        public string UserMessage { get; set; }

        #endregion

        #region Ethernet Target received message Method

        private ReceivedMessage EthernetTargetRecv;
        public ReceivedMessage ReceivedMessage
        {
            get { return EthernetTargetRecv; }
            set { EthernetTargetRecv = value; }
        }

        #endregion

        #region Ethernet Target Message to Send Method

        private SendMessage EthernetTargetSend;
        public SendMessage SendMessage
        {
            get { return EthernetTargetSend; }
            set { EthernetTargetSend = value; }
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

            byte[] DummyData = new byte[80];
            EthernetTargetSend = new SendMessage(0, DummyData);
        }

        #endregion

    }
}
