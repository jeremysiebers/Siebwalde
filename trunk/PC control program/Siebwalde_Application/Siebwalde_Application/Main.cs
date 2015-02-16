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
    }
    
    public partial class Main : Form , iMain
    {
        public FiddleYardController FYcontroller;
        public FiddleYardController MTcontroller;
        public FiddleYardController YDcontroller;
        public MAC_IP_Conditioner MACIPConditioner = new MAC_IP_Conditioner { };

        private const int LINKACTMAX = 100;
        private string path = @"c:\localdata\SiebwaldeAppLogging.txt"; // different logging file per target, this is default
        
        public Main()
        {
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
            SiebwaldeAppLogging("##########################################################" + Environment.NewLine);
            SiebwaldeAppLogging(DateTime.Now + " Main: Siebwalde Application started." + Environment.NewLine);
            SiebwaldeAppLogging(DateTime.Now + " Main: PC MAC adress is: " + MACIPConditioner.MACstring() + Environment.NewLine);
            SiebwaldeAppLogging(DateTime.Now + " Main: PC IP adress is: " + MACIPConditioner.IPstring() + Environment.NewLine);

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
            FYcontroller = new FiddleYardController(MACIPConditioner.MAC(), MACIPConditioner.IP(), FYReceivingport);
            FYcontroller.Connect(this);
            SiebwaldeAppLogging(DateTime.Now + " Main: FiddleYard Controller starting..." + Environment.NewLine);
            FYcontroller.Start();
            FiddleYardFormTop.Visible = true;
            FiddleYardFormBot.Visible = true;
            FYLinkActivity.Visible = true;
            LFYLinkActivity.Visible = true;
            SiebwaldeAppLogging(DateTime.Now + " Main: FiddleYard Controller started." + Environment.NewLine);
        }

        private void StartMTController()
        {
            int MTReceivingport = 28673;
            MTcontroller = new FiddleYardController(MACIPConditioner.MAC(), MACIPConditioner.IP(), MTReceivingport);
            //MTcontroller.start();
            MaintrackForm.Visible = true;
            SiebwaldeAppLogging(DateTime.Now + " Main: Track Controller started." + Environment.NewLine);
        }

        private void StartYARDController()
        {
            int YDReceivingport = 28674;
            YDcontroller = new FiddleYardController(MACIPConditioner.MAC(), MACIPConditioner.IP(), YDReceivingport);
            //YDcontroller.start();
            YardForm.Visible = true;
            SiebwaldeAppLogging(DateTime.Now + " Main: Yard Controller started." + Environment.NewLine);
        }

        private void FiddleYardFormTop_Click(object sender, EventArgs e)
        {
            bool autoscroll = false;
            SiebwaldeAppLogging(DateTime.Now + " Main: Show Fiddle Yard Top Layer interface" + Environment.NewLine);
            if (this.Height < 1199)
                autoscroll = true;
            else { autoscroll = false; }
            FYcontroller.FYTOPShow(autoscroll, this.Height);
        }

        private void FiddleYardFormBot_Click(object sender, EventArgs e)
        {
            bool autoscroll = false;
            SiebwaldeAppLogging(DateTime.Now + " Main: Show Fiddle Yard Bottom Layer interface" + Environment.NewLine);
            if (this.Height < 1199)
                autoscroll = true;
            else { autoscroll = false; }
            FYcontroller.FYBOTShow(autoscroll, this.Height);
        }

        private void MaintrackForm_Click(object sender, EventArgs e)
        {
            SiebwaldeAppLogging(DateTime.Now + " Main: Show Main Track interface" + Environment.NewLine);
        }

        private void YardForm_Click(object sender, EventArgs e)
        {
            SiebwaldeAppLogging(DateTime.Now + " Main: Show Yard interface" + Environment.NewLine);
        }

        public void SiebwaldeAppLogging(string text)
        {
            SiebwaldeAppLog.AppendText(text);
            StoreText(text);
        }
               
        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (FYcontroller != null)
            {
                SiebwaldeAppLogging(DateTime.Now + " Main: FiddleYard Controller stopping..." + Environment.NewLine);
                FYcontroller.Stop();
            }
            if (MTcontroller != null)
            {
                SiebwaldeAppLogging(DateTime.Now + " Main: FiddleYard Controller stopping..." + Environment.NewLine);
                MTcontroller.Stop();
            }
            if (YDcontroller != null)
            {
                SiebwaldeAppLogging(DateTime.Now + " Main: FiddleYard Controller stopping..." + Environment.NewLine);
                YDcontroller.Stop();
            }
            SiebwaldeAppLogging(DateTime.Now + " Main: Exit Main Siebwalde Application " + Environment.NewLine);
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

        public void StoreText(string text)
        {
            try
            {
                
                using (var fs = new FileStream(path, FileMode.Append))
                {
                    Byte[] info =
                        new UTF8Encoding(true).GetBytes(text);
                    fs.Write(info, 0, info.Length);
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
    }
}
