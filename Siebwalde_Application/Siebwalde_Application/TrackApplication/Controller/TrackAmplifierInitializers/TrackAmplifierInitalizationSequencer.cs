using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Siebwalde_Application
{
    /// <summary>
    /// This TrackAmplifierInitalizationSequencer initializes all the Track amplifiers and checks if firmware download is required
    /// </summary>
    public class TrackAmplifierInitalizationSequencer
    {
        #region Local variables

        private TrackApplicationVariables mTrackApplicationVariables;
        private ILogger mTrackApplicationLogging;
        private TrackAmplifierBootloaderHelpers mTrackAmplifierBootloaderHelpers;
        private TrackIOHandle mTrackIOHandle;
        private SendMessage mSendMessage;
        private ReceivedMessage dummyReceivedMessage = new ReceivedMessage();
        private Stopwatch sw = new Stopwatch();
        private object ExecuteLock = new object();
        private System.Timers.Timer AppUpdateTimer = new System.Timers.Timer();

        /// <summary>
        /// This enum holds all the possible states of the TrackAmplifierInitalizationSequencer statemachine
        /// </summary>
        private enum State { Idle, Reset, ConnectToEthernetTarget, ResetAllSlaves, DataUploadStart, DetectSlaves,
            DetectSlaveRecovery, FlashFwTrackamplifiers, InitTrackamplifiers, EnableTrackamplifiers
        };
        private State StateMachine;
        private uint SubMethodState;

        public uint SlaveCount { get; private set; }
        public uint CheckInitSequence { get; internal set; }

        #endregion

        #region Constructor

        public TrackAmplifierInitalizationSequencer(ILogger trackApplicationLogging, TrackApplicationVariables trackApplicationVariables, TrackIOHandle trackIOHandle)
        {
            mTrackApplicationVariables = trackApplicationVariables;
            mTrackApplicationLogging = trackApplicationLogging;
            mTrackIOHandle = trackIOHandle;
            mTrackAmplifierBootloaderHelpers = new TrackAmplifierBootloaderHelpers("C:\\GIT-REPOS\\Siebwalde\\TrackAmplifier4.X\\dist\\Offset\\production\\TrackAmplifier4.X.production.hex", trackApplicationLogging);//(Enums.HOMEPATH + Enums.SLAVEHEXFILE, trackApplicationLogging);

            StateMachine = State.Idle;
            SubMethodState = 0;
            SlaveCount = 0;
            CheckInitSequence = Enums.Standby;

            byte[] DummyData = new byte[80];
            mSendMessage = new SendMessage(0, DummyData);

            // subscribe to commands set in the TrackControllerCommands class
            if(mTrackApplicationVariables != null)
            {
                mTrackApplicationVariables.trackControllerCommands.PropertyChanged += new PropertyChangedEventHandler(TrackControllerCommands_PropertyChanged);
            }            
        }

        /// <summary>
        /// Start of generation of dependencies within this program
        /// </summary>
        public void Start()
        {
            // Load the HEX-file and readout all the data required
            mTrackApplicationLogging.Log(GetType().Name, "TrackAmplifierBootloaderHelpers.Start()");
            mTrackAmplifierBootloaderHelpers.Start();

            // set the state machine to the first case
            StateMachine = State.ConnectToEthernetTarget;

            // set the status of this program to busy
            CheckInitSequence = Enums.Busy;

            AppUpdateTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            AppUpdateTimer.Interval = 1;
            AppUpdateTimer.AutoReset = true;
            // Enable the timer
            AppUpdateTimer.Enabled = true;
        }

        #endregion

        #region TrackControllerCommands_PropertyChanged
        /// <summary>
        /// Property changes event handler on TrackControllerCommands these will be coming typically from the Gui via the viewModel or
        /// from subclasses sending commands to the Ethernet target
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackControllerCommands_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ReceivedMessage":
                    {
                        InitSequence((ReceivedMessage)sender.GetType().GetProperty(e.PropertyName).GetValue(sender));
                        break;
                    }

                default:
                    {
                        break;
                    }                                      
            }
        }

        #endregion

        #region OnTimedEvent

        /// <summary>
        /// Timer event to kick TrackApplication
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            InitSequence(dummyReceivedMessage);
        }

        #endregion

        #region Internal statemachine

        internal void InitSequence(ReceivedMessage receivedMessage)
        {

            // Lock the execution since multiple events may arrive
            lock (ExecuteLock)
            {
                // stop the timer to prevent re-starting during execution of code
                AppUpdateTimer.Stop();

                uint returnval = Enums.Busy;

                if(receivedMessage.TaskId != 0)
                {
                    Console.WriteLine("Received message: TaskId = " +
                    receivedMessage.TaskId.ToString() + ", Taskcommand = " +
                    receivedMessage.Taskcommand.ToString() + ", Taskstate = " +
                    receivedMessage.Taskstate.ToString() + ", Taskmessage = " +
                    receivedMessage.Taskmessage.ToString() + ".");
                }
                

                // Check if the read of the HEX file was successful otherwise return an error to the caller
                if (!mTrackAmplifierBootloaderHelpers.HexFileReadSuccessful)
                {
                    mTrackApplicationLogging.Log(GetType().Name, "TrackAmplifierBootloaderHelpers.HexFileReadSuccessful == false stopping init request.");
                    CheckInitSequence = Enums.Error;
                    return;
                }

                // Init statemachine
                switch (StateMachine)
                {
                    case State.Idle:
                        {
                            returnval = Enums.Finished;
                            break;
                        }

                    case State.Reset:
                        {
                            // Here all sub classes reset methods are called in case of a forced reset
                            returnval = Enums.Finished;
                            break;
                        }


                    case State.ConnectToEthernetTarget:
                        {
                            switch (ConnectToEthernetTarget(receivedMessage))
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
                            switch (ResetAllSlaves(receivedMessage))
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
                            switch (DataUploadStart(receivedMessage))
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
                            switch (DetectSlaves(receivedMessage))
                            {
                                case Enums.Busy:
                                    {
                                        break;
                                    }
                                case Enums.Finished:
                                    {
                                        mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves == Finished.");
                                        StateMachine = State.FlashFwTrackamplifiers;
                                        break;
                                    }
                                case Enums.Next:
                                    {
                                        mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => State.DetectSlaveRecovery.");
                                        StateMachine = State.DetectSlaveRecovery;
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

                    case State.DetectSlaveRecovery:
                        {
                            switch (DetectSlaveRecovery(receivedMessage))
                            {
                                case Enums.Busy:
                                    {
                                        break;
                                    }
                                case Enums.Finished:
                                    {
                                        mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery == Finished.");
                                        StateMachine = State.FlashFwTrackamplifiers;
                                        break;
                                    }
                                case Enums.Next:
                                    {
                                        mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => State.DetectSlaves.");
                                        StateMachine = State.DetectSlaves;
                                        break;
                                    }
                                case Enums.Error:
                                    {
                                        mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery == Error.");
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

                    case State.FlashFwTrackamplifiers:
                        {
                            switch (FlashFwTrackamplifiers(receivedMessage))
                            {
                                case Enums.Busy:
                                    {
                                        break;
                                    }
                                case Enums.Finished:
                                    {
                                        mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers == Finished.");
                                        StateMachine = State.InitTrackamplifiers;
                                        break;
                                    }
                                case Enums.Error:
                                    {
                                        mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers == Error.");
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

                    case State.InitTrackamplifiers:
                        {
                            switch (InitTrackamplifiers(receivedMessage))
                            {
                                case Enums.Busy:
                                    {
                                        break;
                                    }
                                case Enums.Finished:
                                    {
                                        mTrackApplicationLogging.Log(GetType().Name, "State.InitTrackamplifiers == Finished.");
                                        StateMachine = State.EnableTrackamplifiers;
                                        break;
                                    }
                                case Enums.Error:
                                    {
                                        mTrackApplicationLogging.Log(GetType().Name, "State.InitTrackamplifiers == Error.");
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

                    case State.EnableTrackamplifiers:
                        {
                            switch (EnableTrackamplifiers(receivedMessage))
                            {
                                case Enums.Busy:
                                    {
                                        break;
                                    }
                                case Enums.Finished:
                                    {
                                        mTrackApplicationLogging.Log(GetType().Name, "State.EnableTrackamplifiers == Finished.");
                                        StateMachine = State.Idle;
                                        returnval = Enums.Finished;
                                        break;
                                    }
                                case Enums.Error:
                                    {
                                        mTrackApplicationLogging.Log(GetType().Name, "State.EnableTrackamplifiers == Error.");
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

                CheckInitSequence = returnval;

                // Check if the timer has to be started again
                if(CheckInitSequence != Enums.Finished && CheckInitSequence != Enums.Error)
                {
                    AppUpdateTimer.Start();
                }
                else
                {
                    AppUpdateTimer.Close();
                }
                
            }            
        }

        #endregion

        #region internal methods

        #region ConnectToEthernetTarget
        /// <summary>
        /// Method for setting up connection with EthernetTarget
        /// </summary>
        /// <returns></returns>
        private uint ConnectToEthernetTarget(ReceivedMessage receivedMessage)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = EnumClientCommands.CLIENT_CONNECTION_REQUEST;
                        mTrackIOHandle.ActuatorCmd(mSendMessage);
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.ConnectToEthernetTarget => CLIENT_CONNECTION_REQUEST.");
                        break;
                    }
                case 1:
                    {
                        //if(source == "ReceivedMessage")
                        //{
                            //mReceivedMessage = mTrackApplicationVariables.trackControllerCommands.ReceivedMessage;
                            if(receivedMessage.TaskId == TaskId.CONTROLLER &&
                                receivedMessage.Taskcommand == TaskStates.CONNECTED &&
                                receivedMessage.Taskstate == TaskStates.DONE)
                            {
                                SubMethodState = 0;
                                returnval = Enums.Finished;
                                // Set Model variable to indicate a connection was made with the Ethernet Target
                                mTrackApplicationVariables.trackControllerCommands.EthernetTargetConnected = true;
                                mTrackApplicationLogging.Log(GetType().Name, "State.ConnectToEthernetTarget => CLIENT_CONNECTED.");
                            }
                        //}
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
        private uint ResetAllSlaves(ReceivedMessage receivedMessage)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_RESET;
                        mTrackIOHandle.ActuatorCmd(mSendMessage);
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.ResetAllSlaves => EXEC_MBUS_STATE_RESET.");
                        break;
                    }
                case 1:
                    {
                        if (receivedMessage.TaskId == TaskId.MBUS &&
                            receivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_RESET &&
                            receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.ResetAllSlaves => MBUS_STATE_RESET.");
                            mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVES_ON;
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;                                
                            mTrackApplicationLogging.Log(GetType().Name, "State.ResetAllSlaves => EXEC_MBUS_STATE_SLAVES_ON.");
                        }                        
                        break;
                    }
                case 2:
                    {
                        if (receivedMessage.TaskId == TaskId.MBUS &&
                            receivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVES_ON &&
                            receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.ResetAllSlaves => MBUS_STATE_SLAVES_ON.");
                            SubMethodState = 0;
                            returnval = Enums.Finished;
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
        private uint DataUploadStart(ReceivedMessage receivedMessage)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD;
                        mTrackIOHandle.ActuatorCmd(mSendMessage);
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.DataUploadStart => EXEC_MBUS_STATE_START_DATA_UPLOAD.");
                        break;
                    }
                case 1:
                    {
                        if (receivedMessage.TaskId == TaskId.MBUS &&
                            receivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD &&
                            receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.DataUploadStart => MBUS_STATE_START_DATA_UPLOAD.");
                            SubMethodState = 0;
                            returnval = Enums.Finished;
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
        private uint DetectSlaves(ReceivedMessage receivedMessage)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT;
                        mTrackIOHandle.ActuatorCmd(mSendMessage);
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => EXEC_MBUS_STATE_SLAVE_DETECT.");
                        break;
                    }
                case 1:
                    {
                        if (receivedMessage.TaskId == TaskId.MBUS &&
                            receivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_DETECT &&
                            receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => MBUS_STATE_SLAVE_DETECT.");

                            Thread.Sleep(2000);

                            SlaveCount = 0;

                            foreach(TrackAmplifierItem amplifier in mTrackApplicationVariables.trackAmpItems)
                            {
                                if (amplifier.SlaveDetected == 1)
                                {
                                    SlaveCount++;
                                    Console.WriteLine("DetectSlaves --> slave " + amplifier.SlaveNumber.ToString() + " detected.");
                                    mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => slave " + amplifier.SlaveNumber.ToString() + " detected.");
                                }
                            }

                            Console.WriteLine("DetectSlaves --> " + SlaveCount.ToString() + " slaves in total detected.");
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => " + SlaveCount.ToString() + " slaves in total detected.");

                            if (SlaveCount > 2)
                            {
                                Thread.Sleep(1000);
                                SubMethodState = 0;
                                loopcounter = 1;
                                returnval = Enums.Finished;
                            }
                            else if (loopcounter > attemptmax)
                            {
                                Console.WriteLine("DetectSlaves --> more then " + loopcounter.ToString() + " recovery attempts, stopping program!");
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => more then " + loopcounter.ToString() + " recovery attempts, stopping program!");
                                SubMethodState = 0;
                                loopcounter = 1;
                                returnval = Enums.Error;
                            }
                            else
                            {
                                Console.WriteLine("DetectSlaves --> Start checking if one slave is stuck in bootloader mode, attempt " + 
                                    loopcounter.ToString() + " of " + attemptmax.ToString() + ".");
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaves => Start checking if one slave is stuck in bootloader mode, attempt " +
                                    loopcounter.ToString() + " of " + attemptmax.ToString() + ".");
                                SubMethodState = 0;
                                // try to recover slave by running a "next" sequence
                                returnval = Enums.Next;
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

        #region DetectSlaveRecovery
        private int IterationCounter = 0;
        private readonly uint ProcessLines = (Enums.PROGMEMSIZE - Enums.BOOTLOADEROFFSET) / Enums.HEXROWWIDTH;
        private readonly uint Iterations = ((Enums.PROGMEMSIZE - Enums.BOOTLOADEROFFSET) / Enums.HEXROWWIDTH) - Enums.JUMPSIZE; // fixed (and max) jump size of 4 rows
        private uint DetectSlaveRecovery(ReceivedMessage receivedMessage)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH;
                        mTrackIOHandle.ActuatorCmd(mSendMessage);
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_MBUS_STATE_SLAVE_FW_FLASH.");
                        break;
                    }
                case 1:
                    {
                        if (receivedMessage.TaskId == TaskId.FWHANDLER &&
                            receivedMessage.Taskcommand == TrackCommand.FWHANDLERINIT &&
                            receivedMessage.Taskstate == TaskStates.CONNECTED)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => FWHANDLER CONNECTED.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_GET_BOOTLOADER_VERSION;
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_FW_STATE_GET_BOOTLOADER_VERSION.");
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
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => GET_BOOTLOADER_VERSION_OK.");
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => Found a slave in bootloader mode!");
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => Start flash sequence for this slave only.");
                                mSendMessage.Command = TrackCommand.EXEC_FW_STATE_ERASE_FLASH;
                                mTrackIOHandle.ActuatorCmd(mSendMessage);
                                SubMethodState += 1;
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_FW_STATE_ERASE_FLASH.");
                            }
                            else if ((receivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_NOK ||
                                        receivedMessage.Taskcommand == TrackCommand.BOOTLOADER_START_BYTE_ERROR ||
                                        receivedMessage.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT) &&
                                        receivedMessage.Taskstate == TaskStates.ERROR)
                            {
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => GET_BOOTLOADER_VERSION_NOK.");
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => No slave found, stopping slave detection.");
                                SubMethodState = 0;
                                returnval = Enums.Error;
                            }
                            else
                            {
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => GET_BOOTLOADER_VERSION_NOK.");
                                mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => No slave found, stopping slave detection.");
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
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => ERASE_FLASH_RETURNED_OK.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE;
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_FW_FILE.");
                        }
                        break;
                    }

                case 4:
                    {
                        if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY.");
                            SendNextFwDataPacket();
                            SubMethodState = 6;
                        }
                        break;
                    }

                case 5:
                    {
                        if(IterationCounter > Iterations)
                        {
                            IterationCounter = 0;
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_FW_FILE DONE.");
                            SubMethodState += 2;
                        }

                        mSendMessage.Command = TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE;

                        List<byte> Data = new List<byte>();

                        for(int i = IterationCounter; i < (IterationCounter + Enums.JUMPSIZE); i++)
                        {
                            foreach(byte val in mTrackAmplifierBootloaderHelpers.GetHexFileData[i][1])
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
                            SendNextFwDataPacket();
                            //SubMethodState -= 1;
                        }
                        else if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IterationCounter = 0;
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_FW_FILE DONE.");
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
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => RECEIVED_CHECKSUM_OK.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD; 
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_CONFIG_WORD.");
                            SubMethodState += 1;
                        }
                        else if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM &&
                        receivedMessage.Taskstate == TaskStates.DONE &&
                        receivedMessage.Taskmessage == TaskMessages.RECEIVED_CHECKSUM_NOK)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => RECEIVED_CHECKSUM_NOK.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE;
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_FW_FILE try again.");
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
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY.");

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
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_WRITE_FLASH;
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_FW_STATE_WRITE_FLASH.");
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
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => WRITE_FLASH_RETURNED_OK.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_WRITE_CONFIG;
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_FW_STATE_WRITE_CONFIG.");
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
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => WRITE_CONFIG_RETURNED_OK.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_CHECK_CHECKSUM;
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_FW_STATE_CHECK_CHECKSUM.");
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
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => CHECK_CHECKSUM_CONFIG_RETURNED_OK.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_SLAVE_RESET;
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXEC_FW_STATE_SLAVE_RESET.");
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
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => RESET_SLAVE_OK.");
                            mSendMessage.Command = TrackCommand.EXIT_SLAVExFWxHANDLER;
                            mTrackApplicationLogging.Log(GetType().Name, "State.DetectSlaveRecovery => EXIT_SLAVExFWxHANDLER.");
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState = 0;
                            // keep track of the retries
                            loopcounter += 1;
                            returnval = Enums.Next;
                        }
                        break;
                    }

            } 
            return returnval;
        }

        #endregion

        #region FlashFwTrackamplifiers
        private uint FwFlashRequired = 0;
        private uint FlashFwTrackamplifiers(ReceivedMessage receivedMessage)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => Expected Slave CheckSum = ox" + mTrackAmplifierBootloaderHelpers.GetFileCheckSum.ToString("X") + ".");

                        foreach (TrackAmplifierItem amplifierItem in mTrackApplicationVariables.trackAmpItems)
                        {
                            if(mTrackAmplifierBootloaderHelpers.GetFileCheckSum != amplifierItem.HoldingReg[11] &&
                                amplifierItem.SlaveDetected == 1 && amplifierItem.SlaveNumber > 0 &&
                                amplifierItem.SlaveNumber < 51)
                            {
                                FwFlashRequired += 1;
                                mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => Slave " + amplifierItem.SlaveNumber.ToString() + 
                                    " has checksum = 0x" + amplifierItem.HoldingReg[11].ToString("X") + 
                                    " and requires flashing");
                            }
                        }

                        if (FwFlashRequired > 0)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => FwFlashRequired == true.");
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => " + FwFlashRequired.ToString() + " slaves require flashing");
                            mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH;
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => EXEC_MBUS_STATE_SLAVE_FW_FLASH.");
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                        }
                        else
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => FwFlashRequired == false.");
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
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => FWHANDLER CONNECTED.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE;
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            SubMethodState += 1;
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE.");
                        }
                        break;
                    }

                case 2:
                    {
                        if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY.");

                            SendNextFwDataPacket();

                            SubMethodState = 4;
                        }
                        break;
                    }

                case 3:
                    {
                        if (IterationCounter > Iterations)
                        {
                            IterationCounter = 0;
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE DONE.");
                            Console.WriteLine("All packages sent.");
                            SubMethodState = 5;
                            break;
                        }
                        SendNextFwDataPacket();
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
                            SendNextFwDataPacket();
                            //SubMethodState -= 1;
                        }
                        else if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE &&
                        receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            IterationCounter = 0;
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE DONE.");
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
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => RECEIVED_CHECKSUM_OK.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD;
                            mTrackIOHandle.ActuatorCmd(mSendMessage);
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_CONFIG_WORD.");
                            SubMethodState += 1;
                        }
                        else if (receivedMessage.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                        receivedMessage.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM &&
                        receivedMessage.Taskstate == TaskStates.DONE &&
                        receivedMessage.Taskmessage == TaskMessages.RECEIVED_CHECKSUM_NOK)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => RECEIVED_CHECKSUM_NOK.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE;
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE try again.");
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
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY.");

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
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE.");
                            mSendMessage.Command = TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES;
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => EXEC_FW_STATE_FLASH_ALL_SLAVES.");
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
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => Flashing took " + Convert.ToString(elapsedtime / 1000) + " seconds.");
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => That is on average " + Convert.ToString((float)elapsedtime / FwFlashRequired / 1000) + " seconds per slave.");
                            mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => EXEC_FW_STATE_FLASH_ALL_SLAVES DONE.");
                            sw.Stop();
                            SubMethodState = 0;
                            returnval = Enums.Finished;
                        }

                        //else if (receivedMessage.TaskId != 0 &&
                        //receivedMessage.Taskcommand != 0 &&
                        //receivedMessage.Taskstate != 0)
                        //{
                        //    mTrackApplicationLogging.Log(GetType().Name, "State.FlashFwTrackamplifiers => Received data during flashing: TaskId = "
                        //        + Convert.ToString(receivedMessage.TaskId) + " Taskcommand = " + Convert.ToString(receivedMessage.Taskcommand)
                        //        + " Taskstate = " + Convert.ToString(receivedMessage.Taskstate) + " Taskmessage = " + Convert.ToString(receivedMessage.Taskmessage) + ".");
                        //}
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

        #region InitTrackamplifiers
        private uint InitTrackamplifiers(ReceivedMessage receivedMessage)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_INIT;
                        mTrackIOHandle.ActuatorCmd(mSendMessage);
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.InitTrackamplifiers => EXEC_MBUS_STATE_SLAVE_INIT.");
                        break;
                    }
                case 1:
                    {
                        if (receivedMessage.TaskId == TaskId.MBUS &&
                            receivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_INIT &&
                            receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.InitTrackamplifiers => EXEC_MBUS_STATE_SLAVE_INIT DONE.");
                            SubMethodState = 0;
                            returnval = Enums.Finished;
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

        #region EnableTrackamplifiers
        private uint EnableTrackamplifiers(ReceivedMessage receivedMessage)
        {
            uint returnval = Enums.Busy;

            switch (SubMethodState)
            {
                case 0:
                    {
                        mSendMessage.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_ENABLE;
                        mTrackIOHandle.ActuatorCmd(mSendMessage);
                        SubMethodState += 1;
                        mTrackApplicationLogging.Log(GetType().Name, "State.EnableTrackamplifiers => EXEC_MBUS_STATE_SLAVE_ENABLE.");
                        break;
                    }
                case 1:
                    {
                        if (receivedMessage.TaskId == TaskId.MBUS &&
                            receivedMessage.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_ENABLE &&
                            receivedMessage.Taskstate == TaskStates.DONE)
                        {
                            mTrackApplicationLogging.Log(GetType().Name, "State.EnableTrackamplifiers => MBUS_STATE_SLAVE_ENABLE DONE.");
                            SubMethodState = 0;
                            returnval = Enums.Finished;
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

        private void SendNextFwDataPacket()
        {
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

            //Console.WriteLine("Send Package " + (IterationCounter + 1).ToString() + " to Ethernet target.");

            IterationCounter += Enums.JUMPSIZE;
        }

        #endregion
    }

}