using System.Windows.Forms;

namespace Siebwalde_Application.TrackApplication.View
{
    public partial class HmiTrackControlForm : Form
    {
        public HmiTrackControlForm()
        {
            // Setup IoC
            IoC.Setup();
                        
            InitializeComponent();
        }
    }
}
