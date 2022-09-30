using System;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.ComponentModel;
using System.Timers;

namespace SiebwaldeApp
{
    /// <summary>
    /// This is the main Trackcontroller Application class
    /// </summary>
    public class TrackControlMain
    {
        #region Variables

        private TrackIOHandle mTrackIOHandle;
        private TrackApplicationVariables mTrackApplicationVariables;
        private TrackAmplifierInitalizationSequencer mTrackAmplifierInitalizationSequencer;
        private System.Timers.Timer AppUpdateTimer = new System.Timers.Timer();
        private readonly object ExecuteLock = new object();

        private ReceivedMessage dummymessage;

        /// <summary>
        /// This is the HmiTrackForm that holds a container for the WPF via elementhost
        /// </summary>
        //private static HmiTrackControlForm hmiTrackForm;
        //private static HmiTrackControl mHmiTrackControl;

        /// <summary>
        /// This enum holds all the possible states of the TrackControlMain statemachine
        /// </summary>
        private enum State { Idle, Reset, Cmd, InitializeTrackAmplifiers };
        private State State_Machine;

        // Logger instance
        private string mLoggerInstance { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="LoggerInstance"></param>
        /// <param name="trackIOHandle"></param>
        /// <param name="trackApplicationVariables"></param>
        public TrackControlMain(string LoggerInstance, TrackIOHandle trackIOHandle, TrackApplicationVariables trackApplicationVariables)
        {
            // couple and hold local variables
            mTrackIOHandle = trackIOHandle;
            mTrackApplicationVariables = trackApplicationVariables;
            mLoggerInstance = LoggerInstance;

            // instantiate sub classes            
            mTrackAmplifierInitalizationSequencer = new TrackAmplifierInitalizationSequencer(mLoggerInstance, mTrackApplicationVariables, mTrackIOHandle);
            
            // subscribe to trackamplifier data changed events
            foreach (TrackAmplifierItem amplifier in trackApplicationVariables.trackAmpItems)//this.trackIOHandle.trackAmpItems)
            {
                amplifier.PropertyChanged += new PropertyChangedEventHandler(Amplifier_PropertyChanged);
            }

            // subscribe to commands set in the TrackControllerCommands class
            mTrackApplicationVariables.trackControllerCommands.PropertyChanged += new PropertyChangedEventHandler(TrackControllerCommands_PropertyChanged);

            dummymessage = new ReceivedMessage(0, 0, 0, 0);                        
        }

        #endregion

        #region Poperty changed / timer event handlers

        /// <summary>
        /// Property changes event handler on amplifier items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Amplifier_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //Console.WriteLine("Main Track App updated");
            //Console.WriteLine("Amplifier updated: " + e.PropertyName + " set to: " + sender.GetType().GetProperty(e.PropertyName).GetValue(sender).ToString());
        }

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
                case "UserMessage":
                    {
                        break;
                    }

                case "SendMessage":
                    {
                        //mTrackIOHandle.ActuatorCmd(mTrackApplicationVariables.trackControllerCommands.SendMessage);
                        break;
                    }

                case "ReceivedMessage":
                    {
                        break;
                    }

                case "StartHmiTrackControlForm":
                    {
                        // Start Track Control view container housing WPF application
                        //ShowHmiTrackControlWindow();                        
                        break;
                    }

                default:
                    {
                        Console.WriteLine("Command received: " + e.PropertyName + " set to: " + sender.GetType().GetProperty(e.PropertyName).GetValue(sender).ToString());
                        TrackApplicationUpdate(e.PropertyName, Convert.ToInt32(sender.GetType().GetProperty(e.PropertyName).GetValue(sender)));
                        break;
                    }
            }
        }

        /// <summary>
        /// Timer event to kick TrackApplication
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            TrackApplicationUpdate("TimerEvent", 0);
        }

        #endregion

        #region Start method of the Track application

        /// <summary>
        /// Start the Track Main Application
        /// </summary>
        internal void Start(bool TrackRealMode)
        {
            AppUpdateTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            AppUpdateTimer.Interval = 50;
            AppUpdateTimer.AutoReset = true;
            // Enable the timer
            AppUpdateTimer.Enabled = true;
            IoC.Logger.Log("Track Application started.", mLoggerInstance);   
            
            // When in real mode start initializing the amplifiers
            if (TrackRealMode)
            {
                TrackApplicationUpdate("StartInitializeTrackAmplifiers", 0);
            }
        }

        #endregion

        #region Track application updater

        private void TrackApplicationUpdate(string source, Int32 value)
        {
            // Lock the execution since multiple events may arrive
            lock (ExecuteLock)
            {
                // stop the timer to prevent re-starting during execution of code
                AppUpdateTimer.Stop();

                // If StartInitializeTrackAmplifiers is set to true
                if (source == "StartInitializeTrackAmplifiers")
                {
                    //mTrackApplicationVariables.trackControllerCommands.StartInitializeTrackAmplifiers = false;
                    IoC.Logger.Log("Start Initialize Track Amplifiers.", mLoggerInstance);
                    State_Machine = State.InitializeTrackAmplifiers;
                    IoC.Logger.Log("State_Machine = State.StartInitializeTrackAmplifiers.", mLoggerInstance);
                    mTrackApplicationVariables.trackControllerCommands.UserMessage = "Start initialize Track Amplifiers.";
                }
                else if(source == "TimerEvent")
                {
                    StateMachineUpdate(source, value);
                }
                
                // Start the timer again
                AppUpdateTimer.Start();
            }                        
        }

        #endregion

        #region Track Application State Machine

        /// <summary>
        /// Main Track application state machine, calls all the subclass functions
        /// </summary>
        /// <param name="source"></param>
        /// <param name="value"></param>
        private void StateMachineUpdate(string source, Int32 value)
        {
            switch (State_Machine)
            {
                case State.Reset:
                    // Here all sub classes reset methods are called in case of a forced reset
                    break;

                case State.Idle:
                    // Here all manual commands are handled from the user
                    break;

                case State.InitializeTrackAmplifiers:
                    {
                        switch (mTrackAmplifierInitalizationSequencer.CheckInitSequence)
                        {
                            case Enums.Busy:
                                {
                                    break;
                                }
                            case Enums.Standby:
                                {
                                    IoC.Logger.Log("TrackAmplifierInitalizationSequencer.Start().", mLoggerInstance);
                                    mTrackAmplifierInitalizationSequencer.Start();
                                    break;
                                }
                            case Enums.Finished:
                                {
                                    IoC.Logger.Log("State.StartInitializeTrackAmplifiers == Finished.", mLoggerInstance);
                                    State_Machine = State.Idle;
                                    break;
                                }
                            case Enums.Error:
                                {
                                    State_Machine = State.Idle;
                                    break;
                                }
                            default:
                                {
                                    State_Machine = State.Idle;
                                    break;
                                }
                        }
                        break;
                    }
                    
                default:
                    break;
            }

        }
        #endregion
    }

}
