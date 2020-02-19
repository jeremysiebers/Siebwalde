using System.Windows.Controls;

namespace Siebwalde_Application.TrackApplication.View
{
    /// <summary>
    /// Interaction logic for HmiTrackControl.xaml
    /// </summary>
    public partial class HmiTrackControl : UserControl
    {
        /// <summary>
        /// Hold the TrackController instance
        /// </summary>
        private TrackController mTrackController;

        public HmiTrackControl(TrackController trackController)
        {
            mTrackController = trackController;

            InitializeComponent();
            DataContext = new HmiTrackControlViewModel(mTrackController);
        }
    }
}
