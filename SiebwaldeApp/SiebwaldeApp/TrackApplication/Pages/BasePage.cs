using System.Windows.Controls;

namespace Siebwalde_Application
{
    /// <summary>
    /// A base page for all pages to gain base functionality
    /// </summary>
    /// <typeparam name="VM"></typeparam>
    public class BasePage<T> : Page
        where T : BaseViewModel, new()
    {
        #region Private Member

        /// <summary>
        /// The View Model associated wth this page
        /// </summary>
        private T mViewModel;

        #endregion

        #region Public properties

        /// <summary>
        /// The View Model associated with this page
        /// </summary>
        public T ViewModel
        {
            get { return mViewModel; }
            set
            {
                // If nothing has changed, return
                if (mViewModel == value)
                    return;

                // Update the value
                mViewModel = value;

                // Set the data context for this page
                this.DataContext = mViewModel;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasePage()
        {
            // Create a default View Model
            this.ViewModel = new T();
        }

        #endregion

       
    }
}
