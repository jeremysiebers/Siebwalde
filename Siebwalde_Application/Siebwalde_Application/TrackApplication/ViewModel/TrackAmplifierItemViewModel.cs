using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Siebwalde_Application
{
    /// <summary>
    /// A view model for each TrackAmplifier item
    /// </summary>
    public class TrackAmplifierItemViewModel : BaseViewModel
    {
        private TrackIOHandle trackIOHandle;

        public TrackAmplifierItemViewModel(TrackIOHandle trackIOHandle)
        {
            this.trackIOHandle = trackIOHandle;

            foreach(TrackAmplifierItem amplifier in this.trackIOHandle.trackAmpItems)
            {
                amplifier.PropertyChanged += new PropertyChangedEventHandler(Amplifier_PropertyChanged);
            }
        }

        private void Amplifier_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            string slavenumber = sender.GetType().GetProperty("SlaveNumber").GetValue(sender).ToString();
                
            Debug.WriteLine("Slave " + slavenumber + " has " + e.PropertyName.ToString() + " changed to " + sender.GetType().GetProperty(e.PropertyName.ToString()).GetValue(sender).ToString());
        }
         
        
        /// <summary>
        /// The slave number
        /// </summary>
        //public ushort SlaveNumber { get; set; }

        /// <summary>
        /// The recieved mod bus messages counted by the master
        /// </summary>
        //public ushort MbReceiveCounter { get; set; }

    }
}
