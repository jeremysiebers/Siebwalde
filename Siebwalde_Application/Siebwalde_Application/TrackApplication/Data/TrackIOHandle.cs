using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Siebwalde_Application.PublicEnums;

namespace Siebwalde_Application
{
    /// <summary>
    /// Get data from Ethernet Target
    /// </summary>
    public class TrackIOHandle
    {
        private Sender mTrackSender;
        private Receiver mTrackReceiver;
        //private PublicEnums mPublicEnums;
        private TrackApplicationVariables mTrackApplicationVariables;
        /* connect variable to connect to FYController class to Main for application logging */
        private Main mMain;

        private int mTrackSendingPort;
        private int mTrackReceivingPort;
                
        private ReceivedMessage mReceivedMessage;

        //public List<TrackAmplifierItem> trackAmpItems;
        //private TrackAmplifierItem trackAmp;

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
        public TrackIOHandle(Main main, TrackApplicationVariables trackApplicationVariables, int TrackReceivingPort, int TrackSendingPort)
        {
            mMain = main;
            mTrackApplicationVariables = trackApplicationVariables;
            mTrackReceivingPort = TrackReceivingPort;
            mTrackSendingPort = TrackSendingPort;            
            mTrackReceiver = new Receiver(mTrackReceivingPort);
            mTrackSender = new Sender(TRACKTARGET);
            mEthernetTargetDataSimulator = new EthernetTargetDataSimulator();
            mReceivedMessage = new ReceivedMessage(0,0,0,0);
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

        /// <summary>
        /// Actuator command to send command to Ethernet Target
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cmd"></param>
        public void ActuatorCmd(string name, string cmd)
        {
            mTrackSender.SendUdp(Encoding.ASCII.GetBytes(cmd));            
        }


        /// <summary>
        /// Handle new received data from Track Ethernet Target
        /// </summary>
        /// <param name="b"></param>
        public void HandleNewData(byte[] b)
        {
            string _b = Encoding.UTF8.GetString(b, 0, b.Length);        // convert received byte array to string array 
            
            var stream = new MemoryStream(b);
            var reader = new BinaryReader(stream);

            UInt16 Header = reader.ReadByte();
            UInt16 Sender = reader.ReadByte(); // and is also taskid
            if (Header == HEADER && Sender == SLAVEINFO)
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

                mTrackApplicationVariables.trackAmpItems[SlaveNumber].SlaveDetected = SlaveDetected;                
                mTrackApplicationVariables.trackAmpItems[SlaveNumber].HoldingReg = HoldingReg;
                mTrackApplicationVariables.trackAmpItems[SlaveNumber].MbReceiveCounter = MbReceiveCounter;
                mTrackApplicationVariables.trackAmpItems[SlaveNumber].MbSentCounter = MbSentCounter;
                mTrackApplicationVariables.trackAmpItems[SlaveNumber].MbCommError = MbCommError;
                mTrackApplicationVariables.trackAmpItems[SlaveNumber].MbExceptionCode = MbExceptionCode;
                mTrackApplicationVariables.trackAmpItems[SlaveNumber].SpiCommErrorCounter = SpiCommErrorCounter;
            }
            else if (Header == HEADER)
            {
                
                mReceivedMessage.TaskId = reader.ReadByte();
                mReceivedMessage.Taskcommand = reader.ReadByte();
                mReceivedMessage.Taskstate = reader.ReadByte();
                mReceivedMessage.Taskmessage = reader.ReadByte();

                mTrackApplicationVariables.trackControllerCommands.ReceivedMessages = mReceivedMessage;
            }

            // dispose of object data
            reader.Dispose();
            stream.Dispose();

            //m_iMTCtrl.MTLinkActivityUpdate();
        }
                
    }    
}