using SiebwaldeApp.Core.TrackApplication.Comm;
using System.Text;

namespace SiebwaldeApp.Core
{
    public class SiebwaldeApplicationModel
    {
        #region Public events
        public event EventHandler? InstantiateFiddleYardWinForms;
        public event EventHandler? FiddleYardShowWinForms;
        public event EventHandler? FiddleYardShowSettingsWinForms;
        //public StationSettingsPageViewModel SettingsViewModel { get; private set; }
        public FiddleYardController? FYcontroller;
        public FiddleYardController? YDcontroller;
        public TrackControlMain? _trackControlMain;
        #endregion

        #region Private members
        
        private CancellationTokenSource _appCts;
        private readonly NewMAC_IP_Conditioner _macIp = new();        
        private TrackApplicationVariables? _trackVariables;        
        private const string FwPath = "C:\\Localdata\\Siebwalde\\TrackAmplifier4.X\\dist\\Offset\\production\\TrackAmplifier4.X.production.hex";

        private ILogger TrackApplicationLogging;
        private TrackCommClientAsync _trackCommClient;
        private TrackAmplifierBootloaderHelpers _bootloaderHelpers;
        private SendNextFwDataPacket _sendNextFwDataPacket;
        private TrackAmplifierInitializationServiceAsync _trackInitService;

        private string LoggerInstance { get; set; }
        static ILogger GetLogger(string file, string loggerinstance)
        {
            return new FileLogger(file, loggerinstance);
        }

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
            // If the main controller already exists we assume the track application
            // is running and do nothing.
            if (_trackControlMain != null)
                return;

            IoC.Logger.Log("Track Application starting...", "");

            // ---------------------------------------------------------------------
            // 0) Setup logging
            // ---------------------------------------------------------------------
            LoggerInstance = "TrackAppLog";

            TrackApplicationLogging = GetLogger(
                Core.Properties.CoreSettings.Default.LogDirectory
                + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_"
                + "TrackAppLog.txt",
                LoggerInstance);

            IoC.Logger.AddLogger(TrackApplicationLogging);

            // ---------------------------------------------------------------------
            // 1) Setup cancellation token for the whole track application
            // ---------------------------------------------------------------------
            _appCts = new CancellationTokenSource();

            // ---------------------------------------------------------------------
            // 2) Ensure we have the shared legacy variable container
            // ---------------------------------------------------------------------
            _trackVariables ??= new TrackApplicationVariables();

            // ---------------------------------------------------------------------
            // 3) Build low-level Ethernet / Modbus transport
            // ---------------------------------------------------------------------
            const string targetIpAddress = "192.168.1.193"; // PIC32 IP
            const int targetPort = 10000;                  // PIC waiting for client
            const int localPort = 10001;                   // same local port as Python bind

            // Raw UDP client (simple wrapper around UdpClient).
            var rawUdp = new RawUdpTransport(targetIpAddress, targetPort, localPort);

            // Adapter that exposes RawUdpTransport as ITrackTransport.
            ITrackTransport transport = new RawUdpTrackTransport(rawUdp);

            // ---------------------------------------------------------------------
            // 4) Communication client on top of the selected transport
            // ---------------------------------------------------------------------
            _trackCommClient = new TrackCommClientAsync(transport, _trackVariables);

            // ---------------------------------------------------------------------
            // 5) Bootloader helper objects (re-using legacy classes)
            // ---------------------------------------------------------------------
            _bootloaderHelpers ??= new TrackAmplifierBootloaderHelpers(
                FwPath,
                LoggerInstance);

            _sendNextFwDataPacket ??= new SendNextFwDataPacket(
                _trackCommClient,
                _bootloaderHelpers);

            // ---------------------------------------------------------------------
            // 6) Compose initialization steps
            // ---------------------------------------------------------------------
            var steps = new IInitializationStep[]
            {
                new ConnectToEthernetTargetStep(_trackCommClient, _trackVariables, LoggerInstance),
                new ResetAllSlavesStep(_trackCommClient, _trackVariables, LoggerInstance),
                new DataUploadStep(_trackCommClient, _trackVariables, LoggerInstance),
                new DetectSlavesStep(_trackCommClient, _trackVariables, LoggerInstance),
                new RecoverSlavesStep(_trackCommClient, _trackVariables, _sendNextFwDataPacket, _bootloaderHelpers, LoggerInstance),
                new FlashFwTrackamplifiersStep(_trackCommClient, _trackVariables, _sendNextFwDataPacket, _bootloaderHelpers, LoggerInstance),
                new InitTrackamplifiersStep(_trackCommClient, LoggerInstance),
                // Your new step that sets PWM defaults (e.g. 400) before enabling amps
                new SetDefaultPwmSetpointsStep(_trackVariables, LoggerInstance),
                new EnableTrackamplifiersStep(_trackCommClient, LoggerInstance),
            };

            _trackInitService = new TrackAmplifierInitializationServiceAsync(
                _trackCommClient,
                _trackVariables,
                steps,
                LoggerInstance);

            // Optional: log progress of individual steps
            _trackInitService.ProgressChanged += (s, e) =>
            {
                IoC.Logger.Log($"Track init: {e.StepName} - {e.Message}", LoggerInstance);
            };

            // ---------------------------------------------------------------------
            // 7) Create TrackControlMain and hook StatusChanged to start runtime
            // ---------------------------------------------------------------------
            _trackControlMain = new TrackControlMain(
                LoggerInstance,
                _trackCommClient,
                _trackVariables);

            _trackInitService.StatusChanged += (s, status) =>
            {
                IoC.Logger.Log($"Track init status: {status}", LoggerInstance);

                if (status == InitializationStatus.Completed &&
                    _trackControlMain != null &&
                    _appCts != null)
                {
                    // Start the 10 Hz runtime loop that:
                    // - checks TrackApplicationVariables.PendingWrites
                    // - sends EXEC_MBUS_SLAVE_DATA_EXCH frames when data changed
                    _trackControlMain.StartRuntime(_appCts.Token);
                }
            };

            // ---------------------------------------------------------------------
            // 8) Start communication and run the initialization pipeline
            // ---------------------------------------------------------------------
            await _trackCommClient.StartAsync(
                true,
                cancellationToken: _appCts.Token);

            await _trackInitService.InitializeAsync(_appCts.Token);

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
                _trackControlMain.StopRuntime();
            }
            catch { /* ignore */ }

            try
            {
                _appCts?.Cancel();
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

        public IReadOnlyList<TrackAmplifierItem> TrackAmplifiers
        {
            get
            {
                // If the track application has not created its variables yet,
                // simply return an empty list so the UI can safely call this
                // property at any time.
                if (_trackVariables == null)
                {
                    return Array.Empty<TrackAmplifierItem>();
                }

                // Expose the internal amplifier list as a read-only view.
                // The list itself is still owned and updated by the core logic.
                return _trackVariables.GetAmplifierListing();
            }
        }

        /// <summary>
        /// Sets the PWM set point (0..799) for a given amplifier.
        /// This updates HoldingReg0 bits 0..9 for the matching slave.
        /// </summary>
        public void SetAmplifierPwm(ushort slaveNumber, int pwm)
        {
            if (_trackVariables == null || _trackVariables.trackAmpItems == null)
                return;

            var amp = _trackVariables.trackAmpItems
                .FirstOrDefault(a => a.SlaveNumber == slaveNumber);

            if (amp == null)
                return;

            pwm = Math.Max(0, Math.Min(799, pwm));

            var regs = amp.HoldingReg;
            if (regs == null || regs.Length == 0)
                return;

            ushort reg0 = regs[0];

            // Clear bits 0..9
            reg0 = (ushort)(reg0 & ~0x03FF);

            // Set new PWM in bits 0..9
            reg0 |= (ushort)(pwm & 0x03FF);

            regs[0] = reg0;
            amp.HoldingReg = regs;
        }

        /// <summary>
        /// Sets or clears the emergency stop bit (HoldingReg0 bit 15)
        /// for a given amplifier.
        /// </summary>
        public void SetAmplifierEmStop(ushort slaveNumber, bool isEmStop)
        {
            if (_trackVariables == null || _trackVariables.trackAmpItems == null)
                return;

            var amp = _trackVariables.trackAmpItems
                .FirstOrDefault(a => a.SlaveNumber == slaveNumber);

            if (amp == null)
                return;

            var regs = amp.HoldingReg;
            if (regs == null || regs.Length == 0)
                return;

            ushort reg0 = regs[0];

            if (isEmStop)
            {
                // Set bit 15
                reg0 |= 0x8000;
            }
            else
            {
                // Clear bit 15
                reg0 = (ushort)(reg0 & ~0x8000);
            }

            regs[0] = reg0;
            amp.HoldingReg = regs;
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

        /// <summary>
        /// Returns the current list of track amplifiers from the core model.
        /// Returns an empty list if the track application is not running yet.
        /// </summary>
        public List<TrackAmplifierItem> GetAmplifierListing()
        {
            return _trackVariables?.GetAmplifierListing() ?? new List<TrackAmplifierItem>();
        }

        /// <summary>
        /// Updates the desired control parameters (PWM setpoint + EmoStop)
        /// for a given amplifier. The values are not sent immediately; they
        /// are queued in TrackApplicationVariables and sent by TrackControlMain
        /// at 10 Hz.
        /// </summary>
        public void SetAmplifierControl(ushort slaveNumber, int pwmSetpoint, bool emoStop)
        {
            _trackVariables?.SetDesiredAmplifierControl(slaveNumber, pwmSetpoint, emoStop);
        }

    }
}