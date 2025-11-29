using System;
using System.ComponentModel;
using System.Timers;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// This is the main Trackcontroller Application class
    /// </summary>
    public class TrackControlMain
    {
        #region Variables

        // NEW: use ITrackCommClient instead of TrackIOHandle and remove old sequencer
        private readonly ITrackCommClient _trackCommClient;
        private readonly TrackApplicationVariables mTrackApplicationVariables;

        // Geen eigen init-sequencer meer
        // private TrackAmplifierInitalizationSequencer mTrackAmplifierInitalizationSequencer;

        // De state-machine hoeft de init van de amplifiers niet meer zelf te regelen.
        // Die taak ligt nu bij TrackAmplifierInitializationServiceAsync in SiebwaldeApplicationModel.
        private enum State { Idle, Reset, Cmd };
        private State State_Machine;
        private ReceivedMessage dummymessage;
        private System.Timers.Timer AppUpdateTimer = new System.Timers.Timer();
        private readonly object ExecuteLock = new object();

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
        public TrackControlMain(
            string loggerInstance,
            ITrackCommClient trackCommClient,
            TrackApplicationVariables trackApplicationVariables)
        {
            _trackCommClient = trackCommClient ?? throw new ArgumentNullException(nameof(trackCommClient));
            mTrackApplicationVariables = trackApplicationVariables ?? throw new ArgumentNullException(nameof(trackApplicationVariables));
            mLoggerInstance = loggerInstance ?? "Track";

            // We gebruiken niet langer een lokale TrackAmplifierInitalizationSequencer;
            // die verantwoordelijkheid ligt nu bij TrackAmplifierInitializationServiceAsync
            // in SiebwaldeApplicationModel.
            // mTrackAmplifierInitalizationSequencer = new TrackAmplifierInitalizationSequencer(...);

            // subscribe to trackamplifier data changed events
            foreach (TrackAmplifierItem amplifier in trackApplicationVariables.trackAmpItems)
            {
                amplifier.PropertyChanged += Amplifier_PropertyChanged;
            }

            // subscribe to commands set in the TrackControllerCommands class
            mTrackApplicationVariables.trackControllerCommands.PropertyChanged += TrackControllerCommands_PropertyChanged;

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
                        var toSend = mTrackApplicationVariables.trackControllerCommands.SendMessage;
                        if (toSend != null)
                        {
                            // Fire-and-forget; TrackCommClientAsync handelt framing en IO af.
                            _ = _trackCommClient.SendAsync(toSend, CancellationToken.None);
                        }
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
        internal void Start(bool trackRealMode)
        {
            AppUpdateTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            AppUpdateTimer.Interval = 50;
            AppUpdateTimer.AutoReset = true;
            AppUpdateTimer.Enabled = true;

            State_Machine = State.Idle;

            IoC.Logger.Log("Track Application (TrackControlMain) started.", mLoggerInstance);

            // De init van de track amplifiers wordt nu extern gedaan
            // via TrackAmplifierInitializationServiceAsync in SiebwaldeApplicationModel.
            // Hier dus geen StartInitializeTrackAmplifiers meer.
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

                if (source == "TimerEvent")
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
                    
                default:
                    break;
            }

        }
        #endregion
    }

}
