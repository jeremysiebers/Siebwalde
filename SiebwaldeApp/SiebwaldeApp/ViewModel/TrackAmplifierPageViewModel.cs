using System.Windows.Input;

namespace SiebwaldeApp
{
    /// <summary
    /// 
    /// <summary
    public class TrackAmplifierPageViewModel : BaseViewModel
    {
        #region Private members

        #endregion

        #region Public properties
        
        /// <summary>
        /// The command to show the TrackInitPage
        /// </summary>
        public ICommand TrackMainPage { get; set; }

        /// <summary>
        /// The command to show the TrackInitPage
        /// </summary>
        public ICommand TrackInitPage { get; set; }

        /// <summary>
        /// The command to show the TrackInitPage
        /// </summary>
        public ICommand TrackAmplifierPage { get; set; }

        /// <summary>
        /// The command to show the TrackInitPage
        /// </summary>
        public ICommand TrackSettingsPage { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// <summary>
        public TrackAmplifierPageViewModel()
        {
            TrackMainPage = new RelayCommand(() => IoC.Get<ApplicationViewModel>().CurrentPage = ApplicationPage.TrackControl);
            TrackInitPage = new RelayCommand(() => IoC.Get<ApplicationViewModel>().CurrentPage = ApplicationPage.TrackControl);
            TrackAmplifierPage = new RelayCommand(() => IoC.Get<ApplicationViewModel>().CurrentPage = ApplicationPage.TrackAmplifier);
            TrackSettingsPage = new RelayCommand(() => IoC.Get<ApplicationViewModel>().CurrentPage = ApplicationPage.TrackControl);
        }

        #endregion
    }
}
