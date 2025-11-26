using System.Text;

namespace SiebwaldeApp.Core
{
    public class SiebwaldeApplicationModel
    {
        #region Public events
        public event EventHandler? InstantiateFiddleYardWinForms;
        public event EventHandler? FiddleYardShowWinForms;
        public event EventHandler? FiddleYardShowSettingsWinForms;
        public StationSettingsPageViewModel SettingsViewModel { get; private set; }

        public TrackControlMain? _trackControlMain;
        #endregion

        #region Private members
        public FiddleYardController? FYcontroller;
        public FiddleYardController? YDcontroller;
        private CancellationTokenSource _appCts;

        private readonly NewMAC_IP_Conditioner _macIp = new();
        // --- New track amplifier infrastructure ---
        private TrackApplicationVariables? _trackVariables;
        private ITrackCommClient? _trackCommClient;
        private ITrackAmplifierInitializationService? _trackInitService;
        // NEW: bootloader helpers
        private TrackAmplifierBootloaderHelpers? _bootloaderHelpers;
        private SendNextFwDataPacket? _sendNextFwDataPacket;
        private const string FwPath = "C:\\Localdata\\Siebwalde\\TrackAmplifier4.X\\dist\\Offset\\production\\TrackAmplifier4.X.production.hex";

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
            if (_trackControlMain != null) return;

            IoC.Logger.Log("Track Application starting...", "");

            _appCts = new CancellationTokenSource();

            // --- NEW: build low-level amplifier communication + initialization ---

            // 0) Ensure we have the legacy variable container
            _trackVariables ??= new TrackApplicationVariables();

            // TODO: get these ports from configuration or Enums, for now hard-coded
            const int trackReceivingPort = 5000; // replace with your real port
            const int trackSendingPort = 5001;   // replace with your real port
            const string trackLoggerInstance = "Track";

            // 1) Create transport + communication client
            var transport = new UdpTrackTransport(trackReceivingPort, trackSendingPort, trackLoggerInstance);
            _trackCommClient = new TrackCommClientAsync(transport, _trackVariables);

            // 2) Bootloader helper objects(re -using legacy classes)
            // Constructor signatures hier moeten hetzelfde blijven als in je oude sequencer.
            _bootloaderHelpers ??= new TrackAmplifierBootloaderHelpers(
                FwPath,
                trackLoggerInstance);

            _sendNextFwDataPacket ??= new SendNextFwDataPacket(
                _trackCommClient,                
                _bootloaderHelpers);

            // 3) Compose initialization steps
            var steps = new List<IInitializationStep>
            {
                new ConnectToEthernetTargetStep(_trackCommClient, _trackVariables, trackLoggerInstance),
                new ResetAllSlavesStep(_trackCommClient, _trackVariables, trackLoggerInstance),
                new DataUploadStep(_trackCommClient, _trackVariables, trackLoggerInstance),
                new DetectSlavesStep(_trackCommClient, _trackVariables, trackLoggerInstance),
                new RecoverSlavesStep(_trackCommClient, _trackVariables, _sendNextFwDataPacket, _bootloaderHelpers, trackLoggerInstance),
                new FlashFwTrackamplifiersStep(_trackCommClient, _trackVariables, _sendNextFwDataPacket, _bootloaderHelpers, trackLoggerInstance),
                new InitTrackamplifiersStep(_trackCommClient, trackLoggerInstance),
                new EnableTrackamplifiersStep(_trackCommClient, trackLoggerInstance),
            };

            _trackInitService = new TrackAmplifierInitializationServiceAsync(
                _trackCommClient,
                _trackVariables,
                steps);

            // Optional: hook progress to logging or UI
            _trackInitService.ProgressChanged += (s, e) =>
            {
                IoC.Logger.Log($"Track init: {e.StepName} - {e.Message}", trackLoggerInstance);
            };

            _trackInitService.StatusChanged += (s, status) =>
            {
                IoC.Logger.Log($"Track init status: {status}", trackLoggerInstance);
            };

            // 4) Start low-level communication (real HW for now, later also emu)
            await _trackCommClient.StartAsync(realHardwareMode: true, _appCts.Token);

            // 5) Start async initialization sequence in the background
            _ = _trackInitService.InitializeAsync(_appCts.Token);

            // --- Existing high-level application startup ---

            // 6) Start the application (spins up StationSide run loops)
            _trackControlMain.Start(true);

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
            if (_trackControlMain == null)
                return;

            IoC.Logger.Log("Stopping Track Application...", "");

            try
            {
                _appCts.Cancel();
            }
            catch { /* ignore */ }

            // TODO: ideally make this method async and await the dispose calls
            if (_trackCommClient is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().Wait();
            }

            _trackCommClient = null;
            _trackInitService = null;
            _trackVariables = null;

            _trackControlMain = null;

            IoC.Logger.Log("Track Application stopped.", "");
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