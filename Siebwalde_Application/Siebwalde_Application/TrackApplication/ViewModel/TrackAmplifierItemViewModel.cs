using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Siebwalde_Application
{
    /// <summary>
    /// A view model for each TrackAmplifier item
    /// </summary>
    public class TrackAmplifierItemViewModel : BaseViewModel
    {
        #region variables

        private TrackController MTcontroller;
        private TrackIOHandle trackIOHandle;
        private Main mMain;

        /// <summary>
        /// A list of all children contained inside this item
        /// </summary>
        public ObservableCollection<TrackAmplifierItemViewModel> Amps { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Construct all related classes as shown in MVVM examples
        /// </summary>
        public TrackAmplifierItemViewModel(Main main,
            ushort slaveNumber,
            ushort slaveDetected,
            ushort[] holdingReg,
            ushort mbReceiveCounter,
            ushort mbSentCounter,
            UInt32 mbCommError,
            ushort mbExceptionCode,
            ushort spiCommErrorCounter)
        {
            mMain = main;
            if(mMain != null)
            {
                int TrackControllerSendingport = 10000;
                int TrackControllerReceivingport = 10001;
                MTcontroller = new TrackController(this, TrackControllerReceivingport, TrackControllerSendingport, mMain);
                MTcontroller.Start();
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
        /// Hookup the TrackIoHandle which is intantiated via the TrackController (at this time)
        /// </summary>
        /// <param name="trackIOHandle"></param>
        public void TrackAmplifierItemViewModelPassTrackIoHandle(TrackIOHandle trackIOHandle)
        {
            this.trackIOHandle = trackIOHandle;

            foreach (TrackAmplifierItem amplifier in this.trackIOHandle.trackAmpItems)
            {
                amplifier.PropertyChanged += new PropertyChangedEventHandler(Amplifier_PropertyChanged);
            }

            FillTheCollection();
        }

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
                case "SlaveDetected": { EventFrom.SlaveDetected = Convert.ToUInt16(sender.GetType().GetProperty("SlaveDetected").GetValue(sender)); break;}
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
            var amps = this.trackIOHandle.GetAmplifierListing();
            this.Amps = new ObservableCollection<TrackAmplifierItemViewModel>(amps.Select(content => new TrackAmplifierItemViewModel
            (null,
            content.SlaveNumber,
            content.SlaveDetected,
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
        public ushort SlaveDetected { get; set; }

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

    }
}
