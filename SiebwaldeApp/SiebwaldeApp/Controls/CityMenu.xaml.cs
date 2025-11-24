using SiebwaldeApp.Core;

// Alias to make WPF types explicit (avoids clash with WinForms)
using WpfControls = System.Windows.Controls;

namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for CityMenu.xaml
    /// </summary>
    public partial class CityMenu : WpfControls.UserControl
    {
        public CityMenu()
        {
            InitializeComponent();

            DataContext = IoC.SideMenu;
        }
    }
}

//using System.Windows.Controls;

//namespace SiebwaldeApp
//{
//    /// <summary>
//    /// Interaction logic for SiebwaldeMenu.xaml
//    /// </summary>
//    public partial class CityMenu : UserControl
//    {
//        public CityMenu()
//        {
//            InitializeComponent();

//            DataContext = IoC.SideMenu;
//        }
//    }
//}
