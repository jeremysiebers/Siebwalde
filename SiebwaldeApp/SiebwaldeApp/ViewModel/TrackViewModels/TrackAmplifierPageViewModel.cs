using SiebwaldeApp.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace SiebwaldeApp
{
    /// <summary>
    /// View model for the TrackAmplifierPage.
    /// 
    /// It exposes:
    /// - Master: summary of the Modbus master
    /// - Amplifiers: 1..50 amplifiers
    /// - BackplaneSlaves: 51..55 backplane modules
    /// 
    /// It also polls the SiebwaldeApplicationModel at 2 Hz to refresh
    /// the view from the core model.
    /// </summary>
    public class TrackAmplifierPageViewModel : BaseViewModel
    {
        #region Private members

        private readonly SiebwaldeApplicationModel _appModel;
        private readonly DispatcherTimer _refreshTimer;
        private bool _isExpanded;

        #endregion

        #region Public properties

        /// <summary>
        /// Global expand/collapse state for all amplifier items.
        /// When true, amplifier boxes show full details.
        /// When false, only key controls are visible.
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        /// <summary>
        /// Visual representation of the Modbus master.
        /// </summary>
        public TrackAmplifierMasterViewModel Master { get; }

        /// <summary>
        /// Visual representation of all track amplifiers (slave numbers 1..50).
        /// </summary>
        public ObservableCollection<TrackAmplifierVisualViewModel> Amplifiers { get; }

        /// <summary>
        /// Visual representation of the backplane slaves (slave numbers 51..55).
        /// </summary>
        public ObservableCollection<TrackAmplifierVisualViewModel> BackplaneSlaves { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TrackAmplifierPageViewModel()
        {
            _appModel = IoC.Get<SiebwaldeApplicationModel>();

            Master = new TrackAmplifierMasterViewModel();
            Amplifiers = new ObservableCollection<TrackAmplifierVisualViewModel>();
            BackplaneSlaves = new ObservableCollection<TrackAmplifierVisualViewModel>();

            // Refresh 2 times per second (2 Hz)
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _refreshTimer.Tick += RefreshFromCoreModel;
            _refreshTimer.Start();
        }

        #endregion

        #region Refresh from core model

        private void RefreshFromCoreModel(object? sender, EventArgs e)
        {
            var amplifiers = _appModel.GetAmplifierListing();
            if (amplifiers == null || amplifiers.Count == 0)
            {
                Master.UpdateFromModels(null, null);
                Amplifiers.Clear();
                BackplaneSlaves.Clear();
                return;
            }

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

        private static void SyncVisualCollection(
            ObservableCollection<TrackAmplifierVisualViewModel> target,
            System.Collections.Generic.IList<TrackAmplifierItem> source)
        {
            if (source == null)
            {
                target.Clear();
                return;
            }

            // Rebuild if count differs
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

            // Same count: update in-place, verify ordering
            for (int i = 0; i < source.Count; i++)
            {
                var model = source[i];
                var vm = target[i];

                if (vm.SlaveNumber != model.SlaveNumber)
                {
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
    /// Simple view model for a single amplifier/backplane slave.
    /// Wraps TrackAmplifierItem and exposes the properties needed for the page.
    /// It also pushes user changes (PWM setpoint, EmoStop) back into the core
    /// via SiebwaldeApplicationModel.SetAmplifierControl.
    /// </summary>
    public class TrackAmplifierVisualViewModel : BaseViewModel
    {
        #region Private fields

        private readonly SiebwaldeApplicationModel _appModel;

        private ushort _slaveNumber;
        private bool _isDetected;
        private bool _isOccupied;

        private ushort _mbReceiveCounter;
        private ushort _mbSentCounter;
        private uint _mbCommError;
        private ushort _mbExceptionCode;
        private ushort _spiCommErrorCounter;

        private string _holdingRegSummary = string.Empty;
        private string _mbSummary = string.Empty;

        // Key controls (compact view)
        private int _pwmSetpoint;
        private bool _emoStop;

        // Extended / decoded values (expanded view)
        private int _setBemfSpeed;
        private bool _setCsReg;
        private bool _clearAmpStatus;
        private bool _clearMessageBuffer;
        private bool _enableAmplifier;
        private int _readBackEmf;
        private bool _thermalFlag;
        private bool _overCurrent;
        private bool _amplifierIdSet;
        private ushort _amplifierStatus;
        private int _hBridgeFuseVoltage;
        private int _hBridgeTemperature;
        private int _hBridgeCurrent;
        private ushort _messagesReceived;
        private ushort _messagesSent;
        private int _amplifierId;
        private bool _singlePwmMode;
        private bool _resetSlave;
        private int _accelerationPar;
        private int _decelerationPar;
        private ushort _hexChecksum;

        private bool _suppressCoreUpdate;

        #endregion

        #region Public properties

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

        public bool IsOccupied
        {
            get => _isOccupied;
            private set
            {
                if (_isOccupied != value)
                {
                    _isOccupied = value;
                    OnPropertyChanged(nameof(IsOccupied));
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
        /// PWM setpoint 0..799, middle (400) is standstill in dual-sided PWM.
        /// </summary>
        public int PwmSetpoint
        {
            get => _pwmSetpoint;
            set
            {
                int clamped = value;
                if (clamped < 0) clamped = 0;
                if (clamped > 799) clamped = 799;

                if (_pwmSetpoint != clamped)
                {
                    _pwmSetpoint = clamped;
                    OnPropertyChanged(nameof(PwmSetpoint));

                    if (!_suppressCoreUpdate)
                    {
                        _appModel.SetAmplifierControl(SlaveNumber, _pwmSetpoint, _emoStop);
                    }
                }
            }
        }

        /// <summary>
        /// Emergency stop flag (HoldingReg0 bit 15).
        /// </summary>
        public bool EmoStop
        {
            get => _emoStop;
            set
            {
                if (_emoStop != value)
                {
                    _emoStop = value;
                    OnPropertyChanged(nameof(EmoStop));

                    if (!_suppressCoreUpdate)
                    {
                        _appModel.SetAmplifierControl(SlaveNumber, _pwmSetpoint, _emoStop);
                    }
                }
            }
        }

        // Extended properties for expanded view

        public int SetBemfSpeed
        {
            get => _setBemfSpeed;
            private set { if (_setBemfSpeed != value) { _setBemfSpeed = value; OnPropertyChanged(nameof(SetBemfSpeed)); } }
        }

        public bool SetCsReg
        {
            get => _setCsReg;
            private set { if (_setCsReg != value) { _setCsReg = value; OnPropertyChanged(nameof(SetCsReg)); } }
        }

        public bool ClearAmpStatus
        {
            get => _clearAmpStatus;
            private set { if (_clearAmpStatus != value) { _clearAmpStatus = value; OnPropertyChanged(nameof(ClearAmpStatus)); } }
        }

        public bool ClearMessageBuffer
        {
            get => _clearMessageBuffer;
            private set { if (_clearMessageBuffer != value) { _clearMessageBuffer = value; OnPropertyChanged(nameof(ClearMessageBuffer)); } }
        }

        public bool EnableAmplifier
        {
            get => _enableAmplifier;
            private set { if (_enableAmplifier != value) { _enableAmplifier = value; OnPropertyChanged(nameof(EnableAmplifier)); } }
        }

        public int ReadBackEmf
        {
            get => _readBackEmf;
            private set { if (_readBackEmf != value) { _readBackEmf = value; OnPropertyChanged(nameof(ReadBackEmf)); } }
        }

        public bool ThermalFlag
        {
            get => _thermalFlag;
            private set { if (_thermalFlag != value) { _thermalFlag = value; OnPropertyChanged(nameof(ThermalFlag)); } }
        }

        public bool OverCurrent
        {
            get => _overCurrent;
            private set { if (_overCurrent != value) { _overCurrent = value; OnPropertyChanged(nameof(OverCurrent)); } }
        }

        public bool AmplifierIdSet
        {
            get => _amplifierIdSet;
            private set { if (_amplifierIdSet != value) { _amplifierIdSet = value; OnPropertyChanged(nameof(AmplifierIdSet)); } }
        }

        public ushort AmplifierStatus
        {
            get => _amplifierStatus;
            private set { if (_amplifierStatus != value) { _amplifierStatus = value; OnPropertyChanged(nameof(AmplifierStatus)); } }
        }

        public int HBridgeFuseVoltage
        {
            get => _hBridgeFuseVoltage;
            private set { if (_hBridgeFuseVoltage != value) { _hBridgeFuseVoltage = value; OnPropertyChanged(nameof(HBridgeFuseVoltage)); } }
        }

        public int HBridgeTemperature
        {
            get => _hBridgeTemperature;
            private set { if (_hBridgeTemperature != value) { _hBridgeTemperature = value; OnPropertyChanged(nameof(HBridgeTemperature)); } }
        }

        public int HBridgeCurrent
        {
            get => _hBridgeCurrent;
            private set { if (_hBridgeCurrent != value) { _hBridgeCurrent = value; OnPropertyChanged(nameof(HBridgeCurrent)); } }
        }

        public ushort MessagesReceived
        {
            get => _messagesReceived;
            private set { if (_messagesReceived != value) { _messagesReceived = value; OnPropertyChanged(nameof(MessagesReceived)); } }
        }

        public ushort MessagesSent
        {
            get => _messagesSent;
            private set { if (_messagesSent != value) { _messagesSent = value; OnPropertyChanged(nameof(MessagesSent)); } }
        }

        public int AmplifierId
        {
            get => _amplifierId;
            private set { if (_amplifierId != value) { _amplifierId = value; OnPropertyChanged(nameof(AmplifierId)); } }
        }

        public bool SinglePwmMode
        {
            get => _singlePwmMode;
            private set { if (_singlePwmMode != value) { _singlePwmMode = value; OnPropertyChanged(nameof(SinglePwmMode)); } }
        }

        public bool ResetSlave
        {
            get => _resetSlave;
            private set { if (_resetSlave != value) { _resetSlave = value; OnPropertyChanged(nameof(ResetSlave)); } }
        }

        public int AccelerationPar
        {
            get => _accelerationPar;
            private set { if (_accelerationPar != value) { _accelerationPar = value; OnPropertyChanged(nameof(AccelerationPar)); } }
        }

        public int DecelerationPar
        {
            get => _decelerationPar;
            private set { if (_decelerationPar != value) { _decelerationPar = value; OnPropertyChanged(nameof(DecelerationPar)); } }
        }

        public ushort HexChecksum
        {
            get => _hexChecksum;
            private set { if (_hexChecksum != value) { _hexChecksum = value; OnPropertyChanged(nameof(HexChecksum)); } }
        }

        #endregion

        #region Constructor

        public TrackAmplifierVisualViewModel(ushort slaveNumber)
        {
            _appModel = IoC.Get<SiebwaldeApplicationModel>();
            SlaveNumber = slaveNumber;
            _pwmSetpoint = 400; // UI default for standstill (dual sided PWM)
        }

        #endregion

        #region Public methods

        public void UpdateFromModel(TrackAmplifierItem model)
        {
            if (model == null)
                return;

            _suppressCoreUpdate = true;
            try
            {
                SlaveNumber = model.SlaveNumber;
                IsDetected = model.SlaveDetected != 0;

                MbReceiveCounter = model.MbReceiveCounter;
                MbSentCounter = model.MbSentCounter;
                MbCommError = model.MbCommError;
                MbExceptionCode = model.MbExceptionCode;
                SpiCommErrorCounter = model.SpiCommErrorCounter;

                var regs = model.HoldingReg;

                if (regs != null && regs.Length > 0)
                {
                    int count = Math.Min(4, regs.Length);
                    var parts = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        parts[i] = regs[i].ToString();
                    }
                    HoldingRegSummary = $"Regs: {string.Join(", ", parts)}";
                }
                else
                {
                    HoldingRegSummary = string.Empty;
                }

                DecodeHoldingRegisters(regs);
            }
            finally
            {
                _suppressCoreUpdate = false;
            }
        }

        #endregion

        #region Private helpers

        private void UpdateMbSummary()
        {
            MbSummary = $"Rx {MbReceiveCounter}, Tx {MbSentCounter}, Err {MbCommError}, Ex {MbExceptionCode}, Spi {SpiCommErrorCounter}";
        }

        private void DecodeHoldingRegisters(ushort[]? regs)
        {
            ushort hr0 = GetReg(regs, 0);
            ushort hr1 = GetReg(regs, 1);
            ushort hr2 = GetReg(regs, 2);
            ushort hr3 = GetReg(regs, 3);
            ushort hr4 = GetReg(regs, 4);
            ushort hr5 = GetReg(regs, 5);
            ushort hr6 = GetReg(regs, 6);
            ushort hr7 = GetReg(regs, 7);
            ushort hr8 = GetReg(regs, 8);
            ushort hr9 = GetReg(regs, 9);
            ushort hr10 = GetReg(regs, 10);
            ushort hr11 = GetReg(regs, 11);

            // HoldingReg0
            int pwm = GetBits(hr0, 0, 9);
            if (pwm != 0)
            {
                _pwmSetpoint = pwm;
                OnPropertyChanged(nameof(PwmSetpoint));
            }
            _emoStop = HasBit(hr0, 15);
            OnPropertyChanged(nameof(EmoStop));

            // HoldingReg1
            SetBemfSpeed = GetBits(hr1, 0, 9);
            SetCsReg = HasBit(hr1, 10);
            ClearAmpStatus = HasBit(hr1, 11);
            ClearMessageBuffer = HasBit(hr1, 12);
            EnableAmplifier = HasBit(hr1, 15);

            // HoldingReg2
            ReadBackEmf = GetBits(hr2, 0, 9);
            IsOccupied = HasBit(hr2, 10);
            ThermalFlag = HasBit(hr2, 11);
            OverCurrent = HasBit(hr2, 12);
            AmplifierIdSet = HasBit(hr2, 13);

            // HoldingReg3
            AmplifierStatus = hr3;

            // HoldingReg4..6
            HBridgeFuseVoltage = GetBits(hr4, 0, 9);
            HBridgeTemperature = GetBits(hr5, 0, 9);
            HBridgeCurrent = GetBits(hr6, 0, 9);

            // HoldingReg7..8
            MessagesReceived = hr7;
            MessagesSent = hr8;

            // HoldingReg9
            AmplifierId = GetBits(hr9, 0, 5);
            SinglePwmMode = HasBit(hr9, 6);
            ResetSlave = HasBit(hr9, 15);

            // HoldingReg10
            AccelerationPar = GetBits(hr10, 0, 7);
            DecelerationPar = GetBits(hr10, 8, 15);

            // HoldingReg11
            HexChecksum = hr11;
        }

        private static ushort GetReg(ushort[]? regs, int index)
        {
            if (regs == null)
                return 0;
            if (index < 0 || index >= regs.Length)
                return 0;
            return regs[index];
        }

        private static bool HasBit(ushort value, int bitIndex)
        {
            return (value & (1 << bitIndex)) != 0;
        }

        private static int GetBits(ushort value, int fromBit, int toBitInclusive)
        {
            int width = toBitInclusive - fromBit + 1;
            int mask = ((1 << width) - 1) << fromBit;
            return (value & mask) >> fromBit;
        }

        #endregion
    }

    /// <summary>
    /// Simple view model for the Modbus master status box.
    /// </summary>
    public class TrackAmplifierMasterViewModel : BaseViewModel
    {
        private bool _isConnected;
        private int _detectedAmplifiers;
        private int _detectedBackplaneSlaves;

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

        public string StatusText => IsConnected ? "Online" : "Idle";

        public string SummaryText =>
            $"Amps: {DetectedAmplifiers}, BP: {DetectedBackplaneSlaves}";

        public void UpdateFromModels(
            System.Collections.Generic.IList<TrackAmplifierItem>? amplifiers,
            System.Collections.Generic.IList<TrackAmplifierItem>? backplaneSlaves)
        {
            amplifiers ??= Array.Empty<TrackAmplifierItem>();
            backplaneSlaves ??= Array.Empty<TrackAmplifierItem>();

            IsConnected = amplifiers.Count > 0 || backplaneSlaves.Count > 0;

            DetectedAmplifiers = amplifiers.Count(a => a.SlaveDetected != 0);
            DetectedBackplaneSlaves = backplaneSlaves.Count(a => a.SlaveDetected != 0);
        }
    }
}
