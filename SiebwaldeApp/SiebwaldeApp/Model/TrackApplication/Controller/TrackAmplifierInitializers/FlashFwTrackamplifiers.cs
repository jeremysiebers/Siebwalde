using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SiebwaldeApp
{
    public class FlashFwTrackamplifiers : IAmplifierInitializersBaseClass
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
        // Set the recovery iteration counter
        private int IterationCounter { get; set; }
        // Set the Flas required var
        private uint FwFlashRequired { get; set; }
        // Get enew stopwatch
        private Stopwatch sw = new Stopwatch();        
        // Get the amount of iterations required to step through the uController flash memory
        private readonly uint Iterations = ((Enums.PROGMEMSIZE - Enums.BOOTLOADEROFFSET) / Enums.HEXROWWIDTH) - Enums.JUMPSIZE; // fixed (and max) jump size of 4 rows
        // The name of the class
        public string Name { get; set; }

        #endregion

        #region Constructor
        /// <summary>
        /// Setup the Connect to Ethernet target
        /// </summary>
        public FlashFwTrackamplifiers(string LoggerInstance, TrackIOHandle trackIOHandle, TrackApplicationVariables trackApplicationVariables,
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
            // Set the iteration counter
            IterationCounter = 0;
            // Set the FwFlashrequired var
            FwFlashRequired = 0;

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

            switch (SubMethodState)
            {
                case 0:
                    {
                        IoC.Logger.Log("State.FlashFwTrackamplifiers => Expected Slave CheckSum = 0x" + mTrackAmplifierBootloaderHelpers.GetFileCheckSum.ToString("X") + ".", mLoggerInstance);

                        foreach (TrackAmplifierItem amplifierItem in mTrackApplicationVariables.trackAmpItems)
                        {
                            if (mTrackAmplifierBootloaderHelpers.GetFileCheckSum != amplifierItem.HoldingReg[11] &&
                                amplifierItem.SlaveDetected == 1 && amplifierItem.SlaveNumber > 0 &&
                                amplifierItem.SlaveNumber < 51)
                            {
                                FwFlashRequired += 1;
                                IoC.Logger.Log("State.FlashFwTrackamplifiers => Slave " + amplifierItem.SlaveNumber.ToString() +
                                    " has checksum = 0x" + amplifierItem.HoldingReg[11].ToString("X") +
                                    " and requires flashing", mLoggerInstance);
                            }
                        }

                        if (FwFlashRequired > 0)
                        {
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => FwFlashRequired == true.", mLoggerInstance);
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => " + FwFlashRequired.ToString() + " slaves require flashing", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH;
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => EXEC_MBUS_STATE_SLAVE_FW_FLASH.", mLoggerInstance);
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                        }
                        else
                        {
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => FwFlashRequired == false.", mLoggerInstance);
                            SubMethodState = 0;
                            returnval = Enums.Finished;
                        }
                        break;
                    }

                case 1:
                    {
                        if (receivedMessage.TaskId == TaskId.FWHANDLER &&
                            receivedMessage.Taskcommand == TrackCommand.FWHANDLERINIT &&
                            receivedMessage.Taskstate == TaskStates.CONNECTED)
                        {
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => FWHANDLER CONNECTED.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE;
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE.", mLoggerInstance);
                        }
                        break;
                    }

                case 2:
                    {
                        if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY.", mLoggerInstance);

                            mSendNextFwDataPacket.Execute();

                            SubMethodState = 4;
                        }
                        break;
                    }

                case 3:
                    {
                        if (IterationCounter > Iterations)
                        {
                            IterationCounter = 0;
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE DONE.", mLoggerInstance);
                            Console.WriteLine("All packages sent.");
                            SubMethodState = 5;
                            break;
                        }
                        mSendNextFwDataPacket.Execute();
                        SubMethodState += 1;
                        break;
                    }

                case 4:
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
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE DONE.", mLoggerInstance);
                            Console.WriteLine("All packages sent.");
                            SubMethodState = 5;
                        }
                        break;
                    }

                case 5:
                    {
                        if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM &&
                        receivedMessage.Taskstate == TaskStates.DONE &&
                        receivedMessage.Taskmessage == TaskMessages.RECEIVED_CHECKSUM_OK)
                        {
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => RECEIVED_CHECKSUM_OK.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD;
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_CONFIG_WORD.", mLoggerInstance);
                            SubMethodState += 1;
                        }
                        else if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM &&
                        receivedMessage.Taskstate == TaskStates.DONE &&
                        receivedMessage.Taskmessage == TaskMessages.RECEIVED_CHECKSUM_NOK)
                        {
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => RECEIVED_CHECKSUM_NOK.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE;
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE try again.", mLoggerInstance);
                            SubMethodState = 2;
                        }
                        break;
                    }

                case 6:
                    {
                        if (receivedMessage.TaskId == TrackCommand.FWCONFIGWORDDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY.", mLoggerInstance);

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

                case 7:
                    {
                        if (receivedMessage.TaskId == TrackCommand.FWCONFIGWORDDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE.", mLoggerInstance);
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES;
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => EXEC_FW_STATE_FLASH_ALL_SLAVES.", mLoggerInstance);
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            sw.Start();
                            SubMethodState += 1;
                        }
                        break;
                    }

                case 8:
                    {
                        if (receivedMessage.TaskId == TaskId.FWHANDLER &&
                        receivedMessage.Taskcommand == TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            long elapsedtime = sw.ElapsedMilliseconds;
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => Flashing took " + Convert.ToString(elapsedtime / 1000) + " seconds.", mLoggerInstance);
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => That is on average " + Convert.ToString((float)elapsedtime / FwFlashRequired / 1000) + " seconds per slave.", mLoggerInstance);
                            IoC.Logger.Log("State.FlashFwTrackamplifiers => EXEC_FW_STATE_FLASH_ALL_SLAVES DONE.", mLoggerInstance);
                            sw.Stop();
                            SubMethodState = 0;
                            returnval = Enums.Finished;
                        }

                        //else if (receivedMessage.TaskId != 0 &&
                        //receivedMessage.Taskcommand != 0 &&
                        //receivedMessage.Taskstate != 0)
                        //{
                        //    IoC.Logger.Log("State.FlashFwTrackamplifiers => Received data during flashing: TaskId = "
                        //        + Convert.ToString(receivedMessage.TaskId) + " Taskcommand = " + Convert.ToString(receivedMessage.Taskcommand)
                        //        + " Taskstate = " + Convert.ToString(receivedMessage.Taskstate) + " Taskmessage = " + Convert.ToString(receivedMessage.Taskmessage) + ".", mLoggerInstance);
                        //}
                        break;
                    }

                default:
                    {
                        break;
                    }
            }
            return (returnval, "");
        }
        #endregion
    }
}
