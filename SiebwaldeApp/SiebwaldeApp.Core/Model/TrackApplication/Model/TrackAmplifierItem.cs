using PropertyChanged;
using System;
using System.Collections;
using System.ComponentModel;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Information about a Trackamplifier
    /// </summary>
    public class TrackAmplifierItem : INotifyPropertyChanged
    {
        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };

        #region Private Variables

        private ushort mSlaveNumber;
        private ushort[] mHoldingReg = new ushort[12];
        private ushort mMbReceiveCounter;
        private ushort mSlaveDetected;
        private ushort mSpiCommErrorCounter;
        private ushort mMbExceptionCode;
        private uint mMbCommError;
        private ushort mMbSentCounter;

        #endregion

        #region Public Methods

        /// <summary>
        /// Get/Set and generate event for SlaveNumber
        /// </summary>
        [DoNotNotify]
        public ushort SlaveNumber {
            get => mSlaveNumber;            
            set
            {
                if (value == mSlaveNumber)
                {
                    return;
                }
                else
                {
                    mSlaveNumber = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(SlaveNumber)));
                }                               
            } 
        }

        /// <summary>
        /// Get/Set and generate event for SlaveDetected
        /// </summary>
        [DoNotNotify]
        public ushort SlaveDetected
        {
            get => mSlaveDetected;
            set
            {
                if (value == mSlaveDetected)
                {
                    return;
                }
                else
                {
                    mSlaveDetected = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(SlaveDetected)));
                }                
            }
        }

        /// <summary>
        /// Get/Set and generate event for HoldingReg
        /// </summary>
        [DoNotNotify]
        public ushort[] HoldingReg
        {
            get => mHoldingReg;
            set
            {
                if (((IStructuralEquatable)mHoldingReg).Equals(value, StructuralComparisons.StructuralEqualityComparer))
                //if (value == mHoldingReg)
                {
                    return;                    
                }
                else
                {
                    //mHoldingReg = value;
                    Array.Copy(value, 0, mHoldingReg, 0, value.Length);
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HoldingReg)));
                }           
            }
        }

        /// <summary>
        /// Get/Set and generate event for MbReceiveCounter
        /// </summary>
        [DoNotNotify]
        public ushort MbReceiveCounter
        {
            get => mMbReceiveCounter;
            set
            {
                if (value == mMbReceiveCounter)
                {
                    return;
                }
                else
                {
                    mMbReceiveCounter = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(MbReceiveCounter)));
                }                
            }
        }

        /// <summary>
        /// Get/Set and generate event for MbSentCounter
        /// </summary>
        [DoNotNotify]
        public ushort MbSentCounter
        {
            get => mMbSentCounter;
            set
            {
                if (value == mMbSentCounter)
                {
                    return;
                }
                else
                {
                    mMbSentCounter = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(MbSentCounter)));
                }                
            }
        }

        /// <summary>
        /// Get/Set and generate event for MbCommError
        /// </summary>
        [DoNotNotify]
        public UInt32 MbCommError
        {
            get => mMbCommError;
            set
            {
                if (value == mMbCommError)
                {
                    return;
                }
                else
                {
                    mMbCommError = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(MbCommError)));
                }                
            }
        }

        /// <summary>
        /// Get/Set and generate event for MbExceptionCode
        /// </summary>
        [DoNotNotify]
        public ushort MbExceptionCode
        {
            get => mMbExceptionCode;
            set
            {
                if (value == mMbExceptionCode)
                {
                    return;
                }
                else
                {
                    mMbExceptionCode = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(MbExceptionCode)));
                }                
            }
        }

        /// <summary>
        /// Get/Set and generate event for SpiCommErrorCounter
        /// </summary>
        [DoNotNotify]
        public ushort SpiCommErrorCounter
        {
            get => mSpiCommErrorCounter;
            set
            {
                if (value == mSpiCommErrorCounter)
                {
                    return;
                }
                else
                {
                    mSpiCommErrorCounter = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(SpiCommErrorCounter)));
                }                
            }
        }

        #endregion
    }
}
