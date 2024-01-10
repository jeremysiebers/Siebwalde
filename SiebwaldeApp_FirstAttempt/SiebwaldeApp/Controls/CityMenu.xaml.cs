using SiebwaldeApp.Core;
using System.Windows.Controls;

namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for SiebwaldeMenu.xaml
    /// </summary>
    public partial class CityMenu : UserControl
    {
        public CityMenu()
        {
            InitializeComponent();

            DataContext = IoC.SideMenu;
        }
    }
}
