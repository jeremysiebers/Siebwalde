using System;
using System.Windows.Forms;
using System.Threading;

namespace Siebwalde_Application
{
    public delegate void ReceivedMIP50Data(string name, int val, string log);  // defines a delegate type if caller runs on other thread

    public partial class FiddleYardMip50SettingsForm : Form
    {
        private FiddleYardMip50 m_FYMip50;                      // connect to MIP50 class
        private FiddleYardApplicationVariables m_FYAppVar;      // connect to FiddleYardApplicationVariables

        private enum State { NOP, BT_MIP50PositioningVelocity_SET_Click };
        private State LastExecCommand;

        public Sensor Sns_ReceivedDataFromMip50;
        public string ExecResult = null;

        public string Previous_TB_MIP50PositioningVelocity_Value = null;

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
        public FiddleYardMip50SettingsForm(FiddleYardMip50 FYMip50, FiddleYardApplicationVariables FYAppVar)
        {   
            InitializeComponent();
            m_FYMip50 = FYMip50;
            m_FYAppVar = FYAppVar;
            this.TopLevel = true;
            this.Opacity = 100;
            this.ShowInTaskbar = true;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += new FormClosingEventHandler(FiddleYardMip50SettingsForm_Close);

            //string MIP50PositioningVelocity = System.Configuration.ConfigurationManager.AppSettings["MIP50PositioningVelocity"];
            //string MIP50PositioningAcceleration = System.Configuration.ConfigurationManager.AppSettings["MIP50PositioningAcceleration"];
            //string MIP50PositioningDecceleration = System.Configuration.ConfigurationManager.AppSettings["MIP50PositioningDecceleration"];

            TB_MIP50PositioningVelocity.AppendText(System.Configuration.ConfigurationManager.AppSettings["MIP50PositioningVelocity"]);
            TB_MIP50PositioningAcceleration.AppendText(System.Configuration.ConfigurationManager.AppSettings["MIP50PositioningAcceleration"]);

            //TB_MIP50PositioningVelocity.MaskInputRejected += new MaskInputRejectedEventHandler(maskedTBMIP50PositioningVelocity_MaskInputRejected);
            TB_MIP50PositioningVelocity.Click += new EventHandler(TB_MIP50PositioningVelocity_OnClick);
            TB_MIP50PositioningAcceleration.MaskInputRejected += new MaskInputRejectedEventHandler(maskedTB_MIP50PositioningAcceleration_MaskInputRejected);

            Sensor Sns_ReceivedDataFromMip50 = new Sensor("Mip50Rec", " Mip50ReceivedCmd ", 0, (name, val, log) => ReceivedMIP50Data(name, val, log));  // initialize sensors
            m_FYAppVar.ReceivedDataFromMip50.Attach(Sns_ReceivedDataFromMip50);                                                                         // Attach 
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
            TB_MIP50PositioningVelocity.AppendText(System.Configuration.ConfigurationManager.AppSettings["MIP50PositioningVelocity"]);            
        }

        private void BT_MIP50PositioningVelocity_SET_Click(object sender, EventArgs e)
        {
            m_FYMip50.MIP50xSetxPositioningxVelxDefault(TB_MIP50PositioningVelocity.Text);
            LastExecCommand = State.BT_MIP50PositioningVelocity_SET_Click;                  // Set state in order to check when a message from MIP50 is received, if it is X0 or something else            
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
        private void BT_MIP50PositioningAcceleration_RELOAD_Click(object sender, EventArgs e)
        {
            TB_MIP50PositioningAcceleration.Clear();
            TB_MIP50PositioningAcceleration.AppendText(System.Configuration.ConfigurationManager.AppSettings["MIP50PositioningAcceleration"]);
        }

        private void BT_MIP50PositioningAcceleration_SET_Click(object sender, EventArgs e)
        {

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
                            //TB_MIP50PositioningVelocity.AppendText(System.Configuration.ConfigurationManager.AppSettings["MIP50PositioningVelocity"]);
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

                default: break;
            }            
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MaskInputRejectedEventArgs
         * 
         *  Input(s)   : Eventhandlers on Rejected Events
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
        void maskedTB_MIP50PositioningAcceleration_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            toolTip1.ToolTipTitle = "Invalid Input";
            toolTip1.Show("We're sorry, but only digits (0-9) are allowed and max 2 digits.", TB_MIP50PositioningAcceleration, TB_MIP50PositioningAcceleration.Location, 5000);
        }

        void TB_MIP50PositioningVelocity_OnClick(object sender, EventArgs e)
        {
            Previous_TB_MIP50PositioningVelocity_Value = TB_MIP50PositioningVelocity.Text;
        }

        private void TB_MIP50PositioningVelocity_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            toolTip1.ToolTipTitle = "Invalid Input";
            toolTip1.Show("We're sorry, but only digits (0-9) are allowed and max 3 digits.", TB_MIP50PositioningVelocity, TB_MIP50PositioningVelocity.Location, 5000);
        }

        private void FiddleYardMip50SettingsForm_Close(object sender, FormClosingEventArgs e)
        {            
            m_FYAppVar.ReceivedDataFromMip50.Detach(Sns_ReceivedDataFromMip50);
        }
    }
}
