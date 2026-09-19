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
        public FiddleYardController? FYcontroller;
        public FiddleYardController? YDcontroller;
        public TrackControlMain? _trackControlMain;
        #endregion

        #region Private members
        
        private CancellationTokenSource _appCts;
        private readonly NewMAC_IP_Conditioner _macIp = new();        
        private TrackApplicationVariables? _trackVariables;        

        private ILogger TrackApplicationLogging;
        private TrackCommClientAsync _trackCommClient;
        private TrackAmplifierBootloaderHelpers _bootloaderHelpers;
        private SendNextFwDataPacket _sendNextFwDataPacket;
        private TrackAmplifierInitializationServiceAsync _trackInitService;

        /// <summary>
        /// ECoS host that serves Koploper. Owned by the host application and injected here
        /// so this model can start and stop it as part of the application lifecycle.
        /// </summary>
        private readonly IEcosHostService? _ecosHost;

        private string LoggerInstance { get; set; }
        static ILogger GetLogger(string file, string loggerinstance)
        {
            return new FileLogger(file, loggerinstance);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the application model.
        /// </summary>
        /// <param name="ecosHost">
        /// The ECoS host that serves Koploper (port 15471), supplied by the host application
        /// so the composition and lifetime stay outside the UI layer. May be null when the
        /// application runs without Koploper.
        /// </param>
        public SiebwaldeApplicationModel(IEcosHostService? ecosHost = null)
        {
            _ecosHost = ecosHost;

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
        /// <param name="forceSimulator">When true, start the Fiddle Yard in simulator mode without probing the target.</param>
        public async Task StartFYController(bool forceSimulator = false)
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
            await FYcontroller.StartFiddleYardControllerAsync(forceSimulator);
            IoC.Logger.Log("FiddleYard Controller started.", "");
        }



        /// <summary>
        /// Starts the track application: builds the UDP transport, the track communication client, the bootloader
        /// helpers, the initialization pipeline, and the runtime write loop.
        /// </summary>
        /// <remarks>If the runtime controller already exists, the method returns without doing anything. The
        /// runtime write loop is started when initialization reaches the Completed status.</remarks>
        /// <returns>A task that completes when the initialization pipeline has finished.</returns>
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
            var targetIpAddress = CoreConfiguration.TrackControllerIpAddress;
            var targetPort = CoreConfiguration.TrackControllerSendingPort;
            var localPort = CoreConfiguration.TrackControllerReceivingPort;

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
                CoreConfiguration.TrackAmplifierFirmwarePath,
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

            // ---------------------------------------------------------------------
            // 9) Serve Koploper: start the ECoS host on top of the real backend.
            //    This runs last so the initialization sequencing above is unchanged.
            // ---------------------------------------------------------------------
            await StartEcosHostAsync(TrackControlMode.Real);
        }

        /// <summary>
        /// The ECoS mode that is actually active, or null when the host is not running. The
        /// UI reads this instead of assuming that a start request succeeded.
        /// </summary>
        public TrackControlMode? ActiveEcosMode => _ecosHost?.Mode;

        /// <summary>
        /// Starts the ECoS host in simulator mode, so Koploper can be exercised without the
        /// track controller or physical hardware. Reports what actually happened.
        /// </summary>
        public async Task<EcosHostStartResult> StartEcosHostSimulatorAsync()
            => await StartEcosHostAsync(TrackControlMode.Simulator);

        /// <summary>
        /// Starts the ECoS host (the server Koploper connects to on port 15471) in the
        /// requested mode and reports the outcome. Mode conflicts are resolved by the host;
        /// see <see cref="IEcosHostService.StartAsync"/>.
        /// </summary>
        private async Task<EcosHostStartResult> StartEcosHostAsync(TrackControlMode mode)
        {
            if (_ecosHost is null)
            {
                IoC.Logger.Log($"ECoS host not available; skipping the {mode} start.", LoggerInstance);
                return EcosHostStartResult.NotAvailable;
            }

            try
            {
                var result = await _ecosHost.StartAsync(mode, _trackCommClient, _trackVariables, _appCts.Token);

                IoC.Logger.Log(
                    $"ECoS host {mode} start result: {result}; active mode is {_ecosHost.Mode?.ToString() ?? "<none>"}.",
                    LoggerInstance);

                return result;
            }
            catch (Exception ex)
            {
                // Serving Koploper must never take the application down.
                IoC.Logger.Log($"ECoS host failed to start in {mode} mode: {ex.Message}", LoggerInstance);
                return EcosHostStartResult.Failed;
            }
        }

        /// <summary>
        /// Stops the ECoS host when it is running. Safe to call when it was never started.
        /// </summary>
        public void StopEcosHost()
        {
            if (_ecosHost is null || !_ecosHost.IsRunning)
                return;

            IoC.Logger.Log("Stopping ECoS host...", LoggerInstance);

            try
            {
                _ecosHost.Stop();
            }
            catch (Exception ex)
            {
                IoC.Logger.Log($"ECoS host failed to stop: {ex.Message}", LoggerInstance);
            }

            IoC.Logger.Log("ECoS host stopped.", LoggerInstance);
        }


        /// <summary>
        /// Stops the currently running track application, if one is active.
        /// </summary>
        /// <remarks>This method ensures that the track application is properly stopped and its resources
        /// are released.  If no track application is active, the method exits without performing any action.  Any
        /// errors encountered during the stopping process are logged.</remarks>
        public void StopTrackApplication()
        {
            // Stop the ECoS host first. In simulator mode it runs without a track
            // controller, so it must also be stopped when _trackControlMain is null.
            StopEcosHost();

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
        public void  SetAmplifierControl(ushort slaveNumber, int pwmSetpoint, bool emoStop)
        {
            _trackVariables?.SetDesiredAmplifierControl(slaveNumber, pwmSetpoint, emoStop);
        }

    }
}