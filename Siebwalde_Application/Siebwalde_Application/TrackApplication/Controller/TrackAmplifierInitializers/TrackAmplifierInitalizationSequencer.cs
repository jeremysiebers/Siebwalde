using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Siebwalde_Application
{
    /// <summary>
    /// This TrackAmplifierInitalizationSequencer initializes all the Track amplifiers and checks if firmware download is required
    /// </summary>
    public class TrackAmplifierInitalizationSequencer
    {
        #region Local variables

        private TrackApplicationVariables mTrackApplicationVariables;
        private Log2LoggingFile mTrackApplicationLogging;
        private SendMessage mSendMessage;
        private ReceivedMessage mReceivedMessage;

        /// <summary>
        /// This enum holds all the possible states of the TrackAmplifierInitalizationSequencer statemachine
        /// </summary>
        private enum State { Idle, Reset, ConnectToEthernetTarget, ResetAllSlaves, DataUploadStart, DetectSlaves,
        FlashTrackamplifiers
        };
        private State StateMachine;
        private uint SubMethodState;

        #endregion

        #region Constructor

        public TrackAmplifierInitalizationSequencer(Log2LoggingFile trackApplicationLogging, TrackApplicationVariables trackApplicationVariables)
        {
            mTrackApplicationVariables = trackApplicationVariables;
            mTrackApplicationLogging = trackApplicationLogging;

            StateMachine = State.ConnectToEthernetTarget;
            SubMethodState = 0;

            byte[] DummyData = new byte[80];
            mSendMessage = new SendMessage(0, DummyData);
            mReceivedMessage = new ReceivedMessage(0, 0, 0, 0);
        }

        #endregion

        #region Internal statemachine

        internal uint InitSequence(string source, Int32 value)
        {
            uint returnval = Enums.Busy;

            switch (StateMachine)
            {
                case State.Reset:
                    // Here all sub classes reset methods are called in case of a forced reset
                    returnval = Enums.Finished;
                    break;

                case State.ConnectToEthernetTarget:
                    {
                        switch (ConnectToEthernetTarget(source, value))
                        {
                            case Enums.Busy:
                                {
                                    break;
                                }
                            case Enums.Finished:
                                {
                                    mTrackApplicationLogging.Log(GetType().Name, "State.ConnectToEthernetTarget == Finished.");
                                    StateMachine = State.ResetAllSlaves;
                                    break;
                                }
                            case Enums.Error:
                                {
                                    mTrackApplicationLogging.Log(GetType().Name, "State.ConnectToEthernetTarget == Error.");
                                    StateMachine = State.Idle;
                                    returnval = Enums.Error;
                                    break;
                                }
                            default:
                                {
                                    StateMachine = State.Idle;
                                    break;
                                }
                        }
                        break;
                    }

                case State.ResetAllSlaves:
                    {
                        switch (ResetAllSlaves(source, value))
                        {
                            case Enums.Busy:
                                {
                                    break;
                                }
                            case Enums.Finished:
                                {
                                    mTrackApplicationLogging.Log(GetType().Name, "State.ResetAllSlaves == Finished.");
                                    StateMachine = State.DataUploadStart;
                                    break;
                                }
                            case Enums.Error:
                                {
                                    mTrackApplicationLogging.Log(GetType().Name, "State.ResetAllSlaves == Error.");
                                    StateMachine = State.Idle;
                                    returnval = Enums.Error;
                                    break;
                                }
                            default:
                                {
                                    StateMachine = State.Idle;
                                    break;
                                }
                        }
                        break;
                    }

                case State.DataUploadStart:
                    {
                        switch (DataUploadStart(source, value))
                        {
                            case Enums.Busy:
                                {
                                    break;
                                }
                            case Enums.Finished:
                                {
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DataUploadStart == Finished.");
                                    StateMachine = State.DetectSlaves;
                                    break;
                                }
                            case Enums.Error:
                                {
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DataUploadStart == Error.");
                                    StateMachine = State.Idle;
                                    returnval = Enums.Error;
                                    break;
                                }
                            default:
                                {
                                    StateMachine = State.Idle;
                                    break;
                                }
                        }
                        break;
                    }

                case State.DetectSlaves:
                    {
                        switch (DetectSlaves(source, value))
                        {
                            case Enums.Busy:
                                {
                                    break;
                                }
                            case Enums.Finished:
                                {
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves == Finished.");
                                    StateMachine = State.FlashTrackamplifiers;
                                    break;
                                }
                            case Enums.Error:
                                {
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves == Error.");
                                    StateMachine = State.Idle;
                                    returnval = Enums.Error;
                                    break;
                                }
                            default:
                                {
                                    StateMachine = State.Idle;
                                    break;
                                }
                        }
                        break;
                    }

                default:
                    break;
            }

            return returnval;
        }

        #endregion

        #region internal methods

        #region ConnectToEthernetTarget
        /// <summary>
        /// Method for setting up connection with EthernetTarget
        /// </summary>
        /// <returns></returns>
        private uint ConnectToEthernetTarget(string source, Int32 value)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = EnumClientCommands.CLIENT_CONNECTION_REQUEST;
                        mTrackApplicationVariables.trackControllerCommands.SendMessage = mSendMessage;
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.ConnectToEthernetTarget => CLIENT_CONNECTION_REQUEST.");
                        break;
                    }
                case 1:
                    {
                        if(source == "ReceivedMessage")
                        {
                            mReceivedMessage = mTrackApplicationVariables.trackControllerCommands.ReceivedMessage;
                            if(mReceivedMessage.TaskId == TaskId.CONTROLLER &&
                                mReceivedMessage.Taskcommand == TaskStates.CONNECTED &&
                                mReceivedMessage.Taskstate == TaskStates.DONE)
                            {
                                SubMethodState = 0;
                                returnval = Enums.Finished;
                                // Set Model variable to indicate a connection was made with the Ethernet Target
                                mTrackApplicationVariables.trackControllerCommands.EthernetTargetConnected = true;
                                mTrackApplicationLogging.Log(GetType().Name, "State.ConnectToEthernetTarget => CLIENT_CONNECTED.");
                            }
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            return returnval;
        }
        #endregion

        #region ResetAllSlaves
        private uint ResetAllSlaves(string source, Int32 value)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_RESET;
                        mTrackApplicationVariables.trackControllerCommands.SendMessage = mSendMessage;
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.ResetAllSlaves => EXEC_MBUS_STATE_RESET.");
                        break;
                    }
                case 1:
                    {
                        if (source == "ReceivedMessage")
                        {
                            mReceivedMessage = mTrackApplicationVariables.trackControllerCommands.ReceivedMessage;
                            if (mReceivedMessage.TaskId == TaskId.MBUS &&
                                mReceivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_RESET &&
                                mReceivedMessage.Taskstate == TaskStates.DONE)
                            {
                                mTrackApplicationLogging.Log(GetType().Name, "State.ResetAllSlaves => MBUS_STATE_RESET.");
                                mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVES_ON;
                                mTrackApplicationVariables.trackControllerCommands.SendMessage = mSendMessage;
                                SubMethodState += 1;                                
                                mTrackApplicationLogging.Log(GetType().Name, "State.ResetAllSlaves => EXEC_MBUS_STATE_SLAVES_ON.");
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        if (source == "ReceivedMessage")
                        {
                            mReceivedMessage = mTrackApplicationVariables.trackControllerCommands.ReceivedMessage;
                            if (mReceivedMessage.TaskId == TaskId.MBUS &&
                                mReceivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVES_ON &&
                                mReceivedMessage.Taskstate == TaskStates.DONE)
                            {
                                mTrackApplicationLogging.Log(GetType().Name, "State.ResetAllSlaves => MBUS_STATE_SLAVES_ON.");
                                SubMethodState = 0;
                                returnval = Enums.Finished;
                            }
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            return returnval;
        }
        #endregion

        #region DataUploadStart
        private uint DataUploadStart(string source, Int32 value)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD;
                        mTrackApplicationVariables.trackControllerCommands.SendMessage = mSendMessage;
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.DataUploadStart => EXEC_MBUS_STATE_START_DATA_UPLOAD.");
                        break;
                    }
                case 1:
                    {
                        if (source == "ReceivedMessage")
                        {
                            mReceivedMessage = mTrackApplicationVariables.trackControllerCommands.ReceivedMessage;
                            if (mReceivedMessage.TaskId == TaskId.MBUS &&
                                mReceivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD &&
                                mReceivedMessage.Taskstate == TaskStates.DONE)
                            {
                                mTrackApplicationLogging.Log(GetType().Name, "State.DataUploadStart => MBUS_STATE_START_DATA_UPLOAD.");
                                SubMethodState = 0;
                                returnval = Enums.Finished;
                            }
                        }
                        break;
                    }                
                default:
                    {
                        break;
                    }
            }
            return returnval;
        }
        #endregion

        #region DetectSlaves
        private uint loopcounter = 1;
        private static uint attemptmax = 3;
        private uint DetectSlaves(string source, Int32 value)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT;
                        mTrackApplicationVariables.trackControllerCommands.SendMessage = mSendMessage;
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => EXEC_MBUS_STATE_SLAVE_DETECT.");
                        break;
                    }
                case 1:
                    {
                        if (source == "ReceivedMessage")
                        {
                            mReceivedMessage = mTrackApplicationVariables.trackControllerCommands.ReceivedMessage;
                            if (mReceivedMessage.TaskId == TaskId.MBUS &&
                                mReceivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_DETECT &&
                                mReceivedMessage.Taskstate == TaskStates.DONE)
                            {
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => MBUS_STATE_SLAVE_DETECT.");

                                Thread.Sleep(2000);

                                uint count = 0;

                                foreach(TrackAmplifierItem amplifier in mTrackApplicationVariables.trackAmpItems)
                                {
                                    if (amplifier.SlaveDetected == 1)
                                    {
                                        count++;
                                        Console.WriteLine("DetectSlaves --> slave " + amplifier.SlaveNumber.ToString() + " detected.");
                                        mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => slave " + amplifier.SlaveNumber.ToString() + " detected.");
                                    }
                                }

                                Console.WriteLine("DetectSlaves --> " + count.ToString() + " slaves in total detected.");
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => " + count.ToString() + " slaves in total detected.");

                                if (count > 3)
                                {
                                    Thread.Sleep(1000);
                                    SubMethodState = 0;
                                    returnval = Enums.Finished;
                                }
                                else if (loopcounter > attemptmax)
                                {
                                    Console.WriteLine("DetectSlaves --> more then " + loopcounter.ToString() + " recovery attempts, stopping program!");
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => more then " + loopcounter.ToString() + " recovery attempts, stopping program!");
                                    SubMethodState = 0;
                                    returnval = Enums.Error;
                                }
                                else
                                {
                                    Console.WriteLine("DetectSlaves --> Start checking if one slave is stuck in bootloader mode, attempt " + 
                                        loopcounter.ToString() + " of " + attemptmax.ToString() + ".");
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => Start checking if one slave is stuck in bootloader mode, attempt " +
                                        loopcounter.ToString() + " of " + attemptmax.ToString() + ".");
                                    SubMethodState = 2;
                                }                                
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH;
                        mTrackApplicationVariables.trackControllerCommands.SendMessage = mSendMessage;
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => EXEC_MBUS_STATE_SLAVE_FW_FLASH.");
                        break;
                    }
                case 3:
                    {
                        if (source == "ReceivedMessage")
                        {
                            mReceivedMessage = mTrackApplicationVariables.trackControllerCommands.ReceivedMessage;
                            if (mReceivedMessage.TaskId == TaskId.FWHANDLER &&
                                mReceivedMessage.Taskcommand == TrackCommand.FWHANDLERINIT &&
                                mReceivedMessage.Taskstate == TaskStates.CONNECTED)
                            {
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => FWHANDLER CONNECTED.");
                                mSendMessage.Command = TrackCommand.EXEC_FW_STATE_GET_BOOTLOADER_VERSION;
                                mTrackApplicationVariables.trackControllerCommands.SendMessage = mSendMessage;
                                SubMethodState += 1;
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => EXEC_FW_STATE_GET_BOOTLOADER_VERSION.");
                            }
                        }
                        break;
                    }
                case 4:
                    {
                        if (source == "ReceivedMessage")
                        {
                            mReceivedMessage = mTrackApplicationVariables.trackControllerCommands.ReceivedMessage;
                            if (mReceivedMessage.TaskId == TrackCommand.GET_BOOTLOADER_VERSION &&
                                (mReceivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_OK ||
                                 mReceivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_NOK ||
                                 mReceivedMessage.Taskcommand == TrackCommand.BOOTLOADER_START_BYTE_ERROR ||
                                 mReceivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT) &&
                                (mReceivedMessage.Taskstate == TaskStates.DONE ||
                                 mReceivedMessage.Taskstate == TaskStates.ERROR))
                            {
                                if (mReceivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_OK &&
                                    mReceivedMessage.Taskstate == TaskStates.DONE)
                                {
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => GET_BOOTLOADER_VERSION_OK.");
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => Found a slave in bootloader mode!");
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => Start flash sequence for this slave only.");
                                }
                                else if ((mReceivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_NOK ||
                                          mReceivedMessage.Taskcommand == TrackCommand.BOOTLOADER_START_BYTE_ERROR ||
                                          mReceivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT) &&
                                         mReceivedMessage.Taskstate == TaskStates.ERROR)
                                {
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => GET_BOOTLOADER_VERSION_NOK.");
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => No slave found, stopping slave detection.");
                                    SubMethodState = 0;
                                    returnval = Enums.Error;
                                    break;
                                }
                                else
                                {
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => GET_BOOTLOADER_VERSION_NOK.");
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => No slave found, stopping slave detection.");
                                    SubMethodState = 0;
                                    returnval = Enums.Error;
                                    break;
                                }

                                mSendMessage.Command = TrackCommand.EXEC_FW_STATE_ERASE_FLASH;
                                mTrackApplicationVariables.trackControllerCommands.SendMessage = mSendMessage;
                                SubMethodState += 1;
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => EXEC_FW_STATE_ERASE_FLASH.");
                            }
                        }
                        break;
                    }
                case 5:
                    {
                        if (source == "ReceivedMessage")
                        {
                            mReceivedMessage = mTrackApplicationVariables.trackControllerCommands.ReceivedMessage;
                            if (mReceivedMessage.TaskId == TrackCommand.ERASE_FLASH &&
                                mReceivedMessage.Taskcommand == TrackCommand.ERASE_FLASH_RETURNED_OK &&
                                mReceivedMessage.Taskstate == TaskStates.DONE)
                            {
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => ERASE_FLASH_RETURNED_OK.");
                                x
                            }
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            return returnval;
        }
        #endregion

        #endregion
    }
}
