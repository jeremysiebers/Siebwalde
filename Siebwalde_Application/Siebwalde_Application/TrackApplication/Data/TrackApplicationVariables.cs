using System;
using System.Collections.Generic;

namespace Siebwalde_Application
{ 
    public class TrackApplicationVariables
    {
        /// <summary>
        /// Variables of this class
        /// </summary>
        public List<TrackAmplifierItem> trackAmpItems;
        private TrackAmplifierItem trackAmp;
        
        /// <summary>
        /// Constructor to instantiate all varaibles used for trackcontrol
        /// </summary>
        public TrackApplicationVariables()
        {
            #region Instantiate List of TrackAmplifierItem and add items

            ushort[] HoldingRegInit = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            trackAmpItems = new List<TrackAmplifierItem>();

            for (ushort i = 0; i < 56; i++)
            {
                trackAmpItems.Add(trackAmp = new TrackAmplifierItem
                {
                    SlaveNumber = i,
                    SlaveDetected = 0,
                    HoldingReg = HoldingRegInit,
                    MbReceiveCounter = 0,
                    MbSentCounter = 0,
                    MbCommError = 0,
                    MbExceptionCode = 0,
                    SpiCommErrorCounter = 0
                });
            }

            #endregion
        }

        #region Method GetAmplifierListing()

        /// <summary>
        /// Helper function to get amplifier list
        /// </summary>
        /// <returns></returns>
        public List<TrackAmplifierItem> GetAmplifierListing()
        {
            return trackAmpItems;
        }

        #endregion
    }
}
