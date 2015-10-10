using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;

namespace Siebwalde_Application
{
    public partial class FiddleYardSettingsForm : Form
    {
        private decimal FYSimSpeedSetting = Properties.Settings.Default.FIDDLExYARDxSIMxSPEEDxSETTING;

        public FiddleYardSettingsForm()
        {
            InitializeComponent();
            this.Show();
            this.TopLevel = true;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void FiddleYardSettingsForm_Load(object sender, EventArgs e)
        {
            this.FormClosing += new FormClosingEventHandler(FiddleYardSettingsForm_FormClosing);            
        }

        private void FiddleYardSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Reload();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (FiddleYardSimSpeedSetting.Value != FYSimSpeedSetting)
            {
                DialogResult dr = MessageBox.Show(
                           "Save current values for application settings?",
                           "Save Settings", MessageBoxButtons.YesNo);
                if (DialogResult.Yes == dr)
                {
                    Properties.Settings.Default.Save();
                    this.Close();
                }
            }
            if (FiddleYardSimSpeedSetting.Value == FYSimSpeedSetting)
            {
                this.Close();
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {            
            this.Close();
        }
        
    }
}
