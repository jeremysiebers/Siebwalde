using System;
using System.Threading.Tasks;

namespace SiebwaldeApp
{
    public class TrackController_later
    {
        #region Public properties

        #endregion

        #region Private members

        #endregion

        #region variables

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
        private ILogger mTrackApplicationLogging;

        // Logger instance
        private string LoggerInstance { get; set; }

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
        public TrackController_later(int TrackReceivingPort, int TrackSendingPort)
        {
            mTrackReceivingPort = TrackReceivingPort;
            mTrackSendingPort = TrackSendingPort;

            // Set logger instance
            LoggerInstance = "Track";

            // create logging instance for Track application
            mTrackApplicationLogging = GetLogger(Properties.Settings.Default.LogDirectory + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + "TrackApplicationMain.txt", LoggerInstance);
            IoC.Logger.AddLogger(mTrackApplicationLogging);


            //GetLogger("TrackApplicationMain.txt");


            // create new instance of trackApplicationVariables (DATA)
            //mTrackApplicationVariables = IoC.TrackVar;

            // create new instance of TrackIOHandle (communication layer with EthernetTarget)
            //mTrackIOHandle = new TrackIOHandle(mTrackApplicationVariables, mTrackReceivingPort, mTrackSendingPort);

            // create new instance of trackControlMain
            //mTrackControlMain = new TrackControlMain(mTrackApplicationLogging, mTrackIOHandle, mTrackApplicationVariables);
        }

        #endregion

        #region Public methods

        #region Start method

        /// <summary>
        /// Starting async Method of TrackController
        /// </summary>
        public static async Task StartTrackControllerAsync()
        {
            // Start a new task (so it runs on a different thread)
            await Task.Run(() => {
                // Log it
                IoC.Logger.Log($"Start pinging the TrackController HW...", "");

                bool TrackRealMode = ConnectTrackConntroller();
                //force if required
                //TrackRealMode = false;
                
                if (TrackRealMode) // when connection was succesfull and target was found and is connected
                {
                    IoC.Logger.Log("MTCTRL: Track uController target in real mode.");
                }
                else
                {
                    IoC.Logger.Log("MTCTRL: Track uController target in simulator mode!");
                }

                // Log real/simulator to Track controller main application log
                IoC.Logger.Log("Track Control application started in " + ((TrackRealMode == true) ? ("Real mode") : ("Sim mode")) + ".");

                // start listening to data from ethernet target
                IoC.Logger.Log("Start Track I/O Handle.");
                mTrackIOHandle.Start(TrackRealMode);

                // start the Track controller main application
                IoC.Logger.Log("Start Track Application.");
                mTrackControlMain.Start();

                // Log it
                IoC.Logger.Log($"Done work on inner thread for", "");
                return Task.CompletedTask;
            });
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
                IoC.Logger.Log("MTCTRL: Pinging Track controller target...");
                PingReturn = m_PingTarget.TargetFound(TRACKTARGET);
                if (PingReturn == "targetfound")
                {
                    IoC.Logger.Log("MTCTRL: Ping successfull.");
                    return true; // connection succesfull to FIDDLEYARD
                }
                else
                {
                    IoC.Logger.Log("MTCTRL: " + PingReturn);
                    return false; // ping was unsuccessfull
                }
            }
            catch (Exception)
            {
                IoC.Logger.Log("MTCTRL: TrackController failed to ping.");
                return false; // ping was successfull but connecting failed
            }
        }

        internal void Stop()
        {

        }

        #endregion

        #endregion

    }
}
