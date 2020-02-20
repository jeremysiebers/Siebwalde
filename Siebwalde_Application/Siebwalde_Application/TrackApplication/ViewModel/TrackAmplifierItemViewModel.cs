using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace Siebwalde_Application
{
    /// <summary>
    /// A view model for each TrackAmplifier item
    /// </summary>
    public class TrackAmplifierItemViewModel : BaseViewModel
    {
        #region variables

        private TrackController MTcontroller;
        //private TrackIOHandle trackIOHandle;

        /// <summary>
        /// Binding variable to couple to window closing interaction
        /// </summary>
        public ICommand OnWindowClosingCommand { get; set; }

        /// <summary>
        /// A list of all children contained inside this item
        /// </summary>
        public ObservableCollection<TrackAmplifierItemViewModel> Amps { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Construct all related classes as shown in MVVM examples
        /// </summary>
        public TrackAmplifierItemViewModel(TrackController trackController,
            ushort slaveNumber,
            string slaveDetected,
            ushort[] holdingReg,
            ushort mbReceiveCounter,
            ushort mbSentCounter,
            UInt32 mbCommError,
            ushort mbExceptionCode,
            ushort spiCommErrorCounter)
        {
            
            if(trackController != null)//mMain != null)
            {
                MTcontroller = trackController;

                // create new relaycommand called WindowClosingCommand and run OnWindowClosing() when called from XAML
                this.OnWindowClosingCommand = new RelayCommand(OnWindowClosing);

                // Subscribe every trackAmpItem to the propertyChanged method of amplifier
                foreach (TrackAmplifierItem amplifier in MTcontroller.trackApplicationVariables.trackAmpItems)//this.trackIOHandle.trackAmpItems)
                {
                    amplifier.PropertyChanged += new PropertyChangedEventHandler(Amplifier_PropertyChanged);
                }

                // call funciton to create the observable collection
                FillTheCollection();

            }
            if(slaveNumber != 0)
            {
                SlaveNumber = slaveNumber;
                SlaveDetected = slaveDetected;
                HoldingReg = holdingReg;
                MbReceiveCounter = mbReceiveCounter;
                MbSentCounter = mbSentCounter;
                MbCommError = mbCommError;
                MbExceptionCode = mbExceptionCode;
                SpiCommErrorCounter = spiCommErrorCounter;
            }            
        }

        #endregion

        #region Couple and Subscribe to Model property changed events
                
        /// <summary>
        /// Property changed event handler of slaves proprties in the data model pass to viewmodel ObservableCollection of TrackAmplifierItemViewModel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Amplifier_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //string slavenumber = sender.GetType().GetProperty("SlaveNumber").GetValue(sender).ToString();
            //Debug.WriteLine("Slave " + slavenumber + " has " + e.PropertyName.ToString() + " changed to " + sender.GetType().GetProperty(e.PropertyName.ToString()).GetValue(sender).ToString());

            string slavenumber = sender.GetType().GetProperty("SlaveNumber").GetValue(sender).ToString();

            var EventFrom = this.Amps[(Convert.ToInt32(sender.GetType().GetProperty("SlaveNumber").GetValue(sender)) - 1)];

            switch (e.PropertyName.ToString())
            {
                case "SlaveDetected": { EventFrom.SlaveDetected = Convert.ToBoolean(sender.GetType().GetProperty("SlaveDetected").GetValue(sender)) ? ("Yes") : ("No"); break;}
                case "HoldingReg":
                    {
                        var array = sender.GetType().GetProperty("HoldingReg").GetValue(sender);
                        ushort[] newarray = ((System.Collections.IEnumerable)array).Cast<object>()
                        .Select(x => Convert.ToUInt16(x))
                        .ToArray();
                        EventFrom.HoldingReg = newarray;
                        break;
                    }
                case "MbReceiveCounter": { EventFrom.MbReceiveCounter = Convert.ToUInt16(sender.GetType().GetProperty("MbReceiveCounter").GetValue(sender)); break; }
                case "MbSentCounter": { EventFrom.MbSentCounter = Convert.ToUInt16(sender.GetType().GetProperty("MbSentCounter").GetValue(sender)); break; }
                case "MbCommError": { EventFrom.MbCommError = Convert.ToUInt16(sender.GetType().GetProperty("MbCommError").GetValue(sender)); break; }
                case "SpiCommErrorCounter": { EventFrom.SpiCommErrorCounter = Convert.ToUInt16(sender.GetType().GetProperty("SpiCommErrorCounter").GetValue(sender)); break; }
                default: { break; }
            }            
        }

        #endregion

        #region ObservableCollection

        /// <summary>
        /// Create and fill the collection of trackamplifiers
        /// </summary>
        private void FillTheCollection()
        {
            var amps = MTcontroller.trackApplicationVariables.GetAmplifierListing(); // this.trackIOHandle.GetAmplifierListing();
            this.Amps = new ObservableCollection<TrackAmplifierItemViewModel>(amps.Select(content => new TrackAmplifierItemViewModel
            (null,
            content.SlaveNumber,
            Convert.ToBoolean(content.SlaveDetected) ? ("Yes") : ("No"),
            content.HoldingReg,
            content.MbReceiveCounter,
            content.MbSentCounter,
            content.MbCommError,
            content.MbExceptionCode,
            content.SpiCommErrorCounter)));

            this.Amps.Remove(Amps[0]); //update is not working probably negative offset is required
        }

        #endregion

        #region this Viewmodel properties on trackamplifier


        /// <summary>
        /// The slave number
        /// </summary>
        public ushort SlaveNumber { get; set; }

        /// <summary>
        /// If a slave is detected by the master
        /// </summary>
        public string SlaveDetected { get; set; }

        /// <summary>
        /// If a slave is detected by the master
        /// </summary>
        public ushort[] HoldingReg { get; set; }

        /// <summary>
        /// The recieved mod bus messages counted by the master
        /// </summary>
        public ushort MbReceiveCounter { get; set; }

        /// <summary>
        /// The recieved mod bus messages counted by the master
        /// </summary>
        public ushort MbSentCounter { get; set; }

        /// <summary>
        /// The recieved mod bus messages counted by the master
        /// </summary>
        public UInt32 MbCommError { get; set; }

        /// <summary>
        /// The recieved mod bus messages counted by the master
        /// </summary>
        public ushort MbExceptionCode { get; set; }

        /// <summary>
        /// The recieved mod bus messages counted by the master
        /// </summary>
        public ushort SpiCommErrorCounter { get; set; }

        #endregion

        #region Closing event handler
        /// <summary>
        /// On window close event unregister property changed events and clear the ObservableCollection
        /// </summary>
        public void OnWindowClosing()
        {
            foreach (TrackAmplifierItem amplifier in MTcontroller.trackApplicationVariables.trackAmpItems)//this.trackIOHandle.trackAmpItems)
            {
                amplifier.PropertyChanged -= new PropertyChangedEventHandler(Amplifier_PropertyChanged);
            }
            this.Amps.Clear();
        }

        #endregion

    }
}
