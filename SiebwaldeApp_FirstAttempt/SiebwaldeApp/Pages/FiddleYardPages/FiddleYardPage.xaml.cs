using SiebwaldeApp.Core;

namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for SiebwaldePage.xaml
    /// </summary>
    public partial class FiddleYardPage : BasePage<FiddleYardPageViewModel>
    {
        public FiddleYardPage()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            //if(IoC.siebwaldeApplicationModel.FYcontroller == null)
            //{
            //    return;
            //}
            //IoC.siebwaldeApplicationModel.FYcontroller.FYTOPShow(false, 1010, 1948, 0, 0, true);
            //IoC.siebwaldeApplicationModel.FYcontroller.FYBOTShow(false, 1010, 1948, 0, 0, true);
        }
    }
}
