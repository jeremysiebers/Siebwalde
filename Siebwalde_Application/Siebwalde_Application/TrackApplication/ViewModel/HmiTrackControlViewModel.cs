using Prism.Commands;
using Siebwalde_Application.TrackApplication.View;
using System;
using System.Windows;

namespace Siebwalde_Application
{
    /// <summary>
    /// A view model for the main Track view
    /// </summary>
    public class HmiTrackControlViewModel : BaseViewModel
    {
        /// <summary>
        /// Hold the TrackController instance
        /// </summary>
        private TrackController mTrackController;
                
        /// <summary>
        /// Binding variable to couple to window closing interaction
        /// </summary>
        public DelegateCommand OnStartInitTrackAmpplifiers { get; private set; }

        private bool _ButtonEnabled;
        public bool ButtonEnabled
        {
            get { return _ButtonEnabled; }
            set { _ButtonEnabled = value;  }            
        }

        public HmiTrackControlViewModel(TrackController trackController)
        {
            mTrackController = trackController;

            // Binding element for the start init button
            OnStartInitTrackAmpplifiers = new DelegateCommand(InitTrackAmps, CanInitTrackAmps).ObservesProperty(() => ButtonEnabled);

            // check if init already was executed
            ButtonEnabled = !mTrackController.mTrackApplicationVariables.trackControllerCommands.StartInitializeTrackAmplifiers;

        }

        private void InitTrackAmps()
        {
            Console.WriteLine("Button pressed: StartInitializeTrackAmplifiers");
            ButtonEnabled = false;
            mTrackController.mTrackApplicationVariables.trackControllerCommands.StartInitializeTrackAmplifiers = true;
            //Window w = new Window();
            //w.Content = new TrackAmplifierItemView(mTrackController);
            //w.Show();
        }

        private bool CanInitTrackAmps()
        {
            return ButtonEnabled;
        }
    }
}
