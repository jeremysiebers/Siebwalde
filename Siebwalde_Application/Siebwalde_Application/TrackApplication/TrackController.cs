using System;
using System.Linq;
using System.Net;

namespace Siebwalde_Application
{
    /// <summary>
    /// TrackController instantiates all drivers related to Trackcontrol functionality
    /// </summary>
    public class TrackController
    {
        #region variables

        // connect variable to connect to FYController class to Main for application logging
        private Main mMain;

        // Ping instance
        private PingTarget m_PingTarget = new PingTarget { };

        // Data
        public TrackIOHandle trackIOHandle;

        //Controller
        public TrackControlMain trackControlMain;

        // Public variables
        public TrackApplicationVariables trackApplicationVariables;

        //Public Enums
        private PublicEnums mPublicEnums;

        // Sending and receiving port variable
        private int mTrackSendingPort;
        private int mTrackReceivingPort;

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
            
            // create public enums instance
            mPublicEnums = new PublicEnums();
            
            // create new instance of TrackIOHandle
            trackIOHandle = new TrackIOHandle(mMain ,mTrackReceivingPort, mTrackSendingPort);

            // create new instance of trackApplicationVariables
            trackApplicationVariables = new TrackApplicationVariables();

            // create new instance of trackControlMain
            trackControlMain = new TrackControlMain(mMain, mPublicEnums, trackIOHandle, trackApplicationVariables);
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

            trackIOHandle.Start(TrackRealMode);
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
                PingReturn = m_PingTarget.TargetFound(mPublicEnums.TrackTarget());
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
