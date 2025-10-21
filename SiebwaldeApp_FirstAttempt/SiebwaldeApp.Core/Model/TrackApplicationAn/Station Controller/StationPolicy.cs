using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Policy flags for one station side (Top or Bottom).
    /// Bind these from WPF to control passing/storage permissions on the fly.
    /// </summary>
    public sealed class StationPolicy : INotifyPropertyChanged
    {
        private bool _passengerPassingOnMiddle;
        private bool _freightPassingOnMiddle;

        // Storage permissions: which tracks may store passenger/freight
        private bool _passengerStorageOnFirst;   // Top: 10  | Bottom: 1
        private bool _passengerStorageOnMiddle;  // Top: 12  | Bottom: 3
        private bool _passengerStorageOnLast;    // Top: 11  | Bottom: 2

        private bool _freightStorageOnFirst;     // Top: 10  | Bottom: 1
        private bool _freightStorageOnMiddle;    // Top: 12  | Bottom: 3
        private bool _freightStorageOnLast;      // Top: 11  | Bottom: 2

        public event PropertyChangedEventHandler PropertyChanged;

        public bool PassengerPassingOnMiddle
        {
            get => _passengerPassingOnMiddle;
            set { if (_passengerPassingOnMiddle != value) { _passengerPassingOnMiddle = value; OnPropertyChanged(); } }
        }

        public bool FreightPassingOnMiddle
        {
            get => _freightPassingOnMiddle;
            set { if (_freightPassingOnMiddle != value) { _freightPassingOnMiddle = value; OnPropertyChanged(); } }
        }

        public bool PassengerStorageOnFirst
        {
            get => _passengerStorageOnFirst;
            set { if (_passengerStorageOnFirst != value) { _passengerStorageOnFirst = value; OnPropertyChanged(); } }
        }

        public bool PassengerStorageOnMiddle
        {
            get => _passengerStorageOnMiddle;
            set { if (_passengerStorageOnMiddle != value) { _passengerStorageOnMiddle = value; OnPropertyChanged(); } }
        }

        public bool PassengerStorageOnLast
        {
            get => _passengerStorageOnLast;
            set { if (_passengerStorageOnLast != value) { _passengerStorageOnLast = value; OnPropertyChanged(); } }
        }

        public bool FreightStorageOnFirst
        {
            get => _freightStorageOnFirst;
            set { if (_freightStorageOnFirst != value) { _freightStorageOnFirst = value; OnPropertyChanged(); } }
        }

        public bool FreightStorageOnMiddle
        {
            get => _freightStorageOnMiddle;
            set { if (_freightStorageOnMiddle != value) { _freightStorageOnMiddle = value; OnPropertyChanged(); } }
        }

        public bool FreightStorageOnLast
        {
            get => _freightStorageOnLast;
            set { if (_freightStorageOnLast != value) { _freightStorageOnLast = value; OnPropertyChanged(); } }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
