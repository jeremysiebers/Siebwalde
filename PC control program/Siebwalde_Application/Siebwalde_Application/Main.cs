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

    public delegate void SetTextCallback(string text, int Layer, int Indicator, int Val);  // defines a delegate type
    public delegate void ToggleCommLinkCallback();

    public partial class Main : Form
    {
        public Controller _controller1;

        public UdpClient sendingUdpClient = new UdpClient(28671);

        public string path = @"c:\localdata\Logging.txt";

        public System.Timers.Timer aTimer = new System.Timers.Timer();

        public const int SEND_DELAY = 10;

        public bool Led_CommLink_Toggle = false;
                
        #region Constructor

        private const int TOP = 1;
        private const int BOTTOM = 0;
        

        public Main()
        {
            InitializeComponent();


            
            aTimer.Elapsed += new ElapsedEventHandler(StartCommunication);
            aTimer.Interval = 1000;
            aTimer.Enabled = true;
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

        private void StartCommunication(object source, ElapsedEventArgs e) //SetText_ReceivedCmd(string text, int Layer, int Indicator, int Val)
        {
            aTimer.Enabled = false;
            try
            {
                SetText_ReceivedCmd(DateTime.Now + " ### SIEBWALDE APP STARTED! ###" + Environment.NewLine, 1, 0, 0);

                string macAddr =
                    (
                        from nic in NetworkInterface.GetAllNetworkInterfaces()
                        where nic.OperationalStatus == OperationalStatus.Up
                        select nic.GetPhysicalAddress().ToString()
                    ).FirstOrDefault();

                string ipAddr = LocalIPAddress();

                SetText_ReceivedCmd(DateTime.Now + " PC MAC adress is: " + macAddr + Environment.NewLine, 1, 0, 0);
                SetText_ReceivedCmd(DateTime.Now + " PC IP adress is: " + ipAddr + Environment.NewLine, 1, 0, 0);

                int poort1 = 28672;
                SetText_ReceivedCmd(DateTime.Now + " Listening UDP Port is: " + Convert.ToString(poort1) + Environment.NewLine, 1, 0, 0);
                SetText_ReceivedCmd(DateTime.Now + " Starting controller..." + Environment.NewLine, 1, 0, 0);
                _controller1 = new Controller(poort1, SetText_ReceivedCmd, Toggle_Comm_Link);
                _controller1.Start();
                SetText_ReceivedCmd(DateTime.Now + " Controller started." + Environment.NewLine, 1, 0, 0);

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
            sendingUdpClient.Close();
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
            ReceivedCmdTOP.Clear();
            ReceivedCmdBOTTOM.Clear();
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
                SetText_ReceivedCmd(DateTime.Now + " Hard reset of FIDDLEYARD!!!." + Environment.NewLine, 1, 0, 0);
                byte[] Send = new byte[3];
                Send[0] = Convert.ToByte('s');
                Send[1] = Convert.ToByte(0x01);
                Send[2] = 0x0D; // send CR afterwards
                SendUdp(Send);
            }
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox about_box = new AboutBox();
            about_box.Show();
        }

        #endregion Constructor     
   
        #region Connect Fiddle yard, Program MAC and IP

        private void ConnectFiddleYard(string macAddr, string ipAddr)
        {
            try
            {
                //Creates a UdpClient for sending data.
                SetText_ReceivedCmd(DateTime.Now + " Connecting to FIDDLEYARD..." + Environment.NewLine, 1, 0, 0);

                sendingUdpClient.Connect("FIDDLEYARD", 28671);

                SetText_ReceivedCmd(DateTime.Now + " Connected to FIDDLEYARD." + Environment.NewLine, 1, 0, 0);
                SetText_ReceivedCmd(DateTime.Now + " Send FIDDLEYARD MAC and IP..." + Environment.NewLine, 1, 0, 0);

                ProgramMAC(macAddr);

                SetText_ReceivedCmd(DateTime.Now + " MAC is sent." + Environment.NewLine, 1, 0, 0);

                ProgramIP(ipAddr);

                SetText_ReceivedCmd(DateTime.Now + " IP is sent." + Environment.NewLine, 1, 0, 0);

                ProgramReady();

                SetText_ReceivedCmd(DateTime.Now + " MAC_IP_READY is sent." + Environment.NewLine, 1, 0, 0);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                SetText_ReceivedCmd(DateTime.Now + " FIDDLEYARD not found..." + Environment.NewLine, 1, 0, 0);
                sendingUdpClient.Connect("LocalHost", 28671);                                                       //Fiddle Yard not found, swiching to localhost...
                SetText_ReceivedCmd(DateTime.Now + " Connected to LocalHost." + Environment.NewLine, 1, 0, 0);
            }
        }

        private void SendUdp(byte[] Send)
        {
            sendingUdpClient.Send(Send, Send.Length);
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
                SendUdp(Send);

                //SetText_ReceivedCmd(DateTime.Now + " Send Mac: " + macAddr[i] + Environment.NewLine, 1, 0, 0);  // for debugging

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
            SendUdp(Send);

            //SetText_ReceivedCmd(DateTime.Now + " " + " Send Ip: " + Convert.ToString(Send[1]) + Environment.NewLine, 1, 0, 0);  // for debugging

            System.Threading.Thread.Sleep(SEND_DELAY);


            IpSend = "";
            for (int scan = Dot[0] + 1; scan <= (Dot[1] - 1); scan++)   // Second part of the IP number
            {
                IpSend += ipAddr[scan];
            }
            Send[0] = Convert.ToByte('7');
            Send[1] = Convert.ToByte(IpSend);
            Send[2] = 0x0D; // send CR afterwards
            SendUdp(Send);

            //SetText_ReceivedCmd(DateTime.Now + " " + " Send Ip: " + Convert.ToString(Send[1]) + Environment.NewLine, 1, 0, 0);  // for debugging

            System.Threading.Thread.Sleep(SEND_DELAY);


            IpSend = "";
            for (int scan = Dot[1] + 1; scan <= (Dot[2] - 1); scan++)   // Third part of the IP number
            {
                IpSend += ipAddr[scan];
            }
            Send[0] = Convert.ToByte('8');
            Send[1] = Convert.ToByte(IpSend);
            Send[2] = 0x0D; // send CR afterwards
            SendUdp(Send);

            //SetText_ReceivedCmd(DateTime.Now + " " + " Send Ip: " + Convert.ToString(Send[1]) + Environment.NewLine, 1, 0, 0);  // for debugging

            System.Threading.Thread.Sleep(SEND_DELAY);


            IpSend = "";
            for (int scan = Dot[2] + 1; scan <= Length; scan++)   // Fourth part of the IP number
            {
                IpSend += ipAddr[scan];
            }
            Send[0] = Convert.ToByte('9');
            Send[1] = Convert.ToByte(IpSend);
            Send[2] = 0x0D; // send CR afterwards
            SendUdp(Send);

            //SetText_ReceivedCmd(DateTime.Now + " " + " Send Ip: " + Convert.ToString(Send[1]) + Environment.NewLine, 1, 0, 0);  // for debugging

            System.Threading.Thread.Sleep(SEND_DELAY);

        }

        private void ProgramReady()
        {
            byte[] Send = new byte[3];
            Send[0] = Convert.ToByte('t');
            Send[1] = 0x1;
            Send[2] = 0xD;
            SendUdp(Send);

            //SetText_ReceivedCmd(DateTime.Now + " Send Prog_Ready: " + Encoding.UTF8.GetString(Send) + Environment.NewLine, 1, 0, 0);  // for debugging
        }

        #endregion Connect Fiddle yard, Program MAC and IP

        #region Received Data to Event Logger

        private void SetText_ReceivedCmd(string text, int Layer, int Indicator, int Val)
        {   
            if (Layer == TOP)
            {

                if (ReceivedCmdTOP.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(SetText_ReceivedCmd);
                    ReceivedCmdTOP.Invoke(d, new object[] { text, Layer, Indicator, Val });  // invoking itself
                }
                else
                {
                    ReceivedCmdTOP.AppendText(text);      // the "functional part", executing only on the main thread
                    byte[] info = new UTF8Encoding(true).GetBytes(text);
                    StoreText(text, "TOP ");
                    SetLedIndicator(Layer, Indicator, Val);                    
                }
            }

            if (Layer == BOTTOM)
            {

                if (ReceivedCmdBOTTOM.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(SetText_ReceivedCmd);
                    ReceivedCmdBOTTOM.Invoke(d, new object[] { text, Layer, Indicator, Val });  // invoking itself
                }
                else
                {
                    ReceivedCmdBOTTOM.AppendText(text);      // the "functional part", executing only on the main thread
                    byte[] info = new UTF8Encoding(true).GetBytes(text);
                    StoreText(text, "BOT ");
                    SetLedIndicator(Layer, Indicator, Val);                    
                }
            }            
        }

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

        private void StoreText(string text, string Layer)
        {
            try
            {

                using (var fs = new FileStream(path, FileMode.Append))
                {
                    Byte[] info =
                        new UTF8Encoding(true).GetBytes(Layer + text);
                    fs.Write(info, 0, info.Length);
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion Received Data to Event Logger

        #region Set Led indicators

        private void SetLedIndicator(int Layer, int Indicator, int Val)
        {

            #region Set Led indicators TOP

            if (Layer == TOP)
            {
                switch (Indicator)
                {
                    case 1: if (Val >= 1)
                        {
                            Led_Heart_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Heart_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 2: 
                        break;

                    case 3: if (Val >= 1)
                        {
                            Led_F11_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F11_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 4: if (Val >= 1)
                        {
                            Led_EOS10_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_EOS10_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 5: if (Val >= 1)
                        {
                            Led_EOS11_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_EOS11_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 6: 
                        break;

                    case 7: if (Val >= 1)
                        {
                            Led_F13_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F13_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 8: if (Val >= 1)
                        {
                            Led_F12_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F12_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 9: if (Val >= 1)
                        {
                            Led_Block5B_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block5B_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 10: if (Val >= 1)
                        {
                            Led_Block8A_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block8A_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 11: if (Val >= 1)
                        {
                            Led_TrackPower_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_TrackPower_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 12: if (Val >= 1)
                        {
                            Led_Block5BIn_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block5BIn_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 13: if (Val >= 1)
                        {
                            Led_Block6In_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block6In_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 14: if (Val >= 1)
                        {
                            Led_Block7In_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block7In_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 15: if (Val >= 1)
                        {
                            Led_Resistor_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Resistor_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 16: if (Val >= 1)
                        {
                            Led_Track1_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track1_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 17: if (Val >= 1)
                        {
                            Led_Track2_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track2_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 18: if (Val >= 1)
                        {
                            Led_Track3_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track3_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 19: if (Val >= 1)
                        {
                            Led_Track4_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track4_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 20: if (Val >= 1)
                        {
                            Led_Track5_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track5_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 21: if (Val >= 1)
                        {
                            Led_Track6_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track6_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 22: if (Val >= 1)
                        {
                            Led_Track7_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track7_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 23: if (Val >= 1)
                        {
                            Led_Track8_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track8_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 24: if (Val >= 1)
                        {
                            Led_Track9_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track9_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 25: if (Val >= 1)
                        {
                            Led_Track10_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track10_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 26: if (Val >= 1)
                        {
                            Led_Track11_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track11_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 27: if (Val >= 1)
                        {
                            Led_Block6_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block6_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 28: if (Val >= 1)
                        {
                            Led_Block7_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block7_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 29: 
                        break;

                    case 30: if (Val >= 1)
                        {
                            Led_F10_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F10_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 31: if (Val >= 1)
                        {
                            Led_M10_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_M10_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 32: Track_No_TOP.Text = Convert.ToString(Val);
                        break;

                    default: break;
                }

            }

            #endregion Set Led indicators TOP

            #region Set Led indicators BOTTOM

            if (Layer == BOTTOM)
            {
                switch (Indicator)
                {
                    case 1: if (Val >= 1)
                        {
                            Led_Heart_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Heart_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 2:
                        break;

                    case 3: if (Val >= 1)
                        {
                            Led_F21_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F21_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 4: if (Val >= 1)
                        {
                            Led_EOS20_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_EOS20_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 5: if (Val >= 1)
                        {
                            Led_EOS21_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_EOS21_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 6:
                        break;

                    case 7: if (Val >= 1)
                        {
                            Led_F23_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F23_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 8: if (Val >= 1)
                        {
                            Led_F22_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F22_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 9: if (Val >= 1)
                        {
                            Led_Block16B_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block16B_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 10: if (Val >= 1)
                        {
                            Led_Block19A_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block19A_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 11: if (Val >= 1)
                        {
                            Led_TrackPower_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_TrackPower_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 12: if (Val >= 1)
                        {
                            Led_Block16BIn_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block16BIn_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 13: if (Val >= 1)
                        {
                            Led_Block17In_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block17In_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 14: if (Val >= 1)
                        {
                            Led_Block18In_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block18In_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 15: if (Val >= 1)
                        {
                            Led_Resistor_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Resistor_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 16: if (Val >= 1)
                        {
                            Led_Track1_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track1_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 17: if (Val >= 1)
                        {
                            Led_Track2_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track2_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 18: if (Val >= 1)
                        {
                            Led_Track3_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track3_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 19: if (Val >= 1)
                        {
                            Led_Track4_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track4_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 20: if (Val >= 1)
                        {
                            Led_Track5_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track5_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 21: if (Val >= 1)
                        {
                            Led_Track6_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track6_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 22: if (Val >= 1)
                        {
                            Led_Track7_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track7_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 23: if (Val >= 1)
                        {
                            Led_Track8_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track8_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 24: if (Val >= 1)
                        {
                            Led_Track9_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track9_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 25: if (Val >= 1)
                        {
                            Led_Track10_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track10_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 26: if (Val >= 1)
                        {
                            Led_Track11_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track11_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 27: if (Val >= 1)
                        {
                            Led_Block17_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block17_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 28: if (Val >= 1)
                        {
                            Led_Block18_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block18_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 29:if (Val >= 1)
                        {
                            Led_TrackPower.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_TrackPower.BackColor = Color.Transparent;
                        }
                        break;

                    case 30: if (Val >= 1)
                        {
                            Led_F20_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F20_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 31: if (Val >= 1)
                        {
                            Led_M20_BOT.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_M20_BOT.BackColor = Color.Transparent;
                        }
                        break;

                    case 32: Track_No_BOT.Text = Convert.ToString(Val);
                        break;

                    default: break;
                }

            }
            #endregion Set Led indicators BOTTOM
        }

        #endregion Set Led indicators

        #region Form Button Handler TOP

        string[] CmdTOP = new string[27] {
            "a" + "1" + "\r",               //Track On
            "a" + "2" + "\r",               //Track Off
            "a" + "3" + "\r",               //Fiddle Track to the left track ++
            "a" + "4" + "\r",               //Fiddle Track to the right track --
            "a" + "5" + "\r",               //To track 1
            "a" + "6" + "\r",               //To track 2
            "a" + "7" + "\r",               //To track 3
            "a" + "8" + "\r",               //To track 4
            "a" + "9" + "\r",               //To track 5
            "a" + "A" + "\r",               //To track 6
            "a" + "B" + "\r",               //To track 7
            "a" + "C" + "\r",               //To track 8
            "a" + "D" + "\r",               //To track 9
            "a" + "E" + "\r",               //To track 10
            "a" + "F" + "\r",               //To track 11
            "a" + "G" + "\r",               //Train Detection
            "a" + "H" + "\r",               //Start Fiddle Yard
            "a" + "I" + "\r",               //Stop Fiddle Yard
            "a" + "J" + "\r",               //Stop Fiddle Yard Now (Reset)
            "a" + "K" + "\r",               //Bezet_In_5B_Switch_On
            "a" + "L" + "\r",               //Bezet_In_5B_Switch_Off
            "a" + "M" + "\r",               //Bezet_In_6_Switch_On
            "a" + "N" + "\r",               //Bezet_In_6_Switch_Off
            "a" + "O" + "\r",               //Bezet_In_7_Switch_On
            "a" + "P" + "\r",               //Bezet_In_7_Switch_Off
            "a" + "Q" + "\r",               //Resume previous operation
            "a" + "R" + "\r"                //Start Collect
        };

        
        private void TOP_Button_Cmd_Sender(int i)
        {
            byte[] ByteToSend = Encoding.ASCII.GetBytes(CmdTOP[i]);
            sendingUdpClient.Send(ByteToSend, ByteToSend.Length);
        }
               
        private void Btn_Bridge_Open_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(0);
        }

        private void Btn_Bridge_Close_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(1);
        }

        private void Btn_Track_Plus_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(2);
        }

        private void Btn_Track_Min_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(3);
        }

        private void Nuo_Track_No_TOP_ValueChanged(object sender, EventArgs e)
        {
            if (Nuo_Track_No_TOP.Value < 1)
            {
                Nuo_Track_No_TOP.Value = 1;
            }
            if (Nuo_Track_No_TOP.Value > 11)
            {
                Nuo_Track_No_TOP.Value = 11;
            }
        } 

        private void Btn_Go_To_Track_TOP_Click(object sender, EventArgs e)
        {
            int i = Convert.ToInt16(Nuo_Track_No_TOP.Value + 3);
            TOP_Button_Cmd_Sender(i);
        }

        private void Btn_Train_Detect_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(15);
        }

        private void Btn_Start_Fiddle_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(16);
        }

        private void Btn_Stop_Fiddle_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(17);
        }

        private void Btn_Reset_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(18);
        }

        private void Btn_Bezet5BOn_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(19);
        }

        private void Btn_Bezet5BOff_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(20);
        }

        private void Btn_Bezet6On_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(21);
        }

        private void Btn_Bezet6Off_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(22);
        }

        private void Btn_Bezet7On_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(23);
        }

        private void Btn_Bezet7Off_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(24);
        }

        private void Btn_Recovered_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(25);
        }

        private void Btn_Collect_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(26);
        }
        #endregion Form Button Handler TOP

        #region Form Button Handler BOTTOM

        string[] CmdBOTTOM = new string[27] {
            "b" + "1" + "\r",               //Bridge Open
            "b" + "2" + "\r",               //Brdige Close
            "b" + "3" + "\r",               //Fiddle Track to the left track ++
            "b" + "4" + "\r",               //Fiddle Track to the right track --
            "b" + "5" + "\r",               //To track 1
            "b" + "6" + "\r",               //To track 2
            "b" + "7" + "\r",               //To track 3
            "b" + "8" + "\r",               //To track 4
            "b" + "9" + "\r",               //To track 5
            "b" + "A" + "\r",               //To track 6
            "b" + "B" + "\r",               //To track 7
            "b" + "C" + "\r",               //To track 8
            "b" + "D" + "\r",               //To track 9
            "b" + "E" + "\r",               //To track 10
            "b" + "F" + "\r",               //To track 11
            "b" + "G" + "\r",               //Train Detection
            "b" + "H" + "\r",               //Start Fiddle Yard
            "b" + "I" + "\r",               //SBOTTOM Fiddle Yard
            "b" + "J" + "\r",               //SBOTTOM Fiddle Yard Now (Reset)
            "b" + "K" + "\r",               //Bezet_In_5B_Switch_On
            "b" + "L" + "\r",               //Bezet_In_5B_Switch_Off
            "b" + "M" + "\r",               //Bezet_In_6_Switch_On
            "b" + "N" + "\r",               //Bezet_In_6_Switch_Off
            "b" + "O" + "\r",               //Bezet_In_7_Switch_On
            "b" + "P" + "\r",               //Bezet_In_7_Switch_Off
            "b" + "Q" + "\r",               //Resume previous operation
            "b" + "R" + "\r"                //Start Collect
        };

        private void BOTTOM_Button_Cmd_Sender(int i)
        {
            byte[] ByteToSend = Encoding.ASCII.GetBytes(CmdBOTTOM[i]);
            sendingUdpClient.Send(ByteToSend, ByteToSend.Length);
        }

        private void Btn_Bridge_Open_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(0);
        }

        private void Btn_Bridge_Close_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(1);
        }

        private void Btn_Track_Plus_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(2);
        }

        private void Btn_Track_Min_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(3);
        }

        private void Nuo_Track_No_BOTTOM_ValueChanged_1(object sender, EventArgs e)
        {
            if (Nuo_Track_No_BOTTOM.Value < 1)
            {
                Nuo_Track_No_BOTTOM.Value = 1;
            }
            if (Nuo_Track_No_BOTTOM.Value > 11)
            {
                Nuo_Track_No_BOTTOM.Value = 11;
            }
        }

        private void Btn_Go_To_Track_BOTTOM_Click_1(object sender, EventArgs e)
        {
            int i = Convert.ToInt16(Nuo_Track_No_BOTTOM.Value + 3);
            BOTTOM_Button_Cmd_Sender(i);
        }

        private void Btn_Train_Detect_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(15);
        }

        private void Btn_Start_Fiddle_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(16);
        }

        private void Btn_Stop_Fiddle_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(17);
        }

        private void Btn_Reset_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(18);
        }

        private void Btn_Bezet5BOn_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(19);
        }

        private void Btn_Bezet5BOff_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(20);
        }

        private void Btn_Bezet6On_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(21);
        }

        private void Btn_Bezet6Off_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(22);
        }

        private void Btn_Bezet7On_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(23);
        }

        private void Btn_Bezet7Off_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(24);
        }

        private void Btn_Recovered_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(25);
        }

        private void Btn_Collect_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(26);
        }
        #endregion Form Button Handler BOTTOM  

                      
    }
}
