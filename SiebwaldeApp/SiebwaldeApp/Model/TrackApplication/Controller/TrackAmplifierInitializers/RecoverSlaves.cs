using System;
using System.Collections.Generic;

namespace SiebwaldeApp
{
    public class RecoverSlaves : IAmplifierInitializersBaseClass
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
        public RecoverSlaves(string LoggerInstance, TrackIOHandle trackIOHandle, TrackApplicationVariables trackApplicationVariables,
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
        /// Call until Finished returns, this sequence tries to recover a failed flash event by detecting and recovering the slave (only 1 slave should have a problem)
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
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH;
                        mTrackIOHandle.ActuatorCmd(mSendMessage);
                        SubMethodState += 1;
                        IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_MBUS_STATE_SLAVE_FW_FLASH.", mLoggerInstance);
                        break;
                    }
                case 1:
                    {
                        if (receivedMessage.TaskId == TaskId.FWHANDLER &&
                            receivedMessage.Taskcommand == TrackCommand.FWHANDLERINIT &&
                            receivedMessage.Taskstate == TaskStates.CONNECTED)
                        {
                            IoC.Logger.Log("State.DetectSlaveRecovery => FWHANDLER CONNECTED.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_GET_BOOTLOADER_VERSION;
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                            IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_FW_STATE_GET_BOOTLOADER_VERSION.", mLoggerInstance);
                        }
                        break;
                    }
                case 2:
                    {
                        if (receivedMessage.TaskId == TrackCommand.GET_BOOTLOADER_VERSION &&
                            (receivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_OK ||
                                receivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_NOK ||
                                receivedMessage.Taskcommand == TrackCommand.BOOTLOADER_START_BYTE_ERROR ||
                                receivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT) &&
                            (receivedMessage.Taskstate == TaskStates.DONE ||
                                receivedMessage.Taskstate == TaskStates.ERROR))
                        {
                            if (receivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_OK &&
                                receivedMessage.Taskstate == TaskStates.DONE)
                            {
                                IoC.Logger.Log("State.DetectSlaveRecovery => GET_BOOTLOADER_VERSION_OK.", mLoggerInstance);
                                IoC.Logger.Log("State.DetectSlaveRecovery => Found a slave in bootloader mode!", mLoggerInstance);
                                IoC.Logger.Log("State.DetectSlaveRecovery => Start flash sequence for this slave only.", mLoggerInstance);
                                mSendMessage.Command = TrackCommand.EXEC_FW_STATE_ERASE_FLASH;
                                mTrackIOHandle.ActuatorCmd(mSendMessage);
                                SubMethodState += 1;
                                IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_FW_STATE_ERASE_FLASH.", mLoggerInstance);
                            }
                            else if ((receivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_NOK ||
                                        receivedMessage.Taskcommand == TrackCommand.BOOTLOADER_START_BYTE_ERROR ||
                                        receivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT) &&
                                        receivedMessage.Taskstate == TaskStates.ERROR)
                            {
                                IoC.Logger.Log("State.DetectSlaveRecovery => GET_BOOTLOADER_VERSION_NOK.", mLoggerInstance);
                                IoC.Logger.Log("State.DetectSlaveRecovery => No slave found, stopping slave detection.", mLoggerInstance);
                                SubMethodState = 0;
                                returnval = Enums.Error;
                            }
                            else
                            {
                                IoC.Logger.Log("State.DetectSlaveRecovery => GET_BOOTLOADER_VERSION_NOK.", mLoggerInstance);
                                IoC.Logger.Log("State.DetectSlaveRecovery => No slave found, stopping slave detection.", mLoggerInstance);
                                SubMethodState = 0;
                                returnval = Enums.Error;
                            }

                        }
                        break;
                    }

                case 3:
                    {
                        if (receivedMessage.TaskId == TrackCommand.ERASE_FLASH &&
                        receivedMessage.Taskcommand == TrackCommand.ERASE_FLASH_RETURNED_OK &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.DetectSlaveRecovery => ERASE_FLASH_RETURNED_OK.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE;
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                            IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_FW_FILE.", mLoggerInstance);
                        }
                        break;
                    }

                case 4:
                    {
                        if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.DetectSlaveRecovery => FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY.", mLoggerInstance);
                            mSendNextFwDataPacket.Execute();
                            SubMethodState = 6;
                        }
                        break;
                    }

                case 5:
                    {
                        if (IterationCounter > Iterations)
                        {
                            IterationCounter = 0;
                            IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_FW_FILE DONE.", mLoggerInstance);
                            SubMethodState += 2;
                        }

                        mSendMessage.Command = TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE;

                        List<byte> Data = new List<byte>();

                        for (int i = IterationCounter; i < (IterationCounter + Enums.JUMPSIZE); i++)
                        {
                            foreach (byte val in mTrackAmplifierBootloaderHelpers.GetHexFileData[i][1])
                            {
                                Data.Add(val);
                            }
                        }
                        mSendMessage.Data = Data.ToArray();
                        mTrackIOHandle.ActuatorCmd(mSendMessage);
                        IterationCounter += Enums.JUMPSIZE;
                        SubMethodState += 1;
                        break;
                    }

                case 6:
                    {
                        if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            //Console.WriteLine("Ethernet target received package, ready for next one.");
                            mSendNextFwDataPacket.Execute();
                            //SubMethodState -= 1;
                        }
                        else if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IterationCounter = 0;
                            IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_FW_FILE DONE.", mLoggerInstance);
                            Console.WriteLine("All packages sent.");
                            SubMethodState = 7;
                        }
                        break;
                    }

                case 7:
                    {
                        if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM &&
                        receivedMessage.Taskstate == TaskStates.DONE &&
                        receivedMessage.Taskmessage == TaskMessages.RECEIVED_CHECKSUM_OK)
                        {
                            IoC.Logger.Log("State.DetectSlaveRecovery => RECEIVED_CHECKSUM_OK.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD;
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_CONFIG_WORD.", mLoggerInstance);
                            SubMethodState += 1;
                        }
                        else if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM &&
                        receivedMessage.Taskstate == TaskStates.DONE &&
                        receivedMessage.Taskmessage == TaskMessages.RECEIVED_CHECKSUM_NOK)
                        {
                            IoC.Logger.Log("State.DetectSlaveRecovery => RECEIVED_CHECKSUM_NOK.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE;
                            IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_FW_FILE try again.", mLoggerInstance);
                            SubMethodState = 4;
                        }
                        break;
                    }

                case 8:
                    {
                        if (receivedMessage.TaskId == TrackCommand.FWCONFIGWORDDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.DetectSlaveRecovery => CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY.", mLoggerInstance);

                            mSendMessage.Command = TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE;

                            List<byte> Data = new List<byte>();

                            foreach (byte val in mTrackAmplifierBootloaderHelpers.GetConfigWord)
                            {
                                Data.Add(val);
                            }
                            mSendMessage.Data = Data.ToArray();
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                        }
                        break;
                    }

                case 9:
                    {
                        if (receivedMessage.TaskId == TrackCommand.FWCONFIGWORDDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.DetectSlaveRecovery => CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_WRITE_FLASH;
                            IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_FW_STATE_WRITE_FLASH.", mLoggerInstance);
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                        }
                        break;
                    }

                case 10:
                    {
                        if (receivedMessage.TaskId == TrackCommand.WRITE_FLASH &&
                        receivedMessage.Taskcommand == TrackCommand.WRITE_FLASH_RETURNED_OK &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.DetectSlaveRecovery => WRITE_FLASH_RETURNED_OK.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_WRITE_CONFIG;
                            IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_FW_STATE_WRITE_CONFIG.", mLoggerInstance);
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                        }
                        break;
                    }

                case 11:
                    {
                        if (receivedMessage.TaskId == TrackCommand.WRITE_CONFIG &&
                        receivedMessage.Taskcommand == TrackCommand.WRITE_CONFIG_RETURNED_OK &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.DetectSlaveRecovery => WRITE_CONFIG_RETURNED_OK.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_CHECK_CHECKSUM;
                            IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_FW_STATE_CHECK_CHECKSUM.", mLoggerInstance);
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                        }
                        break;
                    }

                case 12:
                    {
                        if (receivedMessage.TaskId == TrackCommand.CHECK_CHECKSUM_CONFIG &&
                        receivedMessage.Taskcommand == TrackCommand.CHECK_CHECKSUM_CONFIG_RETURNED_OK &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.DetectSlaveRecovery => CHECK_CHECKSUM_CONFIG_RETURNED_OK.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_SLAVE_RESET;
                            IoC.Logger.Log("State.DetectSlaveRecovery => EXEC_FW_STATE_SLAVE_RESET.", mLoggerInstance);
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                        }
                        break;
                    }

                case 13:
                    {
                        if (receivedMessage.TaskId == TrackCommand.RESET_SLAVE &&
                        receivedMessage.Taskcommand == TrackCommand.RESET_SLAVE_OK &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.DetectSlaveRecovery => RESET_SLAVE_OK.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXIT_SLAVExFWxHANDLER;
                            IoC.Logger.Log("State.DetectSlaveRecovery => EXIT_SLAVExFWxHANDLER.", mLoggerInstance);
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState = 0;
                            // keep track of the retries
                            loopcounter += 1;
                            returnval = Enums.Next;
                            CallNext = "DetectSlaves";
                        }
                        break;
                    }

            }
            return (returnval, CallNext);
        }

        #endregion
    }
}
