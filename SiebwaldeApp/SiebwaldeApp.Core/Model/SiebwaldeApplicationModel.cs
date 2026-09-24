using System.Text;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Thin facade over <see cref="ITrackApplicationRuntime"/>.
    ///
    /// The track-part composition, the runtime write loop, the ECoS host and their lifetime now
    /// live in the runtime coordinator (the <see cref="ITrackApplicationRuntime"/> implementation),
    /// which is injected here. This model keeps the Fiddle Yard surface unchanged and delegates
    /// every track-control lifecycle/data operation to the runtime.
    /// </summary>
    public class SiebwaldeApplicationModel
    {
        #region Public events
        public event EventHandler? InstantiateFiddleYardWinForms;
        public event EventHandler? FiddleYardShowWinForms;
        public event EventHandler? FiddleYardShowSettingsWinForms;
        public FiddleYardController? FYcontroller;
        public FiddleYardController? YDcontroller;
        #endregion

        #region Private members

        private readonly NewMAC_IP_Conditioner _macIp = new();
        private readonly ITrackApplicationRuntime _runtime;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the application model facade.
        /// </summary>
        /// <param name="runtime">
        /// The runtime coordinator that owns the track-control runtime composition and lifetime.
        /// </param>
        /// <param name="controlTrace">
        /// Kept for constructor compatibility; the production control trace now lives in the
        /// runtime coordinator, which owns the track control path.
        /// </param>
        public SiebwaldeApplicationModel(ITrackApplicationRuntime runtime, IControlTrace? controlTrace = null)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

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
        /// Starts the track application in real mode (delegated to the runtime coordinator).
        /// </summary>
        public async Task StartTrackApplication()
            => await _runtime.StartAsync(TrackControlMode.Real);

        /// <summary>
        /// Stops the currently running track application, if one is active (delegated to the
        /// runtime coordinator; idempotent and fully async).
        /// </summary>
        public async Task StopTrackApplication()
            => await _runtime.StopAsync();

        /// <summary>
        /// Starts the ECoS host in simulator mode, so Koploper can be exercised without the
        /// track controller or physical hardware (delegated to the runtime coordinator).
        /// </summary>
        public async Task StartEcosHostSimulatorAsync()
            => await _runtime.StartAsync(TrackControlMode.Simulator);

        /// <summary>The ECoS mode that is actually active, or null when the host is not running.</summary>
        public TrackControlMode? ActiveEcosMode => _runtime.ActiveEcosMode;

        /// <summary>Diagnostics for the control path, or null when the ECoS host is not running.</summary>
        public ControlDiagnostics? ControlDiagnostics => _runtime.ControlDiagnostics;

        /// <summary>True while an unsafe divergence is latched and not yet reset.</summary>
        public bool IsControlPathUnsafe => _runtime.IsControlPathUnsafe;

        /// <summary>Explicit recovery for a latched safety fault. A latched fault never clears itself.</summary>
        public bool ResetControlSafety() => _runtime.ResetControlSafety();

        /// <summary>Read-only view of the current track amplifiers.</summary>
        public IReadOnlyList<TrackAmplifierItem> TrackAmplifiers => _runtime.TrackAmplifiers;

        /// <summary>Returns the current list of track amplifiers (empty when not running).</summary>
        public List<TrackAmplifierItem> GetAmplifierListing() => _runtime.GetAmplifierListing();

        /// <summary>Sets the PWM set point (0..799) for a given amplifier.</summary>
        public void SetAmplifierPwm(ushort slaveNumber, int pwm)
            => _runtime.SetAmplifierPwm(slaveNumber, pwm);

        /// <summary>Sets or clears the emergency stop bit for a given amplifier.</summary>
        public void SetAmplifierEmStop(ushort slaveNumber, bool isEmStop)
            => _runtime.SetAmplifierEmStop(slaveNumber, isEmStop);

        /// <summary>Updates the desired control parameters (PWM setpoint + EmoStop) for a given amplifier.</summary>
        public void SetAmplifierControl(ushort slaveNumber, int pwmSetpoint, bool emoStop)
            => _runtime.SetAmplifierControl(slaveNumber, pwmSetpoint, emoStop);

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
