using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Siebwalde_Application
{
    /// <summary>
    /// TrackDataItems
    /// </summary>
    public class TrackControllerCommands : INotifyPropertyChanged
    {
        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };
        
        #region TrackController Commands Methods

        private bool mStartInitializeTrackAmplifiers;
        /// <summary>
        /// Holds the start init track amplifier command state
        /// </summary>
        public bool StartInitializeTrackAmplifiers
        {
            get => mStartInitializeTrackAmplifiers;
            set
            {
                if (value == mStartInitializeTrackAmplifiers)
                {
                    return;
                }
                else
                {
                    mStartInitializeTrackAmplifiers = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(StartInitializeTrackAmplifiers)));
                }
            }
        }
        
        /// <summary>
        /// Holds the Ethernet Target connected state
        /// </summary>
        public bool EthernetTargetConnected { get; set; }

        #endregion

        #region Usermessage
        /// <summary>
        /// User message that can be shown on some user interface
        /// </summary>
        public string UserMessage { get; set; }

        #endregion

        #region Ethernet Target received message Method

        private ReceivedMessage EthernetTargetRecv;
        public ReceivedMessage ReceivedMessage
        {
            get { return EthernetTargetRecv; }
            set
            {
                if (value == EthernetTargetRecv)
                {
                    return;
                }
                else
                {
                    EthernetTargetRecv = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(ReceivedMessage)));
                }
            }
        }

        #endregion

        #region Ethernet Target Message to Send Method

        private SendMessage EthernetTargetSend;
        public SendMessage SendMessage
        {
            get { return EthernetTargetSend; }
            set
            {
                if (value == SendMessage)
                {
                    return;
                }
                else
                {
                    EthernetTargetSend = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(SendMessage)));
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize all variables as required
        /// </summary>
        public TrackControllerCommands()
        {
            //public ReceivedMessage EthernetTarget = new ReceivedMessage();
            EthernetTargetRecv = new ReceivedMessage(0, 0, 0, 0);

            byte[] DummyData = new byte[80];
            EthernetTargetSend = new SendMessage(0, DummyData);
        }

        #endregion

        #region OnPropertyChanged

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        //protected void OnPropertyChanged([CallerMemberName] string name = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        //}

        #endregion

    }
}
