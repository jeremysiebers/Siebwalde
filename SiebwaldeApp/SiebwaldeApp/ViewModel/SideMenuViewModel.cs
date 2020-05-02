using System.Windows.Input;

namespace SiebwaldeApp
{
    /// <summary
    /// 
    /// <summary
    public class SideMenuViewModel : BaseViewModel    {

        #region Public properties

        public SideMenuPage CurrentMenu { get; set; }

        #endregion

        #region Public SiebwaldeMenu properties
               
        /// <summary>
        /// The command to show the TrackInitPage
        /// </summary>
        public ICommand SiebwaldeMainPage { get; set; }

        /// <summary>
        /// The command to show the TrackInitPage
        /// </summary>
        public ICommand SiebwaldeInitPage { get; set; }

        /// <summary>
        /// The command to show the TrackInitPage
        /// </summary>
        public ICommand SiebwaldeSettingsPage { get; set; }

        #endregion

        #region Public TrackMenu properties

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
        public SideMenuViewModel()
        {
            #region TrackMenu commands
            TrackMainPage = new RelayCommand(() => IoC.Application.CurrentPage = ApplicationPage.TrackControl);
            TrackInitPage = new RelayCommand(() => IoC.Application.CurrentPage = ApplicationPage.TrackControl);
            TrackAmplifierPage = new RelayCommand(() => IoC.Application.CurrentPage = ApplicationPage.TrackAmplifier);
            TrackSettingsPage = new RelayCommand(() => IoC.Application.CurrentPage = ApplicationPage.TrackControl);
            #endregion

            #region SiebwaldeMenu commands
            SiebwaldeMainPage = new RelayCommand(() => IoC.Application.CurrentPage = ApplicationPage.Siebwalde);
            SiebwaldeInitPage = new RelayCommand(() => IoC.Application.CurrentPage = ApplicationPage.SiebwaldeInit);
            SiebwaldeSettingsPage = new RelayCommand(() => IoC.Application.CurrentPage = ApplicationPage.SiebwaldeSettings);
            #endregion
        }

        #endregion
    }
}
