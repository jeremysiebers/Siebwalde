using System.Windows.Controls;

namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for SiebwaldeMenu.xaml
    /// </summary>
    public partial class TrackMenu : UserControl
    {
        public TrackMenu()
        {
            InitializeComponent();

            DataContext = IoC.SideMenu;
        }
    }
}
