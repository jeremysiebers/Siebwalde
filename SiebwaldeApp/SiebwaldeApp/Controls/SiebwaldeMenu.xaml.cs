using SiebwaldeApp.Core;

// Alias to make WPF types explicit (avoids clash with WinForms)
using WpfControls = System.Windows.Controls;

namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for SiebwaldeMenu.xaml
    /// </summary>
    public partial class SiebwaldeMenu : WpfControls.UserControl
    {
        public SiebwaldeMenu()
        {
            InitializeComponent();

            DataContext = IoC.SideMenu;
        }
    }
}
