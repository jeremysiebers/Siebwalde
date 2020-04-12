using System.Windows;
using System.Windows.Controls;

namespace Siebwalde_Application
{ 
    /// <summary>
    /// Interaction logic for HmiTrackControl.xaml
    /// </summary>
    public partial class SiebwaldeControl : Window
    {
        private Main mMain;

        public SiebwaldeControl(Main main)
        {
            mMain = main;

            InitializeComponent();

            // Get the Viewmodel that is in the IoC
            DataContext = IoC.SiebwaldeMain;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mMain.Close();
        }
    }
}
