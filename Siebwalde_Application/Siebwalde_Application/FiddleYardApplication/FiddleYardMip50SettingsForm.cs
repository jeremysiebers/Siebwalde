using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Siebwalde_Application
{
    public partial class FiddleYardMip50SettingsForm : Form
    {
        private FiddleYardMip50 m_FYMip50;                    // connect to MIP50 class

        public FiddleYardMip50SettingsForm(FiddleYardMip50 FYMip50)
        {
            InitializeComponent();
            m_FYMip50 = FYMip50;
            this.TopLevel = true;
            this.Opacity = 100;
            this.ShowInTaskbar = true;
            this.TopMost = true;
            
        }


    }
}
