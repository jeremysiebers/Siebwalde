namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Semantic version surfaced by the PIC32 master's <c>GET_CONTROLLER_VERSION</c> (113) reply.
    /// Purely informational: the value is surfaced but NOT used as a protocol gate.
    /// </summary>
    public readonly struct TrackControllerVersion
    {
        public byte Major { get; }
        public byte Minor { get; }
        public byte Patch { get; }
        public byte ProtocolVersion { get; }
        public string? BuildDate { get; }

        public TrackControllerVersion(byte major, byte minor, byte patch, byte protocolVersion, string? buildDate)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            ProtocolVersion = protocolVersion;
            BuildDate = buildDate;
        }

        public override string ToString() => $"{Major}.{Minor}.{Patch}";
    }

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

        /// <summary>
        /// Last PIC32 semantic version surfaced from a <c>GET_CONTROLLER_VERSION</c> (113) reply.
        /// Null until such a reply has been parsed. Informational only — not a protocol gate.
        /// </summary>
        public TrackControllerVersion? ControllerVersion { get; set; }

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
