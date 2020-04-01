using Prism.Commands;
using Siebwalde_Application.TrackApplication.View;
using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Input;

namespace Siebwalde_Application
{
    /// <summary>
    /// A view model for the main Track view
    /// </summary>
    public class HmiTrackControlViewModel : INotifyPropertyChanged
    {
        #region Local Variables

        /// <summary>
        /// This variable holds the link to the Data events
        /// </summary>
        private TrackApplicationVariables mTrackApplicationVariables;

        /// <summary>
        /// This is the HmiTrackForm that holds a container for the WPF via elementhost
        /// </summary>
        private HmiTrackControlForm hmiTrackForm;
        
        /// <summary>
        /// Holds the TrackController instance
        /// </summary>
        private TrackController mTrackController;

        /// <summary>
        /// Holds the TrackAmplifierItemViewModel instance
        /// </summary>
        private TrackAmplifierItemViewModel trackAmplifierItemViewModel;
        
        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };

        #endregion

        #region Properties

        /// <summary>
        /// The current page of the application
        /// </summary>
        public ApplicationPage CurrentPage { get; set; } = ApplicationPage.StartPage;
               
        /// <summary>
        /// Binding variable to couple to window closing interaction
        /// </summary>
        public DelegateCommand OnStartInitTrackAmpplifiers { get; private set; }

        /// <summary>
        /// Holds the status of the init button
        /// </summary>
        private bool _ButtonEnabled;
        public bool ButtonEnabled
        {
            get { return _ButtonEnabled; }
            set 
            {
                _ButtonEnabled = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ButtonEnabled)));
            }            
        }

        /// <summary>
        /// The command to show the Track amplifier window
        /// </summary>
        public ICommand ShowTrackAmplifierWindowCommand { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="trackController"></param>
        public HmiTrackControlViewModel(TrackApplicationVariables trackApplicationVariables)
        {
            mTrackApplicationVariables = trackApplicationVariables;

            // Binding element for the start init button
            OnStartInitTrackAmpplifiers = new DelegateCommand(InitTrackAmps, CanInitTrackAmps).ObservesProperty(() => ButtonEnabled);

            // check if init already was executed
            ButtonEnabled = !mTrackApplicationVariables.trackControllerCommands.StartInitializeTrackAmplifiers;

            this.ShowTrackAmplifierWindowCommand = new RelayCommand(ShowTrackAmplifierWindow);

            mTrackApplicationVariables.trackControllerCommands.PropertyChanged += new PropertyChangedEventHandler(TrackControllerCommands_PropertyChanged);

        }

        #endregion

        #region Property changed handler

        private void TrackControllerCommands_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "StartHmiTrackControlForm":
                    {
                        ShowHmiTrackControlWindow();
                        break;
                    }
                    
                default:
                    {                        
                        break;
                    }
            }
        }

        #endregion

        #region ShowHmiTrackControlWindow

        /// <summary>
        /// Show the HmiTrackControlWindow
        /// </summary>
        public void ShowHmiTrackControlWindow()
        {
            if (hmiTrackForm != null && hmiTrackForm.IsDisposed != true)
            {
                if (hmiTrackForm.Visible && hmiTrackForm.WindowState != FormWindowState.Minimized)
                {
                    //SiebwaldeAppLogging("Main: Hide Main Track interface");
                    hmiTrackForm.Hide();
                }
                else
                {
                    //SiebwaldeAppLogging("Main: Show Main Track interface");
                    //hmiTrackForm.Location = new Point(Location.X, Location.Y + 80);
                    //hmiTrackForm.Width = Width;
                    //hmiTrackForm.Height = Height - 80;
                    hmiTrackForm.Show();
                    hmiTrackForm.TopLevel = true;
                    hmiTrackForm.BringToFront();

                    if (hmiTrackForm.WindowState == FormWindowState.Minimized)
                    {
                        hmiTrackForm.WindowState = FormWindowState.Maximized;
                    }
                }
            }
            else
            {
                hmiTrackForm = new HmiTrackControlForm();
                hmiTrackForm.Show();
                //hmiTrackForm.Location = new Point(Location.X, Location.Y + 80);
                //hmiTrackForm.Width = Width;
                //hmiTrackForm.Height = Height - 80;
                hmiTrackForm.TopLevel = true;
                hmiTrackForm.BringToFront();
                hmiTrackForm.WindowState = FormWindowState.Maximized;
            }
        }

        #endregion



        private void ShowTrackAmplifierWindow()
        {
            UInt16[] HoldingRegInit = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            trackAmplifierItemViewModel = new TrackAmplifierItemViewModel(mTrackController, 0, "", HoldingRegInit, 0, 0, 0, 0, 0);
        }

        /// <summary>
        /// Set the status of the init button and set the variable in the data
        /// </summary>
        private void InitTrackAmps()
        {
            Console.WriteLine("Button pressed: StartInitializeTrackAmplifiers");
            ButtonEnabled = false;
            // set the value in the DATA
            mTrackController.mTrackApplicationVariables.trackControllerCommands.StartInitializeTrackAmplifiers = true;

        }

        private bool CanInitTrackAmps()
        {
            return ButtonEnabled;
        }
    }
}
