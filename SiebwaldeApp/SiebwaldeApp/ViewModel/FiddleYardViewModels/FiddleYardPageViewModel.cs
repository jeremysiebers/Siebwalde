using SiebwaldeApp.Core;
using System;
using System.Windows.Input;

namespace SiebwaldeApp
{
    /// <summary
    /// 
    /// <summary
    public class FiddleYardPageViewModel : BaseViewModel
    {
        #region Commands

        /// <summary>
        /// The command to show the system menu of the window
        /// </summary>
        public ICommand FiddleYardSettingsmenu { get; set; }

        #endregion

        #region Private members

        /// <summary>
        /// Method to load the Siebwalde page and menu
        /// </summary>
        private void LoadFiddleYardSettingsmenu()
        {
            // Show the Fiddle Yard Settings Winforms
            IoC.siebwaldeApplicationModel.OnFiddleYardSettingsWinForm(EventArgs.Empty);
        }

        #endregion

        #region Public properties

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// <summary>
        public FiddleYardPageViewModel()
        {
            FiddleYardSettingsmenu = new RelayCommand(LoadFiddleYardSettingsmenu);
        }
        #endregion
    }
}
