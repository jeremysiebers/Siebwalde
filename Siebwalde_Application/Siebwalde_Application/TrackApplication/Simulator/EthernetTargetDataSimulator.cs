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

        private int AmplifiersPresent;
        //private ushort UpdateTrackAmpNo, SendTrackAmpNo;
        //private UInt16[] HoldingReg;
        private readonly Random rand = new Random();

        // Create a timer
        private System.Timers.Timer UpdateToTrackIoHandleTimer = new System.Timers.Timer();
        private System.Timers.Timer InternallUpdateDataTimer = new System.Timers.Timer();

        // Create event for new data handling towards TrackIoHandle
        public Action<byte[]> NewData;

        /// <summary>
        /// Constructor, construct local variables
        /// </summary>
        public EthernetTargetDataSimulator(PublicEnums PublicEnums)
        {
            mPublicEnums = PublicEnums;

            //Random rng = new Random();

            AmplifiersPresent = rand.Next(1, 51);

            trackAmpItems = new List<TrackAmplifierItem>();

            ushort[] HoldingRegInit = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            for (ushort i = 0; i < 56; i++)
            {
                trackAmpItems.Add(trackAmp = new TrackAmplifierItem
                {
                    SlaveNumber = i,
                    SlaveDetected = (i <= AmplifiersPresent) && (i < 51) && (i > 0) ? (ushort)1 : (ushort)0,
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

            //UpdateTrackAmpNo = SendTrackAmpNo = 1;
            //UInt16[] HoldingReg = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
        }

        /// <summary>
        /// Start the Ethernet target simulator to emulate data coming from the target
        /// </summary>
        internal void Start()
        {
            // Event to TrackIoHandleData
            UpdateToTrackIoHandleTimer.Elapsed += new ElapsedEventHandler(UpdateToTrackIoHandle);
            // Set the Interval to [x] miliseconds.
            UpdateToTrackIoHandleTimer.Interval = 50;
            UpdateToTrackIoHandleTimer.AutoReset = true;
            // Enable the timer
            UpdateToTrackIoHandleTimer.Enabled = true;

            // update the simulator data
            InternallUpdateDataTimer.Elapsed += new ElapsedEventHandler(UpdateTrackIoHandleData);
            // Set the Interval to [x] miliseconds.
            InternallUpdateDataTimer.Interval = 10;
            InternallUpdateDataTimer.AutoReset = true;
            // Enable the timer
            InternallUpdateDataTimer.Enabled = true;


        }

        /// <summary>
        /// When the timer expires create a new data event per amplifier to TrackIoHandle
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void UpdateToTrackIoHandle(object source, ElapsedEventArgs e)
        {
            UpdateToTrackIoHandleTimer.Stop();
            byte[] data = new byte[45];

            foreach(TrackAmplifierItem Amp in trackAmpItems)
            {
                if(Amp.SlaveNumber < 51 && Amp.SlaveDetected != 0)
                {
                    data[0] = Convert.ToByte(mPublicEnums.Header());
                    data[1] = Convert.ToByte(mPublicEnums.SlaveInfo());
                    data[2] = Convert.ToByte(mPublicEnums.Header());
                    data[3] = Convert.ToByte(Amp.SlaveNumber);
                    data[4] = Convert.ToByte(Amp.SlaveDetected);
                    data[5] = 0; //Padding byte

                    UInt16 j = 0;

                    for (UInt16 i = 6; i < 30; i += 2)
                    {
                        data[i] = Convert.ToByte(Amp.HoldingReg[j] & 0x00FF);
                        data[i + 1] = Convert.ToByte((Amp.HoldingReg[j] & 0xFF00) >> 8);
                        j += 1;
                    }

                    data[30] = Convert.ToByte(Amp.MbReceiveCounter & 0x00FF);
                    data[31] = Convert.ToByte((Amp.MbReceiveCounter & 0xFF00) >> 8);
                    data[32] = Convert.ToByte(Amp.MbSentCounter & 0x00FF);
                    data[33] = Convert.ToByte((Amp.MbSentCounter & 0xFF00) >> 8);

                    data[34] = Convert.ToByte(Amp.MbCommError & 0x000000FF);
                    data[35] = Convert.ToByte((Amp.MbCommError & 0x0000FF00) >> 8);
                    data[36] = Convert.ToByte((Amp.MbCommError & 0x00FF0000) >> 16);
                    data[37] = Convert.ToByte((Amp.MbCommError & 0xFF000000) >> 24);

                    data[38] = Convert.ToByte(Amp.MbExceptionCode);
                    data[39] = Convert.ToByte(Amp.SpiCommErrorCounter);
                    data[40] = Convert.ToByte(mPublicEnums.Footer());

                    NewData(data);
                }
            }

            //SendTrackAmpNo++;

            //if (SendTrackAmpNo > 50)
            //{
            //    SendTrackAmpNo = 1;
            //}

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

            foreach (TrackAmplifierItem Amp in trackAmpItems)
            {
                if (Amp.SlaveNumber < 51 || Convert.ToBoolean(Amp.SlaveDetected) == true)
                {
                    //Reference to HoldingReg
                    UInt16[] HoldingReg = Amp.HoldingReg;

                    HoldingReg[0] = Convert.ToUInt16(rand.Next(0, 0xFFFF));
                    HoldingReg[1] = Convert.ToUInt16(rand.Next(0, 0xFFFF));
                    HoldingReg[2] = Convert.ToUInt16(rand.Next(0, 0xFFFF));
                    HoldingReg[3] = Convert.ToUInt16(rand.Next(0, 0xFFFF));
                    HoldingReg[4] = Convert.ToUInt16(rand.Next(0, 0xFFFF));
                    HoldingReg[5] = Convert.ToUInt16(rand.Next(0, 0xFFFF));
                    HoldingReg[6] = Convert.ToUInt16(rand.Next(0, 0xFFFF));
                    HoldingReg[7] = Convert.ToUInt16(rand.Next(0, 0xFFFF));
                    HoldingReg[8] = Convert.ToUInt16(rand.Next(0, 0xFFFF));
                    HoldingReg[11] = Convert.ToUInt16(rand.Next(0, 0xFFFF));

                    //Simulate trackamplifier sent/receive counter
                    HoldingReg[9] += 1;
                    HoldingReg[10] += 1;

                    //trackAmpItems[UpdateTrackAmpNo].HoldingReg = HoldingReg;
                    Amp.MbReceiveCounter += 1;
                    Amp.MbSentCounter += 1;
                    Amp.MbCommError = 0;
                    Amp.MbExceptionCode = 0;
                    Amp.SpiCommErrorCounter = 0;
                }
            }

            //UpdateTrackAmpNo++;

            //if(UpdateTrackAmpNo > 50)
            //{
            //    UpdateTrackAmpNo = 1;
            //}

            InternallUpdateDataTimer.Start();
        }
    }
}
