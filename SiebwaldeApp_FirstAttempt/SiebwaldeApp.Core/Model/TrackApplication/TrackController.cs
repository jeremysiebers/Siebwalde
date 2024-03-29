﻿using System;
using System.Threading.Tasks;

namespace SiebwaldeApp
{
    /// <summary>
    /// TrackController instantiates all drivers related to Trackcontrol functionality and trackapplication
    /// </summary>
    public class TrackController
    {
        #region variables

        // connect variable to connect to FYController class to Main for application logging
        //private Main mMain;

        // Ping instance
        private static PingTarget m_PingTarget = new PingTarget { };

        // Data
        public static TrackIOHandle mTrackIOHandle;

        //Controller
        public static TrackControlMain mTrackControlMain;

        // Public variables
        public TrackApplicationVariables mTrackApplicationVariables;

        // Sending and receiving port variable
        private int mTrackSendingPort;
        private int mTrackReceivingPort;

        // logging local variables
        private ILogger mTrackApplicationLogging;

        // Logger instance
        private static string LoggerInstance { get; set; }

        // Get a new log factory
        static ILogger GetLogger(string file, string loggerinstance)
        {
            return new FileLogger(file, loggerinstance);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for TrackController
        /// </summary>
        /// <param name="main"></param>
        /// <param name="TrackReceivingPort"></param>
        /// <param name="TrackSendingPort"></param>
        public TrackController(int TrackReceivingPort, int TrackSendingPort)
        {            
            mTrackReceivingPort = TrackReceivingPort;
            mTrackSendingPort = TrackSendingPort;

            // Set logger instance
            LoggerInstance = "Track";

            // create logging instance for Track application
            mTrackApplicationLogging = GetLogger(SiebwaldeApp.Properties.Settings.Default.LogDirectory + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + "TrackApplicationMain.txt", LoggerInstance);
            IoC.Logger.AddLogger(mTrackApplicationLogging);

            // create new instance of trackApplicationVariables (DATA)
            mTrackApplicationVariables = IoC.TrackVar;

            // create new instance of TrackIOHandle (Data exchange layer from EthernetTarget)
            mTrackIOHandle = new TrackIOHandle(mTrackApplicationVariables, mTrackReceivingPort, mTrackSendingPort, LoggerInstance);

            // create new instance of trackControlMain
            mTrackControlMain = new TrackControlMain(LoggerInstance,  mTrackIOHandle, mTrackApplicationVariables);
        }

        #endregion

        #region Start method

        /// <summary>
        /// Starting async Method of TrackController
        /// </summary>
        public static async Task StartTrackControllerAsync()
        {
            // Start a new task (so it runs on a different thread)
            await Task.Run(() =>
            {
                // Log it
                IoC.Logger.Log($"Start pinging the TrackController HW...", LoggerInstance);
                
                bool TrackRealMode = ConnectTrackConntroller();
                
                //force if required
                //TrackRealMode = false;

                if (TrackRealMode) // when connection was succesfull and target was found and is connected
                {
                    IoC.Logger.Log("MTCTRL: Track uController target in real mode.", LoggerInstance);
                }
                else
                {
                    IoC.Logger.Log("MTCTRL: Track uController target in simulator mode!", LoggerInstance);
                }

                // Log real/simulator to Track controller main application log
                IoC.Logger.Log("Track Control application started in " + ((TrackRealMode == true) ? ("Real mode") : ("Sim mode")) + ".", LoggerInstance);

                // start listening to data from ethernet target
                IoC.Logger.Log("Start Track I/O Handle.", LoggerInstance);
                mTrackIOHandle.Start(TrackRealMode);

                // start the Track controller main application
                IoC.Logger.Log("Start Track Application.", LoggerInstance);
                mTrackControlMain.Start(TrackRealMode);
            });
        }

        #endregion

        #region Ping/Connect Ethernet target

        /// <summary>
        /// Try to connect/ping the Ethernet TrackTarget 
        /// </summary>
        /// <returns></returns>
        private static bool ConnectTrackConntroller()
        {
            string PingReturn = "";
            try
            {
                
                IoC.Logger.Log("MTCTRL: Pinging Track controller target...", LoggerInstance);
                PingReturn = m_PingTarget.TargetFound(Enums.TRACKTARGET);
                if (PingReturn == "targetfound")
                {
                    IoC.Logger.Log("MTCTRL: Ping successfull.", LoggerInstance);
                    return true; // connection succesfull to FIDDLEYARD
                }
                else
                {
                    IoC.Logger.Log("MTCTRL: " + PingReturn, LoggerInstance);
                    return false; // ping was unsuccessfull
                }
            }
                
            catch (Exception)
            {
                IoC.Logger.Log("MTCTRL: TrackController failed to ping.", LoggerInstance);                
                return false; // ping was successfull but connecting failed
            }
        }

        internal void Stop()
        {
            
        }

        #endregion
    }
}
