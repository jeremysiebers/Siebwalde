using System;
using System.Windows.Forms;

namespace SiebwaldeApp
{
    public partial class FiddleYardSettingsForm : Form
    {
        private decimal FYSimSpeedSetting;
                
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
            FYSimSpeedSetting = Core.Properties.CoreSettings.Default.FIDDLExYARDxSIMxSPEEDxSETTING;
            SetColorTrackOccupied.BackColor = Core.Properties.CoreSettings.Default.SETxCOLORxTRACKxOCCUPIED;
            SetColorTrackNotInitialized.BackColor = Core.Properties.CoreSettings.Default.SETxCOLORxTRACKxNOTxINITIALIZED;
            SetColorTrackNotActive.BackColor = Core.Properties.CoreSettings.Default.SETxCOLORxTRACKxNOTxACTIVE;
            SetColorTrackDisabled.BackColor = Core.Properties.CoreSettings.Default.SETxCOLORxTRACKxDISABLED;
            SetColorTrackDisabledNotOccupied.BackColor = Core.Properties.CoreSettings.Default.SETxCOLORxTRACKxDISABLEDxNOTxOCCUPIED;
        }

        private void FiddleYardSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Core.Properties.CoreSettings.Default.Reload();
        }        

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            //Properties.Settings.Default.Reload();
            Close();
        }

        private void BtnReload_Click(object sender, EventArgs e)
        {
            Core.Properties.CoreSettings.Default.Reload();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Core.Properties.CoreSettings.Default.Save();            
            Close();            
        }

        private void SetColorTrackOccupied_Click(object sender, EventArgs e)
        {
            ColorDialog MyDialog = new ColorDialog();
            // Keeps the user from selecting a custom color.
            MyDialog.AllowFullOpen = false;
            // Allows the user to get help. (The default is false.)
            MyDialog.ShowHelp = true;
            // Sets the initial color select to the current text color.
            MyDialog.Color = SetColorTrackOccupied.BackColor;

            // Update the text box color if the user clicks OK 
            if (MyDialog.ShowDialog() == DialogResult.OK)
            {
                SetColorTrackOccupied.BackColor = MyDialog.Color;
                Core.Properties.CoreSettings.Default.SETxCOLORxTRACKxOCCUPIED = SetColorTrackOccupied.BackColor;
            }
        }       

        private void SetColorTrackNotInitialized_Click(object sender, EventArgs e)
        {
            ColorDialog MyDialog = new ColorDialog();
            // Keeps the user from selecting a custom color.
            MyDialog.AllowFullOpen = false;
            // Allows the user to get help. (The default is false.)
            MyDialog.ShowHelp = true;
            // Sets the initial color select to the current text color.
            MyDialog.Color = SetColorTrackNotInitialized.BackColor;

            // Update the text box color if the user clicks OK 
            if (MyDialog.ShowDialog() == DialogResult.OK)
            {
                SetColorTrackNotInitialized.BackColor = MyDialog.Color;
                Core.Properties.CoreSettings.Default.SETxCOLORxTRACKxNOTxINITIALIZED = SetColorTrackNotInitialized.BackColor;
            }
        }

        private void SetColorTrackNotActive_Click(object sender, EventArgs e)
        {
            ColorDialog MyDialog = new ColorDialog();
            // Keeps the user from selecting a custom color.
            MyDialog.AllowFullOpen = false;
            // Allows the user to get help. (The default is false.)
            MyDialog.ShowHelp = true;
            // Sets the initial color select to the current text color.
            MyDialog.Color = SetColorTrackNotActive.BackColor;

            // Update the text box color if the user clicks OK 
            if (MyDialog.ShowDialog() == DialogResult.OK)
            {
                SetColorTrackNotActive.BackColor = MyDialog.Color;
                Core.Properties.CoreSettings.Default.SETxCOLORxTRACKxNOTxACTIVE = SetColorTrackNotActive.BackColor;
            }
        }       

        private void SetColorTrackDisabled_Click(object sender, EventArgs e)
        {
            //ColorDialog MyDialog = new ColorDialog();
            //// Keeps the user from selecting a custom color.
            //MyDialog.AllowFullOpen = false;
            //// Allows the user to get help. (The default is false.)
            //MyDialog.ShowHelp = true;
            //// Sets the initial color select to the current text color.
            //MyDialog.Color = SetColorTrackDisabled.BackColor;

            //// Update the text box color if the user clicks OK 
            //if (MyDialog.ShowDialog() == DialogResult.OK)
            //{
            //    SetColorTrackDisabled.BackColor = MyDialog.Color;
            //    Properties.Settings.Default.SETxCOLORxTRACKxDISABLED = SetColorTrackDisabled.BackColor;
            //}
        }

        private void SetColorTrackDisabledNotOccupied_Click(object sender, EventArgs e)
        {
            //ColorDialog MyDialog = new ColorDialog();
            //// Keeps the user from selecting a custom color.
            //MyDialog.AllowFullOpen = false;
            //// Allows the user to get help. (The default is false.)
            //MyDialog.ShowHelp = true;
            //// Sets the initial color select to the current text color.
            //MyDialog.Color = SetColorTrackDisabledNotOccupied.BackColor;

            //// Update the text box color if the user clicks OK 
            //if (MyDialog.ShowDialog() == DialogResult.OK)
            //{
            //    SetColorTrackDisabledNotOccupied.BackColor = MyDialog.Color;
            //    Properties.Settings.Default.SETxCOLORxTRACKxDISABLEDxNOTxOCCUPIED = SetColorTrackDisabledNotOccupied.BackColor;
            //}
        }        
        
    }
}
