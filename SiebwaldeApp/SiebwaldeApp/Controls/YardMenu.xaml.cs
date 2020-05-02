using System.Windows.Controls;

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
