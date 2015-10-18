using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Timers;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using System.Net.NetworkInformation;
using System.Globalization;

namespace Siebwalde_Application
{    
    public delegate void ToggleCommLinkCallback();

    public interface iMain
    {
        void SiebwaldeAppLogging(string text);
        void FYLinkActivityUpdate();
        Main GetMain();                             // interface to Main
    }
    
    public partial class Main : Form , iMain
    {
        public FiddleYardController FYcontroller;
        public FiddleYardSettingsForm FYSettingsForm;
        public FiddleYardController MTcontroller;
        public FiddleYardController YDcontroller;
        public MAC_IP_Conditioner MACIPConditioner = new MAC_IP_Conditioner { };

        private const int LINKACTMAX = 100;
        private string path = @"c:\localdata\Siebwalde\"+ DateTime.Now.Day + "-"+ DateTime.Now.Month + "-"+ DateTime.Now.Year + "_SiebwaldeApplicationMain.txt"; //  different logging file per target, this is default
        public Log2LoggingFile SiebwaldeApplicationMainLogging;

        private bool ViewTop = true;
        private bool ViewBot = true;

        public Main GetMain()
        {
            return this;
        }

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
            SiebwaldeAppLogging("########################################################################");
            SiebwaldeAppLogging("#                                                                      #");
            SiebwaldeAppLogging("#                   Siebwalde Application started.                     #");
            SiebwaldeAppLogging("#                                                                      #");
            SiebwaldeAppLogging("########################################################################");
            SiebwaldeAppLogging("Siebwalde Application started.");
            SiebwaldeAppLogging("Main: PC MAC adress is: " + MACIPConditioner.MACstring());
            SiebwaldeAppLogging("Main: PC IP adress is: " + MACIPConditioner.IPstring());

            StartApplication.Visible = false;
            LStartApplication.Visible = false;       
                                    
            /// start FY CONTROLLER ///
            StartFYController();

            /// start MT CONTROLLER ///
            //StartMTController();

            /// start YARD CONTROLLER ///
            //StartYARDController();
        }

        private void StartFYController()
        {
            int FYReceivingport = 0x7000;    
            FYcontroller = new FiddleYardController(this, MACIPConditioner.MAC(), MACIPConditioner.IP(), FYReceivingport);            
            SiebwaldeAppLogging("Main: FiddleYard Controller starting...");
            FYcontroller.Start();
            FiddleYardFormTop.Visible = true;
            FiddleYardFormBot.Visible = true;
            FYLinkActivity.Visible = true;
            LFYLinkActivity.Visible = true;
            SiebwaldeAppLogging("Main: FiddleYard Controller started.");
        }

        private void StartMTController()
        {
            int MTReceivingport = 28673;
            MTcontroller = new FiddleYardController(this, MACIPConditioner.MAC(), MACIPConditioner.IP(), MTReceivingport);
            //MTcontroller.start();
            MaintrackForm.Visible = true;
            SiebwaldeAppLogging("Main: Track Controller started.");
        }

        private void StartYARDController()
        {
            int YDReceivingport = 28674;
            YDcontroller = new FiddleYardController(this, MACIPConditioner.MAC(), MACIPConditioner.IP(), YDReceivingport);
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
                FiddleYardFormTop.Text = "Hide Fiddle Yard Top";
            }
            else 
            {
                SiebwaldeAppLogging("Main: Hide Fiddle Yard Top Layer interface");
                FiddleYardFormTop.Text = "Show Fiddle Yard Top"; 
            }
        }

        private void FiddleYardFormBot_Click(object sender, EventArgs e)
        {
            bool autoscroll = false;            
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
            SiebwaldeAppLogging("Main: Show Main Track interface");
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
                MTcontroller.ReConnect();
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
                MTcontroller.ClearEventLoggers();
            }
            if (YDcontroller != null)
            {
                YDcontroller.ClearEventLoggers();
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
