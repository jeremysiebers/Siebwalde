using System.Collections.Generic;
using System.Threading.Tasks;

namespace SiebwaldeApp
{
    public class SiebwaldeApplicationModel
    {
        #region Public properties
            
        //public ObservableCollection<LogList> Log { get; set; }
        //public LogList Logging { get; private set; }

        public List<string> Logging { get; private set; }

        #endregion

        #region Private members

        public FiddleYardController FYcontroller;
        public FiddleYardSettingsForm FYSettingsForm;
        //private TrackController MTcontroller;
        public FiddleYardController YDcontroller;
        public MAC_IP_Conditioner MACIPConditioner = new MAC_IP_Conditioner { };

        public const string FYTarget = "FIDDLEYARD";
        public Sender FYSender = new Sender(FYTarget);
        public Receiver FYReceiver;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// <summary>
        public SiebwaldeApplicationModel()
        {            
            IoC.Logger.Log("Siebwalde Application started.", "");
            IoC.Logger.Log("Main: PC MAC adress is: " + MACIPConditioner.MACstring(), "");
            IoC.Logger.Log("Main: PC IP adress is: " + MACIPConditioner.IPstring(), "");

            FYReceiver = new Receiver(Properties.Settings.Default.FYReceivingport);
            //FYSender.ConnectUdp(Properties.Settings.Default.FYSendingport);
        }
        #endregion

        public void FYLinkActivityUpdate()
        {
            //if (FYLinkActivity.InvokeRequired)
            //{
            //    ToggleCommLinkCallback d = new ToggleCommLinkCallback(FYLinkActivityUpdate);
            //    FYLinkActivity.Invoke(d, new object[] { });  // invoking itself
            //}
            //else
            //{
            //    if (FYLinkActivity.Value >= LINKACTMAX)
            //    {
            //        FYLinkActivity.Value = 0;
            //    }
            //    FYLinkActivity.Value++;
            //}
        }

        public void StartFYController()
        {
            
            if (FYcontroller != null)
            {
                //FYcontroller.FYTOPShow(false, 1010, 1948, 0, 0, true);
                //FYcontroller.FYBOTShow(false, 1010, 1948, 0, 0, true);
                return;
            }
 
            // Tried to make Task and start FiddleYard with it, however the winform stuff needs to run on "main" task as well hence
            // it will give error when starting it on a different task. Therefore first continue with the whole track, later re-build 
            // the fiddle yard part....
            FYcontroller = new FiddleYardController(this, 
                MACIPConditioner.MAC(), 
                MACIPConditioner.IP(), 
                Properties.Settings.Default.FYReceivingport, 
                Properties.Settings.Default.FYSendingport);

            IoC.Logger.Log("FiddleYard Controller starting...", "");
            FYcontroller.Start();
;
            //FYcontroller.FYTOPShow(false, 1010, 1948, 0, 0, true);
            //FYcontroller.FYBOTShow(false, 1010, 1948, 0, 0, true);
            //FiddleYardFormTop.Visible = true;
            //FYLinkActivity.Visible = true;
            //LFYLinkActivity.Visible = true;
            IoC.Logger.Log("FiddleYard Controller started.", "");
        }

        public async void StartTrackController()
        {
            IoC.Logger.Log("Fake Track Controller started.", "");
            await StartTrackControlApp();
        }

        /// <summary>
        /// Does some work asynchronously for somebody
        /// </summary>
        /// <param name="forWho">Who we are doing the work for</param>
        /// <returns></returns>
        private static async Task StartTrackControlApp()
        {
            // Log it
            IoC.Logger.Log($"Doing work for","");

            // Start a new task (so it runs on a different thread)
            await Task.Run(async () =>
            {
                // Log it
                IoC.Logger.Log($"Doing work on inner thread for ","");

                // Wait 
                await Task.Delay(500);

                // Log it
                IoC.Logger.Log($"Done work on inner thread for","");
            });

            // Log it
            IoC.Logger.Log($"Done work for ","");
        }

    }
}
