using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;

namespace SiebwaldeApp
{
    /// <summary>
    /// This TrackAmplifierInitalizationSequencer initializes all the Track amplifiers and checks if firmware download is required
    /// </summary>
    public class TrackAmplifierInitalizationSequencer
    {
        #region Local variables

        private TrackApplicationVariables mTrackApplicationVariables;
        private TrackAmplifierBootloaderHelpers mTrackAmplifierBootloaderHelpers;
        private SendNextFwDataPacket mSendNextFwDataPacket;
        private TrackIOHandle mTrackIOHandle;
        private SendMessage mSendMessage;
        private ReceivedMessage dummyReceivedMessage = new ReceivedMessage();
        private Stopwatch sw = new Stopwatch();
        private object ExecuteLock = new object();
        private System.Timers.Timer AppUpdateTimer = new System.Timers.Timer();
        //private ConnectToEthernetTarget mConnectToEthernetTarget;
        //private ResetAllSlaves mResetAllSlaves;
        //private DataUpload mDataUpload;
        //private DetectSlaves mDetectSlaves;        
        //private FlashFwTrackamplifiers mFlashFwTrackamplifiers;
        //private InitTrackamplifiers mInitTrackamplifiers;
        //private EnableTrackamplifiers mEnableTrackamplifiers;
        //private RecoverSlaves mRecoverSlaves;

        // Logger instance
        private string mLoggerInstance { get; set; }

        /// <summary>
        /// This enum holds all the possible states of the TrackAmplifierInitalizationSequencer statemachine
        /// </summary>
        private enum State { Idle, Reset, ConnectToEthernetTarget, ResetAllSlaves, DataUploadStart, DetectSlaves,
            DetectSlaveRecovery, FlashFwTrackamplifiers, InitTrackamplifiers, EnableTrackamplifiers
        };
        // State machine
        private State StateMachine;

        // Holds the status of the whole sequence to return to the caller
        public uint CheckInitSequence { get; internal set; }

        // Create a list that will hold all classes to initialize the amplifiers
        protected List<IAmplifierInitializersBaseClass> mAmplifierInitializers = new List<IAmplifierInitializersBaseClass>();

        // Create a reference for the mAmplifierInitializers list
        private int InitializerState {get; set; }

        #endregion

        #region Constructor
        /// <summary>
        /// Setup the Track amplifier sequencer that will automatic detect, recover, flash, init and enable the trackamplifiers
        /// </summary>
        /// <param name="LoggerInstance"></param>
        /// <param name="trackApplicationVariables"></param>
        /// <param name="trackIOHandle"></param>
        public TrackAmplifierInitalizationSequencer(string LoggerInstance, TrackApplicationVariables trackApplicationVariables, TrackIOHandle trackIOHandle)
        {
            mLoggerInstance = LoggerInstance;
            mTrackApplicationVariables = trackApplicationVariables;
            mTrackIOHandle = trackIOHandle;

            // Set the statemachine to idle
            StateMachine = State.Idle;

            // Set the return value to standby
            CheckInitSequence = Enums.Standby;
            // init the helper function
            mTrackAmplifierBootloaderHelpers = new TrackAmplifierBootloaderHelpers("C:\\Users\\jerem\\Siebwalde\\TrackAmplifier4.X\\dist\\Offset\\production\\TrackAmplifier4.X.production.hex", LoggerInstance);//(Enums.HOMEPATH + Enums.SLAVEHEXFILE, trackApplicationLogging);
            // init send FW helper function
            mSendNextFwDataPacket = new SendNextFwDataPacket(mTrackIOHandle, mTrackAmplifierBootloaderHelpers);

            // Init all the sub PC-Master-Slave routines
            //mConnectToEthernetTarget = new ConnectToEthernetTarget(mLoggerInstance, mTrackIOHandle);
            //mResetAllSlaves = new ResetAllSlaves(mLoggerInstance, mTrackIOHandle);
            //mDataUpload = new DataUpload(mLoggerInstance, mTrackIOHandle);
            //mDetectSlaves = new DetectSlaves(mLoggerInstance, mTrackIOHandle, mTrackApplicationVariables, mSendNextFwDataPacket, 
            //    mTrackAmplifierBootloaderHelpers );
            //mFlashFwTrackamplifiers = new FlashFwTrackamplifiers(mLoggerInstance, mTrackIOHandle, mTrackApplicationVariables, mSendNextFwDataPacket,
            //    mTrackAmplifierBootloaderHelpers);
            //mInitTrackamplifiers = new InitTrackamplifiers(mLoggerInstance, mTrackIOHandle);
            //mEnableTrackamplifiers = new EnableTrackamplifiers(mLoggerInstance, mTrackIOHandle);
            //mRecoverSlaves = new RecoverSlaves(mLoggerInstance, mTrackIOHandle, mTrackApplicationVariables, mSendNextFwDataPacket,
            //    mTrackAmplifierBootloaderHelpers);

            // Fill the mAmplifierInitializers list with all the classes that must be executed
            mAmplifierInitializers.AddRange(new List<IAmplifierInitializersBaseClass>  
            {
                new ConnectToEthernetTarget(mLoggerInstance, mTrackIOHandle),
                new ResetAllSlaves(mLoggerInstance, mTrackIOHandle),
                new DataUpload(mLoggerInstance, mTrackIOHandle),
                new DetectSlaves(mLoggerInstance, mTrackIOHandle, mTrackApplicationVariables, mSendNextFwDataPacket,
                mTrackAmplifierBootloaderHelpers),
                new RecoverSlaves(mLoggerInstance, mTrackIOHandle, mTrackApplicationVariables, mSendNextFwDataPacket,
                mTrackAmplifierBootloaderHelpers),
                new FlashFwTrackamplifiers(mLoggerInstance, mTrackIOHandle, mTrackApplicationVariables, mSendNextFwDataPacket,
                mTrackAmplifierBootloaderHelpers),
                new InitTrackamplifiers(mLoggerInstance, mTrackIOHandle),
                new EnableTrackamplifiers(mLoggerInstance, mTrackIOHandle),
            });
            // Init the reference to mAmplifierInitializers
            InitializerState = 0;

            // subscribe to commands set in the TrackControllerCommands class
            if (mTrackApplicationVariables != null)
            {
                mTrackApplicationVariables.trackControllerCommands.PropertyChanged += new PropertyChangedEventHandler(TrackControllerCommands_PropertyChanged);
            }            
        }

        #endregion

        #region Start

        /// <summary>
        /// Start of generation of dependencies within this program
        /// </summary>
        public void Start()
        {
            // Load the HEX-file and readout all the data required
            IoC.Logger.Log("TrackAmplifierBootloaderHelpers.Execute()", mLoggerInstance);
            mTrackAmplifierBootloaderHelpers.Execute();

            // set the state machine to the first case
            StateMachine = State.ConnectToEthernetTarget;

            // set the status of this program to busy
            CheckInitSequence = Enums.Busy;

            AppUpdateTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            AppUpdateTimer.Interval = 100;
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

                // Set the return value to busy
                uint returnval = Enums.Busy;

                // Log the received message to console
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
                    IoC.Logger.Log("TrackAmplifierBootloaderHelpers.HexFileReadSuccessful == false stopping init request.", mLoggerInstance);
                    CheckInitSequence = Enums.Error;
                    return;
                }


                if (mAmplifierInitializers.Count > 0)
                {
                    var initializer = mAmplifierInitializers[InitializerState].Execute(receivedMessage);

                    switch (initializer.Item1)
                    {
                        case Enums.Busy:
                            {
                                break;
                            }
                        case Enums.Finished:
                            {
                                IoC.Logger.Log(mAmplifierInitializers[InitializerState].Name + " == Finished.", mLoggerInstance);
                                mAmplifierInitializers.RemoveAt(InitializerState);
                                if (initializer.Item2 != "")
                                {
                                    if (initializer.Item2.Contains("Remove:RecoverSlaves"))
                                    {
                                        var count = 0;
                                        foreach (var item in mAmplifierInitializers)
                                        {
                                            if (item.Name == "RecoverSlaves")
                                            {
                                                mAmplifierInitializers.RemoveAt(count);
                                                break;
                                            }
                                            count++;
                                        }
                                    }
                                    else if (initializer.Item2.Contains("DONE"))
                                    {
                                        returnval = Enums.Finished;
                                    }
                                }
                                break;
                            }
                        case Enums.Next:
                            {
                                IoC.Logger.Log(mAmplifierInitializers[InitializerState].Name + " == Call next: " + initializer.Item2, mLoggerInstance);
                                var count = 0;
                                foreach (var item in mAmplifierInitializers)
                                {
                                    if (item.Name == initializer.Item2)
                                    {
                                        InitializerState = count;
                                        break;
                                    }
                                    count++;
                                }
                                break;
                            }
                        case Enums.Error:
                            {
                                IoC.Logger.Log(mAmplifierInitializers[InitializerState].Name + " == Error.", mLoggerInstance);
                                mAmplifierInitializers.Clear();
                                returnval = Enums.Error;
                                break;
                            }
                        default:
                            {
                                StateMachine = State.Idle;
                                break;
                            }
                    }
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
    }
}