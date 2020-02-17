using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
    }
}
