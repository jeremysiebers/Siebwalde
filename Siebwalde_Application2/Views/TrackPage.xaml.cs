using Siebwalde_Application2.ViewModels;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Siebwalde_Application2.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TrackPage : Page
    {

        public TrackViewModel ViewModel { get; } = new TrackViewModel();

        public TrackPage()
        {
            this.InitializeComponent();
        }
    }
}
