using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Siebwalde_Application
{
    /// <summary>
    /// Get data from Ethernet Target
    /// </summary>
    public class TrackIOHandle
    {
        private Sender mTrackSender;
        private Receiver mTrackReceiver;
        private PublicEnums mPublicEnums;
        /* connect variable to connect to FYController class to Main for application logging */
        private Main mMain;

        private int mTrackSendingPort;
        private int mTrackReceivingPort;

        public List<TrackAmplifierItem> trackAmpItems;
        private TrackAmplifierItem trackAmp;

        [DoNotNotify]
        public bool mTrackRealMode { get; set; }
        public EthernetTargetDataSimulator mEthernetTargetDataSimulator;

        /// <summary>
        /// TrackIoHandle Constructor
        /// </summary>
        /// <param name="publicEnums"></param>
        /// <param name="TrackReceivingPort"></param>
        /// <param name="TrackSendingPort"></param>
        /// <param name="trackApplicationVariables"></param>
        public TrackIOHandle(Main main, int TrackReceivingPort, int TrackSendingPort)
        {
            mMain = main;
            mTrackReceivingPort = TrackReceivingPort;
            mTrackSendingPort = TrackSendingPort;

            mPublicEnums = new PublicEnums();

            ushort[] HoldingRegInit = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            trackAmpItems = new List<TrackAmplifierItem>();

            for (ushort i = 0; i < 56; i++)
            {
                trackAmpItems.Add(trackAmp = new TrackAmplifierItem
                {
                    SlaveNumber = i,
                    SlaveDetected = 0,
                    HoldingReg = HoldingRegInit,
                    MbReceiveCounter = 0,
                    MbSentCounter = 0,
                    MbCommError = 0,
                    MbExceptionCode = 0,
                    SpiCommErrorCounter = 0
                });
            }           

            mTrackReceiver = new Receiver(mTrackReceivingPort);
            mTrackSender = new Sender(mPublicEnums.TrackTarget());
            mEthernetTargetDataSimulator = new EthernetTargetDataSimulator(mPublicEnums);
        }
        
        public List<TrackAmplifierItem> GetAmplifierListing()
        {
            return trackAmpItems;
        }

        /// <summary>
        /// Start the TrackIoHandle and check if simulator is active
        /// </summary>
        /// <param name="tracksimulator"></param>
        public void Start(bool trackrealmode)
        {

            mTrackRealMode = trackrealmode;

            if (mTrackRealMode == true)
            {
                mEthernetTargetDataSimulator.NewData -= HandleNewData;
                mMain.SiebwaldeAppLogging("MTCTRL: Track controller Connecting...");
                mTrackSender.ConnectUdp(mTrackSendingPort);
                mTrackReceiver.NewData += HandleNewData;
                mMain.SiebwaldeAppLogging("MTCTRL: Track controller Connected.");
                mTrackReceiver.Start();
            }
            else if (mTrackRealMode == false)
            {
                mTrackReceiver.NewData -= HandleNewData;
                mEthernetTargetDataSimulator.Start();
                mEthernetTargetDataSimulator.NewData += HandleNewData;
                mMain.SiebwaldeAppLogging("MTCTRL: Track controller Simulator started.");
            }

        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: ActuatorCmd
         *               Sends all commands from FYApplication to real target or
         *               to simulator.
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
         *  Notes      : Kicked by this application
         */
        /*#--------------------------------------------------------------------------#*/
        public void ActuatorCmd(string name, string cmd)
        {
            mTrackSender.SendUdp(Encoding.ASCII.GetBytes(cmd));            
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: HandleNewData
         *               to handle new data from target or from simulator
         *               
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
        public void HandleNewData(byte[] b)
        {
            string _b = Encoding.UTF8.GetString(b, 0, b.Length);        // convert received byte array to string array 
            
            var stream = new MemoryStream(b);
            var reader = new BinaryReader(stream);

            UInt16 Header = reader.ReadByte();
            UInt16 Sender = reader.ReadByte(); // and is also taskid
            if (Header == mPublicEnums.Header() && Sender == mPublicEnums.SlaveInfo())
            {
                UInt16 MbHeader = reader.ReadByte();
                UInt16 SlaveNumber = reader.ReadByte();
                UInt16 SlaveDetected = reader.ReadByte();
                UInt16 Padding = reader.ReadByte();

                UInt16[] HoldingReg = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
                for (int i = 0; i < 12; i++)
                {
                    HoldingReg[i] = reader.ReadUInt16();
                }

                UInt16 MbReceiveCounter = reader.ReadUInt16();
                UInt16 MbSentCounter = reader.ReadUInt16();

                UInt32 MbCommError = reader.ReadUInt32();

                UInt16 MbExceptionCode = reader.ReadByte();
                UInt16 SpiCommErrorCounter = reader.ReadByte();
                UInt16 MbFooter = reader.ReadByte();

                trackAmpItems[SlaveNumber].SlaveDetected = SlaveDetected;                
                trackAmpItems[SlaveNumber].HoldingReg = HoldingReg;
                trackAmpItems[SlaveNumber].MbReceiveCounter = MbReceiveCounter;
                trackAmpItems[SlaveNumber].MbSentCounter = MbSentCounter;
                trackAmpItems[SlaveNumber].MbCommError = MbCommError;
                trackAmpItems[SlaveNumber].MbExceptionCode = MbExceptionCode;
                trackAmpItems[SlaveNumber].SpiCommErrorCounter = SpiCommErrorCounter;
            }
            else if (Header == mPublicEnums.Header())
            {
                UInt16 taskcommand = reader.ReadByte();
                UInt16 taskstate = reader.ReadByte();
                UInt16 taskmessage = reader.ReadByte();

                //mTrackApplicationVariables.EthTargetMessage.UpdateEthernetTargetMessage(Sender, taskcommand, taskstate, taskmessage);
            }


            // dispose of object data
            reader.Dispose();
            stream.Dispose();

            //m_iMTCtrl.MTLinkActivityUpdate();
        }
                
    }    
}