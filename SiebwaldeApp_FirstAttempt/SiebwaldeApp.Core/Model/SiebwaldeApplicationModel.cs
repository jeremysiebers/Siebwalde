using System;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    public class SiebwaldeApplicationModel
    {
        #region Public events
        public event EventHandler? InstantiateFiddleYardWinForms;
        public event EventHandler? FiddleYardShowWinForms;
        public event EventHandler? FiddleYardShowSettingsWinForms;
        #endregion

        #region Private members
        public FiddleYardController? FYcontroller;
        public FiddleYardController? YDcontroller;

        private readonly NewMAC_IP_Conditioner _macIp = new();
        public TrackApplication? _trackApplication;
        #endregion

        #region Constructor
        public SiebwaldeApplicationModel()
        {
            IoC.Logger.Log("Siebwalde Application started.", "");

            var macString = _macIp.MACstring();
            IoC.Logger.Log($"Main: PC MAC address is: {(string.IsNullOrWhiteSpace(macString) ? "<unknown>" : macString)}", "");
            IoC.Logger.Log($"Main: PC IP address is:  {_macIp.IPstring()}", "");
        }
        #endregion

        protected virtual void OnLaunchWinFormsFormRequested(EventArgs e)
            => InstantiateFiddleYardWinForms?.Invoke(this, e);

        public void OnFiddleYardShowWinForm(EventArgs e)
            => FiddleYardShowWinForms?.Invoke(this, e);

        public void OnFiddleYardSettingsWinForm(EventArgs e)
            => FiddleYardShowSettingsWinForms?.Invoke(this, e);

        /// <summary>Fiddle Yard</summary>
        public async Task StartFYController()
        {
            if (FYcontroller != null)
                return;

            // --- SAFE MAC HANDLING ---
            byte[,] macPayload;
            if (!_macIp.TryGetMAC(out macPayload))
            {
                // Fallback: 12 rows of [identifier, 0, CR]
                macPayload = BuildDummyMacPayload();
                IoC.Logger.Log("FY: No active NIC MAC found. Using dummy MAC payload (zeros).", "");
            }

            // IP is deterministic; ProgramIP already validates inside NewMAC_IP_Conditioner
            var ipPayload = _macIp.IP();

            FYcontroller = new FiddleYardController(
                macPayload,
                ipPayload,
                Properties.CoreSettings.Default.FYReceivingport,
                Properties.CoreSettings.Default.FYSendingport);

            OnLaunchWinFormsFormRequested(EventArgs.Empty);

            IoC.Logger.Log("FiddleYard Controller starting...", "");
            await FYcontroller.StartFiddleYardControllerAsync();
            IoC.Logger.Log("FiddleYard Controller started.", "");
        }

        /// <summary>Track Controller</summary>
        public async Task StartTrackApplication()
        {
            if (_trackApplication != null) return;

            IoC.Logger.Log("Track Application starting...", "");
            // Get ports from your IoC (adapters already set up elsewhere)
            ITrackIn trackIn = IoC.TrackAdapter.TrackIn;
            ITrackOut trackOut = IoC.TrackAdapter.TrackOut;
            IYardIn yardIn = IoC.YardAdapter.YardIn;
            IYardOut yardOut = IoC.YardAdapter.YardOut;

            _trackApplication = new TrackApplication(trackIn, trackOut, yardIn, yardOut);

            RegisterStationBlocks(trackOut);

            // StationController was constructed inside TrackApplication and is event-wired already.
            await Task.CompletedTask;

            //TrackController = initTrackApplication();
            //await TrackApplication.InitTrackControllerAsync();
            //TrackController.StartTrackController();
            IoC.Logger.Log("Track Application started.", "");
        }

        private void RegisterStationBlocks(ITrackOut trackOut)
        {
            foreach (var id in TrackApplication.TopTracks.Concat(TrackApplication.BottomTracks))
            {
                var entry = new TrackSensor($"{id}-Entry");
                var exit = new TrackSensor($"{id}-Exit");
                var signal = new Signal($"{id}-Head"); // station head per track (software-controlled)
                var amp = new Amplifier(id, trackOut);

                _trackApplication.RegisterBlock(new TrackBlock(id, entry, exit, signal, amp));
            }
        }

        /// <summary>
        /// Dummy MAC payload (12×3): identifiers u..z,0..5; value=0; CR
        /// Matches the wire format expected by FiddleYardController.
        /// </summary>
        private static byte[,] BuildDummyMacPayload()
        {
            string[] identifiers = { "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5" };
            var send = new byte[12, 3];

            for (int i = 0; i < 12; i++)
            {
                send[i, 0] = Encoding.ASCII.GetBytes(identifiers[i])[0];
                send[i, 1] = 0x00;  // nibble value
                send[i, 2] = 0x0D;  // CR
            }
            return send;
        }
    }
}


//using System;
//using System.Threading.Tasks;

//namespace SiebwaldeApp.Core
//{
//    public class SiebwaldeApplicationModel
//    {
//        #region Public properties

//        public event EventHandler InstantiateFiddleYardWinForms;
//        public event EventHandler FiddleYardShowWinForms;
//        public event EventHandler FiddleYardShowSettingsWinForms;

//        #endregion

//        #region Private members

//        public FiddleYardController FYcontroller;
//        //public FiddleYardSettingsForm FYSettingsForm;

//        public FiddleYardController YDcontroller;
//        public NewMAC_IP_Conditioner MACIPConditioner = new NewMAC_IP_Conditioner { };

//        public TrackController TrackController;

//        #endregion

//        #region Constructor

//        /// <summary>
//        /// Default constructor
//        /// <summary>
//        public SiebwaldeApplicationModel()
//        {
//            IoC.Logger.Log("Siebwalde Application started.", "");
//            IoC.Logger.Log("Main: PC MAC adress is: " + MACIPConditioner.MACstring(), "");
//            IoC.Logger.Log("Main: PC IP adress is: " + MACIPConditioner.IPstring(), "");
//        }
//        #endregion

//        protected virtual void OnLaunchWinFormsFormRequested(EventArgs e)
//        {
//            InstantiateFiddleYardWinForms?.Invoke(this, e);
//        }

//        public void OnFiddleYardShowWinForm(EventArgs e) {
//            FiddleYardShowWinForms?.Invoke(this, e);
//        }

//        public void OnFiddleYardSettingsWinForm(EventArgs e)
//        {
//            FiddleYardShowSettingsWinForms?.Invoke(this, e);
//        }

//        /// <summary>
//        /// Fiddle Yard
//        /// </summary>
//        public async Task StartFYController()
//        {
//            if (FYcontroller != null)
//            {                
//                return;
//            }

//            // Tried to make Task and start FiddleYard with it, however the winform stuff needs to run on "main" task as well hence
//            // it will give error when starting it on a different task. Therefore first continue with the whole track, later re-build 
//            // the fiddle yard part....
//            FYcontroller = new FiddleYardController(
//                MACIPConditioner.MAC(),
//                MACIPConditioner.IP(),
//                Properties.CoreSettings.Default.FYReceivingport,
//                Properties.CoreSettings.Default.FYSendingport);

//            // Launch winform here before controller is started.
//            OnLaunchWinFormsFormRequested(EventArgs.Empty);


//            IoC.Logger.Log("FiddleYard Controller starting...", "");

//            await FYcontroller.StartFiddleYardControllerAsync();

//            ////FYcontroller.FYTOPShow(false, 1010, 1948, 0, 0, true);
//            //FYcontroller.FYBOTShow(false, 1010, 1948, 0, 0, true);
//            //FiddleYardFormTop.Visible = true;
//            //FYLinkActivity.Visible = true;
//            //LFYLinkActivity.Visible = true;
//            IoC.Logger.Log("FiddleYard Controller started.", "");

//        }



//        /// <summary>
//        /// Track Controller
//        /// </summary>
//        /// <returns></returns>
//        public async Task StartTrackController()
//        {
//            if (TrackController == null)
//            {
//                IoC.Logger.Log("Track Controller starting...", "");

//                TrackController = initTrackcontroller();

//                await TrackController.InitTrackControllerAsync();

//                TrackController.StartTrackController();

//                IoC.Logger.Log("Track Controller started.", "");
//            }

//        }

//        private static TrackController initTrackcontroller()
//        {
//            return new TrackController(60000, 60000);
//        }


//    }
//}