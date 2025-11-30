using SiebwaldeApp.Core;
using System;
using System.Windows.Input;

namespace SiebwaldeApp
{
    /// <summary
    /// The application state as a view model
    /// <summary
    public class ApplicationViewModel : BaseViewModel    {

        #region Commands
                
        /// <summary>
        /// The command to show the system menu of the window
        /// </summary>
        public ICommand SiebwaldeApplicationPage { get; set; }

        /// <summary>
        /// The command to show the system menu of the window
        /// </summary>
        public ICommand SiebwaldeTrackControlPage { get; set; }

        /// <summary>
        /// The command to show the system menu of the window
        /// </summary>
        public ICommand SiebwaldeFiddleYardControlPage { get; set; }

        /// <summary>
        /// The command to show the system menu of the window
        /// </summary>
        public ICommand SiebwaldeYardControlPage { get; set; }

        /// <summary>
        /// The command to show the system menu of the window
        /// </summary>
        public ICommand SiebwaldeCityControlPage { get; set; }

        #endregion

        #region Public properties

        public ApplicationPage CurrentPage { get; set; } = ApplicationPage.Siebwalde;

        #endregion

        #region Private Methods

        /// <summary>
        /// Method to load the Siebwalde page and menu
        /// </summary>
        private void LoadSiebwaldeApplicationPage()
        {
            // Load Siebwalde page
            IoC.Application.CurrentPage = ApplicationPage.Siebwalde;
            // Load Siebwalde menu
            IoC.SideMenu.CurrentMenu = SideMenuPage.Siebwalde;
        }

        /// <summary>
        /// Method to load the Track control page and menu
        /// </summary>
        private void LoadSiebwaldeTrackControlPage()
        {
            // Load Siebwalde page
            IoC.Application.CurrentPage = ApplicationPage.TrackControl;
            // Load Siebwalde menu
            IoC.SideMenu.CurrentMenu = SideMenuPage.TrackControl;
        }

        /// <summary>
        /// Method to load the Fiddle Yard control page and menu
        /// </summary>
        private void LoadSiebwaldeFiddleYardControlPage()
        {
            // Load Siebwalde page
            IoC.Application.CurrentPage = ApplicationPage.FiddleYardControl;
            // Load Siebwalde menu
            IoC.SideMenu.CurrentMenu = SideMenuPage.FiddleYardControl;
            // Show the Fiddle Yard Winforms
            IoC.siebwaldeApplicationModel.OnFiddleYardShowWinForm(EventArgs.Empty);
        }

        /// <summary>
        /// Method to load the Yard control page and menu
        /// </summary>
        private void LoadSiebwaldeYardControlPage()
        {
            // Load Siebwalde page
            IoC.Application.CurrentPage = ApplicationPage.YardControl;
            // Load Siebwalde menu
            IoC.SideMenu.CurrentMenu = SideMenuPage.YardControl;
        }

        /// <summary>
        /// Method to load the City control page and menu
        /// </summary>
        private void LoadSiebwaldeCityControlPage()
        {
            // Load Siebwalde page
            IoC.Application.CurrentPage = ApplicationPage.CityControl;
            // Load Siebwalde menu
            IoC.SideMenu.CurrentMenu = SideMenuPage.CityControl;
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public ApplicationViewModel()
        {
            SiebwaldeApplicationPage = new RelayCommand(LoadSiebwaldeApplicationPage);
            SiebwaldeTrackControlPage = new RelayCommand(LoadSiebwaldeTrackControlPage);
            SiebwaldeFiddleYardControlPage = new RelayCommand(LoadSiebwaldeFiddleYardControlPage);
            SiebwaldeYardControlPage = new RelayCommand(LoadSiebwaldeYardControlPage);
            SiebwaldeCityControlPage = new RelayCommand(LoadSiebwaldeCityControlPage);
        }
    }
}
