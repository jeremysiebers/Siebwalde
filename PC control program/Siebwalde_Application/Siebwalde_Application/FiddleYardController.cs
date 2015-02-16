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
    public interface iFiddleYardController
    {        
        void FYLinkActivityUpdate();            // Update Link activity in main form
        void ClearEventLoggers();               // Clear event loggers interface to form eventloggers for clearing
        void ReConnect();                       // Re-connect to target
        FiddleYardIOHandle GetIoHandler();      // interface to pass the IOHANDLER methods to the form for subscribing to sensor updates etc.
        FiddleYardForm GetFYFormTop();          // interface to pass the FiddleYardForm methods
        FiddleYardForm GetFYFormBot();          // interface to pass the FiddleYardForm methods
        FiddleYardSimulator GetFYSimulatorTop();// interface to pass the FYSimulatorTop methods
        FiddleYardSimulator GetFYSimulatorBot();// interface to pass the FYSimulatorBot methods
        FiddleYardApplication GetFYAppTop();    // interface to pass the FYAppTop methods
        FiddleYardApplication GetFYAppBot();    // interface to pass the FYAppBot methods
    }
    
    public class FiddleYardController : iFiddleYardController
    {
        iMain m_iMain; // connect variable to connect to FYController class to Main for application logging
        public FiddleYardApplication FYAppTop;
        public FiddleYardApplication FYAppBot;
        public FiddleYardForm FYTOP = new FiddleYardForm();
        public FiddleYardForm FYBOT = new FiddleYardForm();
        public FiddleYardSimulator FYSimulatorTop;
        public FiddleYardSimulator FYSimulatorBot;        
        public const int SEND_DELAY = 10;
        public FiddleYardIOHandle FYIOHandle;
        public PingTarget m_PingTarget = new PingTarget { };
        private int m_FYReceivingPort = 0;
        private bool FYSimulatorActive = true;        
        private const int TOP = 1;
        private const int BOTTOM = 0;
        private byte[,] m_macAddr;
        private byte[,] m_ipAddr;

        public FiddleYardApplication GetFYAppTop()
        {
            return FYAppTop;
        }
        public FiddleYardApplication GetFYAppBot()
        {
            return FYAppBot;
        }
        public FiddleYardIOHandle GetIoHandler()
        {
            return FYIOHandle;
        }
        public FiddleYardForm GetFYFormTop()
        {
            return FYTOP;
        }
        public FiddleYardForm GetFYFormBot()
        {
            return FYBOT;
        }
        public FiddleYardSimulator GetFYSimulatorTop()
        {
            return FYSimulatorTop;
        }
        public FiddleYardSimulator GetFYSimulatorBot()
        {
            return FYSimulatorBot;
        }        
        public void Connect(iMain iMainCtrl)
        {
            m_iMain = iMainCtrl;    // connect to Main interface for application text logging and link activity update, save interface in variable
        }



        public FiddleYardController(byte[,] macAddr, byte[,] ipAddr, int FYReceivingPort)
        {
            m_FYReceivingPort = FYReceivingPort;
            m_macAddr = macAddr;
            m_ipAddr = ipAddr;
                        
            FYIOHandle = new FiddleYardIOHandle(m_FYReceivingPort, FYSimulatorActive, this);
            FYAppTop = new FiddleYardApplication("FiddleYardTOP", this);
            FYAppBot = new FiddleYardApplication("FiddleYardBOT", this);
            FYSimulatorTop = new FiddleYardSimulator("FiddleYardTOP", this);
            FYSimulatorBot = new FiddleYardSimulator("FiddleYardBOT", this);
            FYAppTop.Start();
            FYAppBot.Start();
            FYTOP.Name = "FiddleYardTOP";
            FYBOT.Name = "FiddleYardBOT";
            FYTOP.Show();
            FYTOP.Hide();
            FYBOT.Show();
            FYBOT.Hide();
            FYTOP.Connect(this); // connect the TOP Form to the FYController interface
            FYBOT.Connect(this); // connect the BOTTOM Form to the FYController interface            
        }

        public void Start()
        {
            ReConnect();            
        }

        public void Stop()
        {
            if (GetIoHandler().FYSender != null) // when a real connection is made, close the UDP port
            {

                //Also send stop command to uController to suspend all and to put tracks to occupied.
                //FYSender.SendUdp(Encoding.ASCII.GetBytes("a" + CmdToFY[18])); // uController program build-in-reset setting all occupied to true stopping motors etc.
                //FYSender.SendUdp(Encoding.ASCII.GetBytes("b" + CmdToFY[18])); // uController program build-in-reset setting all occupied to true stopping motors etc.
                GetIoHandler().FYSender.CloseUdp();
            }
        }

        public void ReConnect()
        {
            if (ConnectFiddleYard(m_macAddr, m_ipAddr) == true) // when connection was succesfull and target was found and is connected
            {
                m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: Fiddle Yard uController target in real mode" + Environment.NewLine);
                FYTOP.StoreText(DateTime.Now + " ###Fiddle Yard uController target in real mode###" + Environment.NewLine);
                FYTOP.SimMode(FYSimulatorActive);
                FYBOT.StoreText(DateTime.Now + " ###Fiddle Yard uController target in real mode###" + Environment.NewLine);
                FYBOT.SimMode(FYSimulatorActive);
            }
            else                                                // else start the simulator
            {
                m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: Fiddle Yard uController target in sim1 mode" + Environment.NewLine);
                FYTOP.StoreText(DateTime.Now + " ###Fiddle Yard uController target in sim1 mode###" + Environment.NewLine);
                FYTOP.SimMode(FYSimulatorActive);
                FYBOT.StoreText(DateTime.Now + " ###Fiddle Yard uController target in sim1 mode###" + Environment.NewLine);
                FYBOT.SimMode(FYSimulatorActive);
                FYSimulatorTop.Start();
                FYSimulatorBot.Start();
            }
            FYIOHandle.Start(FYSimulatorActive);
            m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: Fiddle Yard uController Reset." + Environment.NewLine);
        }

        public void ClearEventLoggers()
        {
            FYTOP.ClearReceivedCmd();
            FYBOT.ClearReceivedCmd();
        }

        public void FYLinkActivityUpdate()
        {
            m_iMain.FYLinkActivityUpdate();
        }        

        public void FYTOPShow(bool autoscroll, int height)
        {
            FYTOP.Location = new System.Drawing.Point(0, 75);            
            FYTOP.Height = height - 60 - 20;
            FYTOP.Width = 960;
            FYTOP.AutoScroll = autoscroll;
            FYTOP.FYFORMShow(); 
        }

        public void FYBOTShow(bool autoscroll, int height)
        {
            FYBOT.Location = new System.Drawing.Point(960, 75);
            FYBOT.Height = height - 60 - 20;
            FYBOT.Width = 960;
            FYBOT.AutoScroll = autoscroll;
            FYBOT.FYFORMShow();
        }
        
        private bool ConnectFiddleYard(byte[,] macAddr, byte[,] ipAddr)
        {
            string PingReturn = "";
            try
            {                
                m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: Pinging FIDDLEYARD target..." + Environment.NewLine);
                PingReturn = m_PingTarget.TargetFound(FiddleYardIOHandle.Target);
                if (PingReturn == "targetfound")
                {
                    m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: Ping successfull." + Environment.NewLine);

                    m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: FiddleYard Connecting..." + Environment.NewLine);
                    GetIoHandler().FYSender.ConnectUdp();

                    m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: FiddleYard Connected." + Environment.NewLine);
                    m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: FiddleYard Send MAC and IP..." + Environment.NewLine);

                    ProgramMACIPPORT(macAddr, ipAddr);
                    FYSimulatorActive = false;
                    return true; // connection succesfull to FIDDLEYARD
                }
                else
                {
                    m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: " + PingReturn + Environment.NewLine);                    
                    //FYSender.ConnectUdpLocalHost();
                    //m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: Connected to LocalHost." + Environment.NewLine);
                    FYSimulatorActive = true;              //<--------------------------------------- change return false to true when simulator is ready and remove all connect to localhost
                    return false; // ping was unsuccessfull    <--------------------------------------- change return true to false when simulator is ready and remove all connect to localhost
                }

            }
            catch (Exception)
            {                
                m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: FiddleYard failed to connect." + Environment.NewLine);                
                FYSimulatorActive = true;
                return false; // ping was successfull but connecting failed
            }
        } 

        private void ProgramMACIPPORT(byte[,] macAddr, byte[,] ipAddr)
        {
            byte[] Send = new byte[3];
            for (int i = 0; i < 12; i++)
            {
                Send[0] = macAddr[i, 0];
                Send[1] = macAddr[i, 1];
                Send[2] = macAddr[i, 2];
                GetIoHandler().FYSender.SendUdp(Send);
                System.Threading.Thread.Sleep(50);
            }
            m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: FiddleYard MAC is sent." + Environment.NewLine);

            for (int i = 0; i < 4; i++)
            {
                Send[0] = ipAddr[i, 0];
                Send[1] = ipAddr[i, 1];
                Send[2] = ipAddr[i, 2];
                GetIoHandler().FYSender.SendUdp(Send);
                System.Threading.Thread.Sleep(50);
            }
            m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: FiddleYard IP is sent." + Environment.NewLine);

            /* Also sent to FY uController to set the port on which it has to sent its data to the PC: m_FYReceivingPort
            Send[0] = Convert.ToByte('r');
            Send[1] = Convert.ToByte(m_FYReceivingPort >> 8);
            Send[2] = 0xD;
            FYSender.SendUdp(Send);
            
            Send[0] = Convert.ToByte('s');
            Send[1] = Convert.ToByte(m_FYReceivingPort & 0xFF00 >> 8);
            Send[2] = 0xD;
            FYSender.SendUdp(Send);
            m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: FiddleYard sending Port is sent." + Environment.NewLine);*/

            Send[0] = Convert.ToByte('t');
            Send[1] = 0x1;
            Send[2] = 0xD;
            GetIoHandler().FYSender.SendUdp(Send);
            System.Threading.Thread.Sleep(50);
            m_iMain.SiebwaldeAppLogging(DateTime.Now + " FYCTRL: FiddleYard MAC_IP_READY is sent." + Environment.NewLine);
        }               
        
    }
}
