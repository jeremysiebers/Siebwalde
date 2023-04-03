using System.Windows.Controls;

namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for SiebwaldeMenu.xaml
    /// </summary>
    public partial class FiddleYardMenu : UserControl
    {
        public FiddleYardMenu()
        {
            InitializeComponent();

            DataContext = IoC.SideMenu;
        }
    }
}
