using System;
using System.Windows.Forms;
using System.Threading;
using System.Configuration;

namespace Siebwalde_Application
{
    public delegate void ReceivedMIP50Data(string name, int val, string log);  // defines a delegate type if caller runs on other thread

    public partial class FiddleYardMip50SettingsForm : Form
    {
        private FiddleYardMip50 m_FYMip50;                      // connect to MIP50 class
        private FiddleYardApplicationVariables m_FYAppVar;      // connect to FiddleYardApplicationVariables
        private string m_instance = null;

        private enum State { NOP, BT_MIP50PositioningVelocity_SET_Click, BT_MIP50PositioningAcceleration_SET_Click };
        private State LastExecCommand;

        public Sensor Sns_ReceivedDataFromMip50;
        public string ExecResult = null;

        public string Previous_TB_MIP50PositioningVelocity_Value = null;
        public string Previous_TB_MIP50PositioningAcceleration_Value = null;
        public Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Constructor
         * 
         *  Input(s)   : FiddleYardMip50
         *
         *  Output(s)  : 
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         */
        /*#--------------------------------------------------------------------------#*/
        public FiddleYardMip50SettingsForm(string instance, FiddleYardMip50 FYMip50, FiddleYardApplicationVariables FYAppVar)
        {
            InitializeComponent();
            m_FYMip50 = FYMip50;
            m_FYAppVar = FYAppVar;
            m_instance = instance;

            

            //config.AppSettings.Settings[m_instance + "_" + "Track1_Abs_Position"].Value = "50";

            this.FormClosing += new FormClosingEventHandler(FiddleYardMip50SettingsForm_Close);
            try
            {
                TB_MIP50PositioningVelocity.AppendText(config.AppSettings.Settings[m_instance + "_" + "MIP50PositioningVelocity"].Value);
                TB_MIP50PositioningAcceleration.AppendText(config.AppSettings.Settings[m_instance + "_" + "MIP50PositioningAcceleration"].Value);
                TB_TRACK1_ABS_POS.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track1_Abs_Position"].Value);
                TB_TRACK2_ABS_POS.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track2_Abs_Position"].Value);
                TB_TRACK3_ABS_POS.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track3_Abs_Position"].Value);
                TB_TRACK4_ABS_POS.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track4_Abs_Position"].Value);
                TB_TRACK5_ABS_POS.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track5_Abs_Position"].Value);
                TB_TRACK6_ABS_POS.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track6_Abs_Position"].Value);
                TB_TRACK7_ABS_POS.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track7_Abs_Position"].Value);
                TB_TRACK8_ABS_POS.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track8_Abs_Position"].Value);
                TB_TRACK9_ABS_POS.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track9_Abs_Position"].Value);
                TB_TRACK10_ABS_POS.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track10_Abs_Position"].Value);
                TB_TRACK11_ABS_POS.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track11_Abs_Position"].Value);
                TB_TRACK1_BKOFFSET.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track1_Back_Offset"].Value);
                TB_TRACK2_BKOFFSET.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track2_Back_Offset"].Value);
                TB_TRACK3_BKOFFSET.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track3_Back_Offset"].Value);
                TB_TRACK4_BKOFFSET.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track4_Back_Offset"].Value);
                TB_TRACK5_BKOFFSET.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track5_Back_Offset"].Value);
                TB_TRACK6_BKOFFSET.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track6_Back_Offset"].Value);
                TB_TRACK7_BKOFFSET.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track7_Back_Offset"].Value);
                TB_TRACK8_BKOFFSET.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track8_Back_Offset"].Value);
                TB_TRACK9_BKOFFSET.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track9_Back_Offset"].Value);
                TB_TRACK10_BKOFFSET.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track10_Back_Offset"].Value);
                TB_TRACK11_BKOFFSET.AppendText(config.AppSettings.Settings[m_instance + "_" + "Track11_Back_Offset"].Value);
            }
            catch
            {
                MessageBox.Show("Reading " + m_instance + " Appsettings error in constructor FiddleYardMip50SettingsForm");
            }


            TB_MIP50PositioningVelocity.Click += new EventHandler(TB_MIP50PositioningVelocity_OnClick);
            TB_MIP50PositioningAcceleration.Click += new EventHandler(TB_MIP50PositioningAcceleration_OnClick);

            Sensor Sns_ReceivedDataFromMip50 = new Sensor("Mip50Rec", " Mip50ReceivedCmd ", 0, (name, val, log) => ReceivedMIP50Data(name, val, log));  // initialize sensors
            m_FYAppVar.ReceivedDataFromMip50.Attach(Sns_ReceivedDataFromMip50);                                                                         // Attach 
        }


        public void FYMIP50SETTINGSFORMShow()
        {
            this.TopLevel = true;
            this.Opacity = 100;
            this.ShowInTaskbar = true;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            if (this.Name == "FiddleYardTOP")
            {               
                this.Text = "FiddleYard MIP50 Settings TOP Layer";
            }
            else if (this.Name == "FiddleYardBOT")
            {
                this.Text = "FiddleYard MIP50 Settings BOTTOM Layer";
            }
            this.Show();
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: BT_MIP50PositioningVelocity
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : 
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         */
        /*#--------------------------------------------------------------------------#*/
        private void BT_MIP50PositioningVelocity_RELOAD_Click(object sender, EventArgs e)
        {
            TB_MIP50PositioningVelocity.Clear();
            TB_MIP50PositioningVelocity.AppendText(config.AppSettings.Settings[m_instance + "_" + "MIP50PositioningVelocity"].Value);            
        }

        private void BT_MIP50PositioningVelocity_SET_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xSetxPositioningxVelxDefault(TB_MIP50PositioningVelocity.Text);
            LastExecCommand = State.BT_MIP50PositioningVelocity_SET_Click;                  // Set state in order to check when a message from MIP50 is received, if it is X0 or something else            
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: BT_MIP50PositioningAcceleration
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : 
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         */
        /*#--------------------------------------------------------------------------#*/
        private void BT_MIP50PositioningAcceleration_RELOAD_Click(object sender, EventArgs e)
        {
            TB_MIP50PositioningAcceleration.Clear();
            TB_MIP50PositioningAcceleration.AppendText(config.AppSettings.Settings[m_instance + "_" + "MIP50PositioningAcceleration"].Value);
        }

        private void BT_MIP50PositioningAcceleration_SET_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xSetxAcceleration(TB_MIP50PositioningAcceleration.Text);
            LastExecCommand = State.BT_MIP50PositioningAcceleration_SET_Click;                  // Set state in order to check when a message from MIP50 is received, if it is X0 or something else
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: BT_MIP50PositioningVelocity
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : 
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         */
        /*#--------------------------------------------------------------------------#*/
        private void ReceivedMIP50Data(string name, int val, string log)
        {
            string m_MIP50xRECxCMDxR;            

            if (LastExecCommand != State.NOP)                                       // Check if the received command is created due to change of data in this form
            {
                m_MIP50xRECxCMDxR = m_FYMip50.MIP50xRECxCMDxR();                    // Read from mailbox

                if (m_MIP50xRECxCMDxR == "X0")                                      // Check first if received message is X0
                {                    
                    ExecResult = "OK";
                }
                else if (m_MIP50xRECxCMDxR != "")                                   // if X0 was not received
                {
                    if (m_FYMip50.MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)   // check if Translated message is error, else Message was received wait for next command to be X0
                    {
                        ExecResult = "Error";                                      // Set active state to UNDO action because an error from MIP50 was received
                    }
                }
            }

            switch (LastExecCommand)
            {
                case State.BT_MIP50PositioningVelocity_SET_Click:
                    if (ExecResult == "Error")
                    {
                        if (TB_MIP50PositioningVelocity.InvokeRequired)
                        {
                            ReceivedMIP50Data d = new ReceivedMIP50Data(ReceivedMIP50Data);
                            TB_MIP50PositioningVelocity.Invoke(d, new object[] { name, val, log });  // invoking itself                            
                        }
                        else
                        {
                            TB_MIP50PositioningVelocity.Clear();                            
                            TB_MIP50PositioningVelocity.Text = Previous_TB_MIP50PositioningVelocity_Value;
                            toolTip1.ToolTipTitle = "MIP50 Error response";
                            toolTip1.Show("See error log for details.", TB_MIP50PositioningVelocity, TB_MIP50PositioningVelocity.Location, 5000);
                            LastExecCommand = State.NOP;
                        }
                    }
                    else if (ExecResult == "OK")
                    {
                        Previous_TB_MIP50PositioningVelocity_Value = TB_MIP50PositioningVelocity.Text;  // Update previous value with new value after succesfull execute
                        LastExecCommand = State.NOP;                                                    // if X0 then execution was succesful
                    }
                    break;

                case State.BT_MIP50PositioningAcceleration_SET_Click:
                    if (ExecResult == "Error")
                    {
                        if (TB_MIP50PositioningAcceleration.InvokeRequired)
                        {
                            ReceivedMIP50Data d = new ReceivedMIP50Data(ReceivedMIP50Data);
                            TB_MIP50PositioningAcceleration.Invoke(d, new object[] { name, val, log });  // invoking itself                            
                        }
                        else
                        {
                            TB_MIP50PositioningAcceleration.Clear();
                            TB_MIP50PositioningAcceleration.Text = Previous_TB_MIP50PositioningAcceleration_Value;
                            toolTip1.ToolTipTitle = "MIP50 Error response";
                            toolTip1.Show("See error log for details.", TB_MIP50PositioningAcceleration, TB_MIP50PositioningAcceleration.Location, 5000);
                            LastExecCommand = State.NOP;
                        }
                    }
                    else if (ExecResult == "OK")
                    {
                        Previous_TB_MIP50PositioningAcceleration_Value = TB_MIP50PositioningAcceleration.Text;  // Update previous value with new value after succesfull execute
                        LastExecCommand = State.NOP;                                                    // if X0 then execution was succesful
                    }
                    break;

                default: break;
            }

            if (TB_ACTUALPOSITION.InvokeRequired)
            {
                ReceivedMIP50Data d = new ReceivedMIP50Data(ReceivedMIP50Data);
                TB_ACTUALPOSITION.Invoke(d, new object[] { name, val, log });  // invoking itself      
            }
            else { TB_ACTUALPOSITION.Text = Convert.ToString(m_FYMip50.ActualPosition); }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MaskInputRejectedEventArgs
         * 
         *  Input(s)   : Eventhandlers on Rejected Events and mouse click
         *
         *  Output(s)  : 
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         */
        /*#-----------------------------------------------------------------------------------------------------------------------------------------------------------#*/
        private void TB_MIP50PositioningVelocity_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            toolTip1.ToolTipTitle = "Invalid Input";
            toolTip1.Show("Only digits (0-9) are allowed and max 3 digits.", TB_MIP50PositioningVelocity, TB_MIP50PositioningVelocity.Location, 5000);
        }
        private void TB_MIP50PositioningVelocity_OnClick(object sender, EventArgs e)
        {
            Previous_TB_MIP50PositioningVelocity_Value = TB_MIP50PositioningVelocity.Text;
        }
        /*#-------------------------------------------------------------------------------------------------------------------------------------------------------------#*/

        private void TB_MIP50PositioningAcceleration_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            toolTip1.ToolTipTitle = "Invalid Input";
            toolTip1.Show("Only digits (0-9) are allowed and max 2 digits.", TB_MIP50PositioningAcceleration, TB_MIP50PositioningAcceleration.Location, 5000);
        }

        private void TB_MIP50PositioningAcceleration_OnClick(object sender, EventArgs e)
        {
            Previous_TB_MIP50PositioningAcceleration_Value = TB_MIP50PositioningAcceleration.Text;
        }
        /*#--------------------------------------------------------------------------------------------------------------------------------------------------------------#*/

        
        /*#--------------------------------------------------------------------------#*/
        /*  Description: Other Buttons (Save, Cancel etc)
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : 
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         */
        /*#--------------------------------------------------------------------------#*/
        private void BT_SAVEPARAMS_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "MIP50PositioningVelocity"].Value = TB_MIP50PositioningVelocity.Text;
            config.AppSettings.Settings[m_instance + "_" + "MIP50PositioningAcceleration"].Value = TB_MIP50PositioningAcceleration.Text;
        }

        private void BT_CANCEL_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BT_SAVE_TO_FILE_Click(object sender, EventArgs e)
        {
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            this.Close();
        }

        private void FiddleYardMip50SettingsForm_Close(object sender, FormClosingEventArgs e)
        {

        }

        private void BT_TRACK1_ABS_POS_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track1_Abs_Position"].Value = TB_TRACK1_ABS_POS.Text;
        }

        private void BT_TRACK1_BKOFFSET_SAVE_Click(object sender, EventArgs e)
        {
            //config.AppSettings.Settings[m_instance + "_" + "Track1_Back_Offset"].Value = TB_TRACK1_BKOFFSET.Text;
            config.AppSettings.Settings[m_instance + "_" + "Track1_Back_Offset"].Value = TB_TRACK1_BKOFFSET.Text;
        }

        private void BT_TRACK2_ABS_POS_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track2_Abs_Position"].Value = TB_TRACK2_ABS_POS.Text;
        }

        private void BT_TRACK2_BKOFFSET_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track2_Back_Offset"].Value = TB_TRACK2_BKOFFSET.Text;
        }

        private void BT_TRACK3_ABS_POS_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track3_Abs_Position"].Value = TB_TRACK3_ABS_POS.Text;
        }

        private void BT_TRACK3_BKOFFSET_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track3_Back_Offset"].Value = TB_TRACK3_BKOFFSET.Text;
        }

        private void BT_TRACK4_ABS_POS_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track4_Abs_Position"].Value = TB_TRACK4_ABS_POS.Text;
        }

        private void BT_TRACK4_BKOFFSET_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track4_Back_Offset"].Value = TB_TRACK4_BKOFFSET.Text;
        }

        private void BT_TRACK5_ABS_POS_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track5_Abs_Position"].Value = TB_TRACK5_ABS_POS.Text;
        }

        private void BT_TRACK5_BKOFFSET_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track5_Back_Offset"].Value = TB_TRACK5_BKOFFSET.Text;
        }

        private void BT_TRACK6_ABS_POS_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track6_Abs_Position"].Value = TB_TRACK6_ABS_POS.Text;
        }

        private void BT_TRACK6_BKOFFSET_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track6_Back_Offset"].Value = TB_TRACK6_BKOFFSET.Text;
        }

        private void BT_TRACK7_ABS_POS_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track7_Abs_Position"].Value = TB_TRACK7_ABS_POS.Text;
        }

        private void BT_TRACK7_BKOFFSET_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track7_Back_Offset"].Value = TB_TRACK7_BKOFFSET.Text;
        }

        private void BT_TRACK8_ABS_POS_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track8_Abs_Position"].Value = TB_TRACK8_ABS_POS.Text;
        }

        private void BT_TRACK8_BKOFFSET_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track8_Back_Offset"].Value = TB_TRACK8_BKOFFSET.Text;
        }

        private void BT_TRACK9_ABS_POS_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track9_Abs_Position"].Value = TB_TRACK9_ABS_POS.Text;
        }

        private void BT_TRACK9_BKOFFSET_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track9_Back_Offset"].Value = TB_TRACK9_BKOFFSET.Text;
        }

        private void BT_TRACK10_ABS_POS_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track10_Abs_Position"].Value = TB_TRACK10_ABS_POS.Text;
        }

        private void BT_TRACK10_BKOFFSET_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track10_Back_Offset"].Value = TB_TRACK10_BKOFFSET.Text;
        }

        private void BT_TRACK11_ABS_POS_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track11_Abs_Position"].Value = TB_TRACK11_ABS_POS.Text;
        }

        private void BT_TRACK11_BKOFFSET_SAVE_Click(object sender, EventArgs e)
        {
            config.AppSettings.Settings[m_instance + "_" + "Track11_Back_Offset"].Value = TB_TRACK11_BKOFFSET.Text;
        }

        private void BT_MIPENABLE_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xENABLE();
        }

        private void BT_MIPDISABLE_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xDISABLE();
        }

        private void BT_CLEARERROR_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xClearxError();
        }

        private void BT_POSREGON_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xActivatexPosxReg();
        }

        private void BT_POSREGOFF_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xDeactivatexPosxReg();
        }

        private void BT_HOME_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xHomexAxis();
        }

        private void BT_READPOS_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xReadxPosition();
        }

        private void BT_MOVEREL_Click(object sender, EventArgs e)
        {
           
        }

        private void BT_MOVEABS_Click(object sender, EventArgs e)
        {
            Int32 Number = Convert.ToInt32(TB_MOVEABSPOS.Text);
            UInt32 Number2 = 0;

            if (Number < 0)
            {
                Number2 -= Convert.ToUInt32(Number * -1);
            }
            else
            {
                Number2 = Convert.ToUInt32(Number);
            }

            m_FYMip50.MIP50xAbs_Pos(Number2);
        }
    }
}
