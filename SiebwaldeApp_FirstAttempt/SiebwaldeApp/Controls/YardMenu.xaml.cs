using System.Windows.Controls;
using SiebwaldeApp.Core;

namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for SiebwaldeMenu.xaml
    /// </summary>
    public partial class YardMenu : UserControl
    {
        public YardMenu()
        {
            InitializeComponent();

            DataContext = IoC.SideMenu;
        }
    }
}
