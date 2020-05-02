// https://youtu.be/w5kAUCFDRy4?t=5100

namespace SiebwaldeApp
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
        public static ApplicationViewModel ApplicationViewModel => IoC.Application;

        /// <summary>
        /// The menu view model
        /// </summary>
        public static SideMenuViewModel SideMenuViewModel => IoC.SideMenu;

        #endregion
    }
}
