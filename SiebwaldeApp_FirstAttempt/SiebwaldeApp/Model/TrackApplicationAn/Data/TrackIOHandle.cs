using System;
using System.IO;
using System.Text;
using static SiebwaldeApp.Enums;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

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

        public bool mconnected { get; private set; }

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
            mTrackRealMode = trackrealmode;

            if (mTrackRealMode == true)
            {
                //mEthernetTargetDataSimulator.NewData -= HandleNewData;
                IoC.Logger.Log("Track IO handler Connecting to sending port: " + mTrackSendingPort.ToString() + ".", mLoggerInstance);
                mTrackSender.ConnectUdp(mTrackSendingPort);
                
                mTrackReceiver.NewData += HandleNewData;
                IoC.Logger.Log("Track IO handler handle incoming data.", mLoggerInstance);

                mTrackReceiver.Start();
                IoC.Logger.Log("Track IO handler reciever started on receiving port: " + mTrackReceivingPort.ToString() + ".", mLoggerInstance);

                mconnected = true;
            }
            else if (mTrackRealMode == false)
            {
                mTrackReceiver.NewData -= HandleNewData;
                //mEthernetTargetDataSimulator.Start();
                //mEthernetTargetDataSimulator.NewData += HandleNewData;
                IoC.Logger.Log("Track IO handler connected to Simulator.", mLoggerInstance);
            }

            IoC.Logger.Log("Track IO Handler started.", mLoggerInstance);

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
            if (Header == HEADER && Sender == CONTROLLERDATA)
            {
                UInt16 Data = reader.ReadByte();
                mTrackApplicationVariables.HallBlock4A = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.HallBlock9B = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.HallBlockT2 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.HallBlockT1 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.HallBlockT5 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.HallBlockT4 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.HallBlock21A = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.HallBlock13 = Convert.ToBoolean((Data & 0x01));

                Data = reader.ReadByte();
                mTrackApplicationVariables.OccFromStn10 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromStn3 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromStn2 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromStn1 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromBlock4 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromBlock13 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.HallBlockT8 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.HallBlockT7 = Convert.ToBoolean((Data & 0x01));

                Data = reader.ReadByte();
                mTrackApplicationVariables.OccFromBlock9B = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromBlock22B = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromBlock23B = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.CtrlOff = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromT3 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromT6 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromStn12 = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromStn11 = Convert.ToBoolean((Data & 0x01));

                Data = reader.ReadByte();
                Data >>= 0x06;
                mTrackApplicationVariables.VoltageDetected = Convert.ToBoolean((Data & 0x01));
                Data >>= 0x01;
                mTrackApplicationVariables.OccFromBlock21B = Convert.ToBoolean((Data & 0x01));

            }
            else if (Header == HEADER && Sender == CONTROLLERALIVE)
            {
                UInt32 MbCommError = reader.ReadUInt32();
                //IoC.Logger.Log("received count = " + MbCommError.ToString(), mLoggerInstance);
            }
            else if (Header == HEADER)
            {                
                mReceivedMessage.TaskId = Sender;
                mReceivedMessage.Taskcommand = reader.ReadByte();
                mReceivedMessage.Taskstate = reader.ReadByte();
                mReceivedMessage.Taskmessage = reader.ReadByte();

                IoC.Logger.Log("TaskId      = " + TranslateNumber(mReceivedMessage.TaskId), mLoggerInstance);
                IoC.Logger.Log("Taskcommand = " + TranslateNumber(mReceivedMessage.Taskcommand), mLoggerInstance);
                IoC.Logger.Log("Taskstate   = " + TranslateNumber(mReceivedMessage.Taskstate), mLoggerInstance);

                if(mReceivedMessage.Taskstate != TIME)
                {
                    IoC.Logger.Log("Taskmessage = " + TranslateNumber(mReceivedMessage.Taskmessage), mLoggerInstance);
                }
                else
                {
                    IoC.Logger.Log("WaitTime is set to " + Convert.ToString(mReceivedMessage.Taskmessage) + " sec.", mLoggerInstance);
                }
                IoC.Logger.Log("---------------------------------", mLoggerInstance);
            }

        }
                
    }    
}