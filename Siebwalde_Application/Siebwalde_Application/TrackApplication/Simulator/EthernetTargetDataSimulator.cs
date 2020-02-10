using System;
using System.Collections.Generic;
using System.Timers;

namespace Siebwalde_Application
{
    /// <summary>
    /// This class simulates the EthernetTarget data events
    /// </summary>
    public class EthernetTargetDataSimulator
    {
        private List<TrackAmplifierItem> trackAmpItems;
        private TrackAmplifierItem trackAmp;
        private PublicEnums mPublicEnums;

        private ushort UpdateTrackAmpNo, SendTrackAmpNo;
        private UInt16[] HoldingReg;

        // Create a timer
        private System.Timers.Timer UpdateToTrackIoHandleTimer = new System.Timers.Timer();
        private System.Timers.Timer InternallUpdateDataTimer = new System.Timers.Timer();
        private Random rng;

        // Create event for new data handling towards TrackIoHandle
        public Action<byte[]> NewData;

        /// <summary>
        /// Constructor, construct local variables
        /// </summary>
        public EthernetTargetDataSimulator(PublicEnums PublicEnums)
        {
            mPublicEnums = PublicEnums;

            Random rng = new Random();

            int AmplifiersPresent = rng.Next(1, 56);

            trackAmpItems = new List<TrackAmplifierItem>();

            ushort[] HoldingRegInit = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            for (ushort i = 0; i < 56; i++)
            {
                trackAmpItems.Add(trackAmp = new TrackAmplifierItem
                {
                    SlaveNumber = i,
                    SlaveDetected = i <= AmplifiersPresent && i < 51 ? (ushort)1 : (ushort)0,
                    HoldingReg = HoldingRegInit,
                    MbReceiveCounter = 0,
                    MbSentCounter = 0,
                    MbCommError = 0,
                    MbExceptionCode = 0,
                    SpiCommErrorCounter = 0
                });
            }

            if (AmplifiersPresent > 0)
            {
                trackAmpItems[51].SlaveDetected = 1;
            }
            if (AmplifiersPresent > 10)
            {
                trackAmpItems[52].SlaveDetected = 1;
            }
            if (AmplifiersPresent > 20)
            {
                trackAmpItems[53].SlaveDetected = 1;
            }
            if (AmplifiersPresent > 30)
            {
                trackAmpItems[54].SlaveDetected = 1;
            }
            if (AmplifiersPresent > 40)
            {
                trackAmpItems[55].SlaveDetected = 1;
            }

            UpdateTrackAmpNo = SendTrackAmpNo = 1;
            UInt16[] HoldingReg = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
        }

        /// <summary>
        /// Start the Ethernet target simulator to emulate data coming from the target
        /// </summary>
        internal void Start()
        {
            // Event to TrackIoHandleData
            UpdateToTrackIoHandleTimer.Elapsed += new ElapsedEventHandler(UpdateToTrackIoHandle);
            // Set the Interval to [x] miliseconds.
            UpdateToTrackIoHandleTimer.Interval = 200;
            UpdateToTrackIoHandleTimer.AutoReset = true;
            // Enable the timer
            UpdateToTrackIoHandleTimer.Enabled = true;

            // update the simulator data
            InternallUpdateDataTimer.Elapsed += new ElapsedEventHandler(UpdateTrackIoHandleData);
            // Set the Interval to [x] miliseconds.
            InternallUpdateDataTimer.Interval = 190;
            InternallUpdateDataTimer.AutoReset = true;
            // Enable the timer
            InternallUpdateDataTimer.Enabled = true;


        }

        /// <summary>
        /// When the timer expires create a new data event to TrackIoHandle
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void UpdateToTrackIoHandle(object source, ElapsedEventArgs e)
        {
            UpdateToTrackIoHandleTimer.Stop();
            byte[] data = new byte[45];

            data[0] = Convert.ToByte(mPublicEnums.Header());
            data[1] = Convert.ToByte(mPublicEnums.SlaveInfo());
            data[2] = Convert.ToByte(mPublicEnums.Header());
            data[3] = Convert.ToByte(trackAmpItems[SendTrackAmpNo].SlaveNumber);
            data[4] = Convert.ToByte(trackAmpItems[SendTrackAmpNo].SlaveDetected);
            data[5] = 0; //Padding byte

            UInt16 j = 0;

            for (UInt16 i = 6; i < 30; i += 2)
            {
                data[i] = Convert.ToByte((trackAmpItems[SendTrackAmpNo].HoldingReg[j] & 0xFF00) >> 8);
                data[i + 1] = Convert.ToByte(trackAmpItems[SendTrackAmpNo].HoldingReg[j] & 0x00FF);
                j += 1;
            }
                        
            data[32] = Convert.ToByte((trackAmpItems[SendTrackAmpNo].MbReceiveCounter & 0xFF00) >> 8);
            data[33] = Convert.ToByte(trackAmpItems[SendTrackAmpNo].MbReceiveCounter & 0x00FF);
            data[34] = Convert.ToByte((trackAmpItems[SendTrackAmpNo].MbSentCounter & 0xFF00) >> 8);
            data[35] = Convert.ToByte(trackAmpItems[SendTrackAmpNo].MbSentCounter & 0x00FF);

            data[36] = Convert.ToByte((trackAmpItems[SendTrackAmpNo].MbCommError & 0xFF000000) >> 24);
            data[37] = Convert.ToByte((trackAmpItems[SendTrackAmpNo].MbCommError & 0x00FF0000) >> 16);
            data[38] = Convert.ToByte((trackAmpItems[SendTrackAmpNo].MbCommError & 0x0000FF00) >> 8);
            data[39] = Convert.ToByte(trackAmpItems[SendTrackAmpNo].MbCommError & 0x000000FF);

            data[40] = Convert.ToByte(trackAmpItems[SendTrackAmpNo].MbExceptionCode);
            data[41] = Convert.ToByte(trackAmpItems[SendTrackAmpNo].SpiCommErrorCounter);
            data[42] = Convert.ToByte(mPublicEnums.Footer());

            NewData(data);

            SendTrackAmpNo++;

            if (SendTrackAmpNo > 50)
            {
                SendTrackAmpNo = 1;
            }

            UpdateToTrackIoHandleTimer.Start();
        }


        /// <summary>
        /// When the timer expires update the simulated amplifier data
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        /// 

        public void UpdateTrackIoHandleData(object source, ElapsedEventArgs e)
        {
            InternallUpdateDataTimer.Stop();

            //Reference to HoldingReg
            UInt16[] HoldingReg = trackAmpItems[UpdateTrackAmpNo].HoldingReg;
            //Simulate trackamplifier sent/receive counter
            HoldingReg[8] += 1;
            HoldingReg[9] += 1;

            //trackAmpItems[UpdateTrackAmpNo].HoldingReg = HoldingReg;
            trackAmpItems[UpdateTrackAmpNo].MbReceiveCounter += 1;
            trackAmpItems[UpdateTrackAmpNo].MbSentCounter += 1;
            trackAmpItems[UpdateTrackAmpNo].MbCommError = 0;
            trackAmpItems[UpdateTrackAmpNo].MbExceptionCode = 0;
            trackAmpItems[UpdateTrackAmpNo].SpiCommErrorCounter = 0;

            UpdateTrackAmpNo++;

            if(UpdateTrackAmpNo > 50)
            {
                UpdateTrackAmpNo = 1;
            }

            InternallUpdateDataTimer.Start();
        }
    }
}
