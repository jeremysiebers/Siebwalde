using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace SiebwaldeApp
{
    /// <summary>
    /// Minimal DEV view model for the Simulation tab. It reads the running track-control runtime
    /// (via <see cref="IoC.TrackRuntime"/>) and exposes the active profile, detected slaves, per-
    /// section occupancy, runtime state, movement permission and the movement simulator's loco
    /// positions. It also offers minimal controlled I/O: a per-section occupancy toggle and simple
    /// place/drive/stop controls for the movement simulator.
    /// </summary>
    public class TrackSimulationPageViewModel : BaseViewModel
    {
        private readonly ITrackApplicationRuntime _runtime;
        private readonly DispatcherTimer _refreshTimer;

        private string _profileName = "—";
        private string _runtimeState = "—";
        private string _movementPermission = "—";
        private string _activeMode = "—";
        private string _detectedSlaves = "—";
        private string _movementSimulationAvailable = "No";

        private string _placeAddress = "1000";
        private string _placeBlock = "1";
        private string _driveAddress = "1000";
        private string _driveSpeed = "10";

        public TrackSimulationPageViewModel()
        {
            _runtime = IoC.TrackRuntime;

            PlaceLocoCommand = new RelayCommand(PlaceLoco);
            DriveLocoCommand = new RelayCommand(DriveLoco);
            StopAllCommand = new RelayCommand(StopAll);

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _refreshTimer.Tick += Refresh;
            _refreshTimer.Start();

            Refresh(this, EventArgs.Empty);
        }

        #region Status properties

        public string ProfileName
        {
            get => _profileName;
            private set { if (_profileName != value) { _profileName = value; OnPropertyChanged(nameof(ProfileName)); } }
        }

        public string RuntimeState
        {
            get => _runtimeState;
            private set { if (_runtimeState != value) { _runtimeState = value; OnPropertyChanged(nameof(RuntimeState)); } }
        }

        public string MovementPermission
        {
            get => _movementPermission;
            private set { if (_movementPermission != value) { _movementPermission = value; OnPropertyChanged(nameof(MovementPermission)); } }
        }

        public string ActiveMode
        {
            get => _activeMode;
            private set { if (_activeMode != value) { _activeMode = value; OnPropertyChanged(nameof(ActiveMode)); } }
        }

        public string DetectedSlaves
        {
            get => _detectedSlaves;
            private set { if (_detectedSlaves != value) { _detectedSlaves = value; OnPropertyChanged(nameof(DetectedSlaves)); } }
        }

        public string MovementSimulationAvailable
        {
            get => _movementSimulationAvailable;
            private set { if (_movementSimulationAvailable != value) { _movementSimulationAvailable = value; OnPropertyChanged(nameof(MovementSimulationAvailable)); } }
        }

        public string PlaceAddress
        {
            get => _placeAddress;
            set { if (_placeAddress != value) { _placeAddress = value; OnPropertyChanged(nameof(PlaceAddress)); } }
        }

        public string PlaceBlock
        {
            get => _placeBlock;
            set { if (_placeBlock != value) { _placeBlock = value; OnPropertyChanged(nameof(PlaceBlock)); } }
        }

        public string DriveAddress
        {
            get => _driveAddress;
            set { if (_driveAddress != value) { _driveAddress = value; OnPropertyChanged(nameof(DriveAddress)); } }
        }

        public string DriveSpeed
        {
            get => _driveSpeed;
            set { if (_driveSpeed != value) { _driveSpeed = value; OnPropertyChanged(nameof(DriveSpeed)); } }
        }

        #endregion

        #region Collections

        public ObservableCollection<SimulationSectionViewModel> Sections { get; } = new();

        public ObservableCollection<LocoPositionViewModel> Locos { get; } = new();

        #endregion

        #region Commands

        public ICommand PlaceLocoCommand { get; }

        public ICommand DriveLocoCommand { get; }

        public ICommand StopAllCommand { get; }

        #endregion

        #region Command handlers

        private void PlaceLoco()
        {
            if (int.TryParse(PlaceAddress, out var address) && int.TryParse(PlaceBlock, out var block))
            {
                _runtime.MovementSimulation?.PlaceLoco(address, block);
                Refresh(this, EventArgs.Empty);
            }
        }

        private void DriveLoco()
        {
            if (int.TryParse(DriveAddress, out var address) && int.TryParse(DriveSpeed, out var speed))
            {
                var clamped = Math.Clamp(speed, 0, AmplifierSpeedMapper.MaxEcosSpeed);
                // 0 = forward, non-zero = reverse (ECoS/hardware convention).
                _runtime.MovementSimulation?.SetLocoSpeed(address, clamped, direction: 0);
                Refresh(this, EventArgs.Empty);
            }
        }

        private void StopAll()
        {
            _runtime.MovementSimulation?.SetPower(on: false);
            Refresh(this, EventArgs.Empty);
        }

        #endregion

        #region Refresh

        private void Refresh(object? sender, EventArgs e)
        {
            ProfileName = _runtime.ActiveProfileName ?? "—";
            RuntimeState = _runtime.State.ToString();
            MovementPermission = _runtime.MovementPermission.ToString();
            ActiveMode = _runtime.ActiveEcosMode?.ToString() ?? "—";

            var io = _runtime.SimulatedTrackIo;
            var movement = _runtime.MovementSimulation;
            MovementSimulationAvailable = movement is not null ? "Yes" : "No";

            DetectedSlaves = io is null
                ? "—"
                : string.Join(", ", io.DetectedSlaves);

            SyncSections(io);
            SyncLocos(movement);
        }

        private void SyncSections(ISimulatedTrackIo? io)
        {
            var amplifiers = _runtime.TrackAmplifiers;

            if (io is null)
            {
                Sections.Clear();
                return;
            }

            var slaves = io.DetectedSlaves;
            if (Sections.Count != slaves.Count)
            {
                Sections.Clear();
                foreach (var slave in slaves)
                {
                    Sections.Add(new SimulationSectionViewModel(slave, io));
                }
            }

            foreach (var vm in Sections)
            {
                var item = amplifiers.FirstOrDefault(a => a.SlaveNumber == vm.SlaveNumber);
                vm.RefreshOccupancy(item);
            }
        }

        private void SyncLocos(IMovementSimulation? movement)
        {
            var positions = movement?.GetLocoPositions() ?? Array.Empty<LocoPosition>();

            if (Locos.Count != positions.Count)
            {
                Locos.Clear();
                foreach (var position in positions)
                {
                    Locos.Add(new LocoPositionViewModel(position));
                }

                return;
            }

            for (var i = 0; i < positions.Count; i++)
            {
                Locos[i].UpdateFrom(positions[i]);
            }
        }

        #endregion
    }

    /// <summary>Visual + I/O wrapper for one simulated amplifier section (detected slave).</summary>
    public class SimulationSectionViewModel : BaseViewModel
    {
        private readonly ISimulatedTrackIo _io;
        private bool _occupied;
        private bool _simulateOccupancy;
        private string _status = string.Empty;

        public SimulationSectionViewModel(byte slaveNumber, ISimulatedTrackIo io)
        {
            SlaveNumber = slaveNumber;
            _io = io;
        }

        public byte SlaveNumber { get; }

        public bool Occupied
        {
            get => _occupied;
            private set { if (_occupied != value) { _occupied = value; OnPropertyChanged(nameof(Occupied)); } }
        }

        public string Status
        {
            get => _status;
            private set { if (_status != value) { _status = value; OnPropertyChanged(nameof(Status)); } }
        }

        public bool SimulateOccupancy
        {
            get => _simulateOccupancy;
            set
            {
                if (_simulateOccupancy != value)
                {
                    _simulateOccupancy = value;
                    OnPropertyChanged(nameof(SimulateOccupancy));
                    _io.SetSlaveOccupancy(SlaveNumber, value);
                }
            }
        }

        public void RefreshOccupancy(TrackAmplifierItem? item)
        {
            var detected = item is not null && item.SlaveDetected != 0;
            Occupied = TrackAmplifierRegisters.IsOccupied(item?.HoldingReg);
            Status = detected ? $"amp {SlaveNumber} detected" : $"amp {SlaveNumber} not detected";
        }
    }

    /// <summary>Read-only visual wrapper for one simulated locomotive position.</summary>
    public class LocoPositionViewModel : BaseViewModel
    {
        private int _blockId;
        private int _sectionId;
        private double _offsetMm;
        private int _speedSteps;
        private int _direction;

        public LocoPositionViewModel(LocoPosition position)
        {
            Address = position.Address;
            UpdateFrom(position);
        }

        public int Address { get; }

        public int BlockId
        {
            get => _blockId;
            private set { if (_blockId != value) { _blockId = value; OnPropertyChanged(nameof(BlockId)); } }
        }

        public int SectionId
        {
            get => _sectionId;
            private set { if (_sectionId != value) { _sectionId = value; OnPropertyChanged(nameof(SectionId)); } }
        }

        public double OffsetMm
        {
            get => _offsetMm;
            private set { if (_offsetMm != value) { _offsetMm = value; OnPropertyChanged(nameof(OffsetMm)); } }
        }

        public int SpeedSteps
        {
            get => _speedSteps;
            private set { if (_speedSteps != value) { _speedSteps = value; OnPropertyChanged(nameof(SpeedSteps)); } }
        }

        public int Direction
        {
            get => _direction;
            private set { if (_direction != value) { _direction = value; OnPropertyChanged(nameof(Direction)); } }
        }

        public string Summary => $"block {BlockId}, section {SectionId}, {OffsetMm:F0} mm, speed {SpeedSteps}, dir {Direction}";

        public void UpdateFrom(LocoPosition position)
        {
            BlockId = position.BlockId;
            SectionId = position.SectionId;
            OffsetMm = position.OffsetMm;
            SpeedSteps = position.SpeedSteps;
            Direction = position.Direction;
            OnPropertyChanged(nameof(Summary));
        }
    }
}
