using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Siebwalde_Application.TrackApplication
{
    public interface iTrackController
    {
        //void FYLinkActivityUpdate();                // Update Link activity in main form
        void ClearEventLoggers();                   // Clear event loggers interface to form eventloggers for clearing
        //void ReConnect();                           // Re-connect to target
        Sender GetMTSender();                       // interface to MTSender
        Receiver GetMTReceiver();                   // interface to MTReceiver        
    }

    public class TrackController : iTrackController
    {
        public iMain m_iMain;                                       // connect variable to connect to FYController class to Main for application logging
        public const string TrackTarget = "TRACKCONTROL";
        public Sender MTSender = new Sender(TrackTarget);
        private Receiver MTReceiver;
        public PingTarget m_PingTarget = new PingTarget { };
        private int m_MTReceivingPort = 0;
        private int m_MTSendingPort   = 0;
        public TrackIOHandle MTIOHandle;

        public Sender GetMTSender()
        {
            return MTSender;
        }

        public Receiver GetMTReceiver()
        {
            return MTReceiver;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MainTrackController
         *               Constructor
         *              
         *               
         *
         *  Input(s)   : MacAddress, IPAddress, Receiving port for main trak controller
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
        public TrackController(iMain iMainCtrl, int MTReceivingPort, int MTSendingPort)
        {
            m_iMain = iMainCtrl;                        // connect to Main interface for application text logging and link activity update, save interface in variable
            m_MTReceivingPort = MTReceivingPort;
            m_MTSendingPort = MTSendingPort;
            MTReceiver = new Receiver(m_MTReceivingPort);
            MTIOHandle = new TrackIOHandle(this);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MainTrackController start
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
        public void Start()
        {
            if (ConnectTrackConntroller() == true) // when connection was succesfull and target was found and is connected
            {
                m_iMain.SiebwaldeAppLogging("MTCTRL: Track uController target in real mode");
                MTReceiver.Start();
            }
            else
            {
                m_iMain.SiebwaldeAppLogging("MTCTRL: Track uController target in simulator mode");                
            }
            MTIOHandle.Start();

        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardController ConnectFiddleYard
         *               
         *
         *  Input(s)   : macAddr, ipAddr
         *
         *  Output(s)  : FYSimulatorActive
         *
         *  Returns    : ConnectFiddleYard
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      :
         */
        /*#--------------------------------------------------------------------------#*/
        private bool ConnectTrackConntroller()
        {
            string PingReturn = "";
            try
            {
                m_iMain.SiebwaldeAppLogging("MTCTRL: Pinging Track controller target...");
                PingReturn = m_PingTarget.TargetFound(TrackTarget);                
                if (PingReturn == "targetfound")
                {
                    m_iMain.SiebwaldeAppLogging("MTCTRL: Ping successfull.");

                    m_iMain.SiebwaldeAppLogging("MTCTRL: Track controller Connecting...");
                    MTSender.ConnectUdp(m_MTSendingPort);

                    m_iMain.SiebwaldeAppLogging("MTCTRL: Track controller Connected.");
                    m_iMain.SiebwaldeAppLogging("MTCTRL: Track controller Send MAC and IP...");
                                        
                    return true; // connection succesfull to FIDDLEYARD
                }
                else
                {
                    var address = Dns.GetHostEntry(TrackTarget).AddressList.First();
                    if (address != null)
                    {
                        m_iMain.SiebwaldeAppLogging("MTCTRL: Dns.GetHostEntry successfull.");
                        m_iMain.SiebwaldeAppLogging("MTCTRL: Track controller Connecting...");
                        MTSender.ConnectUdp(m_MTSendingPort);
                        m_iMain.SiebwaldeAppLogging("MTCTRL: Track controller Connected.");
                        return true; // connection succesfull to FIDDLEYARD
                    }
                    else
                    {
                        m_iMain.SiebwaldeAppLogging("MTCTRL: " + PingReturn);
                        return false; // ping was unsuccessfull
                    }
                     
                }

            }
            catch (Exception)
            {
                m_iMain.SiebwaldeAppLogging("MTCTRL: TrackController failed to connect.");                
                return false; // ping was successfull but connecting failed
            }
        }

        public void Stop()
        {

        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardController ClearEventLoggers
         *               
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
        public void ClearEventLoggers()
        {
            
        }
    }
}
