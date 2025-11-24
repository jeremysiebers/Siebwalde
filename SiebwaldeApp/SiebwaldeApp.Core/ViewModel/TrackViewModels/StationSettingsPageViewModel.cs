// SiebwaldeApp.Core (same namespace as your other page VMs)
using Ninject;

namespace SiebwaldeApp.Core
{
    public sealed class StationSettingsPageViewModel : BaseViewModel
    {
        public StationPolicy TopPolicy { get; }
        public StationPolicy BottomPolicy { get; }
        private readonly SiebwaldeApplicationModel _appModel;

        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }

        // Constructor injection — Ninject will resolve the named policies
        public StationSettingsPageViewModel(
        [Named("TopStationPolicy")] StationPolicy topPolicy,
        [Named("BottomStationPolicy")] StationPolicy bottomPolicy,
        SiebwaldeApplicationModel appModel)
        {
            TopPolicy = topPolicy ?? throw new ArgumentNullException(nameof(topPolicy));
            BottomPolicy = bottomPolicy ?? throw new ArgumentNullException(nameof(bottomPolicy));
            _appModel = appModel ?? throw new ArgumentNullException(nameof(appModel));

            StartCommand = new RelayCommand(async _ => await _appModel.StartTrackApplication());
            StopCommand = new RelayCommand(_ => _appModel.StopTrackApplication());
        }
    }
}
