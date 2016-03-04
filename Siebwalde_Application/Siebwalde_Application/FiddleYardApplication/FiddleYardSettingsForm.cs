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
            FYSimSpeedSetting = Properties.Settings.Default.FIDDLExYARDxSIMxSPEEDxSETTING;
            SetColorTrackOccupied.BackColor = Properties.Settings.Default.SETxCOLORxTRACKxOCCUPIED;
            SetColorTrackNotInitialized.BackColor = Properties.Settings.Default.SETxCOLORxTRACKxNOTxINITIALIZED;
            SetColorTrackNotActive.BackColor = Properties.Settings.Default.SETxCOLORxTRACKxNOTxACTIVE;
            SetColorTrackDisabled.BackColor = Properties.Settings.Default.SETxCOLORxTRACKxDISABLED;
            SetColorTrackDisabledNotOccupied.BackColor = Properties.Settings.Default.SETxCOLORxTRACKxDISABLEDxNOTxOCCUPIED;
        }

        private void FiddleYardSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Reload();
        }        

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            //Properties.Settings.Default.Reload();
            this.Close();
        }

        private void BtnReload_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reload();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();            
            this.Close();            
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
                Properties.Settings.Default.SETxCOLORxTRACKxOCCUPIED = SetColorTrackOccupied.BackColor;
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
                Properties.Settings.Default.SETxCOLORxTRACKxNOTxINITIALIZED = SetColorTrackNotInitialized.BackColor;
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
                Properties.Settings.Default.SETxCOLORxTRACKxNOTxACTIVE = SetColorTrackNotActive.BackColor;
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
