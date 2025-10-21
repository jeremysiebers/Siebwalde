// SiebwaldeApp.Core (same namespace as your other page VMs)
using Ninject;
using System.Windows.Input;

namespace SiebwaldeApp.Core
{
    public sealed class StationSettingsPageViewModel : BaseViewModel
    {
        public StationPolicy TopPolicy { get; }
        public StationPolicy BottomPolicy { get; }

        private readonly SiebwaldeApplicationModel _appModel;

        // Parameterless ctor required by BasePage<T>
        public StationSettingsPageViewModel()
            : this(
                IoC.Kernel.Get<StationPolicy>("TopStationPolicy"),
                IoC.Kernel.Get<StationPolicy>("BottomStationPolicy"),
                IoC.Kernel.Get<SiebwaldeApplicationModel>())
        {
        }

        // DI-friendly ctor (unit tests / manual composition)
        public StationSettingsPageViewModel(
            StationPolicy topPolicy,
            StationPolicy bottomPolicy,
            SiebwaldeApplicationModel appModel)
        {
            TopPolicy = topPolicy ?? new StationPolicy();
            BottomPolicy = bottomPolicy ?? new StationPolicy();
            _appModel = appModel ?? throw new ArgumentNullException(nameof(appModel));

            StartCommand = new RelayCommand(async _ => await _appModel.StartTrackApplication());
            StopCommand = new RelayCommand(_ => _appModel.StopTrackApplication());
        }

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
    }
}
