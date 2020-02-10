using System;
using System.Linq;
using System.Net;

namespace Siebwalde_Application
{

    public class TrackController
    {
        /* connect variable to connect to FYController class to Main for application logging */
        private Main mMain;

        public PingTarget m_PingTarget = new PingTarget { };

        /* Data */
        public TrackIOHandle trackIOHandle;
        
        /* Public Enums */
        public PublicEnums mPublicEnums;
        /* ViewModel */
        public TrackAmplifierItemViewModel mTrackAmplifierItemViewModel;

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
        public TrackController(TrackAmplifierItemViewModel trackAmplifierItemViewModel ,int TrackReceivingPort, int TrackSendingPort, Main main)
        {
            /*
             * connect to Main interface for application text logging and link activity 
             * update, save interface in variable.
             */
            //mMain = iMainCtrl;
            mTrackReceivingPort = TrackReceivingPort;
            mTrackSendingPort = TrackSendingPort;
            mTrackAmplifierItemViewModel = trackAmplifierItemViewModel;
            mMain = main;

            mPublicEnums = new PublicEnums();

            /*
             * Public ENUM init and pass to all
             */

            trackIOHandle = new TrackIOHandle(mTrackReceivingPort, mTrackSendingPort);
            mTrackAmplifierItemViewModel.TrackAmplifierItemViewModelPassTrackIoHandle(trackIOHandle);

            //TrackAmplifierItemViewModel = new TrackAmplifierItemViewModel(trackIOHandle);

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
            bool TrackRealMode = ConnectTrackConntroller();

            //force if required
            //TrackRealMode = false;

            trackIOHandle.Start(TrackRealMode);

            if (TrackRealMode) // when connection was succesfull and target was found and is connected
            {
                mMain.SiebwaldeAppLogging("MTCTRL: Track uController target in real mode.");                
            }
            else
            {
                mMain.SiebwaldeAppLogging("MTCTRL: Track uController target in simulator mode!");
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
                mMain.SiebwaldeAppLogging("MTCTRL: Pinging Track controller target...");
                PingReturn = m_PingTarget.TargetFound(mPublicEnums.TrackTarget());                
                if (PingReturn == "targetfound")
                {
                    mMain.SiebwaldeAppLogging("MTCTRL: Ping successfull.");

                    mMain.SiebwaldeAppLogging("MTCTRL: Track controller Connecting...");
                    //MTSender.ConnectUdp(m_MTSendingPort);

                    mMain.SiebwaldeAppLogging("MTCTRL: Track controller Connected.");
                    mMain.SiebwaldeAppLogging("MTCTRL: Track controller Send MAC and IP...");
                                        
                    return true; // connection succesfull to FIDDLEYARD
                }
                else
                {
                    var address = Dns.GetHostEntry(mPublicEnums.TrackTarget()).AddressList.First();
                    if (address != null)
                    {
                        mMain.SiebwaldeAppLogging("MTCTRL: Dns.GetHostEntry successfull.");
                        mMain.SiebwaldeAppLogging("MTCTRL: Track controller Connecting...");
                        //MTSender.ConnectUdp(m_MTSendingPort);
                        mMain.SiebwaldeAppLogging("MTCTRL: Track controller Connected.");
                        return true; // connection succesfull to FIDDLEYARD
                    }
                    else
                    {
                        mMain.SiebwaldeAppLogging("MTCTRL: " + PingReturn);
                        return false; // ping was unsuccessfull
                    }
                     
                }

            }
            catch (Exception)
            {
                mMain.SiebwaldeAppLogging("MTCTRL: TrackController failed to connect.");                
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
