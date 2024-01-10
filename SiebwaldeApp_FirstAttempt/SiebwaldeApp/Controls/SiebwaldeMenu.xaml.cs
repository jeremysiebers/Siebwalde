using System.Windows.Controls;
using SiebwaldeApp.Core;

namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for SiebwaldeMenu.xaml
    /// </summary>
    public partial class SiebwaldeMenu : UserControl
    {
        public SiebwaldeMenu()
        {
            InitializeComponent();

            DataContext = IoC.SideMenu;
        }
    }
}
