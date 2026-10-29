using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Topology;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace SiebwaldeApp
{
    /// <summary>
    /// View model for the start/initialization page. It detects the expected hosts,
    /// shows their presence, keeps a human-readable step/state log, and starts the
    /// functionality of detected hosts. Host detection repeats automatically at a
    /// fixed interval without overlapping passes.
    /// </summary>
    public class SiebwaldeInitPageViewModel : BaseViewModel
    {
        #region Private members

        /// <summary>Interval between the automatic host re-detection passes.</summary>
        private const int DetectionIntervalSeconds = 10;

        /// <summary>Timer that triggers the automatic host re-detection on the UI thread.</summary>
        private readonly DispatcherTimer _detectionTimer;

        /// <summary>The runtime coordinator that owns the track-control runtime lifecycle.</summary>
        private readonly ITrackApplicationRuntime _runtime;

        private readonly RelayCommand _startRuntimeCommand;
        private readonly RelayCommand _stopRuntimeCommand;
        private readonly RelayCommand _restartRuntimeCommand;
        private readonly RelayCommand _initTrackControllerCommand;
        private readonly RelayCommand _initEcosSimulatorCommand;
        private readonly RelayCommand _initFullSimulationCommand;
        private readonly RelayCommand _recoverGlobalStopCommand;

        private readonly RelayCommand _stepInitCommand;
        private readonly RelayCommand _retryInitCommand;
        private readonly RelayCommand _backtrackInitCommand;
        private readonly RelayCommand _enableTrackPowerCommand;
        private readonly RelayCommand _disableTrackPowerCommand;

        #endregion

        #region Public properties

        /// <summary>Human-readable step/state log shown on the page.</summary>
        public ObservableCollection<string> Log2 { get; set; }

        /// <summary>Status text for the Fiddle Yard host.</summary>
        public string FiddleYardStatus { get; set; } = "Not checked";

        /// <summary>Status text for the TrackController host.</summary>
        public string TrackControllerStatus { get; set; } = "Not checked";

        /// <summary>Status text for the Koploper host.</summary>
        public string KoploperStatus { get; set; } = "Not checked";

        /// <summary>True when the Fiddle Yard was detected.</summary>
        public bool FiddleYardPresent { get; set; }

        /// <summary>True when the TrackController was detected.</summary>
        public bool TrackControllerPresent { get; set; }

        /// <summary>True when Koploper was detected.</summary>
        public bool KoploperPresent { get; set; }

        /// <summary>True while a detection pass is running.</summary>
        public bool IsDetecting { get; set; }

        /// <summary>
        /// Human-readable description of the ECoS mode that is actually active, so the page
        /// shows reality instead of assuming a start request succeeded.
        /// </summary>
        public string EcosModeStatus { get; set; } = "ECoS host: not running";

        /// <summary>
        /// Human-readable description of the logical-section -&gt; physical-amplifier binding declared
        /// by the active layout profile (for example "Physical binding: 1, 3, 4, 6" or
        /// "Physical binding: none"). Read from the runtime's physical-amplifier projection.
        /// </summary>
        public string PhysicalBindingStatus { get; set; } = "Physical binding: none";

        /// <summary>
        /// READ-ONLY Koploper logical-block -&gt; section -&gt; amplifier -&gt; bezetmelder mapping for
        /// the active profile, plus the validation status (conflicts/missing mapping). This is a view
        /// only: the <c>LayoutProfile</c> JSON remains the single runtime configuration authority.
        /// </summary>
        public string KoploperMappingStatus { get; set; } = "Koploper mapping: none";

        /// <summary>
        /// Health of the control path as reported by the diagnostics surface: healthy, warning
        /// or unsafe. This view model only displays it; the decision is made in the host.
        /// </summary>
        public string ControlHealthStatus { get; set; } = "Control path: not running";

        /// <summary>The latest diagnostic, already formatted by the domain model.</summary>
        public string LatestDiagnostic { get; set; } = "";

        /// <summary>True when the control path is latched unsafe, so a reset is meaningful.</summary>
        public bool CanResetControlSafety { get; set; }

        /// <summary>
        /// Human-readable global-stop state (Disarmed/Armed/Latched + reason) shown to the operator.
        /// </summary>
        public string GlobalStopStatus { get; set; } = "Global stop: disarmed";

        /// <summary>
        /// True when the global stop is latched and an operator recovery is meaningful, so the
        /// "Recover global stop" command is enabled.
        /// </summary>
        public bool CanRecoverGlobalStop { get; set; }

        /// <summary>Human-readable lifecycle state of the track-control runtime.</summary>
        public string RuntimeStateText { get; set; } = "Runtime: stopped";

        /// <summary>Distinct failure line shown when the runtime enters the Failed state.</summary>
        public string FailureText { get; set; } = "";

        /// <summary>Operator-visible initialization stage tree (7 stages with state + per-amp + recovery).</summary>
        public ObservableCollection<TrackInitStageRow> InitStages { get; } = new();

        /// <summary>Dutch operator log of state-machine events (timestamp + session + stage + amp).</summary>
        public ObservableCollection<string> OperatorLog { get; } = new();

        /// <summary>Human-readable track-power state ("Trackspanning: uit/aan").</summary>
        public string TrackPowerStatus { get; set; } = "Trackspanning: uit";

        /// <summary>True when the operator may enable track power (authorized + global ready).</summary>
        public bool CanEnableTrackPower { get; set; }

        /// <summary>True when track power is ON and may be disabled.</summary>
        public bool CanDisableTrackPower { get; set; }

        #endregion

        #region Public commands

        /// <summary>Detect the expected hosts.</summary>
        public ICommand DetectHosts { get; set; }

        /// <summary>Detect hosts and start the detected ones.</summary>
        public ICommand InitAllControllers { get; set; }

        /// <summary>Start the track application.</summary>
        public ICommand InitTrackController => _initTrackControllerCommand;

        /// <summary>Start the Fiddle Yard in real mode (falls back to simulator when not found).</summary>
        public ICommand InitFiddleYardController { get; set; }

        /// <summary>Start the Fiddle Yard in simulator mode (operator-activated).</summary>
        public ICommand InitFiddleYardSimulator { get; set; }

        /// <summary>Start the ECoS host in simulator mode, so Koploper can connect without hardware.</summary>
        public ICommand InitEcosSimulator => _initEcosSimulatorCommand;

        /// <summary>Start the full software simulation (real track-control chain on the deterministic transport).</summary>
        public ICommand InitFullSimulation => _initFullSimulationCommand;

        /// <summary>Explicit recovery for a latched control-path safety fault.</summary>
        public ICommand ResetControlSafety { get; set; }
        public ICommand GlobalEmergencyStop { get; }

        /// <summary>
        /// Operator-initiated global-stop recovery: prompts to confirm the physical stop inputs are
        /// clear, then authorizes the startup re-arm. Enabled only while the global stop is latched.
        /// </summary>
        public ICommand RecoverGlobalStop => _recoverGlobalStopCommand;

        /// <summary>Start the track-control runtime in the last used mode (defaults to simulator when none).</summary>
        public ICommand StartRuntime => _startRuntimeCommand;

        /// <summary>Stop the track-control runtime.</summary>
        public ICommand StopRuntime => _stopRuntimeCommand;

        /// <summary>Restart the track-control runtime.</summary>
        public ICommand RestartRuntime => _restartRuntimeCommand;

        /// <summary>Manual initialization: run exactly one stage (step).</summary>
        public ICommand StepInit => _stepInitCommand;

        /// <summary>Manual initialization: re-run the current stage (retry).</summary>
        public ICommand RetryInit => _retryInitCommand;

        /// <summary>Manual initialization: move back one stage (backtrack).</summary>
        public ICommand BacktrackInit => _backtrackInitCommand;

        /// <summary>Authorize + run the gated Stage 6 track-power transition.</summary>
        public ICommand EnableTrackPower => _enableTrackPowerCommand;

        /// <summary>Clear HR_TRACK_POWER (de-energize the bridges).</summary>
        public ICommand DisableTrackPower => _disableTrackPowerCommand;

        #endregion

        #region Constructor

        public SiebwaldeInitPageViewModel()
        {
            Log2 = new ObservableCollection<string>();

            _runtime = IoC.TrackRuntime;
            GlobalEmergencyStop = new RelayCommand(() =>
            {
                try { _runtime.EmergencyStop(); }
                catch (Exception ex) { Log($"Emergency stop failed: {ex.Message}"); }
            });
            _runtime.StateChanged += HandleRuntimeStateChanged;
            _runtime.Faulted += HandleRuntimeFaulted;
            _runtime.InitStageStatusChanged += HandleInitStageStatusChanged;

            UpdatePhysicalBindingStatus();
            UpdateKoploperMappingStatus();
            UpdateGlobalStopStatus();

            DetectHosts = new RelayCommand(async () => await DetectHostsAsync(logAlways: true));

            InitAllControllers = new RelayCommand(async () =>
            {
                await DetectHostsAsync(logAlways: true);

                if (FiddleYardPresent)
                {
                    await StartFiddleYardAsync(false);
                }

                if (TrackControllerPresent)
                {
                    await StartTrackAsync();
                }
            });

            InitFiddleYardController = new RelayCommand(async () => await StartFiddleYardAsync(false));
            InitFiddleYardSimulator = new RelayCommand(async () => await StartFiddleYardAsync(true));
            ResetControlSafety = new RelayCommand(ResetControlSafetyNow);

            // Operator-initiated global-stop recovery. The button is enabled only while the
            // global stop is latched (CanRecoverGlobalStop); the physical-stop-clear evidence is
            // the operator's explicit confirmation (the physical EMO/stop buttons are hardware and
            // not software-readable). The command itself also re-checks the latched state.
            _recoverGlobalStopCommand = new RelayCommand(RecoverGlobalStopNow);

            _startRuntimeCommand = new RelayCommand(
                async () => await StartRuntimeAsync(),
                () => TrackRuntimeControlPolicy.CanStart(_runtime.State));
            _stopRuntimeCommand = new RelayCommand(
                async () => await StopRuntimeAsync(),
                () => TrackRuntimeControlPolicy.CanStop(_runtime.State));
            _restartRuntimeCommand = new RelayCommand(
                async () => await RestartRuntimeAsync(),
                () => TrackRuntimeControlPolicy.CanRestart(_runtime.State));

            _initTrackControllerCommand = new RelayCommand(
                async () => await StartTrackAsync(),
                () => TrackRuntimeControlPolicy.CanStart(_runtime.State));
            _initEcosSimulatorCommand = new RelayCommand(
                async () => await StartEcosSimulatorAsync(),
                () => TrackRuntimeControlPolicy.CanStart(_runtime.State));
            _initFullSimulationCommand = new RelayCommand(
                async () => await StartFullSimulationAsync(),
                () => TrackRuntimeControlPolicy.CanStart(_runtime.State));

            _stepInitCommand = new RelayCommand(async () => await StepInitAsync());
            _retryInitCommand = new RelayCommand(async () => await RetryInitAsync());
            _backtrackInitCommand = new RelayCommand(BacktrackInitNow);
            _enableTrackPowerCommand = new RelayCommand(async () => await EnableTrackPowerAsync(), () => CanEnableTrackPower);
            _disableTrackPowerCommand = new RelayCommand(async () => await DisableTrackPowerAsync(), () => CanDisableTrackPower);

            Log("Init page ready. Press 'Detect hosts' to scan for FiddleYard, TrackController and Koploper.");

            // Re-detect the hosts periodically. The DispatcherTimer ticks on the UI thread and
            // DetectHostsAsync guards against overlapping passes via IsDetecting.
            _detectionTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(DetectionIntervalSeconds)
            };
            _detectionTimer.Tick += async (_, __) => await DetectHostsAsync(logAlways: false);
            _detectionTimer.Start();

            // Run an initial detection pass so the page shows current status.
            _ = DetectHostsAsync(logAlways: true);
        }

        #endregion

        #region Page lifetime

        /// <summary>
        /// Starts the periodic host re-detection. Called when the page becomes visible.
        /// </summary>
        public void StartDetection() => _detectionTimer.Start();

        /// <summary>
        /// Stops the periodic host re-detection. Called when the page is unloaded so that
        /// navigating away does not keep the timer (and this view model) alive.
        /// </summary>
        public void StopDetection() => _detectionTimer.Stop();

        #endregion

        #region Detection

        private async Task DetectHostsAsync(bool logAlways)
        {
            if (IsDetecting)
            {
                return;
            }

            // Refresh the global-stop surface on every detection pass so the recover command
            // enables promptly once the startup precondition observes a latched controller.
            UpdateGlobalStopStatus();

            IsDetecting = true;
            if (logAlways)
            {
                Log("Detecting hosts...");
            }

            try
            {
                var previousFiddleYard = FiddleYardPresent;
                var previousTrackController = TrackControllerPresent;
                var previousKoploper = KoploperPresent;

                var fiddleYard = await HostDetection.DetectFiddleYardAsync();
                FiddleYardPresent = fiddleYard.IsPresent;
                FiddleYardStatus = Format(fiddleYard);
                if (logAlways || FiddleYardPresent != previousFiddleYard)
                {
                    Log($"FiddleYard: {FiddleYardStatus}");
                }

                var trackController = await HostDetection.DetectTrackControllerAsync();
                TrackControllerPresent = trackController.IsPresent;
                TrackControllerStatus = Format(trackController);
                if (logAlways || TrackControllerPresent != previousTrackController)
                {
                    Log($"TrackController: {TrackControllerStatus}");
                }

                var koploper = await HostDetection.DetectKoploperAsync();
                KoploperPresent = koploper.IsPresent;
                KoploperStatus = Format(koploper);
                if (logAlways || KoploperPresent != previousKoploper)
                {
                    Log($"Koploper: {KoploperStatus}");
                }

                if (logAlways)
                {
                    Log("Detection finished.");
                }
            }
            catch (Exception ex)
            {
                Log($"Detection failed: {ex.Message}");
            }
            finally
            {
                IsDetecting = false;
            }
        }

        private static string Format(HostDetectionResult result)
            => $"{(result.IsPresent ? "Present" : "Absent")} - {result.Target} ({result.Detail})";

        #endregion

        #region Start actions

        private async Task StartTrackAsync()
        {
            if (!TrackRuntimeControlPolicy.CanStart(_runtime.State))
            {
                Log($"Cannot start: runtime is {_runtime.State}.");
                return;
            }

            if (!TrackControllerPresent)
            {
                Log("TrackController not detected. Detection or the real target is required before starting.");
                return;
            }

            Log("Starting track application...");
            await IoC.siebwaldeApplicationModel.StartTrackApplication();
            Log("Track application start requested.");

            UpdateControlStatus();
            Log($"ECoS after track start: {EcosModeStatus}");
        }

        private async Task StartFiddleYardAsync(bool simulator)
        {
            if (!simulator && !FiddleYardPresent)
            {
                Log("Fiddle Yard not detected. Use the simulator button to start it in simulation mode.");
                return;
            }

            Log(simulator
                ? "Starting Fiddle Yard in simulator mode (operator activated)..."
                : "Starting Fiddle Yard...");

            await IoC.siebwaldeApplicationModel.StartFYController(simulator);
            Log("Fiddle Yard start requested.");
        }

        /// <summary>
        /// Starts the ECoS host against the software simulator. No track controller or
        /// hardware is required; Koploper can connect on port 15471.
        /// </summary>
        private async Task StartEcosSimulatorAsync()
        {
            if (!TrackRuntimeControlPolicy.CanStart(_runtime.State))
            {
                Log($"Cannot start: runtime is {_runtime.State}.");
                return;
            }

            Log("Starting ECoS host in simulator mode (Koploper can connect on port 15471)...");

            await IoC.siebwaldeApplicationModel.StartEcosHostSimulatorAsync();

            UpdateControlStatus();
            Log($"ECoS simulator start requested. {EcosModeStatus}");
        }

        /// <summary>
        /// Starts the full software simulation: the real track-control chain driven by the
        /// deterministic transport, with a controllable simulated-amplifier I/O surface.
        /// </summary>
        private async Task StartFullSimulationAsync()
        {
            if (!TrackRuntimeControlPolicy.CanStart(_runtime.State))
            {
                Log($"Cannot start: runtime is {_runtime.State}.");
                return;
            }

            Log("Starting full software simulation (real track-control chain on the deterministic transport)...");

            await IoC.siebwaldeApplicationModel.StartFullSimulationAsync();

            UpdateControlStatus();
            Log($"Full software simulation start requested. {EcosModeStatus}");
        }

        /// <summary>Start the track-control runtime in the last used mode (defaults to simulator when none).</summary>
        private async Task StartRuntimeAsync()
        {
            if (!TrackRuntimeControlPolicy.CanStart(_runtime.State))
            {
                Log($"Cannot start: runtime is {_runtime.State}.");
                return;
            }

            Log($"Starting track-control runtime (mode: {_runtime.LastRequestedMode ?? TrackControlMode.Simulator})...");
            await _runtime.StartAsync(_runtime.LastRequestedMode ?? TrackControlMode.Simulator);
            Log($"Track-control runtime start requested. {RuntimeStateText}");
        }

        /// <summary>Stops the track-control runtime.</summary>
        private async Task StopRuntimeAsync()
        {
            Log("Stopping track-control runtime...");
            await _runtime.StopAsync();
            Log($"Track-control runtime stop requested. {RuntimeStateText}");
        }

        /// <summary>Restarts the track-control runtime.</summary>
        private async Task RestartRuntimeAsync()
        {
            Log("Restarting track-control runtime...");
            await _runtime.RestartAsync();
            Log($"Track-control runtime restart requested. {RuntimeStateText}");
        }

        /// <summary>Marshals a runtime state change to the UI thread and refreshes the surface.</summary>
        private void HandleRuntimeStateChanged(object? sender, TrackRuntimeStatusChangedEventArgs e)
        {
            _detectionTimer.Dispatcher.BeginInvoke(() =>
            {
                RuntimeStateText = $"Runtime: {e.State}";
                if (e.FailureReason is not null)
                {
                    FailureText = $"Failure: {e.FailureReason}";
                }

                _startRuntimeCommand.RaiseCanExecuteChanged();
                _stopRuntimeCommand.RaiseCanExecuteChanged();
                _restartRuntimeCommand.RaiseCanExecuteChanged();
                _initTrackControllerCommand.RaiseCanExecuteChanged();
                _initEcosSimulatorCommand.RaiseCanExecuteChanged();
                _initFullSimulationCommand.RaiseCanExecuteChanged();
                UpdateGlobalStopStatus();
            });
        }

        /// <summary>Marshals a runtime fault to the UI thread and shows a distinct failure line.</summary>
        private void HandleRuntimeFaulted(object? sender, RuntimeFaultEventArgs e)
        {
            _detectionTimer.Dispatcher.BeginInvoke(() =>
            {
                FailureText = $"Fault: {e.Source}: {e.Exception.Message}";
            });
        }

        /// <summary>
        /// Refreshes <see cref="EcosModeStatus"/> from the application model so the page
        /// always shows the mode that is really active.
        /// </summary>
        private void UpdateControlStatus()
        {
            var mode = IoC.siebwaldeApplicationModel.ActiveEcosMode;

            EcosModeStatus = mode is null
                ? "ECoS host: not running"
                : $"ECoS host: {mode} mode active";

            UpdatePhysicalBindingStatus();
            UpdateKoploperMappingStatus();

            var diagnostics = IoC.siebwaldeApplicationModel.ControlDiagnostics;
            if (diagnostics is null)
            {
                ControlHealthStatus = "Control path: not running";
                LatestDiagnostic = "";
                CanResetControlSafety = false;
                return;
            }

            ControlHealthStatus = diagnostics.IsUnsafe
                ? "Control path: UNSAFE (latched)"
                : $"Control path: {diagnostics.CurrentSeverity}";

            var latest = diagnostics.LatchedUnsafe ?? diagnostics.Latest;
            LatestDiagnostic = latest?.ToString() ?? "";

            CanResetControlSafety = diagnostics.IsUnsafe;
        }

        /// <summary>
        /// Refreshes <see cref="PhysicalBindingStatus"/> from the runtime's physical-amplifier
        /// projection so the page shows which REAL amplifiers the profile binds.
        /// </summary>
        private void UpdatePhysicalBindingStatus()
        {
            var binding = IoC.TrackRuntime.PhysicalAmplifierBinding;
            PhysicalBindingStatus = binding is null || binding.Count == 0
                ? "Physical binding: none"
                : $"Physical binding: {string.Join(", ", binding)}";
        }

        /// <summary>
        /// Refreshes <see cref="KoploperMappingStatus"/> from the active profile via the pure
        /// <see cref="KoploperMappingViewProjector"/>. The projector is read-only and holds no
        /// runtime authority; this view model only renders its text.
        /// </summary>
        private void UpdateKoploperMappingStatus()
        {
            var profile = IoC.TrackRuntime.ActiveFullSimulationProfile;
            KoploperMappingStatus = profile is null
                ? "Koploper mapping: none"
                : KoploperMappingViewProjector.Build(profile).ToDisplayText();
        }

        /// <summary>
        /// Clears a latched safety fault. The view model only asks for the reset; the host
        /// owns the latch and the safety decision.
        /// </summary>
        private void ResetControlSafetyNow()
        {
            var applied = IoC.siebwaldeApplicationModel.ResetControlSafety();
            UpdateControlStatus();

            Log(applied
                ? "Control safety reset applied."
                : "Control safety reset refused: the underlying condition is still present.");
        }

        /// <summary>
        /// Operator-initiated global-stop recovery. Prompts the operator to confirm the physical
        /// stop inputs (EMO / stop buttons) are released; only on explicit confirmation does it
        /// authorize the startup re-arm. The operator confirmation is the physical-stop-clear
        /// evidence source per the frozen architecture (R8-2 / Amendment A2): the physical buttons
        /// are hardware and not software-readable, so no sensor is consulted.
        /// </summary>
        private void RecoverGlobalStopNow()
        {
            if (!_runtime.GlobalStopState.IsLatched)
            {
                Log("Global stop is not latched; recovery is not required.");
                return;
            }

            var confirmed = System.Windows.MessageBox.Show(
                "Re-arming the global stop lets the track amplifiers be re-energized.\n\n" +
                "Confirm that ALL physical stop inputs (EMO / stop buttons) are released before continuing.",
                "Confirm physical stop inputs clear",
                System.Windows.MessageBoxButton.OKCancel,
                System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.OK;

            if (!confirmed)
            {
                Log("Global stop recovery cancelled: physical stop inputs were not confirmed clear.");
                return;
            }

            _runtime.AuthorizeGlobalStopRecovery(() => true);
            Log("Global stop recovery authorized (operator confirmed physical stop inputs clear).");
            UpdateGlobalStopStatus();
        }

        /// <summary>
        /// Refreshes <see cref="GlobalStopStatus"/> and <see cref="CanRecoverGlobalStop"/> from the
        /// runtime's read-only global-stop projection and updates the recover command's enabled state.
        /// </summary>
        private void UpdateGlobalStopStatus()
        {
            var view = _runtime.GlobalStopState;
            GlobalStopStatus = view.IsLatched
                ? $"Global stop: LATCHED ({(view.Reason?.ToString() ?? "unknown")})"
                : $"Global stop: {view.State.ToString().ToLowerInvariant()}";
            CanRecoverGlobalStop = view.IsLatched;
            OnPropertyChanged(nameof(GlobalStopStatus));
            OnPropertyChanged(nameof(CanRecoverGlobalStop));
        }

        #endregion

        private void Log(string message)
        {
            Log2.Add($"{DateTime.Now:HH:mm:ss} {message}");
        }

        // ---------------------------------------------------------------------------------
        // Initialization stage tree + Dutch operator log
        // ---------------------------------------------------------------------------------

        /// <summary>One row of the operator-visible stage tree.</summary>
        public sealed class TrackInitStageRow
        {
            public string Name { get; set; } = "";
            public string State { get; set; } = "";
            public string Detail { get; set; } = "";
        }

        /// <summary>Marshals a stage-status change to the UI thread and rebuilds the stage tree + Dutch log.</summary>
        private void HandleInitStageStatusChanged(object? sender, TrackInitStageStatusChangedEventArgs e)
        {
            _detectionTimer.Dispatcher.BeginInvoke(() =>
            {
                RefreshInitStages();
                LogOperatorEvent(e.Status);
            });
        }

        /// <summary>Rebuilds the seven-stage tree from the runtime's current stage statuses.</summary>
        private void RefreshInitStages()
        {
            InitStages.Clear();
            foreach (var status in _runtime.InitStages)
            {
                var detail = BuildStageDetail(status);
                InitStages.Add(new TrackInitStageRow
                {
                    Name = $"{StageNameDutch(status.Stage)}",
                    State = StageStateDutch(status.State),
                    Detail = detail
                });
            }

            UpdateTrackPowerSurface();
        }

        private static string BuildStageDetail(TrackInitStageStatus status)
        {
            if (status.FailureReason is { Length: > 0 })
            {
                return $"Mislukt: {status.FailureReason}" +
                       (status.RecoveryAction is { Length: > 0 } ? $" — Herstel: {status.RecoveryAction}" : "");
            }

            if (status.Amps.Count > 0)
            {
                var amps = string.Join(", ", status.Amps.Select(a => $"amp {a.SlaveNumber}: {(a.Ok ? "ok" : "niet ok")} ({a.Detail})"));
                return amps;
            }

            return "";
        }

        private void LogOperatorEvent(TrackInitStageStatus status)
        {
            var line = $"{DateTime.Now:HH:mm:ss} — Stage {status.Stage}: {StageNameDutch(status.Stage)} — {StageStateDutch(status.State)}";
            if (status.FailureReason is { Length: > 0 })
            {
                line += $" — {status.FailureReason}";
            }

            OperatorLog.Insert(0, line);
            while (OperatorLog.Count > 500)
            {
                OperatorLog.RemoveAt(OperatorLog.Count - 1);
            }
        }

        private static string StageNameDutch(TrackInitStage stage) => stage switch
        {
            TrackInitStage.ControllerConnection => "Controllerverbinding",
            TrackInitStage.FirmwareAddress => "Firmware-/adresverificatie",
            TrackInitStage.AmplifierStandby => "Amplifier STANDBY",
            TrackInitStage.ConfigureValidate => "Configureren en valideren",
            TrackInitStage.GlobalReady => "Globaal gereed",
            TrackInitStage.AmpPowerEnable => "Trackspanning inschakelen",
            TrackInitStage.NormalOperation => "Normaal bedrijf",
            _ => stage.ToString()
        };

        private static string StageStateDutch(TrackInitStageState state) => state switch
        {
            TrackInitStageState.Pending => "In afwachting",
            TrackInitStageState.Running => "Bezig",
            TrackInitStageState.Passed => "Geslaagd",
            TrackInitStageState.Failed => "Mislukt",
            TrackInitStageState.Blocked => "Geblokkeerd",
            _ => state.ToString()
        };

        private void UpdateTrackPowerSurface()
        {
            TrackPowerStatus = _runtime.TrackPowerOn
                ? "Trackspanning: aan"
                : "Trackspanning: uit";
            // Defense-in-depth (R2): the enable button is only offered once Stage 5 (GlobalReady)
            // has Passed; the state machine enforces the same precondition on the write path.
            var globalReadyPassed = _runtime.InitStages.Any(
                s => s.Stage == TrackInitStage.GlobalReady && s.State == TrackInitStageState.Passed);
            CanEnableTrackPower = !_runtime.TrackPowerOn && globalReadyPassed;
            CanDisableTrackPower = _runtime.TrackPowerOn;
            OnPropertyChanged(nameof(TrackPowerStatus));
            OnPropertyChanged(nameof(CanEnableTrackPower));
            OnPropertyChanged(nameof(CanDisableTrackPower));
            _enableTrackPowerCommand.RaiseCanExecuteChanged();
            _disableTrackPowerCommand.RaiseCanExecuteChanged();
        }

        private async Task StepInitAsync()
        {
            var outcome = await _runtime.StepInitAsync();
            Log($"Init stap uitgevoerd: {StageStateDutch(outcome)}.");
        }

        private async Task RetryInitAsync()
        {
            var outcome = await _runtime.RetryInitAsync();
            Log($"Init opnieuw geprobeerd: {StageStateDutch(outcome)}.");
        }

        private void BacktrackInitNow()
        {
            _runtime.BacktrackInit();
            Log("Init een stap teruggezet (backtrack).");
        }

        /// <summary>
        /// Explicit operator-gated track-power enable: prompts for confirmation before energizing
        /// the H-bridges at neutral (never silently energized), then authorizes and runs Stage 6.
        /// </summary>
        private async Task EnableTrackPowerAsync()
        {
            var confirmed = System.Windows.MessageBox.Show(
                "Trackspanning inschakelen bekrachtigt de H-bruggen op neutraal PWM (399) " +
                "voor bezetmeldingsdetectie.\n\nEr wordt GEEN rijspanning (niet-neutraal PWM) afgegeven. " +
                "Bevestig dat de baan veilig is voordat u doorgaat.",
                "Trackspanning inschakelen bevestigen",
                System.Windows.MessageBoxButton.OKCancel,
                System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.OK;

            if (!confirmed)
            {
                Log("Trackspanning inschakelen geannuleerd door operator.");
                return;
            }

            _runtime.AuthorizeTrackPowerEnable();
            var ok = await _runtime.EnableTrackPowerAsync();
            Log(ok
                ? "Trackspanning ingeschakeld (TRACK_POWER_ON)."
                : "Trackspanning NIET ingeschakeld: niet alle amplifiers zijn veilig bekrachtigd.");
            UpdateTrackPowerSurface();
        }

        private async Task DisableTrackPowerAsync()
        {
            await _runtime.DisableTrackPowerAsync();
            Log("Trackspanning uitgeschakeld (bruggen gede-energiseerd).");
            UpdateTrackPowerSurface();
        }
    }
}
