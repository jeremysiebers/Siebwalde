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
        public HmiTrackControl(Main main)
        {
            UInt16[] HoldingRegInit = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            InitializeComponent();
            this.DataContext = new TrackAmplifierItemViewModel(main, 0, "", HoldingRegInit, 0, 0, 0, 0, 0);
        }
    }
}
