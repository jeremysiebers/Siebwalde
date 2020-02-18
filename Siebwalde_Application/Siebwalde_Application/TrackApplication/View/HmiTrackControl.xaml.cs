using System;
using System.ComponentModel;
using System.Windows;
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

        private TrackAmplifierItemViewModel trackAmplifierItemViewModel;

        public HmiTrackControl(TrackController trackController)
        {
            mTrackController = trackController;

            UInt16[] HoldingRegInit = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            InitializeComponent();

            trackAmplifierItemViewModel = new TrackAmplifierItemViewModel(mTrackController, 0, "", HoldingRegInit, 0, 0, 0, 0, 0);

            this.DataContext = trackAmplifierItemViewModel;
        }

        void DataWindow_Closing(object sender, RoutedEventArgs e)
        {
            trackAmplifierItemViewModel.OnWindowClosing();
        }
    }
}
