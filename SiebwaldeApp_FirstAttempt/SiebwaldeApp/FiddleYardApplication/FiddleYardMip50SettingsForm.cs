using System;
using System.Windows.Forms;
using System.Configuration;
using System.Timers;
using SiebwaldeApp.Core;

namespace SiebwaldeApp
{
    public delegate void ReceivedMIP50Data(string name, int val, string log);  // defines a delegate type if caller runs on other thread
    public delegate void TB_LOGGING_APPEND(string text);                       // defines a delegate type if caller runs on other thread

    public partial class FiddleYardMip50SettingsForm : Form
    {
        private FiddleYardMip50 m_FYMip50;                      // connect to MIP50 class
        private FiddleYardApplicationVariables m_FYAppVar;      // connect to FiddleYardApplicationVariables
        private string m_instance = null;

        private enum State { NOP, BT_MIP50PositioningVelocity_SET_Click, BT_MIP50PositioningAcceleration_SET_Click, BT_CLEARERROR_Click, BT_POSREGON_Click, BT_POSREGOFF_Click,
            BT_HOME_Click, BT_READPOS_Click, BT_MOVEREL_Click, BT_MOVEABS_Click, BT_READPOS_Click2, BT_MIP50HOMEOFFSETMOVE_SET_Click, BT_MIP50HOMEOFFSETMOVE_READ_Click, BT_MIP50HOMEOFFSETMOVE_READ_Click2
        };
        private State LastExecCommand;

        public Sensor Sns_ReceivedDataFromMip50;
        public string ExecResult = null;

        public string Previous_TB_MIP50PositioningVelocity_Value = null;
        public string Previous_TB_MIP50PositioningAcceleration_Value = null;
        public Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        // Create a timer
        private System.Timers.Timer bTimer = new System.Timers.Timer();
        private bool SettingsFormActive = false;

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
            TB_MIP50PositioningVelocity.TextChanged += new EventHandler(TB_MIP50PositioningVelocity_TextChanged);
            TB_MIP50PositioningAcceleration.Click += new EventHandler(TB_MIP50PositioningAcceleration_OnClick);
            TB_MIP50PositioningAcceleration.TextChanged += new EventHandler(TB_MIP50PositioningAcceleration_TextChanged);

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
            SettingsFormActive = true;

            if (this.Name == "FiddleYardTOP")
            {               
                this.Text = "FiddleYard MIP50 Settings TOP Layer";
            }
            else if (this.Name == "FiddleYardBOT")
            {
                this.Text = "FiddleYard MIP50 Settings BOTTOM Layer";
            }
            this.ShowDialog();

            bTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            bTimer.Interval = 100;
            bTimer.AutoReset = true;
            // Enable the timer
            bTimer.Enabled = true;
        }
        /*#--------------------------------------------------------------------------#*/
        /*  Description: OnTimedEvent
         *               Used to keep the application alive independent from the target
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
        public void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            bTimer.Stop();
            ReceivedMIP50Data("TimerEvent", 0,"");
            bTimer.Start();
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
            TB_LOGGING_APPEND("Reload MIP50 Positioning Velocity from config file: " + TB_MIP50PositioningVelocity.Text);
        }

        private void BT_MIP50PositioningVelocity_SET_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xSetxPositioningxVelxDefault(TB_MIP50PositioningVelocity.Text);
            LastExecCommand = State.BT_MIP50PositioningVelocity_SET_Click;                  // Set state in order to check when a message from MIP50 is received, if it is X0 or something else            
            TB_LOGGING_APPEND("Set MIP50 Positioning Velocity to: "+ TB_MIP50PositioningVelocity.Text);
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
            TB_LOGGING_APPEND("Reload MIP50 Positioning Aceleration from config file: " + TB_MIP50PositioningAcceleration.Text);
        }

        private void BT_MIP50PositioningAcceleration_SET_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xSetxAcceleration(TB_MIP50PositioningAcceleration.Text);
            LastExecCommand = State.BT_MIP50PositioningAcceleration_SET_Click;                  // Set state in order to check when a message from MIP50 is received, if it is X0 or something else
            TB_LOGGING_APPEND("Set MIP50 Positioning Aceleration to: " + TB_MIP50PositioningAcceleration.Text);
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
                    TB_LOGGING_APPEND(m_MIP50xRECxCMDxR);
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
                            TB_LOGGING_APPEND("Error returned from MIP50, check MIP50 error log");
                            LastExecCommand = State.NOP;
                        }
                    }
                    else if (ExecResult == "OK")
                    {
                        Previous_TB_MIP50PositioningVelocity_Value = TB_MIP50PositioningVelocity.Text;  // Update previous value with new value after succesfull execute
                        LastExecCommand = State.NOP;                                                    // if X0 then execution was succesful
                        TB_LOGGING_APPEND("OK returned from MIP50");
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
                            TB_LOGGING_APPEND("Error returned from MIP50, check MIP50 error log");
                            LastExecCommand = State.NOP;
                        }
                    }
                    else if (ExecResult == "OK")
                    {
                        Previous_TB_MIP50PositioningAcceleration_Value = TB_MIP50PositioningAcceleration.Text;  // Update previous value with new value after succesfull execute
                        LastExecCommand = State.NOP;                                                    // if X0 then execution was succesful
                        TB_LOGGING_APPEND("OK returned from MIP50");
                    }
                    break;

                case State.BT_CLEARERROR_Click:
                    if (ExecResult == "Error")
                    {
                        TB_LOGGING_APPEND("Error returned from MIP50, check MIP50 error log");
                        LastExecCommand = State.NOP;
                    }
                    else if (ExecResult == "OK")
                    {
                        TB_LOGGING_APPEND("OK returned from MIP50");
                        LastExecCommand = State.NOP;                                                    // if X0 then execution was succesful
                    }
                    break;

                case State.BT_POSREGON_Click:
                    if (ExecResult == "Error")
                    {
                        TB_LOGGING_APPEND("Error returned from MIP50, check MIP50 error log");
                        LastExecCommand = State.NOP;
                    }
                    else if (ExecResult == "OK")
                    {
                        TB_LOGGING_APPEND("OK returned from MIP50");
                        LastExecCommand = State.NOP;                                                    // if X0 then execution was succesful
                    }
                    break;

                case State.BT_POSREGOFF_Click:
                    if (ExecResult == "Error")
                    {
                        TB_LOGGING_APPEND("Error returned from MIP50, check MIP50 error log");
                        LastExecCommand = State.NOP;
                    }
                    else if (ExecResult == "OK")
                    {
                        TB_LOGGING_APPEND("OK returned from MIP50");
                        LastExecCommand = State.NOP;                                                    // if X0 then execution was succesful
                    }
                    break;

                case State.BT_HOME_Click:
                    if (ExecResult == "Error")
                    {
                        TB_LOGGING_APPEND("Error returned from MIP50, check MIP50 error log");
                        LastExecCommand = State.NOP;
                    }
                    else if (ExecResult == "OK")
                    {
                        TB_LOGGING_APPEND("OK returned from MIP50");
                        LastExecCommand = State.NOP;                                                    // if X0 then execution was succesful
                    }
                    break;

                case State.BT_READPOS_Click:
                    if (ExecResult == "Error")
                    {
                        TB_LOGGING_APPEND("Error returned from MIP50, check MIP50 error log");
                        LastExecCommand = State.NOP;
                    }
                    else if (ExecResult == "OK")
                    {
                        TB_LOGGING_APPEND("OK returned from MIP50");
                        LastExecCommand = State.BT_READPOS_Click2;                                                    // if X0 then execution was succesful wait for update of postion
                    }
                    break;

                case State.BT_READPOS_Click2:
                    if (m_FYMip50.GetActualPositionUpdated() == true)
                    {
                        LastExecCommand = State.NOP;
                    }
                    break;

                case State.BT_MOVEREL_Click:
                    if (ExecResult == "Error")
                    {
                        TB_LOGGING_APPEND("Error returned from MIP50, check MIP50 error log");
                        LastExecCommand = State.NOP;
                    }
                    else if (ExecResult == "OK")
                    {
                        TB_LOGGING_APPEND("OK returned from MIP50");
                        m_FYMip50.MIP50xReadxPosition();
                        LastExecCommand = State.BT_READPOS_Click;
                        TB_LOGGING_APPEND("MIP50 Read Position");
                    }
                    break;

                case State.BT_MOVEABS_Click:
                    if (ExecResult == "Error")
                    {
                        TB_LOGGING_APPEND("Error returned from MIP50, check MIP50 error log");
                        LastExecCommand = State.NOP;
                    }
                    else if (ExecResult == "OK")
                    {
                        TB_LOGGING_APPEND("OK returned from MIP50");
                        m_FYMip50.MIP50xReadxPosition();
                        LastExecCommand = State.BT_READPOS_Click;
                        TB_LOGGING_APPEND("MIP50 Read Position");
                    }
                    break;

                case State.BT_MIP50HOMEOFFSETMOVE_SET_Click:
                    if (ExecResult == "Error")
                    {
                        TB_LOGGING_APPEND("Error returned from MIP50, check MIP50 error log");
                        LastExecCommand = State.NOP;
                    }
                    else if (ExecResult == "OK")
                    {
                        TB_LOGGING_APPEND("OK returned from MIP50");
                        LastExecCommand = State.NOP;                        
                    }
                    break;

                case State.BT_MIP50HOMEOFFSETMOVE_READ_Click:
                    if (ExecResult == "Error")
                    {
                        TB_LOGGING_APPEND("Error returned from MIP50, check MIP50 error log");
                        LastExecCommand = State.NOP;
                    }
                    else if (ExecResult == "OK")
                    {
                        TB_LOGGING_APPEND("OK returned from MIP50");
                        LastExecCommand = State.BT_MIP50HOMEOFFSETMOVE_READ_Click2;
                        
                    }
                    break;

                case State.BT_MIP50HOMEOFFSETMOVE_READ_Click2:
                    if (m_FYMip50.GetHomeOffsetMovementUpdated() == true)
                    {
                        if (TB_HOMEOFFSETMOVE.InvokeRequired)
                        {
                            ReceivedMIP50Data d = new ReceivedMIP50Data(ReceivedMIP50Data);
                            TB_HOMEOFFSETMOVE.Invoke(d, new object[] { name, val, log });  // invoking itself      
                        }
                        else
                        {
                            TB_HOMEOFFSETMOVE.Text = Convert.ToString(m_FYMip50.GetHomeOffsetMovement());
                            LastExecCommand = State.NOP;
                        }                        
                    }
                    break;
                    
                default: break;
            }

            if (SettingsFormActive == true) // because of not detaching (because of unkown object while detach --> ask somebody (detach from unknown object or something))
            {
                try
                {
                    if (TB_ACTUALPOSITION.InvokeRequired)
                    {
                        ReceivedMIP50Data d = new ReceivedMIP50Data(ReceivedMIP50Data);
                        TB_ACTUALPOSITION.Invoke(d, new object[] { name, val, log });  // invoking itself                      
                    }
                    else
                    {
                        TB_ACTUALPOSITION.Text = Convert.ToString(m_FYMip50.GetActualPosition());
                    }
                }
                catch { }
            }
            
        }
        /*#--------------------------------------------------------------------------#*/
        /*  Description: TB_LOGGING_APPEND
         * 
         *  Input(s)   : New logging text to log
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
        public void TB_LOGGING_APPEND(string text)
        {
            string fmt = "000";

            if (TB_LOGGING.InvokeRequired)
            {
                TB_LOGGING_APPEND d = new TB_LOGGING_APPEND(TB_LOGGING_APPEND);
                TB_LOGGING.Invoke(d, new object[] { text });  // invoking itself                            
            }
            else
            {
                int m_Millisecond = DateTime.Now.Millisecond;
                string m_text = DateTime.Now + ":" + m_Millisecond.ToString(fmt) + " " + text + " " + Environment.NewLine;
                TB_LOGGING.AppendText(m_text);
            }
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
            toolTip1.Show("Only digits (0-9) are allowed, max 3 digits, max 150 QC/ms.", TB_MIP50PositioningVelocity, TB_MIP50PositioningVelocity.Location, 5000);
        }
        private void TB_MIP50PositioningVelocity_OnClick(object sender, EventArgs e)
        {
            Previous_TB_MIP50PositioningVelocity_Value = TB_MIP50PositioningVelocity.Text;
        }

        private void TB_MIP50PositioningVelocity_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Convert.ToDouble(TB_MIP50PositioningVelocity.Text) > 150)
                {
                    TB_MIP50PositioningVelocity.Text = "150";
                }
            }
            catch { }
        }
        /*#-------------------------------------------------------------------------------------------------------------------------------------------------------------#*/

        private void TB_MIP50PositioningAcceleration_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            toolTip1.ToolTipTitle = "Invalid Input";
            toolTip1.Show("Only digits (0-9) are allowed, max 3 digits, max 0.90 QC/ms^2.", TB_MIP50PositioningAcceleration, TB_MIP50PositioningAcceleration.Location, 5000);
        }

        private void TB_MIP50PositioningAcceleration_OnClick(object sender, EventArgs e)
        {
            Previous_TB_MIP50PositioningAcceleration_Value = TB_MIP50PositioningAcceleration.Text;
        }

        private void TB_MIP50PositioningAcceleration_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Convert.ToDouble(TB_MIP50PositioningAcceleration.Text) > 0.90)
                {
                    TB_MIP50PositioningAcceleration.Text = "0.90";
                }
            }
            catch { }
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
            try
            {
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch
            {
                MessageBox.Show("BT_SAVE_TO_FILE_Click error");
            }
            this.Close();
        }

        private void FiddleYardMip50SettingsForm_Close(object sender, FormClosingEventArgs e)
        {
            bTimer.Enabled = false;
            SettingsFormActive = false;
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
            TB_LOGGING_APPEND("MIP50 Enable");
        }

        private void BT_MIPDISABLE_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xDISABLE();
            TB_LOGGING_APPEND("MIP50 Disable");
        }

        private void BT_CLEARERROR_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xClearxError();
            LastExecCommand = State.BT_CLEARERROR_Click;
            TB_LOGGING_APPEND("MIP50 Clear Error");
        }

        private void BT_POSREGON_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xActivatexPosxReg();
            LastExecCommand = State.BT_POSREGON_Click;
            TB_LOGGING_APPEND("MIP50 Position Regulation ON");
        }

        private void BT_POSREGOFF_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xDeactivatexPosxReg();
            LastExecCommand = State.BT_POSREGOFF_Click;
            TB_LOGGING_APPEND("MIP50 Position Regulation OFF");
        }

        private void BT_HOME_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xHomexAxis();
            LastExecCommand = State.BT_HOME_Click;
            TB_LOGGING_APPEND("MIP50 Start Homing");
        }

        private void BT_READPOS_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xReadxPosition();
            LastExecCommand = State.BT_READPOS_Click;
            TB_LOGGING_APPEND("MIP50 Read Position");
        }

        private void BT_MOVEREL_Click(object sender, EventArgs e)
        {
            Int32 Number = Convert.ToInt32(TB_MOVERELPOS.Text);
            UInt32 Number2 = 0;

            if (Number < 0)
            {
                Number2 -= Convert.ToUInt32(Number * -1);
            }
            else
            {
                Number2 = Convert.ToUInt32(Number);
            }

            m_FYMip50.MIP50xRel_Pos(Number2);
            LastExecCommand = State.BT_MOVEREL_Click;
            TB_LOGGING_APPEND("MIP50 Move Relative : " + TB_MOVERELPOS.Text);
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
            LastExecCommand = State.BT_MOVEABS_Click;
            TB_LOGGING_APPEND("MIP50 Move to Absolute Position: " + TB_MOVEABSPOS.Text);
        }

        private void BT_MIP50HOMEOFFSETMOVE_SET_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xWritexPermanentxParameter("33", TB_HOMEOFFSETMOVE.Text);
            LastExecCommand = State.BT_MIP50HOMEOFFSETMOVE_SET_Click;                  // Set state in order to check when a message from MIP50 is received, if it is X0 or something else
            TB_LOGGING_APPEND("Set MIP50 Parameter 33 Home Offset Move to: " + TB_HOMEOFFSETMOVE.Text);
        }

        private void BT_MIP50HOMEOFFSETMOVE_READ_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xReadxPermanentxParameter("33");
            LastExecCommand = State.BT_MIP50HOMEOFFSETMOVE_READ_Click;                  // Set state in order to check when a message from MIP50 is received, if it is X0 or something else
            TB_LOGGING_APPEND("Read MIP50 Parameter 33 Home Offset Move");
        }
    }
}
