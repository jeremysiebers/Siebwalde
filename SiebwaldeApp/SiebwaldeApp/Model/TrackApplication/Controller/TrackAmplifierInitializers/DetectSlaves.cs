using System;
using System.Collections.Generic;
using System.Threading;

namespace SiebwaldeApp
{
    public class DetectSlaves : IAmplifierInitializersBaseClass
    {
        #region Local variables
        // Hold the Track IO Handle instance
        private TrackIOHandle mTrackIOHandle;
        // Hold the Track application variables
        private TrackApplicationVariables mTrackApplicationVariables;
        // Set the TrackAmplifierBootloaderHelpers
        private SendNextFwDataPacket mSendNextFwDataPacket;
        // Set the TrackAmplifierBootloaderHelpers
        private TrackAmplifierBootloaderHelpers mTrackAmplifierBootloaderHelpers;
        // Switch-case variable
        private uint SubMethodState { get; set; }
        // Message conatiner for sending messages
        private SendMessage mSendMessage;
        // Logger instance
        private string mLoggerInstance { get; set; }
        // Loop counter for detection
        private uint loopcounter { get; set; }
        // Max attempts
        private static uint attemptmax { get; set; }
        // Hold the number of slaves counted
        private uint SlaveCount { get; set; }        
        // Set the recovery iteration counter
        private int IterationCounter { get; set; }
        // Get the amount of process lines of the flash program memory of the used slave target uController
        private readonly uint ProcessLines = (Enums.PROGMEMSIZE - Enums.BOOTLOADEROFFSET) / Enums.HEXROWWIDTH;
        // Get the amount of iterations required to step through the uController flash memory
        private readonly uint Iterations = ((Enums.PROGMEMSIZE - Enums.BOOTLOADEROFFSET) / Enums.HEXROWWIDTH) - Enums.JUMPSIZE; // fixed (and max) jump size of 4 rows
        // The name of the class
        public string Name { get; set; }

        #endregion

        #region Constructor
        /// <summary>
        /// Setup the Connect to Ethernet target
        /// </summary>
        public DetectSlaves(string LoggerInstance, TrackIOHandle trackIOHandle, TrackApplicationVariables trackApplicationVariables,
            SendNextFwDataPacket sendNextFwDataPacket, TrackAmplifierBootloaderHelpers trackAmplifierBootloaderHelpers)
        {
            // Hold the TrackAmplifierBootloaderHelpers
            mTrackAmplifierBootloaderHelpers = trackAmplifierBootloaderHelpers;

            // Hold the TrackAmplifierBootloaderHelpers
            mSendNextFwDataPacket = sendNextFwDataPacket;

            // Hold the Track application variables
            mTrackApplicationVariables = trackApplicationVariables;

            // Hold the Track IO Handle instance
            mTrackIOHandle = trackIOHandle;

            // Hold the logger instance
            mLoggerInstance = LoggerInstance;

            // init the switch-case
            SubMethodState = 0;
            // Set the loopcounter
            loopcounter = 1;
            // Set the Max attempts
            attemptmax = 3;
            // Set the amount of detected slaves to 0
            SlaveCount = 0;
            // Set the iteration counter
            IterationCounter = 0;

            // Create dummy data container
            byte[] DummyData = new byte[80];

            // Create Sendmessage container
            mSendMessage = new SendMessage(0, DummyData);

            //Set the name of the class for referencing during execution from list
            Name = this.GetType().Name;
        }
        #endregion

        #region public Methods

        /// <summary>
        /// Call until finished returns, this sequence calls the detect mechanism of the modbus master in the ethernet target
        /// </summary>
        /// <param name="receivedMessage"></param>
        /// <returns></returns>
        public (uint, string) Execute(ReceivedMessage receivedMessage)
        {            
            uint returnval = Enums.Busy;
            string CallNext = "";

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT;
                        mTrackIOHandle.ActuatorCmd(mSendMessage);
                        SubMethodState += 1;
                        IoC.Logger.Log("State.DetectSlaves => EXEC_MBUS_STATE_SLAVE_DETECT.", mLoggerInstance);
                        break;
                    }
                case 1:
                    {
                        if (receivedMessage.TaskId == TaskId.MBUS &&
                            receivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_DETECT &&
                            receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.DetectSlaves => MBUS_STATE_SLAVE_DETECT.", mLoggerInstance);

                            Thread.Sleep(2000);

                            SlaveCount = 0;

                            foreach (TrackAmplifierItem amplifier in mTrackApplicationVariables.trackAmpItems)
                            {
                                if (amplifier.SlaveDetected == 1)
                                {
                                    SlaveCount++;
                                    //Console.WriteLine("DetectSlaves --> slave " + amplifier.SlaveNumber.ToString() + " detected.");
                                    IoC.Logger.Log("State.DetectSlaves => slave " + amplifier.SlaveNumber.ToString() + " detected.", mLoggerInstance);
                                }
                            }

                            //Console.WriteLine("DetectSlaves --> " + SlaveCount.ToString() + " slaves in total detected.");
                            IoC.Logger.Log("State.DetectSlaves => " + SlaveCount.ToString() + " slaves in total detected.", mLoggerInstance);

                            if (SlaveCount > 2)
                            {
                                Thread.Sleep(1000);
                                SubMethodState = 0;
                                loopcounter = 1;
                                returnval = Enums.Finished;
                                CallNext = "Remove:RecoverSlaves";
                            }
                            else if (loopcounter > attemptmax)
                            {
                                //Console.WriteLine("DetectSlaves --> more then " + loopcounter.ToString() + " recovery attempts, stopping program!");
                                IoC.Logger.Log("State.DetectSlaves => more then " + loopcounter.ToString() + " recovery attempts, stopping program!", mLoggerInstance);
                                SubMethodState = 0;
                                loopcounter = 1;
                                returnval = Enums.Error;
                            }
                            else
                            {
                                //Console.WriteLine("DetectSlaves --> Start checking if one slave is stuck in bootloader mode, attempt " + 
                                //    loopcounter.ToString() + " of " + attemptmax.ToString() + ".");
                                IoC.Logger.Log("State.DetectSlaves => Start checking if one slave is stuck in bootloader mode, attempt " +
                                    loopcounter.ToString() + " of " + attemptmax.ToString() + ".", mLoggerInstance);
                                SubMethodState = 0;
                                // try to recover slave by running a "next" sequence
                                returnval = Enums.Next;
                                CallNext = "RecoverSlaves";
                            }
                        }
                        break;
                    }

                default:
                    {
                        break;
                    }
            }
            return (returnval, CallNext);
        }
        #endregion
    }
}
