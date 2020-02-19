using Prism.Commands;
using System;

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

            OnStartInitTrackAmpplifiers = new DelegateCommand(InitTrackAmps, CanInitTrackAmps).ObservesProperty(() => ButtonEnabled);

            ButtonEnabled = true;

        }

        private void InitTrackAmps()
        {
            Console.WriteLine("button clicked");
            ButtonEnabled = false;
        }

        private bool CanInitTrackAmps()
        {
            return ButtonEnabled;
        }
    }
}
