using System;
using System.IO;
using System.Text;
using static SiebwaldeApp.Enums;

namespace SiebwaldeApp
{
    /// <summary>
    /// Get data from Ethernet Target
    /// </summary>
    public class TrackIOHandle
    {
        private Sender mTrackSender;
        private Receiver mTrackReceiver;
        //private Enums mEnums;
        
        private int mTrackSendingPort;
        private int mTrackReceivingPort;
                
        private ReceivedMessage mReceivedMessage;

        // Public variables
        private TrackApplicationVariables mTrackApplicationVariables;

        // If track runs in real mode or in sim mode
        public bool mTrackRealMode { get; set; }

        //private EthernetTargetDataSimulator mEthernetTargetDataSimulator;

        // Logger instance
        private string mLoggerInstance { get; set; }

        /// <summary>
        /// TrackIoHandle Constructor
        /// </summary>
        /// <param name="Enums"></param>
        /// <param name="TrackReceivingPort"></param>
        /// <param name="TrackSendingPort"></param>
        /// <param name="trackApplicationVariables"></param>
        public TrackIOHandle(int TrackReceivingPort, int TrackSendingPort, string LoggerInstance)
        {
            mTrackReceivingPort = TrackReceivingPort;
            mTrackSendingPort = TrackSendingPort;
            mLoggerInstance = LoggerInstance;

            mTrackReceiver = new Receiver(mTrackReceivingPort);
            mTrackSender = new Sender(TRACKTARGET);
            // create new instance of trackApplicationVariables (DATA)
            mTrackApplicationVariables = IoC.TrackVar;

            //mEthernetTargetDataSimulator = new EthernetTargetDataSimulator();
            mReceivedMessage = new ReceivedMessage(0,0,0,0);
        }
        
        /// <summary>
        /// Start the TrackIoHandle and check if simulator is active
        /// </summary>
        /// <param name="tracksimulator"></param>
        public void Start(bool trackrealmode)
        {
            IoC.Logger.Log("Track IO Handler started.", mLoggerInstance);

            mTrackRealMode = trackrealmode;

            if (mTrackRealMode == true)
            {
                //mEthernetTargetDataSimulator.NewData -= HandleNewData;
                IoC.Logger.Log("Track IO handler Connecting to target...", mLoggerInstance);
                mTrackSender.ConnectUdp(mTrackSendingPort);

                mTrackReceiver.NewData += HandleNewData;
                IoC.Logger.Log("Track IO handler connected to target.", mLoggerInstance);

                mTrackReceiver.Start();
                IoC.Logger.Log("Track IO handler reciever started.", mLoggerInstance);
            }
            else if (mTrackRealMode == false)
            {
                mTrackReceiver.NewData -= HandleNewData;
                //mEthernetTargetDataSimulator.Start();
                //mEthernetTargetDataSimulator.NewData += HandleNewData;
                IoC.Logger.Log("Track IO handler connected to Simulator.", mLoggerInstance);
            }

        }

        /// <summary>
        /// Actuator command to send command to Ethernet Target
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cmd"></param>
        public void ActuatorCmd(SendMessage sendMessage)
        {
            byte[] datatosend = new byte[sendMessage.Data.Length + 2];
            datatosend[0] = HEADER;
            datatosend[1] = sendMessage.Command;
            //datatosend[sendMessage.Data.Length + 2] = FOOTER;
            Buffer.BlockCopy(sendMessage.Data, 0, datatosend, 2, sendMessage.Data.Length);
            mTrackSender.SendUdp(datatosend);            
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

                //mTrackApplicationVariables.trackAmpItems[SlaveNumber].SlaveDetected = SlaveDetected;                
                //mTrackApplicationVariables.trackAmpItems[SlaveNumber].HoldingReg = HoldingReg;
                //mTrackApplicationVariables.trackAmpItems[SlaveNumber].MbReceiveCounter = MbReceiveCounter;
                //mTrackApplicationVariables.trackAmpItems[SlaveNumber].MbSentCounter = MbSentCounter;
                //mTrackApplicationVariables.trackAmpItems[SlaveNumber].MbCommError = MbCommError;
                //mTrackApplicationVariables.trackAmpItems[SlaveNumber].MbExceptionCode = MbExceptionCode;
                //mTrackApplicationVariables.trackAmpItems[SlaveNumber].SpiCommErrorCounter = SpiCommErrorCounter;
            }
            else if (Header == HEADER)
            {                
                mReceivedMessage.TaskId = Sender;
                mReceivedMessage.Taskcommand = reader.ReadByte();
                mReceivedMessage.Taskstate = reader.ReadByte();
                mReceivedMessage.Taskmessage = reader.ReadByte();

                //mTrackApplicationVariables.trackControllerCommands.ReceivedMessage = mReceivedMessage;

                //Console.WriteLine("Received message: TaskId = " +
                //mReceivedMessage.TaskId.ToString() + ", Taskcommand = " +
                //mReceivedMessage.Taskcommand.ToString() + ", Taskstate = " +
                //mReceivedMessage.Taskstate.ToString() + ", Taskmessage = " +
                //mReceivedMessage.Taskmessage.ToString() + ".");
            }

            // dispose of object data
            //reader.Dispose();
            //stream.Dispose();

            //m_iMTCtrl.MTLinkActivityUpdate();
        }
                
    }    
}