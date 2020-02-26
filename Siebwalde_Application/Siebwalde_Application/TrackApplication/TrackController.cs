using System;
using static Siebwalde_Application.PublicEnums;

namespace Siebwalde_Application
{
    /// <summary>
    /// TrackController instantiates all drivers related to Trackcontrol functionality and trackapplication
    /// </summary>
    public class TrackController
    {
        #region variables

        // connect variable to connect to FYController class to Main for application logging
        private Main mMain;

        // Ping instance
        private PingTarget m_PingTarget = new PingTarget { };

        // Data
        public TrackIOHandle mTrackIOHandle;

        //Controller
        public TrackControlMain mTrackControlMain;

        // Public variables
        public TrackApplicationVariables mTrackApplicationVariables;

        // Sending and receiving port variable
        private int mTrackSendingPort;
        private int mTrackReceivingPort;

        // logging local variables
        private Log2LoggingFile mTrackApplicationLogging;
        private string path = "null";

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for TrackController
        /// </summary>
        /// <param name="main"></param>
        /// <param name="TrackReceivingPort"></param>
        /// <param name="TrackSendingPort"></param>
        public TrackController(Main main, int TrackReceivingPort, int TrackSendingPort)
        {            
            mMain = main;
            mTrackReceivingPort = TrackReceivingPort;
            mTrackSendingPort = TrackSendingPort;

            // create logging instance for Track application
            path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_TrackApplicationMain.txt";
            mTrackApplicationLogging = new Log2LoggingFile(path);


            // create new instance of trackApplicationVariables (DATA)
            mTrackApplicationVariables = new TrackApplicationVariables();

            // create new instance of TrackIOHandle (communication layer with EthernetTarget)
            mTrackIOHandle = new TrackIOHandle(mMain, mTrackApplicationVariables, mTrackReceivingPort, mTrackSendingPort);

            // create new instance of trackControlMain
            mTrackControlMain = new TrackControlMain(mMain, mTrackApplicationLogging,  mTrackIOHandle, mTrackApplicationVariables);
        }

        #endregion

        #region Start method

        /// <summary>
        /// Starting Method of TrackController
        /// </summary>
        public void Start()
        {
            bool TrackRealMode = ConnectTrackConntroller();
            //force if required
            //TrackRealMode = false;

            if (TrackRealMode) // when connection was succesfull and target was found and is connected
            {
                mMain.SiebwaldeAppLogging("MTCTRL: Track uController target in real mode.");                
            }
            else
            {
                mMain.SiebwaldeAppLogging("MTCTRL: Track uController target in simulator mode!");
            }

            // Log real/simulator to Track controller main application log
            mTrackApplicationLogging.Log(GetType().Name, "Track Control application started in " + ((TrackRealMode == true) ? ("Real mode") : ("Sim mode")) + ".");

            // start listening to data from ethernet target
            mTrackApplicationLogging.Log(GetType().Name, "Start Track I/O Handle.");
            mTrackIOHandle.Start(TrackRealMode);

            // start the Track controller main application
            mTrackApplicationLogging.Log(GetType().Name, "Start Track Application.");
            mTrackControlMain.Start();
        }

        #endregion

        #region Ping/Connect Ethernet target

        /// <summary>
        /// Try to connect/ping the Ethernet TrackTarget 
        /// </summary>
        /// <returns></returns>
        private bool ConnectTrackConntroller()
        {
            string PingReturn = "";
            try
            {
                mMain.SiebwaldeAppLogging("MTCTRL: Pinging Track controller target...");
                PingReturn = m_PingTarget.TargetFound(TRACKTARGET);
                if (PingReturn == "targetfound")
                {
                    mMain.SiebwaldeAppLogging("MTCTRL: Ping successfull.");
                    return true; // connection succesfull to FIDDLEYARD
                }
                else
                {
                    mMain.SiebwaldeAppLogging("MTCTRL: " + PingReturn);
                    return false; // ping was unsuccessfull
                }
            }
            catch (Exception)
            {
                mMain.SiebwaldeAppLogging("MTCTRL: TrackController failed to ping.");                
                return false; // ping was successfull but connecting failed
            }
        }

        internal void Stop()
        {
            
        }

        #endregion
    }
}
