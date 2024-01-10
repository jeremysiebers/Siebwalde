

using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    public class SiebwaldeApplicationModel
    {
        #region Public properties
                
        #endregion

        #region Private members

        //public FiddleYardController FYcontroller;
        //public FiddleYardSettingsForm FYSettingsForm;
        //private TrackController MTcontroller;
        //public FiddleYardController YDcontroller;
        public NewMAC_IP_Conditioner MACIPConditioner = new NewMAC_IP_Conditioner { };

        //public const string FYTarget = "FIDDLEYARD";
        //public Sender FYSender = new Sender(FYTarget);
        //public Receiver FYReceiver;

        public TrackController TrackController;

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

            //FYReceiver = new Receiver(28672);
            //FYSender.ConnectUdp(Properties.Settings.Default.FYSendingport);
        }
        #endregion

        
        //public void StartFYController()
        //{

        //    if (FYcontroller != null)
        //    {
        //        //FYcontroller.FYTOPShow(false, 1010, 1948, 0, 0, true);
        //        //FYcontroller.FYBOTShow(false, 1010, 1948, 0, 0, true);
        //        return;
        //    }

        //    // Tried to make Task and start FiddleYard with it, however the winform stuff needs to run on "main" task as well hence
        //    // it will give error when starting it on a different task. Therefore first continue with the whole track, later re-build 
        //    // the fiddle yard part....
        //    FYcontroller = new FiddleYardController(this,
        //        MACIPConditioner.MAC(),
        //        MACIPConditioner.IP(),
        //        28672,
        //        28671);

        //    IoC.Logger.Log("FiddleYard Controller starting...", "");
        //    FYcontroller.Start();
        //    ;
        //    //FYcontroller.FYTOPShow(false, 1010, 1948, 0, 0, true);
        //    //FYcontroller.FYBOTShow(false, 1010, 1948, 0, 0, true);
        //    //FiddleYardFormTop.Visible = true;
        //    //FYLinkActivity.Visible = true;
        //    //LFYLinkActivity.Visible = true;
        //    IoC.Logger.Log("FiddleYard Controller started.", "");
        //}

        public async Task StartTrackController()
        {
            if (TrackController == null)
            {
                IoC.Logger.Log("Track Controller starting...", "");

                TrackController = initTrackcontroller();

                await TrackController.StartTrackControllerAsync();

                IoC.Logger.Log("Track Controller started.", "");
            }

        }

        private static TrackController initTrackcontroller()
        {
            return new TrackController(60000, 60000);
        }


    }
}