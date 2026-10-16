using System;
using System.Collections.Generic;
using System.Timers;
using static SiebwaldeApp.Core.Enums;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// This class simulates the EthernetTarget data events
    /// </summary>
    public class EthernetTargetDataSimulator
    {
        private List<TrackAmplifierItem> trackAmpItems;
        private TrackAmplifierItem trackAmp;

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
        public EthernetTargetDataSimulator()
        {
            //Random rng = new Random();

            AmplifiersPresent = 50;// rand.Next(1, 51);

            trackAmpItems = new List<TrackAmplifierItem>();

            for (ushort i = 0; i < 56; i++)
            {
                trackAmpItems.Add(trackAmp = new TrackAmplifierItem
                {
                    SlaveNumber = i,
                    SlaveDetected = (i <= AmplifiersPresent) && (i < 51) && (i > 0) ? (ushort)1 : (ushort)0,
                    HoldingReg = new ushort[TrackAmplifierRegisters.Count],
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
            UpdateToTrackIoHandleTimer.Interval = 100;
            UpdateToTrackIoHandleTimer.AutoReset = true;
            // Enable the timer
            UpdateToTrackIoHandleTimer.Enabled = true;

            // update the simulator data
            InternallUpdateDataTimer.Elapsed += new ElapsedEventHandler(UpdateTrackIoHandleData);
            // Set the Interval to [x] miliseconds.
            InternallUpdateDataTimer.Interval = 100;
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

            foreach(TrackAmplifierItem Amp in trackAmpItems)
            {
                if(Amp.SlaveNumber < 51 && Amp.SlaveDetected != 0)
                {
                    NewData(TrackSlaveInfoFrame.Encode((byte)Amp.SlaveNumber, (byte)Amp.SlaveDetected,
                        Amp.HoldingReg, Amp.MbReceiveCounter, Amp.MbSentCounter, Amp.MbCommError,
                        (byte)Amp.MbExceptionCode, (byte)Amp.SpiCommErrorCounter));
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

                    // UI-only telemetry source, not a control-core or FC03-freshness simulator.
                    HoldingReg[TrackAmplifierRegisters.PwmCommand] = AmplifierSpeedMapper.NeutralPwm;
                    HoldingReg[TrackAmplifierRegisters.RequestedPwm] = AmplifierSpeedMapper.NeutralPwm;
                    HoldingReg[TrackAmplifierRegisters.TargetPwm] = AmplifierSpeedMapper.NeutralPwm;
                    HoldingReg[TrackAmplifierRegisters.AppliedPwm] = AmplifierSpeedMapper.NeutralPwm;
                    HoldingReg[TrackAmplifierRegisters.Identity] = 0xF101;
                    HoldingReg[TrackAmplifierRegisters.Address] = Amp.SlaveNumber;
                    HoldingReg[TrackAmplifierRegisters.Validity] = (ushort)(rand.Next(0, 2) << 8);
                    HoldingReg[TrackAmplifierRegisters.FuseVoltage] = (ushort)rand.Next(0, 1024);
                    HoldingReg[TrackAmplifierRegisters.HBridgeTemperature] = (ushort)rand.Next(0, 1024);
                    HoldingReg[TrackAmplifierRegisters.HBridgeCurrent] = (ushort)rand.Next(0, 1024);

                    //Simulate trackamplifier sent/receive counter
                    HoldingReg[TrackAmplifierRegisters.MessagesReceived] += 1;
                    HoldingReg[TrackAmplifierRegisters.MessagesSent] += 1;

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
