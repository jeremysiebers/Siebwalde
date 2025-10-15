using SiebwaldeApp.Core;

// Alias to make WPF types explicit (avoids clash with WinForms)
using WpfControls = System.Windows.Controls;

namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for CityMenu.xaml
    /// </summary>
    public partial class FiddleYardMenu : WpfControls.UserControl
    {
        public FiddleYardMenu()
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
//    public partial class FiddleYardMenu : UserControl
//    {
//        public FiddleYardMenu()
//        {
//            InitializeComponent();

//            DataContext = IoC.SideMenu;
//        }
//    }
//}
