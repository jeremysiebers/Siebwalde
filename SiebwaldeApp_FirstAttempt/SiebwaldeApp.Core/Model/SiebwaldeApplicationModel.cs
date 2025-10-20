using Ninject;
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
        private CancellationTokenSource _appCts;

        private readonly NewMAC_IP_Conditioner _macIp = new();
        public TrackApplication? _trackApplication;
        #endregion

        #region Constructor
        public SiebwaldeApplicationModel()
        {
            IoC.Logger.Log("Siebwalde Application started.", "");

            _appCts?.Cancel();
            _appCts = new CancellationTokenSource();

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



        /// <summary>
        /// Starts the track application, initializing and registering station tracks, and launching the simulation
        /// controller.
        /// </summary>
        /// <remarks>This method initializes the track application if it has not already been started. It
        /// retrieves the necessary input and output ports, registers station tracks with metadata, and starts the
        /// application's main processing loop. Additionally, it starts the simulation controller to manage
        /// simulation-related tasks.</remarks>
        /// <returns></returns>
        public async Task StartTrackApplication()
        {
            if (_trackApplication != null) return;

            IoC.Logger.Log("Track Application starting...", "");

            // Get ports from IoC (throw if not initialized)
            var trackIn = IoC.TrackAdapter.RequireIn();
            var trackOut = IoC.TrackAdapter.RequireOut();

            // Build root
            _trackApplication = new TrackApplication(trackIn, trackOut);

            // Register the 6 station tracks with metadata into the registry
            RegisterStationBlocks(trackOut);

            // 1) Start the application (spins up StationSide run loops)
            await _trackApplication.StartAsync(_appCts.Token);

            // 2) Pas nu de simulatie starten
            var simCtrl = IoC.Kernel.Get<ISimulationController>();
            if (simCtrl != null)
            {
                await simCtrl.StartAsync(_appCts.Token);
            }

            IoC.Logger.Log("Track Application started.", "");
        }

        /// <summary>
        /// Stops the currently running track application, if one is active.
        /// </summary>
        /// <remarks>This method ensures that the track application is properly stopped and its resources
        /// are released.  If no track application is active, the method exits without performing any action.  Any
        /// errors encountered during the stopping process are logged.</remarks>
        public void StopTrackApplication()
        {
            if (_trackApplication == null)
                return;

            IoC.Logger.Log("Track Application stopping...", "");

            try
            {
                _trackApplication.Stop();
                _trackApplication = null;
                IoC.Logger.Log("Track Application stopped.", "");
                // sim ook stoppen
                IoC.Kernel.Get<ISimulationController>().Stop();

                _appCts?.Cancel();
            }
            catch (Exception ex)
            {
                IoC.Logger.Log($"Error while stopping Track Application: {ex.Message}", "");
            }
        }

        /// <summary>
        /// Registers station track blocks and their associated metadata in the track application registry.
        /// </summary>
        /// <remarks>This method defines and registers track blocks for two station zones: "StationTop"
        /// and "StationBottom". Each block is associated with entry and exit sensors, a signal, an amplifier, and
        /// metadata specifying the zone and track role. The metadata includes a "Station" tag to identify the blocks as
        /// station-related.</remarks>
        /// <param name="trackOut">The output track interface used to initialize amplifiers for the registered track blocks.</param>
        private void RegisterStationBlocks(ITrackOut trackOut)
        {
            if (_trackApplication == null)
                throw new InvalidOperationException("TrackApplication must be initialized before registering blocks.");

            void Add(int id, string zone, TrackRole role)
            {
                var entry = new TrackSensor($"{id}-Entry");
                var exit = new TrackSensor($"{id}-Exit");
                var signal = new Signal($"{id}-Head");
                var amp = new Amplifier(id, trackOut);

                var block = new TrackBlock(id, entry, exit, signal, amp);
                var meta = new TrackMetadata { Zone = zone, Role = role };
                meta.Tags.Add("Station");

                _trackApplication.Registry.Register(block, meta);
            }

            // Top: 10,11,12 (12 = middle freight)
            Add(10, "StationTop", TrackRole.FreightAllowed);
            Add(11, "StationTop", TrackRole.FreightAllowed);
            Add(12, "StationTop", TrackRole.MiddleFreight);

            // Bottom: 1,2,3 (3 = middle freight)
            Add(1, "StationBottom", TrackRole.FreightAllowed);
            Add(2, "StationBottom", TrackRole.FreightAllowed);
            Add(3, "StationBottom", TrackRole.MiddleFreight);
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