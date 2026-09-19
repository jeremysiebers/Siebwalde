using SiebwaldeApp.Core;
using System;
using System.Collections.ObjectModel;
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

        #endregion

        #region Public commands

        /// <summary>Detect the expected hosts.</summary>
        public ICommand DetectHosts { get; set; }

        /// <summary>Detect hosts and start the detected ones.</summary>
        public ICommand InitAllControllers { get; set; }

        /// <summary>Start the track application.</summary>
        public ICommand InitTrackController { get; set; }

        /// <summary>Start the Fiddle Yard in real mode (falls back to simulator when not found).</summary>
        public ICommand InitFiddleYardController { get; set; }

        /// <summary>Start the Fiddle Yard in simulator mode (operator-activated).</summary>
        public ICommand InitFiddleYardSimulator { get; set; }

        /// <summary>Start the ECoS host in simulator mode, so Koploper can connect without hardware.</summary>
        public ICommand InitEcosSimulator { get; set; }

        #endregion

        #region Constructor

        public SiebwaldeInitPageViewModel()
        {
            Log2 = new ObservableCollection<string>();

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

            InitTrackController = new RelayCommand(async () => await StartTrackAsync());
            InitFiddleYardController = new RelayCommand(async () => await StartFiddleYardAsync(false));
            InitFiddleYardSimulator = new RelayCommand(async () => await StartFiddleYardAsync(true));
            InitEcosSimulator = new RelayCommand(async () => await StartEcosSimulatorAsync());

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
            if (!TrackControllerPresent)
            {
                Log("TrackController not detected. Detection or the real target is required before starting.");
                return;
            }

            Log("Starting track application...");
            await IoC.siebwaldeApplicationModel.StartTrackApplication();
            Log("Track application start requested.");
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
            Log("Starting ECoS host in simulator mode (Koploper can connect on port 15471)...");

            await IoC.siebwaldeApplicationModel.StartEcosHostSimulatorAsync();

            Log("ECoS simulator start requested.");
        }

        #endregion

        private void Log(string message)
        {
            Log2.Add($"{DateTime.Now:HH:mm:ss} {message}");
        }
    }
}
