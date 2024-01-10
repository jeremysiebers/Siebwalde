using PropertyChanged;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace SiebwaldeApp.Core
{
    public class TrackApplicationVariables
    {
        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };

        #region Variables

        private bool _HallBlock13;
        private bool _HallBlock21A;
        private bool _HallBlockT4;
        private bool _HallBlockT5;
        private bool _HallBlockT1;
        private bool _HallBlockT2;
        private bool _HallBlock9B;
        private bool _HallBlock4A;

        private bool _HallBlockT7;
        private bool _HallBlockT8;
        private bool _OccFromBlock13;
        private bool _OccFromBlock4;
        private bool _OccFromStn1;
        private bool _OccFromStn2;
        private bool _OccFromStn3;
        private bool _OccFromStn10;

        private bool _OccFromStn11;
        private bool _OccFromStn12;
        private bool _OccFromT6;
        private bool _OccFromT3;
        private bool _CtrlOff;
        private bool _OccFromBlock23B;
        private bool _OccFromBlock22B;
        private bool _OccFromBlock9B;

        private bool _OccFromBlock21B;
        private bool _VoltageDetected;

        #endregion

        #region Public Methods

        /// <summary>
        /// Get/Set and generate event for HallBlock13
        /// </summary>
        [DoNotNotify]
        public bool HallBlock13
        {
            get => _HallBlock13;
            set
            {
                if (value == _HallBlock13)
                {
                    return;
                }
                else
                {
                    _HallBlock13 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HallBlock13)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for HallBlock21A
        /// </summary>
        [DoNotNotify]
        public bool HallBlock21A
        {
            get => _HallBlock21A;
            set
            {
                if (value == _HallBlock21A)
                {
                    return;
                }
                else
                {
                    _HallBlock21A = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HallBlock21A)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for HallBlockT4
        /// </summary>
        [DoNotNotify]
        public bool HallBlockT4
        {
            get => _HallBlockT4;
            set
            {
                if (value == _HallBlockT4)
                {
                    return;
                }
                else
                {
                    _HallBlockT4 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HallBlockT4)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for HallBlockT5
        /// </summary>
        [DoNotNotify]
        public bool HallBlockT5
        {
            get => _HallBlockT5;
            set
            {
                if (value == _HallBlockT5)
                {
                    return;
                }
                else
                {
                    _HallBlockT5 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HallBlockT5)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for HallBlockT1
        /// </summary>
        [DoNotNotify]
        public bool HallBlockT1
        {
            get => _HallBlockT1;
            set
            {
                if (value == _HallBlockT1)
                {
                    return;
                }
                else
                {
                    _HallBlockT1 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HallBlockT1)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for HallBlockT2
        /// </summary>
        [DoNotNotify]
        public bool HallBlockT2
        {
            get => _HallBlockT2;
            set
            {
                if (value == _HallBlockT2)
                {
                    return;
                }
                else
                {
                    _HallBlockT2 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HallBlockT2)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for HallBlock9B
        /// </summary>
        [DoNotNotify]
        public bool HallBlock9B
        {
            get => _HallBlock9B;
            set
            {
                if (value == _HallBlock9B)
                {
                    return;
                }
                else
                {
                    _HallBlock9B = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HallBlock9B)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for HallBlock4A
        /// </summary>
        [DoNotNotify]
        public bool HallBlock4A
        {
            get => _HallBlock4A;
            set
            {
                if (value == _HallBlock4A)
                {
                    return;
                }
                else
                {
                    _HallBlock4A = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HallBlock4A)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for HallBlockT7
        /// </summary>
        [DoNotNotify]
        public bool HallBlockT7
        {
            get => _HallBlockT7;
            set
            {
                if (value == _HallBlockT7)
                {
                    return;
                }
                else
                {
                    _HallBlockT7 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HallBlockT7)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for HallBlockT8
        /// </summary>
        [DoNotNotify]
        public bool HallBlockT8
        {
            get => _HallBlockT8;
            set
            {
                if (value == _HallBlockT8)
                {
                    return;
                }
                else
                {
                    _HallBlockT8 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HallBlockT8)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromBlock13
        /// </summary>
        [DoNotNotify]
        public bool OccFromBlock13
        {
            get => _OccFromBlock13;
            set
            {
                if (value == _OccFromBlock13)
                {
                    return;
                }
                else
                {
                    _OccFromBlock13 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromBlock13)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromBlock4
        /// </summary>
        [DoNotNotify]
        public bool OccFromBlock4
        {
            get => _OccFromBlock4;
            set
            {
                if (value == _OccFromBlock4)
                {
                    return;
                }
                else
                {
                    _OccFromBlock4 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromBlock4)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromStn1
        /// </summary>
        [DoNotNotify]
        public bool OccFromStn1
        {
            get => _OccFromStn1;
            set
            {
                if (value == _OccFromStn1)
                {
                    return;
                }
                else
                {
                    _OccFromStn1 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromStn1)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromStn2
        /// </summary>
        [DoNotNotify]
        public bool OccFromStn2
        {
            get => _OccFromStn2;
            set
            {
                if (value == _OccFromStn2)
                {
                    return;
                }
                else
                {
                    _OccFromStn2 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromStn2)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromStn1
        /// </summary>
        [DoNotNotify]
        public bool OccFromStn3
        {
            get => _OccFromStn3;
            set
            {
                if (value == _OccFromStn3)
                {
                    return;
                }
                else
                {
                    _OccFromStn3 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromStn3)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromStn1
        /// </summary>
        [DoNotNotify]
        public bool OccFromStn10
        {
            get => _OccFromStn10;
            set
            {
                if (value == _OccFromStn10)
                {
                    return;
                }
                else
                {
                    _OccFromStn10 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromStn10)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromStn11
        /// </summary>
        [DoNotNotify]
        public bool OccFromStn11
        {
            get => _OccFromStn11;
            set
            {
                if (value == _OccFromStn11)
                {
                    return;
                }
                else
                {
                    _OccFromStn11 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromStn11)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromStn12
        /// </summary>
        [DoNotNotify]
        public bool OccFromStn12
        {
            get => _OccFromStn12;
            set
            {
                if (value == _OccFromStn12)
                {
                    return;
                }
                else
                {
                    _OccFromStn12 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromStn12)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromT6
        /// </summary>
        [DoNotNotify]
        public bool OccFromT6
        {
            get => _OccFromT6;
            set
            {
                if (value == _OccFromT6)
                {
                    return;
                }
                else
                {
                    _OccFromT6 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromT6)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromT3
        /// </summary>
        [DoNotNotify]
        public bool OccFromT3
        {
            get => _OccFromT3;
            set
            {
                if (value == _OccFromT3)
                {
                    return;
                }
                else
                {
                    _OccFromT3 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromT3)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for CtrlOff
        /// </summary>
        [DoNotNotify]
        public bool CtrlOff
        {
            get => _CtrlOff;
            set
            {
                if (value == _CtrlOff)
                {
                    return;
                }
                else
                {
                    _CtrlOff = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(CtrlOff)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromBlock23B
        /// </summary>
        [DoNotNotify]
        public bool OccFromBlock23B
        {
            get => _OccFromBlock23B;
            set
            {
                if (value == _OccFromBlock23B)
                {
                    return;
                }
                else
                {
                    _OccFromBlock23B = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromBlock23B)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromBlock22B
        /// </summary>
        [DoNotNotify]
        public bool OccFromBlock22B
        {
            get => _OccFromBlock22B;
            set
            {
                if (value == _OccFromBlock22B)
                {
                    return;
                }
                else
                {
                    _OccFromBlock22B = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromBlock22B)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromBlock9B
        /// </summary>
        [DoNotNotify]
        public bool OccFromBlock9B
        {
            get => _OccFromBlock9B;
            set
            {
                if (value == _OccFromBlock9B)
                {
                    return;
                }
                else
                {
                    _OccFromBlock9B = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromBlock9B)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for OccFromBlock9B
        /// </summary>
        [DoNotNotify]
        public bool OccFromBlock21B
        {
            get => _OccFromBlock21B;
            set
            {
                if (value == _OccFromBlock21B)
                {
                    return;
                }
                else
                {
                    _OccFromBlock21B = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(OccFromBlock21B)));
                }
            }
        }

        /// <summary>
        /// Get/Set and generate event for VoltageDetected
        /// </summary>
        [DoNotNotify]
        public bool VoltageDetected
        {
            get => _VoltageDetected;
            set
            {
                if (value == _VoltageDetected)
                {
                    return;
                }
                else
                {
                    _VoltageDetected = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(VoltageDetected)));
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor to instantiate all variables used for trackcontrol
        /// </summary>
        public TrackApplicationVariables()
        {
            

 
        }
        #endregion

        //// Event declaration using EventHandler<T>
        //public event EventHandler<EthernetDataEventArgs> EthernetDataReceived;

        //// Method to simulate receiving Ethernet data
        //public void ReceiveEthernetData(string data)
        //{
        //    // Process the data, e.g., store it in a variable
        //    // ...

        //    // Raise the event to notify subscribers
        //    OnEthernetDataReceived(new EthernetDataEventArgs(data));
        //}

        //// Method to raise the event
        //protected virtual void OnEthernetDataReceived(EthernetDataEventArgs e)
        //{
        //    // Check if there are subscribers
        //    EthernetDataReceived?.Invoke(this, e);
        //}
    }
}
