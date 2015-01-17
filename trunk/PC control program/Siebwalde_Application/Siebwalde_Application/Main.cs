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
    
    public partial class Main : Form
    {
        public Sender FYSender = new Sender("FIDDLEYARD");
        public Sender LHSender = new Sender("LocalHost");
        public FiddleYardController FYcontroller;
        public FiddleYardForm FYTOP = new FiddleYardForm();
        public FiddleYardForm FYBOT = new FiddleYardForm();
        

        public const int SEND_DELAY = 10;
        
        public bool Led_CommLink_Toggle = false;

        public const int TOP = 1;
        public const int BOTTOM = 0;

        private const int LINKACTMAX = 100;
                
        #region Constructor

        public Main()
        {
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);
            //this.Width = SystemInformation.VirtualScreen.Width;
            InitializeComponent();
            FYTOP.Name = "FiddleYardTOP";
            FYBOT.Name = "FiddleYardBOT";
            FYTOP.Show();
            FYTOP.Hide();
            FYBOT.Show();
            FYBOT.Hide();
            FYTOP.Start(FYSender.SendUdp, FYSender.StoreText);
            FYBOT.Start(FYSender.SendUdp, FYSender.StoreText);
                                   
            
            LinkActivity.Minimum = 0;
            LinkActivity.Maximum = LINKACTMAX;
            LinkActivity.Step = 1;
            LinkActivity.Value = 0;

            
        }
        
        public string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }



        private void StartCommunication()//(object source, ElapsedEventArgs e) //SetText_ReceivedCmdTOP(string text, int Layer, int Indicator, int Val)
        {               
            try
            {
                string macAddr =
                    (
                        from nic in NetworkInterface.GetAllNetworkInterfaces()
                        where nic.OperationalStatus == OperationalStatus.Up
                        select nic.GetPhysicalAddress().ToString()
                    ).FirstOrDefault();

                string ipAddr = LocalIPAddress();

                SiebwaldeAppLogging(DateTime.Now + " PC MAC adress is: " + macAddr + Environment.NewLine, 1, 0, 0);
                SiebwaldeAppLogging(DateTime.Now + " PC IP adress is: " + ipAddr + Environment.NewLine, 1, 0, 0);

                int poort1 = 0;

                if (ConnectFiddleYard(macAddr, ipAddr) == true)
                {

                    poort1 = 28672;
                    SiebwaldeAppLogging(DateTime.Now + " FiddleYard Listening UDP Port is: " + Convert.ToString(poort1) + Environment.NewLine, 1, 0, 0);

                }
                
                SiebwaldeAppLogging(DateTime.Now + " FiddleYard controller Starting ..." + Environment.NewLine, 1, 0, 0);
                FYcontroller = new FiddleYardController(poort1, FYTOP.SetText_ReceivedCmd, FYBOT.SetText_ReceivedCmd, Toggle_Comm_Link);
                FYcontroller.Start();               

                SiebwaldeAppLogging(DateTime.Now + " FiddleYard Controller started." + Environment.NewLine, 1, 0, 0);
                
            }
            catch (Exception ex)
            {
                //logger.error(.../.)
                MessageBox.Show(ex.Message);
            }
        } 

        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            FYSender.CloseUdp();
            Application.Exit();
        }

        private void reConnectFiddleYardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string macAddr =
                    (
                        from nic in NetworkInterface.GetAllNetworkInterfaces()
                        where nic.OperationalStatus == OperationalStatus.Up
                        select nic.GetPhysicalAddress().ToString()
                    ).FirstOrDefault();

            string ipAddr = LocalIPAddress();
            ConnectFiddleYard(macAddr, ipAddr);
        }

        private void clearEventLoggersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FYTOP.ClearReceivedCmd();
            FYBOT.ClearReceivedCmd();
            SiebwaldeAppLog.Clear();
        }

        private void hardResetFiddleYardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string message = "Do you realy want to hard-reset the FiddleYard uController?";
            const string caption = "Hard-reset of the FiddleYard uController";
            var result = MessageBox.Show(message, caption,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Stop);

            if (result == DialogResult.OK)
            {                
                byte[] Send = new byte[3];
                Send[0] = Convert.ToByte('s');
                Send[1] = Convert.ToByte(0x01);
                Send[2] = 0x0D; // send CR afterwards
                FYSender.SendUdp(Send);                
            }
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox about_box = new AboutBox();
            about_box.Show();
        }

        private void FiddleYardFormTop_Click(object sender, EventArgs e)
        {
            if (this.Height < 1199)
                FYTOP.AutoScroll = true;
            else { FYTOP.AutoScroll = false; }
            FYTOP.Location = new System.Drawing.Point(0, 75);            
            FYTOP.Height = this.Height - 60 - 20;
            FYTOP.Width = 960;
            FYTOP.FYFORMShow();            
        }

        private void FiddleYardFormBot_Click(object sender, EventArgs e)
        {
            if (this.Height < 1199)
                FYBOT.AutoScroll = true;
            else {FYBOT.AutoScroll = false;}
            FYBOT.Location = new System.Drawing.Point(960, 75);
            FYBOT.Height = this.Height - 60 - 20;
            FYBOT.Width = 960;
            FYBOT.FYFORMShow();
        }

        #endregion Constructor  
   
        private void SiebwaldeAppLogging(string text, int Layer, int Indicator, int Val)
        {
            SiebwaldeAppLog.AppendText(text);
            byte[] info = new UTF8Encoding(true).GetBytes(text);
            FYSender.StoreText(text, "MAIN ");
        }
   
        #region Connect Fiddle yard, Program MAC and IP

        private bool ConnectFiddleYard(string macAddr, string ipAddr)
        {
            try
            {
                SiebwaldeAppLogging(DateTime.Now + " FiddleYard Connecting..." + Environment.NewLine, 1, 0, 0);
                
                FYSender.ConnectUdp();
                LHSender.ConnectUdp();

                labelFiddleYard.Text = "Link Activity FiddleYard";
                FYTOP.ShowNotConnected(false);
                FYBOT.ShowNotConnected(false);

                SiebwaldeAppLogging(DateTime.Now + " FiddleYard Connected." + Environment.NewLine, 1, 0, 0);
                SiebwaldeAppLogging(DateTime.Now + " FiddleYard MAC and IP Send..." + Environment.NewLine, 1, 0, 0);
                
                ProgramMAC(macAddr);

                SiebwaldeAppLogging(DateTime.Now + " FiddleYard MAC is sent." + Environment.NewLine, 1, 0, 0);

                ProgramIP(ipAddr);

                SiebwaldeAppLogging(DateTime.Now + " FiddleYard IP is sent." + Environment.NewLine, 1, 0, 0);

                ProgramReady();

                SiebwaldeAppLogging(DateTime.Now + " FiddleYard MAC_IP_READY is sent." + Environment.NewLine, 1, 0, 0);

                return true; // connection succesfull

            }
            catch (Exception )//ex)
            {
                //MessageBox.Show(ex.Message);
                SiebwaldeAppLogging(DateTime.Now + " FiddleYard not found..." + Environment.NewLine, 1, 0, 0);
                FYSender.ConnectUdpLocalHost();
                SiebwaldeAppLogging(DateTime.Now + " Connected to LocalHost." + Environment.NewLine, 1, 0, 0);
                FYTOP.ShowNotConnected(true);
                FYBOT.ShowNotConnected(true);
                labelFiddleYard.Text = "FiddleYard in Simulation";
                return false; // no connection
            }
        }

        private void ProgramMAC(string macAddr)
        {
            string[] Identifier = new string[12] { "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5" };
            byte[] _Identifier = new byte[1];
            byte[] Send = new byte[3];
            
            for (int i = 0; i <= 11; i++)
            {
                _Identifier = Encoding.ASCII.GetBytes(Identifier[i]);
                Send[0] = _Identifier[0];
                Send[1] = Convert.ToByte(int.Parse(Convert.ToString(macAddr[i]), NumberStyles.HexNumber));
                Send[2] = 0x0D; // send CR afterwards
                FYSender.SendUdp(Send);//sendingUdpClientMain.Send(Send, Send.Length);
                LHSender.SendUdp(Send);//sendingUdpClientMain.Send(Send, Send.Length);
                                
                System.Threading.Thread.Sleep(SEND_DELAY);
            }
        }

        private void ProgramIP(string ipAddr)
        {
            // Identifiers for sending IP number are: { "6", "7", "8", "9"};
            string IpSend = "";            
            byte[] Send = new byte[3];
            
            int Length = ipAddr.Length - 1;             // start at 0 so 1 less then leght to use array indexing
            int[] Dot = new int[3];                     // dot index number in the ipAddr string
            int _DotPoint = 0;                          // counter to point to dot in ip number

            for (int Scan = 0; Scan <= Length; Scan++) // check were the dots are in the IP number
            {
                if (ipAddr[Scan] == '.')
                {
                    Dot[_DotPoint] = Scan;          // Store the location of the dot in the IP number
                    _DotPoint++;    
                }
            }

            for (int scan = 0; scan <= (Dot[0] - 1); scan++)    // First part of the IP number
            {
                IpSend += ipAddr[scan];
            }
            Send[0] = Convert.ToByte('6');
            Send[1] = Convert.ToByte(IpSend);
            Send[2] = 0x0D; // send CR afterwards
            FYSender.SendUdp(Send); //sendingUdpClientMain.Send(Send, Send.Length);

            //SetText_ReceivedCmdTOP(DateTime.Now + " " + " Send Ip: " + Convert.ToString(Send[1]) + Environment.NewLine, 1, 0, 0);  // for debugging

            System.Threading.Thread.Sleep(SEND_DELAY);


            IpSend = "";
            for (int scan = Dot[0] + 1; scan <= (Dot[1] - 1); scan++)   // Second part of the IP number
            {
                IpSend += ipAddr[scan];
            }
            Send[0] = Convert.ToByte('7');
            Send[1] = Convert.ToByte(IpSend);
            Send[2] = 0x0D; // send CR afterwards
            FYSender.SendUdp(Send); //sendingUdpClientMain.Send(Send, Send.Length);

            //SetText_ReceivedCmdTOP(DateTime.Now + " " + " Send Ip: " + Convert.ToString(Send[1]) + Environment.NewLine, 1, 0, 0);  // for debugging

            System.Threading.Thread.Sleep(SEND_DELAY);


            IpSend = "";
            for (int scan = Dot[1] + 1; scan <= (Dot[2] - 1); scan++)   // Third part of the IP number
            {
                IpSend += ipAddr[scan];
            }
            Send[0] = Convert.ToByte('8');
            Send[1] = Convert.ToByte(IpSend);
            Send[2] = 0x0D; // send CR afterwards
            FYSender.SendUdp(Send); //sendingUdpClientMain.Send(Send, Send.Length);

            //SetText_ReceivedCmdTOP(DateTime.Now + " " + " Send Ip: " + Convert.ToString(Send[1]) + Environment.NewLine, 1, 0, 0);  // for debugging

            System.Threading.Thread.Sleep(SEND_DELAY);


            IpSend = "";
            for (int scan = Dot[2] + 1; scan <= Length; scan++)   // Fourth part of the IP number
            {
                IpSend += ipAddr[scan];
            }
            Send[0] = Convert.ToByte('9');
            Send[1] = Convert.ToByte(IpSend);
            Send[2] = 0x0D; // send CR afterwards
            FYSender.SendUdp(Send); //sendingUdpClientMain.Send(Send, Send.Length);

            //SetText_ReceivedCmdTOP(DateTime.Now + " " + " Send Ip: " + Convert.ToString(Send[1]) + Environment.NewLine, 1, 0, 0);  // for debugging

            System.Threading.Thread.Sleep(SEND_DELAY);

        }

        private void ProgramReady()
        {
            byte[] Send = new byte[3];
            Send[0] = Convert.ToByte('t');
            Send[1] = 0x1;
            Send[2] = 0xD;
            FYSender.SendUdp(Send); //sendingUdpClientMain.Send(Send, Send.Length);
        }

        #endregion Connect Fiddle yard, Program MAC and IP
        
        private void Toggle_Comm_Link()
        {
            if (LinkActivity.InvokeRequired)
            {
                ToggleCommLinkCallback d = new ToggleCommLinkCallback(Toggle_Comm_Link);
                LinkActivity.Invoke(d, new object[] { });  // invoking itself
            }
            else
            {
                if (LinkActivity.Value >= LINKACTMAX)
                {
                    LinkActivity.Value = 0;                    
                }                
                LinkActivity.Value++;
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            LinkActivity.Location = new System.Drawing.Point(this.Width - LinkActivity.Width - 20, 1);
            labelFiddleYard.Location = new System.Drawing.Point(this.Width - LinkActivity.Width - 20 - labelFiddleYard.Width, 6);

            StartCommunication();
        }
                        
    }
}
