using SiebwaldeApp.Core;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SiebwaldeApp
{
    /// <summary>
    /// View model for the start/initialization page. It detects the expected hosts,
    /// shows their presence, keeps a human-readable step/state log, and starts the
    /// functionality of detected hosts.
    /// </summary>
    public class SiebwaldeInitPageViewModel : BaseViewModel
    {
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

        #endregion

        #region Constructor

        public SiebwaldeInitPageViewModel()
        {
            Log2 = new ObservableCollection<string>();

            DetectHosts = new RelayCommand(async () => await DetectHostsAsync());

            InitAllControllers = new RelayCommand(async () =>
            {
                await DetectHostsAsync();

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

            Log("Init page ready. Press 'Detect hosts' to scan for FiddleYard, TrackController and Koploper.");

            // Run an initial detection pass so the page shows current status.
            _ = DetectHostsAsync();
        }

        #endregion

        #region Detection

        private async Task DetectHostsAsync()
        {
            if (IsDetecting)
            {
                return;
            }

            IsDetecting = true;
            Log("Detecting hosts...");

            try
            {
                var fiddleYard = await HostDetection.DetectFiddleYardAsync();
                FiddleYardPresent = fiddleYard.IsPresent;
                FiddleYardStatus = Format(fiddleYard);
                Log($"FiddleYard: {FiddleYardStatus}");

                var trackController = await HostDetection.DetectTrackControllerAsync();
                TrackControllerPresent = trackController.IsPresent;
                TrackControllerStatus = Format(trackController);
                Log($"TrackController: {TrackControllerStatus}");

                var koploper = await HostDetection.DetectKoploperAsync();
                KoploperPresent = koploper.IsPresent;
                KoploperStatus = Format(koploper);
                Log($"Koploper: {KoploperStatus}");

                Log("Detection finished.");
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

        #endregion

        private void Log(string message)
        {
            Log2.Add($"{DateTime.Now:HH:mm:ss} {message}");
        }
    }
}
