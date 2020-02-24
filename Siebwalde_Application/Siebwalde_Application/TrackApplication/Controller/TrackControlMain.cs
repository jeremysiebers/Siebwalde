using System;
using System.ComponentModel;

namespace Siebwalde_Application
{
    /// <summary>
    /// This is the main Trackcontroller class
    /// </summary>
    public class TrackControlMain
    {
        #region Variables

        private Main main;
        private TrackIOHandle trackIOHandle;
        private TrackApplicationVariables trackApplicationVariables;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="main"></param>
        /// <param name="trackIOHandle"></param>
        /// <param name="trackApplicationVariables"></param>
        public TrackControlMain(Main main, TrackIOHandle trackIOHandle, TrackApplicationVariables trackApplicationVariables)
        {
            // couple and hold local variables
            this.main = main;
            this.trackIOHandle = trackIOHandle;
            this.trackApplicationVariables = trackApplicationVariables;

            // subscribe to trackamplifier data changed events
            foreach (TrackAmplifierItem amplifier in trackApplicationVariables.trackAmpItems)//this.trackIOHandle.trackAmpItems)
            {
                amplifier.PropertyChanged += new PropertyChangedEventHandler(Amplifier_PropertyChanged);
            }

            // subscribe to commands set in the TrackControllerCommands class
            trackApplicationVariables.trackControllerCommands.PropertyChanged += new PropertyChangedEventHandler(TrackControllerCommands_PropertyChanged);
        }

        #endregion

        #region Poperty changed event handlers

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
        /// Property changes event handler on TrackControllerCommands
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackControllerCommands_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Console.WriteLine("Command received: " + e.PropertyName +" set to: " + sender.GetType().GetProperty(e.PropertyName).GetValue(sender).ToString() );
        }

        #endregion
    }
}
