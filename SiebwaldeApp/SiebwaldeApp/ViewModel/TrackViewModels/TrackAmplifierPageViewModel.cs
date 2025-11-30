using SiebwaldeApp.Core;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace SiebwaldeApp
{
    /// <summary>
    /// View model that exposes a live overview of the Modbus master
    /// and all track amplifiers for the TrackAmplifierPage.
    /// </summary>
    public class TrackAmplifierPageViewModel : BaseViewModel
    {
        #region Private members

        private readonly DispatcherTimer _refreshTimer;
        private bool _areAmplifiersExpanded;

        #endregion

        #region Public properties

        /// <summary>
        /// Visual representation of the Modbus master that talks to all slaves.
        /// </summary>
        public TrackAmplifierMasterViewModel Master { get; }

        /// <summary>
        /// Visual representation of all track amplifiers (slave numbers 1..50).
        /// </summary>
        public ObservableCollection<TrackAmplifierVisualViewModel> Amplifiers { get; }

        /// <summary>
        /// Visual representation of the backplane slaves (slave numbers 51..55),
        /// shown as smaller boxes at the bottom of the page.
        /// </summary>
        public ObservableCollection<TrackAmplifierVisualViewModel> BackplaneSlaves { get; }

        /// <summary>
        /// When true, all amplifier boxes show their expanded view
        /// (all register values); when false they show only the key parameters.
        /// </summary>
        public bool AreAmplifiersExpanded
        {
            get => _areAmplifiersExpanded;
            set
            {
                if (_areAmplifiersExpanded == value)
                    return;

                _areAmplifiersExpanded = value;
                OnPropertyChanged(nameof(AreAmplifiersExpanded));
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// Sets up the collections and starts a 2 Hz refresh timer to
        /// sample the core model state.
        /// </summary>
        public TrackAmplifierPageViewModel()
        {
            Master = new TrackAmplifierMasterViewModel();
            Amplifiers = new ObservableCollection<TrackAmplifierVisualViewModel>();
            BackplaneSlaves = new ObservableCollection<TrackAmplifierVisualViewModel>();

            // Refresh the view at a fixed 2 Hz rate, independent of the Modbus data rate.
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _refreshTimer.Tick += RefreshFromCoreModel;
            _refreshTimer.Start();
        }

        #endregion

        #region Private helpers

        /// <summary>
        /// Periodically called by the dispatcher timer to copy data from
        /// the core model into the view models.
        /// 
        /// This runs on the UI thread, so we keep it simple and reasonably cheap.
        /// </summary>
        private void RefreshFromCoreModel(object? sender, EventArgs e)
        {
            var app = IoC.siebwaldeApplicationModel;
            if (app == null)
            {
                // Application model not available yet.
                return;
            }

            var amplifiers = app.TrackAmplifiers;
            if (amplifiers == null || amplifiers.Count == 0)
            {
                // Track application not started or no slaves seen yet.
                Master.UpdateFromModels(null, null);
                Amplifiers.Clear();
                BackplaneSlaves.Clear();
                return;
            }

            // Split into main amplifiers (1..50) and backplane slaves (51..55).
            var mainAmps = amplifiers
                .Where(a => a.SlaveNumber >= 1 && a.SlaveNumber <= 50)
                .OrderBy(a => a.SlaveNumber)
                .ToList();

            var backplane = amplifiers
                .Where(a => a.SlaveNumber >= 51 && a.SlaveNumber <= 55)
                .OrderBy(a => a.SlaveNumber)
                .ToList();

            SyncVisualCollection(Amplifiers, mainAmps);
            SyncVisualCollection(BackplaneSlaves, backplane);

            Master.UpdateFromModels(mainAmps, backplane);
        }

        /// <summary>
        /// Keeps an ObservableCollection of visual view models in sync with a list
        /// of core TrackAmplifierItem instances.
        /// 
        /// The mapping is based on SlaveNumber and the collection order.
        /// </summary>
        private static void SyncVisualCollection(
            ObservableCollection<TrackAmplifierVisualViewModel> target,
            IList<TrackAmplifierItem> source)
        {
            if (source == null)
            {
                target.Clear();
                return;
            }

            // If the count changed, rebuild the collection in one go.
            if (target.Count != source.Count)
            {
                target.Clear();
                foreach (var model in source)
                {
                    var vm = new TrackAmplifierVisualViewModel(model.SlaveNumber);
                    vm.UpdateFromModel(model);
                    target.Add(vm);
                }
                return;
            }

            // Same number of items: update in-place and verify ordering.
            for (int i = 0; i < source.Count; i++)
            {
                var model = source[i];
                var vm = target[i];

                if (vm.SlaveNumber != model.SlaveNumber)
                {
                    // If the order changed, rebuild to keep things simple and robust.
                    target.Clear();
                    foreach (var m in source)
                    {
                        var newVm = new TrackAmplifierVisualViewModel(m.SlaveNumber);
                        newVm.UpdateFromModel(m);
                        target.Add(newVm);
                    }
                    return;
                }

                vm.UpdateFromModel(model);
            }
        }

        #endregion
    }

    /// <summary>
    /// Simple visual view model for a single slave (track amplifier or backplane slave).
    /// Wraps TrackAmplifierItem and exposes only the data needed for the page.
    /// </summary>
    public class TrackAmplifierVisualViewModel : BaseViewModel
    {
        #region Private fields

        private ushort _slaveNumber;
        private bool _isDetected;

        private ushort _mbReceiveCounter;
        private ushort _mbSentCounter;
        private uint _mbCommError;
        private ushort _mbExceptionCode;
        private ushort _spiCommErrorCounter;

        private string _mbSummary = string.Empty;
        private string _holdingRegSummary = string.Empty;
        private string _allHoldingRegsSummary = string.Empty;

        private int _pwmSetPoint;
        private bool _isEmStop;
        private bool _isOccupied;

        #endregion

        #region Public properties

        /// <summary>
        /// Modbus slave number (1..50 for amplifiers, 51..55 for backplane slaves).
        /// </summary>
        public ushort SlaveNumber
        {
            get => _slaveNumber;
            private set
            {
                if (_slaveNumber != value)
                {
                    _slaveNumber = value;
                    OnPropertyChanged(nameof(SlaveNumber));
                }
            }
        }

        /// <summary>
        /// True if the slave is detected (SlaveDetected != 0).
        /// Used to drive the green/red LED in the UI.
        /// </summary>
        public bool IsDetected
        {
            get => _isDetected;
            private set
            {
                if (_isDetected != value)
                {
                    _isDetected = value;
                    OnPropertyChanged(nameof(IsDetected));
                }
            }
        }

        public ushort MbReceiveCounter
        {
            get => _mbReceiveCounter;
            private set
            {
                if (_mbReceiveCounter != value)
                {
                    _mbReceiveCounter = value;
                    OnPropertyChanged(nameof(MbReceiveCounter));
                    UpdateMbSummary();
                }
            }
        }

        public ushort MbSentCounter
        {
            get => _mbSentCounter;
            private set
            {
                if (_mbSentCounter != value)
                {
                    _mbSentCounter = value;
                    OnPropertyChanged(nameof(MbSentCounter));
                    UpdateMbSummary();
                }
            }
        }

        public uint MbCommError
        {
            get => _mbCommError;
            private set
            {
                if (_mbCommError != value)
                {
                    _mbCommError = value;
                    OnPropertyChanged(nameof(MbCommError));
                    UpdateMbSummary();
                }
            }
        }

        public ushort MbExceptionCode
        {
            get => _mbExceptionCode;
            private set
            {
                if (_mbExceptionCode != value)
                {
                    _mbExceptionCode = value;
                    OnPropertyChanged(nameof(MbExceptionCode));
                    UpdateMbSummary();
                }
            }
        }

        public ushort SpiCommErrorCounter
        {
            get => _spiCommErrorCounter;
            private set
            {
                if (_spiCommErrorCounter != value)
                {
                    _spiCommErrorCounter = value;
                    OnPropertyChanged(nameof(SpiCommErrorCounter));
                    UpdateMbSummary();
                }
            }
        }

        /// <summary>
        /// Short, human readable summary of the Modbus counters.
        /// Shown as text inside the amplifier box.
        /// </summary>
        public string MbSummary
        {
            get => _mbSummary;
            private set
            {
                if (_mbSummary != value)
                {
                    _mbSummary = value;
                    OnPropertyChanged(nameof(MbSummary));
                }
            }
        }

        /// <summary>
        /// Short summary of the first few holding registers
        /// (for the collapsed view).
        /// </summary>
        public string HoldingRegSummary
        {
            get => _holdingRegSummary;
            private set
            {
                if (_holdingRegSummary != value)
                {
                    _holdingRegSummary = value;
                    OnPropertyChanged(nameof(HoldingRegSummary));
                }
            }
        }

        /// <summary>
        /// Summary of all holding registers (R00=..., R01=..., ...).
        /// Used in the expanded view.
        /// </summary>
        public string AllHoldingRegsSummary
        {
            get => _allHoldingRegsSummary;
            private set
            {
                if (_allHoldingRegsSummary != value)
                {
                    _allHoldingRegsSummary = value;
                    OnPropertyChanged(nameof(AllHoldingRegsSummary));
                }
            }
        }

        /// <summary>
        /// PWM set point (0..799). Default idle is 400.
        /// Changing this property will update the corresponding holding register
        /// via the core application model.
        /// </summary>
        public int PwmSetPoint
        {
            get => _pwmSetPoint;
            set
            {
                // Clamp to allowed range
                int clamped = Math.Max(0, Math.Min(799, value));
                if (_pwmSetPoint == clamped)
                    return;

                _pwmSetPoint = clamped;
                OnPropertyChanged(nameof(PwmSetPoint));

                // Inform core model so it can update HoldingReg[0] bits 0..9
                IoC.siebwaldeApplicationModel?.SetAmplifierPwm(SlaveNumber, _pwmSetPoint);
            }
        }

        /// <summary>
        /// Emergency stop flag. When true, the amplifier should float the H-bridge
        /// (no power to the rails). This maps to HoldingReg0 bit 15.
        /// </summary>
        public bool IsEmStop
        {
            get => _isEmStop;
            set
            {
                if (_isEmStop == value)
                    return;

                _isEmStop = value;
                OnPropertyChanged(nameof(IsEmStop));

                IoC.siebwaldeApplicationModel?.SetAmplifierEmStop(SlaveNumber, _isEmStop);
            }
        }

        /// <summary>
        /// True when the amplifier detects an occupancy on the track
        /// (HoldingReg2 bit 10). Read-only from the UI perspective.
        /// </summary>
        public bool IsOccupied
        {
            get => _isOccupied;
            private set
            {
                if (_isOccupied == value)
                    return;

                _isOccupied = value;
                OnPropertyChanged(nameof(IsOccupied));
            }
        }

        #endregion

        #region Constructor

        public TrackAmplifierVisualViewModel(ushort slaveNumber)
        {
            SlaveNumber = slaveNumber;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Copies the latest values from a core TrackAmplifierItem
        /// into this view model.
        /// 
        /// Note: this method updates backing fields directly to avoid
        /// triggering core write-back logic when we are just reflecting
        /// current state from the model.
        /// </summary>
        public void UpdateFromModel(TrackAmplifierItem model)
        {
            if (model == null)
                return;

            SlaveNumber = model.SlaveNumber;
            IsDetected = model.SlaveDetected != 0;

            MbReceiveCounter = model.MbReceiveCounter;
            MbSentCounter = model.MbSentCounter;
            MbCommError = model.MbCommError;
            MbExceptionCode = model.MbExceptionCode;
            SpiCommErrorCounter = model.SpiCommErrorCounter;

            var regs = model.HoldingReg;

            // Decode PWM/EmStop/Occupied from holding registers
            if (regs != null && regs.Length > 0)
            {
                ushort reg0 = regs[0];

                int pwm = reg0 & 0x03FF;          // bits 0..9
                bool emStop = (reg0 & 0x8000) != 0; // bit 15

                if (_pwmSetPoint != pwm)
                {
                    _pwmSetPoint = pwm;
                    OnPropertyChanged(nameof(PwmSetPoint));
                }

                if (_isEmStop != emStop)
                {
                    _isEmStop = emStop;
                    OnPropertyChanged(nameof(IsEmStop));
                }

                bool occupied = false;
                if (regs.Length > 2)
                {
                    ushort reg2 = regs[2];
                    occupied = (reg2 & (1 << 10)) != 0;
                }

                IsOccupied = occupied;

                // Short summary (first 4 registers)
                int shortCount = Math.Min(4, regs.Length);
                if (shortCount > 0)
                {
                    var parts = new string[shortCount];
                    for (int i = 0; i < shortCount; i++)
                        parts[i] = regs[i].ToString();

                    HoldingRegSummary = $"Regs: {string.Join(", ", parts)}";
                }
                else
                {
                    HoldingRegSummary = string.Empty;
                }

                // Full summary for expanded view
                var allParts = new string[regs.Length];
                for (int i = 0; i < regs.Length; i++)
                {
                    allParts[i] = $"R{i:D2}={regs[i]}";
                }
                AllHoldingRegsSummary = string.Join("  ", allParts);
            }
            else
            {
                HoldingRegSummary = string.Empty;
                AllHoldingRegsSummary = string.Empty;
                IsOccupied = false;
            }
        }

        #endregion

        #region Private helpers

        private void UpdateMbSummary()
        {
            MbSummary = $"Rx {MbReceiveCounter}, Tx {MbSentCounter}, " +
                        $"Err {MbCommError}, Ex {MbExceptionCode}, Spi {SpiCommErrorCounter}";
        }

        #endregion
    }

    /// <summary>
    /// Simple view model for the Modbus master status box.
    /// Aggregates some statistics from the slaves.
    /// </summary>
    public class TrackAmplifierMasterViewModel : BaseViewModel
    {
        #region Private fields

        private bool _isConnected;
        private int _detectedAmplifiers;
        private int _detectedBackplaneSlaves;

        #endregion

        #region Public properties

        /// <summary>
        /// Indicates if the master is considered "connected".
        /// For now this simply means: we see at least one slave.
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        /// <summary>
        /// Number of detected track amplifiers (1..50).
        /// </summary>
        public int DetectedAmplifiers
        {
            get => _detectedAmplifiers;
            private set
            {
                if (_detectedAmplifiers != value)
                {
                    _detectedAmplifiers = value;
                    OnPropertyChanged(nameof(DetectedAmplifiers));
                    OnPropertyChanged(nameof(SummaryText));
                }
            }
        }

        /// <summary>
        /// Number of detected backplane slaves (51..55).
        /// </summary>
        public int DetectedBackplaneSlaves
        {
            get => _detectedBackplaneSlaves;
            private set
            {
                if (_detectedBackplaneSlaves != value)
                {
                    _detectedBackplaneSlaves = value;
                    OnPropertyChanged(nameof(DetectedBackplaneSlaves));
                    OnPropertyChanged(nameof(SummaryText));
                }
            }
        }

        /// <summary>
        /// Short status text used in the master box.
        /// </summary>
        public string StatusText => IsConnected ? "Online" : "Idle";

        /// <summary>
        /// Combined summary text used in the master box.
        /// </summary>
        public string SummaryText =>
            $"Amps: {DetectedAmplifiers}, BP: {DetectedBackplaneSlaves}";

        #endregion

        #region Public methods

        /// <summary>
        /// Updates the master status based on the currently known
        /// amplifiers and backplane slaves.
        /// </summary>
        public void UpdateFromModels(
            IList<TrackAmplifierItem>? amplifiers,
            IList<TrackAmplifierItem>? backplaneSlaves)
        {
            amplifiers ??= Array.Empty<TrackAmplifierItem>();
            backplaneSlaves ??= Array.Empty<TrackAmplifierItem>();

            IsConnected = amplifiers.Count > 0 || backplaneSlaves.Count > 0;

            DetectedAmplifiers = amplifiers.Count(a => a.SlaveDetected != 0);
            DetectedBackplaneSlaves = backplaneSlaves.Count(a => a.SlaveDetected != 0);
        }

        #endregion
    }
}
