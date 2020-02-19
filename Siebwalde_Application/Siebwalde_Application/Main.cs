using Siebwalde_Application.TrackApplication.View;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace Siebwalde_Application
{
    public delegate void ToggleCommLinkCallback();
    
    public interface iMain
    {
        void SiebwaldeAppLogging(string text);
        void FYLinkActivityUpdate();        
    }
    
    public partial class Main : Form , iMain
    {
        public FiddleYardController FYcontroller;
        public FiddleYardSettingsForm FYSettingsForm;
        public TrackController MTcontroller;
        public FiddleYardController YDcontroller;
        public MAC_IP_Conditioner MACIPConditioner = new MAC_IP_Conditioner { };
        public HmiTrackControlForm hmiTrackForm;

        private const int LINKACTMAX = 100;
        private string path = @"c:\localdata\Siebwalde\"+ DateTime.Now.Day + "-"+ DateTime.Now.Month + "-"+ DateTime.Now.Year + "_SiebwaldeApplicationMain.txt"; //  different logging file per target, this is default
        public Log2LoggingFile SiebwaldeApplicationMainLogging;

        private bool ViewTop = true;
        private bool ViewBot = true;

        public Main()
        {

            Siebwalde_Application.Properties.Settings.Default.Reload();
            SiebwaldeApplicationMainLogging = new Log2LoggingFile(path);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);
            InitializeComponent();            
            
            StartApplication.Visible = true;
            LStartApplication.Visible = true;
            FiddleYardFormTop.Visible = false;
            FiddleYardFormBot.Visible = false;
            MaintrackForm.Visible = false;
            YardForm.Visible = false;

            FYLinkActivity.Visible = false;
            LFYLinkActivity.Visible = false;
            FYLinkActivity.Minimum = 0;
            FYLinkActivity.Maximum = LINKACTMAX;
            FYLinkActivity.Step = 1;
            FYLinkActivity.Value = 0;
            FYLinkActivity.Location = new System.Drawing.Point(this.Width - FYLinkActivity.Width - 20, 1);
            LFYLinkActivity.Location = new System.Drawing.Point(this.Width - FYLinkActivity.Width - 20 - LFYLinkActivity.Width, 6);

        }

        private void StartApplication_Click(object sender, EventArgs e)
        {
            SiebwaldeAppLogging("Siebwalde Application started.");
            SiebwaldeAppLogging("Main: PC MAC adress is: " + MACIPConditioner.MACstring());
            SiebwaldeAppLogging("Main: PC IP adress is: " + MACIPConditioner.IPstring());

            StartApplication.Visible = false;
            LStartApplication.Visible = false;       
                                    
            /// start FY CONTROLLER ///
            StartFYController();

            /// start MT CONTROLLER ///
            StartMTController();

            /// start YARD CONTROLLER ///
            //StartYARDController();
        }

        private void StartFYController()
        {
            int FYSendingport = 28671;
            int FYReceivingport = 0x7000; // Port on which the PC will receive data from the FiddleYard   
            FYcontroller = new FiddleYardController(this, MACIPConditioner.MAC(), MACIPConditioner.IP(), FYReceivingport, FYSendingport);            
            SiebwaldeAppLogging("Main: FiddleYard Controller starting...");
            FYcontroller.Start();
            FiddleYardFormTop.Visible = true;
            FiddleYardFormBot.Visible = false;
            FYLinkActivity.Visible = true;
            LFYLinkActivity.Visible = true;
            SiebwaldeAppLogging("Main: FiddleYard Controller started.");
        }

        private void StartMTController()
        {
            int TrackControllerSendingport = 10000;
            int TrackControllerReceivingport = 10001;
            MTcontroller = new TrackController(this, TrackControllerReceivingport, TrackControllerSendingport);
            MTcontroller.Start();
            MaintrackForm.Visible = true;
            SiebwaldeAppLogging("Main: Track Controller started.");

            //hmiTrackForm = new HmiTrackControlForm(MTcontroller);
            //hmiTrackForm.Show();
            //hmiTrackForm.Location = new Point(Location.X, Location.Y + 80);
            //hmiTrackForm.Width = Width;
            //hmiTrackForm.Height = Height - 80;
            //hmiTrackForm.TopLevel = true;
            //hmiTrackForm.BringToFront();
        }

        private void StartYARDController()
        {
            int YDSendingport = 28673;
            int YDReceivingport = 28674;
            YDcontroller = new FiddleYardController(this, MACIPConditioner.MAC(), MACIPConditioner.IP(), YDReceivingport, YDSendingport);
            //YDcontroller.start();
            YardForm.Visible = true;
            SiebwaldeAppLogging("Main: Yard Controller started.");
        }

        private void FiddleYardFormTop_Click(object sender, EventArgs e)
        {
            bool autoscroll = false;            
            if (this.Height < 1200 || this.Width < 1920)
                autoscroll = true;
            else { autoscroll = false; }
            FYcontroller.FYTOPShow(autoscroll, this.Height, this.Width, this.Location.X, this.Location.Y, ViewTop);
            ViewTop = !ViewTop;
            if (!ViewTop)
            {
                SiebwaldeAppLogging("Main: Show Fiddle Yard Top Layer interface");
                FiddleYardFormTop.Text = "Hide Fiddle Yard";
            }
            else 
            {
                SiebwaldeAppLogging("Main: Hide Fiddle Yard Top Layer interface");
                FiddleYardFormTop.Text = "Show Fiddle Yard"; 
            }
            
            if (this.Height < 1200 || this.Width < 1920)
                autoscroll = true;
            else { autoscroll = false; }
            FYcontroller.FYBOTShow(autoscroll, this.Height, this.Width, this.Location.X, this.Location.Y, ViewBot);
            ViewBot = !ViewBot;
            if (!ViewBot)
            {
                SiebwaldeAppLogging("Main: Show Fiddle Yard Bottom Layer interface");
                FiddleYardFormBot.Text = "Hide Fiddle Yard Bot";
            }
            else
            {
                SiebwaldeAppLogging("Main: Hide Fiddle Yard Bottom Layer interface");
                FiddleYardFormBot.Text = "Show Fiddle Yard Bot";
            }
        }

        // no used
        private void FiddleYardFormBot_Click(object sender, EventArgs e)
        {
            bool autoscroll;
            if (this.Height < 1200 || this.Width < 1920)
                autoscroll = true;
            else { autoscroll = false; }
            FYcontroller.FYBOTShow(autoscroll, this.Height, this.Width, this.Location.X, this.Location.Y, ViewBot);
            ViewBot = !ViewBot;
            if (!ViewBot)
            {
                SiebwaldeAppLogging("Main: Show Fiddle Yard Bottom Layer interface");
                FiddleYardFormBot.Text = "Hide Fiddle Yard Bot";
            }
            else 
            {
                SiebwaldeAppLogging("Main: Hide Fiddle Yard Bottom Layer interface");
                FiddleYardFormBot.Text = "Show Fiddle Yard Bot"; 
            }
        }

        private void MaintrackForm_Click(object sender, EventArgs e)
        {
            if(hmiTrackForm != null && hmiTrackForm.IsDisposed != true)
            {
                if (hmiTrackForm.Visible && hmiTrackForm.WindowState != FormWindowState.Minimized)
                {
                    SiebwaldeAppLogging("Main: Hide Main Track interface");
                    hmiTrackForm.Hide();
                }
                else
                {
                    SiebwaldeAppLogging("Main: Show Main Track interface");
                    hmiTrackForm.Location = new Point(Location.X, Location.Y + 80);
                    hmiTrackForm.Width = Width;
                    hmiTrackForm.Height = Height - 80;
                    hmiTrackForm.Show();
                    hmiTrackForm.TopLevel = true;
                    hmiTrackForm.BringToFront();

                    if(hmiTrackForm.WindowState == FormWindowState.Minimized)
                    {
                        hmiTrackForm.WindowState = FormWindowState.Normal;
                    }
                }
            }
            else
            {
                hmiTrackForm = new HmiTrackControlForm(MTcontroller);
                hmiTrackForm.Show();
                hmiTrackForm.Location = new Point(Location.X, Location.Y + 80);
                hmiTrackForm.Width = Width;
                hmiTrackForm.Height = Height - 80;
                hmiTrackForm.TopLevel = true;
                hmiTrackForm.BringToFront();
            }
        }

        private void YardForm_Click(object sender, EventArgs e)
        {
            SiebwaldeAppLogging("Main: Show Yard interface");
        }

        public void SiebwaldeAppLogging(string text)
        {
            SiebwaldeApplicationMainLogging.StoreText(text);
            string fmt = "000";
            int m_Millisecond = DateTime.Now.Millisecond;
            string m_text = DateTime.Now + ":" + m_Millisecond.ToString(fmt) + " " + text + " " + Environment.NewLine;
            SiebwaldeAppLog.AppendText(m_text);            
        }
               
        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (FYcontroller != null)
            {
                SiebwaldeAppLogging("Main: FiddleYard Controller stopping...");
                FYcontroller.Stop();
            }
            if (MTcontroller != null)
            {
                SiebwaldeAppLogging("Main: FiddleYard Controller stopping...");
                MTcontroller.Stop();
            }
            if (YDcontroller != null)
            {
                SiebwaldeAppLogging("Main: FiddleYard Controller stopping...");
                YDcontroller.Stop();
            }
            SiebwaldeAppLogging("Main: Exit Main Siebwalde Application ");
            Application.Exit();
        }

        private void reConnectFiddleYardToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            if (FYcontroller != null)
            {
                FYcontroller.ReConnect();
            }
            if (MTcontroller != null)
            {
                //MTcontroller.ReConnect();
            }
            if (YDcontroller != null)
            {
                YDcontroller.ReConnect();
            }
        }

        private void clearEventLoggersToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            SiebwaldeAppLog.Clear();
            if (FYcontroller != null)
            {
                FYcontroller.ClearEventLoggers();
            }
            if (MTcontroller != null)
            {
                //MTcontroller.ClearEventLoggers();
            }
            if (YDcontroller != null)
            {
                //YDcontroller.ClearEventLoggers();
            }
        }

        private void hardResetFiddleYardToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox about_box = new AboutBox();            
            about_box.Show();
        }        

        public void FYLinkActivityUpdate()
        {
            if (FYLinkActivity.InvokeRequired)
            {
                ToggleCommLinkCallback d = new ToggleCommLinkCallback(FYLinkActivityUpdate);
                FYLinkActivity.Invoke(d, new object[] { });  // invoking itself
            }
            else
            {
                if (FYLinkActivity.Value >= LINKACTMAX)
                {
                    FYLinkActivity.Value = 0;                    
                }                
                FYLinkActivity.Value++;
            }
        }

        private void fiddleYardSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FYSettingsForm = new FiddleYardSettingsForm();
        }
    }
}
