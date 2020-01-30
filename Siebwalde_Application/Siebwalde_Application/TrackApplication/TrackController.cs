using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Siebwalde_Application.TrackApplication.Model
{

    public class TrackController
    {
        /* connect variable to connect to FYController class to Main for application logging */
        public iMain m_iMain;
        
        public PingTarget m_PingTarget = new PingTarget { };

        /* Public Enums */
        public Services.PublicEnums mPublicEnums;
        /* Model */
        public TrackApplicationVariables mTrackApplicationVariables;
        /* ViewModel */
        public TrackApplication mTrackApplication;

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

            /*
             * Public ENUM init and pass to all
             */
            mPublicEnums = new Services.PublicEnums();

            /*
             * Init the Model
             */
            mTrackApplicationVariables = new TrackApplicationVariables(mPublicEnums, mTrackReceivingPort, mTrackSendingPort);
            
            /*
             * Init the ViewModel
             */
            mTrackApplication = new TrackApplication();

            /*
             * Init the View
             */
            // VIEW is started via MAIN FORM button event.
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
                mTrackApplicationVariables.Start();
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
