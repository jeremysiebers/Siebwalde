namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Commands and state used by the track controller.
    /// </summary>
    /// <remarks>
    /// This version no longer uses INotifyPropertyChanged.
    /// Values are plain properties that the controller and UI can read/write.
    /// </remarks>
    public class TrackControllerCommands
    {
        #region TrackController Command properties

        /// <summary>
        /// Indicates that initialization of all track amplifiers should start.
        /// </summary>
        public bool StartInitializeTrackAmplifiers { get; set; }

        /// <summary>
        /// Indicates that the HMI track control form should be started.
        /// </summary>
        public bool StartHmiTrackControlForm { get; set; }

        /// <summary>
        /// Indicates whether the Ethernet target is connected.
        /// </summary>
        public bool EthernetTargetConnected { get; set; }

        #endregion

        #region User message

        /// <summary>
        /// User message that can be shown on the user interface.
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;

        #endregion

        #region Ethernet target received message

        /// <summary>
        /// Last message received from the Ethernet target.
        /// </summary>
        public ReceivedMessage ReceivedMessage { get; set; }

        #endregion

        #region Ethernet target send message

        /// <summary>
        /// Next message to send to the Ethernet target.
        /// </summary>
        public SendMessage SendMessage { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize all variables as required.
        /// </summary>
        public TrackControllerCommands()
        {
            // Default dummy messages
            ReceivedMessage = new ReceivedMessage(0, 0, 0, 0);

            var dummyData = new byte[80];
            SendMessage = new SendMessage(0, dummyData);
        }

        #endregion
    }
}
