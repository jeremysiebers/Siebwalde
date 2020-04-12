// https://youtu.be/w5kAUCFDRy4?t=5100

namespace Siebwalde_Application
{
    /// <summary>
    /// Locates view models from the IoC for use in binding in Xaml files
    /// </summary>
    public class ViewModelLocator
    {
        #region Public properties

        /// <summary>
        /// Singleton instance of the locator
        /// </summary>
        public static ViewModelLocator Instance { get; private set; } = new ViewModelLocator();

        /// <summary>
        /// The application view model
        /// </summary>
        public static SiebwaldeControlViewModel HmiTrackControlViewModel => IoC.SiebwaldeMain;

        #endregion
    }
}
