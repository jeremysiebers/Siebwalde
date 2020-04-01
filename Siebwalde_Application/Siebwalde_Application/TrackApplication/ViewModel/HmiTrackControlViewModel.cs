namespace Siebwalde_Application
{
    /// <summary>
    /// A view model for the main Track view
    /// </summary>
    public class HmiTrackControlViewModel : BaseViewModel
    {
        #region Local Variables

        /// <summary>
        /// The window this view model controls
        /// </summary>
        private HmiTrackControl mWindow;

        #endregion

        #region Properties

        /// <summary>
        /// The current page of the application
        /// </summary>
        public ApplicationPage CurrentPage { get; set; } = ApplicationPage.StartPage;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="trackController"></param>
        public HmiTrackControlViewModel(HmiTrackControl hmiTrackControl)
        {
            mWindow = hmiTrackControl;
        }

        #endregion
    }
}
