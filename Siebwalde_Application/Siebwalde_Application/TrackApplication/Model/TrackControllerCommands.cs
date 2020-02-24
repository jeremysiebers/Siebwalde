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

        #region Ethernet Target received answer Methods

        /// <summary>
        /// Received answers from Ethernet Target
        /// </summary>
        public ushort TaskId { get; set; }
        public ushort Taskcommand { get; set; }
        public ushort Taskstate { get; set; }
        public ushort Taskmessage { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize all variables as required
        /// </summary>
        public TrackControllerCommands()
        {

        }

        #endregion

    }
}
