using System;
using System.Windows.Input;

namespace Siebwalde_Application
{
    /// <summary>
    /// A view model for the main Track view
    /// </summary>
    public class HmiTrackControlViewModel : BaseViewModel
    {
        #region Private Members

        #endregion

        #region Public properties

        /// <summary>
        /// The current page of the application
        /// </summary>
        public ApplicationPage CurrentPage { get; set; } = ApplicationPage.TrackControlView;

        /// <summary>
        /// The command to switch to the manual mode for track amplifiers page
        /// </summary>
        public ICommand TrackAmpManualMode { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="trackController"></param>
        public HmiTrackControlViewModel()
        {
            // Create Commands
            TrackAmpManualMode = new RelayCommand(SwitchToTrackAmpManualModePage);
        }

        #region Private methods

        private void SwitchToTrackAmpManualModePage()
        {
            IoC.Get<HmiTrackControlViewModel>().CurrentPage = ApplicationPage.TrackAmplifierManualControlView;
        }

        #endregion

        #endregion
    }
}
