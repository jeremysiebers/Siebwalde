using System;
using System.ComponentModel;
using System.Windows.Input;
using PropertyChanged;

namespace Siebwalde_Application
{
    /// <summary>
    /// A view model for the main Track view
    /// </summary>
    public class SiebwaldeControlViewModel : BaseViewModel
    {
        #region Private Members

        #endregion

        #region Public properties

        /// <summary>
        /// The current page of the application
        /// </summary>
        public ApplicationPage CurrentPage { get; set; } = ApplicationPage.TrackControlView;

        #endregion

        #region Dockpanel FILE public properties



        #endregion

        #region Dockpanel VIEW public properties

        public ICommand FileAutoModePage { get; set; }
        public ICommand FileManualModePage { get; set; }

        public bool FileAutoModeSelected { get; set; } = true;
        public bool FileManualModeSelected { get; set; }

        /// <summary>
        /// The command to switch to the manual mode for track amplifiers page
        /// </summary>
        public ICommand TrackAmpExpertModePage { get; set; }

        public bool TrackAmpExpertModeSelected { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public SiebwaldeControlViewModel()
        {
            // Create Commands
            FileAutoModePage = new RelayCommand(SwitchToTrackControlAutoModePage);
            FileManualModePage = new RelayCommand(SwitchToTrackControlManModePage);
            TrackAmpExpertModePage = new RelayCommand(SwitchToTrackAmpExpertModePage);
        }

        #endregion
               
        #region Dockpanel FILE Private methods

        private void SwitchToTrackControlAutoModePage()
        {
            IoC.SiebwaldeMain.CurrentPage = ApplicationPage.TrackControlView;
            FileAutoModeSelected = true;
            FileManualModeSelected = false;
            TrackAmpExpertModeSelected = false;
        }
        private void SwitchToTrackControlManModePage()
        {
            IoC.SiebwaldeMain.CurrentPage = ApplicationPage.TrackControlView;
            FileAutoModeSelected = false;
            FileManualModeSelected = true;
            TrackAmpExpertModeSelected = false;
        }

        #endregion

        #region Dockpanel VIEW Private methods

        private void SwitchToTrackAmpExpertModePage()
        {
            IoC.SiebwaldeMain.CurrentPage = ApplicationPage.TrackAmplifierManualControlView;
            FileAutoModeSelected = false;
            FileManualModeSelected = false;
            TrackAmpExpertModeSelected = true;
        }

        #endregion
    }
}
