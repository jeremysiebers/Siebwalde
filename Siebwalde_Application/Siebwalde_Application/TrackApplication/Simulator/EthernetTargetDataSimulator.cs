using System;
using System.Timers;

namespace Siebwalde_Application
{
    /// <summary>
    /// This class simulates the EthernetTarget data events
    /// </summary>
    public class EthernetTargetDataSimulator
    {
        // Create a timer
        System.Timers.Timer aTimer = new System.Timers.Timer();

        // Create event for new data handling towards TrackIoHandle
        public Action<byte[]> NewData;

        /// <summary>
        /// Start the Ethernet target simulator to emulate data coming from the target
        /// </summary>
        internal void Start()
        {
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            // Set the Interval to [x] miliseconds.
            aTimer.Interval = 1000;
            aTimer.AutoReset = true;
            // Enable the timer
            aTimer.Enabled = true;
        }

        /// <summary>
        /// When the timer expires create a new data event to TrackIoHandle
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            aTimer.Stop();
            byte[] data = new byte[45];
            NewData(data);
            aTimer.Start();
        }
    }
}
