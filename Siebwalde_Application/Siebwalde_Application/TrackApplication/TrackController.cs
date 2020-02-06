using System;
using System.Linq;
using System.Net;

namespace Siebwalde_Application
{

    public class TrackController
    {
        /* connect variable to connect to FYController class to Main for application logging */
        public iMain m_iMain;

        public PingTarget m_PingTarget = new PingTarget { };

        /* Data */
        public TrackIOHandle trackIOHandle;
        /* Public Enums */
        public PublicEnums mPublicEnums;
        /* ViewModel */
        public TrackAmplifierItemViewModel TrackAmplifierItemViewModel;

        public int mTrackSendingPort;
        public int mTrackReceivingPort;

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
        public TrackController(iMain iMainCtrl, int TrackReceivingPort, int TrackSendingPort)
        {
            /*
             * connect to Main interface for application text logging and link activity 
             * update, save interface in variable.
             */
            m_iMain = iMainCtrl;
            mTrackReceivingPort = TrackReceivingPort;
            mTrackSendingPort = TrackSendingPort;

            mPublicEnums = new PublicEnums();

            /*
             * Public ENUM init and pass to all
             */

            trackIOHandle = new TrackIOHandle(mTrackReceivingPort, mTrackSendingPort);

            TrackAmplifierItemViewModel = new TrackAmplifierItemViewModel(trackIOHandle);
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
                m_iMain.SiebwaldeAppLogging("MTCTRL: Track uController target in real mode.");
                //mTrackApplicationVariables.Start();
                trackIOHandle.Start();
            }
            else
            {
                m_iMain.SiebwaldeAppLogging("MTCTRL: Track uController target in simulator mode, not started!");                
            }
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
                PingReturn = m_PingTarget.TargetFound(mPublicEnums.TrackTarget());                
                if (PingReturn == "targetfound")
                {
                    m_iMain.SiebwaldeAppLogging("MTCTRL: Ping successfull.");

                    m_iMain.SiebwaldeAppLogging("MTCTRL: Track controller Connecting...");
                    //MTSender.ConnectUdp(m_MTSendingPort);

                    m_iMain.SiebwaldeAppLogging("MTCTRL: Track controller Connected.");
                    m_iMain.SiebwaldeAppLogging("MTCTRL: Track controller Send MAC and IP...");
                                        
                    return true; // connection succesfull to FIDDLEYARD
                }
                else
                {
                    var address = Dns.GetHostEntry(mPublicEnums.TrackTarget()).AddressList.First();
                    if (address != null)
                    {
                        m_iMain.SiebwaldeAppLogging("MTCTRL: Dns.GetHostEntry successfull.");
                        m_iMain.SiebwaldeAppLogging("MTCTRL: Track controller Connecting...");
                        //MTSender.ConnectUdp(m_MTSendingPort);
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
