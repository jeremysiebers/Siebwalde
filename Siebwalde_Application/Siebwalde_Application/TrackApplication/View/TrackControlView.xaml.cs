using System.Windows.Controls;

namespace Siebwalde_Application
{
    /// <summary>
    /// Interaction logic for TrackControlView.xaml
    /// </summary>
    public partial class TrackControlView : UserControl
    {
        public TrackControlView()
        {
            InitializeComponent();
            DataContext = new TrackControlViewModel();
        }
    }
}
