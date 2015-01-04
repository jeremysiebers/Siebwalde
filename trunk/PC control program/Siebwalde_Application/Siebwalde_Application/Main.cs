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
        public Controller _controller1;

        public FiddleYardFormTop FYTOP = new FiddleYardFormTop();
        public FiddleYardFormBot FYBOT = new FiddleYardFormBot();
        public const int SEND_DELAY = 10;
        
        public bool Led_CommLink_Toggle = false;

        public const int TOP = 1;
        public const int BOTTOM = 0;
                
        #region Constructor

        public Main()
        {
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);
            this.Width = SystemInformation.VirtualScreen.Width;
            InitializeComponent();
            FYTOP.Show();
            FYTOP.Hide();
            FYBOT.Show();
            FYBOT.Hide();            
            StartCommunication();
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

                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " PC MAC adress is: " + macAddr + Environment.NewLine, 1, 0, 0);
                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " PC IP adress is: " + ipAddr + Environment.NewLine, 1, 0, 0);

                int poort1 = 28672;
                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " Listening UDP Port is: " + Convert.ToString(poort1) + Environment.NewLine, 1, 0, 0);
                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " Starting controller..." + Environment.NewLine, 1, 0, 0);
                _controller1 = new Controller(poort1, FYTOP.SetText_ReceivedCmdTOP, FYBOT.SetText_ReceivedCmdBOT, Toggle_Comm_Link);
                _controller1.Start();
                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " Controller started." + Environment.NewLine, 1, 0, 0);

                ConnectFiddleYard(macAddr, ipAddr);

            }
            catch (Exception ex)
            {
                //logger.error(.../.)
                MessageBox.Show(ex.Message);

            }
        } 

        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            Sender.CloseUdp();
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
            FYTOP.ClearReceivedCmdTOP();
            FYBOT.ClearReceivedCmdBOTTOM();
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
                //FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " Hard reset of FIDDLEYARD!!!." + Environment.NewLine, 1, 0, 0);
                byte[] Send = new byte[3];
                Send[0] = Convert.ToByte('s');
                Send[1] = Convert.ToByte(0x01);
                Send[2] = 0x0D; // send CR afterwards
                Sender.SendUdp(Send);                
            }
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox about_box = new AboutBox();
            about_box.Show();
        }

        private void FiddleYardFormTop_Click(object sender, EventArgs e)
        {
            FYTOP.FYTOPShow();            
        }

        private void FiddleYardFormBot_Click(object sender, EventArgs e)
        {            
            FYBOT.FYBOTShow();
        }

        #endregion Constructor     
   
        #region Connect Fiddle yard, Program MAC and IP

        private void ConnectFiddleYard(string macAddr, string ipAddr)
        {
            try
            {                
                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " Connecting to FIDDLEYARD..." + Environment.NewLine, 1, 0, 0);
                //sendingUdpClientMain.Connect("FIDDLEYARD", 28671);
                Sender.ConnectUdp();
                
                                
                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " Connected to FIDDLEYARD." + Environment.NewLine, 1, 0, 0);
                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " Send FIDDLEYARD MAC and IP..." + Environment.NewLine, 1, 0, 0);
                
                ProgramMAC(macAddr);

                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " MAC is sent." + Environment.NewLine, 1, 0, 0);

                ProgramIP(ipAddr);

                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " IP is sent." + Environment.NewLine, 1, 0, 0);

                ProgramReady();

                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " MAC_IP_READY is sent." + Environment.NewLine, 1, 0, 0);


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " FIDDLEYARD not found..." + Environment.NewLine, 1, 0, 0);
                Sender.ConnectUdpLocalHost();//sendingUdpClientMain.Connect("LocalHost", 28671);//Fiddle Yard not found, swiching to localhost...                
                FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " Connected to LocalHost." + Environment.NewLine, 1, 0, 0);
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
                Sender.SendUdp(Send);//sendingUdpClientMain.Send(Send, Send.Length);

                //FYTOP.SetText_ReceivedCmdTOP(DateTime.Now + " Send Mac: " + macAddr[i] + Environment.NewLine, 1, 0, 0);  // for debugging

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
            Sender.SendUdp(Send); //sendingUdpClientMain.Send(Send, Send.Length);

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
            Sender.SendUdp(Send); //sendingUdpClientMain.Send(Send, Send.Length);

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
            Sender.SendUdp(Send); //sendingUdpClientMain.Send(Send, Send.Length);

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
            Sender.SendUdp(Send); //sendingUdpClientMain.Send(Send, Send.Length);

            //SetText_ReceivedCmdTOP(DateTime.Now + " " + " Send Ip: " + Convert.ToString(Send[1]) + Environment.NewLine, 1, 0, 0);  // for debugging

            System.Threading.Thread.Sleep(SEND_DELAY);

        }

        private void ProgramReady()
        {
            byte[] Send = new byte[3];
            Send[0] = Convert.ToByte('t');
            Send[1] = 0x1;
            Send[2] = 0xD;
            Sender.SendUdp(Send); //sendingUdpClientMain.Send(Send, Send.Length);

            //SetText_ReceivedCmdTOP(DateTime.Now + " Send Prog_Ready: " + Encoding.UTF8.GetString(Send) + Environment.NewLine, 1, 0, 0);  // for debugging
        }

        #endregion Connect Fiddle yard, Program MAC and IP
        
        private void Toggle_Comm_Link()
        {            
            Led_CommLink_Toggle = !Led_CommLink_Toggle;
            if (Led_CommLink_Toggle == true)
            {
                Led_CommLink.BackColor = Color.Fuchsia;
            }
            if (Led_CommLink_Toggle == false)
            {
                Led_CommLink.BackColor = Color.Transparent;
            }
        }

                       
    }
}
