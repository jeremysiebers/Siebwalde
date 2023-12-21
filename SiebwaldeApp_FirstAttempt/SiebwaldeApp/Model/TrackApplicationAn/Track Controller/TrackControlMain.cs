using System;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.ComponentModel;
using System.Timers;

namespace SiebwaldeApp
{
    /// <summary>
    /// This is the main Trackcontroller Application class
    /// </summary>
    public class TrackControlMain
    {
        #region Variables

        private TrackIOHandle mTrackIOHandle;
        private TrackApplicationVariables mTrackApplicationVariables;
        private System.Timers.Timer AppUpdateTimer = new System.Timers.Timer();
        private readonly object ExecuteLock = new object();

        private ReceivedMessage dummymessage;

        /// <summary>
        /// This is the HmiTrackForm that holds a container for the WPF via elementhost
        /// </summary>
        //private static HmiTrackControlForm hmiTrackForm;
        //private static HmiTrackControl mHmiTrackControl;

        /// <summary>
        /// This enum holds all the possible states of the TrackControlMain statemachine
        /// </summary>
        private enum State { Idle, Reset, Cmd, InitializeTrackAmplifiers };
        private State State_Machine;

        // Logger instance
        private string mLoggerInstance { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="LoggerInstance"></param>
        /// <param name="trackIOHandle"></param>
        /// <param name="trackApplicationVariables"></param>
        public TrackControlMain(string LoggerInstance, TrackIOHandle bla)
        {
            // couple and hold local variables                    
            mLoggerInstance = LoggerInstance;
            mTrackIOHandle = bla;

            dummymessage = new ReceivedMessage(0, 0, 0, 0);
        }

        #endregion

        /// <summary>
        /// Timer event to kick TrackApplication
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (mTrackIOHandle.mconnected)
            {
                mTrackIOHandle.ActuatorCmd(new SendMessage(8, new byte[] { 0, 1, 3, 4, 5,6 ,7 ,8 }));
            }            
        }


        

        /// <summary>
        /// Start the Track Main Application
        /// </summary>
        internal void Start(bool TrackRealMode)
        {
            AppUpdateTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            AppUpdateTimer.Interval = 500;
            AppUpdateTimer.AutoReset = true;
            // Enable the timer
            AppUpdateTimer.Enabled = true;
            IoC.Logger.Log("Track Application started.", mLoggerInstance);

        }
    }
}